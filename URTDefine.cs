
using System;

namespace URT
{
    public enum ErrorCode
    {
        SUCCESS = 0,
        ERROR = 1,
        CANCEL = 2,
        TIMEOUT = 3,
    }

    public enum Project
    {
        Download = 1,
        Decompress = 2,
        RemoteText = 3,
    }

    public class ABFileInfo
    {
        public string filename;
        public string md5;
        public int rawSize;              // 压缩前的文件大小
        public int compressedSize;		 // 压缩后的文件大小
    }
}
