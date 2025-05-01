using JornaPay.Models;
using JornaPay.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JornaPay.ViewModels
{
    public class ImportesPagadosViewModel : INotifyPropertyChanged
    {
        private readonly TrabajadoresServicio _trabajadoresServicio;
        private string _nombreBusqueda;
        private List<TrabajadorDatos> _trabajadores;

        public event PropertyChangedEventHandler PropertyChanged;

        public string NombreBusqueda
        {
            get => _nombreBusqueda;
            set
            {
                _nombreBusqueda = value;
                OnPropertyChanged(nameof(NombreBusqueda));
            }
        }

        public List<TrabajadorDatos> Trabajadores
        {
            get => _trabajadores;
            set
            {
                _trabajadores = value;
                OnPropertyChanged(nameof(Trabajadores));
            }
        }

        public ICommand BuscarCommand => new Command(async () =>
        {
            var listaCompleta = await _trabajadoresServicio.ObtenerTodosTrabajadoresAsync();

            Trabajadores = listaCompleta
                .Where(t => t.Pagado == true &&
                            (string.IsNullOrEmpty(NombreBusqueda) ||
                             t.Nombre.Contains(NombreBusqueda, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(t => new { t.Nombre, t.Apellidos }) //Agrupar por Nombre y Apellidos
                .Select(g => new TrabajadorDatos
                {
                    Nombre = g.Key.Nombre + " " + g.Key.Apellidos, 
                    ImporteTotal = g.Sum(t => t.ImporteTotal), //Sumar correctamente
                    Pagado = true
                })
                .Where(t => t.ImporteTotal > 0) //Eliminar registros donde el total sea 0
                .ToList();

            OnPropertyChanged(nameof(Trabajadores)); // Notificar cambios
        });

        public ImportesPagadosViewModel(TrabajadoresServicio trabajadoresServicio)
        {
            _trabajadoresServicio = trabajadoresServicio;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
