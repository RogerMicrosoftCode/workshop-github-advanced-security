# Workshop: GitHub Advanced Security (GHAS)

> **⚠️ ADVERTENCIA:** Este repositorio contiene código **intencionalmente vulnerable** con fines educativos. No usar en producción. Ver [SECURITY.md](./SECURITY.md) para más detalles.

---

## ¿Qué es este workshop?

Una demo práctica de las funcionalidades de **GitHub Advanced Security** usando una API .NET 10 con vulnerabilidades reales y configuraciones paso a paso.

Al completar el workshop entenderás cómo configurar y demostrar:

| Feature | ¿Qué detecta? | Lab |
|---|---|---|
| **Dependabot** | CVEs en dependencias existentes | [Lab 02](./docs/02-dependabot.md) |
| **Secret Scanning** | Secretos y tokens expuestos en el código | [Lab 03](./docs/03-secret-scanning.md) |
| **Push Protection** | Bloquea commits con secretos antes del push | [Lab 03](./docs/03-secret-scanning.md) |
| **Code Scanning (CodeQL)** | Vulnerabilidades en el flujo de datos del código | [Lab 04](./docs/04-code-scanning.md) |
| **Dependency Review** | CVEs en dependencias nuevas en cada PR | [Lab 05](./docs/05-dependency-review.md) |
| **Custom Patterns** | Secretos de formato interno no conocidos por GitHub | [Guía](./docs/custom-patterns.md) |

---

## Arquitectura del proyecto

```
ghas/
├── src/
│   └── UsersApi/               # .NET 10 Minimal API
│       ├── Program.cs          # Entry point — endpoints y DI
│       ├── Models/
│       │   └── User.cs         # Entidad User
│       ├── Data/
│       │   └── AppDbContext.cs # EF Core InMemory + seed data
│       ├── Services/
│       │   ├── AuthService.cs          # ❌ SQL Injection, hardcoded secrets
│       │   ├── ReportService.cs        # ❌ Path Traversal, SSRF, XXE
│       │   └── CustomPatternDemoService.cs  # ❌ Secretos formato interno
│       ├── appsettings.json    # ❌ API keys hardcodeadas
│       └── UsersApi.csproj     # ❌ Paquetes NuGet vulnerables
├── .github/
│   ├── dependabot.yml          # Monitoreo de NuGet y GitHub Actions
│   └── workflows/
│       ├── codeql.yml          # Code Scanning (2 jobs)
│       └── dependency-review.yml  # Dependency Review en PRs
├── docs/
│   ├── 01-setup.md             # Prerequisitos y configuración
│   ├── 02-dependabot.md        # Lab: Dependabot
│   ├── 03-secret-scanning.md   # Lab: Secret Scanning + Push Protection
│   ├── 04-code-scanning.md     # Lab: CodeQL
│   ├── 05-dependency-review.md # Lab: Dependency Review
│   └── custom-patterns.md      # Guía: Custom Patterns
├── SECURITY.md                 # Política de seguridad
└── README.md                   # Este archivo
```

---

## Vulnerabilidades incluidas

### Paquetes NuGet vulnerables

| Paquete | Versión | Severidad | CVE |
|---|---|---|---|
| `Newtonsoft.Json` | 12.0.2 | High | GHSA-5crp-9r3c-p9vr |
| `Microsoft.Data.SqlClient` | 2.0.0 | High | GHSA-98g6-xh36-x2p7 |
| `System.IdentityModel.Tokens.Jwt` | 5.6.0 | Moderate | GHSA-59j7-ghrg-fj52 |
| `log4net` | 2.0.10 | High | GHSA-rxg9-xrhp-64gj |

### Vulnerabilidades en código (CodeQL)

| Tipo | CWE | Archivo | Endpoint |
|---|---|---|---|
| SQL Injection | CWE-89 | `AuthService.cs` | `POST /api/auth/login` |
| SQL Injection | CWE-89 | `AuthService.cs` | `GET /api/auth/search` |
| Path Traversal | CWE-22 | `ReportService.cs` | `GET /api/reports/file` |
| SSRF | CWE-918 | `ReportService.cs` | `GET /api/reports/fetch` |
| XXE | CWE-611 | `ReportService.cs` | `POST /api/reports/parse-xml` |
| Deserialización insegura | CWE-502 | `ReportService.cs` | `POST /api/reports/deserialize` |

### Secretos expuestos (Secret Scanning)

| Tipo de secreto | Ubicación |
|---|---|
| GitHub PAT (`ghp_`) | `AuthService.cs` |
| AWS Access Key (`AKIA`) | `AuthService.cs` |
| Stripe Live Key (`pk_live_`) | `appsettings.json` |
| SendGrid API Key (`SG.`) | `appsettings.json` |
| Azure Storage Connection String | `appsettings.json` |
| JWT hardcoded secret | `AuthService.cs`, `appsettings.json` |

### Secretos de formato interno (Custom Patterns)

| Patrón | Ejemplo | Regex |
|---|---|---|
| API Key corporativa | `MYCO-PRD-1042-a3f9c21b` | `MYCO-[A-Z]{3}-[0-9]{4}-[a-f0-9]{8}` |
| DB Token temporal | `DB-TOKEN-20260101-Xk92mNpQ7rLwVjT4` | `DB-TOKEN-[0-9]{8}-[A-Za-z0-9]{16}` |
| Service account key | `SVC-payments-prod-aB3cD4...` | `SVC-[a-z]+-(?:prod\|staging)-[A-Za-z0-9]{24}` |
| Webhook secret | `whsec_MyCompanyWebhook...` | `whsec_[A-Za-z0-9]{32,64}` |

---

## Prerequisitos

| Requisito | Versión mínima |
|---|---|
| .NET SDK | 10.x |
| Git | 2.x |
| Cuenta GitHub | Con acceso al repo |
| GHAS habilitado | En la organización o repo |

---

## Labs del Workshop

| # | Lab | Descripción | Tiempo estimado |
|---|---|---|---|
| 01 | [Setup](./docs/01-setup.md) | Configurar GHAS en el repositorio | 15 min |
| 02 | [Dependabot](./docs/02-dependabot.md) | Alertas de CVEs en dependencias | 20 min |
| 03 | [Secret Scanning](./docs/03-secret-scanning.md) | Detectar y bloquear secretos expuestos | 25 min |
| 04 | [Code Scanning](./docs/04-code-scanning.md) | Análisis de vulnerabilidades con CodeQL | 30 min |
| 05 | [Dependency Review](./docs/05-dependency-review.md) | Bloquear CVEs en Pull Requests | 20 min |
| — | [Custom Patterns](./docs/custom-patterns.md) | Patrones regex para secretos internos | 20 min |

**Tiempo total estimado:** ~2 horas

---

## Inicio rápido

### Clonar el repositorio

```bash
git clone https://github.com/armblaorg/workshop-github-advanced-security.git
cd workshop-github-advanced-security
```

### Ejecutar la API localmente

```bash
cd src/UsersApi
dotnet restore
dotnet run
```

La API estará disponible en:
- **Swagger UI:** `http://localhost:5000/swagger`
- **API base:** `http://localhost:5000/api`

### Endpoints disponibles

```
GET    /api/users              # Listar todos los usuarios
GET    /api/users/{id}         # Obtener usuario por ID
POST   /api/users              # Crear usuario
PUT    /api/users/{id}         # Actualizar usuario
DELETE /api/users/{id}         # Eliminar usuario

POST   /api/auth/login         # Login (SQL Injection demo)
GET    /api/auth/search        # Buscar por nombre (SQL Injection demo)

GET    /api/reports/file       # Leer archivo (Path Traversal demo)
GET    /api/reports/fetch      # Fetch URL externa (SSRF demo)
POST   /api/reports/parse-xml  # Parsear XML (XXE demo)
POST   /api/reports/deserialize# Deserializar JSON (Insecure Deserialization demo)
```

---

## Estado de GHAS en el repositorio

| Feature | Estado | Configuración |
|---|---|---|
| Dependabot alerts | Activo | `.github/dependabot.yml` |
| Dependabot security updates | Activo | Automático |
| Secret Scanning | Activo | Habilitado en Settings |
| Push Protection | Activo | Habilitado en Settings |
| Code Scanning | Activo | `.github/workflows/codeql.yml` |
| Dependency Review | Activo | `.github/workflows/dependency-review.yml` |
| Custom Patterns | Manual | Ver `docs/custom-patterns.md` |

---

## Recursos

- [GitHub Advanced Security Docs](https://docs.github.com/en/code-security)
- [CodeQL Query Suite Reference](https://docs.github.com/en/code-security/code-scanning/managing-your-code-scanning-configuration/codeql-query-suites)
- [Dependabot Configuration Reference](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file)
- [Secret Scanning Pattern Reference](https://docs.github.com/en/code-security/secret-scanning/introduction/supported-secret-scanning-patterns)
- [Dependency Review Action](https://github.com/actions/dependency-review-action)
- [Hyperscan Regex Syntax](https://intel.github.io/hyperscan/dev-reference/compilation.html#pattern-support)

---

## Licencia

MIT — ver [LICENSE](./LICENSE) para detalles.

Este proyecto es solo para fines educativos. No usar en producción.
