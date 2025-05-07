
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
    public class ImportesImpagadosViewModel : INotifyPropertyChanged
    {
        private readonly TrabajadoresServicio _trabajadoresServicio;
        private string _nombreBusqueda;
        private List<TrabajadorDatos> _trabajadores;
        private decimal _importeTotalImpagado;
        public event PropertyChangedEventHandler PropertyChanged;

        public ImportesImpagadosViewModel(TrabajadoresServicio trabajadoresServicio)
        {
            _trabajadoresServicio = trabajadoresServicio;
        }

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

        public decimal ImporteTotalImpagado
        {
            get => _importeTotalImpagado;
            set
            {
                _importeTotalImpagado = value;
                OnPropertyChanged(nameof(ImporteTotalImpagado));
            }
        }
        public ICommand BuscarCommand => new Command(async () =>
        {
            var listaCompleta = await _trabajadoresServicio.ObtenerTodosTrabajadoresAsync();
            var impagadosAgrupados = new Dictionary<string, decimal>();

            foreach (var trabajador in listaCompleta)
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);
                var registrosImpagados = historial.Where(h => !h.Pagado);

                foreach (var registro in registrosImpagados)
                {
                    string clave = trabajador.Nombre + " " + trabajador.Apellidos;

                    if (impagadosAgrupados.ContainsKey(clave))
                    {
                        impagadosAgrupados[clave] += registro.PrecioTotal; //Sumo los importes si ya existen
                    }
                    else
                    {
                        impagadosAgrupados[clave] = registro.PrecioTotal; //Agrego un nuevo trabajador
                    }
                }
            }

            //Convierto el diccionario en una lista de TrabajadorDatos
            Trabajadores = impagadosAgrupados.Select(t => new TrabajadorDatos
            {
                Nombre = t.Key,
                ImporteTotal = t.Value,
                Pagado = false
            }).ToList();

            ImporteTotalImpagado = impagadosAgrupados.Values.Sum(); //Guardo el total impagado
            OnPropertyChanged(nameof(Trabajadores));
        });


        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
