using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;

namespace StudentMonitor.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// Sử dụng Firebase Auth thay vì Flask backend
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // ===== Placeholder behavior =====
        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UsernamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
                UsernamePlaceholder.Visibility = Visibility.Visible;
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PasswordBox.Password))
                PasswordPlaceholder.Visibility = Visibility.Visible;
        }

        // ===== Login button click =====
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string emailOrUsername = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Validate input
            if (string.IsNullOrEmpty(emailOrUsername) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập email và mật khẩu.");
                return;
            }

            // Firebase Auth yêu cầu email - nếu user nhập username thì thêm domain
            string email = emailOrUsername.Contains("@") ? emailOrUsername : emailOrUsername + "@student.dtu.edu.vn";

            HideMessages();
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Đang đăng nhập...";

            try
            {
                var (success, message, role, fullName) = await FirebaseService.LoginAsync(email, password);

                if (success)
                {
                    // Đồng bộ tài khoản lên MySQL và SQL Server (chạy nền, không block UI)
                    _ = Services.DatabaseSyncService.SyncUserAsync(
                        fullName,
                        FirebaseService.CurrentEmail ?? email,
                        FirebaseService.CurrentUsername ?? emailOrUsername,
                        FirebaseService.CurrentStudentId ?? "",
                        role,
                        password // Truyền password để băm và lưu
                    );

                    // Open main window based on role
                    if (role == "student" || role == "SinhVien")
                    {
                        var studentWindow = new StudentWindow(fullName, FirebaseService.CurrentUsername ?? emailOrUsername);
                        studentWindow.Show();
                    }
                    else
                    {
                        var mainWindow = new MainWindow();
                        mainWindow.Title = $"E-Learning - Xin chào {fullName}";
                        mainWindow.Show();
                    }
                    this.Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Đăng nhập";
            }
        }

        // ===== Navigate to Register =====
        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        // ===== Message helpers =====
        private void ShowError(string message)
        {
            SuccessBorder.Visibility = Visibility.Collapsed;
            ErrorMessage.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void ShowSuccess(string message)
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            SuccessMessage.Text = message;
            SuccessBorder.Visibility = Visibility.Visible;
        }

        private void HideMessages()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            SuccessBorder.Visibility = Visibility.Collapsed;
        }
    }
}
