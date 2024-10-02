using System.ComponentModel; 
using System.Runtime.CompilerServices; 

namespace ShippingControl_v8.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        // Implement the INotifyPropertyChanged interface to notify the UI of property changes
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
