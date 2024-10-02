using ShippingControl_v8.Commands; 
using System.Text.RegularExpressions; 
using System.Windows.Input;

namespace ShippingControl_v8.ViewModels
{
    public class BoxQuantityViewModel : ViewModelBase
    {
        private int _qty = 1;
        private string _bin;
        private string _partNumber;
        private bool toAlloc;

        private bool _binVisibility = true;
        public bool BinVisibility
        {
            get { return _binVisibility; }
            set
            {
                _binVisibility = value;
                OnPropertyChanged();
            }
        }

        public string PartNumber
        {
            get { return _partNumber; }
            set 
            { 
                _partNumber = value;
                OnPropertyChanged();
            }
        }

        public int Qty
        {
            get { return _qty; }
            set 
            {
                _qty = value; 
                OnPropertyChanged();
            }
        }

        public string Bin
        {
            get { return _bin; }
            set { _bin = value; }
        }

        public bool WasModalConfirmed = false;
        public ICommand CloseModal { get; }
        public ICommand ConfirmModal { get; }
        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<object, string>(this, "ScannedEventTriggered");
        }

        public BoxQuantityViewModel(bool alloc, INavigation navigation, string partNumber) 
        {
            toAlloc = alloc;
            BinVisibility = !toAlloc;
            PartNumber = partNumber;

            CloseModal = new RelayCommand(async() =>
            {
                WasModalConfirmed = false;
                await navigation.PopModalAsync(true);
            });


            ConfirmModal = new RelayCommand(async () =>
            {
                WasModalConfirmed = true;
                await navigation.PopModalAsync(true);
            });

            MessagingCenter.Subscribe<object, string>(this, "ScannedEventTriggered", (sender, message) =>
            {
                ScanTriggered(message);
            });
        }

        public void ScanTriggered(string read)
        {
            if (!toAlloc && Regex.IsMatch(read, @"\d{3}-\d{3}"))
            {
                Bin = read;
            }
            else if(read.StartsWith("Q"))
            {
                if (Int32.TryParse(read,out int readInt))
                {
                    Qty = readInt;
                }
            }
        }

    }
}
