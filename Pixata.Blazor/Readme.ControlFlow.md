# Replacing Razor code with declarative components

>Part of [Pixata.Blazor](Readme.md)

> **Note:** Since adding these components, I have discovered that intermittently, my apps would randomly stop working without any exceptions or errors being surfaced. This doesn't happen very often, but once it happens, it can stick. For that reason, I recommend using these components with caution. If you encounter any weird, intermittent issues, try replacing these components with the equivalent `@if`, `@switch` and `@foreach` to see if that resolves the problem.

I have found some issues using some `@` statements in Razor markup. For one, Visual Studio seems to have its own ideas about how the braces should be formatted, and these are usually different from my ideas! Also, the functionality from the rather fabulous [ZenCoding](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ZenCoding) extension doesn't work consistently with `@` statements.

For this reason, I have added some components to replace these statements with Blazor components.

## If

The `If` component replaces the Razor `@if` statement. Usage is pretty simple. Assume `_n` is an `int` variable...

```xml
<If Condition="@(_n > 10)">
  <Then>
    <p>Number is greater than 10</p>
  </Then>
  <ElseIf Condition="@(_n == 5)">
    <p>Number is 5</p>
  </ElseIf>
  <ElseIf Condition="@(_n > 5)">
    <p>Number is greater than 5</p>
  </ElseIf>
  <Else>
    <p>Number is 5 or less</p>
  </Else>
</If>
```

The `ElseIf` and `Else` components are optional, and you can have as many `ElseIf` components as you like. The `Condition` parameter is a `bool` that determines if the content is displayed.

**Note:** This package also contains a deprecated `Conditional` component, which is a more basic version of the above. It takes an `IfTrue` boolean, an `If` fragment and an optional `Else` fragment. Apart from the fact that "if", "then" and "else" are keywords familiar to generations of programmers, `Conditional` only allows for the "if" clause and an optional "else". By contrast, `If` allows for as many "else if" clauses as you like.

## Switch

The `Switch` component replaces the Razor `@switch` statement. Assume the same `int` variable as above...

```xml
<Switch Variable="@_n">
  <Case Equals="1">
    <p>n is one</p>
  </Case>
  <Case Equals="2">
    <p>n is two</p>
  </Case>
  <Default>
    <p>n is not one or two</p>
  </Default>
</Switch>
```

## ForEach

The `ForEach` component allows you to replace usages of `@foreach` with a component...

```xml
<ul>
  <ForEach Collection="@Enumerable.Range(0, 3)">
    <Each Context="n">
      <li>@n</li>
    </Each>
  </ForEach>
</ul>
```

The `Collection` parameter can be any `IEnumerable<T>`. The `Context` parameter on `Each` supplies an item from the collection.

In the case above, you can use the `EachFunc` parameter to do the same with much less code...

```xml
<ul>
  <ForEach Collection="@Enumerable.Range(0, 3)" EachFunc="@(n => $"<li>{n}</li>")" />
</ul>
```

**Note:** If you specify both `Each` and `EachFunc`, `Each` alone will be used.
