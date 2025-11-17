using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// КРОК 1: Додаємо namespace
namespace NetSdrClientApp.Networking
{
    // КРОК 2: Додаємо успадкування від BaseClientWrapper
    public class UdpClientWrapper : BaseClientWrapper, IUdpClient
    {
        private readonly IPEndPoint _localEndPoint;
        
        // КРОК 3: Видаляємо поле _cts, оскільки воно тепер у батьківському класі
        // private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public async Task StartListeningAsync()
        {
            // КРОК 4: Замінюємо логіку створення _cts на метод з бази
            ResetCancellationToken();
            Console.WriteLine("Start listening for UDP messages...");

            try
            {
                _udpClient = new UdpClient(_localEndPoint);
                
                // Цей _cts.Token тепер береться з батьківського класу
                while (!_cts.Token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                    MessageReceived?.Invoke(this, result.Buffer);

                    Console.WriteLine($"Received from {result.RemoteEndPoint}");
                }
            }
            catch (OperationCanceledException)
            {
                //empty
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }
        }

        public void StopListening()
        {
            try
            {
                // КРОК 5: Замінюємо логіку зупинки _cts на метод з бази
                StopCancellationToken();
                _udpClient?.Close();
                Console.WriteLine("Stopped listening for UDP messages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while stopping: {ex.Message}");
            }
        }

        public void Exit()
        {
            // КРОК 6: Прибираємо дублювання - цей метод робить те саме, що й StopListening
            StopListening();
        }

        public override int GetHashCode()
        {
            var payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint.Address}|{_localEndPoint.Port}";

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return BitConverter.ToInt32(hash, 0);
        }
    }
}