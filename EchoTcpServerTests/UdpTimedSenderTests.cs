using EchoTcpServerApp.Client;
using NUnit.Framework;
using System;
using System.Threading;

namespace EchoTcpServerTests
{
    [TestFixture]
    public class UdpTimedSenderTests
    {
        [Test]
        public void Constructor_ShouldInitialize_Correctly()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            Assert.That(sender, Is.Not.Null);
        }

        [Test]
        public void StartSending_ShouldThrow_IfAlreadyStarted()
        {
            // Arrange
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            
            // Act
            sender.StartSending(1000);

            // Assert
            // Намагаємося запустити другий раз
            Assert.Throws<InvalidOperationException>(() => sender.StartSending(1000));
        }

        [Test]
        public void StopSending_ShouldNotThrow_IfNotStarted()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            Assert.DoesNotThrow(() => sender.StopSending());
        }

        [Test]
        public void StopSending_ShouldWork_AfterStart()
        {
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(100);
            
            // Даємо йому трохи часу попрацювати (і викликати callback хоча б раз)
            Thread.Sleep(150); 
            
            Assert.DoesNotThrow(() => sender.StopSending());
        }

        [Test]
        public void Dispose_ShouldCleanUpResources()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(100);
            
            Assert.DoesNotThrow(() => sender.Dispose());
            
            // Повторний виклик Dispose теж не має падати
            Assert.DoesNotThrow(() => sender.Dispose());
        }
    }
}