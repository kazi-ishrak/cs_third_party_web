namespace cs_third_party_web.Models
{
    public class RumyResponse
    {
        public List<RumyModelLogResponse?> log { get; set; }
        public class RumyModelLogResponse
        {
            public string unit_name { get; set; }
            public string registration_id { get; set; }
            public long access_id { get; set; }
            public string department { get; set; }
            public string access_time { get; set; }
            public DateOnly access_date { get; set; }
            public string user_name { get; set; }
            public string unit_id { get; set; }
            public string card { get; set; }
        }

        public class RumyLogRequest
        {
            public long access_id { get; set; }
            public string operation { get; set; }
            public string auth_code { get; set; }
            public string auth_user { get; set; }
            public DateOnly start_date { get; set; }
            public DateOnly end_date { get; set; }
            public string start_time { get; set; }
            public string end_time { get; set; }
        }
    }
}
