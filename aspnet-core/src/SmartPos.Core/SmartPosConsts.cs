using SmartPos.Debugging;

namespace SmartPos
{
    public class SmartPosConsts
    {
        public const string LocalizationSourceName = "SmartPos";

        public const string ConnectionStringName = "Default";

        public const bool MultiTenancyEnabled = true;


        /// <summary>
        /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
        /// </summary>
        public static readonly string DefaultPassPhrase =
            DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "c17f97478b4547fb8562ff9b18e3cb40";
    }
}
