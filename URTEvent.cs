using System;

namespace URT
{
    public static class URTEvent
    {
        public static ConfirmDelegate ConfirmUpdateEvent;
        public static ProjectCompleteDelegate UpdateProjectCompleteEvent;
        public static NoticeDelegate DiskSizeNotEnoughEvent;
        public static RemoteTextDelegate GetServerMD5TextEvent;
        public static RemoteTextDelegate GetServerInfoEvent;


        public delegate void ProjectCompleteDelegate(ErrorCode code, string message);
        public delegate void ConfirmDelegate(Action ok, Action cancel, params object[] args);
        public delegate void NoticeDelegate(Action ok, params object[] args);
        public delegate void RemoteTextDelegate(ErrorCode code, string message);

        public delegate void OnDownloadAssetBundleProcess(string abname, int totalSize, int currentSize, int contentSize);
        public delegate void OnDownloadAssetBundleComplete(ErrorCode code, string message);
    }
}
