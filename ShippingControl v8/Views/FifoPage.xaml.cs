using ShippingControl_v8.Models;
using ShippingControl_v8.ViewModels;
using System.Collections.ObjectModel;

namespace ShippingControl_v8.Views;
public partial class FifoPage : ContentPage
{
    private FifoViewModel _viewModel;
    public FifoPage(string pn)
	{
		InitializeComponent();
        _viewModel = new FifoViewModel(pn);
        this.BindingContext = _viewModel; 
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ((FifoViewModel)this.BindingContext).Unsubscribe();
    }

    public void InvalidateTheMeasure(object sender, EventArgs e)
    {
        this.InvalidateMeasure();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        InvalidateTheMeasure(new object(), new EventArgs());
    }
}