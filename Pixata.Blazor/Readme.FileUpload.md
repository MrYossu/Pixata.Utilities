# File select and upload

>Part of [Pixata.Blazor](Readme.md)

Two components for getting files from the user...

- `PixataFileSelect` lets the user pick (or drag and drop) files, and hands them to your code, with a stream for reading each one. Use it when your code wants the file's contents, eg to resize an image, parse a spreadsheet or save the file to a database.
- `PixataUpload` posts each file from the browser straight to an endpoint you provide, with a progress bar, cancel and retry buttons, and (if you want) chunked uploads that can be paused and resumed. Use it for large files, or when the files should go to a controller or minimal API endpoint rather than through your component. In Blazor Server, the file never goes through the SignalR connection.

They look and behave the same: a select button, with a drop area around it, and a list of the files picked, each with an icon for its type, its size, and a message if it isn't allowed. Both work on phones, where the select button opens the phone's own picker (including the camera), and from the keyboard.

There is a [live demo](https://test.pixata.co.uk/FileUploadSample) on the sample site, and you can see the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FileUploadSample.razor).

## Setup

There's nothing to register. Make sure you have the [Pixata.Blazor stylesheet](Readme.Styling.md) linked, and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

The components use a small JavaScript module (`_content/Pixata.Blazor/pixata-upload.js`), which they load themselves the first time one is shown, so you don't need to add a `<script>` tag. They are safe to prerender, as the script is only called once the component is interactive.

## Picking files and reading them

Handle `OnSelect`, and read each file's `Stream`...

```razor
<PixataFileSelect AllowedExtensions="@([".jpg", ".jpeg", ".png"])"
                  MaxFileSize="@(4 * 1024 * 1024)"
                  OnSelect="@Selected" />

@code {
  private async Task Selected(PixataFileSelectEventArgs args) {
    foreach (PixataFileSelectFileInfo file in args.Files.Where(f => f.IsValid)) {
      using MemoryStream copy = new();
      await file.Stream!.CopyToAsync(copy);
      await PhotoService.Save(file.Name, file.ContentType, copy.ToArray());
    }
  }
}
```

A few things to know about the stream...

- It reads the file from the browser as you go, so it has to be read asynchronously (`CopyToAsync()`, `ReadAsync()` and so on). The synchronous `Read()` throws.
- `Stream` can be read once. Once it has been read to the end, it lets go of the browser's copy of the file, so you don't need to dispose it (though doing so does no harm, and releases the file early if you stop part way). If you need to read the file again, call `OpenReadStream()`, which gives you a new stream each time. Its `maxAllowedSize` parameter (500 KB by default) is a safety net: it throws an `IOException` if the file is bigger, so set it to the most you're prepared to read.
- Read the file before it leaves the list, as the browser lets go of it then. You can read it in `OnSelect`, or later (eg when a form is saved) from `CurrentFiles`.
- Files that fail validation are still passed to `OnSelect` (so you can tell the user why), with `IsValid` set to false. Check it before using the file. Validation in the browser is a convenience for the user, not security, so check the file again on the server if it matters.

In Blazor Server, the file comes up through the SignalR connection, which is fine for photos and documents, but slow for very large files. Use `PixataUpload` for those.

`OnSelect` is raised before the files are added to the list. Set `IsCancelled` to add none of them, or take files out of `args.Files` to leave just those out.

## Uploading files to an endpoint

Point `SaveUrl` at an endpoint that accepts a form post...

```razor
<PixataUpload SaveUrl="@RoutesHelper.UploadSave"
              RemoveUrl="@RoutesHelper.UploadRemove"
              AllowedExtensions="@([".pdf", ".docx"])"
              MaxFileSize="@(20 * 1024 * 1024)"
              OnUpload="@AddDetails"
              OnSuccess="@Uploaded" />

@code {
  private void AddDetails(PixataUploadEventArgs args) {
    args.RequestHeaders["Authorization"] = $"Bearer {_token}";
    args.RequestData["customerId"] = CustomerId;
  }

  private void Uploaded(PixataUploadSuccessEventArgs args) {
    if (args.Operation == PixataUploadOperationType.Upload) {
      _uploaded.AddRange(args.Files.Select(f => f.Name));
    }
  }
}
```

By default, each file is uploaded as soon as it is picked. It is sent in its own request: a `POST` of `multipart/form-data`, with the file in the field named by `SaveField` (`files` by default), plus a field for each entry you add to `RequestData` in `OnUpload`, and a header for each entry in `RequestHeaders`. Values are converted to strings using the invariant culture. Any 2xx response counts as success; anything else (or not being able to reach the server) marks the file as failed, and the user can retry it. The response's status and text are passed to `OnSuccess` and `OnError` in `args.Request`, so the endpoint can send back something useful, eg the id it saved the file under.

When the user removes a file that has been uploaded, and you've set `RemoveUrl`, the component posts the file's name to that URL (in the field named by `RemoveField`, `files` by default), and only takes the file out of the list if that succeeds. `OnRemove` is raised first, and like `OnUpload`, lets you add headers and data, or cancel. Without a `RemoveUrl`, removing a file just takes it out of the list.

The requests are sent by the browser, so they carry the browser's cookies for your site, and your endpoint can use cookie authentication as normal. Set `WithCredentials` if the endpoint is on another domain and needs the cookies too (its CORS policy has to allow credentials).

Set `AutoUpload="false"` to let the user pick several files and then click Upload. The component shows Upload and Clear buttons under the list while there are files waiting. You can also call `UploadFiles()` yourself, eg from your form's Save button.

### Receiving the files

Your endpoint can read the file however you like, eg as an `IFormFile`. If you use Pixata.AspNetCore, its [`ReceiveUpload()`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.md#receiving-uploads) extension method does the work for you: it saves the file to a folder (sanitising the name, so a request can't write anywhere else), checks the extension and size again, and puts chunked uploads back together...

```csharp
app.MapPost(RoutesHelper.UploadSave, async (HttpRequest request) => {
  UploadReceiveResult result = await request.ReceiveUpload(uploadFolder, o => o.AllowedExtensions = [".pdf", ".docx"]);
  return result.Success ? Results.Ok() : Results.BadRequest(result.Error);
});

app.MapPost(RoutesHelper.UploadRemove, async (HttpRequest request) =>
  Results.Ok(await request.RemoveUploads(uploadFolder)));
```

Don't save uploads anywhere under `wwwroot` unless you want anyone to be able to download them.

### Chunked uploads

Large files can be sent in chunks, each in its own request. This gets around the server's limits on request size, means a failed chunk can be sent again rather than the whole file, and lets the user pause and resume the upload. Add a `PixataUploadChunkSettings` inside `UploadSettings`...

```razor
<PixataUpload SaveUrl="@RoutesHelper.UploadSave">
  <UploadSettings>
    <PixataUploadChunkSettings Size="@(512 * 1024)" MaxAutoRetries="3" />
  </UploadSettings>
</PixataUpload>
```

Each chunk is posted like a whole file would be, with the chunk in the `SaveField` field, your `RequestData`, and a JSON description of the chunk in the `chunkMetadata` field...

```json
{
  "fileId": "3f2b6c0e8a4d4b7f9c1e2d3a4b5c6d7e",
  "fileName": "survey.mp4",
  "fileSize": 52428800,
  "contentType": "video/mp4",
  "chunkIndex": 0,
  "totalChunks": 100,
  "offset": 0
}
```

`fileId` is the same for every chunk of one upload, and different for each upload (including a retry of the whole file), so the server knows which chunks belong together. `offset` is where the chunk starts in the file, so a chunk that is sent twice (because the response to the first attempt got lost) can overwrite itself rather than being added twice. The endpoint should return a 2xx response for each chunk it accepts. `OnUpload` is raised once per file, not per chunk, and its headers and data are sent with every chunk.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `true` | When false, files are sent whole |
| `Size` | `long` | 1 MB | The size of each chunk, in bytes |
| `AutoRetryAfter` | `int` | 100 | How long to wait before sending a failed chunk again, in milliseconds |
| `MaxAutoRetries` | `int` | 1 | How many times a failed chunk is sent again before the file is marked as failed |
| `Resumable` | `bool` | `true` | When true, the user can pause and resume the upload. Pausing lets the chunk being sent finish, and stops before the next one; resuming carries on from there |
| `MetadataField` | `string` | `chunkMetadata` | The form field the JSON description is sent in |

`PixataUploadChunkSettings` throws an `InvalidOperationException` if it isn't inside a `PixataUpload`.

## Dropping files somewhere else

Files can always be dropped on the component itself. To give the user a bigger target, or one elsewhere on the page, add a `PixataDropZone` and pass its `Id` to the component's `DropZoneId`...

```razor
<PixataDropZone Id="invoice-drop" NoteText="PDFs only, up to 20 MB" />

<PixataUpload SaveUrl="@RoutesHelper.UploadSave" DropZoneId="invoice-drop" AllowedExtensions="@([".pdf"])" />
```

Files dropped there are treated exactly like files picked with the button, so they are validated and raise `OnSelect` as usual. `DropZoneId` can be the id of any element, if you'd rather style your own.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string` | | Required. The id to pass to the component's `DropZoneId` |
| `Enabled` | `bool` | `true` | When false, the drop zone is greyed out and ignores files dropped on it |
| `Multiple` | `bool` | `true` | When false, only the first of several files dropped at once is taken |
| `HintText` | `string?` | "Drag and drop files here to upload" | The main text |
| `NoteText` | `string?` | | Smaller text under the hint, eg the kinds of file allowed |
| `Template` | `RenderFragment?` | | Replaces the icon, hint and note with your own content |
| `Height`, `Width` | `string?` | 220px, the container's width | The size, as CSS lengths |
| `DragOverClass` | `string?` | | Classes to add while files are dragged over the drop zone |
| `Class` | `string?` | | Extra classes for the drop zone |

## Files that are already there

Pass `Files` to show files in the list when the component first renders, eg ones the user uploaded on an earlier visit. It is only read once; after that, the component keeps its own list, which you can read from `CurrentFiles`.

```razor
<PixataUpload SaveUrl="@RoutesHelper.UploadSave" RemoveUrl="@RoutesHelper.UploadRemove" Files="@_existing" />

@code {
  private List<PixataUploadFileInfo> _existing = [
    new() { Id = "17", Name = "Contract.pdf", Size = 284512 }
  ];
}
```

Set each file's `Id` to something that means something to you, as it's what you pass to methods such as `RemoveFile()`. For `PixataUpload`, these files are treated as already on the server: their status is `Blank` unless you set it to `Uploaded` or `Failed`, and removing one sends a remove request. For `PixataFileSelect`, the browser doesn't have their contents, so their `Stream` is null.

## Taking photos on a phone

On a phone, the select button opens the phone's own picker, which offers the camera as well as files. Set `Accept="image/*"` to offer just images, and `Capture` to go straight to the camera (`environment` for the back camera, `user` for the front one)...

```razor
<PixataUpload SaveUrl="@RoutesHelper.UploadSave" Accept="image/*" Capture="environment" />
```

`Accept` only filters what the file dialog shows (the user can usually choose to see every file anyway), so use `AllowedExtensions` to actually reject other files.

## Templates

| Template | Context | Description |
| --- | --- | --- |
| `SelectFilesButtonTemplate` | | The content of the select button, in place of "Select files..." |
| `FileInfoTemplate` | `PixataFileTemplateContext<T>` | Replaces the file's name and size (or message), keeping the icon, progress bar and buttons |
| `FileTemplate` | `PixataFileTemplateContext<T>` | Replaces the whole of each item in the list, including the buttons and progress bar |

The context's `File` is the file (a `PixataFileSelectFileInfo` or a `PixataUploadFileInfo`)...

```razor
<PixataFileSelect>
  <SelectFilesButtonTemplate>Add photos</SelectFilesButtonTemplate>
  <FileInfoTemplate>
    <strong>@context.File.Name</strong>
    @if (!context.File.IsValid) {
      <em>Photos only, please</em>
    }
  </FileInfoTemplate>
</PixataFileSelect>
```

If you use `FileTemplate`, you're taking over the buttons too, so call the component's methods (through a `@ref`) to remove, cancel or retry files. `PixataSvgIcons.ForFile(extension)` gives you the icon the component uses for a file type.

## Reference

### Parameters (both components)

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `true` | When false, the buttons are disabled and files can't be picked or dropped. The list can still be read |
| `Multiple` | `bool` | `true` | When false, the user can only pick one file, and picking another replaces it |
| `Accept` | `string?` | | The file input's `accept` attribute, eg `image/*` or `.pdf,.docx`. Filters what the file dialog shows |
| `AllowedExtensions` | `IEnumerable<string>?` | | The extensions allowed, eg `.pdf`. The dot is optional and case is ignored. Other files are shown with a message, and aren't uploaded |
| `MinFileSize`, `MaxFileSize` | `long?` | | The smallest and largest files allowed, in bytes |
| `Id` | `string?` | | The id of the file input, so a `<label for="...">` opens the file dialog |
| `Capture` | `string?` | | The file input's `capture` attribute (`user` or `environment`) |
| `DropZoneId` | `string?` | | The id of a `PixataDropZone` (or any element) that files can also be dropped on |
| `AriaLabelledBy`, `AriaDescribedBy` | `string?` | | Ids of elements that label and describe the select button |
| `Files` | `IEnumerable<T>?` | | Files to show when the component first renders |
| `Messages` | `PixataUploadMessages?` | | The text the component shows, to translate or reword it |
| `Class` | `string?` | | Extra classes for the outer element. Any other attributes are added to it too |

### PixataUpload parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `SaveUrl` | `string?` | | The URL files are posted to. Uploading without one throws an `InvalidOperationException` |
| `RemoveUrl` | `string?` | | The URL that removed files' names are posted to. When null, removing a file just takes it out of the list |
| `SaveField` | `string` | `files` | The form field the file is sent in |
| `RemoveField` | `string` | `files` | The form field the file name is sent in when removing a file |
| `WithCredentials` | `bool` | `false` | Send cookies with requests to another domain |
| `AutoUpload` | `bool` | `true` | Upload files as soon as they are picked. When false, the user clicks Upload |
| `UploadSettings` | `RenderFragment?` | | Holds a `PixataUploadChunkSettings` |

### Events

| Event | Component | Args | Description |
| --- | --- | --- | --- |
| `OnSelect` | both | `PixataFileSelectEventArgs` / `PixataUploadSelectEventArgs` | Files were picked or dropped, and are about to be added to the list. Cancellable, and you can take files out of `Files` |
| `OnRemove` | both | `PixataFileSelectEventArgs` / `PixataUploadEventArgs` | The user is removing a file. Cancellable. For `PixataUpload`, add headers and data for the remove request |
| `OnUpload` | upload | `PixataUploadEventArgs` | A file is about to be uploaded. Add headers and data, or cancel (the file stays in the list, waiting) |
| `OnProgress` | upload | `PixataUploadProgressEventArgs` | A file's upload has moved on. `Progress` is a percentage |
| `OnSuccess` | upload | `PixataUploadSuccessEventArgs` | An upload or remove request succeeded. `Operation` says which, and `Request` has the response |
| `OnError` | upload | `PixataUploadErrorEventArgs` | An upload or remove request failed. `Operation` says which, and `Request` has the response |
| `OnCancel` | upload | `PixataUploadCancelEventArgs` | The user is cancelling an upload. Cancellable, which lets the upload carry on |
| `OnClear` | upload | `PixataUploadClearEventArgs` | The user clicked Clear. Cancellable |
| `OnPause`, `OnResume` | upload | `PixataUploadPauseEventArgs`, `PixataUploadResumeEventArgs` | The user is pausing or resuming a chunked upload. Cancellable |

Every event's args have a `Files` list. The cancellable ones have `IsCancelled`. `Request` (a `PixataUploadHttpRequest`) has the response's `Status`, `StatusText`, `ResponseType` and `ResponseText`; the status is 0 if the server couldn't be reached.

### Methods

Grab a reference to the component with `@ref` to call these. They are all async.

| Method | Component | Description |
| --- | --- | --- |
| `OpenSelectFilesDialog()` | both | Opens the file dialog. Browsers only allow this in response to something the user did, such as clicking a button |
| `RemoveFile(id)` | both | Removes a file, just as the remove button does |
| `ClearFiles()` | both | Empties the list (cancelling any uploads), without raising events or sending remove requests |
| `UploadFiles()` | upload | Uploads every valid file that is waiting. Finishes when they have all finished |
| `RetryFile(id)` | upload | Uploads a failed file again |
| `CancelFile(id)` | upload | Cancels a file's upload and takes it out of the list |
| `PauseFile(id)`, `ResumeFile(id)` | upload | Pauses or resumes a chunked upload |

`CurrentFiles` is the list of files.

### The files

Both kinds of file have `Id`, `Name`, `Size` (in bytes), `Extension` (with the dot), `ContentType`, `LastModified`, and the validation results `IsValid`, `InvalidExtension`, `InvalidMinFileSize` and `InvalidMaxFileSize`.

A `PixataFileSelectFileInfo` adds `Stream` and `OpenReadStream()`, described [above](#picking-files-and-reading-them).

A `PixataUploadFileInfo` adds `Progress` (a percentage) and `Status`, one of `Selected` (waiting to be uploaded), `Uploading`, `Paused`, `Uploaded`, `Failed` or `Blank` (passed in through `Files`).

## Keyboard

The select button is the only tab stop. From there...

| Key | Where | Action |
| --- | --- | --- |
| Enter or Space | Select button | Opens the file dialog |
| Down arrow | Select button | Moves into the file list |
| Up and down arrows, Home, End | File list | Move through the files (up from the first file goes back to the button) |
| Delete | File list | Removes the file (cancelling it if it's uploading) |
| Escape | Uploading file | Cancels the upload |
| Enter | Failed file | Retries the upload |
| Space | Uploading file | Pauses or resumes a chunked upload |

Screen readers hear how many files are in the list when they reach the button, and each file's name with its size or message (eg "File failed to upload.") as they move through the list. The progress bar is a `progressbar`, with its value.

## Styling

The components use these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-upload` | The outer element, plus `pixata-blazor-fileselect` for `PixataFileSelect`, and `pixata-blazor-upload-disabled` when disabled |
| `pixata-blazor-upload-dropzone` | The area around the select button. `pixata-blazor-upload-drag-active` is added to the outer element while files are dragged over the page, and `pixata-blazor-upload-drag-hover` while they are over the component |
| `pixata-blazor-upload-button` | The select button |
| `pixata-blazor-upload-dropzone-hint` | The "Drag and drop" hint |
| `pixata-blazor-upload-status` | "Uploading..." or "Done", which replaces the hint while there are uploads |
| `pixata-blazor-upload-files` | The file list |
| `pixata-blazor-upload-file` | A file, plus `-progress`, `-paused`, `-success`, `-error` or `-invalid` for its state (eg `pixata-blazor-upload-file-error`) |
| `pixata-blazor-upload-file-icon`, `-file-name`, `-file-size`, `-file-validation-message` | The parts of a file |
| `pixata-blazor-upload-actions` | A file's buttons (`pixata-blazor-upload-remove`, `-cancel`, `-retry`, `-pause` and `-resume`) |
| `pixata-blazor-upload-progressbar`, `-progressbar-value` | The progress bar |
| `pixata-blazor-upload-actions-bar` | The Clear and Upload buttons, when `AutoUpload` is false |
| `pixata-blazor-external-dropzone` | A `PixataDropZone`, with `pixata-blazor-dropzone-icon`, `-hint` and `-note` inside, `pixata-blazor-external-dropzone-active` while files are dragged over the page, and `-hover` while they are over it |

...and these variables, which you can set in your own CSS (on `:root`, or on a class you give the component)...

| Variable | Default | Description |
| --- | --- | --- |
| `--pixata-blazor-upload-bg` | the surface colour | The component's background |
| `--pixata-blazor-upload-border-color` | the border colour | The component's border |
| `--pixata-blazor-upload-dropzone-bg`, `--pixata-blazor-upload-dropzone-hover-bg` | the alternative surface colour | The area around the select button, normally and when files are dragged over it |
| `--pixata-blazor-upload-button-min-width` | `7em` | The select button's minimum width |
| `--pixata-blazor-upload-files-max-height` | `300px` | The height the file list grows to before it scrolls |
| `--pixata-blazor-upload-file-icon-size`, `--pixata-blazor-upload-file-icon-color` | `2rem`, the subtle colour | The file type icons |
| `--pixata-blazor-upload-progress-height`, `--pixata-blazor-upload-progress-bg`, `--pixata-blazor-upload-progress-color` | `2px`, grey, the info colour | The progress bar |
| `--pixata-blazor-upload-success-color`, `--pixata-blazor-upload-error-color`, `--pixata-blazor-upload-invalid-color` | darkened success and error colours, the subtle colour | The messages under uploaded, failed and invalid files |
| `--pixata-blazor-dropzone-height` | `220px` | A `PixataDropZone`'s height |
| `--pixata-blazor-dropzone-bg`, `--pixata-blazor-dropzone-border-color` | the alternative surface and border colours | A `PixataDropZone`'s background and border |
| `--pixata-blazor-dropzone-icon-size`, `--pixata-blazor-dropzone-icon-color` | `3rem`, the subtle colour | Its icon |
| `--pixata-blazor-dropzone-hover-color` | the primary colour | Its border and icon while files are dragged over it |

The defaults are taken from the theme variables at the top of `pixata.css`, so if you've changed those, the components follow.
