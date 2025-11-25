using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EngineMonitoring.Services
{
    public class FuelSenseApiClient : IDisposable
    {
        private readonly HttpClient _client;
        private string _apiUrl;
        private bool _isEnabled;

        public FuelSenseApiClient()
        {
            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            
            // Default production URL
            _apiUrl = "https://capstone-website-snowy.vercel.app/api/sensor-data";
            _isEnabled = true; // Always enabled - auto send to website
        }

        public void SetApiUrl(string baseUrl)
        {
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                _apiUrl = $"{baseUrl.TrimEnd('/')}/api/sensor-data";
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public bool IsEnabled => _isEnabled;

        public async Task<bool> SendSensorDataAsync(
            double rpm,
            double torque,
            double maf,
            double temperature,
            double fuelConsumption,
            double? customSensor = null,
            bool? alertStatus = null)
        {
            if (!_isEnabled)
            {
                return false; // Silently skip if disabled
            }

            try
            {
                var payload = new
                {
                    rpm = rpm,
                    torque = torque,
                    maf = maf,
                    temperature = temperature,
                    fuelConsumption = fuelConsumption,
                    customSensor = customSensor,
                    alertStatus = alertStatus,
                    timestamp = DateTime.UtcNow.ToString("o") // ISO 8601 format
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(_apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ [API] Data sent successfully - Status: {response.StatusCode}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ [API] Failed: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ [API] Network Error: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"❌ [API] Timeout: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [API] Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWithRetryAsync(
            double rpm,
            double torque,
            double maf,
            double temperature,
            double fuelConsumption,
            double? customSensor = null,
            int maxRetries = 3)
        {
            if (!_isEnabled) return false;

            for (int i = 0; i < maxRetries; i++)
            {
                var success = await SendSensorDataAsync(rpm, torque, maf, temperature, fuelConsumption, customSensor);
                if (success)
                {
                    return true;
                }

                if (i < maxRetries - 1)
                {
                    // Exponential backoff: 1s, 2s, 3s
                    await Task.Delay(1000 * (i + 1));
                    Console.WriteLine($"🔄 [API] Retry {i + 1}/{maxRetries}...");
                }
            }

            Console.WriteLine($"❌ [API] Failed after {maxRetries} retries");
            return false;
        }

        public async Task<bool> CheckConnectionAsync()
        {
            if (!_isEnabled) return false;

            try
            {
                var healthUrl = _apiUrl.Replace("/api/sensor-data", "/api/health");
                var response = await _client.GetAsync(healthUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ [API] Connection successful");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ [API] Connection failed: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [API] Connection error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetLatestDataAsync()
        {
            if (!_isEnabled) return null;

            try
            {
                var latestUrl = _apiUrl + "/latest";
                var response = await _client.GetAsync(latestUrl);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"❌ [API] Failed to get latest data: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [API] Error getting latest data: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
