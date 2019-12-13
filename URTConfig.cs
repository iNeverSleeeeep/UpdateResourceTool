using UnityEngine;

namespace URT
{
    public static class URTConfig
    {
        public static string PREF_APP_CURRENT_VERSION = "PREF_APP_CURRENT_VERSION";
        public static string PREF_APP_UPDATECACHE_VERSION = "PREF_APP_UPDATECACHE_VERSION";

        public static int MAX_BUSY_WORKERS = SystemInfo.processorCount + 2;
        public static int WEB_REQUEST_TIMEOUT = 10;

        public static string UPDATE_CACHE_PATH = Application.persistentDataPath + "/updatecache";
        public static string UPDATE_PATH = Application.persistentDataPath + "/update";

        public static string MD5_FILENAME = "md5";


#if UNITY_EDITOR
        public static double DEBUG_DOWNLOAD_FAIL = 0;
#endif
    }
}