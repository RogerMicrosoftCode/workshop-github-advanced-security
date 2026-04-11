# Lab 01 — Setup del Entorno

## Objetivos

- Clonar el repositorio y ejecutar la aplicación localmente
- Verificar los prerequisitos necesarios para el workshop
- Entender la estructura del proyecto

---

## Prerequisitos

| Herramienta | Versión mínima | Verificar con |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | `dotnet --version` |
| [Git](https://git-scm.com/) | 2.x | `git --version` |
| [GitHub CLI](https://cli.github.com/) | 2.x | `gh --version` |
| Cuenta GitHub | con GHAS habilitado | — |

---

## Paso 1 — Clonar el repositorio

```bash
git clone https://github.com/armblaorg/workshop-github-advanced-security.git
cd workshop-github-advanced-security
```

---

## Paso 2 — Ejecutar la aplicación

```bash
cd src/UsersApi
dotnet restore
dotnet run
```

Abre el navegador en `http://localhost:5000/swagger` para ver la API con Swagger UI.

---

## Paso 3 — Explorar los endpoints disponibles

Una vez en Swagger, verás tres grupos de endpoints:

### Grupo `Users` — CRUD estándar (sin vulnerabilidades)

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/users` | Lista los 5 usuarios seed |
| `GET` | `/api/users/{id}` | Obtiene un usuario por ID |
| `POST` | `/api/users` | Crea un nuevo usuario |
| `PUT` | `/api/users/{id}` | Actualiza un usuario |
| `DELETE` | `/api/users/{id}` | Elimina un usuario |

### Grupo `Auth - GHAS Demo` — endpoints con vulnerabilidades intencionales

| Método | Ruta | Vulnerabilidad |
|---|---|---|
| `POST` | `/api/auth/login` | SQL Injection + JWT con clave hardcodeada |
| `GET` | `/api/auth/search?name=` | SQL Injection por query param |

### Grupo `Reports - GHAS Demo` — endpoints con vulnerabilidades intencionales

| Método | Ruta | Vulnerabilidad |
|---|---|---|
| `GET` | `/api/reports/file?fileName=` | Path Traversal |
| `GET` | `/api/reports/fetch?url=` | SSRF |
| `POST` | `/api/reports/parse-xml` | XXE Injection |
| `POST` | `/api/reports/deserialize` | Deserialización insegura |

> ⚠️ Estos endpoints contienen vulnerabilidades **intencionales** para la demo. No los expongas en ningún entorno real.

---

## Paso 4 — Revisar la estructura del proyecto

```
workshop-github-advanced-security/
├── .github/
│   ├── dependabot.yml              # Lab 02 — Dependabot
│   └── workflows/
│       ├── codeql.yml              # Lab 04 — Code Scanning
│       └── dependency-review.yml   # Lab 05 — Dependency Review
├── docs/
│   ├── 01-setup.md                 # Este archivo
│   ├── 02-dependabot.md
│   ├── 03-secret-scanning.md
│   ├── 04-code-scanning.md
│   ├── 05-dependency-review.md
│   └── custom-patterns.md
├── src/
│   └── UsersApi/
│       ├── Data/
│       │   └── AppDbContext.cs     # Base de datos en memoria + datos seed
│       ├── Models/
│       │   └── User.cs             # Entidad User
│       ├── Services/
│       │   ├── AuthService.cs      # Demo: SQL Injection + Secrets
│       │   ├── ReportService.cs    # Demo: Path Traversal, SSRF, XXE
│       │   └── CustomPatternDemoService.cs  # Demo: Custom Patterns
│       ├── Program.cs              # Punto de entrada + registro de endpoints
│       ├── appsettings.json        # Demo: secretos hardcodeados en config
│       └── UsersApi.csproj         # Demo: dependencias con CVEs conocidas
├── SECURITY.md                     # Política de seguridad del repositorio
└── README.md                       # Índice del workshop
```

---

## Paso 5 — Habilitar GitHub Advanced Security en el repositorio

1. Ve a tu repositorio en GitHub
2. Settings → Security → habilita las siguientes features:

| Feature | Sección en Settings |
|---|---|
| Dependabot alerts | Code security → Dependabot |
| Dependabot security updates | Code security → Dependabot |
| Code scanning | Code security → Code scanning → Set up CodeQL |
| Secret scanning | Code security → Secret scanning |
| Push protection | Code security → Secret scanning → Push protection |

---

## Siguiente paso

➡️ [Lab 02 — Dependabot: gestión de dependencias vulnerables](./02-dependabot.md)

🏢 ¿Quieres habilitar GHAS en múltiples repositorios a la vez? ➡️ [Lab 06 — GHAS a escala](./06-ghas-at-scale.md)

🔵 ¿Usas Azure DevOps en lugar de GitHub.com? ➡️ [Lab 07 — GHAS en Azure DevOps](./07-ghas-azure-devops.md)
