using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// Додаємо namespace, щоб тести могли його бачити

namespace EchoTcpServerApp.Server
{
    public class EchoServer
    {
        private readonly int _port;
        private TcpListener? _listener;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public EchoServer(int port)
        {
            _port = port;
            // _cancellationTokenSource ініціалізовано inline як readonly
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    // Викликаємо приватний обробник — тепер static (не використовує стан екземпляра)
                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        // Цей приватний метод керує з'єднанням
        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    // Викликаємо тестовану логіку, яка працює зі Stream
                    await EchoStreamAsync(stream, token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        client.Close();
                    }
                    catch { /* ignore */ }

                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        // Публічний метод для тестування логіки ехо
        public static async Task EchoStreamAsync(Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[8192];

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(), token);
                if (bytesRead <= 0) break;

                // Echo back the received message (новий overload)
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
            }
        }

        public void Stop()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
            }
            catch { /* ignore */ }

            try
            {
                _listener?.Stop();
            }
            catch { /* ignore */ }

            try
            {
                _cancellationTokenSource.Dispose();
            }
            catch { /* ignore */ }

            Console.WriteLine("Server stopped.");
        }
    }
}
