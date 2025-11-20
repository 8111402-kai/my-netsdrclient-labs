using System;
using System.Threading;

namespace NetSdrClientApp.Networking
{
    public abstract class BaseClientWrapper : IDisposable
    {
        // Робимо nullable, щоб уникнути null-reference
        protected CancellationTokenSource? _cts;

        // Безпечний доступ до токена
        protected CancellationToken Token => _cts?.Token ?? CancellationToken.None;

        protected void ResetCancellationToken()
        {
            // Перед створенням нового, безпечно чистимо старий
            Cancel(); 
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        protected void Cancel()
        {
            if (_cts != null)
            {
                try
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        _cts.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ігноруємо, якщо об'єкт вже видалений. Це нормально.
                }
            }
        }

        // Цей метод фігурував у твоєму стектрейсі, залишаємо його як обгортку
        protected void StopCancellationToken()
        {
            Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Безпечно зупиняємо і видаляємо
                Cancel();
                _cts?.Dispose();
                _cts = null;
            }
        }
    }
}