﻿using JornaPay.Models;
using JornaPay.Services;
using System.ComponentModel;
using System.Windows.Input;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace JornaPay.ViewModels
{
    public class ImportesImpagadosViewModel : INotifyPropertyChanged
    {
        private readonly TrabajadoresServicio _trabajadoresServicio;
        private string _nombreBusqueda;
        private List<TrabajadorDatos> _trabajadores;
        private decimal _importeTotalImpagado;

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

        public decimal ImporteTotalImpagado
        {
            get => _importeTotalImpagado;
            set
            {
                _importeTotalImpagado = value;
                OnPropertyChanged(nameof(ImporteTotalImpagado));
            }
        }

        public ICommand BuscarCommand { get; }
        public ICommand DescargarPdfCommand { get; }

        public ImportesImpagadosViewModel(TrabajadoresServicio trabajadoresServicio)
        {
            _trabajadoresServicio = trabajadoresServicio;
            BuscarCommand = new Command(async () => await BuscarTrabajadores());
            DescargarPdfCommand = new Command(async () => await GenerarYDescargarPdf());

            //Ejecutar búsqueda automáticamente al cargar el ViewModel
            BuscarCommand.Execute(null);
        }

        private async Task BuscarTrabajadores()
        {
            if (string.IsNullOrEmpty(SesionUsuario.NombreUsuarioActual))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No hay un usuario activo.", "OK");
                return;
            }

            var listaCompleta = await _trabajadoresServicio.ObtenerTrabajadoresPorUsuarioAsync(SesionUsuario.NombreUsuarioActual);
            var registrosAgrupados = new Dictionary<string, decimal>();

            foreach (var trabajador in listaCompleta)
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);
                if (historial == null) continue;

                var registrosFiltrados = historial.Where(h => !h.Pagado);            

                foreach (var registro in registrosFiltrados)
                {
                    string clave = $"{trabajador.Nombre} {trabajador.Apellidos}";
                    registrosAgrupados[clave] = registrosAgrupados.GetValueOrDefault(clave, 0) + registro.PrecioTotal;
                }
            }

            var trabajadoresFiltrados = registrosAgrupados.Select(t => new TrabajadorDatos
            {
                Nombre = t.Key.Split(" ")[0],
                Apellidos = t.Key.Substring(t.Key.IndexOf(" ") + 1),
                ImporteTotal = t.Value,
                Pagado = false 
            }).ToList();

            //Realizo la búsqueda por nombre
            if (!string.IsNullOrWhiteSpace(NombreBusqueda))
            {
                var nombreBusquedaLower = NombreBusqueda.ToLower();
                trabajadoresFiltrados = trabajadoresFiltrados
                    .Where(t => $"{t.Nombre} {t.Apellidos}".ToLower().Contains(nombreBusquedaLower))
                    .ToList();
            }


            Trabajadores = trabajadoresFiltrados;
            ImporteTotalImpagado = trabajadoresFiltrados.Sum(t => t.ImporteTotal);// Calculo el total impagado

            OnPropertyChanged(nameof(Trabajadores));
            OnPropertyChanged(nameof(ImporteTotalImpagado)); //Actualizo el importe
        }

        private async Task GenerarYDescargarPdf()
        {
            try
            {
                if (Trabajadores == null || !Trabajadores.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No hay trabajadores con importes impagados.", "OK");
                    return;
                }

                string fileName = "ImportesImpagados.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using (PdfWriter writer = new PdfWriter(filePath))
                using (PdfDocument pdf = new PdfDocument(writer))
                using (Document doc = new Document(pdf))
                {
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                    doc.Add(new Paragraph("Importes Impagados").SetFont(boldFont).SetFontSize(16));
                    doc.Add(new Paragraph(" "));

                    foreach (var trabajador in Trabajadores)
                    {
                        doc.Add(new Paragraph($"Nombre: {trabajador.Nombre} {trabajador.Apellidos}"));
                        doc.Add(new Paragraph($"Importe Impagado: {trabajador.ImporteTotal:C}"));
                        doc.Add(new Paragraph(" "));
                    }

                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph($"Total impagado: {ImporteTotalImpagado:C}").SetFont(boldFont));
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
