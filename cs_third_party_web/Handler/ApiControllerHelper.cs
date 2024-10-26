using cs_third_party_web.Handler;
using cs_third_party_web.Services;
using static cs_third_party_web.Models.LocalDb;
using static CS_Third_Party_Api.Controller.ApiController;
using System.Linq.Dynamic.Core;
using Project = cs_third_party_web.Models.LocalDb.Project;
using static cs_third_party_web.Models.EmployeePullSettings;
using static cs_third_party_web.Models.CsResponse.CSAttendanceLogResponse;

namespace cs_third_party_web.Handler
{
    public class ApiControllerHelper
    {
        private readonly IServiceProvider _serviceProvider;
        private DbHandler _dbHandler;
        public ApiControllerHelper(IServiceProvider serviceProvider)
        {
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

        public async Task<List<ModAttendanceLogResponse.ModAttendanceLogPayload>> AddPersonOfficeId(string project_id, List<ModAttendanceLogResponse.ModAttendanceLogPayload> logs)
        {
            try
            {
                Dictionary<int, string> person_office_Ids = new Dictionary<int, string>();

                foreach (var log in logs)
                {
                    if (log.person_id == null)
                    {
                        continue;
                    }

                    if (!person_office_Ids.ContainsKey(log.person_id.Value))
                    {
                        var map = await _dbHandler.GetMappingForCsAndHrmEmployee(project_id, log.person_id);
                        if (map == null)
                        {
                            LogHandler.WriteLog($"person_id: {log.person_id} not found with project_id: {project_id} in database or hrm_office_id is null");
                            continue;
                        }

                        person_office_Ids[log.person_id.Value] = map.hrm_office_id;
                    }

                    log.person_office_id = person_office_Ids[log.person_id.Value];
                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return logs;
        }

        public int GetTake(int? per_page)
        {
            int take = 500;

            if (per_page != null && per_page > 0)
            {
                take = per_page.Value;
            }

            return take;
        }
        public int GetSkip(int? page, int take)
        {
            int skip = 0;

            if (page != null && page > 0)
            {
                skip = (page.Value - 1) * take;
            }

            return skip;
        }
        public string GetOrderKey(string? order_key)
        {
            string o_key = "sync_time";

            if (order_key == "logged_time")
            {
                o_key = "log_time";
            }

            return o_key;
        }
        public string GetOrderDirection(string? order_direction)
        {
            string o_direction = "asc";

            if (order_direction == "desc")
            {
                o_direction = order_direction;
            }

            return o_direction;
        }
       
        public async Task<List<AttendanceLog>?> GetAllLogs(DateTime start, DateTime end, string? criteria, string project_id)
        {
            List<AttendanceLog>? all_logs = new List<AttendanceLog>();

            if (criteria == "logged_time")
            {
                all_logs = await _dbHandler.GetAttendanceLogByLogTime(start, end, project_id);
            }
            else
            {
                all_logs = await _dbHandler.GetAttendanceLogBySyncTime(start, end, project_id);
            }

            return all_logs;
        }
        public async Task<List<ModAttendanceLogResponse.ModAttendanceLogPayload>> GetFilteredLogs(Project project, List<AttendanceLog> filtered_logs)
        {
            List<ModAttendanceLogResponse.ModAttendanceLogPayload> response_logs = new List<ModAttendanceLogResponse.ModAttendanceLogPayload>();
            
            response_logs = filtered_logs
                .Select(x => new ModAttendanceLogResponse.ModAttendanceLogPayload
                {
                    uid = x.uid,
                    device_identifier = x.device_identifier,
                    person_id = x.person_id,
                    person_identifier = x.person_identifier,
                    logged_time = x.log_time.ToString("yyyy-MM-dd HH:mm:ss"),
                    sync_time = x.sync_time != null ? x.sync_time.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                    type = x.type,
                    rfid = x.rfid,
                    location = x.location,
                    primary_display_text = x.primary_display_text,
                    secondary_display_text = x.secondary_display_text
                }).ToList();

            if (project.hrm_identifier)
            {
                response_logs = await AddPersonOfficeId(project.project_id, response_logs);
            }

            return response_logs;
        }
        public ModAttendanceLogResponse BuildResponse(int take, int skip, Project project, List<AttendanceLog> all_logs, List<ModAttendanceLogResponse.ModAttendanceLogPayload> filtered_logs)
        {
            ModAttendanceLogResponse response = new ModAttendanceLogResponse();
            CSAttendanceLogMeta meta = new CSAttendanceLogMeta();
            CSAttendanceLogLinks links = new CSAttendanceLogLinks();
            CSAttendanceLogProject proj = new CSAttendanceLogProject();

            try
            {
                response.data = filtered_logs;

                meta.total = all_logs.Count();
                meta.from = skip + 1;
                meta.to = skip + filtered_logs.Count();
                meta.per_page = take;
                meta.current_page = (skip / take) + 1;
                double last_page_decimal = Math.Ceiling((double)all_logs.Count() / take);
                meta.last_page = (int)last_page_decimal;
                response.meta = meta;

                response.links = links;

                proj.code = project.code;
                proj.name = project.name;
                proj.organization = project.organization;
                response.project = proj;
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return response;
        }
    }
}
