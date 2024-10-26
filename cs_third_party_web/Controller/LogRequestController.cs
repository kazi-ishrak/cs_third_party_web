using cs_third_party_web.Handler;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq.Dynamic.Core;
namespace cs_third_party_web.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class LogRequestController : ControllerBase
    {
        private readonly DbHandler _dbHandler;
        public LogRequestController(DbHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        [HttpPost("all")]
        public async Task<IActionResult> GetLogRequestDetails()
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
            var data = await _dbHandler.GetLogRequestDetails(ProjectId, StartDate, EndDate);
            int recordsTotal = data.Count;

            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(x => x.project_id.ToLower().Contains(search.ToLower()) ||
                (x.api_token != null && x.api_token.ToLower().Contains(search.ToLower()))
                ).ToList();
            }

            int recordsFiltered = data.Count;

            //Sorting
            if (!string.IsNullOrEmpty(sortColumn))
            {
                data = data.AsQueryable().OrderBy(sortColumn + " " + sortDirection).ToList();
            }
            //Paging
            data = data.Skip(start).Take(length).ToList();
            return Ok(new { draw, recordsTotal, recordsFiltered, data = data });
        }
    }
}
