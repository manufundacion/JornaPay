using JornaPay.Pages;

namespace JornaPay
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Forzar tema claro
            Application.Current.UserAppTheme = AppTheme.Light;


            MainPage = new NavigationPage(new InicioRegistro());
        }
    }
}
