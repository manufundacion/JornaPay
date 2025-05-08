using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JornaPay.Models;
using JornaPay.Services;

namespace JornaPay.ViewModels
{
    public class NuevoTrabajadorViewModels : INotifyPropertyChanged
    {
        private readonly TrabajadoresServicio _trabajadoresServicio;

        private string _nombre;
        private string _apellidos;
        private decimal _precioPorHora;
        private decimal _horasTrabajadas;
        private string _pagado;
        private DateTime _fechaSeleccionada = DateTime.Today;
        private TrabajadorDatos _elementoSeleccionado;

        public string Nombre
        {
            get => _nombre;
            set
            {
                if (_nombre != value)
                {
                    _nombre = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Titulo));
                }
            }
        }

        public string Apellidos
        {
            get => _apellidos;
            set
            {
                if (_apellidos != value)
                {
                    _apellidos = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Titulo));
                }
            }
        }

        public string Titulo => $"{Nombre} {Apellidos}";

        public decimal PrecioPorHora
        {
            get => _precioPorHora;
            set
            {
                if (_precioPorHora != value)
                {
                    _precioPorHora = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal HorasTrabajadas
        {
            get => _horasTrabajadas;
            set
            {
                if (_horasTrabajadas != value)
                {
                    _horasTrabajadas = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Pagado
        {
            get => _pagado;
            set
            {
                if (_pagado != value)
                {
                    _pagado = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                _fechaSeleccionada = value;
                OnPropertyChanged(nameof(FechaSeleccionada));
            }
        }

        public TrabajadorDatos ElementoSeleccionado
        {
            get => _elementoSeleccionado;
            set
            {
                _elementoSeleccionado = value;
                OnPropertyChanged(nameof(ElementoSeleccionado));
            }
        }

        public decimal TotalPendientePago
        {
            get => Historial.Where(h => !h.Pagado).Sum(h => h.PrecioTotal); //Suma solo los registros no pagados
        }


        public DateTime MinimumDate => DateTime.Today.AddYears(-1);
        public DateTime MaximumDate => DateTime.Today.AddYears(1);

        public ObservableCollection<string> PagadoOptions { get; set; } = new ObservableCollection<string> { "Sí", "No" };

        public ObservableCollection<TrabajadorDatos> Historial { get; set; } = new();

        public ICommand GuardarRegistroCommand { get; }

        public ICommand ModificarRegistroCommand => new Command(async () =>
        {
            if (ElementoSeleccionado != null)
            {
                var registroBD = await _trabajadoresServicio.ObtenerRegistroAsync(ElementoSeleccionado.Id);

                if (registroBD == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "El registro no existe en la base de datos.", "OK");
                    return;
                }

                // Muestro el diálogo con el DatePicker primero
                DateTime? nuevaFecha = await MostrarSelectorFechaEnDialogoAsync(registroBD.Fecha);
                if (!nuevaFecha.HasValue) return; //Si el usuario cancela, no hago cambios

                // Pido las nuevas horas trabajadas
                string nuevasHoras = await Application.Current.MainPage.DisplayPromptAsync(
                    "Modificar Horas", "Introduce las nuevas horas trabajadas",
                    initialValue: registroBD.HorasRealizadas.ToString(), accept: "Aceptar", cancel: "Cancelar");
                if (string.IsNullOrEmpty(nuevasHoras)) return;

                // Pido el nuevo precio por hora
                string nuevoPrecioPorHora = await Application.Current.MainPage.DisplayPromptAsync(
                    "Modificar Precio por Hora", "Introduce el nuevo precio por hora (€)",
                    initialValue: registroBD.PrecioPorHora.ToString(), accept: "Aceptar", cancel: "Cancelar");
                if (string.IsNullOrEmpty(nuevoPrecioPorHora)) return;

                // Pregunto si el registro está pagado
                string nuevoEstadoPago = await Application.Current.MainPage.DisplayActionSheet(
                    "¿Pagado?", "Cancelar", null, "Sí", "No");
                if (nuevoEstadoPago == "Cancelar") return;

                // Aplico los cambios solo si el usuario ha confirmado cada entrada
                registroBD.Fecha = nuevaFecha.Value;
                registroBD.HorasRealizadas = decimal.Parse(nuevasHoras);
                registroBD.PrecioPorHora = decimal.Parse(nuevoPrecioPorHora);
                registroBD.Pagado = nuevoEstadoPago == "Sí";

                // Recalculo el PrecioTotal con el nuevo PrecioPorHora
                registroBD.PrecioTotal = registroBD.HorasRealizadas * registroBD.PrecioPorHora;

                // Guardo los cambios en la base de datos
                await _trabajadoresServicio.ActualizarHistorialAsync(registroBD);

                //Limpio la selección para desactivar los botones
                ElementoSeleccionado = null;
                OnPropertyChanged(nameof(ElementoSeleccionado));

                // Recargo el historial para reflejar los cambios
                await CargarHistorialAsync();
                OnPropertyChanged(nameof(Historial));

                await Application.Current.MainPage.DisplayAlert("Éxito", $"Datos actualizados con éxito", "OK");
            }
        });

        public ICommand EliminarRegistroCommand => new Command(async () =>
        {
            if (ElementoSeleccionado == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Selecciona un registro para eliminar.", "OK");
                return;
            }

            bool confirmar = await Application.Current.MainPage.DisplayAlert("Confirmación",
                $"¿Seguro que deseas eliminar el registro del {ElementoSeleccionado.Fecha:dd/MM/yyyy}?", "Sí", "No");

            if (!confirmar) return;

            await _trabajadoresServicio.EliminarHistorialAsync(ElementoSeleccionado.Id);

            //Limpio la selección para desactivar los botones
            ElementoSeleccionado = null;
            OnPropertyChanged(nameof(ElementoSeleccionado));

            // Recargo el historial para reflejar la eliminación
            await CargarHistorialAsync();
            OnPropertyChanged(nameof(Historial));

            await Application.Current.MainPage.DisplayAlert("Éxito", "Registro eliminado correctamente.", "OK");
        });


        public NuevoTrabajadorViewModels(string nombre, string apellidos, decimal precioPorHora)
        {
            _trabajadoresServicio = TrabajadoresServicio.GetInstance();
            Nombre = nombre;
            Apellidos = apellidos;
            PrecioPorHora = precioPorHora;
            GuardarRegistroCommand = new Command(GuardarRegistro);
        }


        public void CargarHistorialAlAbrir()
        {
            Task.Run(CargarHistorialAsync);
        }

        public async Task CargarHistorialAsync()
        {
            try
            {
                Historial.Clear();

                var trabajador = await _trabajadoresServicio.ObtenerTrabajadorAsync(Nombre, Apellidos);
                if (trabajador == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "El trabajador no existe.", "OK");
                    return;
                }

                var historialTrabajador = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);

                // Muestro el aviso solo si la lista sigue vacía después de cargar la consulta
                if (historialTrabajador.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "No se encontraron registros de historial en la base de datos.", "OK");
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var registro in historialTrabajador)
                        {
                            Historial.Add(registro);
                        }
                        OnPropertyChanged(nameof(Historial)); //Actualizo el historial
                        ActualizarTotalPendiente(); //Se actualiza el total pendiente de pago
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en CargarHistorialAsync: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el historial: {ex.Message}", "OK");
            }
        }

        private void ActualizarTotalPendiente()
        {
            OnPropertyChanged(nameof(TotalPendientePago)); //Notifico que el total ha cambiado
        }


        private async void GuardarRegistro()
        {
            if (HorasTrabajadas <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Introduce un número válido de horas trabajadas.", "OK");
                return;
            }

            if (FechaSeleccionada == default)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Selecciona una fecha válida.", "OK");
                return;
            }

            var trabajadorExistente = await _trabajadoresServicio.ObtenerTrabajadorAsync(Nombre, Apellidos);
            if (trabajadorExistente == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "El trabajador no existe en la base de datos.", "OK");
                return;
            }

            trabajadorExistente.HorasTrabajadas += HorasTrabajadas;
            trabajadorExistente.Fecha = FechaSeleccionada;
            trabajadorExistente.Pagado = (Pagado == "Sí");

            await _trabajadoresServicio.ActualizarTrabajadorAsync(trabajadorExistente);

            var nuevoHistorial = new TrabajadorDatos
            {
                TrabajadorId = trabajadorExistente.Id,
                Fecha = FechaSeleccionada.Date,
                HorasRealizadas = HorasTrabajadas,
                PrecioPorHora = PrecioPorHora,
                PrecioTotal = HorasTrabajadas * PrecioPorHora,
                Pagado = trabajadorExistente.Pagado
            };

            await _trabajadoresServicio.GuardarHistorialAsync(nuevoHistorial);

            // Obtengo el registro con el ID asignado por la base de datos
            var historialGuardado = await _trabajadoresServicio.ObtenerRegistroAsync(nuevoHistorial.Id);

            if (historialGuardado == null || historialGuardado.Id == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No se asignó correctamente el ID al historial.", "OK");
                return;
            }

            if (Historial == null)
            {
                Historial = new ObservableCollection<TrabajadorDatos>();
            }

            Historial.Add(historialGuardado);

            await Application.Current.MainPage.DisplayAlert("Éxito", "Datos insertados correctamente.", "OK");
            // Recargo el historial para reflejar los cambios
            await CargarHistorialAsync();
            OnPropertyChanged(nameof(Historial));
        }

        private async Task<DateTime?> MostrarSelectorFechaEnDialogoAsync(DateTime fechaActual)
        {
            var tcs = new TaskCompletionSource<DateTime?>();
            DatePicker datePicker = new DatePicker
            {
                Date = DateTime.Today, //La fecha inicial es la actual
                MaximumDate = DateTime.Today, // No se pueden seleccionar fechas futuras
                MinimumDate = new DateTime(2000, 1, 1), // Se pueden seleccionar fechas anteriores
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 32,
                WidthRequest = 250,
                HeightRequest = 60,
                Margin = new Thickness(0, 20, 0, 60),
                FlowDirection = FlowDirection.LeftToRight
            };

            var modalPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Padding = 40,
                    Spacing = 50,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
            {
                new Label
                {
                    Text = "Selecciona una nueva fecha",
                    FontSize = 36,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black
                },
                new Frame
                {
                    BorderColor = Colors.Transparent,
                    HasShadow = false,
                    BackgroundColor = Colors.Transparent,
                    Padding = 5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    WidthRequest = 260,
                    Content = datePicker
                },
                new Button
                {
                    Text = "Aceptar",
                    BackgroundColor = Colors.Blue,
                    TextColor = Colors.White,
                    FontSize = 28,
                    WidthRequest = 220,
                    HeightRequest = 80,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Command = new Command(() =>
                    {
                        tcs.SetResult(datePicker.Date);
                        Application.Current.MainPage.Navigation.PopModalAsync();
                    })
                },
                new Button
                {
                    Text = "Cancelar",
                    BackgroundColor = Colors.Red,
                    TextColor = Colors.White,
                    FontSize = 28,
                    WidthRequest = 220,
                    HeightRequest = 80,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Command = new Command(() =>
                    {
                        tcs.SetResult(null);
                        Application.Current.MainPage.Navigation.PopModalAsync();
                    })
                }
            }
                }
            };

            await Application.Current.MainPage.Navigation.PushModalAsync(modalPage);
            return await tcs.Task;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
