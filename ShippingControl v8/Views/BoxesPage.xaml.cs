using ShippingControl_v8.Models;
using ShippingControl_v8.ViewModels;
using System.Collections.ObjectModel;

namespace ShippingControl_v8.Views;
public partial class BoxesPage : ContentPage
{
    public event EventHandler ModalClosed;

    public BoxesPage(ObservableCollection<BoxQty> boxes)
	{
		InitializeComponent();
		this.BindingContext = new BoxesViewModel(this.Navigation, boxes);
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing(); 
        ModalClosed?.Invoke(this, EventArgs.Empty);
    }


    private void ListView_ItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if(sender is CollectionView list)
        {
            if(list.ItemsSource is ObservableCollection<BoxQty> boxes) 
            {
                foreach(BoxQty box in boxes)
                {
                    if(list.SelectedItem == box)
                    {
                        box.BackgroundColor = Color.FromArgb("#e3e3e3");
                    }
                    else
                    {
                        box.BackgroundColor = Colors.Transparent;
                    }
                }            
            }
        }
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }
}