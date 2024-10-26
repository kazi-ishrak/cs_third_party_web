using Azure;
using cs_third_party_web.Handler;
using cs_third_party_web.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static cs_third_party_web.Models.LocalDb;
using static cs_third_party_web.Models.LogPushSettings;
using static cs_third_party_web.Services.PihrService;

namespace cs_third_party_web.Services
{
    public class ZohoService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ZohoService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private string? accessToken = null;
        // Constructor now takes IServiceScopeFactory instead of DbHandler
        public ZohoService(ILogger<ZohoService> logger, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public class ZohoCheckInLogPushRequestDto
        {
            public string empId { get; set; }
            public string checkIn { get; set; }
        }

        public class ZohoCheckOutLogPushRequestDto
        {
            public string empId { get; set; }
            public string checkOut { get; set; }
        }

        public class AccessTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }

        public async Task<string?> GetAccessToken(string url, string key, string clientId, string clientSecret)
        {
            string requestUri = $"{url}?refresh_token={key}&client_id={clientId}&client_secret={clientSecret}&grant_type=refresh_token";
            HttpClient client = new HttpClient();
            var content = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
            try
            {
                HttpResponseMessage response = await client.PostAsync(requestUri, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseBody);
                    return accessTokenResponse.AccessToken;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    LogHandler.WriteDebugLog("Zoho_token - " + error);
                }             
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }

        public async Task<string> PushAttendanceToZoho(Client client_info, List<AttendanceLog> attendanceLogs)
        {
            if (accessToken == null)
            {
                accessToken = await GetAccessToken(client_info.api_auth, client_info.auth_key, client_info.clientId, client_info.clientSecret);
            }
             
            if (accessToken != null)
            {
                var allLogs = new List<object>();
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbHandler = scope.ServiceProvider.GetRequiredService<DbHandler>();
                    Dictionary<string, int> UniqueEmployees = new Dictionary<string, int>();
                    foreach (var log in attendanceLogs)
                    {
                        bool? isCheckIn = false;

                        if (!UniqueEmployees.ContainsKey(log.person_identifier))
                        {
                            isCheckIn = await dbHandler.CheckIfTheLogIsCheckIn(log);
                            UniqueEmployees.Add(log.person_identifier, 1);
                        }

                        if (isCheckIn == null)
                        {
                            return "Database error";
                        }

                        if (isCheckIn.Value)
                        {
                            allLogs.Add(new ZohoCheckInLogPushRequestDto
                            {
                                empId = log.person_identifier,
                                checkIn = log.log_time.ToString("yyyy-MM-dd HH:mm:ss")
                            });
                        }
                        else
                        {
                            allLogs.Add(new ZohoCheckOutLogPushRequestDto
                            {
                                empId = log.person_identifier,
                                checkOut = log.log_time.ToString("yyyy-MM-dd HH:mm:ss")
                            });
                        }
                    }
                }

                string jsonData = JsonConvert.SerializeObject(allLogs);

                try
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
                    var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(jsonData, Encoding.UTF8, "application/json"), "data");
                    formData.Add(new StringContent("yyyy-MM-dd HH:mm:ss"), "dateFormat");

                    var response = await client.PostAsync(client_info.api_log_push, formData);
                    if (response.IsSuccessStatusCode)
                    {
                        return "success";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        LogHandler.WriteDebugLog($"Jwt token ({accessToken}) expired!");
                        accessToken = null;
                        return "Access Token token expired";
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        LogHandler.WriteDebugLog(error);
                        return error;
                    }
                }
                catch (Exception ex)
                {
                    LogHandler.WriteErrorLog(ex);
                    return ex.Message;
                }
            }

            return "Access token null";
        }
    }
}
