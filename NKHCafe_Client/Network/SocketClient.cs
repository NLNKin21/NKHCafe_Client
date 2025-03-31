using System;
using System.IO; // Cần cho IOException
using System.Net.Sockets;
using System.Text;
using System.Threading; // Cần cho CancellationTokenSource
using System.Threading.Tasks;
using System.Diagnostics; // Để dùng Debug.WriteLine

namespace NKHCafe_Client.Network
{
    public class SocketClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource; // Để hủy Task lắng nghe

        // Events để thông báo cho UI
        public event Action<string> OnMessageReceived;
        public event Action OnDisconnected;
        public event Action<string> OnConnectFailed; // Thêm event khi kết nối thất bại

        // Thuộc tính kiểm tra trạng thái kết nối
        public bool IsConnected => _client?.Connected ?? false;

        /// <summary>
        /// Kết nối đến server.
        /// </summary>
        /// <param name="ip">Địa chỉ IP của server.</param>
        /// <param name="port">Cổng của server.</param>
        /// <returns>True nếu kết nối thành công, False nếu thất bại.</returns>
        public bool Connect(string ip, int port)
        {
            // Đảm bảo ngắt kết nối cũ nếu đang thực hiện kết nối lại
            Disconnect();

            try
            {
                _client = new TcpClient();
                // Cân nhắc thêm Timeout cho kết nối nếu cần
                // var connectTask = _client.ConnectAsync(ip, port);
                // if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask) // Ví dụ timeout 5 giây
                // {
                //     throw new TimeoutException("Connection attempt timed out.");
                // }
                _client.Connect(ip, port); // Cách đơn giản hơn

                _stream = _client.GetStream();
                _cancellationTokenSource = new CancellationTokenSource();

                // Bắt đầu Task lắng nghe trong background
                // Không dùng Task.Run() trực tiếp nếu muốn quản lý CancellationToken tốt hơn
                Task.Factory.StartNew(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                Debug.WriteLine($"[CLIENT] Connected to {ip}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CLIENT] Connection failed: {ex.Message}");
                OnConnectFailed?.Invoke(ex.Message); // Thông báo lỗi kết nối
                _client?.Close(); // Dọn dẹp client nếu tạo ra mà kết nối thất bại
                _client = null;
                return false;
            }
        }

        /// <summary>
        /// Gửi một chuỗi message đến server.
        /// </summary>
        /// <param name="message">Chuỗi message cần gửi.</param>
        /// <returns>True nếu gửi thành công, False nếu có lỗi hoặc chưa kết nối.</returns>
        public bool Send(string message) // Đổi tên thành Send và trả về bool
        {
            // Kiểm tra các điều kiện cần thiết trước khi gửi
            if (_client == null || _stream == null || !IsConnected)
            {
                Debug.WriteLine("[CLIENT] Send failed: Not connected.");
                return false;
            }

            try
            {
                // Thêm ký tự xuống dòng (\n) nếu server của bạn đọc theo dòng.
                // Nếu server đọc theo độ dài hoặc dùng delimiter khác, hãy sửa đổi cho phù hợp.
                string messageToSend = message + "\n";
                byte[] data = Encoding.UTF8.GetBytes(messageToSend);

                // Gửi dữ liệu (WriteAsync an toàn hơn nếu dùng trong môi trường async)
                _stream.Write(data, 0, data.Length);
                _stream.Flush(); // Đảm bảo dữ liệu được gửi đi ngay lập tức

                Debug.WriteLine($"[CLIENT] Sent: {message}"); // Log message đã gửi (không kèm \n)
                return true; // Gửi thành công
            }
            // Bắt các lỗi mạng phổ biến
            catch (IOException ex) // Lỗi liên quan đến NetworkStream
            {
                Debug.WriteLine($"[CLIENT] Send failed (IOException): {ex.Message}");
                DisconnectAndNotify(); // Lỗi I/O thường là mất kết nối
                return false;
            }
            catch (SocketException ex) // Lỗi liên quan đến Socket nền tảng
            {
                Debug.WriteLine($"[CLIENT] Send failed (SocketException): {ex.Message}");
                DisconnectAndNotify(); // Lỗi socket thường là mất kết nối
                return false;
            }
            catch (ObjectDisposedException ex) // Lỗi nếu stream hoặc client đã bị đóng
            {
                Debug.WriteLine($"[CLIENT] Send failed (ObjectDisposedException): {ex.Message}");
                // Không cần gọi Disconnect nữa vì đối tượng đã bị disposed
                OnDisconnected?.Invoke(); // Chỉ cần thông báo
                return false;
            }
            catch (Exception ex) // Bắt các lỗi khác không mong muốn
            {
                Debug.WriteLine($"[CLIENT] Send failed (General Exception): {ex.Message}");
                DisconnectAndNotify(); // Ngắt kết nối khi có lỗi lạ
                return false;
            }
        }

        /// <summary>
        /// Vòng lặp lắng nghe dữ liệu từ server. Chạy trên một Task riêng.
        /// </summary>
        private async Task ReceiveLoop(CancellationToken token)
        {
            NetworkStream localStream = _stream; // Tạo bản sao cục bộ để tránh race condition khi Disconnect
            if (localStream == null) return;

            byte[] buffer = new byte[4096]; // Tăng buffer size nếu cần

            try
            {
                Debug.WriteLine("[CLIENT] Receive loop started.");
                while (!token.IsCancellationRequested && IsConnected)
                {
                    int bytesRead = 0;
                    try
                    {
                        // Sử dụng ReadAsync với CancellationToken
                        bytesRead = await localStream.ReadAsync(buffer, 0, buffer.Length, token);
                    }
                    catch (IOException ex) when (ex.InnerException is SocketException sex && (sex.SocketErrorCode == SocketError.ConnectionAborted || sex.SocketErrorCode == SocketError.ConnectionReset))
                    {
                        // Xử lý lỗi khi kết nối bị đóng đột ngột bởi server hoặc mạng
                        Debug.WriteLine($"[CLIENT] ReceiveLoop IOException (Connection closed): {sex.Message}");
                        bytesRead = 0; // Coi như không đọc được gì -> dẫn đến ngắt kết nối bên dưới
                    }
                    catch (ObjectDisposedException)
                    {
                        // Stream/Client đã bị đóng (có thể do gọi Disconnect từ thread khác)
                        Debug.WriteLine("[CLIENT] ReceiveLoop ObjectDisposedException: Stream closed.");
                        break; // Thoát vòng lặp
                    }


                    if (bytesRead == 0)
                    {
                        // Server đã đóng kết nối một cách bình thường hoặc có lỗi IO ở trên
                        Debug.WriteLine("[CLIENT] Server disconnected (bytesRead == 0).");
                        DisconnectAndNotify(); // Ngắt kết nối và thông báo
                        break; // Thoát vòng lặp
                    }

                    // Xử lý dữ liệu nhận được
                    string rawResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.WriteLine($"[CLIENT] Received raw ({bytesRead} bytes): {rawResponse}");

                    // --- Xử lý message bị chia nhỏ hoặc nối liền (Quan trọng!) ---
                    // Nếu server có thể gửi nhiều message liền nhau hoặc một message lớn bị chia nhỏ,
                    // bạn cần cơ chế tách message (ví dụ: dựa vào ký tự '\n')
                    // Đoạn code dưới đây là ví dụ đơn giản, cần điều chỉnh theo protocol của bạn
                    // (Giả sử server gửi các message kết thúc bằng '\n')

                    // TODO: Implement proper message framing/splitting if needed.
                    // For now, invoke for each chunk received, assuming each chunk is a complete message or handled by UI
                    OnMessageReceived?.Invoke(rawResponse);

                }
            }
            catch (OperationCanceledException)
            {
                // Task đã bị hủy (do gọi Disconnect) -> đây là trường hợp thoát bình thường
                Debug.WriteLine("[CLIENT] Receive loop cancelled.");
            }
            catch (Exception ex) // Các lỗi không mong muốn khác trong vòng lặp
            {
                Debug.WriteLine($"[CLIENT] Receive loop error: {ex.GetType().Name} - {ex.Message}");
                if (IsConnected) // Chỉ ngắt kết nối nếu lỗi xảy ra khi đang "connected"
                {
                    DisconnectAndNotify();
                }
            }
            finally
            {
                Debug.WriteLine("[CLIENT] Receive loop finished.");
            }
        }


        /// <summary>
        /// Ngắt kết nối hiện tại và dọn dẹp tài nguyên.
        /// </summary>
        public void Disconnect()
        {
            if (_client == null && _stream == null) return; // Đã ngắt kết nối rồi

            Debug.WriteLine("[CLIENT] Disconnecting...");

            // Hủy Task lắng nghe trước
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            // Đóng stream và client một cách an toàn
            try { _stream?.Close(); } catch (Exception ex) { Debug.WriteLine($"Error closing stream: {ex.Message}"); }
            try { _client?.Close(); } catch (Exception ex) { Debug.WriteLine($"Error closing client: {ex.Message}"); }

            _stream = null;
            _client = null;
            Debug.WriteLine("[CLIENT] Disconnected.");
        }

        /// <summary>
        /// Helper method để ngắt kết nối và kích hoạt sự kiện OnDisconnected.
        /// </summary>
        private void DisconnectAndNotify()
        {
            bool wasConnected = IsConnected; // Kiểm tra trạng thái *trước khi* ngắt
            Disconnect();
            if (wasConnected) // Chỉ thông báo nếu trước đó đang kết nối (tránh thông báo nhiều lần)
            {
                OnDisconnected?.Invoke();
            }
        }
    }
}