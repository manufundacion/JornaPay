using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using JornaPay.Models;
using JornaPay.Pages;
using JornaPay.Services;
using iText.Layout;
using iText.IO.Font.Constants;


namespace JornaPay.ViewModels
{
    public class TrabajadoresViewModels : INotifyPropertyChanged
    {
        private readonly TrabajadoresServicio _trabajadoresServicio;

        public ObservableCollection<TrabajadorDatos> Historial { get; set; } = new();
        public ObservableCollection<Trabajador> Trabajadores { get; set; } = new();

        private string _nombreUsuario;
        private string _contraseña;
        private string _nombreBusqueda;
        private string _nombre;
        private string _apellidos;
        private decimal _precioPorHora;
        private DateTime _fechaSeleccionada;
        private decimal _horasTrabajadas;
        private string _totalAPagar;
        private bool _editarEnabled;
        private string _switchColor = "Red";
        private bool _puedeActualizar;
        private string _nombreUsuarioActual;
        private string _nombreUsuarioEditando;
        private bool CanActualizarDatos() => PuedeActualizar;

        private bool _puedeEliminar;

        private Trabajador _trabajadorSeleccionado;


        // Propiedades públicas
        public string NombreUsuario { get => _nombreUsuario; set => SetProperty(ref _nombreUsuario, value); }
        public string Contraseña { get => _contraseña; set => SetProperty(ref _contraseña, value); }
        public string NombreBusqueda { get => _nombreBusqueda; set => SetProperty(ref _nombreBusqueda, value); }
        public string Nombre { get => _nombre; set => SetProperty(ref _nombre, value); }
        public string Apellidos { get => _apellidos; set => SetProperty(ref _apellidos, value); }
        public decimal PrecioPorHora { get => _precioPorHora; set => SetProperty(ref _precioPorHora, value); }
        public DateTime FechaSeleccionada { get => _fechaSeleccionada; set => SetProperty(ref _fechaSeleccionada, value); }
        public decimal HorasTrabajadas { get => _horasTrabajadas; set => SetProperty(ref _horasTrabajadas, value); }
        public string TotalAPagar { get => _totalAPagar; set => SetProperty(ref _totalAPagar, value); }
        public bool EditarEnabled { get => _editarEnabled; set => SetProperty(ref _editarEnabled, value); }
        public string SwitchColor { get => _switchColor; set => SetProperty(ref _switchColor, value); }
        public bool PuedeActualizar { get => _puedeActualizar; set => SetProperty(ref _puedeActualizar, value); }
        public bool PuedeEliminar { get => _puedeEliminar; set => SetProperty(ref _puedeEliminar, value); }
        public decimal TotalPendientePago
        {
            get => Historial.Where(h => !h.Pagado).Sum(h => h.PrecioTotal); //Sumo solo los registros no pagados
        }


        public string NombreUsuarioActual
        {
            get => _nombreUsuarioActual;
            set
            {
                if (_nombreUsuarioActual != value)
                {
                    _nombreUsuarioActual = value;
                    OnPropertyChanged();
                    VerificarUsuario();
                }
            }
        }
        public string NombreUsuarioEditando
        {
            get => _nombreUsuarioEditando;
            set
            {
                if (_nombreUsuarioEditando != value)
                {
                    _nombreUsuarioEditando = value;
                    OnPropertyChanged();
                    VerificarUsuario();
                }
            }
        }

        public Trabajador TrabajadorSeleccionado
        {
            get => _trabajadorSeleccionado;
            set
            {
                if (_trabajadorSeleccionado != value)
                {
                    _trabajadorSeleccionado = value;
                    OnPropertyChanged(nameof(TrabajadorSeleccionado)); //Notifico cambios
                    OnPropertyChanged(nameof(PuedeActualizar)); //Actualizo al trabjador seleccionado
                    OnPropertyChanged(nameof(PuedeEliminar));   //Elimino al trabjador seleccionado
                    CargarHistorialTrabajadorAsync(); 
                }
            }
        }


        public ICommand IniciarSesionCommand { get; }
        public ICommand RegistrarUsuarioCommand { get; }
        public ICommand CrearTrabajadorCommand { get; }
        public ICommand AnyadirDatosCommand { get; }
        public ICommand ActualizarDatosCommand { get; }
        public ICommand GuardarRegistroCommand { get; }
        public ICommand ModificarDatosCommand { get; }
        public ICommand ActualizarTrabajadorCommand { get; }
        public ICommand EliminarTrabajadorCommand { get; }

        public ICommand DescargarListaPdfCommand => new Command(async () =>
        {
            await GenerarYDescargarPdfAsync();
        });

        public TrabajadoresViewModels()
        {
            _trabajadoresServicio = TrabajadoresServicio.GetInstance();
            IniciarSesionCommand = new Command(IniciarSesion);
            RegistrarUsuarioCommand = new Command(async () =>
            {
                var datosUsuario = await MostrarRegistroUsuarioDialogoAsync();
                if (datosUsuario.HasValue)
                {
                    await RegistrarUsuario(datosUsuario.Value.NombreUsuario, datosUsuario.Value.Contrasenya);
                }
            }); CrearTrabajadorCommand = new Command(CrearTrabajador);
            AnyadirDatosCommand = new Command(AnyadirDatos);
            ActualizarDatosCommand = new Command(ActualizarDatos, CanActualizarDatos);
            GuardarRegistroCommand = new Command(GuardarRegistro);
            ModificarDatosCommand = new Command(ModificarDatos);
            ActualizarTrabajadorCommand = new Command(async () => await ActualizarTrabajador(), () => TrabajadorSeleccionado != null);
            EliminarTrabajadorCommand = new Command(async () => await EliminarTrabajador(), () => TrabajadorSeleccionado != null);
            Task.Run(CargarTrabajadoresAsync);
        }

        private async void IniciarSesion()
        {
            var usuario = await _trabajadoresServicio.ValidarUsuarioAsync(NombreUsuario, Contraseña);
            if (usuario != null)
            {
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña incorrectos.", "OK");
            }
        }
        private async Task RegistrarUsuario(string nombreUsuario, string contrasenya)
        {
            try
            {
                var usuario = new Usuario { NombreUsuario = nombreUsuario, Contraseña = contrasenya };
                await _trabajadoresServicio.RegistrarUsuarioAsync(usuario);
                await Application.Current.MainPage.DisplayAlert("Éxito", "Usuario registrado correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo registrar el usuario: {ex.Message}", "OK");
            }
        }

        private async Task<(string NombreUsuario, string Contrasenya)?> MostrarRegistroUsuarioDialogoAsync()
        {
            var tcs = new TaskCompletionSource<(string NombreUsuario, string Contrasenya)?>();

            Entry nombreEntry = new Entry
            {
                Placeholder = "Introduce tu usuario",
                FontSize = 19,
                BackgroundColor = Colors.White,
                WidthRequest = 230, 
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            Entry contrasenyaEntry = new Entry
            {
                Placeholder = "Introduce tu contraseña",
                IsPassword = true,
                FontSize = 19,
                BackgroundColor = Colors.White,
                WidthRequest = 230, 
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var modalPage = new ContentPage
            {
                BackgroundImageSource = "registrousuario.jpg",

                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Spacing = 15,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    WidthRequest = 350,
                    Children =
                    {
                        new Frame
                        {
                            BorderColor = Colors.Transparent,
                            HasShadow = false,
                            BackgroundColor = Colors.DodgerBlue,
                            Padding = new Thickness(15, 5, 15, 5), // Ajusto el espaciado interno
                            Margin = new Thickness(0, 150, 0, 0), // Desplazo hacia abajo el cuadro de la contraseña
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Start, // Evito que se expanda innecesariamente
                            WidthRequest = 240,
                            Content = nombreEntry
                        },
                        new Frame
                        {
                            BorderColor = Colors.Transparent,
                            HasShadow = false,
                            BackgroundColor = Colors.DodgerBlue,
                            Padding = new Thickness(15, 5, 15, 5), // Ajusto el espaciado interno
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Start, 
                            WidthRequest = 240,
                            Content = contrasenyaEntry
                        },
                        new Button
                        {
                            Text = "Confirmar",
                            BackgroundColor = Colors.Green,
                            TextColor = Colors.White,
                            FontSize = 24,
                            WidthRequest = 200,
                            HeightRequest = 60,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Command = new Command(() =>
                            {
                                if (!string.IsNullOrWhiteSpace(nombreEntry.Text) && !string.IsNullOrWhiteSpace(contrasenyaEntry.Text))
                                {
                                    tcs.SetResult((nombreEntry.Text, contrasenyaEntry.Text));
                                    Application.Current.MainPage.Navigation.PopModalAsync();
                                }
                                else
                                {
                                    Application.Current.MainPage.DisplayAlert("Error", "Todos los campos deben estar rellenados.", "OK");
                                }
                            })
                        },
                        new Button
                        {
                            Text = "Cancelar",
                            BackgroundColor = Colors.Red,
                            TextColor = Colors.White,
                            FontSize = 24,
                            WidthRequest = 200,
                            HeightRequest = 60,
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

        private async void CrearTrabajador()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellidos))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Por favor, completa el nombre y los apellidos.", "OK");
                    return;
                }

                var trabajador = new Trabajador
                {
                    Nombre = Nombre,
                    Apellidos = Apellidos,
                    PrecioPorHora = PrecioPorHora,
                    HorasTrabajadas = 0,
                    Fecha = DateTime.Now,
                    Pagado = false,
                    NombreUsuario = SesionUsuario.NombreUsuarioActual
                };

                await _trabajadoresServicio.CrearTrabajadorAsync(trabajador);
                Trabajadores.Add(trabajador);

                await Application.Current.MainPage.DisplayAlert("Éxito", "Trabajador registrado con éxito.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Hubo un problema: {ex.Message}", "OK");
            }
        }

        private async void AnyadirDatos()
        {
            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellidos))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Por favor, rellena el nombre y los apellidos correctamente.", "OK");
                return;
            }

            try
            {
                var trabajador = new Trabajador
                {
                    Nombre = Nombre,
                    Apellidos = Apellidos,
                    PrecioPorHora = PrecioPorHora,
                    HorasTrabajadas = 0,
                    Fecha = DateTime.Now,
                    Pagado = false,
                    NombreUsuario = SesionUsuario.NombreUsuarioActual
                };

                await _trabajadoresServicio.CrearTrabajadorAsync(trabajador);
                Trabajadores.Add(trabajador);

                var nuevaPagina = new NuevoTrabajador(Nombre, Apellidos, PrecioPorHora);
                Shell.Current.Items.Add(new FlyoutItem
                {
                    Title = $"{Nombre} {Apellidos}",
                    Items = { new ShellContent { Title = $"{Nombre} {Apellidos}", Content = nuevaPagina } }
                });

                await Application.Current.MainPage.DisplayAlert("Éxito", $"El trabajador {Nombre} {Apellidos} se añadió al menú.", "OK");

                // Vacio los campos Nombre y Apellidos después de añadir el trabajador
                Nombre = string.Empty;
                Apellidos = string.Empty;
                OnPropertyChanged(nameof(Nombre));
                OnPropertyChanged(nameof(Apellidos));

            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo añadir la página: {ex.Message}", "OK");
            }
        }


        private void GuardarRegistro()
        {
            if (FechaSeleccionada == default || HorasTrabajadas <= 0)
            {
                Application.Current.MainPage.DisplayAlert("Error", "Completa la fecha y las horas trabajadas correctamente.", "OK");
                return;
            }

            var nuevoRegistro = new TrabajadorDatos
            {
                Fecha = FechaSeleccionada,
                HorasRealizadas = HorasTrabajadas,
                PrecioTotal = PrecioPorHora * HorasTrabajadas,
                Pagado = false
            };

            Historial.Add(nuevoRegistro);
            CalcularTotal();
            ActualizarTotalPendiente();

            Application.Current.MainPage.DisplayAlert("Éxito", "Registro guardado correctamente.", "OK");
        }

        private void VerificarUsuario()
        {
            PuedeActualizar = NombreUsuarioActual == NombreUsuarioEditando;
        }

        private async void ModificarDatos()
        {
            string nuevoNombre = await Application.Current.MainPage.DisplayPromptAsync(
                "Modificar Nombre",
                "Introduce el nuevo nombre:",
                initialValue: Nombre,
                accept: "Aceptar",
                cancel: "Cancelar"
            );
            string nuevosApellidos = await Application.Current.MainPage.DisplayPromptAsync(
                "Modificar Apellidos",
                "Introduce los nuevos apellidos:",
                initialValue: Apellidos,
                accept: "Aceptar",
                cancel: "Cancelar"
            );

            if (!string.IsNullOrWhiteSpace(nuevoNombre) && !string.IsNullOrWhiteSpace(nuevosApellidos))
            {
                Nombre = nuevoNombre;
                Apellidos = nuevosApellidos;
                ActualizarTotalPendiente();
                Application.Current.MainPage.DisplayAlert("Éxito", "Datos actualizados correctamente.", "OK");
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("Error", "No se realizaron cambios.", "OK");
            }
        }

        private void ActualizarTotalPendiente()
        {
            OnPropertyChanged(nameof(TotalPendientePago)); //Notifica el cambio para actualizar
        }


        private async Task ActualizarTrabajador()
        {
            if (TrabajadorSeleccionado == null) return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert("Confirmación", $"¿Seguro que desea actualizar la información de {TrabajadorSeleccionado.Nombre} {TrabajadorSeleccionado.Apellidos}?", "Sí", "No");
            if (!confirmar) return;

            string nuevoNombre = await Application.Current.MainPage.DisplayPromptAsync(
                "Modificar Nombre",
                "Introduce el nuevo nombre:",
                initialValue: Nombre,
                accept: "Aceptar",
                cancel: "Cancelar"
            );

            string nuevosApellidos = await Application.Current.MainPage.DisplayPromptAsync(
                "Modificar Apellidos",
                "Introduce los nuevos apellidos:",
                initialValue: Apellidos,
                accept: "Aceptar",
                cancel: "Cancelar"
            );

            if (string.IsNullOrWhiteSpace(nuevoNombre) || string.IsNullOrWhiteSpace(nuevosApellidos))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No se han ingresado datos nuevos.", "OK");
                return;
            }

            TrabajadorSeleccionado.Nombre = nuevoNombre;
            TrabajadorSeleccionado.Apellidos = nuevosApellidos;
            await _trabajadoresServicio.ActualizarTrabajadorAsync(TrabajadorSeleccionado);

            await Application.Current.MainPage.DisplayAlert("Éxito", "Trabajador actualizado correctamente.", "OK");
            OnPropertyChanged(nameof(Trabajadores));
            OnPropertyChanged(nameof(TrabajadorSeleccionado));

            await CargarTrabajadoresAsync(); //Recarga la lista de trabajadores
        }

        private async Task EliminarTrabajador()
        {
            if (TrabajadorSeleccionado == null) return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert("Confirmación", $"¿Seguro que desea eliminar al trabajador {TrabajadorSeleccionado.Nombre} {TrabajadorSeleccionado.Apellidos}?", "Sí", "No");
            if (!confirmar) return;
                
            try
            {
                await _trabajadoresServicio.EliminarTrabajadorAsync(TrabajadorSeleccionado.Id);
                Trabajadores.Remove(TrabajadorSeleccionado);

                //Busco y elimino la página del trabajador en Shell
                var itemAEliminar = Shell.Current.Items.FirstOrDefault(item => item.Title == $"{TrabajadorSeleccionado.Nombre} {TrabajadorSeleccionado.Apellidos}");
                if (itemAEliminar != null)
                {
                    Shell.Current.Items.Remove(itemAEliminar); //Elimino la página del Shell
                }

                TrabajadorSeleccionado = null;

                //Fuerzo la actualización de la lista
                OnPropertyChanged(nameof(Trabajadores));
                OnPropertyChanged(nameof(TrabajadorSeleccionado));
                ActualizarTotalPendiente();

                await Application.Current.MainPage.DisplayAlert("Éxito", "Trabajador eliminado correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo eliminar el trabajador: {ex.Message}", "OK");
            }
        }

        private async Task CargarHistorialTrabajadorAsync()
        {
            if (TrabajadorSeleccionado == null) return;

            try
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(TrabajadorSeleccionado.Id);
                Historial.Clear();

                foreach (var registro in historial)
                {
                    Historial.Add(registro);
                }

                ActualizarTotalPendiente(); //Actualizo el total pendiente al abrir la página
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al cargar historial: {ex.Message}", "OK");
            }
        }



        private void CalcularTotal()
        {
            decimal total = 0;
            foreach (var registro in Historial)
            {
                if (!registro.Pagado)
                {
                    total += registro.PrecioTotal;
                }
            }
            TotalAPagar = $"Total a Pagar: {total:C}";
        }
        
        private async void ActualizarDatos()
        {
            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellidos))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Nombre y Apellidos no pueden estar vacíos.", "OK");
                return;
            }

            try
            {
                var trabajador = await _trabajadoresServicio.ObtenerTrabajadorAsync(Nombre, Apellidos);
                if (trabajador == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "El trabajador no existe.", "OK");
                    return;
                }

                trabajador.PrecioPorHora = PrecioPorHora;
                trabajador.HorasTrabajadas = HorasTrabajadas;
                await _trabajadoresServicio.ActualizarTrabajadorAsync(trabajador);

                await Application.Current.MainPage.DisplayAlert("Éxito", "Datos actualizados correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al actualizar: {ex.Message}", "OK");
            }
        }

        private async Task CargarTrabajadoresAsync()
        {
            if (string.IsNullOrEmpty(SesionUsuario.NombreUsuarioActual))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No hay usuario activo.", "OK");
                return;
            }

            try
            {
                var trabajadores = await _trabajadoresServicio.ObtenerTrabajadoresPorUsuarioAsync(SesionUsuario.NombreUsuarioActual);

                if (trabajadores == null || trabajadores.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "No hay trabajadores registrados.", "OK");
                    return;
                }

                Trabajadores.Clear();

                Shell.Current.Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        // 🔹 **Mantener opción "Cerrar Sesión"**
                        if (!Shell.Current.Items.Any(item => item.Title == "Cerrar Sesión"))
                        {
                            Shell.Current.Items.Add(new FlyoutItem
                            {
                                Title = "Cerrar Sesión",
                                Items = { new ShellContent { ContentTemplate = new DataTemplate(() => new CerrarSesion()) } }
                            });
                        }

                        //Elimino los elementos duplicados
                        var itemsToRemove = Shell.Current.Items.Where(item =>
                            item is FlyoutItem flyoutItem &&
                            flyoutItem.Title != "Inscripción de Trabajadores" &&
                            flyoutItem.Title != "Lista de Trabajadores" &&
                            flyoutItem.Title != "Importes" &&
                            flyoutItem.Title != "Cerrar Sesión" 
                        ).ToList();

                        foreach (var item in itemsToRemove)
                        {
                            Shell.Current.Items.Remove(item);
                        }

                        //Añado los trabajadores al menú
                        foreach (var trabajador in trabajadores)
                        {
                            Trabajadores.Add(trabajador);

                            // Agrego trabajador al Shell si no existe aún
                            if (!Shell.Current.Items.Any(item => item.Title == $"{trabajador.Nombre} {trabajador.Apellidos}"))
                            {
                                var nuevoFlyoutItem = new FlyoutItem
                                {
                                    Title = $"{trabajador.Nombre} {trabajador.Apellidos}",
                                    Items =
                            {
                                new ShellContent
                                {
                                    Title = $"{trabajador.Nombre} {trabajador.Apellidos}",
                                    ContentTemplate = new DataTemplate(() => new NuevoTrabajador(trabajador.Nombre, trabajador.Apellidos, trabajador.PrecioPorHora))
                                }
                            }
                                };

                                Shell.Current.Items.Add(nuevoFlyoutItem);
                            }
                        }

                        OnPropertyChanged(nameof(Trabajadores));
                    }
                    catch (Exception uiEx)
                    {
                        Console.WriteLine($"Error en actualización de UI: {uiEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al cargar trabajadores: {ex.Message}", "OK");
                Console.WriteLine($"Error en CargarTrabajadoresAsync: {ex.Message}");
            }
        }


        private async Task GenerarYDescargarPdfAsync()
        {
            try
            {
                if (Trabajadores == null || !Trabajadores.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No hay trabajadores disponibles para generar el PDF.", "OK");
                    return;
                }

                string fileName = "ListaTrabajadores.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                using (Document doc = new Document(pdf))
                {
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                    doc.Add(new Paragraph("Lista de Trabajadores").SetFont(boldFont).SetFontSize(16));
                    doc.Add(new Paragraph(" "));

                    foreach (var trabajador in Trabajadores)
                    {
                        doc.Add(new Paragraph($"{trabajador.Nombre} {trabajador.Apellidos} - {trabajador.PrecioPorHora} €/h"));
                    }
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
