using System;
using System.Threading;

namespace NetSdrClientApp.Networking
{
    public abstract class BaseClientWrapper : IDisposable
    {
        protected CancellationTokenSource? _cts;
        private bool _disposed = false;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Звільняємо керовані ресурси
                    StopCancellationToken();
                }
                _disposed = true;
            }
        }
    }
}