using JornaPay.ViewModels;
using JornaPay.Services;

namespace JornaPay.Pages;

public partial class ImportesPagados : ContentPage
{
    private ImportesPagadosViewModel _viewModel;

    public ImportesPagados()
    {
        InitializeComponent();
        _viewModel = new ImportesPagadosViewModel(TrabajadoresServicio.GetInstance());
        BindingContext = _viewModel;
        _viewModel.BuscarCommand.Execute(null);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.BuscarCommand.Execute(null); //Actualizar automáticamente al abrir la pantalla
    }
}