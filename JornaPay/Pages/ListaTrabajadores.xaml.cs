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
    private async Task MostrarErrorAsync(string mensaje)
    {
        await Application.Current.MainPage.DisplayAlert("Error", $"Ocurrió un problema: {mensaje}", "OK");
    }

    private async Task InscribirTrabajador(string nombre, string apellidos, decimal precioPorHora)
    {
        try
        {
            await Shell.Current.GoToAsync($"NuevoTrabajador?nombre={nombre}&apellidos={apellidos}&precioPorHora={precioPorHora}");
        }
        catch (Exception ex)
        {
            await MostrarErrorAsync($"Error al inscribir el trabajador: {ex.Message}");
        }
    }
}