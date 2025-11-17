using NUnit.Framework;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoTcpServerApp.Server; // <--- Вказуємо на наш серверний код

namespace EchoTcpServerTests
{
    public class EchoServerTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task EchoStreamAsync_ShouldEchoBackMessage()
        {
            // Arrange (Підготовка)
            var originalMessage = "Hello, World!";
            var expectedMessage = "Hello, World!"; // Те, що ми очікуємо отримати (повернеться те саме)

            // Створюємо "фальшивий" потік даних у пам'яті
            var stream = new MemoryStream();
            
            // Записуємо наше повідомлення у потік, ніби його прислав клієнт
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            await writer.WriteAsync(originalMessage);
            stream.Position = 0; // "Перемотуємо" потік на початок

            // Act (Дія)
            // Викликаємо нашу тестовану логіку
            // CancellationToken.None - це "заглушка" для токена
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            // Assert (Перевірка)
            // "Перемотуємо" потік і читаємо, що в ньому тепер
            stream.Position = 0;
            var reader = new StreamReader(stream, Encoding.UTF8);
            var actualMessage = await reader.ReadToEndAsync();
            
            Assert.AreEqual(expectedMessage + originalMessage, actualMessage);
        }

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
            var reader = new StreamReader(stream, Encoding.UTF8);
            var actualMessage = await reader.ReadToEndAsync();
            
            Assert.AreEqual(string.Empty, actualMessage);
        }
    }
}