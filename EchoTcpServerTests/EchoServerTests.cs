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
            // Arrange
            var originalMessage = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            using var stream = new MemoryStream();
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            stream.Position = 0;

            // Act
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            // Assert
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var fullContent = await reader.ReadToEndAsync();

            // Smell fix: Assert.That syntax
            Assert.That(fullContent, Is.EqualTo(originalMessage + originalMessage));
        }

        [Test]
        public async Task EchoStreamAsync_ShouldHandleEmptyMessage()
        {
            // Arrange
            using var stream = new MemoryStream();
            
            // Act
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            // Assert
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var actualMessage = await reader.ReadToEndAsync();

            // Smell fix: Assert.That syntax
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

        // --- НОВИЙ ІНТЕГРАЦІЙНИЙ ТЕСТ ---
        // Цей тест піднімає реальний сервер і підключається до нього.
        // Це покриває рядки коду в StartAsync та HandleClientAsync.
        [Test]
        public async Task Integration_ServerShouldHandleRealTcpConnection()
        {
            // 1. Вибираємо випадковий порт, щоб уникнути конфліктів на CI/CD сервері
            int port = new Random().Next(50000, 60000);
            var server = new EchoServer(port);
            
            // Запускаємо сервер
            var serverTask = server.StartAsync();

            // Даємо час на прив'язку до порту
            await Task.Delay(100);

            // 2. Підключаємо реального TCP-клієнта
            using (var client = new TcpClient())
            {
                await client.ConnectAsync("127.0.0.1", port);
                using var stream = client.GetStream();

                // 3. Відправляємо дані
                byte[] dataToSend = { 10, 20, 30 };
                await stream.WriteAsync(dataToSend);

                // 4. Читаємо відповідь (Echo)
                byte[] buffer = new byte[3];
                
                // Читаємо з таймаутом, щоб тест не завис навічно, якщо сервер не відповість
                var readTask = stream.ReadAsync(buffer, 0, 3);
                var completedTask = await Task.WhenAny(readTask, Task.Delay(2000));

                if (completedTask == readTask)
                {
                    // 5. Перевіряємо, що сервер повернув те саме
                    Assert.That(buffer, Is.EqualTo(dataToSend));
                }
                else
                {
                    Assert.Fail("Timeout waiting for echo response");
                }
            }

            // 6. Зупиняємо сервер
            server.Stop();
            
            // Перевіряємо коректне завершення
            Assert.DoesNotThrowAsync(async () => await serverTask);
        }
    }
}