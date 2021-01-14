# Pixata.Utilities

I seem to keep adding the same utility classes to every project I work on, resulting in (amlost) identical code spread across different projects.

I used to get around this by maintaining the utilities in a separate project, and referencing the DLL. However, apart from being soooo 1990s, it also has problems like VS locking the DLL, preventing you from building the utility project and so on.

So, I decided to take a leap into the mdoern world, and try publishing a Nuget package. This is the first time I've done this, so goodness knows what's going to happen.

I stated off with some Blazor components that I've been developing recently. These are offered as-is, with no guarantee that they will work for you. I'm still working on them, and cannot offer much in the way of support, although I'd be very happy to receive any feedback you have.

## Documentation
I know, I really ought to document them. I probably will at some point, but haven't had time yet. I've spent most of this afternoon trying to work out how to publish a Nuget package.

## Sample project
I have added a Blazor web project to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project.

## Blog
Random thoughts and notes that are sometimes useful, and sometimes related to these components can be found on [my blog](https://www.pixata.co.uk/).

## Warning
The package relies on the Telerik.Blazor Nuget package. If you don't have a subscription with Telerik, you can a 30-day trial version from them. I make no apologies for relying on this package, I ahve been using Telerik components for many years, and wouldn't develop without them.