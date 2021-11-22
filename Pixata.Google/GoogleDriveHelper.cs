using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using LanguageExt;
using Microsoft.AspNetCore.Hosting;
using static LanguageExt.Prelude;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Pixata.Google {
  public class GoogleDriveHelper {
    #region Private variables and constructor

    public const string RootFolderName = "My Drive";

    private readonly DriveService _service;

    public GoogleDriveHelper(IWebHostEnvironment environment) =>
      _service = GetDriveService(environment);

    #endregion

    #region Folders

    /// <summary>
    /// Gets the folder with the specified Id
    /// </summary>
    /// <param name="folderId"></param>
    /// <returns>The specified folder</returns>
    public TryAsync<DriveFile> GetFolder(string folderId = "root") =>
      TryAsync(() => _service.Files.Get(folderId).ExecuteAsync());

    /// <summary>
    /// Gets the parent folder of the one whose Id is passed in
    /// </summary>
    /// <param name="folderId">The Id of the folder whose parent is required</param>
    /// <returns>The DriveFile object representing the parent folder</returns>
    public TryAsync<DriveFile> GetParentFolder(string folderId) =>
      TryAsync(() => GetParentFolderDo(folderId));

    private async Task<DriveFile> GetParentFolderDo(string folderId) {
      FilesResource.GetRequest request = _service.Files.Get(folderId);
      request.Fields = "id, name, parents";
      DriveFile folder = await request.ExecuteAsync();
      request = _service.Files.Get(folder.Parents[0]);
      return await request.ExecuteAsync();
    }

    /// <summary>
    /// Get a collection of folder objects that represent the path from the Google Drive root down to the specified folder
    /// </summary>
    /// <param name="folderId">The Id of the folder at the end of the breadcrumb</param>
    /// <returns>A collection of folder objects representing the breadcrumb</returns>
    public TryAsync<List<DriveFile>> GetBreadcrumb(string folderId) =>
      TryAsync(() => GetBreadcrumbDo(folderId));

    private async Task<List<DriveFile>> GetBreadcrumbDo(string folderId) {
      List<DriveFile> breadcrumb = new();
      DriveFile folder = (await GetFolder(folderId)).Match(f => f, _ => null);
      while (folder != null) {
        breadcrumb.Add(folder);
        folder = (await GetParentFolder(folder.Id)).Match(f => f, _ => null);
      }
      breadcrumb.Reverse();
      return breadcrumb;
    }

    /// <summary>
    /// Returns a list of the subfolders of the folder who Id is passed in
    /// </summary>
    /// <param name="folderId">The Id of the folder in Google Drive</param>
    /// <returns>A list of the subfolders of the folder who Id is passed in</returns>
    public TryAsync<List<DriveFile>> GetSubfolders(string folderId = "root") {
      FilesResource.ListRequest request = _service.Files.List();
      request.Q = $"mimeType = 'application/vnd.google-apps.folder' and '{folderId}' in parents and trashed = false";
      return TryAsync(async () => (await request.ExecuteAsync()).Files.OrderBy(f => f.Name).ToList());
    }

    /// <summary>
    /// Creates a new folder as a subfolder of the specified one
    /// </summary>
    /// <param name="folderName">The name for the new folder</param>
    /// <param name="parentFolderId">The Id of the parent folder. If omitted, the new folder will be created in the root</param>
    /// <returns>The Id of the new folder</returns>
    public TryAsync<string> CreateFolder(string folderName, string parentFolderId = "root") =>
      GetSubfolders(parentFolderId)
        .Bind(folders => CreateNewFolder(folderName, parentFolderId, folders));

    private TryAsync<string> CreateNewFolder(string folderName, string parentFolderId, List<DriveFile> folders) {
      if (folders.Any()) {
        return TryAsync(() => Task.Run(() => folders.First().Id));
      }
      DriveFile newFolder = new() {
        Name = folderName,
        MimeType = "application/vnd.google-apps.folder",
        Parents = new[] { parentFolderId }
      };
      FilesResource.CreateRequest command = _service.Files.Create(newFolder);
      return TryAsync(async () => {
        DriveFile folder = await command.ExecuteAsync();
        return folder.Id;
      });
    }

    #endregion

    #region Files

    /// <summary>
    /// Returns the files in the specified folder
    /// </summary>
    /// <param name="folderId">The Id of the specific folder</param>
    /// <returns>The files in the specified folder</returns>
    public TryAsync<List<DriveFile>> GetFilesInFolder(string folderId = "root") =>
      TryAsync(async () => {
        FilesResource.ListRequest fileList = _service.Files.List();
        fileList.Q = $"mimeType != 'application/vnd.google-apps.folder' and '{folderId}' in parents and trashed = false";
        fileList.Fields = "nextPageToken, files(id, name, size, mimeType)";
        List<DriveFile> files = new();
        string pageToken = null;
        do {
          fileList.PageToken = pageToken;
          FileList filesResult = await fileList.ExecuteAsync();
          IList<DriveFile> pageFiles = filesResult.Files;
          pageToken = filesResult.NextPageToken;
          files.AddRange(pageFiles);
        } while (pageToken != null);
        return files.OrderBy(f => f.Name).ToList();
      });

    /// <summary>
    /// Uploads a file to the specified folder
    /// </summary>
    /// <param name="file">A Stream containing the file data</param>
    /// <param name="fileName">The file name</param>
    /// <param name="mimeType">The MIME type of the file</param>
    /// <param name="folderId">The parent folder Id</param>
    /// <returns>The Id of the uploaded file</returns>
    public TryAsync<string> UploadFile(Stream file, string fileName, string mimeType, string folderId) =>
      TryAsync(async () => {
        DriveFile driveFile = new() {
          Name = fileName,
          MimeType = mimeType,
          Parents = new[] { folderId }
        };
        FilesResource.CreateMediaUpload request = _service.Files.Create(driveFile, file, mimeType);
        request.Fields = "id";
        IUploadProgress response = await request.UploadAsync();
        if (response.Status != UploadStatus.Completed) {
          throw response.Exception;
        }
        return request.ResponseBody.Id;
      });

    /// <summary>
    /// Move a file from one folder to another. Note that this will NOT work for folders
    /// </summary>
    /// <param name="fileId">The Id of the file to be moved</param>
    /// <param name="newFolderId">The Id of the target folder</param>
    /// <returns>Unit</returns>
    public TryAsync<Unit> MoveFile(string fileId, string newFolderId) =>
      TryAsync(() => {
        DriveFile file = _service.Files.Get(fileId).Execute();
        FilesResource.UpdateRequest updateRequest = _service.Files.Update(new DriveFile(), file.Id);
        updateRequest.AddParents = newFolderId;
        if (file.Parents != null) {
          updateRequest.RemoveParents = file.Parents[0];
        }
        DriveFile movedFile = updateRequest.Execute();
        return Task.Run(() => unit);
      });

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="fileId">The Id of the file to be deleted</param>
    /// <returns>An empty string if the deletion succeeded. Not sure if this can ever return a non-empty string, as if the deletion was not successful, it's most likely an exception occurred</returns>
    public TryAsync<string> DeleteFile(string fileId) =>
      TryAsync(() => _service.Files.Delete(fileId).ExecuteAsync());

    #endregion

    #region Utility

    private DriveService GetDriveService(IWebHostEnvironment environment) {
      UserCredential credential;
      using (FileStream stream = new(@$"{environment.ContentRootPath}\Google\credentials.json", FileMode.Open, FileAccess.Read)) {
        // If you modify the scopes, make sure to delete your previously saved credentials so new ones can be created
        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, new[] { DriveService.Scope.Drive }, "user", CancellationToken.None, new FileDataStore(@$"{environment.ContentRootPath}\Google\Token", true)).Result;
      }
      DriveService service = new(new BaseClientService.Initializer {
        HttpClientInitializer = credential,
        ApplicationName = ""
      });
      return service;
    }

    #endregion
  }
}
