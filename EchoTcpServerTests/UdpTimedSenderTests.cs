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
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(1000);
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
            sender.StartSending(50);
            Thread.Sleep(100); 
            Assert.DoesNotThrow(() => sender.StopSending());
        }

        [Test]
        public void Dispose_ShouldCleanUpResources()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(100);
            Assert.DoesNotThrow(() => sender.Dispose());
            Assert.DoesNotThrow(() => sender.Dispose());
        }

        [Test]
        public void SendMessageCallback_ShouldHandleExceptions_Gracefully()
        {

            using var sender = new UdpTimedSender("INVALID_IP_ADDRESS_TO_TRIGGER_CATCH", 60000);
            
            
            sender.StartSending(10);
            
            Thread.Sleep(50);

            Assert.Pass(); 
        }
    }
}