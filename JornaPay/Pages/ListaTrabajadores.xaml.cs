namespace JornaPay.Pages;
using JornaPay.ViewModels;
public partial class ListaTrabajadores : ContentPage
{
    public ListaTrabajadores()
    {
        InitializeComponent();
        try
        {
            BindingContext = new TrabajadoresViewModels();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar el ViewModel: {ex.Message}");
            MostrarErrorAsync(ex.Message);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is TrabajadoresViewModels viewModel)
        {
            await viewModel.CargarTrabajadoresAsync();
        }
    }

    private async Task MostrarErrorAsync(string mensaje)
    {
        await Application.Current.MainPage.DisplayAlert("Error", $"Ocurrió un problema: {mensaje}", "OK");
    }
}
