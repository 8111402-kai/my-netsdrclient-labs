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
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;

        public EchoServer(int port)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();
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

                    // Викликаємо приватний обробник
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
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    // РЕФАКТОРИНГ
                    // Ми викликаємо нашу нову ТЕСТОВАНУ логіку
                    await EchoStreamAsync(stream, token);
                    
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        // НОВИЙ МЕТОД ДЛЯ ТЕСТІВ
        // Цей публічний, статичний метод містить *тільки* логіку "ехо".
        // Він нічого не знає про TcpClient, тому ми можемо його легко протестувати.
        public static async Task EchoStreamAsync(Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                // Echo back the received message
                await stream.WriteAsync(buffer, 0, bytesRead, token);
                Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
            }
        }
  

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _cancellationTokenSource.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}