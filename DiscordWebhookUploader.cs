using System.Net.Http.Headers;

public class DiscordWebhookUploader
{
    private readonly string _webhookUrl;
    private readonly HttpClient _httpClient;

    public DiscordWebhookUploader(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
        _httpClient = new HttpClient();
    }

    public async Task<bool> SendFileAsync(string filePath)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            content.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync(_webhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}