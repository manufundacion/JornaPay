
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
            if (_trabajadoresServicio == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "El servicio de trabajadores no está inicializado.", "OK");
                return;
            }

            var listaCompleta = await _trabajadoresServicio.ObtenerTodosTrabajadoresAsync();
            if (listaCompleta == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo obtener la lista de trabajadores.", "OK");
                return;
            }

            var impagadosAgrupados = new Dictionary<string, decimal>();

            foreach (var trabajador in listaCompleta)
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);
                if (historial == null) continue;

                var registrosImpagados = historial.Where(h => !h.Pagado);

                foreach (var registro in registrosImpagados)
                {
                    string clave = trabajador.Nombre + " " + trabajador.Apellidos;

                    if (!string.IsNullOrEmpty(trabajador.Nombre) && !string.IsNullOrEmpty(trabajador.Apellidos))
                    {
                        if (impagadosAgrupados.ContainsKey(clave))
                        {
                            impagadosAgrupados[clave] += registro.PrecioTotal;
                        }
                        else
                        {
                            impagadosAgrupados[clave] = registro.PrecioTotal;
                        }
                    }
                }
            }

            var trabajadoresFiltrados = impagadosAgrupados.Select(t => new TrabajadorDatos
            {
                Nombre = t.Key.Split(" ")[0],
                Apellidos = t.Key.Substring(t.Key.IndexOf(" ") + 1),
                ImporteTotal = t.Value,
                Pagado = false
            }).ToList();

            if (!string.IsNullOrWhiteSpace(NombreBusqueda))
            {
                trabajadoresFiltrados = trabajadoresFiltrados
                    .Where(t => $"{t.Nombre} {t.Apellidos}".Contains(NombreBusqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Trabajadores = trabajadoresFiltrados;
            ImporteTotalImpagado = trabajadoresFiltrados.Sum(t => t.ImporteTotal);
            OnPropertyChanged(nameof(Trabajadores));
        });


        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
