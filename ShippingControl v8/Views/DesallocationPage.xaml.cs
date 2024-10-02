using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;
public partial class DesallocationPage : ContentPage
{
    private DesallocationViewModel viewmodel;
    public DesallocationPage()
    {
        viewmodel = new(this.Navigation);
        this.BindingContext = viewmodel;

        InitializeComponent();
	}

    public async Task<bool> DisplayYesNoMessage(string message)
    {
        var result = await DisplayAlert("Confirmar", message, "Sim", "Não");
        return result;
    } 
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ((DesallocationViewModel)this.BindingContext).Unsubscribe();
    }

}