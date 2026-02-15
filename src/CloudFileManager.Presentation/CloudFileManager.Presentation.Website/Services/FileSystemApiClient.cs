using System.Net.Http.Json;
using System.Text.Json;
using CloudFileManager.Contracts;
using Microsoft.AspNetCore.Http;

namespace CloudFileManager.Presentation.Website.Services;

/// <summary>
/// FileSystemApiClient，封裝 Website 呼叫 Web API 的流程。
/// </summary>
public sealed class FileSystemApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 初始化 FileSystemApiClient。
    /// </summary>
    public FileSystemApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 以非同步方式取得樹狀結構。
    /// </summary>
    public async Task<DirectoryTreeResultDto> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        DirectoryTreeResultDto? result = await _httpClient.GetFromJsonAsync<DirectoryTreeResultDto>("api/filesystem/tree", cancellationToken);
        return result ?? new DirectoryTreeResultDto([]);
    }

    /// <summary>
    /// 以非同步方式建立目錄。
    /// </summary>
    public async Task<OperationResultDto> CreateDirectoryAsync(CreateDirectoryRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/filesystem/directories", request, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式上傳檔案。
    /// </summary>
    public async Task<OperationResultDto> UploadFileAsync(string directoryPath, IFormFile file, CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent(directoryPath), "DirectoryPath");

        using Stream stream = file.OpenReadStream();
        using StreamContent fileContent = new(stream);
        content.Add(fileContent, "File", file.FileName);

        HttpResponseMessage response = await _httpClient.PostAsync("api/filesystem/files/upload-form", content, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式搬移檔案。
    /// </summary>
    public async Task<OperationResultDto> MoveFileAsync(MoveFileRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/filesystem/files/move", request, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式重新命名檔案。
    /// </summary>
    public async Task<OperationResultDto> RenameFileAsync(RenameFileRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/filesystem/files/rename", request, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式刪除檔案。
    /// </summary>
    public async Task<OperationResultDto> DeleteFileAsync(DeleteFileRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = BuildDeleteJsonRequest("api/filesystem/files", request);
        HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式下載檔案內容。
    /// </summary>
    public async Task<FileDownloadResultDto> DownloadFileContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string url = $"api/filesystem/files/content?filePath={Uri.EscapeDataString(filePath)}";
        HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string message = await ParseErrorMessageAsync(response, cancellationToken);
            return new FileDownloadResultDto(false, message, string.Empty, null, "application/octet-stream");
        }

        byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? Path.GetFileName(filePath);
        fileName = fileName.Trim('"');

        return new FileDownloadResultDto(true, "File content loaded.", fileName, content, contentType);
    }

    /// <summary>
    /// 以非同步方式刪除目錄。
    /// </summary>
    public async Task<OperationResultDto> DeleteDirectoryAsync(DeleteDirectoryRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = BuildDeleteJsonRequest("api/filesystem/directories", request);
        HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式搬移目錄。
    /// </summary>
    public async Task<OperationResultDto> MoveDirectoryAsync(MoveDirectoryRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/filesystem/directories/move", request, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式重新命名目錄。
    /// </summary>
    public async Task<OperationResultDto> RenameDirectoryAsync(RenameDirectoryRequestDto request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/filesystem/directories/rename", request, cancellationToken);
        return await ParseOperationResultAsync(response, cancellationToken);
    }

    /// <summary>
    /// 以非同步方式計算容量。
    /// </summary>
    public async Task<SizeCalculationResultDto> CalculateSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        string url = $"api/filesystem/size?path={Uri.EscapeDataString(path)}";
        SizeCalculationResultDto? result = await _httpClient.GetFromJsonAsync<SizeCalculationResultDto>(url, cancellationToken);
        return result ?? new SizeCalculationResultDto(false, 0, "0 Bytes", []);
    }

    /// <summary>
    /// 以非同步方式搜尋資料。
    /// </summary>
    public async Task<SearchResultDto> SearchAsync(string extension, CancellationToken cancellationToken = default)
    {
        string url = $"api/filesystem/search?extension={Uri.EscapeDataString(extension)}";
        SearchResultDto? result = await _httpClient.GetFromJsonAsync<SearchResultDto>(url, cancellationToken);
        return result ?? new SearchResultDto([], []);
    }

    /// <summary>
    /// 以非同步方式匯出 XML。
    /// </summary>
    public async Task<XmlExportResultDto> ExportXmlAsync(CancellationToken cancellationToken = default)
    {
        XmlExportResultDto? result = await _httpClient.GetFromJsonAsync<XmlExportResultDto>("api/filesystem/xml", cancellationToken);
        return result ?? new XmlExportResultDto(string.Empty, null);
    }

    /// <summary>
    /// 以非同步方式取得功能旗標。
    /// </summary>
    public async Task<FeatureFlagsResultDto> GetFeatureFlagsAsync(CancellationToken cancellationToken = default)
    {
        FeatureFlagsResultDto? result = await _httpClient.GetFromJsonAsync<FeatureFlagsResultDto>("api/filesystem/feature-flags", cancellationToken);
        return result ?? new FeatureFlagsResultDto(new Dictionary<string, bool>());
    }

    private static HttpRequestMessage BuildDeleteJsonRequest<TRequest>(string url, TRequest request)
    {
        return new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = JsonContent.Create(request)
        };
    }

    /// <summary>
    /// 以非同步方式解析操作結果。
    /// </summary>
    private static async Task<OperationResultDto> ParseOperationResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        OperationResultDto? result = DeserializeOperationResult(payload);
        if (result is not null)
        {
            return result;
        }

        if (response.IsSuccessStatusCode)
        {
            return new OperationResultDto(true, "Operation completed.");
        }

        string message = ParseErrorMessage(payload, response);
        return new OperationResultDto(false, message);
    }

    /// <summary>
    /// 以非同步方式解析錯誤訊息。
    /// </summary>
    private static async Task<string> ParseErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseErrorMessage(payload, response);
    }

    /// <summary>
    /// 反序列化操作結果內容。
    /// </summary>
    private static OperationResultDto? DeserializeOperationResult(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OperationResultDto>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// 解析錯誤訊息。
    /// </summary>
    private static string ParseErrorMessage(string payload, HttpResponseMessage response)
    {
        OperationResultDto? error = DeserializeOperationResult(payload);
        if (error is not null && !string.IsNullOrWhiteSpace(error.Message))
        {
            return error.Message;
        }

        if (!string.IsNullOrWhiteSpace(payload))
        {
            return payload.Trim();
        }

        return $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
    }
}
