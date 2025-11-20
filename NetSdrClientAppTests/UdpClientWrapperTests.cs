using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientAppTests.Networking
{
    [TestFixture]
    public class UdpClientWrapperTests
    {
        [Test]
        public async Task Integration_ShouldReceiveUdpPackets()
        {
            // 1. Вибираємо вільний порт
            int port = new Random().Next(60000, 65000);
            
            // 2. Створюємо наш Wrapper (слухач)
            using var wrapper = new UdpClientWrapper(port);
            
            // 3. Запускаємо прослуховування
            var listenTask = wrapper.StartListeningAsync();

            // 4. Створюємо відправника (UdpClient)
            using var sender = new UdpClient();
            var dataToSend = new byte[] { 1, 2, 3, 4 };
            var endpoint = new IPEndPoint(IPAddress.Loopback, port);

            // Підписуємося на подію отримання
            var tcs = new TaskCompletionSource<byte[]>();
            wrapper.MessageReceived += (s, e) => tcs.TrySetResult(e);

            // 5. Відправляємо дані
            await sender.SendAsync(dataToSend, dataToSend.Length, endpoint);

            // 6. Чекаємо на отримання (з таймаутом)
            var receivedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            if (receivedTask == tcs.Task)
            {
                var receivedData = tcs.Task.Result;
                Assert.That(receivedData, Is.EqualTo(dataToSend));
            }
            else
            {
                Assert.Fail("Timeout: UDP packet was not received.");
            }

            // 7. Зупинка
            wrapper.StopListening();
            
            try 
            { 
                await listenTask; 
            } 
            catch (Exception) 
            { 
                // Ігноруємо помилки скасування
            }
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