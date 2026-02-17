using CloudFileManager.Application.Models;

namespace CloudFileManager.Application.Interfaces;

public interface ICloudFileDirectoryCommandService
{
    OperationResult CreateDirectory(CreateDirectoryRequest request);

    Task<OperationResult> CreateDirectoryAsync(CreateDirectoryRequest request, CancellationToken cancellationToken = default);

    OperationResult DeleteDirectory(DeleteDirectoryRequest request);

    Task<OperationResult> DeleteDirectoryAsync(DeleteDirectoryRequest request, CancellationToken cancellationToken = default);

    OperationResult MoveDirectory(MoveDirectoryRequest request);

    Task<OperationResult> MoveDirectoryAsync(MoveDirectoryRequest request, CancellationToken cancellationToken = default);

    OperationResult RenameDirectory(RenameDirectoryRequest request);

    OperationResult CopyDirectory(CopyDirectoryRequest request);

    Task<OperationResult> RenameDirectoryAsync(RenameDirectoryRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> CopyDirectoryAsync(CopyDirectoryRequest request, CancellationToken cancellationToken = default);
}
