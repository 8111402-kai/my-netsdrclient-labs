using NetSdrClientApp.Messages;
using NUnit.Framework;
using System;
using System.Linq;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            // Calculate expected length explicitly
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            
            // Length calculation verification
            var expectedLength = num - ((int)actualType << 13);

            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(expectedLength));
                Assert.That(actualType, Is.EqualTo(type));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);

            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(actualType, Is.EqualTo(type));
            });
        }

        // --- ВИПРАВЛЕНИЙ ТЕСТ (IndexOutOfRangeException FIX) ---
        [Test]
        public void TranslateMessage_ShouldParseHeaderAndBodyCorrectly()
        {
            // Arrange
            var expectedType = NetSdrMessageHelper.MsgTypes.SetControlItem;

            // FIX 1: Довжина має бути 6 байт:
            // Header (2) + ItemCode (2) + Body (2) = 6.
            ushort headerValue = (ushort)(((int)expectedType << 13) | 6);
            byte[] headerBytes = BitConverter.GetBytes(headerValue);
            
            // FIX 2: Додаємо 2 байти для ItemCode, які зчитуються всередині методу
            byte[] itemCodePlaceholder = { 0x00, 0x00 };

            // Це наше тіло, яке ми хочемо отримати в кінці
            byte[] bodyBytes = { 0xAA, 0xBB };
            
            // Збираємо повне повідомлення
            byte[] fullMessage = headerBytes
                                 .Concat(itemCodePlaceholder)
                                 .Concat(bodyBytes)
                                 .ToArray();

            // Act
            // FIX 3: Використовуємо правильні змінні для out параметрів
            NetSdrMessageHelper.TranslateMessage(fullMessage, out var type, out var itemCode, out var seqNum, out var body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(type, Is.EqualTo(expectedType));
                Assert.That(body, Has.Length.EqualTo(2)); // Тепер довжина буде 2, а не 0
                Assert.That(body[0], Is.EqualTo(0xAA));
                Assert.That(body[1], Is.EqualTo(0xBB));
            });
        }

        [Test]
        public void GetSamples_ReturnsCorrectValues_For8BitSamples()
        {
            byte[] body = { 1, 2, 3, 4 };
            var samples = NetSdrMessageHelper.GetSamples(8, body).ToArray();
            
            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Length.EqualTo(4));
                Assert.That(samples[0], Is.EqualTo(1));
                Assert.That(samples[3], Is.EqualTo(4));
            });
        }

        [Test]
        public void GetSamples_ReturnsCorrectValues_For16BitSamples()
        {
            byte[] body = { 1, 0, 2, 0 };
            var samples = NetSdrMessageHelper.GetSamples(16, body).ToArray();
            
            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Length.EqualTo(2));
                Assert.That(samples[0], Is.EqualTo(1));
            });
        }

        [Test]
        public void GetSamples_ReturnsCorrectValues_For24BitSamples()
        {
            byte[] body = { 1, 2, 3, 4, 5, 6 };
            var samples = NetSdrMessageHelper.GetSamples(24, body).ToArray();
            
            int expectedFirst = BitConverter.ToInt32(new byte[] { 1, 2, 3, 0 }, 0);

            Assert.Multiple(() =>
            {
                Assert.That(samples, Has.Length.EqualTo(2));
                Assert.That(samples[0], Is.EqualTo(expectedFirst));
            });
        }

        [Test]
        public void GetSamples_ThrowsArgumentOutOfRange_WhenSampleSizeTooBig()
        {
            byte[] body = { 1, 2, 3, 4 };
            Assert.That(() => NetSdrMessageHelper.GetSamples(40, body).ToArray(), 
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GetSamples_ThrowsArgumentNullException_WhenDataIsNull()
        {
            Assert.That(() => NetSdrMessageHelper.GetSamples(16, null!).ToArray(),
                Throws.ArgumentNullException);
        }
    }
}