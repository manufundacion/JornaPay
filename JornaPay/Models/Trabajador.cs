using SQLite;

namespace JornaPay.Models
{
    public class Trabajador
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // Identificador único

        public string Nombre { get; set; } // Nombre del trabajador

        public string Apellidos { get; set; } // Apellidos del trabajador

        public decimal PrecioPorHora { get; set; } // Precio por hora trabajado

        public DateTime Fecha { get; set; } // Fecha de creación del registro

        public decimal HorasTrabajadas { get; set; } // Horas trabajadas totales

        public decimal ImporteTotal => HorasTrabajadas * PrecioPorHora; // Cálculo automático del total

        public bool Pagado { get; set; } // Estado de pago (true/false)

        // Propiedad adicional: Método para mostrar el estado de pago como texto
        public string EstadoPago => Pagado ? "Pagado" : "Pendiente";

        //Relación de usuario y el trabajador, clave foránea
        public string NombreUsuario { get; set; }
    }
}