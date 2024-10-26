using cs_third_party_web.Handler;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using static cs_third_party_web.Models.PageModels;
namespace cs_third_party_web.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CsHrmMapController : ControllerBase
    {
        private readonly ApiControllerHelper _apicontrollerHelper;
        private readonly DbHandler _dbHandler;
        public CsHrmMapController(ApiControllerHelper apicontrollerHelper, DbHandler dbHandler)
        {
            _apicontrollerHelper = apicontrollerHelper;
            _dbHandler = dbHandler;
        }

        [HttpPost("csHrmMap")]
        public async Task<IActionResult> GetProjectList()
        {

          
            string draw = Request.Form["draw"];
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            string search = Request.Form["search[value]"];
            string sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"] + "][data]"];
            string sortDirection = Request.Form["order[0][dir]"];
            string ProjectId = Request.Form["ProjectId"];


            var Data = await _dbHandler.GetMappedEmployeesWithProjectName(ProjectId);

            int recordsTotal = Data.Count;

            //Search
            if (!string.IsNullOrEmpty(search))
            {
                Data = Data.Where(x => x.project_id.ToLower().Contains(search.ToLower()) ||
                (x.hrm_office_id != null && x.hrm_office_id.ToLower().Contains(search.ToLower())) ||
                (x.cs_identifier != null && x.cs_identifier.ToLower().Contains(search.ToLower())) ||
                (x.project_id != null && x.project_id.ToLower().Contains(search.ToLower()))
                ).ToList();
            }

            int recordsFiltered = Data.Count;

            //Sorting
            if (!string.IsNullOrEmpty(sortColumn))
            {
                Data = Data.AsQueryable().OrderBy(sortColumn + " " + sortDirection).ToList();
            }

            //Paging
            Data = Data.Skip(start).Take(length).ToList();


            return Ok(new { draw, recordsTotal, recordsFiltered, data = Data });



        }
    }
}
