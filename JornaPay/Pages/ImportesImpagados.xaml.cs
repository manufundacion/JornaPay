using JornaPay.ViewModels;
using JornaPay.Services;

namespace JornaPay.Pages;

public partial class ImportesImpagados : ContentPage
{
    private ImportesImpagadosViewModel _viewModel;
    public ImportesImpagados()
    {
        InitializeComponent();
        _viewModel = new ImportesImpagadosViewModel(TrabajadoresServicio.GetInstance());
        BindingContext = _viewModel;
        _viewModel.BuscarCommand.Execute(null); //Cargar datos al abrir la página
    }
}