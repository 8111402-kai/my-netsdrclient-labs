using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetSdrClientAppTests.Networking
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        [Test]
        public async Task Integration_ShouldConnectAndSendReceiveData()
        {
            // 1. Setup Fake Server
            int port = new Random().Next(55000, 60000);
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            using var wrapper = new TcpClientWrapper("127.0.0.1", port);
            TcpClient? serverSideClient = null;

            try
            {
                // 2. Connect Wrapper
                wrapper.Connect();
                
                // Server accepts
                serverSideClient = await listener.AcceptTcpClientAsync();
                var serverStream = serverSideClient.GetStream();

                Assert.That(wrapper.Connected, Is.True);

                // 3. Test Sending (Wrapper -> Server)
                byte[] dataToSend = { 1, 2, 3, 4 };
                await wrapper.SendMessageAsync(dataToSend);

                byte[] serverBuffer = new byte[4];
                int bytesRead = await serverStream.ReadAsync(serverBuffer, 0, 4);
                Assert.That(serverBuffer, Is.EqualTo(dataToSend));

                // 4. Test Receiving (Server -> Wrapper)
                var tcs = new TaskCompletionSource<byte[]>();
                wrapper.MessageReceived += (sender, args) => tcs.TrySetResult(args);

                byte[] responseData = { 0xAA, 0xBB };
                await serverStream.WriteAsync(responseData);

                var receivedDataTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));
                
                if (receivedDataTask == tcs.Task)
                {
                    Assert.That(tcs.Task.Result, Is.EqualTo(responseData));
                }
                else
                {
                    Assert.Fail("Timeout waiting for MessageReceived event");
                }
            }
            finally
            {
                // Clean up properly
                wrapper.Dispose();
                serverSideClient?.Dispose();
                listener.Stop();
            }
        }

        [Test]
        public async Task SendMessageAsync_ShouldNotThrow_WhenDisconnected()
        {
            // Цей тест перевіряє захист "if (!Connected) return"
            using var wrapper = new TcpClientWrapper("127.0.0.1", 12345); // Wrong port
            
            Assert.DoesNotThrowAsync(async () => await wrapper.SendMessageAsync(new byte[] { 1, 2 }));
        }

        [Test]
        public void Connect_ShouldHandleConnectionFailures_Gracefully()
        {
            using var wrapper = new TcpClientWrapper("127.0.0.1", 59999);
            
            Assert.Throws<SocketException>(() => wrapper.Connect());
            
            Assert.That(wrapper, Is.Not.Null);
        }
    }
}