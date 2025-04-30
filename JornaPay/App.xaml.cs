using JornaPay.Pages;

namespace JornaPay
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new InicioRegistro());
        }
    }
}
