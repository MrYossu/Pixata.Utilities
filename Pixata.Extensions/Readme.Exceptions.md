# Exception extension methods

>Part of [Pixata.Extensions](Readme.md)

Extension methods for working with exceptions, found in `ExceptionExtensions`.

`MessageStack()` - Returns the exception messages all the way down the `InnerException` stack, including the stack traces. There is an overload that accepts a `separator` string; the default uses `Environment.NewLine`.

`Messages()` - Similar to `MessageStack()`, but returns only the messages, with no stack traces. An overload accepts a `separator` string; the default is `Environment.NewLine`.

`InnerType()` - Returns the type name of the innermost exception.
