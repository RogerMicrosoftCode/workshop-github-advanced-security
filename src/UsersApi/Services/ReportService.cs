// ══════════════════════════════════════════════════════════════════════════════
// ReportService.cs — GHAS Demo: Code Scanning + Dependabot
//
// Este archivo demuestra vulnerabilidades adicionales que CodeQL detecta:
//
//  1. PATH TRAVERSAL (CWE-22)
//     El atacante manipula la ruta del archivo con "../" para acceder a
//     archivos fuera del directorio permitido (ej: /etc/passwd en Linux).
//
//  2. SSRF — Server-Side Request Forgery (CWE-918)
//     El servidor hace requests HTTP a URLs controladas por el atacante.
//     Permite acceder a servicios internos: http://169.254.169.254 (metadata
//     de instancias cloud) o http://localhost:8080 (servicios internos).
//
//  3. XXE — XML External Entity Injection (CWE-611)
//     El parser XML carga entidades externas definidas en el XML del atacante,
//     lo que permite leer archivos del servidor o hacer SSRF.
//
//  4. DEPENDABOT: los paquetes Newtonsoft.Json 12.0.2 y log4net 2.0.10
//     tienen CVEs conocidas que Dependabot detectará y propondrá actualizar.
//
// ⚠️  NUNCA uses estos patrones en código de producción.
// ══════════════════════════════════════════════════════════════════════════════
using Newtonsoft.Json;
using log4net;
using System.Xml;

namespace UsersApi.Services;

/// <summary>
/// GHAS Demo: Path Traversal, SSRF, Log Injection, XML Injection
/// </summary>
public class ReportService
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(ReportService));

    private static readonly string _reportsBasePath =
        Path.Combine(Directory.GetCurrentDirectory(), "reports");

    private readonly IHttpClientFactory _httpClientFactory;

    public ReportService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // ── CODE SCANNING: Path Traversal ────────────────────────────────────────
    // ❌ PROBLEMA: Path.Combine no evita "../" — si fileName = "../../etc/passwd",
    //    la ruta resultante sale del directorio base permitido.
    //
    // ✅ SOLUCIÓN: validar que la ruta final esté dentro del directorio base:
    //    var fullPath = Path.GetFullPath(Path.Combine(_reportsBasePath, fileName));
    //    if (!fullPath.StartsWith(_reportsBasePath)) throw new UnauthorizedAccessException();
    /// <summary>
    /// VULNERABLE: el nombre de archivo viene del usuario y no se sanitiza.
    /// Un atacante puede usar "../../../etc/passwd".
    /// CodeQL (csharp/path-injection) lo detectará.
    /// </summary>
    public string GetReportContent(string fileName)
    {
        // ❌ Path Traversal: el input del usuario controla la ruta del archivo
        var filePath = Path.Combine(_reportsBasePath, fileName);
        return File.ReadAllText(filePath);
    }

    // ── CODE SCANNING: SSRF (Server-Side Request Forgery) ────────────────────
    // ❌ PROBLEMA: el atacante puede pasar url = "http://169.254.169.254/metadata"
    //    para robar las credenciales de la instancia en Azure/AWS/GCP,
    //    o url = "http://internal-service:8080" para escanear la red interna.
    //
    // ✅ SOLUCIÓN: implementar una allowlist de dominios permitidos:
    //    var allowed = new[] { "reports.mi-empresa.com" };
    //    var host = new Uri(url).Host;
    //    if (!allowed.Contains(host)) return Results.BadRequest("URL no permitida");
    /// <summary>
    /// VULNERABLE: la URL la provee el usuario sin validación.
    /// Permite acceder a servicios internos (metadata de cloud, etc.).
    /// CodeQL (csharp/ssrf) lo detectará.
    /// </summary>
    public async Task<string> FetchExternalReportAsync(string url)
    {
        // ❌ SSRF: URL controlada por el usuario, sin whitelist ni validación
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    // ── CODE SCANNING: Log Injection ─────────────────────────────────────────
    /// <summary>
    /// VULNERABLE: el input del usuario se escribe en el log sin sanitizar.
    /// Un atacante puede inyectar entradas de log falsas.
    /// </summary>
    public void LogUserAccess(string username)
    {
        // ❌ Log Injection: el username puede contener newlines y caracteres de control
        _log.Info("User accessed reports: " + username);
        Console.WriteLine("[AUDIT] Report accessed by: " + username);
    }

    // ── CODE SCANNING: XML Injection / XXE ───────────────────────────────────
    /// <summary>
    /// VULNERABLE: procesa XML externo sin deshabilitar entidades externas (XXE).
    /// CodeQL (csharp/xml-injection) lo detectará.
    /// </summary>
    public string ParseUserXmlData(string xmlInput)
    {
        // ❌ XXE: XmlDocument por defecto resuelve entidades externas
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlInput);

        var nameNode = xmlDoc.SelectSingleNode("//Name");
        return nameNode?.InnerText ?? "Unknown";
    }

    // ── CODE SCANNING: Deserialización insegura con Newtonsoft.Json ──────────
    /// <summary>
    /// VULNERABLE: TypeNameHandling.All permite deserialización de tipos arbitrarios.
    /// CVE en versiones &lt; 13.0.1 de Newtonsoft.Json.
    /// </summary>
    public object? DeserializeUserData(string json)
    {
        // ❌ TypeNameHandling.All es peligroso — permite RCE en algunos escenarios
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        return JsonConvert.DeserializeObject(json, settings);
    }

    // ── CODE SCANNING: generación de números aleatorios insegura ─────────────
    public string GenerateReportId()
    {
        // ❌ System.Random no es criptográficamente seguro para IDs
        var rng = new Random(42); // seed fija: predecible
        return rng.Next(100000, 999999).ToString();
    }
}
