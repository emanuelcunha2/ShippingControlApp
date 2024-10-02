using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;
public partial class TransferPage : ContentPage
{

    private TransferPageViewModel viewmodel;
    public TransferPage()
    {
        viewmodel = new(this.Navigation);
        this.BindingContext = viewmodel;
        InitializeComponent();
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ((TransferPageViewModel)this.BindingContext).Unsubscribe();
    }
}