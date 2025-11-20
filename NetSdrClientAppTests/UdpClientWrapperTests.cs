using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetSdrClientAppTests.Networking
{
    [TestFixture]
    public class UdpClientWrapperTests
    {
        [Test]
        public async Task Integration_ShouldReceiveUdpPackets()
        {
            // 1. Випадковий порт
            int port = new Random().Next(60000, 65000);
            
            // 2. Створюємо Wrapper (слухач)
            using var wrapper = new UdpClientWrapper(port);
            
            // 3. Запускаємо прослуховування
            var listenTask = wrapper.StartListeningAsync();

            // 4. Створюємо відправника
            using var sender = new UdpClient();
            var dataToSend = new byte[] { 1, 2, 3, 4 };
            var endpoint = new IPEndPoint(IPAddress.Loopback, port);

            // Підписуємося на подію
            var tcs = new TaskCompletionSource<byte[]>();
            wrapper.MessageReceived += (s, e) => tcs.TrySetResult(e);

            // 5. Відправляємо дані
            await sender.SendAsync(dataToSend, dataToSend.Length, endpoint);

            // 6. Чекаємо
            var receivedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            if (receivedTask == tcs.Task)
            {
                Assert.That(tcs.Task.Result, Is.EqualTo(dataToSend));
            }
            else
            {
                Assert.Fail("Timeout receiving UDP packet");
            }

            // 7. Стоп
            wrapper.StopListening();
            
            // Чекаємо завершення таска прослуховування
            try { await listenTask; } catch {}
        }

        [Test]
        public void StopListening_ShouldNotThrow_IfNotStarted()
        {
            int port = new Random().Next(60000, 65000);
            using var wrapper = new UdpClientWrapper(port);
            Assert.DoesNotThrow(() => wrapper.StopListening());
        }
    }
}