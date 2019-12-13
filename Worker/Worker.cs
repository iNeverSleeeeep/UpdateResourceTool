using System;

namespace URT
{
    internal abstract class Worker : IDisposable
    {
        protected bool Disposed { get; private set; }

        public virtual void Dispose()
        {
            Disposed = true;
        }

        public abstract bool keepWaiting
        {
            get;
        }

        public virtual void Run()
        {

        }

        public virtual void Update()
        {

        }
    }
}
