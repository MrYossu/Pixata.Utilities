# Pixata.Utilities [![Release status](https://github.com/MrYossu/Pixata.Utilities/workflows/release/badge.svg)](https://github.com/MrYossu/Pixata.Utilities/actions?query=workflow%3Arelease) [![Master status](https://github.com/MrYossu/Pixata.Utilities/workflows/master/badge.svg)](https://github.com/MrYossu/Pixata.Utilities/actions?query=workflow%3Amaster) ![Last commit to master](https://img.shields.io/github/last-commit/MrYossu/Pixata.Utilities/master)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Borsalino.png "Pixata")

I seem to keep adding the same utility classes to every project I work on, resulting in (almost) identical code spread across different projects.

I used to get around this by maintaining the utilities in a separate project, and referencing the DLL. However, apart from being soooo 1990s, it also has problems like VS locking the DLL, preventing you from building the utility project and so on.

So, I decided to take a leap into the modern world, and try publishing [some Nuget packages](https://www.nuget.org/packages?q=pixata). This is the first time I've done this, so goodness knows what's going to happen.

These are offered as-is, with no guarantee that they will work for you. I'm still working on them, and cannot offer much in the way of support, although I'd be very happy to receive any feedback you have.

## Documentation
Most of the packages have some documentation in the form of Readme files in their respective project folders. A brief expalantion of each package is below, see the project Readme files for more details.

## Pixata.Extensions
Some useful utility classes and methods I've developed over the past few years. I have only put in a few so far, and the test project is <strike>woefully empty</strike> less desolate than it used to be, but hopefully that will change over time.

A [Nuget package](https://www.nuget.org/packages/Pixata.Extensions/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions).

## Pixata.Email
Given that every app I write needs to send emails, I got fed up of writing the same boilerplate code, so I wrapped it in a package for easy reuse.

A [Nuget package](https://www.nuget.org/packages/Pixata.Email/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Email).

## Pixata.AspNetCore
A small, but hopefully growing collectoin of utilities that I have found useful when writing ASP.NET Core web applications. These compliment the components in Pixata.Blazor, but as they are not Blazor-specific, are kept in a separate package.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore).

## Pixata.Blazor
Over the years since I started using Blazor, I (like many others) have developed some components that I find myself reusing regularly. This project contains those components.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor).

## Pixata.Blazor.Sample
The source code for a sample web project that uses the Blazor components. You can see a [live version of this sample](https://test.pixata.co.uk/), or [browse the source code](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Sample).

## Pixata.TelerikComponents
Some components for use with [Telerik UI for Blazor](https://www.telerik.com/blazor-ui).

A [Nuget package](https://www.nuget.org/packages/Pixata.TelerikComponents/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.TelerikComponents).

## Pixata.Google
Utility code for working with Google's APIs. I wrote an app that used Google Drive for storage, and found the code to be very complex and messy, so I wrapped it up in a clearer API for easier use. I was thinking about adding support for other Google APIs too, but had such a painful experience with Drive that I never got around to it. If you are looking to code against other Google APIs, the code here may give you a head start.

A [Nuget package](https://www.nuget.org/packages/Pixata.Google/) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Google).

## Pixata.SimilarityChooser 
A utility that checks for similar items. Useful for when your users don't bother checking to see if the data they want already exists, and add a duplicate. The Similarity Chooser looks through your existing items and lets you know which may match.

A [Nuget package](https://www.nuget.org/packages/Pixata.SimilarityChooser /) is available for this project.

You can find more detail on the [project page](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.SimilarityChooser).

## Pixata.Blazor.LanguageExtComponents and Pixata.Functional
Please note that I am no longer maintaining these projects, as I am moving away from LanguageExt. This is no reflection on that excellent package, it's more a reflection of my lack of understanding of how to use it properly! I am leaving the code here in case it is useful to anyone else.

## Blog
Random thoughts and notes that are sometimes useful, and sometimes related to these components can be found on [my blog](https://www.pixata.co.uk/).