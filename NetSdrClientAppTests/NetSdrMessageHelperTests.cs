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
            var actualLength = msg.Length;
            
            // Витягуємо значення вручну для перевірки
            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);

            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(num - ((int)actualType << 13))); 
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

        [Test]
        public void TranslateMessage_ShouldParseHeaderAndBodyCorrectly()
        {
            // Arrange
            var expectedType = NetSdrMessageHelper.MsgTypes.SetControlItem;
            
            // Header: Type (3 bits) | Length (13 bits)
            // Casting to ushort explicitly
            ushort headerValue = (ushort)(((int)expectedType << 13) | 4);
            
            byte[] headerBytes = BitConverter.GetBytes(headerValue);
            byte[] bodyBytes = { 0xAA, 0xBB };
            byte[] fullMessage = headerBytes.Concat(bodyBytes).ToArray();

            // Act
            NetSdrMessageHelper.TranslateMessage(fullMessage, out var type, out var length, out var code, out var body);

            // Assert
            Assert.Multiple(() =>
            {
                // FIX: Приводимо обидва значення до int, щоб NUnit не сварився на різні типи
                Assert.That((int)type, Is.EqualTo((int)expectedType));
                Assert.That((int)length, Is.EqualTo(4));
                
                Assert.That(body, Has.Length.EqualTo(2));
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
            // FIX: Додано null! (null-forgiving operator), щоб прибрати Warning CS8625
            Assert.That(() => NetSdrMessageHelper.GetSamples(16, null!).ToArray(),
                Throws.ArgumentNullException);
        }
    }
}