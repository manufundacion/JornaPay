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
        private decimal _importeTotalPagado;


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

        public decimal ImporteTotalPagado
        {
            get => _importeTotalPagado;
            set
            {
                _importeTotalPagado = value;
                OnPropertyChanged(nameof(ImporteTotalPagado));
            }
        }

        public ICommand BuscarCommand => new Command(async () =>
        {
            var listaCompleta = await _trabajadoresServicio.ObtenerTodosTrabajadoresAsync();
            var pagadosAgrupados = new Dictionary<string, decimal>();

            foreach (var trabajador in listaCompleta)
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);
                var registrosPagados = historial.Where(h => h.Pagado);

                foreach (var registro in registrosPagados)
                {
                    string clave = trabajador.Nombre + " " + trabajador.Apellidos;

                    if (pagadosAgrupados.ContainsKey(clave))
                    {
                        pagadosAgrupados[clave] += registro.PrecioTotal; //Sumo los importes si ya existen
                    }
                    else
                    {
                        pagadosAgrupados[clave] = registro.PrecioTotal; //Agrego un nuevo trabajador
                    }
                }
            }

            // Convierto el diccionario en una lista de TrabajadorDatos
            Trabajadores = pagadosAgrupados.Select(t => new TrabajadorDatos
            {
                Nombre = t.Key,
                ImporteTotal = t.Value,
                Pagado = true
            }).ToList();

            ImporteTotalPagado = pagadosAgrupados.Values.Sum(); //Guardo el total pagado
            OnPropertyChanged(nameof(Trabajadores));
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