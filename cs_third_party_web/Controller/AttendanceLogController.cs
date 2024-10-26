using cs_third_party_web.Data;
using cs_third_party_web.Handler;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq.Dynamic.Core;
using static cs_third_party_web.Models.LocalDb;

namespace CS_Third_Party_App.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceLogController : ControllerBase
    {
        private readonly ApiControllerHelper _apiControllerHelper;
        private readonly DbHandler _dbHandler;
        public AttendanceLogController(ApiControllerHelper apiControllerHelper, DbHandler dbHandler)
        {
            _apiControllerHelper = apiControllerHelper;
            _dbHandler = dbHandler;
        }
        [HttpPost]
        public async Task<IActionResult> GetAttendanceLogsFromDb()
        {
            string draw = Request.Form["draw"];
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            string search = Request.Form["search[value]"];
            string sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"] + "][data]"];
            string sortDirection = Request.Form["order[0][dir]"];
            string ProjectId = Request.Form["ProjectId"];
            var StartDate = DateTime.ParseExact(Request.Form["StartDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var EndDate = DateTime.ParseExact(Request.Form["EndDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);

            var all_logs = await _dbHandler.GetAllAttendanceLogFromDb(ProjectId, StartDate, EndDate);

            int recordsTotal = all_logs.Count;

            if (!string.IsNullOrEmpty(search))
            {
                all_logs = all_logs.Where(x => x.project_id != null && x.project_id.ToLower().Contains(search.ToLower()) ||
                (x.device_identifier != null && x.device_identifier.ToLower().Contains(search.ToLower())) ||
                 (x.project_name != null && x.project_name.ToLower().Contains(search.ToLower())) ||
                 (x.hrm_office_id != null && x.hrm_office_id.ToLower().Contains(search.ToLower())) ||
                 (x.person_identifier != null && x.person_identifier.ToLower().Contains(search.ToLower()))
                ).ToList();
            }
            int recordsFiltered = all_logs.Count;

            //Sorting
            if (!string.IsNullOrEmpty(sortColumn))
            {
                all_logs = all_logs.AsQueryable().OrderBy(sortColumn + " " + sortDirection).ToList();
            }
            //Paging
            all_logs = all_logs.Skip(start).Take(length).ToList();
            return Ok(new { draw, recordsTotal, recordsFiltered, data = all_logs });
        }

    }

}
