using cs_third_party_web.Models;
using Newtonsoft.Json.Linq;
using cs_third_party_web.Methods;
using Microsoft.EntityFrameworkCore;
using static cs_third_party_web.Models.LogPushSettings;
using static cs_third_party_web.Models.LocalDb;

namespace cs_third_party_web.Services
{
    public class CommonService
    {
        private readonly IConfiguration _config;

        public CommonService(IConfiguration config)
        {
            _config = config;
        }
        public async Task<string> PushAttendanceToDatabase(Client client_info, List<AttendanceLog> attendanceLogs)
        {
            try
            {
                JArray jsonArray = JArray.FromObject(attendanceLogs);
                string sqlScript = GenerateInsertSql(client_info.table_name, JObject.FromObject(client_info.table_columns), jsonArray);
                string? connectionString = client_info.database_connection;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    LogHandler.WriteDebugLog("Database connection string is empty");
                    return "Database connection string is empty";
                }

                using (var context = new DynamicDbContext(connectionString))
                {
                    await context.Database.ExecuteSqlRawAsync(sqlScript);
                    return "success";
                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
                return ex.Message;
            }
        }
        public static string GenerateInsertSql(string tableName, JObject columnMappings, JArray data)
        {
            if (data == null || data.Count == 0)
            {
                throw new ArgumentException("Data array is empty.");
            }

            try
            {
                var columns = columnMappings.Properties();
                var columnList = string.Join(", ", columnMappings.Properties().Select(column => column.Value.ToString()).ToList());
                var values = data.Select(item =>
                {
                    var rowValues = columns.Select(column =>
                    {
                        var value = item[column.Name]?.ToString() ?? " ";
                        return $"'{value.Replace("'", "''")}'";
                    });
                    return $"({string.Join(", ", rowValues)})";
                });
                var valuesList = string.Join(", ", values);
                return $"INSERT INTO {tableName} ({columnList}) VALUES {valuesList}";
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }

            return "";
        }
    }
}
