using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using DeepEqual.Syntax;
using NUnit.Framework;
using XmlDiff.Models;

namespace XmlDiff.Tests
{
    [TestFixture]
    public class FlatDiffComparerTests
    {
        private FlatDiffComparer _comparer;

        [SetUp]
        public void SetUp()
        {
            _comparer = new FlatDiffComparer();
        }

        private static IEnumerable<object> files = new[]
        {
            null,
            new XElement("flat", 1),
            new XElement("notflat", new XElement("value", 1)),
            new XElement("list", new XElement("value", 1), new XElement("value", 2))
        };

        [TestCaseSource(nameof(files))]
        public void Compare_WithSameFile_ShouldReturnNoChange(XElement file)
        {
            // Act
            var result = _comparer.Compare(file, file);
            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Compare_WithValueChange_ShouldReturnChangeWithOldAndNew()
        {
            // Arrange
            XElement a = new XElement("item", 1);
            XElement b = new XElement("item", 2);
            // Act
            var result = _comparer.Compare(a, b);
            // Assert
            Assert.That(result, Has.One.Items);
            var diff = result.First();
            Assert.That(diff.A, Is.EqualTo("1"));
            Assert.That(diff.B, Is.EqualTo("2"));
        }

        [Test]
        public void Compare_WithKeyChangingName_ShouldReturnTwoChanges()
        {
            // Arrange
            XElement a = new XElement("item", new XElement("item", 1));
            XElement b = new XElement("item", new XElement("changed", 1));
            // Act
            var result = _comparer.Compare(a, b).ToArray();
            // Assert
            Assert.That(result, Has.Exactly(2).Items);
            Assert.That(result.Select(d => d.GetChangeType()), Contains.Item(ChangeType.Removed).And.Contains(ChangeType.Added));
        }

        [Test]
        public void Compare_WithValueRemoved_ShouldReturnRemovedDiffWithOld()
        {
            // Arrange
            XElement a = new XElement("item", 1);
            XElement b = new XElement("item");
            // Act
            IEnumerable<FlatNodeDiff> result = _comparer.Compare(a, b);
            // Assert
            FlatNodeDiff diff = result.First();
            Assert.That(diff.GetChangeType(), Is.EqualTo(ChangeType.Removed));
            Assert.That(diff.A, Is.EqualTo("1"));
        }

        [Test]
        public void Compare_WithValueAdded_ShouldReturnAddedDiffWithNew()
        {
            // Arrange
            XElement a = new XElement("item");
            XElement b = new XElement("item", 1);
            // Act
            IEnumerable<FlatNodeDiff> result = _comparer.Compare(a, b);
            // Assert
            FlatNodeDiff diff = result.First();
            Assert.That(diff.GetChangeType(), Is.EqualTo(ChangeType.Added));
            Assert.That(diff.B, Is.EqualTo("1"));
        }

        [Test]
        public void Compare_WithEmptyElementAddedOrRemoved_ShouldReturnNoChange()
        {
            // Arrange
            XElement withoutElement = new XElement("root");
            XElement withElement = new XElement("root", new XElement("item"));
            // Act
            var addedDiff = _comparer.Compare(withoutElement, withElement);
            var removedDiff = _comparer.Compare(withoutElement, withElement);
            // Assert
            Assert.That(addedDiff, Is.Empty);
            Assert.That(removedDiff, Is.Empty);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void Compare_WithValueAdded_ShouldReturnAddedChangeWithValue(int depth)
        {
            // Arrange
            XElement a = CreateElementByDepth(depth);
            XElement b = CreateElementByDepth(depth);
            EditRoot(a, element => element.RemoveAll());
            // Act
            IEnumerable<FlatNodeDiff> result = _comparer.Compare(a, b);
            // Assert
            FlatNodeDiff diff = result.First();
            Assert.That(diff.Attributes, Is.Empty);
            diff.WithDeepEqual(new FlatNodeDiff() {Name = "root", B = "1"}).Assert();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void Compare_WithValueDeleted_ShouldRemovedChangeWithOld(int depth)
        {
            // Arrange
            XElement a = CreateElementByDepth(depth);
            XElement b = CreateElementByDepth(depth);
            EditRoot(b, element => element.RemoveAll());
            // Act
            IEnumerable<FlatNodeDiff> result = _comparer.Compare(a, b);
            // Assert
            FlatNodeDiff diff = result.First();
            Assert.That(diff.Attributes, Is.Empty);
            diff.WithDeepEqual(new FlatNodeDiff() {Name = "root", A = "1"}).Assert();
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Compare_WithAttributeChanged_ShouldHaveAttributeDiff(int depth)
        {
            // Arrange
            var firstValue = CreateElementByDepth(depth);
            EditRoot(firstValue, element => element.Add(new XAttribute("attribute", "1")));
            var secondValue = CreateElementByDepth(depth);
            EditRoot(secondValue, element => element.Add(new XAttribute("attribute", "2")));
            var noValue = CreateElementByDepth(depth);
            // Act
            var removed = _comparer.Compare(firstValue, noValue);
            var added = _comparer.Compare(noValue, firstValue);
            var changed = _comparer.Compare(firstValue, secondValue);
            // Assert
            removed.First().Attributes.First().ShouldDeepEqual(new Diff<string>() {Name = "attribute", A = "1"});
            added.First().Attributes.First().ShouldDeepEqual(new Diff<string>() {Name = "attribute", B = "1"});
            changed.First().Attributes.First().ShouldDeepEqual(new Diff<string>() {Name = "attribute", A = "1", B = "2"});
        }

        [Test]
        public void Compare_WithOneChangeInList_ShouldChangeBetweenOneToAmountOfItems()
        {
            for (int i = 0; i < 3; i++)
            {
                // Arrange
                var list = new XElement("list", Enumerable.Range(0, 3).Select(i => CreateElementByDepth(2, i.ToString())));
                var changedList = new XElement(list);
                EditRoot(changedList, element => element.Value = "new", i);
                /* this is done to "move" one change, so the strictest algorithms (using order) will detect two changes
                   e.g. change 1,2,3 => 2,1,3 => new,1,3
                 */
                EditRoot(changedList, element => element.SetValue(i), (i + 1) % 3);
                // Act
                var changes = _comparer.Compare(list, changedList);
                // Assert
                Assert.That(changes.Count(), Is.AtLeast(1).And.AtMost(3));
            }
        }
        [Test]
        public void Compare_WithOneChangeInList_ShouldHaveAllNewValuesInNewDocument()
        {
            // Arrange
            var list = new XElement("list", Enumerable.Range(0, 3).Select(i => CreateElementByDepth(2, i.ToString())));
            var changedList = new XElement(list);
            EditRoot(changedList, element => element.Value = "new", 0);
            /* this is done to "move" one change, so the strictest algorithms (using order) will detect two changes
               e.g. change 1,2,3 => 2,1,3 => new,1,3
             */
            EditRoot(changedList, element => element.SetValue(0), 1);
            // Act
            var changes = _comparer.Compare(list, changedList);
            // Assert
            Assert.That(changes, Is.All.Matches(AllIn(changedList)));
        }

        [Test]
        public void Compare_WithElementAddedToList_ShouldHaveChangeAndAllValues()
        {
            // Arrange
            var list = new XElement("list", Enumerable.Range(0, 3).Select(i => CreateElementByDepth(2, i.ToString())));
            var changedList = new XElement(list);
            changedList.Add(CreateElementByDepth(2, "new"));
            // Act
            var changes = _comparer.Compare(list, changedList);
            // Assert
            Assert.That(changes.Count(), Is.AtLeast(1).And.AtMost(changedList.Elements().Count()));
            Assert.That(changes, Is.All.Matches(AllIn(changedList)));
        }

        [Test]
        public void Compare_WithElementChangedAndAddedInList_ShouldHaveAllValues()
        {
            // Arrange
            var list = new XElement("list", Enumerable.Range(0, 3).Select(i => CreateElementByDepth(2, i.ToString())));
            var changedList = new XElement(list);
            EditRoot(changedList,element => element.SetValue("change"));
            changedList.Add(CreateElementByDepth(2, "new"));
            // Act
            var changes = _comparer.Compare(list, changedList);
            // Assert
            Assert.That(changes, Is.All.Matches(AllIn(changedList)));
        }
        
        #region unsupported behaviour

        [Ignore("Currently you cannot detect root change")]
        [Test]
        public void Compare_WithRootChangingName_ShouldReturnTwoChanges()
        {
            // Arrange
            XElement a = new XElement("item", new XElement("item", 1));
            XElement b = new XElement("changed", new XElement("item", 1));
            // Act
            var result = _comparer.Compare(a, b).ToArray();
            // Assert
            Assert.That(result, Has.Exactly(2).Items);
            Assert.That(result.Select(d => d.GetChangeType()), Contains.Item(ChangeType.Removed).And.Contains(ChangeType.Added));
        }
        
        [Ignore("Currently you cannot find changes with same value")]
        [Test]
        public void Compare_WithItemAddedToListWithSameValue_ShouldHaveAdditionChange()
        {
            // Arrange
            var element = new XElement("list", new XElement("item", "1"));
            var newElement = new XElement(element);
            newElement.Add(new XElement("item", "1"));
            // Act
            var changes = _comparer.Compare(element, newElement);
            // Assert
            Assert.That(changes, Has.One.Items);
            Assert.That(changes.Single(), Has.Property("A").Null.And.Property("B").Not.Null);
        }
        
        [Ignore("Currently you cannot find changes with same value")]
        [Test]
        public void Compare_WithValueChangedInListWithOtherSimilarValues_ShouldHaveOneChange()
        {
            // Arrange
            var element = new XElement("list", new XElement("item", "1"),new XElement("item", "1"));
            var newElement = new XElement(element);
            newElement.Elements().Last().SetValue("2");
            // Act
            var changes = _comparer.Compare(element, newElement);
            // Assert
            Assert.That(changes, Has.One.Items);
            Assert.That(changes.Single(), Has.Property("A").Not.Null.And.Property("B").Not.Null);
        }

        #endregion
        private XElement CreateElementByDepth(int depth, string value = "1")
        {
            XElement element = null;
            if (value != null)
            {
                element = new XElement("root", value);
            }
            for (int i = 1; i < depth; i++)
            {
                element = new XElement("level" + i, element);
            }
            // root
            return element?.AncestorsAndSelf().Last();
        }

        private void EditRoot(XElement element, Action<XElement> edit, int index = 0)
        {
            if (element == null)
            {
                return;
            }
            edit?.Invoke(element.XPathSelectElements("//root").Skip(index).FirstOrDefault() ?? element);
        }

        private Predicate<FlatNodeDiff> AllIn(XElement element)
        {
            return diff => element.XPathSelectElements("//root").Select(n => n.Value).Contains(diff.B);
        }
    }
}
