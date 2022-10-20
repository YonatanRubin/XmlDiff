# XML DIFF
## Quick Start
```cmd
nuget install XmlDiff
```
To compare two documents:
```csharp
var a=XElement.Load("a.xml");
var b=XElement.Load("b.xml");
FlatDiffComparer comparer=new FlatDiffComparer();
IEnumerable<FlatNodeDiff> diffs=comparer.Compare(a,b);
```
To find all tags `sub` that were deleted 
```csharp
diffs.Where(diff => diff.Name == "sub" && diff.GetChangeType() == ChangeType.Removed);
```
To filter all diffs under tag `ignore` you can use the [ComplexFlatDiffComparer](docs/ComplexFlatDiffComparer.md)
```csharp
ComplexFlatDiffComparer comparer=new ComplexFlatDiffComparer(new[]{"ignore"});
```
## Use Case
This library allows you to compute the absolute difference between two xml files.
It is therefore best used when computing the diff between similar xml objects.
See example **[2]**

It will try to minimize the number of changes.
Meaning that some changes might logically come from multiple changes. See example **[1]**.
This is deliberate to prevent sticky situations with overly complex changes based on the order of the items.  
Keep in mind that for complex items in list you should use the [`IdPaths` feature](docs/ComplexFlatDiffComparer.md#IdPaths) and read more about it's known problems [here](!2) 
<table>
<tr>
<th>Example No.</th>
<th>Document 1</th>
<th>Document 2</th>
<th>Diffs</th>
</tr>
<tr>
<td>1</td>
<td>

```xml
<document>
    <array>
        <item>1</item>
        <item>2</item>
        <item>3</item>
    </array>
</document>
```

</td>

<td>

```xml
<document>
    <array>
        <item>4</item>
        <item>1</item>
    </array>
</document>
```

</td>
<td>

even though logically it may look like there were three changes:
* 2 was removed
* 3 was removed
* 4 was inserted

in reality it will detect 2 changes:
* 2/3 was removed
* 2/3 was changed to 4

</td>
</tr>
<tr>
<td>2</td>
<td>

```xml
<dog>
    <name>Buffy</name>
    <kind>Golden Retriever</kind>
</dog>
```

</td>

<td>

```xml
<car>
    <company>hundai</company>
    <type>i30</type>
    <year>2013</year>
</car>
```

</td>
<td>

will have multiple diffs:
* name and kind removed
* company type and year changed 
* the object type was changed from dog to car

</td>
</tr>
</table>


