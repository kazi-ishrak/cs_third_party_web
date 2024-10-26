using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs_third_party_web.Models
{
    public class LocalDb
    {
        [Table(name: "attendance_logs")]
        public class AttendanceLog
        {
            [Key]
            public long id { get; set; }
            public string? uid { get; set; }
            public string? project_id { get; set; }
            public string? device_identifier { get; set; }
            public int? person_id { get; set; }
            public string? person_identifier { get; set; }
            public string? person_name { get; set; }
            public string? type { get; set; }
            public string? location { get; set; }
            public string? rfid { get; set; }
            public string? primary_display_text { get; set; }
            public string? secondary_display_text { get; set; }
            public bool? flag { get; set; }
            public DateTime log_time { get; set; }
            public DateTime? sync_time { get; set; }
            public DateTime? created_at { get; set; }
            public string? remarks { get; set; }
            public DateTime? client_sync_time { get; set; }
        }

        [Table(name: "map_cs_hrm_employees")]
        public class MapCsHrmEmployee
        {
            [Key]
            public long id { get; set; }
            public string project_id { get; set; }
            public int hrm_id { get; set; }
            public string? hrm_office_id { get; set; }
            public int cs_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }

        [Table(name: "projects")]
        public class Project
        {
            [Key]
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
        }

        [Table(name: "project_types")]
        public class ProjectType
        {
            public long id { get; set; }
            public string name { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }

        [Table(name: "attendance_log_requests")]
        public class AttendanceLogRequest
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
        }
    }

    public class CsDb
    {
        [Table(name: "attendance_logs")]
        public class CsAttendanceLog
        {
            [Key]
            public long id { get; set; }
            public string uid { get; set; }
            public int project_id { get; set; }
            public int device_id { get; set; }
            public int person_id { get; set; }
            public DateTime logged_time { get; set; }
        }

        [Table(name: "people")]
        public class CsEmployee
        {
            [Key]
            public int id { get; set; }
            public string? id_in_device { get; set; }
            public int project_id { get; set; }
            public string? name { get; set; }
            public string? photo_url { get; set; }
            public string? old_identifier { get; set; }
            public string identifier { get; set; }
            public string rfid { get; set; }
            public string primary_display_text { get; set; }
            public string secondary_display_text { get; set; }
            public string description { get; set; }
            public string? person_type { get; set; }
            public string? nid { get; set; }
            public string? from_module { get; set; }
            public DateTime? deleted_at { get; set; }
            public DateTime? created_at { get; set; }
            public DateTime? updated_at { get; set; }

        }
    }
}
