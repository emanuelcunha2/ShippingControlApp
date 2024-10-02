using Microsoft.Data.SqlClient;
using ShippingControl_v8.ViewModels;
using System.Data;
using System.Diagnostics;
using System.Text; 
using ShippingControl_v8.Models; 

namespace ShippingControl_v8;
public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
        this.BindingContext = new MainPageViewModel(this.Navigation);
        InitializeComponent(); 
	}
    private bool firstAppearence = true;
    protected override void OnAppearing()
    {
        base.OnAppearing(); 

        if(!firstAppearence) { return; }

        firstAppearence = false;

        // Schedule a callback to be executed after a delay (e.g., 100 milliseconds).
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            AppSettings settings = new AppSettings();
            settings.GetAppSettings();
            StartUpDatabaseConnection(settings);

            var vm = (MainPageViewModel)(this.BindingContext);
            vm.CheckUser();
            return false; // Return false to stop the timer from repeating.
        });
    }

    private void StartUpDatabaseConnection(AppSettings ApplicationSettings)
    {
        SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
            .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
            .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
        SqlCommand cmd = new SqlCommand("SELECT TOP (1) [vs_id] FROM [dbo].[tbVS]", cnn);
        SqlDataReader reader = null;

        try
        {
            cnn.Open();
            reader = cmd.ExecuteReader();
        }
        catch (SqlException ex)
        {
            Debug.WriteLine(ex.Message);
            return;
        }
        finally
        {
            if (reader != null && !reader.IsClosed)
            {
                reader.Close();
            }

            if (cmd != null)
            {
                cmd.Dispose();
            }

            if (cnn != null && cnn.State == ConnectionState.Open)
            {
                cnn.Close();
                cnn.Dispose();
            }
        }
    }



}

