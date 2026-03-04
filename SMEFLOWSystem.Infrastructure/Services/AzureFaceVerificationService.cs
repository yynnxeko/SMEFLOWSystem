using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Config;

namespace SMEFLOWSystem.Infrastructure.Services;

public class AzureFaceVerificationService : IFaceVerificationService
{
    private readonly AzureFaceSettings _settings;
    private readonly HttpClient _httpClient;

    public AzureFaceVerificationService(IOptions<AzureFaceSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;

        if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            throw new InvalidOperationException("Missing config: AzureFace:Endpoint");
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Missing config: AzureFace:ApiKey");

        _httpClient = httpClientFactory.CreateClient("AzureFace");
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
    }

    public async Task<FaceVerificationResult> VerifyAsync(string selfieUrl, string avatarUrl)
    {
        // Step 1: Detect face in selfie
        var selfieFaceId = await DetectFaceAsync(selfieUrl);
        if (selfieFaceId == null)
            return new FaceVerificationResult(false, 0, "Không phát hiện khuôn mặt trong ảnh selfie.");

        // Step 2: Detect face in avatar
        var avatarFaceId = await DetectFaceAsync(avatarUrl);
        if (avatarFaceId == null)
            return new FaceVerificationResult(false, 0, "Không phát hiện khuôn mặt trong ảnh avatar.");

        // Step 3: Verify 2 faces
        return await VerifyFacesAsync(selfieFaceId, avatarFaceId);
    }

    private async Task<string?> DetectFaceAsync(string imageUrl)
    {
        var endpoint = _settings.Endpoint.TrimEnd('/');
        var detectUrl = $"{endpoint}/face/v1.0/detect?returnFaceId=true&recognitionModel=recognition_04&detectionModel=detection_03";

        var body = JsonSerializer.Serialize(new { url = imageUrl });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(detectUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Azure Face Detect failed ({response.StatusCode}): {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var faces = doc.RootElement;

        if (faces.GetArrayLength() == 0)
            return null;

        return faces[0].GetProperty("faceId").GetString();
    }

    private async Task<FaceVerificationResult> VerifyFacesAsync(string faceId1, string faceId2)
    {
        var endpoint = _settings.Endpoint.TrimEnd('/');
        var verifyUrl = $"{endpoint}/face/v1.0/verify";

        var body = JsonSerializer.Serialize(new { faceId1, faceId2 });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(verifyUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Azure Face Verify failed ({response.StatusCode}): {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var isIdentical = doc.RootElement.GetProperty("isIdentical").GetBoolean();
        var confidence = doc.RootElement.GetProperty("confidence").GetDouble();

        return new FaceVerificationResult(
            IsMatch: confidence >= _settings.ConfidenceThreshold,
            Confidence: confidence,
            ErrorMessage: null
        );
    }
}
