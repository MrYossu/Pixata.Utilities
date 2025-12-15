# Pixata.Functional [![Pixata.Functional Nuget package](https://img.shields.io/nuget/v/Pixata.Functional)](https://www.nuget.org/packages/Pixata.Functional/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Functional/Icon/Explorer.png "Pixata")

>Please note that I am no longer maintaining this package. Some of the code here depends on code generation, which doesn't seem to work very well since .NET8 (see [this issue in the LanguageExt repo](https://github.com/louthy/language-ext/issues/1378)). Also, I'm using LanguageExt less and less, as it's far more complex than my little brain can handle! I was only using a few core features, and found it easier to implement those myself, and not have the extra weight of the full package. I'd love to have the time to learn the full functional programming paradigm, but I don't. So, this package is being left as-is for anyone who finds it useful.


Some classes that I found useful when doing functional programming (FP). These are dependent on the rather excellent LanguageExt Nuget package.  If you aren't familiar with this, I very strongly recommend you read [Functional Programming in C#](https://www.manning.com/books/functional-programming-in-c-sharp?query=functional%20programming%20c#), which is one of the best C# books I've read (and re-read, and re-read...) for a long time. Once you are familiar with the concepts, and want to use them, grab a reference to LanguageExt and watch your code become more elegant, more robust, and easier to read.

A [Nuget package](https://www.nuget.org/packages/Pixata.Functional/) is available for this project.

## Fallible
One of the great benefit of FP is that it makes you think about all the ways your code can behave. For example, pulling an entity from the database may return something, or may not, depending on whether the `Id` you passed in was valid. Non-FP code often ignores the latter case...

```c#
var jim = await Context.Customers.SingleAsync(c => c.Id == id);
// Do something with jim, and wait for the NullReferenceException when an invalid Id is passed in
```

FP is much more elegant. You can map the database entity to an `Option<Customer>` and then call `Match()` on it...

```c#
private async Task<Option<Customer>> GetCustomer(int id) =>
  await Context.Customers.SingleAsync(c => c.Id == id);

GetCustomer(int id)
  .Match(c => {
      // Do something with the customer
    },
    () => {
      // The customer wasn't found. Inform the user and feel smug that you didn't have to handle an NRE!
    });
```

As you are handling an `Option<Customer>`, you are forced to consider the `None` case, and so are protected from NREs.

I had a slightly more complex version of this scenario. My database entities all have a `LastUpdatedAt` column (`datetime`) and a `LastUpdatedByUserId` column. Whenever an entity is saved, these two are updated, so we can see who last changed the entity and when.

I want to intercept database updates, and check if the database entity has been updated since the incoming one. If so, I don't want to save the incoming entity, but want to notify the calling code so that it can warn the user. Something like this...

```c#
GetCustomer(customer)
  .Match(c => {
      // Do whatever you do when everything works (apart from breathing a sigh of relief of course!)
    },
    ex => {
      // Exception thrown in the database code
      // Inform the user that something went wrong
    },
    msg => {
      // Bad idea, eg if the customer being saved is out of date.
      // Inform the user that they need to refresh their data and try again
    });

```

For this, I really needed a 3-way monad (oh no, the "M" word!). Predictably, LanguageExt was there to help, as it provides the `[Free]` attribute, which allows you to generate monads for free. Unfortunately, it didn't generate a `Match` function for me, but that was so easy to write that it didn't matter.

So, with a reference to this package, you can do all of the above.

Creating a `Fallible` is very easy...

```c#
Fallible<int> f1 = Fallible.Success(12);
Fallible<int> f2 = Fallible.BadIdea<int>("don't like that number");
Fallible<int> f3 = Fallible.Failure<int>(new Exception("divide by square root of -1"));
```

Using one is basically as shown above. Here we just write something to the console as an example...

```c#
f1.Match(n => Console.WriteLine($"Success: {n}"), 
  ex => Console.WriteLine($"Failure: {ex.Message}"), 
  msg => Console.WriteLine($"BadIdea: {msg}"));
```

You can produce `Fallible`s as follows...

```c#
private static Fallible<double> Sqrt(double d) {
  try {
    return d < 0 
      ? Fallible.BadIdea<double>("Can't take the square root of a negative number (without using complex numbers anyway)") 
      : Fallible.Success(Math.Sqrt(d));
  }
  catch (Exception ex) {
    return Fallible.Failure<double>(ex);
  }
}
```

I think there must be a better way of coding the above, but I haven't had time to look at it yet. Enrico Buonanno's book (linked right up at the top) shows how to lift the child types into the parent type without needing so much boilerplate code. I need to read the book (yet) again, and will hopefully be able to add code to improve the way we create a `Fallible`.