using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServerApp.Server
{
    public class EchoServer
    {
        private readonly int _port;
        private TcpListener? _listener; // Nullable
        private readonly CancellationTokenSource _cancellationTokenSource; // Readonly

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
                    if (_listener == null) break; 
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                    Console.WriteLine("Client connected.");

                    // Викликаємо статичний обробник
                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"Accept error: {ex.Message}");
                }
            }
            Console.WriteLine("Server shutdown.");
        }

        // Smell fix: Made static
        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    await EchoStreamAsync(stream, token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        public static async Task EchoStreamAsync(Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            // Smell fix: Using ReadAsync(Memory<byte>, CancellationToken) implicitly
            while (!token.IsCancellationRequested && 
                   (bytesRead = await stream.ReadAsync(buffer, token)) > 0)
            {
                // Smell fix: Using WriteAsync(ReadOnlyMemory<byte>, CancellationToken) implicitly
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _cancellationTokenSource.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}