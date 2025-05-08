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
            var listaCompleta = await _trabajadoresServicio.ObtenerTodosTrabajadoresAsync();
            var impagadosAgrupados = new Dictionary<string, decimal>();

            foreach (var trabajador in listaCompleta)
            {
                var historial = await _trabajadoresServicio.ObtenerHistorialPorTrabajadorAsync(trabajador.Id);
                var registrosImpagados = historial.Where(h => !h.Pagado);

                foreach (var registro in registrosImpagados)
                {
                    string clave = trabajador.Nombre + " " + trabajador.Apellidos;
                    impagadosAgrupados[clave] = impagadosAgrupados.GetValueOrDefault(clave, 0) + registro.PrecioTotal;
                }
            }

            Trabajadores = impagadosAgrupados.Select(t => new TrabajadorDatos
            {
                Nombre = t.Key.Split(" ")[0],
                Apellidos = t.Key.Substring(t.Key.IndexOf(" ") + 1),
                ImporteTotal = t.Value,
                Pagado = false
            }).ToList();

            ImporteTotalImpagado = Trabajadores.Sum(t => t.ImporteTotal);
            OnPropertyChanged(nameof(Trabajadores));
        }

        private async Task GenerarYDescargarPdf()
        {
            try
            {
#if ANDROID
        var pdf = new Android.Graphics.Pdf.PdfDocument();
        var pageInfo = new Android.Graphics.Pdf.PdfDocument.PageInfo.Builder(595, 842, 1).Create();
        var page = pdf.StartPage(pageInfo);
        var canvas = page.Canvas;
        var paint = new Android.Graphics.Paint();
        paint.Color = Android.Graphics.Color.Black;
        paint.TextSize = 18;

        int y = 50;
        canvas.DrawText("Importes Impagados", 50, y, paint);
        y += 30;

        foreach (var trabajador in Trabajadores)
        {
            canvas.DrawText($"{trabajador.Nombre} {trabajador.Apellidos} - {trabajador.ImporteTotal:C}", 50, y, paint);
            y += 25;
        }

        y += 30;
        canvas.DrawText($"Total impagado: {ImporteTotalImpagado:C}", 50, y, paint);

        pdf.FinishPage(page);

        var fileName = "ImportesImpagados.pdf";
        var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
        var filePath = Path.Combine(downloadsPath.AbsolutePath, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            pdf.WriteTo(fileStream);
        }

        pdf.Close();

        await Application.Current.MainPage.DisplayAlert("Éxito", "PDF guardado en la carpeta Descargas.", "OK");

        //Abrir el PDF automáticamente después de guardarlo
        var file = new Java.IO.File(filePath);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            Android.App.Application.Context,
            $"{Android.App.Application.Context.PackageName}.fileprovider",
            file);

        var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
        intent.SetDataAndType(uri, "application/pdf");
        intent.SetFlags(Android.Content.ActivityFlags.NoHistory | 
                        Android.Content.ActivityFlags.NewTask | 
                        Android.Content.ActivityFlags.GrantReadUriPermission);

        Android.App.Application.Context.StartActivity(intent);

#else
                await Application.Current.MainPage.DisplayAlert("Error", "Generación de PDF solo disponible en Android.", "OK");
#endif
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
