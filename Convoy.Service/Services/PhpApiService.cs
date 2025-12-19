using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Convoy.Service.Services;
public class PhpApiService : IPhpApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PhpApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;

    public PhpApiService(HttpClient httpClient, IConfiguration configuration, ILogger<PhpApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _baseUrl = configuration["PhpApi:GlobalPathForSupport"] ?? "https://garant-hr.uz/api/";
        _username = configuration["PhpApi:Username"] ?? "login";
        _password = configuration["PhpApi:Password"] ?? "password";
    }

    public async Task<PhpWorkerDto?> VerifyUserAsync(string phoneNumber)
    {
        try
        {
            var endpoint = $"{_baseUrl.TrimEnd('/')}/auth-service/verification-user";

            var payload = new { phone_number = phoneNumber };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            // Basic Authentication
            var authBytes = Encoding.UTF8.GetBytes($"{_username}:{_password}");
            var authHeader = Convert.ToBase64String(authBytes);
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            var response = await _httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PHP API returned status code {StatusCode} for phone {Phone}",
                    response.StatusCode, phoneNumber);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            // PHP API dan kelgan JSON snake_case formatda
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var worker = JsonSerializer.Deserialize<PhpWorkerDto>(responseBody, options);

            if (worker != null)
            {
                _logger.LogInformation("Successfully verified user {WorkerName} (ID: {WorkerId}) from PHP API",
                    worker.WorkerName, worker.WorkerId);
            }

            return worker;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PHP API for phone {Phone}", phoneNumber);
            return null;
        }
    }
}