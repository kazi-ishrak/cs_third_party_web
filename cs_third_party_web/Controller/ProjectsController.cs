using Azure;
using cs_third_party_web.Handler;
using Microsoft.AspNetCore.Mvc;
using static cs_third_party_web.Models.LocalDb;
using cs_third_party_web.Services;
using System.Linq.Dynamic.Core;


namespace CS_Third_Party_Api.Controller
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {

        private readonly ApiControllerHelper _apicontrollerHelper;
        private readonly DbHandler _dbHandler;
        public ProjectsController(ApiControllerHelper apicontrollerHelper, DbHandler dbHandler)
        {
            _apicontrollerHelper = apicontrollerHelper;
            _dbHandler = dbHandler;
        }

        [HttpPost("projectList")]
        public async Task<IActionResult> GetProjectList()
        {

            string draw = Request.Form["draw"];
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            string search = Request.Form["search[value]"];
            string sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"] + "][data]"];
            string sortDirection = Request.Form["order[0][dir]"];

            var data = await _dbHandler.GetAllProjectsListFromDB();

            int recordsTotal = data.Count;

            if (!string.IsNullOrEmpty(search))
            {
                data = data.Where(x => x.project_id.ToLower().Contains(search.ToLower()) ||
                (x.name != null && x.name.ToLower().Contains(search.ToLower())) ||
                (x.organization != null && x.organization.ToLower().Contains(search.ToLower())) ||
                (x.code != null && x.code.ToLower().Contains(search.ToLower())) ||
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

        [HttpPost("update")]
        public async Task<IActionResult> UpdateExistingProject([FromBody] Project Data)
        {
            if (Data == null)
                return BadRequest("Empty Request Body");

            try
            {
                await _dbHandler.UpdateExistingProjectIntoDb(Data);
                return Ok("Updated Successfully");
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
                return BadRequest();
            }

        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectById(long id)
        {
            try
            {
                var project = await _dbHandler.GetProjectById(id);
                return Ok(project);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return NotFound();
            }
        }

        [HttpGet("type")]
        public async Task<IActionResult> GetProjectType()
        {
            try
            {
                var TypeList = await _dbHandler.GetProjectType();
                return Ok(TypeList);
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
                return NoContent();
            }
        }

        [HttpGet("idAndName")]
        public async Task<IActionResult> GetProjectIdAndName()
        {
            try
            {
                var Projects = await _dbHandler.GetAllProjectsListFromDB();
                return Ok(Projects);
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return NotFound();
            }
            /*var projects = await _dbHandler.GetProjectIdAndNameDropdown();
            var logPullSection = _iconfiguration.GetSection("LogPull");
            var serviceSection = logPullSection.GetSection("services").GetChildren();
            var projects = new List<ProjectDropDownModel>();
            foreach (var abc in serviceSection)
            {
                var projectSection = abc.GetSection("projects").GetChildren();

                foreach (var x in projectSection)
                {
                    var projectId = x["project_id"];
                    var Name = x["name"];

                    projects.Add(new ProjectDropDownModel
                    {
                        Project_id = projectId,
                        Project_name = Name
                    });
                }
                break;
            }
            return Ok(projects);*/
        }
        [HttpPost("insert")]
        public async Task<IActionResult> InsertAProject([FromBody] Project Data)
        {

            if (Data == null)
            {
                return BadRequest("Empty Request Body");
            }
            try
            {
                await _dbHandler.InsertProjectIntoDb(Data);
                return Ok("Sucessfully inserted");
            }
            catch (Exception ex)
            {

                LogHandler.WriteErrorLog(ex);
                return BadRequest("Cant insret into Db");
            }
        }
    }
}
