using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoTcpServerApp.Server;

namespace EchoTcpServerTests
{
    public class EchoServerTests
    {
        [SetUp]
        public void Setup()
        {
        }

        // --------------------------------------------------------------------
        // 1. Основний тест: ехо повинно повернути ті самі дані
        // --------------------------------------------------------------------
        [Test]
        public async Task EchoStreamAsync_ShouldEchoBackMessage()
        {
            // Arrange
            var originalMessage = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            var stream = new MemoryStream();
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            stream.Position = 0;

            // Act
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            // Assert
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var fullContent = await reader.ReadToEndAsync();

            Assert.AreEqual(originalMessage + originalMessage, fullContent);
        }

        // --------------------------------------------------------------------
        // 2. Тест: пустий потік не повинен нічого записати
        // --------------------------------------------------------------------
        [Test]
        public async Task EchoStreamAsync_ShouldHandleEmptyMessage()
        {
            // Arrange
            var stream = new MemoryStream();
            stream.Position = 0;

            // Act
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            // Assert
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var actualMessage = await reader.ReadToEndAsync();

            Assert.AreEqual(string.Empty, actualMessage);
        }

        // --------------------------------------------------------------------
        // 3. Новий тест: метод повинен коректно поводитися,
        //    якщо потік закритий і запис у нього неможливий
        // --------------------------------------------------------------------
        [Test]
        public void EchoStreamAsync_ShouldThrowIfStreamIsClosed()
        {
            // Arrange
            var stream = new MemoryStream();
            stream.Close(); // явно закриваємо

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await EchoServer.EchoStreamAsync(stream, CancellationToken.None);
            });
        }
    }
}
