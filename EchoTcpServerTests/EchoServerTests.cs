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
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task EchoStreamAsync_ShouldEchoBackMessage()
        {
            // --- Arrange (Підготовка) ---
            var originalMessage = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            // Створюємо потік і записуємо туди "вхідні" дані як байти
            var stream = new MemoryStream();
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

            // "Перемотуємо" на початок, щоб сервер міг прочитати це як вхідні дані
            stream.Position = 0;

            // --- Act (Дія) ---
            // Запускаємо ехо. Воно прочитає дані і допише їх в кінець потоку.
            await EchoServer.EchoStreamAsync(stream, CancellationToken.None);

            // --- Assert (Перевірка) ---
            // Тепер у потоці має бути: [Вхідні дані] + [Ехо-відповідь]
            
            // Перемотуємо на початок і читаємо все разом
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var fullContent = await reader.ReadToEndAsync();

            // Перевіряємо, що повний текст це "Hello, World!" + "Hello, World!"
            Assert.AreEqual(originalMessage + originalMessage, fullContent);
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
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var actualMessage = await reader.ReadToEndAsync();

            Assert.AreEqual(string.Empty, actualMessage);
        }
    }
}