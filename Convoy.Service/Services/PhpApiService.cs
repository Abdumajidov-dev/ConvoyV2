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
    private readonly JsonSerializerOptions _jsonOptions;

    public PhpApiService(HttpClient httpClient, IConfiguration configuration, ILogger<PhpApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PhpApi:GlobalPathForSupport"] ?? "http://delivery.garant.uz/api/";
        _username = configuration["PhpApi:Username"] ?? "login";
        _password = configuration["PhpApi:Password"] ?? "password";

        // JSON serialization options (snake_case)
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Basic Authentication setup
        var authBytes = Encoding.UTF8.GetBytes($"{_username}:{_password}");
        var authHeader = Convert.ToBase64String(authBytes);
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
    }

    /// <summary>
    /// Telefon raqamni PHP API orqali verify qiladi
    /// Proxy: POST /auth/verify/phone
    /// </summary>
    public async Task<PhpApiResponse<PhpWorkerDto>> VerifyNumberAsync(string phoneNumber)
    {
        try
        {

            var endpoint = $"{_baseUrl.TrimEnd('/')}/auth/verify/phone";
            var payload = new { phone = phoneNumber };
            var serializedPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(
                serializedPayload,
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Calling PHP API verify phone: {Endpoint} with phone: {Phone}, payload: {Payload}",
                endpoint, phoneNumber, serializedPayload);

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("PHP API verify phone response: {Response}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PHP API returned status code {StatusCode} for phone {Phone}",
                    response.StatusCode, phoneNumber);

                return new PhpApiResponse<PhpWorkerDto>
                {
                    Status = false,
                    Message = $"PHP API xatolik: {response.StatusCode}",
                    Result = null
                };
            }

            var apiResponse = JsonSerializer.Deserialize<PhpApiResponse<PhpWorkerDto>>(responseBody, _jsonOptions);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize PHP API response");
                return new PhpApiResponse<PhpWorkerDto>
                {
                    Status = false,
                    Result = null,
                    Message = "PHP API javobni parse qilishda xatolik"
                };
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PHP API verify phone for {Phone}", phoneNumber);
            return new PhpApiResponse<PhpWorkerDto>
            {
                Status = false,
                Message = "PHP API bilan bog'lanishda xatolik",
                Result = null
            };
        }
    }

    /// <summary>
    /// OTP kod yuborishni PHP API orqali amalga oshiradi
    /// Proxy: POST /auth/send-otp
    /// </summary>
    public async Task<PhpApiResponse<object>> SendOtpAsync(string phoneNumber)
    {
        try
        {
            var endpoint = $"{_baseUrl.TrimEnd('/')}/auth/otp";
            var payload = new { phone = phoneNumber };
            var serializedPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(
                serializedPayload,
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Calling PHP API send OTP: {Endpoint} with phone: {Phone}, payload: {Payload}",
                endpoint, phoneNumber, serializedPayload);

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("PHP API send OTP response: {Response}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PHP API returned status code {StatusCode} for send OTP to {Phone}",
                    response.StatusCode, phoneNumber);

                return new PhpApiResponse<object>
                {
                    Status = false,
                    Message = $"PHP API xatolik: {response.StatusCode}",
                    Result = null
                };
            }

            var apiResponse = JsonSerializer.Deserialize<PhpApiResponse<object>>(responseBody, _jsonOptions);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize PHP API send OTP response");
                return new PhpApiResponse<object>
                {
                    Status = false,
                    Result = null,
                    Message = "PHP API javobni parse qilishda xatolik"
                };
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PHP API send OTP for {Phone}", phoneNumber);
            return new PhpApiResponse<object>
            {
                Status = false,
                Message = "PHP API bilan bog'lanishda xatolik",
                Result = null
            };
        }
    }

    /// <summary>
    /// OTP kodni PHP API orqali verify qiladi va JWT token oladi
    /// Proxy: POST /auth/verify-otp
    /// </summary>
    public async Task<PhpApiResponse<PhpAuthTokenDto>> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
        try
        {
            //var endpoint = "http://delivery.garant.uz/api/auth/otp/verify";
            var endpoint = $"{_baseUrl.TrimEnd('/')}/auth/otp/verify";
            var payload = new { phone = phoneNumber, otp_code = otpCode };
            var serializedPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(
                serializedPayload,
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Calling PHP API verify OTP: {Endpoint} with phone: {Phone}, payload: {Payload}",
                endpoint, phoneNumber, serializedPayload);

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("PHP API verify OTP response: {Response}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PHP API returned status code {StatusCode} for verify OTP for {Phone}",
                    response.StatusCode, phoneNumber);

                return new PhpApiResponse<PhpAuthTokenDto>
                {
                    Status = false,
                    Message = $"PHP API xatolik: {response.StatusCode}",
                    Result = null
                };
            }

            var apiResponse = JsonSerializer.Deserialize<PhpApiResponse<PhpAuthTokenDto>>(responseBody, _jsonOptions);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize PHP API verify OTP response");
                return new PhpApiResponse<PhpAuthTokenDto>
                {
                    Status = false,
                    Result = null,
                    Message = "PHP API javobni parse qilishda xatolik"
                };
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PHP API verify OTP for {Phone}", phoneNumber);
            return new PhpApiResponse<PhpAuthTokenDto>
            {
                Status = false,
                Message = "PHP API bilan bog'lanishda xatolik",
                Result = null
            };
        }
    }

    /// <summary>
    /// JWT token orqali user ma'lumotlarini PHP API'dan oladi
    /// Proxy: GET /auth/unduruv/me
    /// </summary>
    public async Task<PhpApiResponse<PhpUserDto>> GetMeAsync(string token)
    {
        try
        {
         
            //var endpoint = $"http://10.100.104.104:1004/api/auth/unduruv/me";
            var endpoint = $"{_baseUrl.TrimEnd('/')}/auth/unduruv/me";

            _logger.LogInformation("Calling PHP API get me: {Endpoint} with token: {Token}",
                endpoint, token.Substring(0, Math.Min(20, token.Length)) + "...");

            // JWT token'ni Authorization header'ga qo'shish
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("PHP API get me response status: {StatusCode}, body: {Response}",
                response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PHP API returned status code {StatusCode} for get me",
                    response.StatusCode);

                // PHP API xatolik qaytarsa ham JSON parse qilishga harakat qilamiz
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<PhpApiResponse<PhpUserDto>>(responseBody, _jsonOptions);
                    if (errorResponse != null)
                    {
                        return errorResponse;
                    }
                }
                catch
                {
                    // JSON emas bo'lsa, oddiy xatolik xabarini qaytaramiz
                }

                return new PhpApiResponse<PhpUserDto>
                {
                    Status = false,
                    Message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Token noto'g'ri yoki muddati tugagan"
                        : $"PHP API xatolik: {response.StatusCode}",
                    Result = null
                };
            }

            var apiResponse = JsonSerializer.Deserialize<PhpApiResponse<PhpUserDto>>(responseBody, _jsonOptions);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize PHP API get me response");
                return new PhpApiResponse<PhpUserDto>
                {
                    Status = false,
                    Result = null,
                    Message = "PHP API javobni parse qilishda xatolik"
                };
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PHP API get me");
            return new PhpApiResponse<PhpUserDto>
            {
                Status = false,
                Message = "PHP API bilan bog'lanishda xatolik",
                Result = null
            };
        }
    }

    /// <summary>
    /// PHP API dan filiallar ro'yxatini oladi
    /// </summary>
    public async Task<List<BranchDto>> GetBranchesAsync(string? searchTerm = null)
    {
        try
        {
            var _baseUrl = "https://garant-hr.uz/api/";
            //var _baseUrl = "http://10.100.104.120:8008/api/";
            var endpoint = $"{_baseUrl.TrimEnd('/')}/branch-list";

            _logger.LogInformation("Calling PHP API branch-list endpoint: {Endpoint} with search term: {SearchTerm}",
                endpoint, searchTerm ?? "null");

            // PHP API faqat POST method qabul qiladi
            var payload = new { search = searchTerm ?? "" };
            var content = new StringContent(
                JsonSerializer.Serialize(payload, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PHP API returned status code {StatusCode} for branch-list",
                    response.StatusCode);
                return new List<BranchDto>();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("PHP API branch-list response: {Response}", responseBody);

            var phpResponse = JsonSerializer.Deserialize<PhpBranchResponse>(responseBody, _jsonOptions);

            if (phpResponse != null && phpResponse.Status && phpResponse.Data != null && phpResponse.Data.Any())
            {
                _logger.LogInformation("Successfully retrieved {Count} branches from PHP API", phpResponse.Data.Count);
                return phpResponse.Data;
            }

            _logger.LogWarning("No branches found in PHP API response or status is false");
            return new List<BranchDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PHP API branch-list endpoint");
            return new List<BranchDto>();
        }
    }
}
