using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json.Bson;
using Pixata.Extensions;

namespace Pixata.Blazor.Extensions;

public class PixataBaseClientService {
  private readonly HttpClient _httpClient;
  private readonly string _baseUri;

  private readonly JsonSerializerOptions _jsonSerializerOptions = new() {
    PropertyNameCaseInsensitive = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
  };

  // Note that we assume that all GET calls only require the URI, as that includes any parameters (eg /api-company/613). Any POST calls require the URI and the data.
  public PixataBaseClientService(IHttpClientFactory httpClientFactory, NavigationManager navManager) {
    _httpClient = httpClientFactory.CreateClient("Default");
    _baseUri = navManager.BaseUri;
    if (_baseUri.EndsWith("/")) {
      _baseUri = _baseUri[..^1];
    }
  }

  public async Task<ApiResponse<T>> Get<T>(string uri) =>
    await SendRequest<T>(uri, HttpMethod.Get);

  public async Task<ApiResponse<T>> Post<T>(string uri, object data) =>
    await SendRequest<T>(uri, HttpMethod.Post, data);

  public async Task<ApiResponse<T>> PostBson<T>(string uri, UploadFileDto dto) =>
    await SendRequest<T>(uri, HttpMethod.Post, dto, (data) => CreateBsonContent((UploadFileDto)data!));

  public async Task<ApiResponse<T>> SendRequest<T>(string uri, HttpMethod method, object? data = null, Func<object?, HttpContent?>? contentFactory = null) {
    try {
      HttpRequestMessage request = new(method, Uri(uri));
      if (data is not null) {
        request.Content = new StringContent(JsonSerializer.Serialize(data, _jsonSerializerOptions), Encoding.UTF8, "application/json");
      }
      HttpResponseMessage response = await _httpClient.SendAsync(request);
      if (response.StatusCode == HttpStatusCode.ServiceUnavailable) {
        return new ApiResponse<T>(ApiResponseStates.ServiceUnavailable);
      }
      if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) {
        return new ApiResponse<T>(ApiResponseStates.Unauthorised);
      }
      if (response.StatusCode == HttpStatusCode.NotFound) {
        return new ApiResponse<T>(ApiResponseStates.NotFound);
      }
      if (response.StatusCode == HttpStatusCode.Conflict) {
        return new ApiResponse<T>(ApiResponseStates.Conflict, Message: await ReadMessage<T>(response));
      }
      if (!response.IsSuccessStatusCode) {
        return new ApiResponse<T>(ApiResponseStates.Failure, Message: $"HTTP error ({response.StatusCode}) {await response.Content.ReadAsStringAsync()}");
      }
      ApiResponse<T> apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(await response.Content.ReadAsStringAsync(), _jsonSerializerOptions)!;
      // Preserve whatever state the server sent. Previously anything that wasn't Success was flattened to Failure,
      // which meant a server returning NotFound or Unauthorised in the body lost that distinction
      return apiResponse.State == ApiResponseStates.Success
        ? new ApiResponse<T>(ApiResponseStates.Success, apiResponse.Data)
        : new ApiResponse<T>(apiResponse.State, Message: apiResponse.Message);
    } catch (HttpRequestException ex) {
      return new ApiResponse<T>(ApiResponseStates.HttpFailure, Message: ex.Message);
    } catch (Exception ex) {
      return new ApiResponse<T>(ApiResponseStates.Failure, Message: ex.Message);
    }
  }

  /// <summary>
  /// Reads the message from an unsuccessful response. The body may be a serialised ApiResponse, in which case we want its
  /// Message, or plain text, in which case we use the body as it stands
  /// </summary>
  private async Task<string> ReadMessage<T>(HttpResponseMessage response) {
    string body = await response.Content.ReadAsStringAsync();
    try {
      string? message = JsonSerializer.Deserialize<ApiResponse<T>>(body, _jsonSerializerOptions)?.Message;
      return string.IsNullOrWhiteSpace(message) ? body : message!;
    } catch (JsonException) {
      return body;
    }
  }

  private static ByteArrayContent CreateBsonContent(UploadFileDto fileDto) {
    using MemoryStream ms = new();
    new Newtonsoft.Json.JsonSerializer().Serialize(new BsonDataWriter(ms), new {
      fileDto.JoinGuid,
      fileDto.FileName,
      fileDto.Buffer
    });
    ByteArrayContent content = new(ms.ToArray());
    content.Headers.ContentType = new("application/bson");
    return content;
  }

  private string Uri(string uri) =>
    uri.StartsWith("http")
      ? uri
      : $"{_baseUri}{uri}";

}