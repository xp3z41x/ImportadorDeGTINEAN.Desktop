using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ImportadorDeGTINEAN.Desktop.Models;
using ImportadorDeGTINEAN.Desktop.ViewModels;

namespace ImportadorDeGTINEAN.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AnalysisCompleted += OnAnalysisCompleted;
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AnalysisCompleted -= OnAnalysisCompleted;
            }
        }

        private void OnAnalysisCompleted()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, AutoFitColumns);
        }

        private void AutoFitColumns()
        {
            if (DataContext is not MainViewModel vm || vm.Results.Count == 0)
                return;

            var grid = ResultsDataGrid;
            var dpi = VisualTreeHelper.GetDpi(this);
            var pixelsPerDip = dpi.PixelsPerDip;

            // DataGrid font from the grid itself
            var typeface = new Typeface(
                grid.FontFamily,
                grid.FontStyle,
                grid.FontWeight,
                grid.FontStretch);
            var fontSize = grid.FontSize;

            // MaterialDesign CellPadding is "8 4" => 8px left + 8px right = 16px horizontal
            const double cellPadding = 20; // 16 + small extra margin for safety

            // Column definitions: header text -> function to get cell text
            var columnDefs = new (string Header, Func<AnalysisResult, string?> GetValue)[]
            {
                ("Referência (Planilha)", r => r.SpreadsheetReference),
                ("Código de Barras",     r => r.SpreadsheetBarcode),
                ("Referência (Banco)",   r => r.MatchedDbReference),
                ("Descrição",            r => r.Descricao),
                ("Marca",                r => r.Marca),
                ("Status",               r => StatusToText(r.Status)),
                ("Mensagem",             r => r.StatusMessage),
            };

            // Build a map of header text -> desired pixel width
            var widthMap = new Dictionary<string, double>();

            foreach (var (header, getValue) in columnDefs)
            {
                // Start with header width
                var maxWidth = MeasureText(header, typeface, fontSize, FontWeights.Bold, pixelsPerDip);

                // Measure every cell value
                foreach (var result in vm.Results)
                {
                    var text = getValue(result);
                    if (string.IsNullOrEmpty(text))
                        continue;

                    var w = MeasureText(text, typeface, fontSize, FontWeights.Normal, pixelsPerDip);
                    if (w > maxWidth)
                        maxWidth = w;
                }

                widthMap[header] = maxWidth + cellPadding;
            }

            // Apply to grid columns
            foreach (var col in grid.Columns)
            {
                var headerText = col.Header as string;
                if (headerText != null && widthMap.TryGetValue(headerText, out var desiredWidth))
                {
                    col.Width = new DataGridLength(desiredWidth, DataGridLengthUnitType.Pixel);
                }
            }
        }

        private static double MeasureText(string text, Typeface typeface, double fontSize, FontWeight weight, double pixelsPerDip)
        {
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(typeface.FontFamily, typeface.Style, weight, typeface.Stretch),
                fontSize,
                Brushes.Black,
                pixelsPerDip);
            return ft.WidthIncludingTrailingWhitespace;
        }

        private static string StatusToText(ImportStatus status)
        {
            return status switch
            {
                ImportStatus.Pending => "Pendente",
                ImportStatus.Matched => "OK",
                ImportStatus.Updated => "Atualizado",
                ImportStatus.NoMatch => "Não Encontrado",
                ImportStatus.InvalidBarcode => "EAN Inválido",
                ImportStatus.DuplicateBarcode => "Duplicado",
                ImportStatus.AlreadySet => "Já Existe",
                ImportStatus.Error => "Erro",
                _ => ""
            };
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
