using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    // КРОК 1: Додаємо успадкування від BaseClientWrapper
    public class TcpClientWrapper : BaseClientWrapper, ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;

        // КРОК 2: Видаляємо поле _cts, оскільки воно тепер у батьківському класі
        // private CancellationTokenSource _cts; 

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected)
            {
                Console.WriteLine($"Already connected to {_host}:{_port}");
                return;
            }

            _tcpClient = new TcpClient();

            try
            {
                // КРОК 3: Замінюємо логіку створення _cts на метод з бази
                ResetCancellationToken(); 
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                Console.WriteLine($"Connected to {_host}:{_port}");
                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (Connected)
            {
                // КРОК 4: Замінюємо логіку зупинки _cts на метод з бази
                StopCancellationToken();
                
                _stream?.Close();
                _tcpClient?.Close();

                _tcpClient = null;
                _stream = null;
                Console.WriteLine("Disconnected.");
            }
            else
            {
                Console.WriteLine("No active connection to disconnect.");
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (Connected && _stream != null && _stream.CanWrite)
            {
                Console.WriteLine($"Message sent: " + data.Select(b => Convert.ToString(b, toBase: 16)).Aggregate((l, r) => $"{l} {r}"));
                await _stream.WriteAsync(data, 0, data.Length);
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        public async Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            if (Connected && _stream != null && _stream.CanWrite)
            {
                Console.WriteLine($"Message sent: " + data.Select(b => Convert.ToString(b, toBase: 16)).Aggregate((l, r) => $"{l} {r}"));
                await _stream.WriteAsync(data, 0, data.Length);
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        private async Task StartListeningAsync()
        {
            if (Connected && _stream != null && _stream.CanRead)
            {
                try
                {
                    Console.WriteLine($"Starting listening for incomming messages.");

                    // Цей _cts.Token тепер береться з батьківського класу
                    while (!_cts.Token.IsCancellationRequested) 
                    {
                        byte[] buffer = new byte[8194];

                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                        if (bytesRead > 0)
                        {
                            MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                        }
                    }
                }
                catch (OperationCanceledException) // Цей 'catch' тепер спрацює коректно
                {
                    //empty
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in listening loop: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("Listener stopped.");
                }
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }
    }

}