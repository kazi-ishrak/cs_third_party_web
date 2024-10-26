using cs_third_party_web.Data;
using cs_third_party_web.Methods;
using cs_third_party_web.Models;
using cs_third_party_web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using static cs_third_party_web.Models.CsDb;
using static cs_third_party_web.Models.LocalDb;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Client = cs_third_party_web.Models.LogPushSettings.Client;
using System.Linq.Dynamic.Core;
using static cs_third_party_web.Models.PageModels;
namespace cs_third_party_web.Handler
{
    public class DbHandler
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _db;
        //private CsDbContext _dbCs = null;
        public DbHandler(IConfiguration config, IServiceProvider serviceProvider, ApplicationDbContext db)
        {
            _config = config;
            _db = db;
            _serviceProvider = serviceProvider;
        }
        public async Task<MapCsHrmEmployee?> GetEmployeeByHrmId(string projectId, int hrmId)
        {
            try
            {
                var employee = await _db.MapCsHrmEmployees.Where(x => x.project_id == projectId && x.hrm_id == hrmId).FirstOrDefaultAsync();
                return employee;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public async Task<MapCsHrmEmployee?> GetEmployeeByCsId(int csId)
        {
            try
            {
                var employee = await _db.MapCsHrmEmployees.Where(x => x.cs_id == csId).FirstOrDefaultAsync();
                return employee;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        //public async Task<CsEmployee?> GetEmployeeFromCS(int csId)
        //{
        //    try
        //    {
        //        var employee = await _dbCs.CsEmployees.Where(x => x.id == csId).FirstOrDefaultAsync();
        //        return employee;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHandler.WriteErrorLog(ex);
        //    }

        //    return null;
        //}
        //public async Task UpdateCsEmployeeIdentifierByHrmIdentifier(List<HrmResponse.HRMEmployeeListResponse.HRMEmployeeListResponseData> employees)
        //{
        //    foreach (var employee in employees)
        //    {
        //        try
        //        {
        //            var emp = await _dbCs.CsEmployees.Where(x => x.id == employee.centralServerId).FirstOrDefaultAsync();
        //            if (emp == null)
        //            {
        //                continue;
        //            }

        //            if (emp.identifier == employee.employeeOfficeId)
        //            {
        //                continue;
        //            }

        //            emp.identifier = employee.employeeOfficeId;
        //            _dbCs.Entry(emp).State = EntityState.Modified;
        //            await _dbCs.SaveChangesAsync();
        //        }
        //        catch (Exception ex)
        //        {
        //            LogHandler.WriteErrorLog(ex);
        //        }
        //    }
        //}
        public async Task<bool> CreateMappingForCsAndHrmEmployee(MapCsHrmEmployee mapping)
        {
            try
            {
                _db.MapCsHrmEmployees.Add(mapping);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return false;
        }
        public async Task<bool> UpdateMappingForCsAndHrmEmployee(MapCsHrmEmployee mapping)
        {
            try
            {
                _db.Entry(mapping).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return false;
        }
        public async Task<MapCsHrmEmployee?> GetMappingForCsAndHrmEmployee(string project_id, int? person_id)
        {
            try
            {
                var map = await _db.MapCsHrmEmployees.Where(x => x.project_id == project_id && x.cs_id == person_id).FirstOrDefaultAsync();
                return map;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public async Task<Project?> GetProjectByApiToken(string api_token)
        {
            try
            {
                var project = await _db.Projects.Where(x => x.api_token == api_token).FirstOrDefaultAsync();
                return project;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }


        public async Task<List<AttendanceLogWithProjectNameDTO>?> GetAllAttendanceLogFromDb(String? ProjectId, DateTime? StartDate, DateTime? EndDate)
        {
            try
            {
                var result = await _db.AttendanceLogs
                    .Where(x => x.log_time >= StartDate && x.log_time < EndDate &&
                                (string.IsNullOrWhiteSpace(ProjectId) || (!string.IsNullOrWhiteSpace(ProjectId) && x.project_id == ProjectId)))
                    .GroupJoin(_db.Projects, logs => logs.project_id, proj => proj.project_id, (logs, proj) => new { logs, proj })
                    .SelectMany(
                        x => x.proj.DefaultIfEmpty(),
                        (x, proj) => new { x.logs, Project = proj }
                    )
                    // Additional join with the map_cs_hrm_employees table
                    .GroupJoin(_db.MapCsHrmEmployees,
                               logProj => logProj.logs.person_id,
                               emp => emp.cs_id,
                               (logProj, emps) => new { logProj, Emps = emps })
                    .SelectMany(
                        x => x.Emps.DefaultIfEmpty(),
                        (x, emp) => new {
                            x.logProj.logs,
                            x.logProj.Project,
                            Emp = emp
                        }
                    )
                    .Select(x => new AttendanceLogWithProjectNameDTO
                    {
                        id = x.logs.id,
                        project_id = x.logs.project_id,
                        device_identifier = x.logs.device_identifier,
                        person_id = x.logs.person_id,
                        person_identifier = x.logs.person_identifier,
                        flag = x.logs.flag,
                        log_time = x.logs.log_time,
                        sync_time = x.logs.sync_time,
                        created_at = x.logs.created_at,
                        remarks = x.logs.remarks,
                        client_sync_time = x.logs.client_sync_time,
                        project_name = x.Project != null ? x.Project.name : "Unknown",
                        hrm_office_id = x.Emp != null ? x.Emp.hrm_office_id : "Not Found",
                        hrm_identifier_enabled = x.Project!=null? x.Project.hrm_identifier : false // Assuming hrm_identifier is a non-nullable type
                    })
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }

        public async Task <List<AttendanceLogRequestWithPrjectNameDTO>?> GetLogRequestDetails(string? ProjectId, DateTime? StartDate, DateTime? EndDate)
        {
            List<AttendanceLogRequestWithPrjectNameDTO> LogRequests = new List<AttendanceLogRequestWithPrjectNameDTO>();
            try
            {
                LogRequests = await _db.AttendanceLogRequests.Where(x => x.created_at >= StartDate && x.created_at < EndDate &&
                                (string.IsNullOrWhiteSpace(ProjectId) || (!string.IsNullOrWhiteSpace(ProjectId) && x.project_id == ProjectId)))
                    .GroupJoin(_db.Projects, al => al.project_id, p => p.project_id, (al, p) => new { al, p })
                    .Select(x => new AttendanceLogRequestWithPrjectNameDTO
                    {
                        project_id = x.al.project_id,
                        project_name = x.p.FirstOrDefault().name,
                        code = x.al.code,
                        message = x.al.message,
                        start_date = x.al.start_date,
                        end_date = x.al.end_date,
                        criteria = x.al.criteria,
                        per_page = x.al.per_page,
                        page = x.al.page,
                        order_direction = x.al.order_direction,
                        order_key = x.al.order_key,
                        api_token = x.al.api_token,
                        created_at=x.al.created_at
                    })
                    .ToListAsync();
                return LogRequests;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
            
        }
        public async Task<List<Project>?> GetProjectById(long id)
        {
            try
            {
                var project = await _db.Projects.Where(x => x.id == id).ToListAsync();
                return project;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }
        public async Task<List<ProjectListWithTypeNameDTO>?> GetAllProjectsListFromDB()
        {
            try
            {
                
                var all_projects = await _db.Projects
                    .GroupJoin(_db.ProjectTypes, p => p.type_id, t => t.id, (p, t) => new { p, t })
                    .Select(
                        x=> new ProjectListWithTypeNameDTO
                        {
                            id = x.p.id,          
                            project_id = x.p.project_id, 
                            code = x.p.code,          
                            name = x.p.name,      
                            organization = x.p.organization, 
                            api_token = x.p.api_token,
                            hrm_identifier = x.p.hrm_identifier, 
                            created_at = x.p.created_at, 
                            updated_at = x.p.updated_at, 
                            type_id = x.p.type_id,      
                            type_name =x.t.FirstOrDefault().name
                        })
                    .ToListAsync();
                return all_projects;
            }
            catch (Exception ex)
            {
                // Log the exception if any occurs and return null
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }


        public async Task<List<MappedEmployeeProjectDTO>?> GetMappedEmployeesWithProjectName(string ProjectId)
        {
            try
            {
                var query = await _db.MapCsHrmEmployees
                    .Where(x => (string.IsNullOrWhiteSpace(ProjectId) || (!string.IsNullOrWhiteSpace(ProjectId) && x.project_id == ProjectId)))
                    .Join(
                        _db.Projects,
                        map => map.project_id,
                        proj => proj.project_id,
                        (map, proj) => new MappedEmployeeProjectDTO
                        {
                            id = map.id,
                            project_id = map.project_id,
                            hrm_id = map.hrm_id,
                            hrm_office_id = map.hrm_office_id,
                            cs_id = map.cs_id,
                            created_at = map.created_at,
                            updated_at = map.updated_at,
                            project_name = proj.name
                        })
                    .ToListAsync();

                return query;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }
        public async Task PushTheLogRequestToDb(AttendanceLogRequest LogRequest)
        {
            try
            {
                await _db.AttendanceLogRequests.AddAsync(LogRequest);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
            }
            
        }
        public async Task InsertProjectIntoDb(Project Data)
        {
            Data.created_at = DateTime.Now;
            Data.updated_at = DateTime.Now;
            await _db.Projects.AddAsync(Data);
            await _db.SaveChangesAsync();
        }

        public async Task<bool?> CheckIfTheLogIsCheckIn(AttendanceLog Log)
        {
            try
            {
                var Result =await _db.AttendanceLogs.Where(x=> x.person_identifier == Log.person_identifier && x.flag== true && x.log_time.Date == Log.log_time.Date).ToListAsync();
                if (Result.Count > 0)
                    return false;
                else
                    return true;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }
        public async Task UpdateExistingProjectIntoDb(Project Data)
        {
            var project = await _db.Projects.Where(x => x.id == Data.id).FirstOrDefaultAsync();
            if (project != null)
            {
                project.project_id = Data.project_id;
                project.type_id = Data.type_id;
                project.code = Data.code;
                project.name = Data.name;
                project.organization = Data.organization;
                project.api_token = Data.api_token;
                project.hrm_identifier = Data.hrm_identifier;
                project.created_at = project.created_at;
                project.updated_at = DateTime.Now;
            }
            try
            {

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
            }
        }

        public async Task<List<ProjectType>?> GetProjectType()
        {
            try
            {
                var TypeList = await _db.ProjectTypes.ToListAsync();
                return TypeList;
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
                return null;
            }
            
        }

        public async Task<List<int>?> GetDashboardInfoCards()
        {
            List<int> Infos = new List<int>();
            try
            {
                var CntTotalProject = await _db.Projects.CountAsync();

                var cntTotalEntered = await _db.AttendanceLogs.Where(x => x.created_at.HasValue && x.created_at.Value.Date == DateTime.Today.Date).CountAsync();
                var CntTotalSynced = await _db.AttendanceLogs.Where(x=> x.client_sync_time.HasValue && x.client_sync_time.Value.Date==DateTime.Today.Date && x.flag== true).CountAsync();
                Infos.Add(CntTotalProject);
                Infos.Add(cntTotalEntered);
                Infos.Add(CntTotalSynced);
                return Infos;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return null;
            }
        }

        public async Task<List<DashboardDTO>?> GetDashboardTable()
        {

            var query = await _db.Projects
                .Join(_db.ProjectTypes, p=> p.type_id , pt=> pt.id, (p, pt)=> new {p,pt})
                .GroupJoin(_db.AttendanceLogs, p => p.p.project_id, al => al.project_id, (p, alGroup) => new { p.p, p.pt, alGroup })
                .SelectMany(x => x.alGroup.DefaultIfEmpty(), (x, al) => new { x.p, x.pt, al })
                .GroupBy(x => new { x.p.project_id, x.p.name, x.p.type_id, TypeName = x.pt.name })
                .Select(x => new DashboardDTO
                {
                    project_id = x.Key.project_id,
                    project_name = x.Key.name,
                    project_type= x.Key.TypeName,
                    type_id=x.Key.type_id,
                    client_sync_time = x.Max(x =>  x.al.client_sync_time ),
                    logs_entered_today = x.Count(x => x.al != null  && x.al.created_at.HasValue && x.al.created_at.Value.Date == DateTime.Today.Date),
                    logs_synced_today = x.Count(x => x.al != null && x.al.flag==true && x.al.client_sync_time.HasValue && x.al.client_sync_time.Value.Date == DateTime.Today.Date)
                }).ToListAsync();
            return query;
        }

        public async Task<List<AttendanceLog>?> GetAttendanceLogByLogTime(DateTime start, DateTime end, string project_id)
        {
            try
            {
                var logs = await _db.AttendanceLogs.Where(x => x.project_id == project_id && x.log_time >= start && x.log_time <= end).ToListAsync();
                return logs;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public async Task<List<AttendanceLog>?> GetAttendanceLogBySyncTime(DateTime start, DateTime end, string project_id)
        {
            try
            {
                var logs = await _db.AttendanceLogs.Where(x => x.project_id == project_id && x.sync_time >= start && x.sync_time <= end).ToListAsync();
                return logs;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }

        public async Task<List<AttendanceLog>?> GetAttendanceLogByClientConfiguration(Client client)
        {
            try
            {
                var logs = new List<AttendanceLog>();

                if (client.hrm_identifier)
                {
                    logs = await _db.AttendanceLogs.Where(x => client.project_ids.Contains(x.project_id) && x.flag != true)
                        .Join(_db.MapCsHrmEmployees, x => x.person_id, y => y.cs_id, (x, y) => new AttendanceLog { id = x.id, uid = x.uid, project_id = x.project_id, device_identifier = x.device_identifier, log_time = x.log_time, sync_time = x.sync_time, person_name = x.person_name, flag = x.flag, location = x.location, rfid = x.rfid, type = x.type, person_identifier = y != null && y.hrm_office_id != null ? y.hrm_office_id : "" })
                        .Where(x => !string.IsNullOrWhiteSpace(x.person_identifier))
                        .OrderBy(x => x.sync_time)
                        .Take(client.per_page)
                        .ToListAsync();
                }
                else
                {
                    
                    logs = await _db.AttendanceLogs.Where(x => client.project_ids.Contains(x.project_id) && x.flag != true)
                        .OrderBy(x => x.sync_time)
                        .Take(client.per_page)
                        .ToListAsync();
                }

                return logs;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return null;
        }
        public async Task UpdateSyncStatusOfAttendanceLogs(List<AttendanceLog> logs, string status)
        {
            try
            {
                foreach (var item in logs)
                {
                    var log = await _db.AttendanceLogs.Where(x => x.id == item.id).FirstOrDefaultAsync();
                    if (log != null)
                    {
                        if (status == "success")
                        {
                            log.remarks = "OK";
                            log.flag = true;
                            log.client_sync_time = DateTime.Now;
                        }
                        else
                        {
                            log.remarks = status;
                        }
                        _db.Entry(log).State = EntityState.Modified;
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
        }
        public async Task<bool> StoreDataToMSSQLServer(string connectionString, string sqlScript)
        {
            using (var context = new DynamicDbContext(connectionString))
            {
                await context.Database.ExecuteSqlRawAsync(sqlScript);
                return true;
            }
        }
        public async Task<bool> StoreDataToMySQLServer(string connectionString, string sqlScript)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new MySqlCommand(sqlScript, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }
        public async Task<bool> StoreDataToOracleServer(string connectionString, string sqlScript)
        {
            //LogHandler.WriteDebugLog("Inserting data: " + sqlScript);
            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(sqlScript, connection))
                {
                    await command.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }
        public async Task<bool?> ExecuteRecordExists(string database, string connectionString, string query, JObject tableColumns, IEnumerable<JProperty> filteredColumns, JObject processedData)
        {
            var paramNamesInQuery = Regex.Matches(query, @"[:@]\w+") // Modified regex
                     .Cast<Match>()
                     .Select(x => x.Value.ToLowerInvariant())
                     .ToList();

            switch (database)
            {
                case "mssql":

                    using (var connection = new SqlConnection(connectionString))
                    {
                        try
                        {
                            await connection.OpenAsync();
                            using (var command = new SqlCommand(query, connection))
                            {
                                foreach (var column in filteredColumns)
                                {
                                    string columnName = column.Name;
                                    if (column.Name == "uid")
                                    {
                                        columnName = "l_uid";
                                    }

                                    if (!paramNamesInQuery.Contains($"@{columnName.ToLowerInvariant()}"))
                                    {
                                        continue;
                                    }

                                    object columnValue = (processedData[column.Name] as JValue)?.Value;
                                    command.Parameters.AddWithValue($"@{columnName}", columnValue);
                                }
                                var result = (int)await command.ExecuteScalarAsync() > 0;
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHandler.WriteErrorLog(ex);
                            return null;
                        }
                    }

                case "mysql":

                    using (var connection = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            await connection.OpenAsync();
                            using (var command = new MySqlCommand(query, connection))
                            {
                                foreach (var column in filteredColumns)
                                {
                                    string columnName = column.Name;
                                    if (column.Name == "uid")
                                    {
                                        columnName = "l_uid";
                                    }

                                    if (!paramNamesInQuery.Contains($"@{columnName.ToLowerInvariant()}"))
                                    {
                                        continue;
                                    }

                                    object columnValue = (processedData[column.Name] as JValue)?.Value;
                                    command.Parameters.AddWithValue($"@{columnName}", columnValue);
                                }
                                var result = (int)await command.ExecuteScalarAsync() > 0;
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHandler.WriteErrorLog(ex);
                            return null;
                        }
                    }

                case "oracle":

                    using (var connection = new OracleConnection(connectionString))
                    {
                        try
                        {
                            //query = $"SELECT Count(*) FROM C##SA.attendance_logs WHERE project_id = 1 AND person_identifier = 2 AND log_time <= '27-DEC-2023 10:25:15' AND log_time >= '27-NOV-2023 10:25:15'";
                            await connection.OpenAsync();
                            using (var command = new OracleCommand(query, connection))
                            {
                                foreach (var column in filteredColumns)
                                {
                                    string columnName = column.Name;
                                    if (column.Name == "uid")
                                    {
                                        columnName = "l_uid";
                                    }

                                    if (!paramNamesInQuery.Contains($":{columnName.ToLowerInvariant()}"))
                                    {
                                        continue;
                                    }

                                    object columnValue = (processedData[column.Name] as JValue)?.Value;
                                    var param = new OracleParameter($":{column.Name}", columnValue);
                                    param = new OracleParameter($":{columnName}", columnValue);
                                    command.Parameters.Add(param);
                                }

                                var count = await command.ExecuteScalarAsync();
                                LogHandler.WriteDebugLog("Select: " + query);
                                LogHandler.WriteDebugLog("Count: " + count.ToString());
                                var result = (decimal)count > 0;
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHandler.WriteErrorLog(ex);
                            return null;
                        }
                    }

                default:
                    LogHandler.WriteLog("Unknown database!");
                    return null;
            }
        }
    }
}
