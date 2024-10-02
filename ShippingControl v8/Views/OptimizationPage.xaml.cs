using ShippingControl_v8.Models;

using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui;
using ShippingControl_v8.Commands;
using ShippingControl_v8.Models;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ShippingControl_v8.ViewModels;

namespace ShippingControl_v8.Views;

public partial class OptimizationPage : ContentPage
{
	private OptimizationViewModel _viewmodel;
	public OptimizationPage()
	{
		InitializeComponent();
		_viewmodel = new OptimizationViewModel();
		BindingContext = _viewmodel;
    } 
}