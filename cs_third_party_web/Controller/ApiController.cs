using cs_third_party_web.Handler;
using cs_third_party_web.Services;
using cs_third_party_web.Data;
using static cs_third_party_web.Models.LocalDb;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using Org.BouncyCastle.Utilities;
using System.IO;
using Microsoft.EntityFrameworkCore;
using static cs_third_party_web.Models.CsResponse.CSAttendanceLogResponse;


namespace CS_Third_Party_Api.Controller
{
    [ApiController]
    [Route("api/v1")]
    public class ApiController : ControllerBase
    {
        private readonly DbHandler _dbHandler;
        private readonly ApiControllerHelper _helper;
        private readonly IConfiguration _configuration;
        public ApiController(DbHandler dbHandler, ApiControllerHelper helper, IConfiguration configuration)
        {
            _dbHandler = dbHandler;
            _helper = helper;
            _configuration = configuration;
        }
        public class ErrorResponse
        {
            public int code { get; set; }
            public string context { get; set; }
            public string message { get; set; }
        }
        public class ModAttendanceLogResponse
        {
            public List<ModAttendanceLogPayload>? data { get; set; }
            public class ModAttendanceLogPayload
            {
                public string? uid { get; set; }
                public string? sync_time { get; set; }
                public string? logged_time { get; set; }
                public string? type { get; set; }
                public string? device_identifier { get; set; }
                public string? location { get; set; }
                public int? person_id { get; set; }
                public string? person_identifier { get; set; }
                public string? person_office_id { get; set; }
                public string? rfid { get; set; }
                public string? primary_display_text { get; set; }
                public string? secondary_display_text { get; set; }
            }
            public CSAttendanceLogMeta? meta { get; set; }
            public CSAttendanceLogLinks? links { get; set; }
            public CSAttendanceLogProject? project { get; set; }

        }

        [HttpGet("logs", Name = "logs")]
        public async Task<IActionResult> GetAttendanceLogs(DateTime start, DateTime end, string? criteria, int? per_page, int? page, string? order_key, string? order_direction, string api_token)
        {
            AttendanceLogRequest LogRequest = new AttendanceLogRequest();
            LogRequest.api_token = api_token;
            LogRequest.start_date = start;
            LogRequest.end_date = end;
            LogRequest.per_page = per_page;
            LogRequest.page = page;
            LogRequest.criteria = criteria;
            LogRequest.created_at = DateTime.Now;
            ErrorResponse error = new ErrorResponse();

            if (start > end)
            {
                LogRequest.project_id = "Unknown";
                LogRequest.code = "400";
                LogRequest.message = "Bad Request";
                await _dbHandler.PushTheLogRequestToDb(LogRequest);
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(api_token))
            {
                LogRequest.code = "401";
                LogRequest.project_id = "Unknown";
                LogRequest.message = "failed to authenticate";
                await _dbHandler.PushTheLogRequestToDb(LogRequest);

                error.code = 401;
                error.context = "authentication";
                error.message = "failed to authenticate";
                return Unauthorized(error);
            }

            var project = await _dbHandler.GetProjectByApiToken(api_token);
            if (project == null)
            {
                LogRequest.project_id = "Unknown";
                LogRequest.code = "401";
                LogRequest.message = "failed to authenticate";
                await _dbHandler.PushTheLogRequestToDb(LogRequest);

                error.code = 401;
                error.context = "authentication";
                error.message = "failed to authenticate";
                return Unauthorized(error);
            }

            int take = _helper.GetTake(per_page);
            int skip = _helper.GetSkip(page, take);
            string o_key = _helper.GetOrderKey(order_key);
            string o_direction = _helper.GetOrderDirection(order_direction);

            var all_logs = await _helper.GetAllLogs(start, end, criteria, project.project_id);
            if (all_logs == null)
            {
                LogRequest.project_id = project.project_id;
                LogRequest.code = "500";
                LogRequest.message = "No Logs";
                await _dbHandler.PushTheLogRequestToDb(LogRequest);
                return StatusCode(500);
            }

            var filtered_logs = all_logs.AsQueryable().OrderBy(o_key + " " + o_direction).Skip(skip).Take(take).ToList();
            if (filtered_logs == null)
            {
                LogRequest.project_id = project.project_id;
                LogRequest.code = "500";
                LogRequest.message = "No Logs";
                await _dbHandler.PushTheLogRequestToDb(LogRequest);
                return StatusCode(500);
            }

            await _dbHandler.UpdateSyncStatusOfAttendanceLogs(filtered_logs, "success");
            var response_logs = await _helper.GetFilteredLogs(project, filtered_logs);
            var response = _helper.BuildResponse(take, skip, project, all_logs, response_logs);

            LogRequest.project_id = project.project_id;
            LogRequest.code = "200";
            LogRequest.message = "Success";
            await _dbHandler.PushTheLogRequestToDb(LogRequest);

            return Ok(response);
        }
    }
}
