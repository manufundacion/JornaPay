namespace JornaPay.Pages;
using JornaPay.ViewModels;
using Microsoft.Maui.Controls;
using JornaPay.Models;

[QueryProperty(nameof(Nombre), "nombre")]
[QueryProperty(nameof(Apellidos), "apellidos")]
[QueryProperty(nameof(PrecioPorHora), "precioPorHora")]
public partial class NuevoTrabajador : ContentPage
{
    public NuevoTrabajadorViewModels ViewModel { get; private set; }

    public string Nombre { get; set; }
    public string Apellidos { get; set; }
    public decimal PrecioPorHora { get; set; }

    public NuevoTrabajador(string nombre, string apellidos, decimal precioPorHora)
    {
        InitializeComponent();

        Nombre = nombre;
        Apellidos = apellidos;
        PrecioPorHora = precioPorHora;

        ViewModel = new NuevoTrabajadorViewModels(Nombre, Apellidos, PrecioPorHora);
        BindingContext = ViewModel;

        Title = $"{Nombre} {Apellidos}";

        // Suscribirse al mensaje para recargar la página
        MessagingCenter.Subscribe<NuevoTrabajadorViewModels>(this, "RecargarHistorial", async (sender) =>
        {
            await RecargarPagina();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MessagingCenter.Unsubscribe<NuevoTrabajadorViewModels>(this, "RecargarHistorial");
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        if (ViewModel == null)
        {
            ViewModel = new NuevoTrabajadorViewModels(Nombre, Apellidos, PrecioPorHora);
            BindingContext = ViewModel;
        }
        await ViewModel.CargarHistorialAsync();
    }

    // Método público para recargar el historial desde fuera sin llamar OnAppearing
    public async Task RecargarPagina()
    {
        await CargarDatosAsync();
    }
}
