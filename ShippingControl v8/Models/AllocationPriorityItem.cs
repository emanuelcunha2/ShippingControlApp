using ShippingControl_v8.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class AllocationPriorityItem  : ViewModelBase
    {

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

        private int _countBoxes = 0;
        public int CountBoxes
        {
            get => _countBoxes;
            set
            {
                _countBoxes = value;
                OnPropertyChanged();
            }
        }

        private int _maxBoxes = 0;
        public int MaxBoxes
        {
            get => _maxBoxes;
            set
            {
                _maxBoxes = value;
                OnPropertyChanged();
            }
        }


        private int _numBins = 0;
        public int NumBins
        {
            get => _numBins;
            set
            {
                _numBins = value;
                OnPropertyChanged();
            }
        }

        private string _boxType = "";
        public string BoxType
        {
            get => _boxType;
            set
            {
                _boxType = value;
                OnPropertyChanged();
            }
        }


    }
}
