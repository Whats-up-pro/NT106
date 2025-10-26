using MessagingApp.Forms.Auth;
using MessagingApp.Config;
using System;
using System.Windows.Forms;

namespace MessagingApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Initialize Firebase on startup
            try
            {
                FirebaseConfig.Initialize();
                Console.WriteLine("✅ Firebase initialized successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Lỗi khởi tạo Firebase:\n\n{ex.Message}\n\nVui lòng kiểm tra file firebase-credentials.json trong thư mục Config.",
                    "Lỗi Khởi Động",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return; // Exit app if Firebase fails
            }

            // Run login form
            Application.Run(new LoginForm());
        }
    }
}
