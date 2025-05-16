using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using JornaPay.Models;
using JornaPay.Services;
using iText.Layout;
using iText.IO.Font.Constants;
using JornaPay.Pages;
using MauiImage = Microsoft.Maui.Controls.Image;

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

        // Variables para los filtros
        private string _mesSeleccionado;
        private string _añoSeleccionado;
        private Trabajador _trabajadorSeleccionado;
        private List<TrabajadorDatos> _historialCompleto = new();


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

        public string MesSeleccionado
        {
            get => _mesSeleccionado;
            set
            {
                _mesSeleccionado = value;
                OnPropertyChanged();
                FiltrarHistorial();
            }
        }

        public string AñoSeleccionado
        {
            get => _añoSeleccionado;
            set
            {
                _añoSeleccionado = value;
                OnPropertyChanged();
                FiltrarHistorial();
            }
        }

        public Trabajador TrabajadorSeleccionado
        {
            get => _trabajadorSeleccionado;
            set
            {
                _trabajadorSeleccionado = value;
                OnPropertyChanged();
            }
        }


        public DateTime MinimumDate => DateTime.Today.AddYears(-1);
        public DateTime MaximumDate => DateTime.Today.AddYears(1);

        public ObservableCollection<string> PagadoOptions { get; set; } = new ObservableCollection<string> { "Sí", "No" };

        public ObservableCollection<TrabajadorDatos> Historial { get; set; } = new();

        // Listas de Meses
        public ObservableCollection<string> Meses { get; set; } = new ObservableCollection<string>
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto",
            "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        //Lista de Años
        public ObservableCollection<string> Años { get; set; }

        // Lista del historial filtrado
        public ObservableCollection<TrabajadorDatos> HistorialFiltrado { get; set; } = new();

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

                DateTime? nuevaFecha = await MostrarSelectorFechaEnDialogoAsync(registroBD.Fecha);
                if (!nuevaFecha.HasValue) return;

                string nuevasHoras = await Application.Current.MainPage.DisplayPromptAsync(
                    "Modificar Horas", "Introduce las nuevas horas trabajadas",
                    initialValue: registroBD.HorasRealizadas.ToString(), accept: "Aceptar", cancel: "Cancelar");
                if (string.IsNullOrEmpty(nuevasHoras)) return;

                string nuevoPrecioPorHora = await Application.Current.MainPage.DisplayPromptAsync(
                    "Modificar Precio por Hora", "Introduce el nuevo precio por hora (€)",
                    initialValue: registroBD.PrecioPorHora.ToString(), accept: "Aceptar", cancel: "Cancelar");
                if (string.IsNullOrEmpty(nuevoPrecioPorHora)) return;

                string nuevoEstadoPago = await Application.Current.MainPage.DisplayActionSheet(
                    "¿Pagado?", "Cancelar", null, "Sí", "No");
                if (nuevoEstadoPago == "Cancelar") return;

                registroBD.Fecha = nuevaFecha.Value;
                registroBD.HorasRealizadas = decimal.Parse(nuevasHoras);
                registroBD.PrecioPorHora = decimal.Parse(nuevoPrecioPorHora);
                registroBD.Pagado = nuevoEstadoPago == "Sí";
                registroBD.PrecioTotal = registroBD.HorasRealizadas * registroBD.PrecioPorHora;

                await _trabajadoresServicio.ActualizarHistorialAsync(registroBD);

                await Application.Current.MainPage.DisplayAlert("Éxito", "Datos actualizados con éxito", "OK");

                // Envía el mensaje para que la página recargue
                MessagingCenter.Send(this, "RecargarHistorial");


                ElementoSeleccionado = null;

                // Recargo la lista
                await CargarHistorialAsync();

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

        public ICommand DescargarHistorialPdfCommand { get; }



        public NuevoTrabajadorViewModels(string nombre, string apellidos, decimal precioPorHora)
        {
            _trabajadoresServicio = TrabajadoresServicio.GetInstance();
            Nombre = nombre;
            Apellidos = apellidos;
            PrecioPorHora = precioPorHora;
            GuardarRegistroCommand = new Command(GuardarRegistro);
            DescargarHistorialPdfCommand = new Command(async () => await GenerarYDescargarHistorialPdf());

            // Genero la lista de Años dinámicamente (últimos 6 años, incluyendo el actual)
            Años = new ObservableCollection<string>();
            int currentYear = DateTime.Now.Year;
            for (int i = currentYear - 5; i <= currentYear; i++)
            {
                Años.Add(i.ToString());
            }

            // Establezco el mes y año actual como selección por defecto
            var fechaActual = DateTime.Now;
            MesSeleccionado = Meses[fechaActual.Month - 1];
            AñoSeleccionado = fechaActual.Year.ToString();
        }

        public void CargarHistorialAlAbrir()
        {
            Task.Run(CargarHistorialAsync);
        }

        public async Task CargarHistorialAsync()
        {
            try
            {
                // Limpio la colección del ObservableCollection
                Historial.Clear();

                var trabajador = await _trabajadoresServicio.ObtenerTrabajadorAsync(Nombre, Apellidos);

                if (trabajador == null || trabajador.NombreUsuario != SesionUsuario.NombreUsuarioActual)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "El trabajador no existe o no pertenece a este usuario.", "OK");
                    return;
                }

                var historialTrabajador = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);

                // Guardo la copia completa para el nuevo filtrado
                _historialCompleto = historialTrabajador.ToList();

                if (_historialCompleto.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "No se encontraron registros de historial en la base de datos.", "OK");
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var registro in _historialCompleto)
                        {
                            Historial.Add(registro);
                        }
                        OnPropertyChanged(nameof(Historial)); // Actualizo el historial del trabajdor
                        FiltrarHistorial(); // Aplico el filtrado usando la copia completa para que me cargue la fecha correcta
                        ActualizarTotalPendiente();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en CargarHistorialAsync: {ex.Message}");
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

            // Vuelvo a filtrar la lista según el mes y año seleccionados
            FiltrarHistorial();

            // Notifico que Historial ha sido actualizado.
            OnPropertyChanged(nameof(Historial));
        }

        // Filtro el historial por mes y año seleccionado
        private void FiltrarHistorial()
        {
            if (string.IsNullOrEmpty(MesSeleccionado) || string.IsNullOrEmpty(AñoSeleccionado))
                return;

            int mesNumero = Meses.IndexOf(MesSeleccionado) + 1;
            int añoNumero = int.Parse(AñoSeleccionado);

            Console.WriteLine($"Filtrando: Mes {MesSeleccionado} ({mesNumero}), Año {AñoSeleccionado}");

            // Aplicar el filtro sobre la lista completa (o, si usas respaldo, sobre la copia completa)
            var registrosFiltrados = _historialCompleto.Where(h => h.Fecha.Month == mesNumero && h.Fecha.Year == añoNumero).ToList();

            Console.WriteLine($"Registros encontrados tras el filtro: {registrosFiltrados.Count}");

            Historial.Clear();
            foreach (var registro in registrosFiltrados)
            {
                Historial.Add(registro);
            }

            // Notificolos cambios
            OnPropertyChanged(nameof(Historial));

            // Actualizo la propiedad TotalPendientePago para que se recalcule
            OnPropertyChanged(nameof(TotalPendientePago));
        }


        private async Task<DateTime?> MostrarSelectorFechaEnDialogoAsync(DateTime fechaActual)
        {
            var tcs = new TaskCompletionSource<DateTime?>();

            DatePicker datePicker = new DatePicker
            {
                Date = DateTime.Today,
                MaximumDate = DateTime.Today,
                MinimumDate = new DateTime(2000, 1, 1),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 32,
                WidthRequest = 180,
                HeightRequest = 60,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(20, 0), // Compensa visualmente el espacio
                FlowDirection = FlowDirection.LeftToRight
            };


            var modalPage = new ContentPage
            {
                Content = new Grid
                {
                    Children =
                    {
                        new MauiImage
                        {
                            Source = "fechamodificar.png",
                            Aspect = Aspect.AspectFill,
                            Opacity = 0.16
                        },
                        new VerticalStackLayout
                        {
                            Spacing = 50,
                            Padding = new Thickness(0, 60, 0, 0), // Mueve todo el contenido un poco hacia abajo
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Frame
                                {
                                    BorderColor = Colors.Transparent,
                                    HasShadow = false,
                                    BackgroundColor = Colors.Transparent,
                                    HorizontalOptions = LayoutOptions.Center,
                                    VerticalOptions = LayoutOptions.Center,
                                    WidthRequest = 260,
                                    Padding = new Thickness(0), // Elimina cualquier padding extra
                                    Content = new Grid
                                    {
                                        VerticalOptions = LayoutOptions.Center,
                                        HorizontalOptions = LayoutOptions.Center,
                                        Children =
                                        {
                                            datePicker
                                        }
                                    }
                                },
                                // Botón Aceptar
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
                                // Botón Cancelar
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
                    }
                }
            };

            await Application.Current.MainPage.Navigation.PushModalAsync(modalPage);
            return await tcs.Task;
        }

        private async Task GenerarYDescargarHistorialPdf()
        {
            try
            {
                if (Historial == null || !Historial.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No hay historial disponible para generar el PDF.", "OK");
                    return;
                }

                string fileName = $"Historial_{Nombre}_{Apellidos}.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                using (Document doc = new Document(pdf))
                {
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                    doc.Add(new Paragraph($"Historial de {Nombre} {Apellidos}").SetFont(boldFont).SetFontSize(16));
                    doc.Add(new Paragraph(" "));

                    decimal totalPagado = 0;
                    decimal totalImpagado = 0;

                    foreach (var registro in Historial)
                    {
                        doc.Add(new Paragraph($"Fecha: {registro.Fecha:dd/MM/yyyy}"));
                        doc.Add(new Paragraph($"Horas Trabajadas: {registro.HorasRealizadas}"));

                        //Convierto el EstadoPago a booleano
                        bool estadoPago = registro.EstadoPago?.ToLower() == "true" || registro.EstadoPago == "Sí";

                        doc.Add(new Paragraph($"Pagado: {(estadoPago ? "Sí" : "No")}"));
                        doc.Add(new Paragraph($"Total: {registro.PrecioTotal:C}"));
                        doc.Add(new Paragraph(" "));

                        if (estadoPago)
                            totalPagado += registro.PrecioTotal;
                        else
                            totalImpagado += registro.PrecioTotal;
                    }

                    //Agrego la suma total al final de todos los registros
                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph($"Total Pagado: {totalPagado:C}").SetFont(boldFont));
                    doc.Add(new Paragraph($"Total Impagado: {totalImpagado:C}").SetFont(boldFont));
                }

                await Application.Current.MainPage.DisplayAlert("Éxito", "PDF guardado correctamente.", "OK");

                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath),
                });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo generar el PDF: {ex.Message}", "OK");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

    }
}