
using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;
public partial class AllocationPage : ContentPage
{
    private AllocationViewModel viewmodel;
	public AllocationPage()
	{
        viewmodel = new(this.Navigation);
        this.BindingContext = viewmodel;

        InitializeComponent();
        Preferences.Default.Set("hasSettings", "true");

        var settingsFound = Preferences.Default.Get("hasSettings", "false");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ((AllocationViewModel)this.BindingContext).Unsubscribe();
    }
}