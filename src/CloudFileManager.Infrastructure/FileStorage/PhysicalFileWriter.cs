using CloudFileManager.Application.Models;

namespace CloudFileManager.Infrastructure.FileStorage;

/// <summary>
/// PhysicalFileWriter 類別，負責寫入實體檔案。
/// </summary>
public static class PhysicalFileWriter
{
    /// <summary>
    /// 寫入實體檔案。
    /// </summary>
    public static long Write(string targetPath, UploadFileRequest request)
    {
        string? sourceLocalPath = request.SourceLocalPath;
        if (!string.IsNullOrWhiteSpace(sourceLocalPath))
        {
            string fullSourcePath = Path.GetFullPath(sourceLocalPath);
            if (!File.Exists(fullSourcePath))
            {
                throw new InvalidOperationException($"Source file not found: {fullSourcePath}");
            }

            File.Copy(fullSourcePath, targetPath, overwrite: true);
            return new FileInfo(targetPath).Length;
        }

        using FileStream stream = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(request.Size);
        return request.Size;
    }

    /// <summary>
    /// 以非同步方式寫入實體檔案。
    /// </summary>
    public static async Task<long> WriteAsync(string targetPath, UploadFileRequest request, CancellationToken cancellationToken = default)
    {
        string? sourceLocalPath = request.SourceLocalPath;
        if (!string.IsNullOrWhiteSpace(sourceLocalPath))
        {
            string fullSourcePath = Path.GetFullPath(sourceLocalPath);
            if (!File.Exists(fullSourcePath))
            {
                throw new InvalidOperationException($"Source file not found: {fullSourcePath}");
            }

            await using FileStream source = new(fullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await using FileStream target = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await source.CopyToAsync(target, cancellationToken);
            return target.Length;
        }

        await using FileStream stream = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        stream.SetLength(request.Size);
        return request.Size;
    }
}
