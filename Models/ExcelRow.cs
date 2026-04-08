namespace ImportadorDeGTINEAN.Desktop.Models
{
    public class ExcelRow
    {
        public int RowNumber { get; set; }
        public string RawReference { get; set; } = string.Empty;
        public string RawBarcode { get; set; } = string.Empty;
        public string NormalizedReference { get; set; } = string.Empty;
        public string NormalizedBarcode { get; set; } = string.Empty;
    }
}
