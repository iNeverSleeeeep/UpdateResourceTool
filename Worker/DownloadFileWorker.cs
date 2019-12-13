using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace URT
{
    // 下载文件 支持断点续传
    internal sealed class DownloadFileWorker : Worker
    {
        private string m_FileUrl;
        private string m_LocalPath;
        private int m_FileSize;
        private UnityWebRequestAsyncOperation m_WebRequest;
        private DownloadHandler m_DownloadHandler;

        private bool m_Finished = false;
        private object m_FinishLocker = new object();

        private URTEvent.OnDownloadAssetBundleProcess m_OnProcess;
        private URTEvent.OnDownloadAssetBundleComplete m_OnComplete;

        public DownloadFileWorker(string url, int fileSize, string path, URTEvent.OnDownloadAssetBundleProcess onProcess, URTEvent.OnDownloadAssetBundleComplete onComplete)
        {
            m_FileUrl = url;
            m_LocalPath = path;
            m_FileSize = fileSize;

            m_OnProcess = onProcess;
            m_OnComplete = onComplete;
        }

        public override void Dispose()
        {
            if (Disposed)
                return;
            base.Dispose();
        }

        public override bool keepWaiting
        {
            get
            {
                return m_Finished == false;
            }
        }

        public override void Run()
        {
            var request = UnityWebRequest.Get(m_FileUrl);

            m_DownloadHandler = new DownloadHandler(m_LocalPath, m_FileSize, OnProgress, OnComplete);

            request.chunkedTransfer = true;
            request.disposeDownloadHandlerOnDispose = true;
            request.SetRequestHeader("Range", "bytes=" + m_DownloadHandler.CurrentSize + "-");
            request.downloadHandler = m_DownloadHandler;

            m_WebRequest = request.SendWebRequest();
        }

        public override void Update()
        {
            if (Disposed)
                return;

            if (m_WebRequest == null)
                return;

            if (m_Finished)
                return;

            if (m_WebRequest.isDone)
            {
                var request = m_WebRequest.webRequest;
                // 发生错误
                if (string.IsNullOrEmpty(request.error) == false)
                {
                    OnComplete(ErrorCode.ERROR, request.error);
                }
                else
                {
                    switch (request.responseCode)
                    {
                        case 200: // 正确
                            // 什么也不做 等待DownloadHandler回调OmComplete
                            break;
                        case 206: // 断点续传成功
                            // 什么也不做 等待DownloadHandler回调OmComplete
                            break;
                        default:
                            OnComplete(ErrorCode.ERROR, string.Format("ResponseCode Failed: {0}", request.responseCode));
                            break;
                    }
                }
            }
        }

        private void OnProgress(string abname, int totalSize, int currentSize, int contentSize)
        {
            if (Disposed)
                return;
            m_OnProcess(abname, totalSize, currentSize, contentSize);
        }

        private void OnComplete(ErrorCode code, string message = "")
        {
            lock (m_FinishLocker)
            {
                if (m_Finished)
                    return;
                m_Finished = true;
            }
            if (Disposed)
                return;
            if (code == ErrorCode.ERROR || code == ErrorCode.TIMEOUT)
                message += " " + m_LocalPath;
            m_OnComplete(code, message);
        }
    }
    
    // 下载的数据，先写入缓存文件(*.temp) 下载完成后，再重命名为真正的文件
    public class DownloadHandler : DownloadHandlerScript
    {
        private string m_LocalPath;
        private string m_LocalPathTemp;
        private FileStream m_TempFileStream;

        private int m_TotalSize; // 文件总大小
        private int m_CurrentSize; // 已经下载的大小
        private int m_ContentSize; // 本次下载的大小

        private URTEvent.OnDownloadAssetBundleProcess m_OnProcess;
        private URTEvent.OnDownloadAssetBundleComplete m_OnComplete;

        public DownloadHandler(string path, int totalSize, URTEvent.OnDownloadAssetBundleProcess onProcess, URTEvent.OnDownloadAssetBundleComplete onComplete)
        {
            m_OnProcess = onProcess;
            m_OnComplete = onComplete;

            m_TotalSize = totalSize;

            m_LocalPath = path.Replace('\\', '/');
            
            m_LocalPathTemp = m_LocalPath + ".temp";
            m_TempFileStream = FileHelper.GetFileStream(m_LocalPathTemp);

            m_CurrentSize = (int)m_TempFileStream.Length;
            m_TempFileStream.Position = m_CurrentSize;
        }
        
        public int CurrentSize
        {
            get
            {
                return m_CurrentSize;
            }
        }

        public void Close()
        {
            if (m_TempFileStream != null)
            {
                var fs = m_TempFileStream;
                m_TempFileStream = null;
                fs.Close();
                fs.Dispose();
            }
        }

        void OnCompleted(ErrorCode code, string message = "")
        {
            m_OnComplete(code, message);
        }

        void OnProcess(int totalSize, int currentSize, int contentSize)
        {
            m_OnProcess(m_LocalPath, totalSize, currentSize, contentSize);
        }

        // 下载完成
        protected override void CompleteContent()
        {
            Close();

            // 没有下载到数据
            if (m_ContentSize == 0)
                OnCompleted(ErrorCode.ERROR, "Content size is 0");

            // 缓存文件不存在
            else if (FileHelper.FileExists(m_LocalPathTemp) == false)
                    OnCompleted(ErrorCode.ERROR, "Temp file miss: " + m_LocalPathTemp);

            // 成功了
            else
            {
#if UNITY_EDITOR
                var random = new System.Random();
                // 编辑器模拟随机失败
                if (random.NextDouble() < URTConfig.DEBUG_DOWNLOAD_FAIL)
                {
                    if (random.NextDouble() < 0.5)
                    {
                        // 模拟连接超时了
                        FileHelper.DeleteFile(m_LocalPathTemp);
                        OnProcess(m_TotalSize, 0, -m_CurrentSize);
                        OnCompleted(ErrorCode.TIMEOUT, "Simulate Timeout");
                    }
                    else
                    {
                        // 模拟下载了一半
                        byte[] buffer;
                        int throwSize = m_CurrentSize - m_CurrentSize / 2;
                        using (var fs = new FileStream(m_LocalPathTemp, FileMode.Open))
                        {
                            buffer = new byte[m_CurrentSize / 2];
                            fs.Read(buffer, 0, buffer.Length);
                            fs.Close();
                        }
                        FileHelper.DeleteFile(m_LocalPathTemp);
                        File.WriteAllBytes(m_LocalPathTemp, buffer);
                        OnProcess(m_TotalSize, m_CurrentSize - throwSize, -throwSize);
                        OnCompleted(ErrorCode.ERROR, "Simulate About");
                    }
                    return;
                }
#endif
                FileHelper.MoveFileForce(m_LocalPathTemp, m_LocalPath);
                OnCompleted(ErrorCode.SUCCESS);
            }
        }

        // 接收到要下载的长度
        protected override void ReceiveContentLength(int contentSize)
        {
            m_ContentSize = contentSize;
            return;
        }

        // 接收到数据
        protected override bool ReceiveData(byte[] data, int dataSize)
        {
            if (data == null || dataSize == 0)
                return false;
            
            m_TempFileStream.Write(data, 0, dataSize);
            m_CurrentSize += dataSize;
            OnProcess(m_TotalSize, m_CurrentSize, dataSize);
            return true;
        }
    }
}
