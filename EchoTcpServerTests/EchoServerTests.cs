using NUnit.Framework;
using System.IO;
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
    }
}