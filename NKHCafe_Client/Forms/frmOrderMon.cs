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
using System.IO;
using System.Net.Sockets;
using NKHCafe_Client.Utils;
using Newtonsoft.Json;

namespace NKHCafe_Client.Forms
{
    public partial class frmOrderMon : Form
    {

        public static string serverIp = "127.0.0.1"; // hoặc IP LAN của server
        public static int serverPort = 8888;
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
            GuiYeuCauOrderQuaSocket(); // Gọi hàm xử lý mới
        }

        private void GuiYeuCauOrderQuaSocket()
        {
            if (dgvGioHang.Rows.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var hoaDonRequest = new YeuCauOrder
            {
                IdTaiKhoan = _idTaiKhoan,
                IdMay = _idMay,
                ChiTiet = new List<ChiTietOrder>()
            };

            try
            {
                foreach (DataGridViewRow row in dgvGioHang.Rows)
                {
                    if (row.IsNewRow) continue;

                    int idMon;
                    string tenMon = row.Cells["TenMon"].Value?.ToString();
                    int soLuong;
                    decimal donGia;

                    bool idValid = int.TryParse(row.Cells["IDMon"].Value?.ToString(), out idMon);
                    bool slValid = int.TryParse(row.Cells["SoLuong"].Value?.ToString(), out soLuong);
                    bool giaValid = decimal.TryParse(row.Cells["DonGia"].Value?.ToString(), out donGia);

                    if (!idValid || string.IsNullOrEmpty(tenMon) || !slValid || !giaValid || soLuong <= 0)
                    {
                        Console.WriteLine($"Cảnh báo: Bỏ qua dòng {row.Index} trong giỏ hàng do dữ liệu không hợp lệ.");
                        continue;
                    }

                    hoaDonRequest.ChiTiet.Add(new ChiTietOrder(idMon, tenMon, soLuong, donGia));
                }

                if (!hoaDonRequest.ChiTiet.Any())
                {
                    MessageBox.Show("Không có món hợp lệ nào trong giỏ hàng để gửi đi.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý giỏ hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string jsonBody;
            string commandRequest;
            try
            {
                jsonBody = JsonConvert.SerializeObject(hoaDonRequest);
                commandRequest = "HOADONDOAN|" + jsonBody;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo dữ liệu JSON: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Gửi socket
            TcpClient client = null;
            NetworkStream stream = null;
            try
            {
                client = new TcpClient();
                client.Connect(serverIp, serverPort);
                stream = client.GetStream();

                byte[] dataToSend = Encoding.UTF8.GetBytes(commandRequest);
                stream.Write(dataToSend, 0, dataToSend.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string[] parts = response.Split('|');
                if (parts.Length >= 2)
                {
                    string command = parts[0].Trim();
                    string status = parts[1].Trim();
                    string message = parts.Length >= 3 ? parts[2].Trim() : "";

                    if (command == "ORDER_CONFIRMATION" && status == "OK")
                    {
                        MessageBox.Show("Đã gửi yêu cầu Order thành công!\n" + message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dgvGioHang.DataSource = null;
                        dgvGioHang.Rows.Clear();
                    }
                    else
                    {
                        MessageBox.Show($"Phản hồi từ server: {status}\n{message}", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Phản hồi từ server không hợp lệ:\n" + response, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gửi dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                stream?.Close();
                client?.Close();
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

        private void dgvThucDon_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    } 
} 