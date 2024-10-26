using cs_third_party_web.Methods;
using cs_third_party_web.Models;
using cs_third_party_web.Services;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace cs_third_party_web.Handler
{
    public class ApiHandler
    {
        private readonly IConfiguration _config;
        public ApiHandler(IConfiguration config)
        {
            _config = config;
        }
        public async Task<CsResponse.CSAttendanceLogResponse?> PullAttendanceFromCs(string url)
        {
            CsResponse.CSAttendanceLogResponse? result = new CsResponse.CSAttendanceLogResponse();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string data = await response.Content.ReadAsStringAsync();
                /*var result0 = JsonConvert.DeserializeObject<object>(data);*/
                result = JsonConvert.DeserializeObject<CsResponse.CSAttendanceLogResponse>(data);
                if (result.meta.total > 0)
                {

                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return result;
        }

        public async Task<RumyResponse?> PullAttendanceFromRumy(string url, string RequestBody)
        {
            RumyResponse? Result = new RumyResponse();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                var RequestContent = new StringContent(RequestBody, Encoding.UTF8, "application/json");
                var Response =  client.PostAsync(url, RequestContent).Result;
                Response.EnsureSuccessStatusCode();
                string Data = await Response.Content.ReadAsStringAsync();
                Result = JsonConvert.DeserializeObject<RumyResponse>(Data);
                
                return Result;
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }
        public async Task<string?> GetJwtTokenFromHrm(string url, string email, string password)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string requestBody = JsonConvert.SerializeObject(new
            {
                email = email,
                password = password
            });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<HrmResponse.HRMLoginResponse>(data);
                return result.token;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public async Task<HrmResponse.HRMAttendanceLogResponse?> PullAttendanceFromHrm(string url, string token)
        {
            HrmResponse.HRMAttendanceLogResponse? result = new HrmResponse.HRMAttendanceLogResponse();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{token}");

            try
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                string data = await response.Content.ReadAsStringAsync();
                var result0 = JsonConvert.DeserializeObject<object>(data);
                result = JsonConvert.DeserializeObject<HrmResponse.HRMAttendanceLogResponse>(data);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return result;
        }
        public async Task<HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse?> GetEmployeeIdentifierOfficeIdMapfromHrm(string url)
        {
            HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse? result = new HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                string data = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse>(data);
                return result;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }

        public async Task<HrmResponse.HRMEmployeeListResponse?> GetEmployeesFromHrm(string url, string token)
        {
            HrmResponse.HRMEmployeeListResponse? result = new HrmResponse.HRMEmployeeListResponse();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{token}");

            try
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                string data = await response.Content.ReadAsStringAsync();
                var result0 = JsonConvert.DeserializeObject<object>(data);
                result = JsonConvert.DeserializeObject<HrmResponse.HRMEmployeeListResponse>(data);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return result;
        }
        public async Task<List<CsResponse.CSEmployeeListResponse>?> GetEmployeesFromCs(string url)
        {
            List<CsResponse.CSEmployeeListResponse>? result = new List<CsResponse.CSEmployeeListResponse>();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                string data = await response.Content.ReadAsStringAsync();
                var result0 = JsonConvert.DeserializeObject<object>(data);
                result = JsonConvert.DeserializeObject<List<CsResponse.CSEmployeeListResponse>>(data);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return result;
        }
    }
}
