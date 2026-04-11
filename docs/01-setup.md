# Lab 01 — Setup del Entorno

Bienvenido al workshop de **GitHub Advanced Security**. Antes de entrar en las vulnerabilidades y las herramientas de detección, necesitamos que el entorno esté listo y que entiendas qué hace la aplicación demo que usaremos para todo el workshop.

Este lab es corto, en 15 minutos tendrás el proyecto corriendo localmente, habrás explorado la API y habrás habilitado GHAS en el repositorio.

## ¿Qué vas a hacer en este lab?

- Clonar el repositorio y ejecutar la aplicación localmente
- Explorar los endpoints con vulnerabilidades intencionales que detectarás en labs posteriores
- Revisar la estructura del proyecto y entender qué hace cada archivo
- Habilitar GitHub Advanced Security en el repositorio

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
git clone https://github.com/armandoblanco/workshop-github-advanced-security.git
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

Tómate un momento para entender cómo está organizado el repositorio. Esto te ayudará a navegar los labs con soltura:

```
workshop-github-advanced-security/
├── .github/
│   ├── dependabot.yml              # Lab 02 — Monitoreo de NuGet + GitHub Actions
│   ├── secret_scanning.yml         # Lab 03 — Paths excluidos de Secret Scanning
│   └── workflows/
│       ├── codeql.yml              # Lab 04 — Code Scanning (2 jobs)
│       └── dependency-review.yml   # Lab 05 — Dependency Review en PRs
├── docs/
│   ├── 01-setup.md                 # Este archivo
│   ├── 02-dependabot.md
│   ├── 03-secret-scanning.md
│   ├── 04-code-scanning.md
│   ├── 05-dependency-review.md
│   ├── 06-ghas-at-scale.md
│   ├── 07-ghas-azure-devops.md
│   ├── custom-patterns.md
│   └── examples/
│       └── release-notes.txt       # Ejemplo para demo de paths-ignore
├── src/
│   └── UsersApi/
│       ├── Data/
│       │   └── AppDbContext.cs     # Base de datos en memoria + datos seed
│       ├── Models/
│       │   └── User.cs             # Entidad User
│       ├── Services/
│       │   ├── AuthService.cs      # ❌ Demo: SQL Injection + Secrets
│       │   ├── ReportService.cs    # ❌ Demo: Path Traversal, SSRF, XXE, Deserialización
│       │   └── CustomPatternDemoService.cs  # ❌ Demo: Custom Patterns
│       ├── Program.cs              # Punto de entrada + registro de endpoints
│       ├── appsettings.json        # ❌ Demo: secretos hardcodeados en config
│       └── UsersApi.csproj         # ❌ Demo: dependencias con CVEs conocidas
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

¡Entorno listo! Ahora empieza la parte entretenida.

Los labs 02 al 05 son independientes entre sí, puedes seguirlos en cualquier orden, aunque los recomendamos en secuencia:

➡️ **Siguiente:** [Lab 02 — Dependabot: alertas de CVEs en dependencias](./02-dependabot.md)

---

📌 **También disponibles:**
- [Lab 03 — Secret Scanning y Push Protection](./03-secret-scanning.md)
- [Lab 04 — Code Scanning con CodeQL](./04-code-scanning.md)
- [Lab 05 — Dependency Review en PRs](./05-dependency-review.md)
- [Lab 06 — GHAS a escala (org/enterprise)](./06-ghas-at-scale.md)
- [Lab 07 — GHAS en Azure DevOps](./07-ghas-azure-devops.md)
