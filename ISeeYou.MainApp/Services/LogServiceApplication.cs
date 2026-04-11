using ISeeYou.Core.Services;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace ISeeYou.Services
{
    class LogServiceApplication : ILogService
    {
        private ILogger _appLog;

        public LogServiceApplication()
        {
            LoggerConfiguration();
        }

        public void Info(string message)
        {
            _appLog.Information(message);
        }

        public void Warn(string message)
        {
            _appLog.Warning(message);
        }

        public void Error(string message)
        {
            _appLog.Error(message);
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
                string fileNameFormat = $"log_iseeyou-.txt";

                string logPath = ApplicationData.Current.LocalFolder.Path;
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
