using cs_third_party_web.Handler;
using cs_third_party_web.Services;
using Newtonsoft.Json.Linq;
using static cs_third_party_web.Models.LocalDb;
using Client = cs_third_party_web.Models.LogPushSettings.Client;

namespace cs_third_party_web.Job
{
    public class AttendanceLogPushJob
    {
        private readonly IConfiguration _config;
        private readonly PrismService _prism;
        private readonly PihrService _pihr;
        private readonly CommonService _common;
        private readonly ZohoService _zohoService;
        private readonly IServiceProvider _serviceProvider;
        private readonly CsService _csService;
        private DbHandler _dbHandler;
        public AttendanceLogPushJob(CsService csService, IConfiguration config, IServiceProvider serviceProvider, PrismService prism, PihrService pihr, CommonService common, ZohoService zohoService)
        {
            _zohoService= zohoService;
            _csService= csService;
            _config = config;
            _prism = prism;
            _pihr = pihr;
            _common = common;
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
        public List<Client> JArrayToList<T>(JArray jsonArray)
        {
            List<Client> objectList = jsonArray.Select(item => item.ToObject<Client>()).ToList();
            return objectList;
        }

        public async Task StartProcess()
        {
            try
            {
                string jsonFilePath = "appsettings.json";
                string jsonText = File.ReadAllText(jsonFilePath);
                JObject jsonObject = JObject.Parse(jsonText);
                JArray jArray = (JArray)jsonObject["LogPush"]["clients"];
                List<Client> clients = JArrayToList<Client>(jArray);

                foreach (var client in clients)
                {
                    if (!client.status)
                    {
                        continue;
                    }

                    if (client.per_page < 1)
                    {
                        client.per_page = 100;
                    }

                    var logs = await _dbHandler.GetAttendanceLogByClientConfiguration(client);
                    if (logs == null || logs.Count == 0)
                    {
                        continue;
                    }

                    var filtered_logs = new List<AttendanceLog>();

                    if (client.log_optimization_time > 0)
                    {
                        filtered_logs = logs
                            .GroupBy(x => x.person_id)
                            .SelectMany(group => group.Select((log, index) => new { Log = log, Index = index }))
                            .Where(x => x.Index == 0 || (x.Index > 0 && (x.Log.log_time - logs[x.Index - 1].log_time).TotalSeconds >= (client.log_optimization_time / 1000)))
                            .Select(x => x.Log)
                            .ToList();
                    }
                    else
                    {
                        filtered_logs = logs;
                    }

                    if (filtered_logs.Count < 1)
                    {
                        continue;
                    }

                    if (filtered_logs == null || filtered_logs.Count == 0)
                    {
                        continue;
                    }

                    var status = await PushAttendanceToClient(client, filtered_logs);

                    //if (status != "success")
                    //{
                    //    _dbHandler.
                    //    continue;
                    //}

                    await _dbHandler.UpdateSyncStatusOfAttendanceLogs(logs, status);
                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
        }
        public async Task<string> PushAttendanceToClient(Client client, List<AttendanceLog> logs)
        {
            switch (client.type)
            {
                case 0:
                    return await _pihr.PushAttendanceToPihr(client, logs);

                case 1:
                    return await _prism.PushAttendanceToPrism(client, logs);

                case 2:
                    return await _zohoService.PushAttendanceToZoho(client, logs);

                case 3:
                    return await _csService.PushAttendanceToCs(client, logs);

                case 10:
                    return await _common.PushAttendanceToDatabase(client, logs);

                default:
                    return "error";
            }
        }
    }
}
