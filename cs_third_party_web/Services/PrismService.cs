using cs_third_party_web.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static cs_third_party_web.Models.LogPushSettings;
using static cs_third_party_web.Models.LocalDb;

namespace cs_third_party_web.Services
{
    public class PrismService
    {
        public class PrismLogPushRequestDto
        {
            public string employee_no { get; set; }
            public string device_serial { get; set; }
            public string entry_time { get; set; }
            public string entry_date { get; set; }
        }
        public async Task<string> PushAttendanceToPrism(Client client_info, List<AttendanceLog> attendanceLogs)
        {
            var prismObj = await FormatAttendanceLogs(attendanceLogs);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{JWT_PRISM}");

            string requestBody = JsonConvert.SerializeObject(prismObj);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(client_info.api_log_push, content);

                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                    string data = await response.Content.ReadAsStringAsync();
                    return "success";
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    //var error = JsonConvert.DeserializeObject<JObject>(result);
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
        public async Task<List<PrismLogPushRequestDto>> FormatAttendanceLogs(List<AttendanceLog> attendanceLogs)
        {
            return attendanceLogs.Select(x => new PrismLogPushRequestDto { employee_no = x.person_identifier, device_serial = x.device_identifier, entry_time = x.log_time.ToString("HH:mm:ss"), entry_date = x.log_time.ToString("yyyy-MM-dd") }).ToList();
        }
    }
}
