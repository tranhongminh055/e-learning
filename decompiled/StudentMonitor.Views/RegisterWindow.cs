using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Newtonsoft.Json;

namespace StudentMonitor.Views;

public class RegisterWindow : Window, IComponentConnector
{
	private static readonly HttpClient _httpClient = new HttpClient();

	private const string API_BASE_URL = "http://192.168.110.53:5000/api";

	internal Border ErrorBorder;

	internal TextBlock ErrorMessage;

	internal TextBox FullNameTextBox;

	internal TextBlock FullNamePlaceholder;

	internal TextBox EmailTextBox;

	internal TextBlock EmailPlaceholder;

	internal TextBox UsernameTextBox;

	internal TextBlock UsernamePlaceholder;

	internal ComboBox RoleComboBox;

	internal TextBox StudentIdTextBox;

	internal TextBlock StudentIdPlaceholder;

	internal PasswordBox PasswordBox;

	internal TextBlock PasswordPlaceholder;

	internal PasswordBox ConfirmPasswordBox;

	internal TextBlock ConfirmPasswordPlaceholder;

	internal Button RegisterButton;

	internal Button LoginLink;

	private bool _contentLoaded;

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
			var registerData = new
			{
				full_name = fullName,
				email = email,
				username = username,
				student_id = studentId,
				password = password,
				role = role
			};
			string json = JsonConvert.SerializeObject(registerData);
			StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
			HttpResponseMessage response = await _httpClient.PostAsync("http://192.168.110.53:5000/api/auth/register", content);
			string responseBody = await response.Content.ReadAsStringAsync();
			if (response.IsSuccessStatusCode)
			{
				MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				LoginWindow loginWindow = new LoginWindow();
				loginWindow.Show();
				Close();
			}
			else
			{
				dynamic error = JsonConvert.DeserializeObject(responseBody);
				string message = error.message ?? "Đăng ký thất bại.";
				ShowError(message);
			}
		}
		catch (HttpRequestException)
		{
			ShowError("Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.");
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			ShowError("Lỗi: " + ex3.Message);
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/StudentMonitor;component/views/registerwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			ErrorBorder = (Border)target;
			break;
		case 2:
			ErrorMessage = (TextBlock)target;
			break;
		case 3:
			FullNameTextBox = (TextBox)target;
			FullNameTextBox.GotFocus += FullNameTextBox_GotFocus;
			FullNameTextBox.LostFocus += FullNameTextBox_LostFocus;
			break;
		case 4:
			FullNamePlaceholder = (TextBlock)target;
			break;
		case 5:
			EmailTextBox = (TextBox)target;
			EmailTextBox.GotFocus += EmailTextBox_GotFocus;
			EmailTextBox.LostFocus += EmailTextBox_LostFocus;
			break;
		case 6:
			EmailPlaceholder = (TextBlock)target;
			break;
		case 7:
			UsernameTextBox = (TextBox)target;
			UsernameTextBox.GotFocus += UsernameTextBox_GotFocus;
			UsernameTextBox.LostFocus += UsernameTextBox_LostFocus;
			break;
		case 8:
			UsernamePlaceholder = (TextBlock)target;
			break;
		case 9:
			RoleComboBox = (ComboBox)target;
			break;
		case 10:
			StudentIdTextBox = (TextBox)target;
			StudentIdTextBox.GotFocus += StudentIdTextBox_GotFocus;
			StudentIdTextBox.LostFocus += StudentIdTextBox_LostFocus;
			break;
		case 11:
			StudentIdPlaceholder = (TextBlock)target;
			break;
		case 12:
			PasswordBox = (PasswordBox)target;
			PasswordBox.GotFocus += PasswordBox_GotFocus;
			PasswordBox.LostFocus += PasswordBox_LostFocus;
			break;
		case 13:
			PasswordPlaceholder = (TextBlock)target;
			break;
		case 14:
			ConfirmPasswordBox = (PasswordBox)target;
			ConfirmPasswordBox.GotFocus += ConfirmPasswordBox_GotFocus;
			ConfirmPasswordBox.LostFocus += ConfirmPasswordBox_LostFocus;
			break;
		case 15:
			ConfirmPasswordPlaceholder = (TextBlock)target;
			break;
		case 16:
			RegisterButton = (Button)target;
			RegisterButton.Click += RegisterButton_Click;
			break;
		case 17:
			LoginLink = (Button)target;
			LoginLink.Click += LoginLink_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
