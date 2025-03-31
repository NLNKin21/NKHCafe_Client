using System;
using System.Linq; // Cần cho Skip, ToArray
using System.Globalization; // Cần cho CultureInfo
using System.Diagnostics; // Cần cho Debug

namespace NKHCafe_Client.Network
{
    /// <summary>
    /// Cung cấp các phương thức tĩnh để tạo và phân tích các chuỗi message
    /// theo một định dạng thống nhất (sử dụng dấu '|' làm phân cách).
    /// </summary>
    public static class MessageHandler
    {
        /// <summary>
        /// Ký tự dùng để phân tách các phần trong message.
        /// </summary>
        public const char DELIMITER = '|';

        /// <summary>
        /// Phân tích chuỗi phản hồi thô từ server thành command và các phần dữ liệu.
        /// </summary>
        /// <param name="response">Chuỗi phản hồi nhận được.</param>
        /// <param name="command">OUT: Lệnh chính (đã chuyển thành chữ hoa, bỏ khoảng trắng thừa).</param>
        /// <param name="dataParts">OUT: Mảng chứa các phần dữ liệu đi kèm lệnh.</param>
        /// <returns>True nếu phân tích thành công (có ít nhất command), False nếu chuỗi rỗng hoặc không hợp lệ.</returns>
        public static bool ParseServerResponse(string response, out string command, out string[] dataParts)
        {
            command = null;
            dataParts = Array.Empty<string>(); // Khởi tạo mảng rỗng

            if (string.IsNullOrWhiteSpace(response))
            {
                Debug.WriteLine("[MessageHandler] Parse failed: Response is null or whitespace.");
                return false;
            }

            // Tách chuỗi dựa trên ký tự phân tách
            string[] parts = response.Split(DELIMITER);

            // Phần đầu tiên phải là command và không được rỗng
            if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]?.Trim())) // Kiểm tra null hoặc rỗng sau khi Trim
            {
                Debug.WriteLine($"[MessageHandler] Parse failed: No valid command found in '{response}'.");
                return false;
            }

            command = parts[0].Trim().ToUpperInvariant(); // Lấy command, chuẩn hóa
            dataParts = parts.Skip(1).ToArray();         // Lấy các phần còn lại làm dữ liệu

            Debug.WriteLine($"[MessageHandler] Parsed: Command='{command}', DataParts={dataParts.Length}");
            return true;
        }

        // --- Các Hàm Tạo Message Gửi Đi ---

        /// <summary>
        /// Tạo message định danh khi client kết nối.
        /// Format: CLIENT_CONNECT|idTaiKhoan|idMay
        /// </summary>
        public static string CreateClientConnectMessage(int idTaiKhoan, int idMay)
        {
            return $"CLIENT_CONNECT{DELIMITER}{idTaiKhoan}{DELIMITER}{idMay}";
        }

        /// <summary>
        /// Tạo message yêu cầu nạp tiền.
        /// Format: REQUEST_DEPOSIT|idTaiKhoan|idMay|amount
        /// </summary>
        /// <remarks>
        /// Sử dụng CultureInfo.InvariantCulture để đảm bảo dấu thập phân là '.'
        /// </remarks>
        public static string CreateDepositRequestMessage(int idTaiKhoan, int idMay, decimal amount)
        {
            string amountString = amount.ToString(CultureInfo.InvariantCulture);
            return $"REQUEST_DEPOSIT{DELIMITER}{idTaiKhoan}{DELIMITER}{idMay}{DELIMITER}{amountString}";
        }

        /// <summary>
        /// Tạo message chat.
        /// Format: CHAT|chatMessage
        /// </summary>
        /// <remarks>
        /// Cân nhắc thêm ID người gửi nếu server cần: CHAT|idNguoiGui|chatMessage
        /// </remarks>
        public static string CreateChatMessage(string chatMessage)
        {
            // Cần xử lý nếu chatMessage chứa ký tự DELIMITER. Có thể thay thế hoặc dùng cơ chế khác.
            // Ví dụ đơn giản: return $"CHAT{DELIMITER}{chatMessage.Replace(DELIMITER, ' ')}";
            return $"CHAT{DELIMITER}{chatMessage}";
        }

        /// <summary>
        /// Tạo message yêu cầu đặt món.
        /// Format: ORDER_REQUEST|idTaiKhoan|idMay|idMon|soLuong
        /// </summary>
        public static string CreateOrderRequestMessage(int idTaiKhoan, int idMay, int idMon, int soLuong)
        {
            return $"ORDER_REQUEST{DELIMITER}{idTaiKhoan}{DELIMITER}{idMay}{DELIMITER}{idMon}{DELIMITER}{soLuong}";
        }

        /// <summary>
        /// Tạo message yêu cầu đăng xuất từ client.
        /// Format: LOGOUT_REQUEST|idTaiKhoan|idMay
        /// </summary>
        public static string CreateLogoutRequestMessage(int idTaiKhoan, int idMay)
        {
            return $"LOGOUT_REQUEST{DELIMITER}{idTaiKhoan}{DELIMITER}{idMay}";
        }

        // --- Có thể thêm các hàm tạo message khác tại đây ---
        // Ví dụ: yêu cầu dịch vụ, thay đổi mật khẩu,...

    }
}