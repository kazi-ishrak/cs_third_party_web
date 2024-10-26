using cs_third_party_web.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static cs_third_party_web.Models.LogPushSettings;
using static cs_third_party_web.Models.LocalDb;
using Newtonsoft.Json.Linq;

namespace cs_third_party_web.Services
{
    public class CsService
    {

        public class CSLogPushRequestWrapper
        {
            public List<CSLogPushRequestDto> data { get; set; }
        }

        public class CSLogPushRequestDto
        {
            public string registration_id { get; set; }
            public string access_time { get; set; }
            public string access_date { get; set; }
            public string unit_id { get; set; }
            public string card { get; set; }
        }

        public async Task<string> PushAttendanceToCs(Client client_info, List<AttendanceLog> attendanceLogs)
        {
            var requestWrapper = await FormatAttendanceLogs(attendanceLogs);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string requestBody = JsonConvert.SerializeObject(requestWrapper);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(client_info.api_log_push, content);

                if (response.IsSuccessStatusCode)
                {
                    string JsonString = await response.Content.ReadAsStringAsync();
                    var JsonObject = JObject.Parse(JsonString);
                    if (JsonObject["success"].Value<bool>())
                        return "success";
                    else
                        return JsonObject["message"].ToString();
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

        public async Task<CSLogPushRequestWrapper> FormatAttendanceLogs(List<AttendanceLog> attendanceLogs)
        {
            var requestWrapper = new CSLogPushRequestWrapper
            {
                data = attendanceLogs.Select(x => new CSLogPushRequestDto
                {
                    registration_id = x.person_identifier ?? "",
                    card = x.rfid ?? "",
                    unit_id = x.device_identifier ?? "",
                    access_time = x.log_time.ToString("HH:mm:ss"),
                    access_date = x.log_time.ToString("yyyy-MM-dd")
                }).ToList()
            };
            return requestWrapper;
        }
    }
}
