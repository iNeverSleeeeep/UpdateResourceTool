using System;
using System.Collections.Generic;

namespace URT
{
    public static class MD5Helper
    {
        public static Dictionary<string, ABFileInfo> GetMD5DictionaryFromText(string text)
        {
            var datas = new Dictionary<string, ABFileInfo>();
            var md5s = text.Split(new[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < md5s.Length; ++i)
            {
                string[] info = md5s[i].Split(new[] { ":" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (info.Length == 4)
                {
                    ABFileInfo fileInfo = new ABFileInfo();
                    fileInfo.filename = info[0];
                    fileInfo.md5 = info[1];
                    fileInfo.rawSize = int.Parse(info[2]);
                    fileInfo.compressedSize = int.Parse(info[3]);
                    datas.Add(fileInfo.filename, fileInfo);
                }
            }
            return datas;
        }

        [Obsolete("现在项目使用的不是这种MD5算法，先这样写，正常的话要使用GetMD5DictionaryFromText函数", false)]
        public static Dictionary<string, ABFileInfo> GetMD5DictionaryFromTextDummy(string text)
        {
            var datas = new Dictionary<string, ABFileInfo>();
            var md5s = text.Split(new[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < md5s.Length; ++i)
            {
                string[] info = md5s[i].Split(new[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
                ABFileInfo fileInfo = new ABFileInfo();
                fileInfo.filename = info[0];
                fileInfo.md5 = info[1];
                fileInfo.rawSize = int.Parse(info[1]);
                fileInfo.compressedSize = int.Parse(info[1]);
                datas.Add(fileInfo.filename, fileInfo);
            }
            return datas;
        }
    }
}
