using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace BauVoraStockBridge;

internal sealed class Settings
{
    public string SqlConnectionString { get; set; } = "";
    public string SqlQuery { get; set; } = "";
    public string ApiUrl { get; set; } = "";
    public string ApiToken { get; set; } = "";
    public string SourceName { get; set; } = "BAUMARKET_VORA";
    public int SyncIntervalMinutes { get; set; } = 5;
    public int CommandTimeoutSeconds { get; set; } = 120;
}

internal sealed record StockItem(
    string ProductCode,
    string? Barcode,
    string ProductName,
    decimal StockQty,
    decimal? Price,
    decimal? PurchasePriceWithVat,
    decimal? SalePriceWithVat
);

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private static string LogPath => Path.Combine(AppContext.BaseDirectory, "logs", "bridge.log");

    public static async Task Main()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath))
        {
            Log("MUNGON appsettings.json. Kopjo appsettings.example.json në appsettings.json dhe plotëso konfigurimin.");
            return;
        }

        Settings settings;
        try
        {
            settings = JsonSerializer.Deserialize<Settings>(await File.ReadAllTextAsync(configPath), JsonOptions)
                       ?? throw new InvalidOperationException("Konfigurimi bosh.");
            Validate(settings);
        }
        catch (Exception ex)
        {
            Log("Gabim konfigurimi: " + ex.Message);
            return;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiToken);
        Log($"Stock Sync Bridge u nis për burimin {settings.SourceName}. Intervali: {settings.SyncIntervalMinutes} minuta.");

        while (true)
        {
            try
            {
                var rows = await ReadStock(settings);
                if (rows.Count == 0)
                {
                    Log("Query ktheu 0 produkte; dërgimi u anashkalua për të shmangur fshirjen/çaktivizimin aksidental të gjendjeve online.");
                }
                else
                {
                    var payload = JsonSerializer.Serialize(new { source = settings.SourceName.Trim().ToUpperInvariant(), generatedAtUtc = DateTime.UtcNow, items = rows }, JsonOptions);
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await http.PostAsync(settings.ApiUrl, content);
                    var body = await response.Content.ReadAsStringAsync();
                    response.EnsureSuccessStatusCode();
                    Log($"Sinkronizimi përfundoi: {rows.Count} produkte. Përgjigjja API: {Trim(body, 500)}");
                }
            }
            catch (Exception ex)
            {
                Log("Sinkronizimi dështoi: " + ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(settings.SyncIntervalMinutes, 1, 1440)));
        }
    }

    private static async Task<List<StockItem>> ReadStock(Settings s)
    {
        var result = new List<StockItem>();
        await using var connection = new SqlConnection(s.SqlConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(s.SqlQuery, connection) { CommandTimeout = s.CommandTimeoutSeconds };
        await using var reader = await command.ExecuteReaderAsync();
        var codeOrd = reader.GetOrdinal("ProductCode");
        var barcodeOrd = reader.GetOrdinal("Barcode");
        var nameOrd = reader.GetOrdinal("ProductName");
        var qtyOrd = reader.GetOrdinal("StockQty");
        var priceOrd = reader.GetOrdinal("Price");
        var purchasePriceOrd = reader.GetOrdinal("PurchasePriceWithVat");
        var salePriceOrd = reader.GetOrdinal("SalePriceWithVat");

        while (await reader.ReadAsync())
        {
            var code = reader.IsDBNull(codeOrd) ? "" : Convert.ToString(reader.GetValue(codeOrd))?.Trim() ?? "";
            var name = reader.IsDBNull(nameOrd) ? "" : Convert.ToString(reader.GetValue(nameOrd))?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(code)) continue;

            result.Add(new StockItem(
                code,
                reader.IsDBNull(barcodeOrd) ? null : Convert.ToString(reader.GetValue(barcodeOrd))?.Trim(),
                name,
                reader.IsDBNull(qtyOrd) ? 0m : Convert.ToDecimal(reader.GetValue(qtyOrd)),
                reader.IsDBNull(priceOrd) ? null : Convert.ToDecimal(reader.GetValue(priceOrd)),
                reader.IsDBNull(purchasePriceOrd) ? null : Convert.ToDecimal(reader.GetValue(purchasePriceOrd)),
                reader.IsDBNull(salePriceOrd) ? null : Convert.ToDecimal(reader.GetValue(salePriceOrd))
            ));
        }
        return result;
    }

    private static void Validate(Settings s)
    {
        if (string.IsNullOrWhiteSpace(s.SqlConnectionString)) throw new Exception("SqlConnectionString është bosh në appsettings.json që ndodhet te: " + AppContext.BaseDirectory);
        if (string.IsNullOrWhiteSpace(s.SqlQuery)) throw new Exception("SqlQuery nuk është vendosur.");
        if (string.IsNullOrWhiteSpace(s.ApiUrl) || !Uri.TryCreate(s.ApiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new Exception("ApiUrl duhet të jetë URL HTTPS e vlefshme.");
        if (string.IsNullOrWhiteSpace(s.ApiToken)) throw new Exception("ApiToken nuk është vendosur.");
        if (string.IsNullOrWhiteSpace(s.SourceName) || !System.Text.RegularExpressions.Regex.IsMatch(s.SourceName, "^[A-Za-z0-9_-]{2,32}$")) throw new Exception("SourceName duhet të jetë identifikues si KRAFT ose BAUVORA.");
        if (s.SyncIntervalMinutes < 1) throw new Exception("SyncIntervalMinutes duhet të jetë të paktën 1.");
    }

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        Console.WriteLine(line);
        File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "...";
}
