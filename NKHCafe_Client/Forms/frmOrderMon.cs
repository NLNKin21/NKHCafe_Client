using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NKHCafe_Client.Models;
using NKHCafe_Client.Network;
using System.Diagnostics;

namespace NKHCafe_Client.Forms
{
    public partial class frmOrderMon : Form
    {
        // Biến thành viên để lưu thông tin cần thiết
        private int _idTaiKhoan;
        private int _idMay;
        private SocketClient _socketClient; // Để gửi yêu cầu qua mạng

        // Giỏ hàng tạm thời trên client
        private List<ChiTietOrder> _gioHang = new List<ChiTietOrder>();
        private DataTable _menuTable; // Lưu trữ dữ liệu menu

        // --- CONSTRUCTOR NHẬN 3 THAM SỐ ---
        public frmOrderMon(int idTaiKhoan, int idMay, SocketClient socketClient)
        {
            InitializeComponent();

            // Lưu trữ thông tin
            _idTaiKhoan = idTaiKhoan;
            _idMay = idMay;
            _socketClient = socketClient;

            // Kiểm tra socket client ngay trong constructor (tùy chọn)
            if (_socketClient == null || !_socketClient.IsConnected)
            {
                MessageBox.Show("Lỗi kết nối đến server. Không thể tải thực đơn hoặc đặt món.", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Cân nhắc đóng form nếu không có kết nối
                // this.Load += (s, e) => this.Close(); // Đóng form sau khi load xong nếu không có kết nối
            }
        }

        private void frmOrderMon_Load(object sender, EventArgs e)
        {
            // Chỉ tải thực đơn nếu có kết nối (nếu không đã báo lỗi trong constructor)
            if (_socketClient != null && _socketClient.IsConnected)
            {
                LoadThucDon();
                HienThiGioHang(); // Hiển thị giỏ hàng trống ban đầu
                                  // Thiết lập giá trị mặc định cho nudSoLuong nếu có
                if (this.Controls.ContainsKey("nudSoLuong")) // Kiểm tra control tồn tại
                {
                    ((NumericUpDown)this.Controls["nudSoLuong"]).Value = 1;
                }
            }
            else
            {
                // Vô hiệu hóa các control nếu không có kết nối
                btnThemVaoGio.Enabled = false;
                btnXacNhanOrder.Enabled = false;
                // nudSoLuong.Enabled = false; // Ví dụ
                dgvThucDon.Enabled = false;
            }

        }

        private void LoadThucDon()
        {
            try
            {
                // Lấy thực đơn từ CSDL (Client vẫn cần biết menu)
                string query = "SELECT IDMon, TenMon, DonGia FROM ThucDon"; // Lấy món đang bán
                _menuTable = KetNoiCSDL.ExecuteQuery(query, null); // Sử dụng lớp kết nối CSDL của bạn

                dgvThucDon.DataSource = _menuTable;

                // Cấu hình hiển thị DataGridView
                if (dgvThucDon.Columns.Contains("IDMon")) dgvThucDon.Columns["IDMon"].Visible = false;
                if (dgvThucDon.Columns.Contains("TenMon")) dgvThucDon.Columns["TenMon"].HeaderText = "Tên Món";
                if (dgvThucDon.Columns.Contains("DonGia"))
                {
                    dgvThucDon.Columns["DonGia"].HeaderText = "Đơn Giá";
                    dgvThucDon.Columns["DonGia"].DefaultCellStyle.Format = "N0";
                }
                dgvThucDon.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // Tự chỉnh độ rộng cột
                dgvThucDon.ClearSelection(); // Bỏ chọn hàng mặc định
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi tải thực đơn: {ex.Message}");
                MessageBox.Show("Lỗi khi tải thực đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Vô hiệu hóa control nếu tải lỗi
                btnThemVaoGio.Enabled = false;
                btnXacNhanOrder.Enabled = false;
                dgvThucDon.Enabled = false;
            }
        }

        private void btnThemVaoGio_Click(object sender, EventArgs e)
        {
            // Kiểm tra xem có hàng nào được chọn trong dgvThucDon không
            if (dgvThucDon.CurrentRow != null) // Sử dụng CurrentRow thay vì SelectedRows
            {
                try
                {
                    int idMon = Convert.ToInt32(dgvThucDon.CurrentRow.Cells["IDMon"].Value);
                    string tenMon = dgvThucDon.CurrentRow.Cells["TenMon"].Value.ToString();
                    decimal donGia = Convert.ToDecimal(dgvThucDon.CurrentRow.Cells["DonGia"].Value);
                    // Lấy số lượng từ NumericUpDown (đảm bảo control này tồn tại và có tên đúng)
                    int soLuong = (int)nudSoLuong.Value;

                    if (soLuong <= 0)
                    {
                        MessageBox.Show("Vui lòng chọn số lượng lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Tìm món trong giỏ hàng hiện tại
                    ChiTietOrder existingItem = _gioHang.FirstOrDefault(item => item.IDMon == idMon);
                    if (existingItem != null)
                    {
                        // Nếu đã có, cộng dồn số lượng
                        existingItem.SoLuong += soLuong;
                        Debug.WriteLine($"Updated item {idMon} in cart. New quantity: {existingItem.SoLuong}");
                    }
                    else
                    {
                        // Nếu chưa có, thêm mới vào giỏ
                        ChiTietOrder newItem = new ChiTietOrder(idMon, tenMon, soLuong, donGia);
                        _gioHang.Add(newItem);
                        Debug.WriteLine($"Added new item {idMon} to cart. Quantity: {soLuong}");
                    }

                    // Cập nhật hiển thị giỏ hàng
                    HienThiGioHang();
                    // Reset số lượng về 1 sau khi thêm (tùy chọn)
                    nudSoLuong.Value = 1;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi thêm vào giỏ: {ex.Message}");
                    MessageBox.Show("Đã xảy ra lỗi khi thêm món vào giỏ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else
            {
                MessageBox.Show("Vui lòng chọn một món ăn từ danh sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HienThiGioHang()
        {
            // Sử dụng BindingList để DataGridView tự cập nhật tốt hơn (tùy chọn nhưng nên dùng)
            // var bindingList = new BindingList<ChiTietOrder>(_gioHang);
            // dgvGioHang.DataSource = bindingList;
            // Hoặc cách đơn giản:
            dgvGioHang.DataSource = null; // Xóa datasource cũ
            dgvGioHang.DataSource = new List<ChiTietOrder>(_gioHang); // Tạo bản sao mới để refresh

            // Cấu hình hiển thị cột giỏ hàng
            if (dgvGioHang.Columns.Contains("IDMon")) dgvGioHang.Columns["IDMon"].Visible = false;
            if (dgvGioHang.Columns.Contains("TenMon")) dgvGioHang.Columns["TenMon"].HeaderText = "Tên Món";
            if (dgvGioHang.Columns.Contains("SoLuong")) dgvGioHang.Columns["SoLuong"].HeaderText = "SL";
            if (dgvGioHang.Columns.Contains("DonGia"))
            {
                dgvGioHang.Columns["DonGia"].HeaderText = "Đơn Giá";
                dgvGioHang.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            }
            if (dgvGioHang.Columns.Contains("ThanhTien")) // Giả sử class ChiTietOrder có thuộc tính này
            {
                dgvGioHang.Columns["ThanhTien"].HeaderText = "Thành Tiền";
                dgvGioHang.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
            }
            dgvGioHang.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void btnXacNhanOrder_Click(object sender, EventArgs e)
        {
            if (_gioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống. Vui lòng chọn món.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kiểm tra lại kết nối socket trước khi gửi
            if (_socketClient == null || !_socketClient.IsConnected)
            {
                MessageBox.Show("Mất kết nối đến server. Không thể gửi order.", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // --- Gửi từng món trong giỏ hàng thành các message riêng ---
            bool allSentSuccessfully = true;
            List<string> failedItems = new List<string>();

            try
            {
                Debug.WriteLine($"Sending {_gioHang.Count} order items...");
                foreach (ChiTietOrder item in _gioHang)
                {
                    // Tạo message yêu cầu đặt món cho từng item
                    string message = MessageHandler.CreateOrderRequestMessage(
                        _idTaiKhoan,
                        _idMay,
                        item.IDMon,
                        item.SoLuong
                    );

                    // Gửi message qua socket
                    bool sent = _socketClient.Send(message);

                    if (!sent)
                    {
                        allSentSuccessfully = false;
                        failedItems.Add($"{item.TenMon} (SL: {item.SoLuong})");
                        Debug.WriteLine($"Failed to send order for item {item.IDMon}");
                        // Có thể dừng ngay khi có lỗi hoặc cố gắng gửi hết
                        // break; // Bỏ comment nếu muốn dừng ngay
                    }
                    else
                    {
                        Debug.WriteLine($"Sent order request for item {item.IDMon}");
                    }
                }

                // --- Xử lý kết quả sau khi gửi ---
                if (allSentSuccessfully)
                {
                    MessageBox.Show("Đã gửi yêu cầu order thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _gioHang.Clear(); // Xóa giỏ hàng trên client sau khi gửi thành công
                    HienThiGioHang();
                    this.Close(); // Đóng form order
                }
                else
                {
                    string errorMessage = "Gửi yêu cầu thất bại cho các món sau:\n - " + string.Join("\n - ", failedItems);
                    MessageBox.Show(errorMessage, "Lỗi Gửi Order", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Không xóa giỏ hàng để người dùng có thể thử lại
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi gửi order: {ex.Message}");
                MessageBox.Show("Đã xảy ra lỗi trong quá trình gửi order: " + ex.Message, "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Không cần phương thức GetOrCreateHoaDon ở client nữa
        // private int GetOrCreateHoaDon(int idTaiKhoan, int idMay) { ... }


        // --- Thêm xử lý sự kiện cho nút xóa khỏi giỏ hàng (nếu có) ---
        private void btnXoaKhoiGio_Click(object sender, EventArgs e)
        {
            if (dgvGioHang.CurrentRow != null)
            {
                try
                {
                    // Lấy ID món từ hàng đang chọn trong giỏ hàng
                    int idMonToRemove = Convert.ToInt32(dgvGioHang.CurrentRow.Cells["IDMon"].Value);
                    // Tìm và xóa món khỏi list _gioHang
                    ChiTietOrder itemToRemove = _gioHang.FirstOrDefault(item => item.IDMon == idMonToRemove);
                    if (itemToRemove != null)
                    {
                        _gioHang.Remove(itemToRemove);
                        HienThiGioHang(); // Cập nhật lại hiển thị
                        Debug.WriteLine($"Removed item {idMonToRemove} from cart.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khi xóa khỏi giỏ: {ex.Message}");
                    MessageBox.Show("Đã xảy ra lỗi khi xóa món khỏi giỏ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một món trong giỏ hàng để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


    } 
} 