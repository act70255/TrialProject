using System.Net.Http.Json;
using CloudFileManager.Presentation.WebApi;
using CloudFileManager.Presentation.WebApi.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CloudFileManager.IntegrationTests;

public sealed class WebApiTagStatePersistenceIntegrationTests
{
    [Fact]
    public async Task Should_PersistTagAssignments_AcrossApiSessionStateRequests()
    {
        string scenarioKey = $"WebApiTag_{Guid.NewGuid():N}";
        string docsPath = $"Root/{scenarioKey}_Docs";
        string filePath = $"{docsPath}/note.txt";

        await using (WebApplicationFactory<Program> writerFactory = new())
        {
            using HttpClient writerClient = CreateAuthenticatedClient(writerFactory);

            await CreateDirectoryAsync(writerClient, new CreateDirectoryApiRequest("Root", $"{scenarioKey}_Docs"));
            await UploadTextFileAsync(writerClient, docsPath, "note.txt", "webapi-tag-persist");

            TagAssignApiRequest assignRequest = new()
            {
                Path = filePath,
                Tag = "Urgent",
                State = null
            };
            StatefulApiResponse<StateOperationInfoApiResponse> assignResponse = await PostStatefulAsync<StateOperationInfoApiResponse>(writerClient, "api/filesystem/tags/assign", assignRequest);

            Assert.True(assignResponse.Success);
        }

        await using (WebApplicationFactory<Program> readerFactory = new())
        {
            using HttpClient readerClient = CreateAuthenticatedClient(readerFactory);

            TagListApiRequest listRequest = new()
            {
                Path = filePath,
                State = null
            };
            StatefulApiResponse<TagListApiResponse> listResponse = await PostStatefulAsync<TagListApiResponse>(readerClient, "api/filesystem/tags/list", listRequest);

            Assert.True(listResponse.Success);
            TaggedNodeApiResponse taggedNode = Assert.Single(listResponse.Data!.Items, item => item.Path == filePath);
            Assert.Contains("Urgent", taggedNode.Tags);

            TagFindApiRequest findRequest = new()
            {
                Tag = "urgent",
                DirectoryPath = "Root",
                State = null
            };
            StatefulApiResponse<TagFindResultApiResponse> findResponse = await PostStatefulAsync<TagFindResultApiResponse>(readerClient, "api/filesystem/tags/find", findRequest);

            Assert.True(findResponse.Success);
            Assert.Equal("Urgent", findResponse.Data!.Tag);
            Assert.Contains(filePath, findResponse.Data.Paths);
        }
    }

    [Fact]
    public async Task Should_ApplyUndoRedoTagMutations_ToPersistedStorage()
    {
        await using WebApplicationFactory<Program> factory = new();
        using HttpClient client = CreateAuthenticatedClient(factory);

        string scenarioKey = $"WebApiUndo_{Guid.NewGuid():N}";
        string docsPath = $"Root/{scenarioKey}_Docs";
        string filePath = $"{docsPath}/plan.txt";

        await CreateDirectoryAsync(client, new CreateDirectoryApiRequest("Root", $"{scenarioKey}_Docs"));
        await UploadTextFileAsync(client, docsPath, "plan.txt", "undo-redo-tag");

        StatefulApiResponse<StateOperationInfoApiResponse> assignResponse = await PostStatefulAsync<StateOperationInfoApiResponse>(
            client,
            "api/filesystem/tags/assign",
            new TagAssignApiRequest
            {
                Path = filePath,
                Tag = "Work",
                State = null
            });

        Assert.True(assignResponse.Success);

        StatefulApiResponse<StateOperationInfoApiResponse> undoResponse = await PostStatefulAsync<StateOperationInfoApiResponse>(
            client,
            "api/filesystem/history/undo",
            new HistoryActionApiRequest
            {
                State = assignResponse.State
            });

        Assert.True(undoResponse.Success);

        StatefulApiResponse<TagListApiResponse> listAfterUndo = await PostStatefulAsync<TagListApiResponse>(
            client,
            "api/filesystem/tags/list",
            new TagListApiRequest
            {
                Path = filePath,
                State = null
            });

        Assert.True(listAfterUndo.Success);
        Assert.Empty(listAfterUndo.Data!.Items);

        StatefulApiResponse<StateOperationInfoApiResponse> redoResponse = await PostStatefulAsync<StateOperationInfoApiResponse>(
            client,
            "api/filesystem/history/redo",
            new HistoryActionApiRequest
            {
                State = undoResponse.State
            });

        Assert.True(redoResponse.Success);

        StatefulApiResponse<TagListApiResponse> listAfterRedo = await PostStatefulAsync<TagListApiResponse>(
            client,
            "api/filesystem/tags/list",
            new TagListApiRequest
            {
                Path = filePath,
                State = null
            });

        Assert.True(listAfterRedo.Success);
        TaggedNodeApiResponse taggedNode = Assert.Single(listAfterRedo.Data!.Items, item => item.Path == filePath);
        Assert.Contains("Work", taggedNode.Tags);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "dev-local-api-key");
        return client;
    }

    private static async Task CreateDirectoryAsync(HttpClient client, CreateDirectoryApiRequest request)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("api/filesystem/directories", request);
        response.EnsureSuccessStatusCode();
        OperationApiResponse result = (await response.Content.ReadFromJsonAsync<OperationApiResponse>())
            ?? new OperationApiResponse(false, "Create directory response is empty.");
        Assert.True(result.Success, result.Message);
    }

    private static async Task UploadTextFileAsync(HttpClient client, string directoryPath, string fileName, string content)
    {
        using MultipartFormDataContent form = new();
        form.Add(new StringContent(directoryPath), "DirectoryPath");

        using ByteArrayContent fileContent = new(System.Text.Encoding.UTF8.GetBytes(content));
        form.Add(fileContent, "File", fileName);

        using HttpResponseMessage response = await client.PostAsync("api/filesystem/files/upload-form", form);
        response.EnsureSuccessStatusCode();
        OperationApiResponse result = (await response.Content.ReadFromJsonAsync<OperationApiResponse>())
            ?? new OperationApiResponse(false, "Upload response is empty.");
        Assert.True(result.Success, result.Message);
    }

    private static async Task<StatefulApiResponse<TData>> PostStatefulAsync<TData>(HttpClient client, string endpoint, object payload)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(endpoint, payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StatefulApiResponse<TData>>())
            ?? new StatefulApiResponse<TData>
            {
                Success = false,
                Message = "Stateful response is empty.",
                State = new ClientSessionStateApiModel()
            };
    }
}
