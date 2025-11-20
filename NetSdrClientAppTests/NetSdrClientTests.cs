using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.IO;
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
            
            // Симулюємо просту відправку
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                    .Returns(Task.CompletedTask);

            _udpMock = new Mock<IUdpClient>();

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsync_ShouldSendInitializationCommands()
        {
            
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() => 
                {
                    
                    _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x00, 0x01 });
                })
                .Returns(Task.CompletedTask);

            await _client.ConnectAsync();

            // Assert
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            // Очікуємо 3 команди ініціалізації
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendCommand()
        {
            // Arrange
            _tcpMock.Setup(t => t.Connected).Returns(true);
             _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01 }))
                .Returns(Task.CompletedTask);

            // Act
            await _client.ChangeFrequencyAsync(100000, 1);

            // Assert
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public async Task StartIQAsync_ShouldStartUdpListener()
        {
            // Arrange
            _tcpMock.Setup(t => t.Connected).Returns(true);
             _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01 }))
                .Returns(Task.CompletedTask);

            // Act
            await _client.StartIQAsync();

            // Assert
            Assert.That(_client.IQStarted, Is.True);
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
        }

        [Test]
        public async Task StopIQAsync_ShouldStopUdpListener()
        {
            // Arrange
            _tcpMock.Setup(t => t.Connected).Returns(true);
             _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() => _tcpMock.Raise(m => m.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01 }))
                .Returns(Task.CompletedTask);
            
            // Спочатку стартуємо
            await _client.StartIQAsync();

            // Act
            await _client.StopIQAsync();

            // Assert
            Assert.That(_client.IQStarted, Is.False);
            _udpMock.Verify(u => u.StopListening(), Times.Once);
        }
        
        [Test]
        public async Task StopIQ_WhenNotConnected_ShouldLogMessage()
        {
            // Arrange
            _tcpMock.Setup(t => t.Connected).Returns(false);
            
            // Act
            await _client.StopIQAsync();

            // Assert
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }
        
        [Test]
        public void UdpMessageReceived_ShouldProcessSamples()
        {
            
            var data = new byte[] { 0x04, 0x84, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04 }; // Fake UDP packet
            
            // Act & Assert
            Assert.DoesNotThrow(() => 
                _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, data));
        }
    }
}