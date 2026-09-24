using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

const string IcpUrl = "https://icp.administracionelectronica.gob.es/icpplus/index.html";
var offices = new[]
{
    "AVDA POBLADOS", "COLMENAR VIEJO", "ALCALA DE HENARES", "ALCOBENDAS",
    "ALCORCON", "ARANJUEZ", "ARGANDA DEL REY", "COLLADO VILLALBA", "COSLADA",
    "FUENLABRADA", "GETAFE", "LAS ROZAS DE MADRID", "LEGANES", "MAJADAHONDA",
    "MOSTOLES", "PARLA", "POZUELO DE ALARCON", "RIVAS VACIAMADRID",
    "TORREJON DE ARDOZ", "VALDEMORO", "SAN FELIPE TIE", "GETAFE 2"
};

var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
var chatId = Environment.GetEnvironmentVariable("CHAT_ID");
if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
{
    Console.Error.WriteLine("Configura BOT_TOKEN y CHAT_ID como variables de entorno.");
    return 1;
}

var intervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("INTERVAL_SECONDS"), out var seconds)
    ? Math.Clamp(seconds, 30, 3600) : 300;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("TieAlertBot/1.0");
var stateFile = Environment.GetEnvironmentVariable("STATE_FILE") ?? "appointment-state.json";
var known = File.Exists(stateFile)
    ? new HashSet<string>(await File.ReadAllLinesAsync(stateFile), StringComparer.OrdinalIgnoreCase)
    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
await File.WriteAllLinesAsync(stateFile, known);
var runOnce = string.Equals(Environment.GetEnvironmentVariable("RUN_ONCE"), "true", StringComparison.OrdinalIgnoreCase);
Console.WriteLine(runOnce ? "Ejecución puntual iniciada." : $"Monitor iniciado; intervalo {intervalSeconds}s. Ctrl+C para salir.");

while (true)
{
    try
    {
        var html = await http.GetStringAsync(IcpUrl);
        var appointments = ParseAppointments(html, offices);
        var newAppointments = appointments
            .Where(a => !known.Contains($"{a.Office}|{a.Date}|{a.Time}"))
            .ToList();
        if (newAppointments.Count == 0)
            Console.WriteLine($"{DateTimeOffset.Now}: sin nuevas citas detectadas.");
        else
        {
            var message = "🚨 CITA TIE DISPONIBLE\n\n" + string.Join("\n", newAppointments.Select(a =>
                $"📍 {a.Office}\n📅 {a.Date}\n🕐 {a.Time}"));
            var response = await http.PostAsJsonAsync($"https://api.telegram.org/bot{botToken}/sendMessage",
                new TelegramMessage(chatId, message));
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"Alerta Telegram enviada ({newAppointments.Count}).");
            foreach (var appointment in newAppointments)
                known.Add($"{appointment.Office}|{appointment.Date}|{appointment.Time}");
            await File.WriteAllLinesAsync(stateFile, known.OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{DateTimeOffset.Now}: {ex.Message}");
        if (runOnce) return 2;
    }

    if (runOnce) break;
    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
}

return 0;

static List<Appointment> ParseAppointments(string html, string[] offices)
{
    var found = new List<Appointment>();
    foreach (Match row in Regex.Matches(html, "<tr\\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
    {
        var text = WebUtility.HtmlDecode(Regex.Replace(row.Groups[1].Value, "<[^>]+>", " "));
        var normalized = Normalize(text);
        if (!text.Contains('/')) continue;
        var date = Regex.Match(text, @"\b\d{1,2}/\d{1,2}/\d{2,4}\b").Value;
        var time = Regex.Match(text, @"\b\d{1,2}:\d{2}\b").Value;
        if (date.Length == 0) continue;
        foreach (var office in offices)
            if (normalized.Contains(Normalize(office), StringComparison.Ordinal))
                found.Add(new Appointment(office, date, time));
    }
    return found;
}

static string Normalize(string value)
{
    var decomposed = value.Normalize(NormalizationForm.FormD);
    var chars = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
    return Regex.Replace(new string(chars).ToUpperInvariant().Replace('.', ' '), @"\s+", " ").Trim();
}

record Appointment(string Office, string Date, string Time);
record TelegramMessage([property: JsonPropertyName("chat_id")] string ChatId,
                       [property: JsonPropertyName("text")] string Text);






