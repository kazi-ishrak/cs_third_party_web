using cs_third_party_web.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static cs_third_party_web.Models.LocalDb;
using static cs_third_party_web.Models.LogPushSettings;

namespace cs_third_party_web.Services
{
    public class PihrService
    {
        private string? JWT_PIHR = null;
        public class PihrLogPushRequestDto
        {
            public string user_id { get; set; }
            public string date_time { get; set; }
        }
        public class PihrJwtTokenResponsetDto
        {
            public string access_token { get; set; }
        }
        public async Task<string?> GetJwtTokenFromPihr(string url, string auth_token)
        {

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string requestBody = JsonConvert.SerializeObject(new
            {
                api_key = auth_token
            });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PihrJwtTokenResponsetDto>(data);
                    return result.access_token;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    LogHandler.WriteLog("pihr_token - " + error);
                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public async Task<string> PushAttendanceToPihr(Client client_info, List<AttendanceLog> attendanceLogs)
        {
            if (JWT_PIHR == null)
            {
                JWT_PIHR = await GetJwtTokenFromPihr(client_info.api_auth, client_info.auth_key);
            }

            if (JWT_PIHR != null)
            {
                var pihrObj = attendanceLogs.Select(x => new PihrLogPushRequestDto { user_id = x.person_identifier, date_time = x.log_time.ToString("yyyy-MM-dd HH:mm:ss") }).ToList();

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{JWT_PIHR}");
                string requestBody = JsonConvert.SerializeObject(pihrObj);
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(client_info.api_log_push, content);

                    if (response.IsSuccessStatusCode)
                    {
                        //string data = await response.Content.ReadAsStringAsync();
                        response.EnsureSuccessStatusCode();
                        return "success";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        LogHandler.WriteLog($"Jwt token ({JWT_PIHR}) expired!");
                        JWT_PIHR = null;
                        return "Jwt token expired";
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        LogHandler.WriteLog(error);
                        return error;
                    }
                }
                catch (Exception ex)
                {
                    LogHandler.WriteErrorLog(ex);
                    return ex.Message;
                }
            }

            return "Jwt token null";
        }
    }
}
