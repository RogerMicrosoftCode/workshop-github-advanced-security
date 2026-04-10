// ══════════════════════════════════════════════════════════════════════════════
// AuthService.cs — GHAS Demo: Code Scanning + Secret Scanning
//
// Este archivo contiene vulnerabilidades INTENCIONALES para demostrar:
//
//  1. SECRET SCANNING
//     GitHub escanea el repositorio buscando patrones de tokens y contraseñas
//     conocidos (GitHub PATs, AWS keys, Stripe keys, etc.).
//     Cuando encuentra uno, crea una alerta en Security > Secret Scanning.
//
//  2. CODE SCANNING (CodeQL)
//     CodeQL analiza el flujo de datos (taint analysis) desde el input del
//     usuario hasta operaciones sensibles. Detecta que un valor controlado
//     por el atacante llega a una query SQL sin sanitizar → SQL Injection.
//
// ⚠️  NUNCA uses estos patrones en código de producción.
// ══════════════════════════════════════════════════════════════════════════════
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UsersApi.Services;

/// <summary>
/// GHAS Demo: múltiples vulnerabilidades para CodeQL y Secret Scanning
/// </summary>
public class AuthService
{
    // ── SECRET SCANNING: credenciales hardcodeadas ────────────────────────────
    // ❌ PROBLEMA: estas constantes contienen secretos reales en el código fuente.
    //    Cuando el repo se sube a GitHub, Secret Scanning los detecta porque:
    //      • "ghp_" es el prefijo oficial de GitHub Personal Access Tokens
    //      • "AKIA" es el prefijo de AWS Access Key IDs
    //    GitHub revoca automáticamente algunos tokens detectados (push protection).
    //
    // ✅ SOLUCIÓN: usar variables de entorno, Azure Key Vault, o GitHub Secrets.
    //    En producción: Environment.GetEnvironmentVariable("JWT_SECRET")
    private const string AdminPassword = "SuperSecret@Admin1234!";
    private const string JwtSecretKey  = "MyHardcoded$ecretKey_NeverChangeThis_12345678";

    // GitHub PAT hardcodeado — Secret Scanning lo detectará por el prefijo "ghp_"
    private const string GitHubToken = "ghp_ReallySecretTokenThatShouldNotBeHere123456";

    // AWS Access Key hardcodeada — detectada por el prefijo "AKIA"
    private const string AwsAccessKey    = "AKIAIOSFODNN7EXAMPLEKEY";
    private const string AwsSecretKey    = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

    private readonly string _connectionString;

    public AuthService(IConfiguration configuration)
    {
        // También hay credentials en la connection string de config
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=prod-sql.internal;Database=UsersDb;User Id=sa;Password=Prod@ssw0rd!;";
    }

    // ── CODE SCANNING: SQL Injection ─────────────────────────────────────────
    // ❌ PROBLEMA: CodeQL rastrea que "username" y "password" vienen del request
    //    HTTP (source) y llegan a SqlCommand sin parametrizar (sink).
    //    Un atacante puede enviar: username = "' OR '1'='1" para saltarse la auth.
    //
    // ✅ SOLUCIÓN: usar parámetros SQL:
    //    command.Parameters.AddWithValue("@Name", username);
    //    command.Parameters.AddWithValue("@Pass", password);
    //    var query = "SELECT COUNT(*) FROM Users WHERE Name=@Name AND Password=@Pass";
    /// <summary>
    /// VULNERABLE: construye la query concatenando el input del usuario directamente.
    /// CodeQL (csharp/sql-injection) lo detectará.
    /// </summary>
    public async Task<bool> ValidateUserAsync(string username, string password)
    {
        // ❌ SQL Injection: el input se concatena sin parametrizar
        var query = "SELECT COUNT(*) FROM Users WHERE Name = '" + username
                  + "' AND Password = '" + password + "'";

        await using var connection = new SqlConnection(_connectionString);
        await using var command    = new SqlCommand(query, connection);

        await connection.OpenAsync();
        var count = (int)await command.ExecuteScalarAsync()!;
        return count > 0;
    }

    // ── CODE SCANNING: SQL Injection (variante con interpolación) ────────────
    public async Task<object?> FindUserByNameAsync(string name)
    {
        // ❌ SQL Injection con string interpolation — también detectado por CodeQL
        var query = $"SELECT * FROM Users WHERE Name = '{name}'";

        await using var connection = new SqlConnection(_connectionString);
        await using var command    = new SqlCommand(query, connection);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
            return new { Id = reader[0], Name = reader[1] };

        return null;
    }

    // ── CODE SCANNING: generación de token con clave débil hardcodeada ───────
    public string GenerateJwtToken(string username)
    {
        // ❌ Clave secreta hardcodeada en el código fuente
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:   "UsersApi",
            audience: "UsersApi",
            claims:   new[] { new Claim(ClaimTypes.Name, username) },
            expires:  DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── CODE SCANNING: comparación insegura de contraseñas ───────────────────
    public bool IsAdmin(string inputPassword)
    {
        // ❌ Comparación directa sin hashing (timing attack susceptible)
        return inputPassword == AdminPassword;
    }
}
