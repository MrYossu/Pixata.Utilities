# Pixata.Google [![Pixata.Google Nuget package](https://img.shields.io/nuget/v/Pixata.Google)](https://www.nuget.org/packages/Pixata.Google/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Google/Icon/PixataGoogle.png "Pixata")

This project contains utility code for working with Google's APIs. At the moment, it only contains one class, but I'll refer to "classes" (in the plural) below, as I will hopefully add more as time goes on.

Note that the Google logo is &copy;Google, and is used without permission. I hope that doesn't get me into trouble!

A [Nuget package](https://www.nuget.org/packages/Pixata.Google/) is available for this project.

## Setting up Google authentication

In order to use the package, you will need to set up authorisation with Google. **Note** that this package assumes you are doing server-to-server communication with Google, meaning that your app uses a pre-defined Google account, and the user does not need to know anything about it. If you want to use the user's Google account (ie have them sign in to Google to use your app), then you would need to use a slightly different method of authentication. It wouldn't be hard to add code to do that, but as I do't have the need for it, I haven't done it yet. If you need this approach, please [open an issue](https://github.com/MrYossu/Pixata.Utilities/issues) and ask. No promises, but I'll see what I can do!

These classes were designed to be used within an ASP.NET Core application. In order to use them, you need to follow a small convention. I would like to be able to relax this, but haven't found a way yet, so bear with me.

First, set up a [service account](https://developers.google.com/identity/protocols/oauth2/service-account) with Google. The service account will have a dummy email address, which you will be able to see in the credentials file you download.

Save the JSON file as `credentials.json` in the root folder of your ASP.NET application project.

Once that is done, you can inject the `GoogleDriveHelper` class into your controllers or Blazor components.

If you want to access a specific Google Drive, you will need to share a folder in that drive with the service account user. All of this is very well [explained in a short video](https://www.youtube.com/watch?v=Q5b0ivBYqeQ) by Linda Lawton, who is a certified Google Expert, and who gave me some very helpful guidance on getting this working. If you want to find the Id of a specific folder, see [this short video](https://www.youtube.com/watch?v=m3euwXcuvrs).

## LanguageExt
The classes here are based on the rather excellent [LanguageExt](https://github.com/louthy/language-ext/) Nuget package. What this means for you is that you will need to adopt a functional approach to using the methods in these classes. This gives a much more robust code base than would have been possible without.

Most of the methods return a `TryAsync<T>`, where `T` is the type of the value you want returned. This can be consumed with code like the following. Suppose you have a `List<File>` (where `File` is the Google Drive API type for a file or folder) named `Folders` and a `string` variable named `Msg` which is used to display messages to the user. You could then get the subfolders of a specified folder as follows...

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

As all methods return the same type, chaining them together is easy. Suppose you want to create a folder as a child of the folder with `Id` of `folderId`, then upload a file to the new child folder, you could do the following...

```c#
Msg += await GoogleDriveHelper.GetFolder(parentFolderId)
       .Bind(folder => GoogleDriveHelper.CreateFolder(newFolderName, folder.Id))
       .Bind(folderId => GoogleDriveHelper.UploadFile(stream, mimetype, folderId))
       .Match(newId => $"New Id: {newId}", ex => $"Ex: {ex.Message}");
```

If you prefer the fluent syntax, then you can do that instead...

```c#
Msg += from folder in GoogleDriveHelper.GetFolder(CurrentFolder.Id)
  from folderId in GoogleDriveHelper.CreateFolder(_newFolderId, folder.Id)
  from newId in GoogleDriveHelper.UploadFile(Stream.Null, "New file.txt", "text/text", folderId)
  select newId;
```

If this isn't clear, then I very strongly recommend you read [Functional Programming in C#](https://www.manning.com/books/functional-programming-in-c-sharp?query=functional%20programming%20c#), which is one of the best C# books I've read (and re-read, and re-read...) for a long time. Once you are familiar with the concepts, the above will be much clearer.

## GoogleDriveHelper
This allows easy access to a Google Drive account. You will need to inject an instance of the class into your code.

Note that the API expects file and folder Ids, **not** names. This is easy to forget at first, and will result in errors.

The class contains a `const string` named `RootFolderName`, which contains the name of the root folder (bet you didn't see that coming did you?), and is used when you want to check if the folder object you have is the root or not. As the `Id` will be different for everyone's individual Google account, this is the easiest way I could think of to check this. It will fail if you have a subfolder named "My Drive", in which case the only way to tell if you are at the root is to try and get the parent folder and see if the exceptional (second) action in the `TryAsync` call is executed. As this is slower and more code, it's only worth doing if you suspect there will be a subfolder named "My Drive".

In order to avoid ambiguity with the `System.IO.File` type, I have added the following type alias in the class...

`using DriveFile = Google.Apis.Drive.v3.Data.File;`

This aliased type is used below. If you don't want to do this, you can leave the type as `File` and make sure to ensure you don't mix it up with `System.IO.File`.

As mentioned above, (nearly) all of these methods return a `TryAsync`, so will need to be handled as shown earlier. For convenience, I will refer to the return type as the inner type (eg `DriveFile` or `string`) rather than the true return type (ie `TryAsync<DriveFile>>`, etc) as this is clearer.

### The methods
`TryAsync<About> GetAbout()` - Returns an `About` object that contains information about the drive. See [the Google API docs](https://developers.google.com/drive/api/v3/reference/about/get) for more information about the `About` object. This method only returns the `User` and `StorageQuota` sections, as these seemed to be the most useful.

`TryAsync<DriveFile> GetFolder(string folderId = "root")` - Returns an object representing the folder whose `Id` was passed

`TryAsync<DriveFile> GetParentFolder(string folderId)` - Returns an object representing the parent folder of the folder whose `Id` was passed

`TryAsync<List<DriveFile>> GetBreadcrumb(string folderId)` - Returns a `List<DriveFile>` representing the trail from the folder whose `Id` was passed up to the root folder

`TryAsync<List<DriveFile>> GetSubfolders(string folderId = "root")` - Returns all the immediate subfolders of the folder whose `Id` was passed

`TryAsync<string> CreateFolder(string folderName, string parentFolderId = "root")` - Creates a new folder with the given name. If the `parentFolderId` parameter is omitted, the new folder is created in the root

`TryAsync<List<DriveFile>> GetFilesInFolder(string folderId = "root")` - Returns all the files (not folders) in the specified folder

`TryAsync<Permission> SetAndGetPermission(string fileId, Permission permission)` - Sets a permission on a file. An example `Permission` object could look like `new Permission { Role = "reader", Type = "domain", Domain = "mydomain.com" }`. Returns the newly-created permission (not sure why you'd need this, but the API returns it, so I'm passing it back to you dear developer!). If you would prefer the file Id back (far more likely in my opnion), use the `SetPermission` method below.

`TryAsync<string> SetPermission(string fileId, Permission permission)` - Same as the previous method, but returns the file Id passed in. This is useful if you follow this call with code that refers to the file, rather than the permission.

`TryAsync<DriveFile> GetWebLink(string fileId)` - Gets a link that allows anyone with permissions to access the file. Note that you will probably need to set at least some permission on the file before you can generate a link. See the `SetPermission` and `SetAndGetPermission` methods above

`Try<byte[]> DownloadFile(string fileId)` - Gets the contents of the file as a byte array. Note that this method returns a `Try`, not a `TryAsync` like the other methods here, so should not (and connot) be awaited. Otherwise, the usage is the same.

`TryAsync<string> UploadFile(Stream file, string fileName, string mimeType, string folderId)` - Uploads a file to the folder whose `Id` is passed

`TryAsync<string> UpdateFile(string id, Stream file, string fileName, string mimeType)` - Updates an existing file

`TryAsync<Unit> MoveFile(string fileId, string newFolderId)` - Move the file to a new folder

`TryAsync<string> DeleteFile(string fileId)` - Deletes the file whose `Id` is passed