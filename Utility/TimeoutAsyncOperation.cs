using System.Collections;
using UnityEngine;

namespace URT
{
    public class TimeoutAsyncOperation : IEnumerator
    {
        private float m_Timeout;

        public AsyncOperation AsyncOperation;
        public bool IsTimeout { get; private set; }

        public TimeoutAsyncOperation(AsyncOperation asyncOperation, float timeout)
        {
            AsyncOperation = asyncOperation;
            m_Timeout = Time.unscaledTime + timeout;
        }

        public object Current
        {
            get
            {
                return null;
            }
        }
        public bool MoveNext()
        {
            return !IsDone();
        }

        public void Reset()
        {

        }

        public bool IsDone()
        {
            if (AsyncOperation.isDone)
            {
                IsTimeout = false;
                return true;
            }
            else if (Time.unscaledTime > m_Timeout)
            {
                IsTimeout = true;
                return true;
            }
            return false;
        }
    }
}