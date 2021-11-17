# Pixata.Google [![Release status](https://github.com/MrYossu/Pixata.Google/workflows/release/badge.svg)](https://github.com/MrYossu/Pixata.Google/actions?query=workflow%3Arelease) [![Master status](https://github.com/MrYossu/Pixata.Google/workflows/master/badge.svg)](https://github.com/MrYossu/Pixata.Google/actions?query=workflow%3Amaster) ![Last commit to master](https://img.shields.io/github/last-commit/MrYossu/Pixata.Google/master)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Google/Icon/PixataGoogle.png "Pixata")

This project contains utility code for working with Google's APIs. At the moment, it onl contains one class, but I'll refer to "classes" (in the plural) below, as I will hopefully add more as time goes on.

Note that the Google logo is (c) Google, and is used without permission. I hope that doesn't get me into trouble!

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.LanguageExtComponents/) is available for this project.

## Setting up Google authentication

This class was designed to be used within an ASP.NET Core application, and allows easy access to a Google Drive account.

In order to use this class, you need to follow a small convention. I would like to be able to relax this, but haven't found a way yet, so bear with me...

You will need to create a credentials file for your Google account. For details, see the [.NET Quickstart](https://developers.google.com/drive/api/v3/quickstart/dotnet), and follow the first few steps. This will give you a JSON file, which you will need to save as `credentials.json` in a top-level folder named `Google`...

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Google/Icon/GoogleFolder.png "A false match") 

When you first run the application, you will be prompted to autheticate. This will create a folder named `Token` in the `Google` folder (see the image above), which will contain a JSON file with the details needed for your application to access Google Drive with the account you used when authenticating.

Note that you will need to edit `credentials.json` to include the URL for the application. You may need to add two entries, one with and one without a trailing slash...

```json
"redirect_uris": [
  "https://localhost:44365/authorize/",
  "https://localhost:44365/authorize"
],
```

Make sure that the port number shown in the file matches the one you see in your browser when running the site. When you come to deploy your application, don't forget to add the URIs for the deployed site. If you get a Google error saying the URI doesn't match, ignore the one they suggest as it is almost always wrong! Double-check the URI in your browser and use that.

## LanguageExt
The classes here are based on the rather excellent [LanguageExt](https://github.com/louthy/language-ext/) Nuget package. What this means for you is that you will need to adopt a functional approach to using the methods in these classes. This gives a much more robust code base than would have been possible without.

In general, most methods return a `TryAsync<T>`, where `T` is the type of the value you want returned. This can be consumed with code like the following. Suppose you have a `List<File>` (where `File` is the Google Drive API type for a file or folder) named `Folders` and a `string` variable named `Msg` which is used to display messages to the user. You could then get the subfolders of a specified folder as follows...

```c#
    private async Task LoadSubfolders() =>
      (await GoogleDriveHelper.GetSubfolders(CurrentFolder.Id))
        .Match(
          folders => Folders = folders,
          ex => Msg = $"Ex: {ex.Message}");
```

The `Match` method takes two lambdas, one that is called when the operation succeeds (in which case we copy the folder list to `Folders`) and one that is called in case something went wrong with the call. In that case, an `Exception` is passed. In the sample, we just inform the user that an exception occurred.

If you are going to set the `Msg` variable in either case, then there is a simpler format. For example, the API call to create a folder returns the `Id` of the newly-created folder. In that case, informing the user of the result could be done like this...

```c#
private async Task CreateFolder(MouseEventArgs arg) =>
  Msg += (await GoogleDriveHelper.CreateFolder(_newFolderId, CurrentFolder.Id))
    .Match(id => $"New folder Id: {id}", ex => $"Ex: {ex.Message}");
```

Note that as both lambdas passed to `Match` return the same type (a `string`), we can move the assignment (`Msg += `) outside the call to `Match`.

If this isn't clear, then I very strongly recommend you read [Functional Programming in C#](https://www.manning.com/books/functional-programming-in-c-sharp?query=functional%20programming%20c#), which is one of the best C# books I've read (and re-read, and re-read...) for a long time. Once you are familiar with the concepts, the above will be much clearer.

## The classes
### GoogleDriveHelper
This allows easy access to a Google Drive account. You will need to inject an instance of the class into your code.

If you look in the source code, you can see the methods. They all have XML comments, so you should get hints when you use them as well.

Note that the API expects file and folder `Id`s, **not** names. This is easy to forget at first, and will result in errors.