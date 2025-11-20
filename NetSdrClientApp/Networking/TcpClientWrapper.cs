using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient, IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _host;
        private readonly int _port;
        private CancellationTokenSource? _cts;
        private bool _disposed;

        public event EventHandler<byte[]>? MessageReceived;

        public bool Connected => _client?.Connected ?? false;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected) return;

            try 
            {
                _client = new TcpClient();
                _client.Connect(_host, _port);
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();

                _ = Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }

            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _client = null;
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            // FIX: Перевірка, щоб не кидати помилку, якщо з'єднання розірвано
            if (_client == null || !Connected || _stream == null) 
            {
                return; 
            }

            try
            {
                await _stream.WriteAsync(data);
            }
            catch (Exception)
            {
                // Ігноруємо помилки запису
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            while (!token.IsCancellationRequested && _stream != null && Connected)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, token);
                    if (bytesRead == 0) break;

                    var receivedData = new byte[bytesRead];
                    Array.Copy(buffer, receivedData, bytesRead);
                    
                    MessageReceived?.Invoke(this, receivedData);
                }
                catch
                {
                    break;
                }
            }
            Disconnect();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Disconnect();
            }
            _disposed = true;
        }
    }
}