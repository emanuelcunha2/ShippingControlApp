using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;

public partial class DocAllocationPage : ContentPage
{
    private DocAllocationViewModel viewmodel;

    public DocAllocationPage()
	{
        viewmodel = new();
        this.BindingContext = viewmodel;
        InitializeComponent();
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ((DocAllocationViewModel)this.BindingContext).Unsubscribe();
    }
}