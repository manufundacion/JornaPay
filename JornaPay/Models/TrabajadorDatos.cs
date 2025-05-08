using SQLite;

namespace JornaPay.Models
{
    public class TrabajadorDatos
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // Identificador único del registro

        public int TrabajadorId { get; set; } // Relación con Trabajador (clave foránea)

        public DateTime Fecha { get; set; } // Fecha del registro, como texto formateado

        public decimal PrecioPorHora { get; set; } // Precio por hora trabajado

        public decimal HorasRealizadas { get; set; } // Horas trabajadas durante el registro

        public decimal PrecioTotal { get; set; } // Total calculado: PrecioPorHora * HorasRealizadas

        public bool Pagado { get; set; } // Estado de pago (true si se ha pagado)

        public string Nombre { get; set; } // Nombre del trabajador

        public string Apellidos { get; set; } // Apellidos del trabajador

        public decimal ImporteTotal { get; set; }

        //Método para actualizar el estado de pago sin modificar la propiedad directamente
        public string EstadoPago => ObtenerEstadoPago();

        private string ObtenerEstadoPago()
        {
            return Pagado ? "Sí" : "No";
        }
    }
}
