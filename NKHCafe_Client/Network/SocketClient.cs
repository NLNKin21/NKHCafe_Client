using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NKHCafe_Client.Network
{
    public class SocketClient
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public event Action<string> OnMessageReceived;

        public bool IsConnected => _client?.Connected ?? false;

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();

                // Bắt đầu lắng nghe phản hồi từ server
                Task.Run(ReceiveLoop);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Send(string message)
        {
            if (!IsConnected || _stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            _stream.Write(data, 0, data.Length);
        }

        private void ReceiveLoop()
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (IsConnected)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived?.Invoke(response);
                }
            }
            catch
            {
                // Có thể log lỗi
            }
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}