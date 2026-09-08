using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class AttachmentService
{
    private readonly HttpClient _client;

    public AttachmentService(ApiService apiService)
    {
        _client = apiService.Client;
    }

    public async Task<HttpResponseMessage> UploadAsync(
        string macAddress,
        string filePath)
    {
        using MultipartFormDataContent form =
            new MultipartFormDataContent();

        using FileStream fileStream =
            File.OpenRead(filePath);

        using StreamContent fileContent =
            new StreamContent(fileStream);

        string fileName =
            Path.GetFileName(filePath);

        form.Add(
            fileContent,
            "file",
            fileName);

        return await _client.PostAsync(
            $"api/sensors/{System.Uri.EscapeDataString(macAddress)}/attachment",
            form);
    }
}