using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StudentMonitor.Services;

namespace StudentMonitor.Views
{
    public partial class ProfileWindow : Window
    {
        private string? _currentAvatarPath;

        public ProfileWindow()
        {
            InitializeComponent();
            LoadProfileData();
        }

        private void LoadProfileData()
        {
            TxtFullName.Text = FirebaseService.CurrentFullName;

            if (FirebaseService.CurrentRole == "teacher" || FirebaseService.CurrentRole == "admin")
            {
                StudentIdPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtStudentId.Text = FirebaseService.CurrentStudentId;
            }

            TxtHometown.Text = FirebaseService.CurrentHometown;
            TxtAddress.Text = FirebaseService.CurrentAddress;
            _currentAvatarPath = FirebaseService.CurrentAvatarUrl;

            if (!string.IsNullOrEmpty(_currentAvatarPath) && File.Exists(_currentAvatarPath))
            {
                AvatarImage.Source = new BitmapImage(new Uri(_currentAvatarPath, UriKind.Absolute));
            }
        }

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Copy file to local app folder for persistence (simulate upload)
                    string uploadsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string ext = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = $"{FirebaseService.CurrentUsername}_{DateTime.Now.Ticks}{ext}";
                    string destPath = Path.Combine(uploadsFolder, newFileName);
                    
                    File.Copy(openFileDialog.FileName, destPath, true);
                    
                    _currentAvatarPath = destPath;
                    AvatarImage.Source = new BitmapImage(new Uri(destPath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tải ảnh: " + ex.Message);
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string newHometown = TxtHometown.Text.Trim();
            string newAddress = TxtAddress.Text.Trim();
            string newPassword = TxtNewPassword.Password;
            string confirmPassword = TxtConfirmPassword.Password;

            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (newPassword.Length < 6)
                {
                    MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            BtnSave.IsEnabled = false;
            BtnSave.Content = "Đang lưu...";

            try
            {
                // Update profile in Firebase RTDB
                var (profSuccess, profMsg) = await FirebaseService.UpdateProfileAsync(newHometown, newAddress, _currentAvatarPath ?? "");
                if (!profSuccess)
                {
                    MessageBox.Show(profMsg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update profile in SQL DBs
                if (!string.IsNullOrEmpty(FirebaseService.CurrentEmail))
                {
                    await DatabaseSyncService.UpdateUserProfileAsync(FirebaseService.CurrentEmail, newHometown, newAddress, _currentAvatarPath ?? "");
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(newPassword))
                {
                    var (pwSuccess, pwMsg) = await FirebaseService.ChangePasswordAsync(newPassword);
                    if (!pwSuccess)
                    {
                        MessageBox.Show(pwMsg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (!string.IsNullOrEmpty(FirebaseService.CurrentEmail))
                    {
                        await DatabaseSyncService.UpdatePasswordAsync(FirebaseService.CurrentEmail, newPassword);
                    }
                    MessageBox.Show("Cập nhật thông tin và mật khẩu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                BtnSave.Content = "Lưu thay đổi";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
