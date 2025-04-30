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
                .GroupBy(t => t.Nombre)
                .Select(g => new TrabajadorDatos
                {
                    Nombre = g.Key,
                    ImporteTotal = g.Sum(t => t.ImporteTotal),
                    Pagado = true
                })
                .ToList();
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
