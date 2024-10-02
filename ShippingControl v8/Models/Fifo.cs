using ShippingControl_v8.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class Fifo : ViewModelBase
    {
      

        private Color _backgroundColor = Colors.White;
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                _backgroundColor = value;
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
        private string _order = string.Empty;
        public string Order
        {
            get { return _order; }
            set
            {
                if (_order != value)
                {
                    _order = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _boxesHeight = 0;
        public double BoxesHeight
        {
            get => _boxesHeight;
            set
            {
                if (_boxesHeight != value)
                {
                    _boxesHeight = value;
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

 
 
        private ObservableCollection<Box> _boxes { get; set; } = new ObservableCollection<Box>();
        public ObservableCollection<Box> boxes
        {
            get => _boxes;
            set
            {
                _boxes = value;
                OnPropertyChanged();
            }
        }

        private string _countBoxes = string.Empty;
        public string countBoxes
        {
            get => _countBoxes;
            set
            {
                _countBoxes = value;
                OnPropertyChanged();
            }
        }

        public string IsFull { get; set; } = "0";

        public ObservableCollection<Box> DisplayedBoxes { get; set; } = new ObservableCollection<Box>();

        public Fifo()
        {
            DisplayedBoxes.CollectionChanged += CollectionChangedHandler;
            boxes.CollectionChanged += CollectionChangedHandler;
        }

        public void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            BoxesHeight = DisplayedBoxes.Count * 40;
            countBoxes = boxes.Count.ToString() + " Caixa(s)";
        }
    }
}
