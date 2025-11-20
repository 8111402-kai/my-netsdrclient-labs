using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : IUdpClient, IDisposable
    {
        private readonly int _port;
        private UdpClient? _udpClient;
        private bool _isListening;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _port = port;
        }

        public async Task StartListeningAsync()
        {
            if (_isListening) return;

            try
            {
                _udpClient = new UdpClient(_port);
                _isListening = true;

                while (_isListening && _udpClient != null)
                {
                    try
                    {
                        var result = await _udpClient.ReceiveAsync();
                        MessageReceived?.Invoke(this, result.Buffer);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Клієнт зупинено
                        break;
                    }
                    catch (SocketException)
                    {
                        // Помилка мережі
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP Listener error: {ex.Message}");
            }
            finally
            {
                _isListening = false;
            }
        }

        public void StopListening()
        {
            _isListening = false;
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }

        public void Dispose()
        {
            StopListening();
            GC.SuppressFinalize(this);
        }
    }
}