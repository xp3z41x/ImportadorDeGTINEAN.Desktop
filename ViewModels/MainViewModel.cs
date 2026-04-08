using ImportadorDeGTINEAN.Desktop.Models;
using ImportadorDeGTINEAN.Desktop.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ImportadorDeGTINEAN.Desktop.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _filePath = string.Empty;
        private string _logText = string.Empty;
        private bool _isAnalyzing;
        private bool _isUpdating;
        private bool _canExecuteUpdate;
        private int _countTotal;
        private int _countMatched;
        private int _countNoMatch;
        private int _countInvalidBarcode;
        private int _countDuplicate;
        private int _countAlreadySet;
        private int _countUpdated;
        private int _countError;
        private int _countSelected;
        private string? _selectedBrand;
        private bool _hasResults;

        private bool _lastUseExactMatch;
        private readonly object _resultsLock = new();

        public ObservableCollection<AnalysisResult> Results { get; } = [];
        public ObservableCollection<string> AvailableBrands { get; } = [];

        /// <summary>Raised on UI thread after analysis results are loaded into the grid.</summary>
        public event Action? AnalysisCompleted;

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    ((RelayCommand)AnalyzeSmartCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)AnalyzeExactCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                if (SetProperty(ref _isAnalyzing, value))
                {
                    ((RelayCommand)AnalyzeSmartCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)AnalyzeExactCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ExecuteUpdateCommand).RaiseCanExecuteChanged();
                    (FixDuplicatesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                if (SetProperty(ref _isUpdating, value))
                {
                    ((RelayCommand)ExecuteUpdateCommand).RaiseCanExecuteChanged();
                    (FixDuplicatesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanExecuteUpdate
        {
            get => _canExecuteUpdate;
            set
            {
                if (SetProperty(ref _canExecuteUpdate, value))
                    ((RelayCommand)ExecuteUpdateCommand).RaiseCanExecuteChanged();
            }
        }

        public int CountTotal { get => _countTotal; set => SetProperty(ref _countTotal, value); }
        public int CountMatched { get => _countMatched; set => SetProperty(ref _countMatched, value); }
        public int CountNoMatch { get => _countNoMatch; set => SetProperty(ref _countNoMatch, value); }
        public int CountInvalidBarcode { get => _countInvalidBarcode; set => SetProperty(ref _countInvalidBarcode, value); }
        public int CountDuplicate
        {
            get => _countDuplicate;
            set
            {
                if (SetProperty(ref _countDuplicate, value))
                    (FixDuplicatesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        public int CountAlreadySet { get => _countAlreadySet; set => SetProperty(ref _countAlreadySet, value); }
        public int CountUpdated { get => _countUpdated; set => SetProperty(ref _countUpdated, value); }
        public int CountError { get => _countError; set => SetProperty(ref _countError, value); }
        public int CountSelected { get => _countSelected; set => SetProperty(ref _countSelected, value); }

        public string? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (SetProperty(ref _selectedBrand, value))
                    (SelectByBrandCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand AnalyzeSmartCommand { get; }
        public ICommand AnalyzeExactCommand { get; }
        public ICommand ExecuteUpdateCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand SelectByBrandCommand { get; }
        public ICommand FixDuplicatesCommand { get; }

        public MainViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(Results, _resultsLock);

            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            AnalyzeSmartCommand = new RelayCommand(async _ => await AnalyzeAsync(useExactMatch: false), _ => !string.IsNullOrEmpty(FilePath) && !IsAnalyzing);
            AnalyzeExactCommand = new RelayCommand(async _ => await AnalyzeAsync(useExactMatch: true), _ => !string.IsNullOrEmpty(FilePath) && !IsAnalyzing);
            ExecuteUpdateCommand = new RelayCommand(async _ => await ExecuteUpdateAsync(), _ => CanExecuteUpdate && !IsUpdating && !IsAnalyzing);
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            SelectAllCommand = new RelayCommand(_ => SelectAll());
            DeselectAllCommand = new RelayCommand(_ => DeselectAll());
            SelectByBrandCommand = new RelayCommand(_ => SelectByBrand(), _ => !string.IsNullOrEmpty(SelectedBrand));
            FixDuplicatesCommand = new RelayCommand(async _ => await FixDuplicatesAsync(), _ => CountDuplicate > 0 && !IsAnalyzing && !IsUpdating);
        }

        private void BrowseFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Planilhas Excel (*.xlsx)|*.xlsx",
                Title = "Selecionar planilha Excel"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
                AppendLog($"Arquivo selecionado: {FilePath}");
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
        }

        private async Task AnalyzeAsync(bool useExactMatch = false)
        {
            _lastUseExactMatch = useExactMatch;
            IsAnalyzing = true;
            CanExecuteUpdate = false;
            UnsubscribeResults();
            Results.Clear();
            ResetCounters();

            try
            {
                if (!System.IO.File.Exists(FilePath))
                {
                    AppendLog("ERRO: Arquivo não encontrado.");
                    MessageBox.Show("O arquivo selecionado não foi encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                AppendLog("Lendo arquivo Excel...");
                var settings = SettingsService.LoadConnectionSettings();
                var db = new DatabaseService(settings.Host, settings.Port, settings.Database, settings.User, settings.Password);

                // Run all heavy work on background thread
                var analysisResult = await Task.Run(async () =>
                {
                    // 1. Read Excel
                    var rows = ExcelReaderService.ReadFileSync(FilePath);

                    // 2. Load all DB references (single query)
                    var dbRecords = await db.GetAllReferencesAsync();

                    // 3. Load all existing barcodes (single query — replaces N per-row queries)
                    var allBarcodes = await db.GetAllBarcodesAsync();

                    // 4. Process each row — all in memory, no UI interaction
                    var resultList = new List<AnalysisResult>();
                    var batchBarcodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    int cTotal = rows.Count, cMatched = 0, cNoMatch = 0,
                        cInvalid = 0, cDuplicate = 0, cAlreadySet = 0, cError = 0;

                    foreach (var row in rows)
                    {
                        var result = new AnalysisResult
                        {
                            RowNumber = row.RowNumber,
                            SpreadsheetReference = row.RawReference,
                            SpreadsheetBarcode = row.RawBarcode
                        };

                        // Validate barcode
                        var (isValid, errorMsg) = BarcodeValidatorService.Validate(row.RawBarcode);
                        if (!isValid)
                        {
                            result.Status = ImportStatus.InvalidBarcode;
                            result.StatusMessage = errorMsg!;
                            resultList.Add(result);
                            cInvalid++;
                            continue;
                        }

                        var normalizedBarcode = BarcodeValidatorService.Normalize(row.RawBarcode);

                        // Find matching DB reference
                        string? matchedRef = null;
                        string? matchedCurrentBarcode = null;
                        string? matchedDescricao = null;
                        string? matchedMarca = null;

                        foreach (var (dbRef, dbBarcode, dbDescricao, dbMarca) in dbRecords)
                        {
                            var isMatch = useExactMatch
                                ? ReferenceMatcherService.IsExactMatch(row.RawReference, dbRef)
                                : ReferenceMatcherService.IsMatch(row.RawReference, dbRef);

                            if (isMatch)
                            {
                                matchedRef = dbRef;
                                matchedCurrentBarcode = dbBarcode;
                                matchedDescricao = dbDescricao;
                                matchedMarca = dbMarca;
                                break;
                            }
                        }

                        if (matchedRef == null)
                        {
                            result.Status = ImportStatus.NoMatch;
                            result.StatusMessage = "Referência não encontrada no banco";
                            resultList.Add(result);
                            cNoMatch++;
                            continue;
                        }

                        result.MatchedDbReference = matchedRef;
                        result.Descricao = matchedDescricao;
                        result.Marca = matchedMarca;

                        // Check if already has this barcode
                        if (!string.IsNullOrEmpty(matchedCurrentBarcode) &&
                            matchedCurrentBarcode.Trim().Equals(normalizedBarcode, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Status = ImportStatus.AlreadySet;
                            result.StatusMessage = "Código de barras já cadastrado neste produto";
                            resultList.Add(result);
                            cAlreadySet++;
                            continue;
                        }

                        // Check duplicate barcode in DB (in-memory HashSet lookup)
                        if (allBarcodes.Contains(normalizedBarcode))
                        {
                            result.Status = ImportStatus.DuplicateBarcode;
                            result.StatusMessage = "Código de barras já existe em outro produto";
                            resultList.Add(result);
                            cDuplicate++;
                            continue;
                        }

                        // Check intra-batch duplicate
                        if (batchBarcodes.TryGetValue(normalizedBarcode, out var existingRow))
                        {
                            result.Status = ImportStatus.DuplicateBarcode;
                            result.StatusMessage = $"Código de barras duplicado na planilha (linha {existingRow})";
                            resultList.Add(result);
                            cDuplicate++;
                            continue;
                        }

                        batchBarcodes[normalizedBarcode] = row.RowNumber;

                        // All checks passed
                        result.Status = ImportStatus.Matched;
                        result.StatusMessage = "Pronto para atualizar";
                        result.IsSelected = true;
                        resultList.Add(result);
                        cMatched++;
                    }

                    // Extract distinct brands from matched items
                    var brands = resultList
                        .Where(r => r.Status == ImportStatus.Matched && !string.IsNullOrWhiteSpace(r.Marca))
                        .Select(r => r.Marca!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(m => m)
                        .ToList();

                    return (resultList, brands, cTotal, cMatched, cNoMatch, cInvalid, cDuplicate, cAlreadySet, cError);
                });

                // Back on UI thread — populate everything in one batch
                var (items, brandList, total, matched, noMatch, invalid, duplicate, alreadySet, error) = analysisResult;

                foreach (var item in items)
                {
                    item.PropertyChanged += OnResultPropertyChanged;
                    Results.Add(item);
                }

                AvailableBrands.Clear();
                foreach (var brand in brandList)
                    AvailableBrands.Add(brand);

                CountTotal = total;
                CountMatched = matched;
                CountNoMatch = noMatch;
                CountInvalidBarcode = invalid;
                CountDuplicate = duplicate;
                CountAlreadySet = alreadySet;
                CountError = error;
                UpdateSelectedCount();
                HasResults = Results.Count > 0;

                var modeLabel = useExactMatch ? "exata" : "inteligente";
                AppendLog($"Análise {modeLabel}: {total} linhas analisadas. {matched} prontos para atualizar, {noMatch} não encontrados, {invalid} códigos inválidos, {duplicate} duplicados, {alreadySet} já cadastrados.");
                CanExecuteUpdate = CountSelected > 0;

                AnalysisCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                AppendLog($"ERRO na análise: {ex.Message}");
                MessageBox.Show($"Erro ao analisar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private async Task ExecuteUpdateAsync()
        {
            UpdateSelectedCount();
            var confirm = MessageBox.Show(
                $"Deseja atualizar {CountSelected} registro(s) selecionado(s) no banco de dados?",
                "Confirmar Atualização",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsUpdating = true;
            var settings = SettingsService.LoadConnectionSettings();
            var db = new DatabaseService(settings.Host, settings.Port, settings.Database, settings.User, settings.Password);

            AppendLog($"Iniciando atualização de {CountSelected} registro(s)...");
            var updated = 0;
            var errors = 0;

            foreach (var result in Results)
            {
                if (!result.IsSelected || result.Status != ImportStatus.Matched || result.MatchedDbReference == null)
                    continue;

                try
                {
                    var normalizedBarcode = BarcodeValidatorService.Normalize(result.SpreadsheetBarcode);
                    var rowsAffected = await db.UpdateBarcodeAsync(result.MatchedDbReference, normalizedBarcode);

                    if (rowsAffected > 0)
                    {
                        result.Status = ImportStatus.Updated;
                        result.StatusMessage = "Atualizado com sucesso";
                        updated++;
                    }
                    else
                    {
                        result.Status = ImportStatus.Error;
                        result.StatusMessage = "Nenhuma linha afetada";
                        errors++;
                    }
                }
                catch (Exception ex)
                {
                    result.Status = ImportStatus.Error;
                    result.StatusMessage = $"Erro: {ex.Message}";
                    errors++;
                    AppendLog($"ERRO ao atualizar {result.MatchedDbReference}: {ex.Message}");
                }
            }

            CountUpdated = updated;
            CountError = errors;
            CanExecuteUpdate = false;

            AppendLog($"Atualização concluída. {updated} atualizado(s), {errors} erro(s).");
            IsUpdating = false;
        }

        private void SelectAll()
        {
            foreach (var result in Results)
            {
                if (result.Status == ImportStatus.Matched)
                    result.IsSelected = true;
            }
            UpdateSelectedCount();
        }

        private void DeselectAll()
        {
            foreach (var result in Results)
                result.IsSelected = false;
            UpdateSelectedCount();
        }

        private void SelectByBrand()
        {
            if (string.IsNullOrEmpty(SelectedBrand))
                return;

            // First deselect all, then select only the chosen brand
            foreach (var result in Results)
            {
                if (result.Status == ImportStatus.Matched &&
                    string.Equals(result.Marca, SelectedBrand, StringComparison.OrdinalIgnoreCase))
                    result.IsSelected = true;
                else
                    result.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        private async Task FixDuplicatesAsync()
        {
            var duplicates = Results
                .Where(r => r.Status == ImportStatus.DuplicateBarcode && r.MatchedDbReference != null)
                .ToList();

            if (duplicates.Count == 0)
                return;

            var confirm = MessageBox.Show(
                $"Deseja limpar o código de barras de {duplicates.Count} produto(s) com duplicidade no banco de dados?\n\n" +
                "Os códigos de barras serão removidos dos produtos INCORRETOS, " +
                "permitindo que sejam atribuídos aos produtos corretos da planilha.\n\n" +
                "Após a correção, a análise será refeita automaticamente.",
                "Corrigir Duplicados",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            IsUpdating = true;
            var settings = SettingsService.LoadConnectionSettings();
            var db = new DatabaseService(settings.Host, settings.Port, settings.Database, settings.User, settings.Password);

            var corrected = 0;
            var errors = 0;

            foreach (var item in duplicates)
            {
                try
                {
                    var normalizedBarcode = BarcodeValidatorService.Normalize(item.SpreadsheetBarcode);
                    var rowsAffected = await db.ClearBarcodeFromOtherProductsAsync(normalizedBarcode, item.MatchedDbReference!);
                    if (rowsAffected > 0)
                    {
                        corrected++;
                        AppendLog($"Corrigido: barcode {normalizedBarcode} limpo de {rowsAffected} produto(s) incorreto(s)");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    AppendLog($"ERRO ao corrigir duplicado {item.SpreadsheetBarcode}: {ex.Message}");
                }
            }

            AppendLog($"Correção concluída: {corrected} corrigido(s), {errors} erro(s). Refazendo análise...");
            IsUpdating = false;

            await AnalyzeAsync(_lastUseExactMatch);
        }

        private void UpdateSelectedCount()
        {
            CountSelected = Results.Count(r => r.IsSelected && r.Status == ImportStatus.Matched);
            CanExecuteUpdate = CountSelected > 0;
        }

        private void ResetCounters()
        {
            CountTotal = 0;
            CountMatched = 0;
            CountNoMatch = 0;
            CountInvalidBarcode = 0;
            CountDuplicate = 0;
            CountAlreadySet = 0;
            CountUpdated = 0;
            CountError = 0;
            CountSelected = 0;
            HasResults = false;
            AvailableBrands.Clear();
            SelectedBrand = null;
            _logLines.Clear();
            LogText = string.Empty;
        }

        private void OnResultPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AnalysisResult.IsSelected))
                UpdateSelectedCount();
        }

        private void UnsubscribeResults()
        {
            foreach (var item in Results)
                item.PropertyChanged -= OnResultPropertyChanged;
        }

        private readonly List<string> _logLines = [];
        private const int MaxLogLines = 500;

        private void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logLines.Add($"[{timestamp}] {message}");
            if (_logLines.Count > MaxLogLines)
                _logLines.RemoveAt(0);
            LogText = string.Join("\n", _logLines);
        }
    }
}
