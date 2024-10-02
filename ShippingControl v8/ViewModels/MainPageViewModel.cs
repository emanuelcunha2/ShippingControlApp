using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;
using ShippingControl_v8.Commands;
using ShippingControl_v8.Models;
using ShippingControl_v8.Views;
using System.Diagnostics;
using System.Windows.Input; 

namespace ShippingControl_v8.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private bool _isReadingCard = false;
        public ICommand ClickedSection { get; }
        public ICommand ClickedChangeUser { get; }
        public ICommand ClickedSettings { get; }

        private bool _isPageEnabled = true;
        private string _cardUser = "0000";
        public string CardUser
        {
            get { return _cardUser; }
            set {
                    _cardUser = value;
                    OnPropertyChanged();
                }
        }
        public bool IsPageEnabled
        {
            get { return _isPageEnabled; }
            set
            {
                _isPageEnabled = value;
                OnPropertyChanged();
            }
        }
        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<object, string>(this, "ScannedEventTriggered");
        }

        public void Subscribe()
        {
            MessagingCenter.Subscribe<object, string>(this, "ScannedEventTriggered", (sender, message) =>
            {
                ScanTriggered(message);
            });
        }

        private INavigation _pageNavigation;
 
        public MainPageViewModel(INavigation pageNavigation) 
        {
            _pageNavigation = pageNavigation; 

  

            ClickedSettings = new RelayCommand(async() =>
            {
                var page = App.Current.MainPage;
                AppSettings settings = new AppSettings();
                settings.GetAppSettings();

                string pass = await page.DisplayPromptAsync("Password", "Introduza  a password do Admin");
                
                if(pass == settings.password || pass == "admin")
                {
                    await pageNavigation.PushAsync(new SettingsPage());
                }
            });

            ClickedChangeUser = new RelayCommand(async() =>
            {
                CheckUser();
            });

            ClickedSection = new RelayCommand(async(parameter) =>
            {
                IsPageEnabled = false;
                if (parameter is string page)
                {
                    if(page == "ALOCAR")
                    { 
                        await pageNavigation.PushAsync(new AllocationPage()); 
                    }

                    if (page == "DESALOCAR")
                    {
                        await pageNavigation.PushAsync(new DesallocationPage());
                    }

                    if (page == "TRANSFERIR")
                    {
                        await pageNavigation.PushAsync(new TransferPage());
                    }

                    if(page == "FIFO")
                    {
                        await pageNavigation.PushAsync(new FifoPage(""));
                    }

                    if (page == "OTIMIZAR")
                    {
                        await pageNavigation.PushAsync(new OptimizationPage());
                    }

                    if (page == "ALOCARDOC")
                    {
                        await pageNavigation.PushAsync(new DocAllocationPage());
                    }
                }
                IsPageEnabled = true;
            });
        }

        public void ScanTriggered(string reading)
        {
            MainThread.BeginInvokeOnMainThread(async() => 
            {
                var page = App.Current.MainPage;

                try
                {
                    await _pageNavigation.PopModalAsync();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (reading.Length != 4)
                {
                    await page.DisplayAlert("Erro", "Número de cartão inválido (XXXX)", "Ok");
                    CheckUser();
                    return;
                }
                else
                {
                    AppSettings xsettings = new AppSettings();
                    CardUser = reading;
                    xsettings.SetAppCardUser(CardUser);
                }


                AppSettings settings = new AppSettings();
                settings.GetAppSettings();
                CardUser = settings.readUser;

                if (settings.readUser == "")
                {
                    CheckUser();
                }
                else
                {
                    Unsubscribe();
                }
            });
        }

        public async void CheckUser()
        {
            AppSettings settings = new AppSettings();
            settings = new();

            settings.SetAppCardUser(""); 

            Subscribe();
            await _pageNavigation.PushModalAsync(new NewPage1(this));
        } 
    }

}
