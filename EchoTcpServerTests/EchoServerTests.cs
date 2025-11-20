using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoTcpServerApp.Server;

namespace EchoTcpServerTests
{
    public class EchoServerTests
    {
        [Test]
        public async Task EchoStreamAsync_ShouldEchoBackMessage()
        {
            var originalMessage = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            using var stream = new MemoryStream();
            // Sonar Fix: Use WriteAsync(ReadOnlyMemory) overload (implicit for byte[])
            await stream.WriteAsync(messageBytes);
            stream.Position = 0;

            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var fullContent = await reader.ReadToEndAsync();

            Assert.That(fullContent, Is.EqualTo(originalMessage + originalMessage));
        }

        [Test]
        public async Task EchoStreamAsync_ShouldHandleEmptyMessage()
        {
            using var stream = new MemoryStream();
            
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var actualMessage = await reader.ReadToEndAsync();

            Assert.That(actualMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task Stop_ShouldCancelServerLoop_Gracefully()
        {
            var server = new EchoServer(0); 
            var serverTask = server.StartAsync();
            
            await Task.Delay(100);
            server.Stop();
            
            Assert.DoesNotThrowAsync(async () => await serverTask);
        }

        [Test]
        public async Task Integration_ServerShouldHandleRealTcpConnection()
        {
            int port = new Random().Next(50000, 60000);
            var server = new EchoServer(port);
            var serverTask = server.StartAsync();

            await Task.Delay(100);

            using (var client = new TcpClient())
            {
                await client.ConnectAsync("127.0.0.1", port);
                using var stream = client.GetStream();

                byte[] dataToSend = { 10, 20, 30 };
                // Sonar Fix
                await stream.WriteAsync(dataToSend);

                byte[] buffer = new byte[3];
                var readTask = stream.ReadAsync(buffer); // Sonar Fix: ReadAsync(Memory)
                var completedTask = await Task.WhenAny(readTask.AsTask(), Task.Delay(2000));

                if (completedTask == readTask.AsTask())
                {
                    Assert.That(buffer, Is.EqualTo(dataToSend));
                }
                else
                {
                    Assert.Fail("Timeout waiting for echo response");
                }
            }
            server.Stop();
            Assert.DoesNotThrowAsync(async () => await serverTask);
        }
    }
}