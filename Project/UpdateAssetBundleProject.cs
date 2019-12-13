using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace URT
{
    public class UpdateAssetBundleProject
    {
        private List<ABFileInfo> m_DifferentFiles;
        private List<string> m_ServerUrls;
        private int m_CurrentUrlIndex;
        private string m_ServerMD5Text;

        private object m_ProcessLocker = new object();
        private object m_FailCountLocker = new object();

        public long TotalSize { get; private set; } // 本次更新总大小
        public long CurrentSize { get; private set; } // 已经下载了的大小
        public long ContentSize { get; private set; } // 本次更新了的大小
        public long FilesRawSize { get; private set; } // 本次需要更新的大小（解压后）
        public long FilesCompressedSize { get; private set; } // 本次需要更新的大小（解压前）

        public int FailCount { get; private set; } // 失败的个数

        public float Process
        {
            get
            {
                return ((float)CurrentSize) / TotalSize;
            }
        }

        private string CurrentServerUrl
        {
            get
            {
                return m_ServerUrls[m_CurrentUrlIndex];
            }
        }

        public void Prepare(string serverMD5Text, List<string> serverUrls, bool isRetry = false)
        {
            m_ServerMD5Text = serverMD5Text;
            m_ServerUrls = serverUrls;
            CurrentSize = 0;
            FailCount = 0;
            // 可能上次更新了一半，这种情况 不要清空更新缓存目录
            if (SameUpdateVersionCache() == false)
            {
                FileHelper.ClearDirectory(URTConfig.UPDATE_CACHE_PATH);
                PlayerPrefs.SetString(URTConfig.PREF_APP_UPDATECACHE_VERSION, Version.Server.ToString());
            }

            var serverFiles = MD5Helper.GetMD5DictionaryFromTextDummy(serverMD5Text);
            var localMD5FilePath = URTConfig.UPDATE_PATH + "/" + URTConfig.MD5_FILENAME;
            var localFiles = MD5Helper.GetMD5DictionaryFromTextDummy(FileHelper.ReadTextFromFile(localMD5FilePath));

            // 比对MD5 得到需要更新的文件
            m_DifferentFiles = CalculateDifferentFiles(localFiles, serverFiles);
            m_DifferentFiles = RemoveFinishedFiles(m_DifferentFiles);
            if (m_DifferentFiles.Count == 0)
                OnComplete(ErrorCode.SUCCESS);
            else
            {
                // 取得下载了一半的资源的大小
                var alreadyDownloadTempFileSize = GetAlreadyDownloadedTempFileSize();

                // 得到本次更新的大小
                long filesRawSize; long filesCompressedSize;
                CalculateFilesSize(m_DifferentFiles, out filesRawSize, out filesCompressedSize);
                FilesRawSize = filesRawSize;
                FilesCompressedSize = filesCompressedSize;
                TotalSize = CurrentSize + FilesCompressedSize;
                CurrentSize += alreadyDownloadTempFileSize;

                // 用户确认是否需要更新
                if (isRetry == false)
                    URTEvent.ConfirmUpdateEvent(PrepareAfterConfirm, OnManualCancelUpdate, FilesRawSize, FilesCompressedSize, alreadyDownloadTempFileSize, m_DifferentFiles.Count);
                else
                    PrepareAfterConfirm();
            }
        }

        public void Retry()
        {
            Prepare(m_ServerMD5Text, m_ServerUrls, isRetry:true);
        }

        public void PrepareAfterConfirm()
        {
            // 检查硬盘空间大小
            long needSize = Mathf.FloorToInt((FilesRawSize + FilesCompressedSize) * 1.2f);
            if (URTUtil.DiskFreeSize < needSize)
            {
                long moreSize = needSize - URTUtil.DiskFreeSize;
                URTEvent.DiskSizeNotEnoughEvent(OnManualCancelUpdate, moreSize);
                return;
            }
            PrepareWorkers();
            StartProject();
        }

        private long GetAlreadyDownloadedTempFileSize()
        {
            long size = 0;
            foreach (var file in m_DifferentFiles)
            {
                var path = URTConfig.UPDATE_CACHE_PATH + "/" + file.filename + ".temp";
                size += FileHelper.FileSize(path);
            }

            return size;
        }

        public void PrepareWorkers()
        {
            foreach (var file in m_DifferentFiles)
            {
                var url = CurrentServerUrl + "/" + file.filename;
                var path = URTConfig.UPDATE_CACHE_PATH + "/" + file.filename;
                var worker = new DownloadFileWorker(url, file.compressedSize, path, OnAssetBundleProcess, OnAssetBundleComplete);
                Boss.Instance.AddWorker(Project.Download, worker);
            }
        }

        public void OnManualCancelUpdate()
        {
            OnComplete(ErrorCode.CANCEL);
        }

        public void StartProject()
        {
            Boss.Instance.StartProject(Project.Download, OnComplete);
        }

        private void OnAssetBundleProcess(string abname, int totalSize, int currentSize, int contentSize)
        {
            lock (m_ProcessLocker)
            {
                CurrentSize += contentSize;
                ContentSize += contentSize;
            }
        }

        private void OnAssetBundleComplete(ErrorCode code, string message)
        {
            if (code == ErrorCode.ERROR || code == ErrorCode.TIMEOUT)
            {
                Debug.LogWarning("OnAssetBundleComplete Fail " + message);
                lock (m_FailCountLocker)
                    FailCount++;
            }
        }

        private void OnComplete(ErrorCode code, string message = "")
        {
            if (FailCount > 0 && code == ErrorCode.SUCCESS)
                code = ErrorCode.ERROR;
            if (code == ErrorCode.SUCCESS)
            {
                var path = URTConfig.UPDATE_PATH + "/" + URTConfig.MD5_FILENAME;
                FileHelper.DeleteFile(path);
                FileHelper.SaveTextToFile(m_ServerMD5Text, path);
            }
            URTEvent.UpdateProjectCompleteEvent(code, message);
        }

        private bool SameUpdateVersionCache()
        {
            var cacheVersion = PlayerPrefs.GetString(URTConfig.PREF_APP_UPDATECACHE_VERSION);
            var serverVersion = Version.Server.ToString();
            return serverVersion == cacheVersion;
        }

        private List<ABFileInfo> CalculateDifferentFiles(Dictionary<string, ABFileInfo> localFiles, Dictionary<string, ABFileInfo> serverFiles)
        {
            var differentFiles = new List<ABFileInfo>();
            foreach (var serverFile in serverFiles)
            {
                ABFileInfo localFile = null;
                if (localFiles.TryGetValue(serverFile.Key, out localFile) == false)
                    differentFiles.Add(serverFile.Value);
                else if (localFile.md5 != serverFile.Value.md5)
                    differentFiles.Add(serverFile.Value);
            }
            return differentFiles;
        }

        private void CalculateFilesSize(List<ABFileInfo> files, out long rawSize, out long compressedSize)
        {
            rawSize = 0;
            compressedSize = 0;
            foreach (var file in files)
            {
                rawSize += file.rawSize;
                compressedSize += file.compressedSize;
            }
        }

        private List<ABFileInfo> RemoveFinishedFiles(List<ABFileInfo> differentFiles)
        {
            var files = new List<ABFileInfo>();
            foreach (var file in differentFiles)
            {
                var path = URTConfig.UPDATE_CACHE_PATH + "/" + file.filename;
                if (FileHelper.FileExists(path) == false)
                    files.Add(file);
                else
                    CurrentSize += FileHelper.FileSize(path);
            }
            return files;
        }
    }
}