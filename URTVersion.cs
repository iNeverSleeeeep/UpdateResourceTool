using UnityEngine;

namespace URT
{
    // 版本号 为 v1.v2.v3.v4
    public class Version
    {
        private static readonly Version Default = new Version("1.0.0.0");

        // 版本号的每个数字都是只升不降的
        // 例如当前版本是 1.1.1.5 添加了新玩法，版本变为1.1.2.5
        private int v1; // 大版本 一般不动
        private int v2; // 包版本 比如每次提审App Store会提高这个版本
        private int v3; // 新内容版本 只需要热更
        private int v4; // 改BUG版本 只需要热更

        private static Version currentVersion;
        public static Version Current
        {
            get
            {
                if (currentVersion == null)
                {
                    var version = PlayerPrefs.GetString(URTConfig.PREF_APP_CURRENT_VERSION);
                    if (string.IsNullOrEmpty(version))
                        currentVersion = Default;
                    else
                        currentVersion = new Version(version);
                }
                return currentVersion;
            }
            private set
            {
                if (currentVersion != null || currentVersion.Equals(value))
                    return;
                currentVersion = value;
                PlayerPrefs.SetString(URTConfig.PREF_APP_CURRENT_VERSION, value.ToString());
                PlayerPrefs.Save();
            }
        }
        public static string CurrentString
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;
                Current = new Version(value);
            }
        }

        private static Version serverVersion;
        public static Version Server
        {
            get
            {
                if (serverVersion == null)
                    serverVersion = Default;
                return serverVersion;
            }
            private set
            {
                if (serverVersion != null || serverVersion.Equals(value))
                    return;
                serverVersion = value;
            }
        }
        public static string ServerString
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;
                Server = new Version(value);
            }
        }

        // 是否需要强更由服务器判断，这里只判断是否需要更新资源
        public static bool NeedUpdate
        {
            get
            {
                if (Current.Equals(Server))
                    return false;
                return Server.v3 > Current.v3 || Server.v4 > Current.v4;
            }
        }

        public Version(string version)
        {
            Parse(version);
        }

        private void Parse(string version)
        {
            var versions = version.Split('.');
            if (versions.Length == 4)
            {
                v1 = int.Parse(versions[0]);
                v2 = int.Parse(versions[1]);
                v3 = int.Parse(versions[2]);
                v4 = int.Parse(versions[3]);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", v1, v2, v3, v4);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Version;
            if (other == null)
                return false;
            if (other.v1 != v1 || other.v2 != v2 || other.v3 != v3 || other.v4 != v4)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return v1 * 100000000 + v2 * 1000000 + v3 * 1000 + v4;
        }
    }
}