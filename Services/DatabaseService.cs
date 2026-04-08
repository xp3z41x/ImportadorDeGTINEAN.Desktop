using Npgsql;

namespace ImportadorDeGTINEAN.Desktop.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string host, string port, string database, string user, string password)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(port, out var p) ? p : 5432,
                Database = database,
                Username = user,
                Password = password,
                Timeout = 10
            };
            _connectionString = builder.ConnectionString;
        }

        public async Task<bool> TestConnectionAsync()
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            return true;
        }

        public async Task<List<(string Referencia, string? CodigoBarra, string? Descricao, string? Marca)>> GetAllReferencesAsync()
        {
            var results = new List<(string, string?, string?, string?)>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                @"SELECT c.referencia, c.codigo_barra, c.descricao, f.descricao AS marca_nome
                  FROM cadpro c
                  LEFT JOIN formar f ON c.marca = f.marca
                  WHERE c.referencia IS NOT NULL", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var referencia = reader.GetString(0);
                var codigoBarra = reader.IsDBNull(1) ? null : reader.GetString(1);
                var descricao = reader.IsDBNull(2) ? null : reader.GetString(2);
                var marcaNome = reader.IsDBNull(3) ? null : reader.GetString(3);
                results.Add((referencia, codigoBarra, descricao, marcaNome));
            }

            return results;
        }

        public async Task<HashSet<string>> GetAllBarcodesAsync()
        {
            var barcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT codigo_barra FROM cadpro WHERE codigo_barra IS NOT NULL AND codigo_barra <> ''", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                barcodes.Add(reader.GetString(0));

            return barcodes;
        }

        public async Task<bool> BarcodeExistsAsync(string barcode, string excludeReferencia)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM cadpro WHERE codigo_barra = @barcode AND referencia <> @ref", conn);
            cmd.Parameters.AddWithValue("barcode", barcode);
            cmd.Parameters.AddWithValue("ref", excludeReferencia);

            var result = await cmd.ExecuteScalarAsync();
            var count = result is long l ? l : Convert.ToInt64(result ?? 0);
            return count > 0;
        }

        public async Task<int> UpdateBarcodeAsync(string referencia, string barcode)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE cadpro SET codigo_barra = @barcode WHERE referencia = @ref", conn);
            cmd.Parameters.AddWithValue("barcode", barcode);
            cmd.Parameters.AddWithValue("ref", referencia);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> ClearBarcodeFromOtherProductsAsync(string barcode, string keepReferencia)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "UPDATE cadpro SET codigo_barra = '' WHERE codigo_barra = @barcode AND referencia <> @ref", conn);
            cmd.Parameters.AddWithValue("barcode", barcode);
            cmd.Parameters.AddWithValue("ref", keepReferencia);

            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
