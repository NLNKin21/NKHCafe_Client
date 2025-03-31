using NKHCafe_Client.Network; // Để sử dụng SocketClient
using NKHCafe_Client.Utils; // Có thể cần nếu bạn có các lớp tiện ích chung (ví dụ: MessageHandler)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NKHCafe_Client.Forms
{
    public partial class frmNapTien : Form
    {
        // Biến lưu trữ thông tin cần thiết và đối tượng SocketClient
        private int _idTaiKhoan;
        private int _idMay;
        private string _tenDangNhap;
        private SocketClient _socketClient; // Quan trọng: Sử dụng lại đối tượng socket từ frmClientMain


        /// <summary>
        /// Constructor của form nạp tiền.
        /// </summary>
        /// <param name="idTaiKhoan">ID Tài khoản của client.</param>
        /// <param name="idMay">ID Máy client đang sử dụng.</param>
        /// <param name="tenDangNhap">Tên đăng nhập của client.</param>
        /// <param name="socketClient">Đối tượng SocketClient đã kết nối từ frmClientMain.</param>
        public frmNapTien(int idTaiKhoan, int idMay, string tenDangNhap, SocketClient socketClient)
        {
            InitializeComponent(); // Khởi tạo các control từ Designer

            // Lưu trữ thông tin nhận được
            _idTaiKhoan = idTaiKhoan;
            _idMay = idMay;
            _tenDangNhap = tenDangNhap;
            _socketClient = socketClient; // Gán đối tượng SocketClient được truyền vào

            // Hiển thị tên đăng nhập lên label
            lblTenDangNhapValue.Text = _tenDangNhap;
        }

        /// <summary>
        /// Xử lý sự kiện khi nhấn nút "Gửi Yêu Cầu".
        /// </summary>
        private void btnGuiYeuCau_Click(object sender, EventArgs e)
        {
            // Lấy số tiền từ NumericUpDown
            decimal soTienNap = nudSoTien.Value;

            // --- Kiểm tra dữ liệu đầu vào ---
            if (soTienNap <= 0)
            {
                MessageBox.Show("Vui lòng nhập số tiền nạp hợp lệ (lớn hơn 0).",
                                "Số Tiền Không Hợp Lệ",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                nudSoTien.Focus(); // Đặt lại focus vào ô nhập
                return; // Dừng thực hiện nếu số tiền không hợp lệ
            }

            // --- Kiểm tra kết nối socket ---
            if (_socketClient == null || !_socketClient.IsConnected)
            {
                MessageBox.Show("Không thể kết nối đến server. Vui lòng thử lại sau hoặc kiểm tra kết nối mạng.",
                                "Lỗi Kết Nối",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return; // Dừng nếu không có kết nối
            }

            // --- Gửi yêu cầu đến Server ---
            try
            {
                // Định dạng message: COMMAND|IDTaiKhoan|IDMay|Amount
                // Ví dụ: REQUEST_DEPOSIT|101|5|50000
                string message = $"REQUEST_DEPOSIT|{_idTaiKhoan}|{_idMay}|{soTienNap}";

                // Gửi message qua socket
                bool sent = _socketClient.Send(message);

                if (sent)
                {
                    // Thông báo cho người dùng đã gửi yêu cầu thành công
                    MessageBox.Show($"Đã gửi yêu cầu nạp số tiền {soTienNap:N0} VNĐ.\n" +
                                    "Vui lòng đợi quản trị viên xác nhận tại quầy.",
                                    "Gửi Yêu Cầu Thành Công",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK; // Đặt kết quả để form cha (frmClientMain) biết là đã OK
                    this.Close(); // Đóng form nạp tiền
                }
                else
                {
                    // Thông báo lỗi nếu không gửi được message
                    MessageBox.Show("Gửi yêu cầu đến server thất bại. Vui lòng thử lại.",
                                    "Lỗi Gửi Dữ Liệu",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi ngoại lệ có thể xảy ra trong quá trình gửi
                MessageBox.Show($"Đã xảy ra lỗi trong quá trình gửi yêu cầu: {ex.Message}",
                                "Lỗi Hệ Thống",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi nhấn nút "Hủy Bỏ".
        /// </summary>
        private void btnHuy_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; // Đặt kết quả là Cancel
            this.Close(); // Đóng form
        }

        /// <summary>
        /// Xử lý sự kiện khi form được tải lên (Load).
        /// </summary>
        private void frmNapTien_Load(object sender, EventArgs e)
        {
            // Có thể thêm các thiết lập ban đầu tại đây nếu cần
            // Ví dụ: Đặt focus vào ô nhập số tiền
            nudSoTien.Select(0, nudSoTien.Text.Length); // Chọn toàn bộ text để dễ ghi đè
            nudSoTien.Focus();
        }
    }
}