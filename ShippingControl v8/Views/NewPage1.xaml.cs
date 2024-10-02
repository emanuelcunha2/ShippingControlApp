using ShippingControl_v8.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShippingControl_v8.Views;

public partial class NewPage1 : ContentPage
{
    public MainPageViewModel VmParent { get; set; }
	public NewPage1(MainPageViewModel parent)
	{
        VmParent = parent; 
        InitializeComponent();
        CardNrEntry.Text = "";
    }
    protected override bool OnBackButtonPressed()
    {
        return false; 
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
       VmParent.ScanTriggered(CardNrEntry.Text);
    }

    private void CardNrEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        VmParent.CardUser = CardNrEntry.Text;
    }
}