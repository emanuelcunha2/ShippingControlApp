using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;
public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		this.BindingContext = new SettingsViewModel(this.Navigation);
	}
}