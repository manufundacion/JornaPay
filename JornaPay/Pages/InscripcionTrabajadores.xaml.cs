using JornaPay.ViewModels;

namespace JornaPay.Pages;

public partial class InscripcionTrabajadores : ContentPage
{
	public InscripcionTrabajadores()
	{
		InitializeComponent();
        BindingContext = new TrabajadoresViewModels();
    }
}