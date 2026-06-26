using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace StudentMonitor.Views
{
    /// <summary>
    /// Sử dụng Firebase Auth + Realtime Database thay vì Flask backend
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void FullNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FullNamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void FullNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FullNamePlaceholder.Visibility = ((!string.IsNullOrEmpty(FullNameTextBox.Text)) ? Visibility.Collapsed : Visibility.Visible);
        }

        private void EmailTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            EmailPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void EmailTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            EmailPlaceholder.Visibility = ((!string.IsNullOrEmpty(EmailTextBox.Text)) ? Visibility.Collapsed : Visibility.Visible);
        }

        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UsernamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UsernamePlaceholder.Visibility = ((!string.IsNullOrEmpty(UsernameTextBox.Text)) ? Visibility.Collapsed : Visibility.Visible);
        }

        private void StudentIdTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            StudentIdPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void StudentIdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            StudentIdPlaceholder.Visibility = ((!string.IsNullOrEmpty(StudentIdTextBox.Text)) ? Visibility.Collapsed : Visibility.Visible);
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = ((!string.IsNullOrEmpty(PasswordBox.Password)) ? Visibility.Collapsed : Visibility.Visible);
        }

        private void ConfirmPasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void ConfirmPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordPlaceholder.Visibility = ((!string.IsNullOrEmpty(ConfirmPasswordBox.Password)) ? Visibility.Collapsed : Visibility.Visible);
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string username = UsernameTextBox.Text.Trim();
            string studentId = StudentIdTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string role = (((RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Giảng viên") ? "teacher" : "student");
            
            if (string.IsNullOrEmpty(fullName))
            {
                ShowError("Vui lòng nhập họ và tên.");
                return;
            }
            if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
            {
                ShowError("Vui lòng nhập email hợp lệ.");
                return;
            }
            if (string.IsNullOrEmpty(username) || username.Length < 4)
            {
                ShowError("Tên đăng nhập phải có ít nhất 4 ký tự.");
                return;
            }
            if (string.IsNullOrEmpty(studentId))
            {
                ShowError("Vui lòng nhập MSSV hoặc mã giảng viên.");
                return;
            }
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
                return;
            }
            if (password != confirmPassword)
            {
                ShowError("Mật khẩu xác nhận không khớp.");
                return;
            }
            HideMessages();
            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Đang đăng ký...";
            try
            {
                var (success, message) = await FirebaseService.RegisterAsync(
                    email, password, fullName, username, studentId, role);

                if (success)
                {
                    // Đồng bộ tài khoản mới lên MySQL và SQL Server
                    _ = Services.DatabaseSyncService.SyncUserAsync(
                        fullName, email, username, studentId, role, password);

                    MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi: " + ex.Message);
            }
            finally
            {
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "Đăng ký";
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideMessages()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }
}
