using System;
using UnityEngine;
using UnityEngine.Networking;

namespace URT
{
    internal class GetRemoteTextWorker : Worker
    {
        private string m_RemoteUrl;
        private URTEvent.RemoteTextDelegate m_FinishCallback;
        private UnityWebRequestAsyncOperation m_Request;
        private int m_Timeout;
        private float m_TimeoutTime = float.MaxValue;
        public bool IsTimeout { get; private set; }
        private bool m_Finished = false;

        public GetRemoteTextWorker(string url, int timeout, URTEvent.RemoteTextDelegate finishCallback)
        {
            m_RemoteUrl = url;
            m_Timeout = timeout;
            m_FinishCallback = finishCallback;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Disposed || m_Finished)
                    return false;
                if (IsTimeout)
                    return false;
                return true;
            }
        }
        public override void Dispose()
        {
            if (Disposed)
                return;
            base.Dispose();
            if (m_Request != null)
                m_Request.webRequest.Abort();
        }

        public override void Run()
        {
            var request = UnityWebRequest.Get(m_RemoteUrl);
            request.timeout = m_Timeout;
            m_Request = request.SendWebRequest();
            m_TimeoutTime = m_Timeout + Time.unscaledTime;
        }

        public override void Update()
        {
            if (Disposed || m_Finished)
                return;

            if (m_Request.isDone)
                IsTimeout = false;
            else if (Time.unscaledTime > m_TimeoutTime)
                IsTimeout = true;

            if (IsTimeout)
            {
                Dispose();
                OnComplete(ErrorCode.TIMEOUT, string.Empty);
            }
            else if (m_Request.isDone)
            {
                var request = m_Request.webRequest;
                switch  (request.responseCode)
                {
                    case 200:
                        OnComplete(ErrorCode.SUCCESS, request.downloadHandler.text);
                        break;
                    default:
                        OnComplete(ErrorCode.ERROR, request.responseCode.ToString());
                        break;
                }
            }
        }

        private void OnComplete(ErrorCode code, string message)
        {
            m_Finished = true;
            if (Disposed)
                return;
            m_FinishCallback(code, message);
        }
    }
}
