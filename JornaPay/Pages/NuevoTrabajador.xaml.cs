namespace JornaPay.Pages;
using JornaPay.ViewModels;
using Microsoft.Maui.Controls;
using JornaPay.Models;

[QueryProperty(nameof(Nombre), "nombre")]
[QueryProperty(nameof(Apellidos), "apellidos")]
[QueryProperty(nameof(PrecioPorHora), "precioPorHora")]
public partial class NuevoTrabajador : ContentPage
{
    private NuevoTrabajadorViewModels _viewModel;

    public string Nombre { get; set; }
    public string Apellidos { get; set; }
    public decimal PrecioPorHora { get; set; }

    public NuevoTrabajador(string nombre, string apellidos, decimal precioPorHora)
    {
        InitializeComponent();

        Nombre = nombre;
        Apellidos = apellidos;
        PrecioPorHora = precioPorHora;

        _viewModel = new NuevoTrabajadorViewModels(Nombre, Apellidos, PrecioPorHora);
        BindingContext = _viewModel;

        Title = $"{Nombre} {Apellidos}";
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrEmpty(Nombre) && !string.IsNullOrEmpty(Apellidos))
        {
            _viewModel = new NuevoTrabajadorViewModels(Nombre, Apellidos, PrecioPorHora);
            BindingContext = _viewModel;
            Title = $"{Nombre} {Apellidos}";
        }

        if (_viewModel != null)
        {
            await _viewModel.CargarHistorialAsync();
        }
    }
}
