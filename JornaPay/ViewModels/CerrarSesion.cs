namespace JornaPay.Pages;
using Microsoft.Maui.Controls;

public partial class CerrarSesion : ContentPage 
{
    public CerrarSesion()
    {
        CerrarSesionYRedirigir();
    }

    private void CerrarSesionYRedirigir()
    {
        // Redirijo a InicioRegistro
        Application.Current.MainPage = new NavigationPage(new InicioRegistro());
    }
}
