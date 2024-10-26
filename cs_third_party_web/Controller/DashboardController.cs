using cs_third_party_web.Handler;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
namespace cs_third_party_web.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DbHandler _dbHandler;
        public DashboardController(DbHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        [HttpGet("cards")]
        public async Task<IActionResult> GetDashboardInfoCards()
        {
            var Infos = await _dbHandler.GetDashboardInfoCards();
            return Ok(Infos);
        }

        [HttpPost("table")]
        public async Task<IActionResult> GetDashboardTable()
        {
            string draw = Request.Form["draw"];
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            string search = Request.Form["search[value]"];
            string sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"] + "][data]"];
            string sortDirection = Request.Form["order[0][dir]"];

            var data = await _dbHandler.GetDashboardTable();
            int recordsTotal = data.Count;

            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(x => x.project_id.ToLower().Contains(search.ToLower()) ||
                (x.project_name != null && x.project_name.ToLower().Contains(search.ToLower()))
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
