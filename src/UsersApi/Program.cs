// ══════════════════════════════════════════════════════════════════════════════
// Program.cs — Punto de entrada de la aplicación (Minimal API .NET)
//
// GHAS Demo: este archivo registra los servicios y endpoints que contienen
// vulnerabilidades intencionales para demostrar las capacidades de:
//   • CodeQL (Code Scanning)  → detecta patrones inseguros en el código
//   • Secret Scanning         → detecta tokens/contraseñas hardcodeadas
//   • Dependabot              → detecta CVEs en los paquetes NuGet
// ══════════════════════════════════════════════════════════════════════════════
using Microsoft.EntityFrameworkCore;
using UsersApi.Data;
using UsersApi.Models;
using UsersApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos en memoria ──────────────────────────────────────────────────
// UseInMemoryDatabase registra un proveedor EF Core que vive únicamente en RAM.
// Es ideal para demos y pruebas: no requiere servidor SQL ni archivos.
// Los datos se pierden al reiniciar la aplicación.
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("UsersDb"));

// ── Servicios con vulnerabilidades (GHAS Demo) ────────────────────────────────
// AddHttpClient registra IHttpClientFactory, necesaria para el demo de SSRF.
// AddScoped crea una instancia nueva por cada request HTTP (ciclo de vida Scoped).
builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthService>();   // Demo: SQL Injection + Secrets
builder.Services.AddScoped<ReportService>(); // Demo: Path Traversal + SSRF + XXE

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
// Swagger genera documentación interactiva de la API accesible en /swagger.
// AddEndpointsApiExplorer descubre los endpoints de Minimal API automáticamente.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Users API", Version = "v1" });
});

var app = builder.Build();

// ── Seed: datos de prueba en la base de datos en memoria ──────────────────────
// EnsureCreated() aplica el modelo y ejecuta los datos semilla del OnModelCreating
// definidos en AppDbContext. Esto popula la BD con 5 usuarios de ejemplo.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
// El orden importa: Swagger → HTTPS → Endpoints
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users API v1"));

app.UseHttpsRedirection();

// ── Users endpoints ──────────────────────────────────────────────────────────
// MapGroup agrupa endpoints bajo un prefijo común y aplica metadatos (tags)
// que Swagger usa para organizar la documentación en secciones.
// Estos endpoints son el CRUD normal sin vulnerabilidades.

var users = app.MapGroup("/api/users").WithTags("Users");

users.MapGet("/", async (AppDbContext db) =>
    await db.Users.ToListAsync())
    .WithName("GetUsers")
    .WithSummary("Obtiene el catálogo completo de usuarios");

users.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Users.FindAsync(id) is User user
        ? Results.Ok(user)
        : Results.NotFound())
    .WithName("GetUserById")
    .WithSummary("Obtiene un usuario por su ID");

users.MapPost("/", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.Id}", user);
})
.WithName("CreateUser")
.WithSummary("Crea un nuevo usuario");

users.MapPut("/{id:int}", async (int id, User input, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    user.Name  = input.Name;
    user.Email = input.Email;
    user.Role  = input.Role;
    await db.SaveChangesAsync();
    return Results.Ok(user);
})
.WithName("UpdateUser")
.WithSummary("Actualiza un usuario existente");

users.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteUser")
.WithSummary("Elimina un usuario");

// ── Auth endpoints (GHAS Demo: SQL Injection + Hardcoded Secrets) ─────────────

var auth = app.MapGroup("/api/auth").WithTags("Auth - GHAS Demo");

auth.MapPost("/login", async (LoginRequest req, AuthService authSvc) =>
{
    // ❌ Llama a un método con SQL Injection
    var isValid = await authSvc.ValidateUserAsync(req.Username, req.Password);
    if (!isValid) return Results.Unauthorized();

    var token = authSvc.GenerateJwtToken(req.Username);
    return Results.Ok(new { token });
})
.WithName("Login")
.WithSummary("GHAS Demo: endpoint con SQL Injection y JWT con clave hardcodeada");

auth.MapGet("/search", async (string name, AuthService authSvc) =>
{
    // ❌ SQL Injection por parámetro de query
    var result = await authSvc.FindUserByNameAsync(name);
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("SearchUser")
.WithSummary("GHAS Demo: búsqueda vulnerable a SQL Injection");

// ── Report endpoints (GHAS Demo: Path Traversal + SSRF + XXE) ────────────────

var reports = app.MapGroup("/api/reports").WithTags("Reports - GHAS Demo");

reports.MapGet("/file", (string fileName, ReportService reportSvc) =>
{
    // ❌ Path Traversal
    var content = reportSvc.GetReportContent(fileName);
    return Results.Ok(new { content });
})
.WithName("GetReportFile")
.WithSummary("GHAS Demo: vulnerable a Path Traversal");

reports.MapGet("/fetch", async (string url, ReportService reportSvc) =>
{
    // ❌ SSRF
    var content = await reportSvc.FetchExternalReportAsync(url);
    return Results.Ok(new { content });
})
.WithName("FetchExternalReport")
.WithSummary("GHAS Demo: vulnerable a SSRF");

reports.MapPost("/parse-xml", (XmlRequest req, ReportService reportSvc) =>
{
    // ❌ XXE
    var name = reportSvc.ParseUserXmlData(req.XmlData);
    return Results.Ok(new { name });
})
.WithName("ParseXmlReport")
.WithSummary("GHAS Demo: vulnerable a XXE injection");

reports.MapPost("/deserialize", (JsonRequest req, ReportService reportSvc) =>
{
    // ❌ Deserialización insegura con Newtonsoft.Json TypeNameHandling.All
    var obj = reportSvc.DeserializeUserData(req.Json);
    return Results.Ok(obj);
})
.WithName("DeserializeData")
.WithSummary("GHAS Demo: deserialización insegura (Newtonsoft.Json CVE)");

app.Run();

// ── Request models ────────────────────────────────────────────────────────────
record LoginRequest(string Username, string Password);
record XmlRequest(string XmlData);
record JsonRequest(string Json);
