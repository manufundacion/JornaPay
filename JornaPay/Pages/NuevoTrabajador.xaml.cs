using JornaPay.ViewModels;
using Microsoft.Maui.Controls;
using JornaPay.Models;

namespace JornaPay.Pages;

public partial class NuevoTrabajador : ContentPage
{
    private NuevoTrabajadorViewModels _viewModel;

    public NuevoTrabajador(string nombre, string apellidos, decimal precioPorHora)
    {
        InitializeComponent();

        // Asignar el BindingContext correctamente
        _viewModel = new NuevoTrabajadorViewModels(nombre, apellidos, precioPorHora);
        BindingContext = _viewModel;

        Title = $"{nombre} {apellidos}";
    }

    // Se ejecuta cuando la vista está lista para mostrar datos
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.CargarHistorialAsync(); // 🔥 Ahora carga el historial AL abrir la página
    }

    private void OnItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (BindingContext is NuevoTrabajadorViewModels viewModel)
        {
            viewModel.ElementoSeleccionado = e.Item as TrabajadorDatos;
        }
    }
}
