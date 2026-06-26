using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace StudentMonitor;

public class App : Application
{
	private bool _contentLoaded;

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.StartupUri = new Uri("Views/LoginWindow.xaml", UriKind.Relative);
			Uri resourceLocator = new Uri("/StudentMonitor;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
