using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TurretShocky.Models;

namespace TurretShocky
{
    public class PiShockService(string apiKey, string username)
    {
        private readonly string _apiKey = apiKey;
        private readonly string _username = username;
        private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(6) };

        public async Task<Dictionary<string, OperationResult>> DoPiShockOperations(FunType type, int nbSeconds, int intensity, List<string> shockerCodes)
        {
            var result = new Dictionary<string, OperationResult>();
            ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 5 };
            await Parallel.ForEachAsync(shockerCodes,parallelOptions, async (shockerCode, cancellationToken) =>
            {
                var shockerResult = await DoPiShockOperation(type, nbSeconds, intensity, shockerCode);
                result.Add(shockerCode, shockerResult);
            });

            return result;
        }

        private async Task<OperationResult> DoPiShockOperation(FunType type, int nbSeconds, int intensity, string shockerCode)
        {
            var result = new OperationResult();
            try
            {
                string json = $"{{\"Username\":\"{_username}\",\"Name\":\"TurretShocky\",\"Code\":\"{shockerCode}\",\"Intensity\":\"{intensity}\",\"Duration\":\"{nbSeconds}\",\"ApiKey\":\"{_apiKey}\",\"Op\":\"{(int)type}\"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://do.pishock.com/api/apioperate", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Success = true;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Error: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Exception: {ex.Message}";
            }
            return result;
        }

        public class OperationResult
        {
            public OperationResult()
            {
                Success = false;
                Message = string.Empty;
            }

            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
