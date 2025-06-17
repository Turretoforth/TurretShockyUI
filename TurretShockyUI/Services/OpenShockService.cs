using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TurretShocky.Services
{
    public class OpenShockService
    {
        private static OpenShockService? _currentInstance = null;
        private static HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(6) };
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private OpenShockService(string baseApi, string apiToken)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TurretShocky/" + Utils.GetCurrentVersion());
            _httpClient.DefaultRequestHeaders.Add("Open-Shock-Token", apiToken);
            _httpClient.BaseAddress = new Uri(baseApi.TrimEnd('/'));
        }

        public static void Initialize(string baseApi, string apiKey)
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose(); // Dispose the old instance if it exists
                _httpClient = new() { Timeout = TimeSpan.FromSeconds(6) };
            }
            _currentInstance = new OpenShockService(baseApi, apiKey);
        }

        public static async Task<List<OpenShocker>> GetSharedShockers()
        {
            if (_currentInstance == null)
            {
                throw new InvalidOperationException("OpenShockService is not initialized. Call Initialize() first.");
            }
            HttpResponseMessage response = await _httpClient.GetAsync("/1/shockers/shared");
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to fetch shockers: {response.ReasonPhrase}");
            }
            string content = await response.Content.ReadAsStringAsync();
            OpenShockerSharedMessage? sharedShockerResponse = JsonSerializer.Deserialize<OpenShockerSharedMessage>(content, _jsonSerializerOptions)
                ?? throw new InvalidOperationException("Failed to deserialize shared shockers response.");
            return [.. sharedShockerResponse.Data.SelectMany(data => data.Devices).SelectMany(s => s.Shockers)
                .Select(s => new OpenShocker() { Id = s.Id, IsPaused = s.IsPaused, Name = s.Name })];
        }

        public static async Task<List<OpenShocker>> GetOwnShockers()
        {
            if (_currentInstance == null)
            {
                throw new InvalidOperationException("OpenShockService is not initialized. Call Initialize() first.");
            }
            HttpResponseMessage response = await _httpClient.GetAsync("/1/shockers/own");
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to fetch own shockers: {response.ReasonPhrase}");
            }
            string content = await response.Content.ReadAsStringAsync();
            OpenShockerOwnMessage? ownerShockerResponse = JsonSerializer.Deserialize<OpenShockerOwnMessage>(content, _jsonSerializerOptions);
            return ownerShockerResponse == null
                ? throw new InvalidOperationException("Failed to deserialize own shockers response.")
                : [.. ownerShockerResponse.Data.SelectMany(device => device.Shockers)
                    .Select(s => new OpenShocker() { Id = s.Id, IsPaused = s.IsPaused, Name = s.Name })];
        }

        public static async Task SendShockerCommand(string[] shockers, Models.FunType funType, int intensity, int duration)
        {
            if (_currentInstance == null)
            {
                throw new InvalidOperationException("OpenShockService is not initialized. Call Initialize() first.");
            }
            if (shockers == null || shockers.Length == 0)
            {
                throw new ArgumentException("At least one shocker must be specified.", nameof(shockers));
            }

            string convertedType = funType switch
            {
                Models.FunType.Shock => "Shock",
                Models.FunType.Vibration => "Vibrate",
                _ => throw new ArgumentOutOfRangeException(nameof(funType), "Invalid fun type specified for OpenShock"),
            };

            OpenShockControlMessage controlMessage = new()
            {
                Shocks = [.. shockers.Select(shocker => new OpenShockerControl
                {
                    Id = shocker,
                    Type = convertedType,
                    Intensity = intensity,
                    Duration = duration,
                    Exclusive = false
                })],
                CustomName = "TurretShocky"
            };

            StringContent content = new(JsonSerializer.Serialize(controlMessage, _jsonSerializerOptions), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync("/2/shockers/control", content);
            if (!response.IsSuccessStatusCode)
            {
                OpenShockProblem? problem = null;
                string responseContent = await response.Content.ReadAsStringAsync();
                try
                {
                    problem = JsonSerializer.Deserialize<OpenShockProblem>(responseContent, _jsonSerializerOptions);
                }
                catch
                {
                    // Ignore JSON parsing errors, we'll just throw a generic exception
                }
                throw new HttpRequestException($"Failed to send shocker command(s): {problem?.Title ?? response.ReasonPhrase} (Status: {problem?.Status ?? ((int)response.StatusCode)})");
            }
        }
    }

    public class OpenShocker
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required bool IsPaused { get; set; }
    }

    sealed class OpenShockerOwnMessage
    {
        public string Message { get; set; }
        public List<OpenShockerOwnData> Data { get; set; }
    }

    sealed class OpenShockerOwnData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<OwnShocker> Shockers { get; set; }
    }

    sealed class OwnShocker
    {
        public Guid Id { get; set; }
        public long RfId { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public bool IsPaused { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    sealed class OpenShockerSharedMessage
    {
        public string Message { get; set; }
        public List<OpenShockerSharedData> Data { get; set; }
    }

    sealed class OpenShockerSharedData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Uri Image { get; set; }
        public List<Device> Devices { get; set; }
    }

    sealed class Device
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<SharedShocker> Shockers { get; set; }
    }

    sealed class SharedShocker
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsPaused { get; set; }
        public Permissions Permissions { get; set; }
        public Limits Limits { get; set; }
    }

    sealed class Limits
    {
        public int Intensity { get; set; }
        public int Duration { get; set; }
    }

    sealed class Permissions
    {
        public bool Vibrate { get; set; }
        public bool Sound { get; set; }
        public bool Shock { get; set; }
        public bool Live { get; set; }
    }

    sealed class OpenShockControlMessage
    {
        public required OpenShockerControl[] Shocks { get; set; }
        public string? CustomName { get; set; } // Not sure what this is for, but it's in the API docs
    }

    sealed class OpenShockerControl
    {
        public required string Id { get; set; } = string.Empty;
        [AllowedValues("Stop", "Shock", "Vibrate", "Sound")]
        public required string Type { get; set; }
        [Range(0, 100)]
        public required int Intensity { get; set; } = 0;
        [Range(300, 65535)]
        public required int Duration { get; set; } = 300;
        public bool Exclusive { get; set; } = false;
    }

    sealed class OpenShockProblem
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public string? RequestId { get; set; }
    }
}
