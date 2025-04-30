namespace JornaPay.Models
{
    public static class SesionUsuario
    {
        public static string NombreUsuarioActual { get; set; } = string.Empty;

        public static void LimpiarSesion()
        {
            NombreUsuarioActual = string.Empty;
        }
    }
}