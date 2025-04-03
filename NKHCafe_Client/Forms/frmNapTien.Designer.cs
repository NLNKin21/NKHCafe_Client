namespace NKHCafe_Client.Forms
{
    partial class frmNapTien
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblInfo = new System.Windows.Forms.Label();
            this.lblTenDangNhapValue = new System.Windows.Forms.Label();
            this.lblAmount = new System.Windows.Forms.Label();
            this.nudSoTien = new System.Windows.Forms.NumericUpDown();
            this.btnGuiYeuCau = new System.Windows.Forms.Button();
            this.btnHuy = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudSoTien)).BeginInit();
            this.SuspendLayout();
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInfo.Location = new System.Drawing.Point(25, 25);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(185, 20);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "Yêu cầu nạp tiền tài khoản:";
            // 
            // lblTenDangNhapValue
            // 
            this.lblTenDangNhapValue.AutoSize = true;
            this.lblTenDangNhapValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTenDangNhapValue.Location = new System.Drawing.Point(229, 25);
            this.lblTenDangNhapValue.Name = "lblTenDangNhapValue";
            this.lblTenDangNhapValue.Size = new System.Drawing.Size(124, 20);
            this.lblTenDangNhapValue.TabIndex = 1;
            this.lblTenDangNhapValue.Text = "[Tên đăng nhập]";
            // 
            // lblAmount
            // 
            this.lblAmount.AutoSize = true;
            this.lblAmount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAmount.Location = new System.Drawing.Point(25, 65);
            this.lblAmount.Name = "lblAmount";
            this.lblAmount.Size = new System.Drawing.Size(174, 20);
            this.lblAmount.TabIndex = 2;
            this.lblAmount.Text = "Số tiền muốn nạp (VNĐ):";
            // 
            // nudSoTien
            // 
            this.nudSoTien.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nudSoTien.Increment = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudSoTien.Location = new System.Drawing.Point(212, 63);
            this.nudSoTien.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.nudSoTien.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudSoTien.Name = "nudSoTien";
            this.nudSoTien.Size = new System.Drawing.Size(180, 27);
            this.nudSoTien.TabIndex = 3;
            this.nudSoTien.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudSoTien.ThousandsSeparator = true;
            this.nudSoTien.Value = new decimal(new int[] {
            50000,
            0,
            0,
            0});
            this.nudSoTien.ValueChanged += new System.EventHandler(this.nudSoTien_ValueChanged);
            // 
            // btnGuiYeuCau
            // 
            this.btnGuiYeuCau.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGuiYeuCau.Location = new System.Drawing.Point(75, 115);
            this.btnGuiYeuCau.Name = "btnGuiYeuCau";
            this.btnGuiYeuCau.Size = new System.Drawing.Size(120, 35);
            this.btnGuiYeuCau.TabIndex = 4;
            this.btnGuiYeuCau.Text = "Gửi Yêu Cầu";
            this.btnGuiYeuCau.UseVisualStyleBackColor = true;
            this.btnGuiYeuCau.Click += new System.EventHandler(this.btnGuiYeuCau_Click);
            // 
            // btnHuy
            // 
            this.btnHuy.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnHuy.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHuy.Location = new System.Drawing.Point(225, 115);
            this.btnHuy.Name = "btnHuy";
            this.btnHuy.Size = new System.Drawing.Size(120, 35);
            this.btnHuy.TabIndex = 5;
            this.btnHuy.Text = "Hủy Bỏ";
            this.btnHuy.UseVisualStyleBackColor = true;
            this.btnHuy.Click += new System.EventHandler(this.btnHuy_Click);
            // 
            // frmNapTien
            // 
            this.AcceptButton = this.btnGuiYeuCau;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnHuy;
            this.ClientSize = new System.Drawing.Size(422, 173);
            this.Controls.Add(this.btnHuy);
            this.Controls.Add(this.btnGuiYeuCau);
            this.Controls.Add(this.nudSoTien);
            this.Controls.Add(this.lblAmount);
            this.Controls.Add(this.lblTenDangNhapValue);
            this.Controls.Add(this.lblInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmNapTien";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Yêu Cầu Nạp Tiền";
            this.Load += new System.EventHandler(this.frmNapTien_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudSoTien)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // Khai báo các biến thành viên cho các control
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label lblTenDangNhapValue;
        private System.Windows.Forms.Label lblAmount;
        private System.Windows.Forms.NumericUpDown nudSoTien;
        private System.Windows.Forms.Button btnGuiYeuCau;
        private System.Windows.Forms.Button btnHuy;
    }
}