// ══════════════════════════════════════════════════════════════════════════════
// CustomPatternDemoService.cs — GHAS Demo: Secret Scanning Custom Patterns
//
// ¿Qué es un Custom Pattern?
//   GitHub Secret Scanning incluye ~200 patrones integrados (GitHub PATs,
//   AWS keys, Stripe keys, etc.). Un Custom Pattern te permite definir TUS
//   PROPIOS patrones mediante expresiones regulares para detectar secretos
//   internos de tu empresa que GitHub no conoce.
//
// ¿Dónde se configura?
//   NO es un archivo del repositorio. Se configura en la UI de GitHub:
//   Settings → Security → Secret scanning → Custom patterns → New pattern
//   (disponible a nivel repo, organización o empresa)
//
// Formato del Custom Pattern en la UI:
//   ┌─────────────────────────────────────────────────────────┐
//   │ Name:         Internal API Key                          │
//   │ Secret format (regex):                                  │
//   │   MYCO-[A-Z]{3}-[0-9]{4}-[a-f0-9]{8}                  │
//   │                                                         │
//   │ Before secret (opcional, ancla de contexto):            │
//   │   (api[_-]?key|token|secret)\s*[:=]\s*["']?            │
//   │                                                         │
//   │ After secret (opcional):                                │
//   │   ["']?                                                 │
//   └─────────────────────────────────────────────────────────┘
//
// Este archivo contiene secretos con formato interno ficticio que serían
// detectados por los custom patterns de la demo.
// ══════════════════════════════════════════════════════════════════════════════

namespace UsersApi.Services;

/// <summary>
/// Demo de Custom Patterns: secretos con formato interno de empresa.
/// Estos NO los detecta GitHub por defecto → necesitan un Custom Pattern.
/// </summary>
public class CustomPatternDemoService
{
    // ── CUSTOM PATTERN 1: Internal API Key ───────────────────────────────────
    // Formato ficticio de empresa: MYCO-[ENV]-[NNNN]-[hex8]
    // Regex del custom pattern: MYCO-[A-Z]{3}-\d{4}-[a-f0-9]{8}
    //
    // ❌ Hardcodeado en código fuente — el custom pattern lo detectaría
    private const string InternalApiKey = "MYCO-PRD-1042-a3f9c21b";
    private const string StagingApiKey  = "MYCO-STG-0891-d7e14fa2";

    // ── CUSTOM PATTERN 2: Database Connection Token ───────────────────────────
    // Formato ficticio: DB-TOKEN-[YYYYMMDD]-[alphanum16]
    // Regex del custom pattern: DB-TOKEN-\d{8}-[A-Za-z0-9]{16}
    //
    // ❌ Token de base de datos hardcodeado
    private const string DbAccessToken = "DB-TOKEN-20260101-Xk92mNpQ7rLwVjT4";

    // ── CUSTOM PATTERN 3: Internal Service Account Key ────────────────────────
    // Formato ficticio: SVC-[service]-[env]-[base64-like 24 chars]
    // Regex: SVC-[a-z]+-[a-z]+-[A-Za-z0-9+/]{24}
    //
    // ❌ Service account key hardcodeada
    private const string ServiceAccountKey = "SVC-payments-prod-aB3cD4eF5gH6iJ7kL8mN9oP0";

    // ── CUSTOM PATTERN 4: Webhook Secret ─────────────────────────────────────
    // Formato ficticio: whsec_[alphanum40]
    // Regex: whsec_[A-Za-z0-9]{40}
    // (similar al formato real de Stripe webhooks — GitHub YA tiene uno integrado,
    //  pero es un buen ejemplo de cómo definirías el tuyo para webhooks internos)
    private const string WebhookSecret = "whsec_MyCompanyWebhookSecret1234567890AbCdEf";

    public string GetInternalApiKey(string environment) => environment switch
    {
        "production" => InternalApiKey,
        "staging"    => StagingApiKey,
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    public string GetDbToken() => DbAccessToken;
}
