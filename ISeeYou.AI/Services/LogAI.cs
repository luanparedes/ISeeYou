using ISeeYou.Core.Services;
using Serilog;
using System.Diagnostics;

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
            string file = SetLogPath();

            _appLog = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(file, rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
        }

        private static string SetLogPath()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                string fileNameFormat = $"log_iseeyou.ai-.txt";

                string logPath = "C:\\ISeeYou.AI.Logs";
                string logFile = Path.Combine(logPath, "logs", fileNameFormat);

                string logDirectory = Path.GetDirectoryName(logFile);

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                return logFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return string.Empty;
        }
    }
}
