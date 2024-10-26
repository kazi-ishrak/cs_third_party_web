using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cs_third_party_web.Models
{
    public class HrmResponse
    {
        public class HRMLoginResponse
        {
            public string message { get; set; }
            public string token { get; set; }
            public string refreshToken { get; set; }
            public int userId { get; set; }
            public int officeId { get; set; }
        }
        public class HRMAttendanceLogResponse
        {
            public string message { get; set; }
            public int total { get; set; }
            public int totalPages { get; set; }
            public int currentPage { get; set; }
            public List<HRMAttendanceLogResponseData> reportResponseList { get; set; }
            public class HRMAttendanceLogResponseData
            {
                public long employeeId { get; set; }
                public string employeeOfficeId { get; set; }
                public string employeeName { get; set; }
                public string departmentName { get; set; }
                public string designationName { get; set; }
                public string rfid { get; set; }
                public HRMAttendanceLogResponseDataSingleInOut singleInOutResponse { get; set; }
                public long startOfDay { get; set; }
                public string location { get; set; }
                public class HRMAttendanceLogResponseDataSingleInOut
                {
                    public long time { get; set; }
                    public string deviceIdentifier { get; set; }
                    public int punchType { get; set; }
                    public string inOutText { get; set; }
                }
            }
        }
        public class HRMEmployeeListResponse
        {
            public string message { get; set; }
            public int total { get; set; }
            public int totalPages { get; set; }
            public List<HRMEmployeeListResponseData> employees { get; set; }
            public class HRMEmployeeListResponseData
            {
                public int id { get; set; }
                public int centralServerId { get; set; }
                public string employeeOfficeId { get; set; }
                public string name { get; set; }
            }
        }
        public class HRMEmployeeIdentifierOfficeIdMapResponse
        {
            public string message { get; set; }
            public Dictionary<string, HRMEmployeeIdentifierOfficeIdMapResponseData> employeeIdentifierOfficeIdMap { get; set; }
            public class HRMEmployeeIdentifierOfficeIdMapResponseData
            {
                public string employeeOfficeId { get; set; }
                public string employeeName { get; set; }
            }
        }
    }
}
