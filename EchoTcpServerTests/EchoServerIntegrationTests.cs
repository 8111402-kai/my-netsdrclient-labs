using NUnit.Framework;
using EchoTcpServerApp.Server;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;

namespace EchoTcpServerTests
{
    [TestFixture]
    public class EchoServerIntegrationTests
    {
        [Test]
        public async Task Server_Should_AcceptClient_And_EchoMessage_RealConnection()
        {
            // 1. Arrange: Запускаємо сервер на тестовому порту
            int port = 55555;
            var server = new EchoServer(port);
            
            // Запускаємо сервер у фоні (не чекаємо await, бо він заблокує тест)
            var serverTask = server.StartAsync();

            // Даємо серверу трохи часу на старт
            await Task.Delay(100);

            try
            {
                // 2. Act: Створюємо справжнього TCP клієнта і підключаємося
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);

                // Відправляємо повідомлення
                var stream = client.GetStream();
                string message = "Integration Test Data";
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);

                // Читаємо відповідь (Ехо)
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // 3. Assert: Сервер мав повернути те саме
                Assert.That(response, Is.EqualTo(message));
            }
            finally
            {
                // 4. Cleanup: Зупиняємо сервер
                server.Stop();
                
                // Ігноруємо помилки скасування задачі, це нормально при зупинці
                try { await serverTask; } catch { }
            }
        }
    }
}