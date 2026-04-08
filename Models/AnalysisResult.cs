using ImportadorDeGTINEAN.Desktop.ViewModels;

namespace ImportadorDeGTINEAN.Desktop.Models
{
    public class AnalysisResult : BaseViewModel
    {
        private ImportStatus _status;
        private string _statusMessage = string.Empty;
        private bool _isSelected;

        public int RowNumber { get; set; }
        public string SpreadsheetReference { get; set; } = string.Empty;
        public string SpreadsheetBarcode { get; set; } = string.Empty;
        public string? MatchedDbReference { get; set; }
        public string? Descricao { get; set; }
        public string? Marca { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool CanBeSelected => Status == ImportStatus.Matched;

        public ImportStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    if (value != ImportStatus.Matched)
                        IsSelected = false;
                    OnPropertyChanged(nameof(CanBeSelected));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
    }
}
