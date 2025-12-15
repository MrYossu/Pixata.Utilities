# Pixata.Blazor.LanguageExtComponents [![Pixata.Blazor.LanguageExtComponents Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor.LanguageExtComponents)](https://www.nuget.org/packages/Pixata.Blazor.LanguageExtComponents/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor.LanguageExtComponents/Icon/performance%20systeme%20OS.png "Pixata") 

>Please note that I am no longer maintaining this package. Some of the code here depends on code generation, which doesn't seem to work very well since .NET8 (see [this issue in the LanguageExt repo](https://github.com/louthy/language-ext/issues/1378)). Also, I'm using LanguageExt less and less, as it's far more complex than my little brain can handle! I was only using a few core features, and found it easier to implement those myself, and not have the extra weight of the full package. I'd love to have the time to learn the full functional programming paradigm, but I don't. So, this package is being left as-is for anyone who finds it useful.

This project contains Blazor components that are useful when working with the rather excellent [LanguageExt](https://github.com/louthy/language-ext/) Nuget package. If you aren't familiar with this, I very strongly recommend you read [Functional Programming in C#](https://www.manning.com/books/functional-programming-in-c-sharp?query=functional%20programming%20c#), which is one of the best C# books I've read (and re-read, and re-read...) for a long time. Once you are familiar with the concepts, and want to use them, grab a reference to LanguageExt and watch your code become more elegant, more robust, and easier to read.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.LanguageExtComponents/) is available for this project.

## The components
### OptionBlazor
One of the first monads (oh no, the m-word!) that you'll probably come across is `Option<T>`. This is a very robust way of handling missing values, whilst avoiding null reference exceptions. If you have an `Option<T>` and want to use it in a Blazor component, then the `OptionBlazor` component will help.

### BusyOption
This is an `Option<T>` version of the [`Busy`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Containers/Busy.razor) component in the [Pixata.Blazor package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor).