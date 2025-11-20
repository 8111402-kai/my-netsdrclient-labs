using System;
using System.Collections.Generic;
using System.Linq;

namespace NetSdrClientApp.Messages
{
    public static class NetSdrMessageHelper
    {
        private const short _maxMessageLength = 8191;
        private const short _maxDataItemMessageLength = 8194;
        private const short _msgHeaderLength = 2; 
        private const short _msgControlItemLength = 2; 
        private const short _msgSequenceNumberLength = 2; 

        public enum MsgTypes
        {
            SetControlItem,
            CurrentControlItem,
            ControlItemRange,
            Ack,
            DataItem0,
            DataItem1,
            DataItem2,
            DataItem3
        }

        public enum ControlItemCodes
        {
            None = 0,
            IQOutputDataSampleRate = 0x00B8,
            RFFilter = 0x0044,
            ADModes = 0x008A,
            ReceiverState = 0x0018,
            ReceiverFrequency = 0x0020
        }

        public static byte[] GetControlItemMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            return GetMessage(type, itemCode, parameters);
        }

        public static byte[] GetDataItemMessage(MsgTypes type, byte[] parameters)
        {
            return GetMessage(type, ControlItemCodes.None, parameters);
        }

        private static byte[] GetMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            var itemCodeBytes = Array.Empty<byte>();
            if (itemCode != ControlItemCodes.None)
            {
                itemCodeBytes = BitConverter.GetBytes((ushort)itemCode);
            }

            var headerBytes = GetHeader(type, itemCodeBytes.Length + parameters.Length);

            List<byte> msg = new List<byte>();
            msg.AddRange(headerBytes);
            msg.AddRange(itemCodeBytes);
            msg.AddRange(parameters);

            return msg.ToArray();
        }

        public static bool TranslateMessage(byte[] msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort sequenceNumber, out byte[] body)
        {
            itemCode = ControlItemCodes.None;
            sequenceNumber = 0;
            bool success = true;
            var msgEnumarable = msg as IEnumerable<byte>;

            TranslateHeader(msgEnumarable.Take(_msgHeaderLength).ToArray(), out type, out int msgLength);
            msgEnumarable = msgEnumarable.Skip(_msgHeaderLength);
            msgLength -= _msgHeaderLength;

            if (type < MsgTypes.DataItem0) 
            {
                var value = BitConverter.ToUInt16(msgEnumarable.Take(_msgControlItemLength).ToArray());
                msgEnumarable = msgEnumarable.Skip(_msgControlItemLength);
                msgLength -= _msgControlItemLength;

                if (Enum.IsDefined(typeof(ControlItemCodes), (int)value))
                {
                    itemCode = (ControlItemCodes)value;
                }
                else
                {
                    success = false;
                }
            }
            else 
            {
                sequenceNumber = BitConverter.ToUInt16(msgEnumarable.Take(_msgSequenceNumberLength).ToArray());
                msgEnumarable = msgEnumarable.Skip(_msgSequenceNumberLength);
                msgLength -= _msgSequenceNumberLength;
            }

            body = msgEnumarable.ToArray();
            success &= body.Length == msgLength;
            return success;
        }

        public static IEnumerable<int> GetSamples(ushort sampleSizeBits, byte[] body)
        {
            // Sonar Fix: Use ThrowIfNull
            ArgumentNullException.ThrowIfNull(body);

            int bytesPerSample = sampleSizeBits / 8;

            if (bytesPerSample > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSizeBits), 
                    $"Sample size ({bytesPerSample} bytes) cannot be greater than 4 bytes (32 bits).");
            }

            return GetSamplesIterator(bytesPerSample, body);
        }

        private static IEnumerable<int> GetSamplesIterator(int bytesPerSample, byte[] body)
        {
            var bodyEnumerable = body as IEnumerable<byte>;
            var prefixBytes = Enumerable.Repeat((byte)0, 4 - bytesPerSample).ToArray();

            while (bodyEnumerable.Count() >= bytesPerSample)
            {
                yield return BitConverter.ToInt32(bodyEnumerable
                    .Take(bytesPerSample)
                    .Concat(prefixBytes)
                    .ToArray());
                
                bodyEnumerable = bodyEnumerable.Skip(bytesPerSample);
            }
        }

        private static byte[] GetHeader(MsgTypes type, int msgLength)
        {
            int lengthWithHeader = msgLength + 2;

            if (type >= MsgTypes.DataItem0 && lengthWithHeader == _maxDataItemMessageLength)
            {
                lengthWithHeader = 0;
            }

            if (msgLength < 0 || lengthWithHeader > _maxMessageLength)
            {
                throw new ArgumentException("Message length exceeds allowed value");
            }

            return BitConverter.GetBytes((ushort)(lengthWithHeader + ((int)type << 13)));
        }

        private static void TranslateHeader(byte[] header, out MsgTypes type, out int msgLength)
        {
            var num = BitConverter.ToUInt16(header.ToArray());
            type = (MsgTypes)(num >> 13);
            msgLength = num - ((int)type << 13);

            if (type >= MsgTypes.DataItem0 && msgLength == 0)
            {
                msgLength = _maxDataItemMessageLength;
            }
        }
    }
}