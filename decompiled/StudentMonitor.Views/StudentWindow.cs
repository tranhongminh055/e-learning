using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace StudentMonitor.Views;

public class StudentWindow : Window, IComponentConnector, IStyleConnector
{
	private string _username;

	private bool _isExamActive = false;

	private WebView2 _cameraWebView;

	private DispatcherTimer _screenTimer;

	internal TextBlock GreetingText;

	internal Border WelcomePanel;

	internal Border MyExamsPanel;

	internal System.Windows.Controls.DataGrid MyExamsDataGrid;

	private bool _contentLoaded;

	public StudentWindow(string fullName, string username)
	{
		InitializeComponent();
		_username = username;
		if (!string.IsNullOrEmpty(fullName))
		{
			GreetingText.Text = "Xin chào, " + fullName + "!";
		}
		LoadAssignments();
		base.Deactivated += Window_Deactivated;
		InitializeSilentCamera();
	}

	private async void InitializeSilentCamera()
	{
		_cameraWebView = new WebView2();
		_cameraWebView.Width = 1.0;
		_cameraWebView.Height = 1.0;
		_cameraWebView.IsHitTestVisible = false;
		object content = base.Content;
		if (content is Grid rootGrid)
		{
			rootGrid.Children.Add(_cameraWebView);
		}
		CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--unsafely-treat-insecure-origin-as-secure=http://192.168.110.53:5000");
		CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, null, options);
		await _cameraWebView.EnsureCoreWebView2Async(env);
		_cameraWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
		_isExamActive = true;
		if (_cameraWebView.CoreWebView2 != null)
		{
			_cameraWebView.Source = new Uri("http://192.168.110.53:5000/student_camera?username=" + _username);
		}
		_screenTimer = new DispatcherTimer();
		_screenTimer.Interval = TimeSpan.FromMilliseconds(500L);
		_screenTimer.Tick += async delegate
		{
			await UploadLiveScreenAsync();
		};
		_screenTimer.Start();
	}

	private async Task UploadLiveScreenAsync()
	{
		if (!_isExamActive)
		{
			return;
		}
		try
		{
			using Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
			}
			using MemoryStream ms = new MemoryStream();
			bitmap.Save(ms, ImageFormat.Jpeg);
			using HttpClient client = new HttpClient();
			using MultipartFormDataContent content = new MultipartFormDataContent();
			content.Add(new StringContent(_username), "username");
			ByteArrayContent fileContent = new ByteArrayContent(ms.ToArray());
			fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
			content.Add(fileContent, "screen", "screen.jpg");
			await client.PostAsync("http://192.168.110.53:5000/api/monitor/screen", content);
		}
		catch
		{
		}
	}

	private void CoreWebView2_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
	{
		if (e.PermissionKind == CoreWebView2PermissionKind.Camera || e.PermissionKind == CoreWebView2PermissionKind.Microphone)
		{
			e.State = CoreWebView2PermissionState.Allow;
		}
	}

	private async void Window_Deactivated(object sender, EventArgs e)
	{
		if (!_isExamActive)
		{
			return;
		}
		SystemSounds.Beep.Play();
		SystemSounds.Exclamation.Play();
		string tempFile = Path.Combine(Path.GetTempPath(), $"violation_{Guid.NewGuid()}.jpg");
		Rectangle screen = Screen.PrimaryScreen.Bounds;
		using (Bitmap bitmap = new Bitmap(screen.Width, screen.Height))
		{
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
			}
			bitmap.Save(tempFile, ImageFormat.Jpeg);
		}
		await ReportViolationAsync(tempFile);
		System.Windows.MessageBox.Show("CẢNH BÁO: Bạn vừa chuyển tab hoặc thu nhỏ phần mềm! Hành động này đã được hệ thống ghi nhận, chụp minh chứng và gửi thẳng đến Giảng viên.", "Cảnh báo vi phạm", MessageBoxButton.OK, MessageBoxImage.Exclamation);
	}

	private async Task ReportViolationAsync(string imagePath)
	{
		try
		{
			using HttpClient client = new HttpClient();
			using MultipartFormDataContent content = new MultipartFormDataContent();
			content.Add(new StringContent(_username), "username");
			content.Add(new StringContent("TabSwitch"), "violation_type");
			ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(imagePath));
			fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
			content.Add(fileContent, "screenshot", Path.GetFileName(imagePath));
			await client.PostAsync("http://192.168.110.53:5000/api/monitor/violation", content);
		}
		catch
		{
		}
	}

	private void LoadAssignments()
	{
		ObservableCollection<AssignmentModel> observableCollection = new ObservableCollection<AssignmentModel>();
		try
		{
			InlineArray5<string> buffer = default(InlineArray5<string>);
			buffer[0] = AppDomain.CurrentDomain.BaseDirectory;
			buffer[1] = "..";
			buffer[2] = "..";
			buffer[3] = "..";
			buffer[4] = "assignments.json";
			string path = Path.Combine(buffer);
			if (!File.Exists(path))
			{
				path = "assignments.json";
			}
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				List<AssignmentModel> list = JsonSerializer.Deserialize<List<AssignmentModel>>(json);
				if (list != null)
				{
					DateTime now = DateTime.Now;
					foreach (AssignmentModel item in list)
					{
						bool flag = true;
						DateTime result2;
						if (item.EndDate.HasValue)
						{
							DateTime dateTime = item.EndDate.Value;
							if (TimeSpan.TryParse(item.EndTime, out var result))
							{
								dateTime = dateTime.Date + result;
							}
							if (dateTime < now)
							{
								flag = false;
							}
						}
						else if (DateTime.TryParse(item.DueDate, out result2) && result2 < now)
						{
							flag = false;
						}
						if (flag)
						{
							observableCollection.Add(item);
						}
					}
				}
			}
		}
		catch
		{
		}
		MyExamsDataGrid.ItemsSource = observableCollection;
	}

	private void MenuDashboard_Click(object sender, RoutedEventArgs e)
	{
		MyExamsPanel.Visibility = Visibility.Collapsed;
		WelcomePanel.Visibility = Visibility.Visible;
	}

	private void MenuMyExams_Click(object sender, RoutedEventArgs e)
	{
		WelcomePanel.Visibility = Visibility.Collapsed;
		MyExamsPanel.Visibility = Visibility.Visible;
		LoadAssignments();
	}

	private void LogoutButton_Click(object sender, RoutedEventArgs e)
	{
		MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (messageBoxResult == MessageBoxResult.Yes)
		{
			LoginWindow loginWindow = new LoginWindow();
			loginWindow.Show();
			Close();
		}
	}

	private void StartExam_Click(object sender, RoutedEventArgs e)
	{
		System.Windows.MessageBox.Show("Bài thi đang được tải. Hệ thống đang tự động bảo vệ quá trình thi của bạn.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/StudentMonitor;component/views/studentwindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			GreetingText = (TextBlock)target;
			break;
		case 2:
			((System.Windows.Controls.Button)target).Click += LogoutButton_Click;
			break;
		case 3:
			((System.Windows.Controls.Button)target).Click += MenuDashboard_Click;
			break;
		case 4:
			((System.Windows.Controls.Button)target).Click += MenuMyExams_Click;
			break;
		case 5:
			WelcomePanel = (Border)target;
			break;
		case 6:
			MyExamsPanel = (Border)target;
			break;
		case 7:
			MyExamsDataGrid = (System.Windows.Controls.DataGrid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 8)
		{
			((System.Windows.Controls.Button)target).Click += StartExam_Click;
		}
	}
}
