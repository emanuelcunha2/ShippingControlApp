
using Microsoft.Data.SqlClient;
using ShippingControl_v8.Models;
using ShippingControl_v8.Views;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace ShippingControl_v8;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		AppSettings settings = new AppSettings(); 
        settings.GetAppSettings();

        if (settings.VerifyAllSettingsSet())
		{ 
            MainPage = new NavigationPage(new MainPage()) { BarBackgroundColor = Color.FromArgb("#333333"), Title = "ARMAZÉM", BarTextColor = Colors.White };
        }
		else
		{
            MainPage = new NavigationPage(new SettingsPage()) { BarBackgroundColor = Color.FromArgb("#333333"), Title = "DEFINIÇÕES", BarTextColor = Colors.White };
        }
		
	}  

}
