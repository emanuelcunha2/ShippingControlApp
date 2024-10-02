using ShippingControl_v8.Commands;
using ShippingControl_v8.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShippingControl_v8.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public AppSettings Settings { get; set; } = new AppSettings();

        private bool hasPreviousSettings = false;
        public ICommand SaveSettings { get; }
        public ICommand DefaultSettings { get; }
        public SettingsViewModel(INavigation navigation) 
        {

            Settings.GetAppSettings();
            hasPreviousSettings = Settings.VerifyAllSettingsSet();

            DefaultSettings = new RelayCommand(() => 
            {
                Settings.posto = "ShipEd1Scan3";
                Settings.szIPSelected = "130.171.191.142";
                Settings.sDB = "pyms";
                Settings.sUser = "passreg";
                Settings.sPrinter = "unknown";
                Settings.iLocal = 1;
            });

            SaveSettings = new RelayCommand(async() =>
            {
                var page = App.Current.MainPage;
                if (Settings.VerifyAllSettingsSet())
                {
                    if (!hasPreviousSettings)
                    {
                        string pass = await page.DisplayPromptAsync("Primeira Configuração", "Introduza uma password nova");
                        Settings.SetAppPassword(pass);
                    }

                    Settings.SetAppSettings();
                    App.Current.MainPage = new NavigationPage(new MainPage()) { BarBackgroundColor = Color.FromArgb("#333333"), Title = "ARMAZÉM", BarTextColor = Colors.White };
                }
                else
                {
                    await page.DisplayAlert("Erro", "Informações em falta!", "Ok");
                } 
            });

        }

        
    }
}
