using System.Windows.Forms;

namespace NKHCafe_Client.Utils
{
    public static class Common
    {
        public static void ShowMessage(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static string FormatCurrency(decimal money)
        {
            return string.Format("{0:N0} VNĐ", money);
        }
    }
}