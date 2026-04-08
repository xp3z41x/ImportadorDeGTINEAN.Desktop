namespace ImportadorDeGTINEAN.Desktop.Services
{
    public static class BarcodeValidatorService
    {
        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            return new string(raw.Trim().Where(char.IsDigit).ToArray());
        }

        public static (bool IsValid, string? ErrorMessage) Validate(string barcode)
        {
            var normalized = Normalize(barcode);

            if (string.IsNullOrEmpty(normalized))
                return (false, "Código de barras vazio");

            if (normalized.Length != 8 && normalized.Length != 13 && normalized.Length != 14)
                return (false, $"Tamanho inválido ({normalized.Length} dígitos). Esperado: 8 (EAN-8), 13 (EAN-13) ou 14 (GTIN-14)");

            if (!normalized.All(char.IsDigit))
                return (false, "Contém caracteres não numéricos");

            var checkDigit = CalculateCheckDigit(normalized[..^1]);
            var actualCheckDigit = int.Parse(normalized[^1].ToString());

            if (checkDigit != actualCheckDigit)
                return (false, $"Dígito verificador inválido. Esperado: {checkDigit}, encontrado: {actualCheckDigit}");

            return (true, null);
        }

        private static int CalculateCheckDigit(string digits)
        {
            var sum = 0;
            var isOdd = true;

            for (var i = digits.Length - 1; i >= 0; i--)
            {
                var digit = int.Parse(digits[i].ToString());
                sum += isOdd ? digit * 3 : digit;
                isOdd = !isOdd;
            }

            return (10 - (sum % 10)) % 10;
        }
    }
}
