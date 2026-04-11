using ISeeYou.Core.Services;
using Serilog;
using System.Diagnostics;
using static System.Net.WebRequestMethods;

namespace ISeeYou.AI.Services
{
    internal class LogAI : ILogService
    {
        private ILogger _appLog;

        public LogAI()
        {
            LoggerConfiguration();
        }

        public void Error(string message)
        {
            _appLog.Error(message);
        }

        public void Info(string message)
        {
            _appLog.Information(message);
        }

        public void Warn(string message)
        {
            _appLog.Warning(message);
        }

        private void LoggerConfiguration()
        {
            string file = GetLogPath();

            _appLog = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(file, rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
        }

        private static string GetLogPath()
        {
            try
            {
                string fileNameFormat = $"log_iseeyou.ai-.txt";

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string path = Path.Combine(localAppData, "Packages", "SunnymoonSoftware.ISeeYouSecurity_ejck4kpr8c0yt", "LocalState", "logs");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, fileNameFormat);
                return filePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return string.Empty;
        }
    }
}
