# Pixata.Utilities [![Release status](https://github.com/MrYossu/Pixata.Utilities/workflows/release/badge.svg)](https://github.com/MrYossu/Pixata.Utilities/actions) [![Master status](https://github.com/MrYossu/Pixata.Utilities/workflows/master/badge.svg)](https://github.com/MrYossu/Pixata.Utilities/actions)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Borsalino.png "Pixata")

I seem to keep adding the same utility classes to every project I work on, resulting in (almost) identical code spread across different projects.

I used to get around this by maintaining the utilities in a separate project, and referencing the DLL. However, apart from being soooo 1990s, it also has problems like VS locking the DLL, preventing you from building the utility project and so on.

So, I decided to take a leap into the modern world, and try publishing [some Nuget packages](https://www.nuget.org/packages?q=pixata). This is the first time I've done this, so goodness knows what's going to happen.

These are offered as-is, with no guarantee that they will work for you. I'm still working on them, and cannot offer much in the way of support, although I'd be very happy to receive any feedback you have.

## Documentation
I know, I really ought to document them. I probably will at some point, but haven't had time yet.

## Pixata.Extensions
Some useful utility classes and methods I've developed over the past few years. I have only put in a few so far, and the test project is <strike>woefully empty</strike> less desolate than it used to be, but hopefully that will change over time.

A [Nuget package](https://www.nuget.org/packages/Pixata.Extensions/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions).

## Pixata.Blazor
I have [blogged about some Blazor components I've been writing](https://www.pixata.co.uk/tag/blazor/). This project contains the source for those components.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor).

## Pixata.Blazor.Sample
The source code for a sample web project that uses the Blazor components. You can see a [live version of this sample](https://test.pixata.co.uk/), or [browse the source code](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Sample).

## Pixata.Blazor.LanguageExtComponents
Some Blazor components for us with the rather excellent [LanguageExt](https://github.com/louthy/language-ext/) Nuget package.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.LanguageExtComponents/) is available for this project.

See the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.LanguageExtComponents) for more details.

## Blog
Random thoughts and notes that are sometimes useful, and sometimes related to these components can be found on [my blog](https://www.pixata.co.uk/).

## Things to do
Some of the more important issues that need addressing...

* The Pixata.Blazor project should be split into two, one that doesn't rely on Telerik, and one that does. That way anyone who wants to use most of the compoents, but doesn't have a licence for Telerik still can
* The form components in the Blazor project need a common base class pulling out, as they are all almost identical
* Those form components also need to use the built-in Blazor form components, not the HTML form elements
