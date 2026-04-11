using ISeeYou.Core.Services;

namespace ISeeYou.Services
{
    class AppInfo : IAppInfo
    {
        #region Constants

        private const string AppName = "I See You";
        private const string AppVersion = "1.0.0";

        #endregion

        public string GetAppFullNameVersion()
        {
            return $"{GetAppName()} v{GetAppVersion}";
        }

        public string GetAppName()
        {
            return AppName;
        }

        public string GetAppVersion()
        {
            return AppVersion;
        }
    }
}
