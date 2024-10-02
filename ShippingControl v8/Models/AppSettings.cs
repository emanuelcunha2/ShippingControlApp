using ShippingControl_v8.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class AppSettings : ViewModelBase
    {
        private string _szIPSelected = "";							
        private string _sUser = "";								
        private string _sDB = "";									
        private string _posto = "";									
        private string _sPrinter = "";								
        private int _iLocal = -1;
        private string _readUser = "";

        public string password = string.Empty;
         
        public string szIPSelected
        {
            get { return _szIPSelected;}
            set
            {
                _szIPSelected = value;
                OnPropertyChanged();
            }
        }
        public string sUser
        {
            get { return _sUser; }
            set
            {
                _sUser = value;
                OnPropertyChanged();
            }
        }
        public string sDB
        {
            get { return _sDB; }
            set
            {
                _sDB = value;
                OnPropertyChanged();
            }
        }
        public string posto
        {
            get { return _posto; }
            set
            {
                _posto = value;
                OnPropertyChanged();
            }
        }

        public string sPrinter
        {
            get { return _sPrinter; }
            set
            {
                _sPrinter = value;
                OnPropertyChanged();
            }
        }

        public int iLocal
        {
            get { return _iLocal; }
            set
            {
                _iLocal = value;
                OnPropertyChanged();
            }
        }
        public string readUser
        {
            get { return _readUser; }
            set
            {
                _readUser = value;
                OnPropertyChanged();
            }
        }
        public AppSettings() 
        {
        
        }

        public void GetAppSettings()
        {
            szIPSelected = Preferences.Default.Get(nameof(_szIPSelected), "");
            sUser = Preferences.Default.Get(nameof(_sUser), "");
            sDB = Preferences.Default.Get(nameof(_sDB), "");
            posto = Preferences.Default.Get(nameof(_posto), "");
            sPrinter = Preferences.Default.Get(nameof(_sPrinter), "");
            iLocal = Preferences.Default.Get(nameof(_iLocal), -1);
            password = Preferences.Default.Get("controlPassword", "");
            readUser = Preferences.Default.Get("cardUser", "");
        }

        public void SetAppPassword(string pw)
        {
            Preferences.Default.Set("controlPassword", pw);
        }

        public void SetAppCardUser(string user)
        {
            Preferences.Default.Set("cardUser", user);
        }

        public void ResetAllSettings()
        {
            Preferences.Default.Set(nameof(_szIPSelected), "");
            Preferences.Default.Set(nameof(_sUser), "");
            Preferences.Default.Set(nameof(_sDB), "");
            Preferences.Default.Set(nameof(_posto), "");
            Preferences.Default.Set(nameof(_sPrinter), "");
            Preferences.Default.Set(nameof(_iLocal), -1);
        }

        public void SetAppSettings()
        {
            Preferences.Default.Set(nameof(_szIPSelected), _szIPSelected);
            Preferences.Default.Set(nameof(_sUser), _sUser);
            Preferences.Default.Set(nameof(_sDB), _sDB);
            Preferences.Default.Set(nameof(_posto), _posto);
            Preferences.Default.Set(nameof(_sPrinter), _sPrinter);
            Preferences.Default.Set(nameof(_iLocal), _iLocal);
        }

        public bool VerifyAllSettingsSet()
        {
            if (_szIPSelected == "" ||
                _sUser == "" ||
                _sDB == "" ||
                _posto == "" ||
                _sPrinter == "" ||
                _iLocal == -1) 
            { return false; }

            return true;
        }

    }
}
