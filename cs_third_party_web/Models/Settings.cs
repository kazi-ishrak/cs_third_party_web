
using Newtonsoft.Json.Linq;

namespace cs_third_party_web.Models
{
    public class EmployeePullSettings
    {
        public bool status { get; set; }
        public int time_interval { get; set; }
        public List<Service> services { get; set; }
        public class Service
        {
            public string name { get; set; }
            public string code { get; set; }
            public bool status { get; set; }
            public string hrm_base_url { get; set; }
            public string cs_base_url { get; set; }
            public string per_page { get; set; }
            public List<Project>? Projects { get; set; }
            public string table_name { get; set; }
            public JObject table_columns { get; set; }
        }
        public class Project
        {
            public string project_id { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public string password { get; set; }
        }
    }
    public class LogPullSettings
    {
        public bool status { get; set; }
        public int time_interval { get; set; }
        public List<Service> services { get; set; }
        public class Service
        {
            public bool status { get; set; }
            public string name { get; set; }
            public string code { get; set; }
            public string base_url { get; set; }
            public string per_page { get; set; }
            public List<Project>? projects { get; set; }
            public class Project
            {
                public int log_optimization_time { get; set; }
                public bool? runtime_hrm_identifier { get; set; }
                public string project_id { get; set; }
                public string? name { get; set; }
                public string? token { get; set; }
                public string? auth_user { get; set; }
                public string? auth_code { get; set; }
                public string? operation { get; set; }
                public string? access_id { get; set; }
            }
            public string table_name { get; set; }
            public bool? stored_procedure_status { get; set; }
            public string? stored_procedure { get; set; }
            public JObject table_columns { get; set; }
            public bool uid_duplication_check { get; set; }

        }
    }
    public class LogPushSettings
    {
        public bool status { get; set; }
        public int time_interval { get; set; }
        public List<Client> clients { get; set; }
        public class Client
        {
            public int type { get; set; }
            public bool status { get; set; }
            public bool hrm_identifier { get; set; }
            public int per_page { get; set; }
            public int log_optimization_time { get; set; }
            public string name { get; set; }
            public List<string> project_ids { get; set; }
            public string? auth_key { get; set; }
            public string? api_auth { get; set; }
            public string? clientId { get; set; }
            public string? clientSecret { get; set; }
            public string api_log_push { get; set; }
            public string? database_connection {  get; set; }
            public string? table_name { get; set; }
            public JObject table_columns { get; set; }
        }
    }
}
