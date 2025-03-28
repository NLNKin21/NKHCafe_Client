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

namespace NKHCafe_Client.Forms
{
    public partial class frmOrderMon : Form
    {
        private List<ChiTietOrder> _gioHang = new List<ChiTietOrder>();

        public frmOrderMon()
        {
            InitializeComponent();
        }

        private void frmOrderMon_Load(object sender, EventArgs e)
        {
            LoadThucDon();
        }

        private void LoadThucDon()
        {
            try
            {
                string query = "SELECT IDMon, TenMon, DonGia FROM ThucDon";
                DataTable dt = KetNoiCSDL.ExecuteQuery(query);

                dgvThucDon.DataSource = dt;
                dgvThucDon.Columns["IDMon"].Visible = false;
                dgvThucDon.Columns["TenMon"].HeaderText = "Tên Món";
                dgvThucDon.Columns["DonGia"].HeaderText = "Đơn Giá";
                dgvThucDon.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải thực đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnThemVaoGio_Click(object sender, EventArgs e)
        {
            if (dgvThucDon.SelectedRows.Count > 0)
            {
                int idMon = Convert.ToInt32(dgvThucDon.SelectedRows[0].Cells["IDMon"].Value);
                string tenMon = dgvThucDon.SelectedRows[0].Cells["TenMon"].Value.ToString();
                decimal donGia = Convert.ToDecimal(dgvThucDon.SelectedRows[0].Cells["DonGia"].Value);
                int soLuong = (int)nudSoLuong.Value;

                if (soLuong <= 0)
                {
                    MessageBox.Show("Vui lòng chọn số lượng lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kiểm tra xem món đã có trong giỏ chưa
                ChiTietOrder existingItem = _gioHang.FirstOrDefault(item => item.IDMon == idMon);
                if (existingItem != null)
                {
                    existingItem.SoLuong += soLuong;
                }
                else
                {
                    ChiTietOrder newItem = new ChiTietOrder(idMon, tenMon, soLuong, donGia);
                    _gioHang.Add(newItem);
                }

                HienThiGioHang();
            }
        }

        private void HienThiGioHang()
        {
            dgvGioHang.DataSource = null;
            dgvGioHang.DataSource = _gioHang;

            dgvGioHang.Columns["IDMon"].Visible = false;
            dgvGioHang.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            dgvGioHang.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
        }

        private void btnXacNhanOrder_Click(object sender, EventArgs e)
        {
            if (_gioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng trống. Vui lòng chọn món.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(this.Owner is frmClientMain parentForm))
            {
                MessageBox.Show("Không thể lấy thông tin tài khoản.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                int idHoaDon = GetOrCreateHoaDon(parentForm.IDTaiKhoan, parentForm.IDMay);

                foreach (ChiTietOrder item in _gioHang)
                {
                    string query = "INSERT INTO ChiTietHoaDon (IDHoaDon, IDMon, SoLuong, DonGia) VALUES (@IDHoaDon, @IDMon, @SoLuong, @DonGia)";
                    SqlParameter[] parameters = new SqlParameter[]
                    {
                new SqlParameter("@IDHoaDon", idHoaDon),
                new SqlParameter("@IDMon", item.IDMon),
                new SqlParameter("@SoLuong", item.SoLuong),
                new SqlParameter("@DonGia", item.DonGia)
                    };

                    KetNoiCSDL.ExecuteNonQuery(query, parameters);
                }

                // Gửi thông báo cho Admin
                string noiDung = $"Máy {parentForm.IDMay} đã order món. ID hóa đơn: {idHoaDon}";
                string queryThongBao = "INSERT INTO ThongBao (NoiDung, ThoiGian, IDNguoiGui, IDNguoiNhan, DaXem) VALUES (@NoiDung, @ThoiGian, @IDNguoiGui, NULL, 0)";
                SqlParameter[] tbParams = new SqlParameter[]
                {
            new SqlParameter("@NoiDung", noiDung),
            new SqlParameter("@ThoiGian", DateTime.Now),
            new SqlParameter("@IDNguoiGui", parentForm.IDTaiKhoan)
                };

                KetNoiCSDL.ExecuteNonQuery(queryThongBao, tbParams);

                _gioHang.Clear();
                HienThiGioHang();

                MessageBox.Show("Order thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xác nhận order: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetOrCreateHoaDon(int idTaiKhoan, int idMay)
        {
            string query = "SELECT IDHoaDon FROM HoaDon WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'";
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@IDTaiKhoan", idTaiKhoan),
        new SqlParameter("@IDMay", idMay)
            };

            DataTable result = KetNoiCSDL.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                return Convert.ToInt32(result.Rows[0]["IDHoaDon"]);
            }
            else
            {
                // Tạo hóa đơn mới
                string insertQuery = "INSERT INTO HoaDon (IDTaiKhoan, IDMay, ThoiGianBatDau, TrangThai) VALUES (@IDTaiKhoan, @IDMay, @ThoiGianBatDau, 'DangCho')";
                SqlParameter[] insertParams = new SqlParameter[]
                {
            new SqlParameter("@IDTaiKhoan", idTaiKhoan),
            new SqlParameter("@IDMay", idMay),
            new SqlParameter("@ThoiGianBatDau", DateTime.Now)
                };

                KetNoiCSDL.ExecuteNonQuery(insertQuery, insertParams);

                // Lấy ID hóa đơn vừa tạo
                string getIdQuery = "SELECT SCOPE_IDENTITY()";
                return Convert.ToInt32(KetNoiCSDL.ExecuteScalar(getIdQuery));
            }
        }
    }
}