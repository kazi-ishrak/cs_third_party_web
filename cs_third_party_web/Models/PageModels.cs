using System.ComponentModel.DataAnnotations;

namespace cs_third_party_web.Models
{
    public class PageModels
    {
        public class MappedEmployeeProjectDTO
        {
            public long id { get; set; }
            public string project_id { get; set; }
            public int hrm_id { get; set; }
            public string? hrm_office_id { get; set; }
            public int cs_id { get; set; }
            public string? cs_identifier { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public string project_name { get; set; }
        }

        public class DashboardDTO
        {
            public string project_id { get; set; }
            public string project_name { get; set; }
            public string project_type { get; set; }
            public long type_id { get; set; }
            public DateTime? client_sync_time { get; set; }
            public long logs_entered_today { get; set; }
            public long? logs_synced_today { get; set; }
        }
        public class AttendanceLogWithProjectNameDTO
        {

            public long id { get; set; }

            public string? project_id { get; set; }
            public string? device_identifier { get; set; }
            public int? person_id { get; set; }
            public string? person_identifier { get; set; }
            public bool? flag { get; set; }
            public DateTime log_time { get; set; }
            public DateTime? sync_time { get; set; }
            public DateTime? created_at { get; set; }
            public string? remarks { get; set; }
            public DateTime? client_sync_time { get; set; }
            public string? project_name { get; set; }
            public string? hrm_office_id { get; set; }
            public bool hrm_identifier_enabled { get; set; }
        }
        public class ProjectDropDownModel
        {
            public string Project_id { get; set; } = string.Empty;
            public string Project_name { get; set; } = string.Empty;
        }
        public class ProjectListWithTypeNameDTO
        {
            public long id { get; set; }
            public string project_id { get; set; }
            public string? code { get; set; }
            public string? name { get; set; }
            public string? organization { get; set; }
            public string api_token { get; set; }
            public bool hrm_identifier { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public long type_id { get; set; }
            public string type_name { get; set; }
        }
        public class AttendanceLogRequestWithPrjectNameDTO
        {
            public long id { get; set; }
            public string? project_id { get; set; }
            public string? code { get; set; }
            public string? message { get; set; }
            public DateTime start_date { get; set; }
            public DateTime end_date { get; set; }
            public string? criteria { get; set; }
            public int? per_page { get; set; }
            public int? page { get; set; }
            public string? order_key { get; set; }
            public string? order_direction { get; set; }
            public string api_token { get; set; }
            public DateTime created_at { get; set; }
            public string? project_name { get; set; }
        }
    }
}
