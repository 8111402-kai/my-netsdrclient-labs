using NUnit.Framework;
using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class UdpClientWrapperTests
    {
        [Test]
        public async Task UdpWrapper_ShouldReceiveData_WhenSentToPort()
        {
            // 1. Arrange: Створюємо обгортку на порті 56789
            int port = 56789;
            using var wrapper = new UdpClientWrapper(port);
            
            // Змінна для збереження отриманих даних
            string receivedData = null;
            var tcs = new TaskCompletionSource<bool>();

            // Підписуємося на подію
            wrapper.MessageReceived += (sender, data) => 
            {
                receivedData = Encoding.UTF8.GetString(data);
                tcs.TrySetResult(true);
            };

            // 2. Act: Запускаємо слухача
            var listenTask = wrapper.StartListeningAsync();

            // Відправляємо UDP пакет на цей порт "ззовні"
            using (var sender = new UdpClient())
            {
                var bytes = Encoding.UTF8.GetBytes("HelloUDP");
                await sender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, port));
            }

            // Чекаємо на отримання (або тайм-аут 1 сек)
            await Task.WhenAny(tcs.Task, Task.Delay(1000));

            // Зупиняємо
            wrapper.StopListening();
            
            // Чекаємо завершення задачі слухача (ігноруємо помилки скасування)
            try { await listenTask; } catch { }

            // 3. Assert
            Assert.That(receivedData, Is.EqualTo("HelloUDP"));
        }

        [Test]
        public void StopListening_ShouldNotThrow_WhenNotStarted()
        {
            using var wrapper = new UdpClientWrapper(56790);
            Assert.DoesNotThrow(() => wrapper.StopListening());
        }
    }
}