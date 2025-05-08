using System.ComponentModel;
using System.Windows.Input;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using JornaPay.Models;
using JornaPay.Services;
using iText.IO.Font;
using iText.IO.Font.Constants;

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

        public ICommand BuscarCommand { get; }
        public ICommand DescargarPdfCommand { get; }

        public ImportesPagadosViewModel(TrabajadoresServicio trabajadoresServicio)
        {
            _trabajadoresServicio = trabajadoresServicio;
            BuscarCommand = new Command(async () => await BuscarTrabajadores());
            DescargarPdfCommand = new Command(async () => await GenerarYDescargarPdf());
        }

        private async Task BuscarTrabajadores()
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

            var pagadosAgrupados = new Dictionary<string, decimal>();

            foreach (var trabajador in listaCompleta)
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);
                if (historial == null) continue;

                var registrosPagados = historial.Where(h => h.Pagado);

                foreach (var registro in registrosPagados)
                {
                    string clave = trabajador.Nombre + " " + trabajador.Apellidos;

                    if (!string.IsNullOrEmpty(trabajador.Nombre) && !string.IsNullOrEmpty(trabajador.Apellidos))
                    {
                        if (pagadosAgrupados.ContainsKey(clave))
                        {
                            pagadosAgrupados[clave] += registro.PrecioTotal;
                        }
                        else
                        {
                            pagadosAgrupados[clave] = registro.PrecioTotal;
                        }
                    }
                }
            }

            var trabajadoresFiltrados = pagadosAgrupados.Select(t => new TrabajadorDatos
            {
                Nombre = t.Key.Split(" ")[0],
                Apellidos = t.Key.Substring(t.Key.IndexOf(" ") + 1),
                ImporteTotal = t.Value,
                Pagado = true
            }).ToList();

            if (!string.IsNullOrWhiteSpace(NombreBusqueda))
            {
                trabajadoresFiltrados = trabajadoresFiltrados
                    .Where(t => $"{t.Nombre} {t.Apellidos}".Contains(NombreBusqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Trabajadores = trabajadoresFiltrados;
            ImporteTotalPagado = trabajadoresFiltrados.Sum(t => t.ImporteTotal);
            OnPropertyChanged(nameof(Trabajadores));
        }

        private async Task GenerarYDescargarPdf()
        {
            try
            {
                if (Trabajadores == null || !Trabajadores.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No hay datos disponibles para generar el PDF.", "OK");
                    return;
                }

                string fileName = "ImportesPagados.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                using (Document doc = new Document(pdf))
                {
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                    doc.Add(new Paragraph($"Historial de Importes Pagados").SetFont(boldFont).SetFontSize(16));
                    doc.Add(new Paragraph(" "));

                    foreach (var trabajador in Trabajadores)
                    {
                        doc.Add(new Paragraph($"Nombre: {trabajador.Nombre} {trabajador.Apellidos}"));
                        doc.Add(new Paragraph($"Total Pagado: {trabajador.ImporteTotal:C}"));
                        doc.Add(new Paragraph(" "));
                    }

                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph($"Total pagado: {ImporteTotalPagado:C}").SetFont(boldFont));
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

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}