using cs_third_party_web.Handler;
using cs_third_party_web.Models;
using cs_third_party_web.Services;
using Newtonsoft.Json.Linq;
using System.Data;
using static cs_third_party_web.Models.LocalDb;

namespace cs_third_party_web.Job
{
    public class EmployeeListPullJob
    {
        private readonly string ENDPOINT_HRM_SIGNIN = "inovace-client/api/v1/auth/external-sync/sign-in";
        private readonly string ENDPOINT_HRM_EMPLOYEE_LIST = "inovace-client/api/v1/employee";
        private readonly string ENDPOINT_CS_PEOPLE_LIST = "api/v1/people";
        private readonly IConfiguration _config;
        private readonly ApiHandler _api;
        private readonly IServiceProvider _serviceProvider;
        private DbHandler _dbHandler;

        public EmployeeListPullJob(IConfiguration config, ApiHandler api, IServiceProvider serviceProvider)
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
            JArray jArray = (JArray)jsonObject["EmployeePull"]["services"];
            List<EmployeePullSettings.Service> services = JArrayToList<EmployeePullSettings.Service>(jArray);

            foreach (var service in services.Where(x => x.status == true))
            {
                switch (service.code)
                {
                    case "0":
                        await InitiateHrmService(service);
                        //await InitiateCsService(service);
                        continue;

                    default:
                        continue;
                }
            }
        }
        public List<EmployeePullSettings.Service> JArrayToList<T>(JArray jsonArray)
        {
            List<EmployeePullSettings.Service> objectList = jsonArray.Select(item => item.ToObject<EmployeePullSettings.Service>()).ToList();
            return objectList;
        }

        #region HRM Service
        public async Task InitiateHrmService(EmployeePullSettings.Service service)
        {
            if (service.Projects == null)
            {
                return;
            }

            foreach (var project in service.Projects)
            {
                string? jwtToken = await _api.GetJwtTokenFromHrm($"{service.hrm_base_url}/{ENDPOINT_HRM_SIGNIN}", project.email, project.password);

                if (String.IsNullOrWhiteSpace(jwtToken))
                {
                    continue;
                }

                await GetEmployeesFromHrm(project, service, jwtToken);

            }
        }
        public async Task GetEmployeesFromHrm(EmployeePullSettings.Project project, EmployeePullSettings.Service service, string jwtToken)
        {
            string url = BuildUrlForHrm(service.hrm_base_url, service.per_page, 0);

            var employeeListResponse = await _api.GetEmployeesFromHrm(url, jwtToken);
            if (employeeListResponse == null || employeeListResponse.totalPages < 2 || employeeListResponse.total < 1 || employeeListResponse.employees == null || employeeListResponse.employees.Count() < 1)
            {
                return;
            }

            bool storeStatus = await StoreHrmDataToServer(project.project_id, employeeListResponse.employees);

            if (!storeStatus)
            {
                return;
            }

            for (int i = 1; i <= employeeListResponse.totalPages; i++)
            {
                url = BuildUrlForHrm(service.hrm_base_url, service.per_page, i);
                employeeListResponse = new HrmResponse.HRMEmployeeListResponse();
                employeeListResponse = await _api.GetEmployeesFromHrm(url, jwtToken);
                if (employeeListResponse == null || employeeListResponse.employees == null || employeeListResponse.employees.Count() < 1)
                {
                    break;
                }

                storeStatus = await StoreHrmDataToServer(project.project_id, employeeListResponse.employees);
                if (!storeStatus)
                {
                    break;
                }
            }
        }
        public string BuildUrlForHrm(string baseUrl, string perPage, int pageNumber)
        {
            string url = $"{baseUrl}/{ENDPOINT_HRM_EMPLOYEE_LIST}?perPage={perPage}&pageNumber={pageNumber}&status=1&status=0";
            return url;
        }
        public async Task<bool> StoreHrmDataToServer(string projectId, List<HrmResponse.HRMEmployeeListResponse.HRMEmployeeListResponseData> employees)
        {
            foreach (var employeeData in employees)
            {
                try
                {
                    var employee = await _dbHandler.GetEmployeeByHrmId(projectId, employeeData.id);
                    if (employee == null)
                    {
                        var newEmployee = new MapCsHrmEmployee
                        {
                            project_id = projectId,
                            hrm_id = employeeData.id,
                            hrm_office_id = employeeData.employeeOfficeId,
                            cs_id = employeeData.centralServerId,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };

                        var result = await _dbHandler.CreateMappingForCsAndHrmEmployee(newEmployee);
                        if (!result)
                        {
                            LogHandler.WriteDebugLog($"MappingProjectEmployee creation error for hrm_id = {newEmployee.hrm_office_id}!");
                        }

                        continue;
                    }

                    var matchData = MatchEmployeeOfficeId(employee, employeeData);

                    if (!matchData)
                    {
                        employee.hrm_office_id = employeeData.employeeOfficeId;
                        await _dbHandler.UpdateMappingForCsAndHrmEmployee(employee);
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
        private bool MatchEmployeeOfficeId(MapCsHrmEmployee employee, HrmResponse.HRMEmployeeListResponse.HRMEmployeeListResponseData employeeData)
        {
            return employee.hrm_office_id == employeeData.employeeOfficeId;
        }
        #endregion

        #region CS Service
        //public async Task InitiateCsService(EmployeePullSettings.Service service)
        //{
        //    if (service.Projects == null)
        //    {
        //        return;
        //    }

        //    foreach (var project in service.Projects)
        //    {
        //        await GetEmployeesFromCs(project, service);
        //    }
        //}

        //public async Task GetEmployeesFromCs(EmployeePullSettings.Project project, EmployeePullSettings.Service service)
        //{
        //    string url = BuildUrlForCs(service.cs_base_url, project.token);

        //    var employeeListResponse = await _api.GetEmployeesFromCs(url);
        //    if (employeeListResponse == null)
        //    {
        //        return;
        //    }

        //    bool storeStatus = await StoreCsDataToServer(project.project_id, employeeListResponse);

        //    if (!storeStatus)
        //    {
        //        return;
        //    }
        //}

        //public string BuildUrlForCs(string baseUrl, string api_token)
        //{
        //    string url = $"{baseUrl}/{ENDPOINT_CS_PEOPLE_LIST}?api_token={api_token}";
        //    return url;
        //}

        //public async Task<bool> StoreCsDataToServer(string projectId, List<CsResponse.CSEmployeeListResponse> employees)
        //{
        //    //await _dbHandler.UpdateCsEmployeeIdentifierByHrmIdentifier(employees);

        //    foreach (var employeeData in employees)
        //    {
        //        try
        //        {
        //            var employee = await _dbHandler.GetEmployeeByCsId(employeeData.id);
        //            if (employee == null)
        //            {
        //                continue;
        //            }

        //            var matchData = MatchPeopleIdentifier(employee, employeeData);

        //            if (!matchData)
        //            {
        //                employee.cs_identifier = employeeData.identifier;
        //                await _dbHandler.UpdateMappingForCsAndHrmEmployee(employee);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            LogHandler.WriteErrorLog(ex);
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        //private bool MatchPeopleIdentifier(MapCsHrmEmployee employee, CsResponse.CSEmployeeListResponse employeeData)
        //{
        //    return employee.cs_identifier == employeeData.identifier;
        //}
        #endregion
    }
}
