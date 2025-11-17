using System;
using System.Threading;

namespace NetSdrClientApp.Networking
{
    public abstract class BaseClientWrapper : IDisposable
    {
        protected CancellationTokenSource _cts;

        protected BaseClientWrapper()
        {
            _cts = new CancellationTokenSource();
        }

        protected void ResetCancellationToken()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
        }

        protected void StopCancellationToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        public void Dispose()
        {
            StopCancellationToken();
            GC.SuppressFinalize(this);
        }
    }
}