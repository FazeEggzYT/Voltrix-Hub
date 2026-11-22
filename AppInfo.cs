namespace Voltrix
{
    static class AppInfo
    {
        public const string Name = "VOLTRIX Hub";
        public const string Version = "0.9.1";
        public const string Channel = "pre-release";

        public static string Full
        {
            get { return Version + " " + Channel; }
        }
    }
}
