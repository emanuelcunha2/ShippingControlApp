using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;
public partial class BoxQuantityPage : ContentPage
{
    private BoxQuantityViewModel viewmodel;
    public event EventHandler ModalClosed;

    public BoxQuantityPage(bool alloc, string partNumber)
	{
        InitializeComponent();
        viewmodel = new(alloc,this.Navigation, partNumber);
        this.BindingContext = viewmodel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ((BoxQuantityViewModel)this.BindingContext).Unsubscribe();
        ModalClosed?.Invoke(this, EventArgs.Empty);
    }
}