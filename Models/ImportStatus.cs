namespace ImportadorDeGTINEAN.Desktop.Models
{
    public enum ImportStatus
    {
        Pending,
        Matched,
        NoMatch,
        InvalidBarcode,
        DuplicateBarcode,
        AlreadySet,
        Updated,
        Error
    }
}
