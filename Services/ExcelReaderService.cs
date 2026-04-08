using ClosedXML.Excel;
using ImportadorDeGTINEAN.Desktop.Models;

namespace ImportadorDeGTINEAN.Desktop.Services
{
    public static class ExcelReaderService
    {
        public static Task<List<ExcelRow>> ReadFileAsync(string filePath)
        {
            return Task.Run(() => ReadFileSync(filePath));
        }

        public static List<ExcelRow> ReadFileSync(string filePath)
        {
            var rows = new List<ExcelRow>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                return rows;

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (var i = 1; i <= lastRow; i++)
            {
                var cellA = worksheet.Cell(i, 1).GetFormattedString().Trim();
                var cellB = worksheet.Cell(i, 2).GetFormattedString().Trim();

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(cellA) && string.IsNullOrWhiteSpace(cellB))
                    continue;

                // Skip header row (heuristic: if column A contains common header words)
                if (i <= 2 && IsHeaderRow(cellA))
                    continue;

                // Skip rows without barcode
                if (string.IsNullOrWhiteSpace(cellB))
                    continue;

                rows.Add(new ExcelRow
                {
                    RowNumber = i,
                    RawReference = cellA,
                    RawBarcode = cellB,
                    NormalizedReference = ReferenceMatcherService.NormalizeReference(cellA),
                    NormalizedBarcode = BarcodeValidatorService.Normalize(cellB)
                });
            }

            return rows;
        }

        private static bool IsHeaderRow(string value)
        {
            var lower = value.ToLowerInvariant();
            return lower.Contains("refer") ||
                   lower.Contains("código") ||
                   lower.Contains("codigo") ||
                   lower.Contains("produto") ||
                   lower.Contains("descri") ||
                   lower.Contains("ean") ||
                   lower.Contains("gtin") ||
                   lower.Contains("barras");
        }
    }
}
