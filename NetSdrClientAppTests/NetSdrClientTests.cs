using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.Threading.Tasks;
using System;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() => _tcpMock.Setup(t => t.Connected).Returns(true));
            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() => _tcpMock.Setup(t => t.Connected).Returns(false));
            
            // Default mocks
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
            _udpMock = new Mock<IUdpClient>();

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsync_ShouldSendInitializationCommands()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x00, 0x01 }))
                .Returns(Task.CompletedTask);

            await _client.ConnectAsync();

            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public async Task ConnectAsync_WhenAlreadyConnected_ShouldDoNothing()
        {
            // Arrange: Симулюємо, що ми вже підключені
            _tcpMock.Setup(t => t.Connected).Returns(true);

            // Act
            await _client.ConnectAsync();

            // Assert: Connect і SendMessage НЕ мають викликатися
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Never);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public async Task StartIQAsync_WhenNotConnected_ShouldDoNothing()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            
            await _client.StartIQAsync();

            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Never);
        }

        [Test]
        public async Task StartIQAsync_ShouldStartUdpListener()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
               .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01 }))
               .Returns(Task.CompletedTask);

            await _client.StartIQAsync();

            Assert.That(_client.IQStarted, Is.True);
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
        }

        [Test]
        public async Task StopIQAsync_WhenNotConnected_ShouldDoNothing()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            
            await _client.StopIQAsync();

            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public async Task StopIQAsync_ShouldStopUdpListener()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
               .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01 }))
               .Returns(Task.CompletedTask);
            
            await _client.StartIQAsync(); // Set started state
            
            await _client.StopIQAsync();

            Assert.That(_client.IQStarted, Is.False);
            _udpMock.Verify(u => u.StopListening(), Times.Once);
        }

        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendCommand()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
               .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01 }))
               .Returns(Task.CompletedTask);

            await _client.ChangeFrequencyAsync(100000, 1);

            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public void UdpMessageReceived_WithNullOrEmptyData_ShouldDoNothing()
        {
            Assert.DoesNotThrow(() => _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, (byte[])null));
            Assert.DoesNotThrow(() => _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, new byte[0]));
        }
        
        [Test]
        public void UdpMessageReceived_ShouldProcessData()
        {
            // Цей тест перевіряє прохід по щасливому шляху обробки UDP
            var data = new byte[] { 0x04, 0x84, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04 };
            Assert.DoesNotThrow(() => _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, data));
        }
    }
}