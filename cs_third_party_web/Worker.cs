using cs_third_party_web.Methods;
using cs_third_party_web.Services;
using cs_third_party_web.Job;
using System.Timers;
using cs_third_party_web.Models;

namespace cs_third_party_web
{
    public class Worker : BackgroundService
    {
        public IConfiguration _config;
        public AttendanceLogPullJob _log_pull_job;
        System.Timers.Timer timer_log_pull = new System.Timers.Timer();

        public AttendanceLogPushJob _log_push_job;
        System.Timers.Timer timer_log_push = new System.Timers.Timer();

        public EmployeeListPullJob _employee_pull_job;
        System.Timers.Timer timer_employee_pull = new System.Timers.Timer();

     
        public Worker(IConfiguration config, AttendanceLogPullJob log_pull_job, AttendanceLogPushJob log_push_job, EmployeeListPullJob employee_pull_job)
        {
            _config = config;
            _log_pull_job = log_pull_job;
            _log_push_job = log_push_job;
            _employee_pull_job = employee_pull_job;

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                LogHandler.WriteDebugLog("");
                LogHandler.WriteDebugLog("Service Started!");

                var logPullSettings = _config.GetSection("LogPull").Get<LogPullSettings>();
                if (logPullSettings.status)
                {
                    timer_log_pull.Interval = _config.GetSection("LogPull:timer_interval").Get<double>();
                    timer_log_pull.Elapsed += TimerElapsed_log_pull;
                    timer_log_pull.AutoReset = true;
                    timer_log_pull.Start();
                }

                var logPushSettings = _config.GetSection("LogPush").Get<LogPushSettings>();
                if (logPushSettings.status)
                {
                    timer_log_push.Interval = _config.GetSection("LogPush:timer_interval").Get<double>();
                    timer_log_push.Elapsed += TimerElapsed_log_push;
                    timer_log_push.AutoReset = true;
                    timer_log_push.Start();
                }

                var employeePullSettings = _config.GetSection("EmployeePull").Get<EmployeePullSettings>();
                if (employeePullSettings.status)
                {
                    timer_employee_pull.Interval = _config.GetSection("EmployeePull:timer_interval").Get<double>();
                    timer_employee_pull.Elapsed += TimerElapsed_employee_pull;
                    timer_employee_pull.AutoReset = true;
                    timer_employee_pull.Start();
                }
            }
            catch (Exception ex)
            {
                LogHandler.WriteErrorLog(ex);
            }
        }

        private async void TimerElapsed_log_pull(object? sender, ElapsedEventArgs e)
        {
            timer_log_pull.Stop();
            await _log_pull_job.StartProcess();
            timer_log_pull.Start();
        }

        private async void TimerElapsed_log_push(object? sender, ElapsedEventArgs e)
        {
            timer_log_push.Stop();
            await _log_push_job.StartProcess();
            timer_log_push.Start();
        }
        private async void TimerElapsed_employee_pull(object? sender, ElapsedEventArgs e)
        {
            timer_employee_pull.Stop();
            await _employee_pull_job.StartProcess();
            timer_employee_pull.Start();
        }
    }
}