using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.IO; // Added for FileStream
using System.Linq;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    public class NetSdrClient
    {
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;
        
        // Smell fix: Nullable TCS
        private TaskCompletionSource<byte[]>? _responseTaskSource;

        public bool IQStarted { get; private set; }

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient;
            _udpClient = udpClient;

            _tcpClient.MessageReceived += OnTcpMessageReceived;
            _udpClient.MessageReceived += OnUdpMessageReceived;
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect();

                var sampleRate = BitConverter.GetBytes((long)100000).Take(5).ToArray();
                var automaticFilterMode = BitConverter.GetBytes((ushort)0).ToArray();
                var adMode = new byte[] { 0x00, 0x03 };

                var msgs = new List<byte[]>
                {
                    NetSdrMessageHelper.GetControlItemMessage(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.IQOutputDataSampleRate, sampleRate),
                    NetSdrMessageHelper.GetControlItemMessage(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.RFFilter, automaticFilterMode),
                    NetSdrMessageHelper.GetControlItemMessage(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.ADModes, adMode),
                };

                foreach (var msg in msgs)
                {
                    await SendTcpRequest(msg);
                }
            }
        }

        public void Disconect()
        {
            _tcpClient.Disconnect();
        }

        public async Task StartIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return;
            }

            var args = new byte[] { 0x80, 0x02, 0x01, 1 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.ReceiverState, args);
            
            await SendTcpRequest(msg);

            IQStarted = true;
            _ = _udpClient.StartListeningAsync();
        }

        public async Task StopIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return;
            }

            var args = new byte[] { 0, 0x01, 0, 0 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.ReceiverState, args);

            await SendTcpRequest(msg);

            IQStarted = false;
            _udpClient.StopListening();
        }

        public async Task ChangeFrequencyAsync(long hz, int channel)
        {
            var channelArg = (byte)channel;
            var frequencyArg = BitConverter.GetBytes(hz).Take(5);
            var args = new[] { channelArg }.Concat(frequencyArg).ToArray();

            var msg = NetSdrMessageHelper.GetControlItemMessage(NetSdrMessageHelper.MsgTypes.SetControlItem, NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency, args);

            await SendTcpRequest(msg);
        }

        private void OnUdpMessageReceived(object? sender, byte[] e)
        {
            
            if (e == null || e.Length == 0) return;

            NetSdrMessageHelper.TranslateMessage(e, out _, out _, out _, out byte[] body);
            
            // Припускаємо 16 біт, бо так в конфігурації
            var samples = NetSdrMessageHelper.GetSamples(16, body);

            // Console output can be slow, consider removing for high performace
            // Console.WriteLine($"Samples: {body.Length} bytes");

            try 
            {
                using FileStream fs = new FileStream("samples.bin", FileMode.Append, FileAccess.Write, FileShare.Read);
                using BinaryWriter sw = new BinaryWriter(fs);
                foreach (var sample in samples)
                {
                    sw.Write((short)sample); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File write error: {ex.Message}");
            }
        }

        // Smell fix: Return type Task<byte[]?> to allow nulls safely
        private async Task<byte[]?> SendTcpRequest(byte[] msg)
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return null;
            }

            _responseTaskSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            var responseTask = _responseTaskSource.Task;

            await _tcpClient.SendMessageAsync(msg);

            // Wait for response
            var resp = await responseTask;
            return resp;
        }

        private void OnTcpMessageReceived(object? sender, byte[] e)
        {
            if (_responseTaskSource != null)
            {
                _responseTaskSource.TrySetResult(e); // TrySetResult is safer
                _responseTaskSource = null;
            }
            // Console.WriteLine("Response received");
        }
    }
}