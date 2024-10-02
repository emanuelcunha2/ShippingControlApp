using ShippingControl_v8.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class Box : ViewModelBase
    {
        private string _tag = string.Empty;
        public string Tag
        {
            get { return _tag; }
            set
            {
                _tag = value;
                OnPropertyChanged();
            }
        }
        private string _type = string.Empty;
        public string type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }
        private string date;
        public string Date
        {
            get { return date; }
            set
            {
                date = value;
                OnPropertyChanged();
            }
        }
        private DateTime fifoDate;
        public DateTime FifoDate
        {
            get => fifoDate;
            set
            {
                fifoDate = value;
                OnPropertyChanged();
            }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                if (value) { BackgroundColor = Colors.LightSkyBlue; }
                else
                {
                    BackgroundColor = Colors.White;
                }

                OnPropertyChanged();
            }
        }

        private Color _backgroundColor = Colors.White;
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { 
                _backgroundColor = value;
                OnPropertyChanged(); 
            }
        }


        private string _bin = string.Empty;
        public string Bin
        {
            get { return _bin; }
            set
            {
                _bin = value;
                OnPropertyChanged();
            }
        }

        private bool _isBinOpened = false;
        public bool IsBinOpened
        {
            get { return _isBinOpened; }
            set
            {
                _isBinOpened = value;
                OnPropertyChanged();
            }
        }


        private string _partnumber = string.Empty;
        public string PartNumber
        {
            get { return _partnumber; }
            set
            {
                if (_partnumber != value)
                {
                    _partnumber = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
