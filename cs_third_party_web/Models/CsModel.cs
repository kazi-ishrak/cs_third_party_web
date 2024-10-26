
namespace cs_third_party_web.Models
{
    public class CsResponse {
        public class CSAttendanceLogResponse
        {
            public List<CSAttendanceLogPayload>? data { get; set; }
            public class CSAttendanceLogPayload
            {
                public string? uid { get; set; }
                public string? sync_time { get; set; }
                public string? logged_time { get; set; }
                public string? type { get; set; }
                public string? device_identifier { get; set; }
                public string? location { get; set; }
                public int? person_id { get; set; }
                public string? person_identifier { get; set; }
                public string? rfid { get; set; }
                public string? primary_display_text { get; set; }
                public string? secondary_display_text { get; set; }
            }

            public CSAttendanceLogMeta? meta { get; set; }
            public class CSAttendanceLogMeta
            {
                public int? current_page { get; set; }
                public int? from { get; set; }
                public int? last_page { get; set; }
                public int? per_page { get; set; }
                public int? to { get; set; }
                public int? total { get; set; }
            }

            public CSAttendanceLogLinks? links { get; set; }
            public class CSAttendanceLogLinks
            {
                public string? first { get; set; }
                public string? last { get; set; }
                public string? prev { get; set; }
                public string? next { get; set; }
            }

            public CSAttendanceLogProject? project { get; set; }
            public class CSAttendanceLogProject
            {
                public string? code { get; set; }
                public string? name { get; set; }
                public string? organization { get; set; }
            }
        }
        public class CSEmployeeListResponse
        {
            public int id { get; set; }
            public string identifier { get; set; }
        }
    }
}
