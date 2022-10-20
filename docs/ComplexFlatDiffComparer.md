# ComplexFlatDiffComparer

The complex flat diff comparer allows you to add features for easier manipulation of the Diffs.
As a rule of thumb this contains features that **need to know about the context of the tree but not about the specific
diff algorithm**;
This mean that it usually need to work after a diff is already given. Currently it has the following features:  
[TOC]

## Blacklisting Paths

Let's assume there are certain items that you don't care when they change.

* This could be for example when the difference of the field is not the point of interest, but only the new value.
  For example it doesn't matter what was the previous length of a line to the store, it matters only how many people are
  left.
* This could also happen with items that change the value independently from time to time . For example getting the
  destination of a taxi does not change with time, but by customer.

To address this we give the option to filter an element and all of it's child elements.

<table>
<tr>
<th>Black Listed</th>
<th>Document 1</th>
<th>Document 2</th>
<th>Diffs</th>
</tr>
<tr>
<td>

```csharp
new []
{
    "rideNumber",
    "destination"
}
```

</td>
<td rowspan="2">

```xml

<taxi>
    <destination>
        <type>single point</type>
        <value>Broadway 1681</value>
    </destination>
    <passenger>joe</passenger>
    <time>16:30</time>
    <rate>13.8$</rate>
</taxi>
```

</td>
<td rowspan="2">

```xml

<taxi>
    <destination>
        <type>multiple points</type>
        <value>Kellerman;Havana</value>
    </destination>
    <passenger>john</passenger>
    <time>17:30</time>
    <rate>20$</rate>
</taxi>
```

</td>
<td>

```xml

<ArrayOfFlatNodeDiff>
    <FlatNodeDiff name="passenger" FullPath="taxi">
        <old>joe</old>
        <new>john</new>
    </FlatNodeDiff>
    <FlatNodeDiff name="time" FullPath="taxi">
        <old>16:30</old>
        <new>17:30</new>
    </FlatNodeDiff>
    <FlatNodeDiff name="rate" FullPath="taxi">
        <old>13.8$</old>
        <new>20$</new>
    </FlatNodeDiff>
</ArrayOfFlatNodeDiff>
```

</td>
</tr>
<tr>
<td>

```csharp
new []
{
    "taxi"
}
```

</td>
<td>

`empty`
</td>
</tr>
</table>

## IdPaths
Some complex items might include an Id object to differ between items.
This is especially useful when using a collection since it problematic to add a different key per item.  
For example what if we want to find all the classes where each student have improved. For this we will find the diff between two grade reports:
<table>
<tr>
<td>

```xml
<class>
  <students>
    <student>
      <name>Johnnie</name>
      <math>80</math>
      <cs>100</cs>
      <english>90</english>
    </student>
    <student>
     <name>Yarin</name>
      <math>100</math>
      <cs>90</cs>
      <english>80</english>
    </student>
    <student>
      <name>Ron</name>
      <math>90</math>
      <cs>80</cs>
      <english>100</english>
    </student>
  </students>
</class>
```
</td>
<td>

```xml
<class>
  <students>
    <student>
      <name>Johnnie</name>
      <math>80</math>
      <cs>100</cs>
      <english>90</english>
    </student>
    <student>
     <name>Yarin</name>
      <math>100</math>
      <cs>100</cs>
      <english>80</english>
    </student>
    <student>
      <name>Ron</name>
      <math>90</math>
      <cs>100</cs>
      <english>100</english>
    </student>
  </students>
</class>
```
</td>
<td>

```xml
<ArrayOfFlatNodeDiff>
  <FlatNodeDiff name="cs" FullPath="class/students/student">
    <old>90</old>
    <new>100</new>
  </FlatNodeDiff>
  <FlatNodeDiff name="cs" FullPath="class/students/student">
    <old>80</old>
    <new>100</new>
  </FlatNodeDiff>
</ArrayOfFlatNodeDiff>

```
</td>
</tr>
</table>

Oh no! We can't find out which student improved....  
Well, let's try again and this time give IdPath for student using it's name:

```csharp
new Dictionary<string, string>()
{
    ["student"] = "name"
}
```

Now the diff will have an `Id` attribute of the student.

```xml
<ArrayOfFlatNodeDiff>
  <FlatNodeDiff name="cs" FullPath="class/students/student" Id="Yarin">
    <old>90</old>
    <new>100</new>
  </FlatNodeDiff>
  <FlatNodeDiff name="cs" FullPath="class/students/student" Id="Ron">
    <old>80</old>
    <new>100</new>
  </FlatNodeDiff>
</ArrayOfFlatNodeDiff>
```
Nice! now we can see the student who changed the most:

```csharp
var studentChanges = diffs.GroupBy(diff => diff.Id).ToDictionary(student => student.Key, student => student.ToArray());
```