using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Newtonsoft.Json;

namespace StudentMonitor.Views;

public class LoginWindow : Window, IComponentConnector
{
	private static readonly HttpClient _httpClient = new HttpClient();

	private const string API_BASE_URL = "http://192.168.110.53:5000/api";

	internal Border ErrorBorder;

	internal TextBlock ErrorMessage;

	internal Border SuccessBorder;

	internal TextBlock SuccessMessage;

	internal TextBox UsernameTextBox;

	internal TextBlock UsernamePlaceholder;

	internal PasswordBox PasswordBox;

	internal TextBlock PasswordPlaceholder;

	internal Button LoginButton;

	internal Button RegisterLink;

	private bool _contentLoaded;

	public LoginWindow()
	{
		InitializeComponent();
	}

	private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
	{
		UsernamePlaceholder.Visibility = Visibility.Collapsed;
	}

	private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(UsernameTextBox.Text))
		{
			UsernamePlaceholder.Visibility = Visibility.Visible;
		}
	}

	private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
	{
		PasswordPlaceholder.Visibility = Visibility.Collapsed;
	}

	private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(PasswordBox.Password))
		{
			PasswordPlaceholder.Visibility = Visibility.Visible;
		}
	}

	private async void LoginButton_Click(object sender, RoutedEventArgs e)
	{
		string username = UsernameTextBox.Text.Trim();
		string password = PasswordBox.Password;
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
		{
			ShowError("Vui lòng nhập tên đăng nhập và mật khẩu.");
			return;
		}
		HideMessages();
		LoginButton.IsEnabled = false;
		LoginButton.Content = "Đang đăng nhập...";
		try
		{
			var loginData = new { username, password };
			string json = JsonConvert.SerializeObject(loginData);
			StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
			HttpResponseMessage response = await _httpClient.PostAsync("http://192.168.110.53:5000/api/auth/login", content);
			string responseBody = await response.Content.ReadAsStringAsync();
			if (response.IsSuccessStatusCode)
			{
				dynamic result = JsonConvert.DeserializeObject(responseBody);
				_ = (string)result.token;
				string role = result.role;
				string fullName = result.full_name;
				if (role == "student" || role == "SinhVien")
				{
					StudentWindow studentWindow = new StudentWindow(fullName, username);
					studentWindow.Show();
				}
				else
				{
					MainWindow mainWindow = new MainWindow();
					mainWindow.Title = "E-Learning - Xin chào " + fullName;
					mainWindow.Show();
				}
				Close();
			}
			else
			{
				dynamic error = JsonConvert.DeserializeObject(responseBody);
				string message = error.message ?? "Đăng nhập thất bại.";
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
			LoginButton.IsEnabled = true;
			LoginButton.Content = "Đăng nhập";
		}
	}

	private void RegisterLink_Click(object sender, RoutedEventArgs e)
	{
		RegisterWindow registerWindow = new RegisterWindow();
		registerWindow.Show();
		Close();
	}

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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/StudentMonitor;component/views/loginwindow.xaml", UriKind.Relative);
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
			SuccessBorder = (Border)target;
			break;
		case 4:
			SuccessMessage = (TextBlock)target;
			break;
		case 5:
			UsernameTextBox = (TextBox)target;
			UsernameTextBox.GotFocus += UsernameTextBox_GotFocus;
			UsernameTextBox.LostFocus += UsernameTextBox_LostFocus;
			break;
		case 6:
			UsernamePlaceholder = (TextBlock)target;
			break;
		case 7:
			PasswordBox = (PasswordBox)target;
			PasswordBox.GotFocus += PasswordBox_GotFocus;
			PasswordBox.LostFocus += PasswordBox_LostFocus;
			break;
		case 8:
			PasswordPlaceholder = (TextBlock)target;
			break;
		case 9:
			LoginButton = (Button)target;
			LoginButton.Click += LoginButton_Click;
			break;
		case 10:
			RegisterLink = (Button)target;
			RegisterLink.Click += RegisterLink_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
