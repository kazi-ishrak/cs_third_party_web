using cs_third_party_web.Handler;
using cs_third_party_web.Methods;
using cs_third_party_web.Models;
using cs_third_party_web.Services;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using static cs_third_party_web.Models.CsResponse;

using static cs_third_party_web.Models.LogPullSettings;
using static cs_third_party_web.Models.LogPullSettings.Service;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace cs_third_party_web.Job
{
    public class AttendanceLogPullJob
    {
        private readonly string ENDPOINT_HRM_SIGNIN = "inovace-client/api/v1/auth/external-sync/sign-in";
        private readonly string ENDPOINT_HRM_ATTENDANCE = "inovace-client/api/v1/attendance/punch-report";
        private readonly string ENDPOINT_HRM_EMPLOYEE_IDENTIFIER_OFFICEID_MAP = "api/v1/projects/identifier-id-map";
        private readonly string ENDPOINT_CS_ATTENDANCE = "api/v1/logs";
        public DateTime LAST_SYNC_TIME = new DateTime();
        public long LAST_ACCESS_ID =0;
        private readonly IConfiguration _config;
        private readonly ApiHandler _api;
        private readonly IServiceProvider _serviceProvider;
        private DbHandler _dbHandler;

        public AttendanceLogPullJob(IConfiguration config, ApiHandler api, IServiceProvider serviceProvider)
        {
            _config = config;
            _api = api;
            _serviceProvider = serviceProvider;
            InitiateDbHandler();
        }
        public void InitiateDbHandler()
        {
            IServiceScope? scope = _serviceProvider.CreateAsyncScope();

            if (scope != null)
            {
                _dbHandler = scope.ServiceProvider.GetRequiredService<DbHandler>();
            }
        }

        public async Task StartProcess()
        {
            string currTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonFilePath = "appsettings.json";
            string jsonText = File.ReadAllText(jsonFilePath);
            JObject jsonObject = JObject.Parse(jsonText);
            JArray jArray = (JArray)jsonObject["LogPull"]["services"];
            List<Service> services = JArrayToList<Service>(jArray);

            foreach (var service in services.Where(x => x.status == true))
            {
                switch (service.code)
                {
                    case "0":
                        await ProcessCsProjects(currTime, service);
                        continue;

                    case "1":
                        await ProcessRumyProjects(currTime, service);
                        continue;

                    default:
                        continue;
                }
            }
        }
        public List<Service> JArrayToList<T>(JArray jsonArray)
        {
            List<Service> objectList = jsonArray.Select(item => item.ToObject<Service>()).ToList();
            return objectList;
        }

        #region CS Service
        public async Task ProcessCsProjects(string currTime, Service service)
        {
            if (service.projects == null)
            {
                return;
            }

            foreach (var project in service.projects)
            {
                await GetCSProjectLogs(project, service, currTime);
            }
        }
        public async Task GetCSProjectLogs(Project project, Service service, string currTime)
        {
            string startTime = await GetStartTime(service.code, project.project_id, null);
            bool isParsed = DateTime.TryParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out DateTime start_time);
            if (isParsed)
            {
                LAST_SYNC_TIME = start_time;
            }
            else
            {
                LAST_SYNC_TIME = new DateTime();
            }


            LogHandler.WriteDebugLog($"{service.code}_{project.project_id} pulling data from {startTime} to {currTime}");
            var logPullServerResponse = await GetLogsFromCS(startTime, currTime, project.token, service.base_url, service.per_page, null);
            if (logPullServerResponse == null || logPullServerResponse.meta == null || logPullServerResponse.meta.total < 1)
            {
                return;
            }

            LogHandler.WriteDebugLog($"total logs = {logPullServerResponse.meta.total}");

            var logs = await ProcessRawLogs(logPullServerResponse);
            bool status = await StoreDataToServer(logs, service, project, project.log_optimization_time);
            await SaveTime(service.code, project.project_id, null, LAST_SYNC_TIME.ToString("yyyy-MM-dd HH:mm:ss"));
            LogHandler.WriteDebugLog($"log processed = {logs.Count}, last time {LAST_SYNC_TIME}");
            if (!status)
            {
                return;
            }

            for (int i = 2; i <= logPullServerResponse.meta.last_page; i++)
            {
                var newLogPullResponse = await GetLogsFromCS(startTime, currTime, project.token, service.base_url, service.per_page, i);
                if (newLogPullResponse == null || newLogPullResponse.data == null)
                {
                    LogHandler.WriteDebugLog("cs break-1 triggered!");
                    break;
                }
                var newLogs = await ProcessRawLogs(newLogPullResponse);
                status = await StoreDataToServer(newLogs, service, project, project.log_optimization_time);
                await SaveTime(service.code, project.project_id, null, LAST_SYNC_TIME.ToString("yyyy-MM-dd HH:mm:ss"));
                LogHandler.WriteDebugLog($"log processed = {newLogs.Count}, last time {LAST_SYNC_TIME}");
                if (!status)
                {
                    LogHandler.WriteDebugLog("cs break-2 triggered!");
                    break;
                }
            }
        }
        public async Task<List<Dictionary<string, string>>> ProcessRawLogs(CSAttendanceLogResponse logPullResponse)
        {
            var logs = new List<Dictionary<string, string>>();
            foreach (var item in logPullResponse.data)
            {
                var dictionary = new Dictionary<string, string>();
                await FlattenObject(dictionary, item);
                logs.Add(dictionary);
            }

            return logs;
        }

        public async Task<CSAttendanceLogResponse?> GetLogsFromCS(string startTime, string endTime, string apiToken, string baseUrl, string perPage, int? page)
        {
            string url = BuildUrlForCS(startTime, endTime, apiToken, baseUrl, perPage);

            if (page != null)
            {
                url = url + $"&page={page}";
            }

            return await _api.PullAttendanceFromCs(url);
        }
        public string BuildUrlForCS(string startTime, string currentTime, string apiToken, string baseUrl, string perPage)
        {
            string url = $"{baseUrl}/{ENDPOINT_CS_ATTENDANCE}?criteria=sync_time&api_token={apiToken}&start={startTime}&end={currentTime}&per_page={perPage}"; //&device_id[]={device}
            return url;
        }
        #endregion

        #region Rumy Service
        public async Task ProcessRumyProjects(string currTime, Service service)
        {
            if (service.projects == null)
            {
                return;
            }

            foreach (var project in service.projects)
            {
                await GetRumyProjectLogs(project, service, currTime);
            }
        }
        public async Task GetRumyProjectLogs(Project project, Service service, string currTime)
        {

            //string startTime = await GetStartTime(service.code, project.project_id, null);
            //bool isParsed = DateTime.TryParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out DateTime start_time);
            //if (isParsed)
            //{
            //    LAST_SYNC_TIME = start_time;
            //}
            //else
            //{
            //    LAST_SYNC_TIME = new DateTime();
            //}

            DateTime startTime = new DateTime(2023, 1, 1);
            LogHandler.WriteDebugLog($"{service.code}_{project.project_id} pulling data from {startTime.ToString("yyyy-MM-dd HH:mm:ss")} to {currTime}");

            long? AccessId = await GetAccessId(service.code, project.project_id);

            if (AccessId == null)
            {
                long.TryParse(project.access_id, out long AccessIdFromSettings);

                if (AccessIdFromSettings > 0)
                {
                    AccessId = AccessIdFromSettings;
                }
            }

            if (AccessId == null)
            {
                return;
            }

            var logPullServerResponse = await GetLogsFromRumy(startTime.ToString("yyyy-MM-dd HH:mm:ss"), currTime, service.base_url, project, AccessId.Value);
            if (logPullServerResponse == null || logPullServerResponse.log.Count == 0)
            {
                return;
            }

            LogHandler.WriteDebugLog($"total logs = {logPullServerResponse.log.Count}");

            var logs = await ProcessRumyLogs(logPullServerResponse, service, AccessId.Value);
            bool status = await StoreDataToServer(logs, service, project, project.log_optimization_time);
            //await SaveTime(service.code, project.project_id, null, LAST_SYNC_TIME.ToString("yyyy-MM-dd HH:mm:ss"));
            await SaveAccessId(service.code, project.project_id);

            LogHandler.WriteDebugLog($"log processed = {logs.Count}, last time {LAST_SYNC_TIME}");
            if (!status)
            {
                return;
            }
        }

        public async Task<RumyResponse?> GetLogsFromRumy(string startTime, string endTime, string baseUrl, Project project, long AccessId)
        {
            DateTime dateTime = DateTime.Parse(startTime);
            var LogRequest = new RumyResponse.RumyLogRequest
            {
                access_id = AccessId,
                operation = project.operation,
                auth_user = project.auth_user,
                auth_code = project.auth_code,
                start_date = DateOnly.FromDateTime(dateTime),
                end_date= DateOnly.FromDateTime(DateTime.Now),
                start_time= TimeOnly.FromDateTime(dateTime).ToString("HH:mm:ss"),
                end_time= TimeOnly.FromDateTime(DateTime.Now).ToString("HH:mm:ss")

            };
            string JsonRequest = JsonConvert.SerializeObject(LogRequest);
            var rummyResponse = await _api.PullAttendanceFromRumy(baseUrl, JsonRequest);
            return rummyResponse;
        }
        public async Task<List<Dictionary<string, string>>> ProcessRumyLogs(RumyResponse logPullResponse, Service service, long AccessId)
        {
            var logs = new List<Dictionary<string, string>>();
            foreach (var item in logPullResponse.log)
            {
                var dictionary = new Dictionary<string, string>();
                dictionary= await FormatLogsIntoTableColumnNames(item,  service, AccessId);
                logs.Add(dictionary);
            }

            return logs;
        }

        public async Task<Dictionary<string, string>> FormatLogsIntoTableColumnNames(RumyResponse.RumyModelLogResponse item, Service service, long AccessId)
        {
            JObject TableColumns = service.table_columns;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary["access_id"] = item.access_id.ToString();

            if (TableColumns.ContainsKey("person_identifier"))
            {
                dictionary["person_identifier"] = item.registration_id;
            }
                
            if(TableColumns.ContainsKey("logged_time"))
            {
                var isValidDate = DateTime.TryParse(item.access_date.ToString(), out DateTime accessDate);
                var isValidTime = TimeSpan.TryParse(item.access_time.ToString(), out TimeSpan accessTime);

                if (isValidDate && isValidTime)
                {
                    DateTime logTime = accessDate.Date + accessTime;
                    dictionary["logged_time"] = logTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            if (TableColumns.ContainsKey("rfid"))
            {
                dictionary["rfid"] = item.card;
            }
                
            if (TableColumns.ContainsKey("device_identifier"))
            {
                dictionary["device_identifier"] = item.unit_id;
            } 

            return await Task.FromResult(dictionary);
        }

        public async Task<long?> GetAccessId(string service_code, string project_code)
        {
            try
            {
                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AccessIds");
                string filePath = "";
                filePath = Path.Combine(directoryPath, $"access_id_{service_code}_{project_code}.txt");
                var AccessId = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                if (long.TryParse(AccessId, out long access_id))
                    return access_id;
                else
                    return null;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }
        public async Task SaveAccessId(string service_code, string project_id)
        {
            try
            {
                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AccessIds");
                string filePath = "";
                filePath = Path.Combine(directoryPath, $"access_id_{service_code}_{project_id}.txt");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                await File.WriteAllTextAsync(filePath, LAST_ACCESS_ID.ToString());
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
        }
        #endregion

        #region HRM Service
        /* public async Task ProcessHrmProjects(string currTime, Service service)
         {
             if (service.projects == null)
             {
                 return;
             }

             foreach (var project in service.projects)
             {
                 string? jwtToken = await _api.GetJwtTokenFromHrm($"{service.base_url}/{ENDPOINT_HRM_SIGNIN}", project.email, project.password);

                 if (String.IsNullOrWhiteSpace(jwtToken))
                 {
                     continue;
                 }

                 await GetDeviceLogs(project, service, currTime, jwtToken);

             }
         }
         public async Task GetDeviceLogs(Project project, Service service, string currTime, string jwtToken)
         {
             foreach (var device in project.devices)
             {
                 LAST_SYNC_TIME = new DateTime();
                 string startTime = await GetStartTime(service.code, project.project_id, device);
                 LogHandler.WriteDebugLog($"{service.code}_{project.project_id}_{device}, pulling data from {startTime} to {currTime}");
                 HrmResponse.HRMAttendanceLogResponse? logPullServerResponse = await GetLogsFromHRM(startTime, currTime, jwtToken, service.base_url, service.per_page, device, null);
                 if (logPullServerResponse == null || logPullServerResponse.totalPages < 1 || logPullServerResponse.total < 1)
                 {
                     continue;
                 }

                 LogHandler.WriteDebugLog($"total logs = {logPullServerResponse.total}");

                 var logs = await ProcessRawLogs(logPullServerResponse);
                 bool storeStatus = await StoreDataToServer(logs, service, project.project_id, project.log_optimization_time);
                 await SaveTime(service.code, project.project_id, device, LAST_SYNC_TIME.ToString("yyyy-MM-dd HH:mm:ss"));
                 LogHandler.WriteDebugLog($"log processed = {logs.Count}, last time {LAST_SYNC_TIME}");
                 if (!storeStatus)
                 {
                     continue;
                 }

                 for (int i = 2; i <= logPullServerResponse.totalPages; i++)
                 {
                     HrmResponse.HRMAttendanceLogResponse? newLogPullResponse = await GetLogsFromHRM(startTime, currTime, jwtToken, service.base_url, service.per_page, device, i);
                     if (newLogPullResponse == null || newLogPullResponse.reportResponseList == null)
                     {
                         LogHandler.WriteDebugLog("hrm break-1 triggered!");
                         break;
                     }
                     var newLogs = await ProcessRawLogs(newLogPullResponse);
                     storeStatus = await StoreDataToServer(newLogs, service, project.project_id, project.log_optimization_time);
                     await SaveTime(service.code, project.project_id, device, LAST_SYNC_TIME.ToString("yyyy-MM-dd HH:mm:ss"));
                     LogHandler.WriteDebugLog($"log processed = {newLogs.Count}, last time {LAST_SYNC_TIME}");
                     if (!storeStatus)
                     {
                         LogHandler.WriteDebugLog("hrm break-2 triggered!");
                         break;
                     }
                 }
             }
         }

         public async Task<List<Dictionary<string, string>>> ProcessRawLogs(HrmResponse.HRMAttendanceLogResponse logPullResponse)
         {
             var logs = new List<Dictionary<string, string>>();
             foreach (var item in logPullResponse.reportResponseList)
             {
                 Dictionary<string, string> mergedDictionary = await MergeObjectsToDictionary(item);
                 logs.Add(mergedDictionary);
             }

             return logs;
         }

         public async Task<HrmResponse.HRMAttendanceLogResponse?> GetLogsFromHRM(string startTime, string endTime, string jwtToken, string baseUrl, string perPage, string device, int? page)
         {
             string url = BuildUrlForHRM(startTime, endTime, baseUrl, perPage, device);

             if (page != null)
             {
                 url = url + $"&pageNumber={page}";
             }
             else
             {
                 url = url + $"&pageNumber=0";
             }

             return await _api.PullAttendanceFromHrm(url, jwtToken);
         }

         public string BuildUrlForHRM(string startTime, string currTime, string baseUrl, string perPage, string device)
         {
             try
             {
                 var start_time = DateTime.ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null).AddSeconds(1);
                 var curr_time = DateTime.ParseExact(currTime, "yyyy-MM-dd HH:mm:ss", null).AddSeconds(1);
                 return $"{baseUrl}/{ENDPOINT_HRM_ATTENDANCE}?from={((DateTimeOffset)start_time).ToUnixTimeMilliseconds()}&to={((DateTimeOffset)curr_time).ToUnixTimeMilliseconds()}&perPage={perPage}&order=asc&sortKey=reportPosition&deviceId={device}";
             }
             catch (Exception ex)
             {
                 LogHandler.WriteErrorLog(ex);
             }

             return "";
         }*/
        #endregion

        #region Common
        public async Task<string> GetStartTime(string service_code, string project_code, string? device_code)
        {
            try
            {
                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Times");
                string filePath = "";

                if (device_code == null)
                {
                    filePath = Path.Combine(directoryPath, $"time_{service_code}_{project_code}.txt");
                }
                else
                {
                    filePath = Path.Combine(directoryPath, $"time_{service_code}_{project_code}_{device_code}.txt");
                }

                var startTime = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return startTime;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                var dt = DateTime.Now; //7 days before
                var todayStart = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0);
                string startTime = todayStart.ToString("yyyy-MM-dd HH:mm:ss");
                return startTime;
            }
        }

        public async Task SaveTime(string service_code, string project_code, string? device_code, string currTime)
        {
            try
            {
                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Times");
                string filePath = "";

                if (device_code == null)
                {
                    filePath = Path.Combine(directoryPath, $"time_{service_code}_{project_code}.txt");
                }
                else
                {
                    filePath = Path.Combine(directoryPath, $"time_{service_code}_{project_code}_{device_code}.txt");
                }

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                await File.WriteAllTextAsync(filePath, currTime);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
        }
        public async Task<Dictionary<string, string>> MergeObjectsToDictionary(HrmResponse.HRMAttendanceLogResponse.HRMAttendanceLogResponseData attendanceData)
        {
            var dictionary = new Dictionary<string, string>();
            try
            {
                await FlattenObject(dictionary, attendanceData);
                await FlattenObject(dictionary, attendanceData.singleInOutResponse);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
            return dictionary;
        }

        public async Task FlattenObject(Dictionary<string, string> dictionary, object obj)
        {
            foreach (var prop in obj.GetType().GetProperties())
            {
                try
                {
                    var key = $"{prop.Name}";

                    if (prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType)
                    {
                        dictionary[key] = prop.GetValue(obj)?.ToString() ?? "";
                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        await FlattenObject(dictionary, prop.GetValue(obj));
                    }
                }
                catch (Exception ex)
                {
                    LogHandler.WriteErrorLog(ex);
                }
            }
        }

        public async Task<string?> GetConnectionString(string database)
        {
            switch (database)
            {
                case "mssql":
                    return _config.GetConnectionString("mssqlConnection");
                case "mysql":
                    return _config.GetConnectionString("mysqlConnection");
                case "oracle":
                    return _config.GetConnectionString("oracleConnection");
                default:
                    return null;
            }
        }
        public async Task<Dictionary<string, HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse.HRMEmployeeIdentifierOfficeIdMapResponseData>> GetEmployeeIdentifierAndOfficeIdMap(string baseUrl, string api_token)
        {
            string url = $"{baseUrl}/{ENDPOINT_HRM_EMPLOYEE_IDENTIFIER_OFFICEID_MAP}?api_token={api_token}";
            var employeeIdentifierOfficeIdMap = await _api.GetEmployeeIdentifierOfficeIdMapfromHrm(url);

            if (employeeIdentifierOfficeIdMap == null || employeeIdentifierOfficeIdMap.employeeIdentifierOfficeIdMap == null)
            {
                return null;
            }

            return employeeIdentifierOfficeIdMap.employeeIdentifierOfficeIdMap;
        }
        public async Task<bool> StoreDataToServer(List<Dictionary<string, string>> logs, Service service, Project project, int log_optimization_time)
        {
            string database = _config["Database"];
            string tableName = service.table_name;
            JObject tableColumns = service.table_columns;

            string? connectionString = await GetConnectionString(database);
            if (connectionString == null)
            {
                return false;
            }

            var employeeIdentifierOfficeIdMap = new Dictionary<string, HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse.HRMEmployeeIdentifierOfficeIdMapResponseData>();
            if (project.runtime_hrm_identifier.HasValue && project.runtime_hrm_identifier.Value)
            {
                employeeIdentifierOfficeIdMap = await GetEmployeeIdentifierAndOfficeIdMap(service.base_url, project.token);
            }

            foreach (var item in logs)
            {
                try
                {
                    JObject jsonData = JObject.FromObject(item);
                    JObject processedData = ProcessData(database, project, JObject.FromObject(tableColumns), jsonData, employeeIdentifierOfficeIdMap);

                    string log_time = processedData["logged_time"].ToString();
                    if (jsonData.ContainsKey("sync_time"))
                    {
                        log_time = jsonData["sync_time"].ToString();
                    }

                    //if (processedData["person_identifier"].ToString().StartsWith("["))
                    //{
                    //    continue;
                    //}

                    bool? recordExists = await RecordExists(service, database, connectionString, tableName, tableColumns, processedData, log_optimization_time);

                    if (recordExists == null)
                    {
                        LogHandler.WriteDebugLog("record exists check error!");
                        return false;
                    }

                    string sqlScript = "";
                    if (service.stored_procedure_status.HasValue && service.stored_procedure_status.Value)
                    {
                        sqlScript = GenerateStoredProcedureSql(database, tableName, JObject.FromObject(tableColumns), processedData, service.stored_procedure);
                    }
                    else
                    {
                        sqlScript = GenerateInsertSql(database, tableName, JObject.FromObject(tableColumns), processedData);
                    }

                    LogHandler.WriteDebugLog("Data to be inserted: " + sqlScript);

                    if (!recordExists.Value)
                    {
                        if (database == "mssql")
                        {
                            await _dbHandler.StoreDataToMSSQLServer(connectionString, sqlScript);
                        }
                        else if (database == "mysql")
                        {
                            await _dbHandler.StoreDataToMySQLServer(connectionString, sqlScript);
                        }
                        else if (database == "oracle")
                        {
                            await _dbHandler.StoreDataToOracleServer(connectionString, sqlScript);
                        }
                    }

                    bool updateLastLog = UpdateLastLogTime(log_time);

                    if (service.code == "1")
                    {
                       bool updateAccessId = UpdateMaxAccessId(processedData["access_id"].ToString());
                    }
                    
                }
                catch (Exception ex)
                {
                    LogHandler.WriteErrorLog(ex);
                    return false;
                }
            }

            return true;
        }


        public JObject ProcessData(string database, Project project, JObject columnMappings, JObject data, Dictionary<string, HrmResponse.HRMEmployeeIdentifierOfficeIdMapResponse.HRMEmployeeIdentifierOfficeIdMapResponseData>? employeeIdentifierOfficeIdMap)
        {
            try
            {
                if (columnMappings.ContainsKey("project_id"))
                {
                    data["project_id"] = project.project_id;
                }

                //mgi only
                //if (data.ContainsKey("device_identifier"))
                //{
                //    data["device_identifier"] = "888";
                //}
                //

                if (data.ContainsKey("time"))
                {
                    string dateTime = data["time"].ToString();
                    DateTime? punchTime = UnixTimeStampToDateTime(dateTime);
                    if (punchTime != null)
                    {
                        data["logged_time"] = punchTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }

                //if (data.ContainsKey("person_identifier"))
                //{
                //    string[]? personIdentifierParts = data["person_identifier"]?.ToString().Split('-');

                //    if (personIdentifierParts != null)
                //    {
                //        if (personIdentifierParts.Length > 0)
                //        {
                //            data["person_identifier"] = personIdentifierParts[0];
                //        }

                //        if (personIdentifierParts.Length > 1)
                //        {
                //            data["person_name"] = personIdentifierParts[1];
                //        }
                //    }
                //}
                //else if (data.ContainsKey("employeeOfficeId"))
                //{
                //    data["person_identifier"] = data["employeeOfficeId"];
                //}

                if (columnMappings.ContainsKey("created_at"))
                {
                    data["created_at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (columnMappings.ContainsKey("flag"))
                {
                    data["flag"] = 0;
                }

                if (columnMappings.ContainsKey("person_identifier"))
                {
                    string personIdentifierValue = data["person_identifier"].ToString();

                    if (personIdentifierValue.StartsWith("["))
                    {
                        string[] parts = personIdentifierValue.Split(']');
                        if (parts.Length > 1)
                        {
                            data["person_identifier"] = parts[1];
                        }
                    }

                    if (project.runtime_hrm_identifier.HasValue && project.runtime_hrm_identifier.Value)
                    {
                        if (employeeIdentifierOfficeIdMap != null && employeeIdentifierOfficeIdMap.ContainsKey(personIdentifierValue))
                        {
                            data["person_identifier"] = employeeIdentifierOfficeIdMap[personIdentifierValue].employeeOfficeId ?? personIdentifierValue;
                        }
                    }
                }

                if (columnMappings.ContainsKey("logged_time"))
                {
                    string format = GetDateTimeFormat(database, "datetime");
                    data["logged_time"] = DateTime.Parse(data["logged_time"]?.ToString()).ToString(format);
                }

                if (columnMappings.ContainsKey("logged_time_oDate"))
                {
                    string format = GetDateTimeFormat(database, "date");
                    data["logged_time_oDate"] = DateTime.Parse(data["logged_time"]?.ToString()).ToString(format);
                }

                if (columnMappings.ContainsKey("logged_time_oTime"))
                {
                    string format = GetDateTimeFormat(database, "time");
                    data["logged_time_oTime"] = DateTime.Parse(data["logged_time"]?.ToString()).ToString(format);
                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
            return data;
        }
        public string GetDateTimeFormat(string database, string datetimetype)
        {
            string formatLoggedTime = "yyyy-MM-dd HH:mm:ss";
            string formatLoggedTimeoDate = "yyyy-MM-dd";
            string formatLoggedTimeoTime = "HH:mm:ss";

            if (database == "oracle")
            {
                formatLoggedTime = "dd-MMM-yyyy hh:mm:ss tt";
            }

            if (datetimetype == "date")
            {
                return formatLoggedTimeoDate;
            }
            else if (datetimetype == "time")
            {
                return formatLoggedTimeoTime;
            }
            else
            {
                return formatLoggedTime;
            }
        }
        public string GenerateInsertSql(string database, string tableName, JObject columnMappings, JObject data)
        {
            if (database == "oracle")
            {
                var columns = columnMappings.Properties();
                string columnList = string.Join(", ", columns.Select(column => column.Value.ToString()));

                string values = string.Join(", ", columns.Select(column =>
                {
                    if (column.Name == "logged_time" || column.Name == "sync_time" || column.Name == "created_at")
                    {
                        DateTime dt = (DateTime)data[column.Name];
                        return $"'{dt.ToString("dd-MMM-yyyy hh:mm:ss tt")}'";
                    }
                    else
                    {
                        string value = data[column.Name]?.ToString() ?? "";
                        return $"'{EscapeSqlString(value)}'";
                    }
                }));

                return $"INSERT INTO {tableName} ({columnList}) VALUES ({values})";
            }
            else
            {
                var columns = columnMappings.Properties();
                string columnList = string.Join(", ", columns.Select(column => column.Value.ToString()));
                string values = string.Join(", ", columns.Select(column =>
                {
                    string value = data[column.Name]?.ToString() ?? "";
                    return $"'{EscapeSqlString(value)}'";
                }));
                return $"INSERT INTO {tableName} ({columnList}) VALUES ({values})";
            }
        }

        public string EscapeSqlString(string input)
        {
            return input.Replace("'", "''");
        }

        public string GenerateStoredProcedureSql(string database, string tableName, JObject columnMappings, JObject data, string storedProcedure)
        {
            string values = "";
            if (database == "oracle")
            {
                var columns = columnMappings.Properties();
                //string columnList = string.Join(", ", columns.Select(column => column.Value.ToString()));

                values = string.Join(", ", columns.Select(column =>
                {
                    if (column.Name == "logged_time" || column.Name == "sync_time" || column.Name == "created_at")
                    {
                        DateTime dt = (DateTime)data[column.Name];
                        return $"'{dt.ToString("dd-MMM-yyyy hh:mm:ss tt")}'";
                    }
                    else if (column.Name == "logged_time_oDate")
                    {
                        DateTime dt = (DateTime)data[column.Name];
                        return $"'{dt.ToString("MM-dd-yyyy")}'";             //Need to be dynamic in next version
                    }
                    else
                    {
                        return $"'{data[column.Name]}'";
                    }
                }));
            }
            else
            {
                var columns = columnMappings.Properties();
                //string columnList = string.Join(", ", columns.Select(column => column.Value.ToString()));
                values = string.Join(", ", columns.Select(column => $"'{data[column.Name]}'"));
            }

            return $"{storedProcedure} ({values})";
        }

        public DateTime? UnixTimeStampToDateTime(string timestampString)
        {
            try
            {
                long timestampSeconds = long.Parse(timestampString);
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestampSeconds).ToLocalTime();
                return dateTimeOffset.LocalDateTime;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public bool UpdateMaxAccessId(string access_id)
        {
            if (long.TryParse(access_id, out long x))
            {
                if (LAST_ACCESS_ID < x)
                {
                    LAST_ACCESS_ID = x;
                    return true;
                }
            }

            return false;
        }
        public bool UpdateLastLogTime(string logged_time)
        {
            bool isParsed = DateTime.TryParseExact(logged_time, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out DateTime logTime);
            if (isParsed && logTime > LAST_SYNC_TIME)
            {
                LAST_SYNC_TIME = logTime;
                return true;
            }

            return false;
        }

        public JObject FilterProcessedDataByLogOptimizationTime(string database, JObject processedData, JObject tableColumns, DateTime loggedTime, int log_optimization_time)
        {
            DateTime loggedTime_up = loggedTime.AddMilliseconds(log_optimization_time);
            DateTime loggedTime_lo = loggedTime.AddMilliseconds(-log_optimization_time);

            if (tableColumns.ContainsKey("logged_time"))
            {
                string format = GetDateTimeFormat(database, "datetime");
                processedData["logged_time_up"] = loggedTime_up.ToString(format);
                processedData["logged_time_lo"] = loggedTime_lo.ToString(format);
            }
            else if (tableColumns.ContainsKey("logged_time_oDate"))
            {
                string format = GetDateTimeFormat(database, "date");
                processedData["logged_time_up"] = loggedTime_up.ToString(format);
                processedData["logged_time_lo"] = loggedTime_lo.ToString(format);
            }
            else if (tableColumns.ContainsKey("logged_time_oTime"))
            {
                string format = GetDateTimeFormat(database, "time");
                processedData["logged_time_up"] = loggedTime_up.ToString(format);
                processedData["logged_time_lo"] = loggedTime_lo.ToString(format);
            }

            return processedData;
        }
        async Task<bool?> RecordExists(Service service, string database, string connectionString, string tableName, JObject tableColumns, JObject processedData, int log_optimization_time)
        {
            string loggedTimeString =  processedData["logged_time"].ToString() ;

            bool parseLoggedTime = DateTime.TryParse(loggedTimeString, out DateTime loggedTime);
            if (!parseLoggedTime)
            {
                return null;
            }

            processedData = FilterProcessedDataByLogOptimizationTime(database, processedData, tableColumns, loggedTime, log_optimization_time);

            IEnumerable<JProperty>? columns = tableColumns.Properties();

            string? person_id = null;
            if (tableColumns.ContainsKey("person_id"))
            {
                person_id = "person_id";
            }

            string? person_identifier = null;
            if (tableColumns.ContainsKey("person_identifier"))
            {
                person_identifier = "person_identifier";
            }

            string? project_id = null;
            if (tableColumns.ContainsKey("project_id"))
            {
                project_id = "project_id";
            }

            string? device_identifier = null;
            if (tableColumns.ContainsKey("device_identifier"))
            {
                device_identifier = "device_identifier";
            }

            string? uid = null;
            if (service.uid_duplication_check && tableColumns.ContainsKey("uid"))
            {
                uid = "uid";

            }

            var filteredColumns = tableColumns.Properties().Where(column => new[] { person_id, person_identifier, project_id, device_identifier, uid }.Contains(column.Name))
            .Concat(new[]
            {
                    new JProperty("logged_time_up", tableColumns["logged_time"] ?? tableColumns["logged_time_oDate"] ?? tableColumns["logged_time_oTime"]),
                    new JProperty("logged_time_lo", tableColumns["logged_time"] ?? tableColumns["logged_time_oDate"] ?? tableColumns["logged_time_oTime"])
            });

            string whereClause = BuidWhereClause(service, database, processedData, filteredColumns, tableColumns);
            string query = $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";
            return await _dbHandler.ExecuteRecordExists(database, connectionString, query, tableColumns, filteredColumns, processedData);
        }

        public string BuidWhereClause(Service service, string database, JObject processedData, IEnumerable<JProperty> filteredColumns, JObject tableColumns)
        {
            string syntax = "@";
            if (database == "oracle")
            {
                syntax = ":";
            }

            if (service.uid_duplication_check)
            {
                return $"{tableColumns["uid"]} = {syntax}{"l_uid"}";
            }

            string whereClause = string.Join(" AND ", filteredColumns.Select(column =>
            {
                string columnName = column.Name;
                object columnValue = (processedData[columnName] as JValue)?.Value;
                switch (columnName)
                {
                    case "logged_time_up":

                        if (tableColumns.ContainsKey("logged_time"))
                        {
                            return $"{column.Value} <= {syntax}{column.Name}";
                        }
                        else if (tableColumns.ContainsKey("logged_time_oDate"))
                        {
                            return $"TRUNC({column.Value}) <= {syntax}{column.Name}";
                        }
                        else
                        {
                            return $"{column.Value} <= {syntax}{column.Name}";
                        }

                    case "logged_time_lo":
                        if (tableColumns.ContainsKey("logged_time"))
                        {
                            return $"{column.Value} >= {syntax}{column.Name}";
                        }
                        else if (tableColumns.ContainsKey("logged_time_oDate"))
                        {
                            return $"TRUNC({column.Value}) >= {syntax}{column.Name}";
                        }
                        else
                        {
                            return $"{column.Value} >= {syntax}{column.Name}";
                        }

                    default:
                        return $"{column.Value} = {syntax}{column.Name}";
                }
            }));

            return whereClause;
        }


        #endregion

    }
}
