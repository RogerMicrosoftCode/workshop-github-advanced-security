# Lab 02 — Dependabot: Gestión de Dependencias Vulnerables

## Objetivos

- Entender cómo Dependabot detecta CVEs en dependencias NuGet
- Revisar las alertas generadas por los paquetes vulnerables del proyecto
- Interpretar la configuración de `dependabot.yml`
- Comprender el agrupamiento de PRs y la estrategia de versionado

---

## Contexto: ¿Qué es Dependabot?

Dependabot analiza el archivo de dependencias del proyecto (`.csproj`, `package.json`, `pom.xml`, etc.) y compara las versiones usadas contra la base de datos de advisories de GitHub (basada en CVE/NVD).

Genera dos tipos de alertas:

| Tipo | Qué hace | Dónde se ve |
|---|---|---|
| **Dependabot Alerts** | Notifica CVEs en dependencias actuales | Security → Dependabot alerts |
| **Dependabot Security Updates** | Abre PRs automáticos para parchear CVEs | Pull requests del repo |
| **Dependabot Version Updates** | Abre PRs para mantener dependencias actualizadas | Pull requests del repo |

---

## Paso 1 — Revisar las dependencias vulnerables del proyecto

Abre `src/UsersApi/UsersApi.csproj` y observa los paquetes instalados:

```xml
<PackageReference Include="Newtonsoft.Json"             Version="12.0.2" />
<PackageReference Include="log4net"                     Version="2.0.10" />
<PackageReference Include="Microsoft.Data.SqlClient"    Version="2.0.0"  />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="5.6.0" />
```

Estas versiones contienen CVEs conocidas:

| Paquete | Versión instalada | Severidad | CVE / Advisory |
|---|---|---|---|
| `Newtonsoft.Json` | 12.0.2 | **High** | [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr) — ReDoS |
| `System.Drawing.Common` | 4.7.0 (transitiva) | **Critical** | [GHSA-rxg9-xrhp-64gj](https://github.com/advisories/GHSA-rxg9-xrhp-64gj) |
| `Microsoft.Data.SqlClient` | 2.0.0 | **High** | [GHSA-98g6-xh36-x2p7](https://github.com/advisories/GHSA-98g6-xh36-x2p7) |
| `System.IdentityModel.Tokens.Jwt` | 5.6.0 | **Moderate** | [GHSA-59j7-ghrg-fj52](https://github.com/advisories/GHSA-59j7-ghrg-fj52) |

---

## Paso 2 — Ver las alertas en GitHub

1. Ve al repositorio en GitHub
2. Haz clic en la pestaña **Security**
3. En el menú lateral, selecciona **Dependabot alerts**

Verás una alerta por cada CVE detectada. Cada alerta muestra:
- Nombre del paquete y versión afectada
- Descripción de la vulnerabilidad
- Versión que parchea el problema
- Severidad (Critical / High / Moderate / Low)

---

## Paso 3 — Interpretar el archivo `dependabot.yml`

Abre `.github/dependabot.yml`:

```yaml
version: 2

updates:
  - package-ecosystem: "nuget"
    directory: "/src/UsersApi"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
      timezone: "America/Mexico_City"
    open-pull-requests-limit: 5
    assignees:
      - "armandoblanco"
    labels:
      - "dependencies"
      - "security"
      - ".net"
    versioning-strategy: "auto"
    groups:
      microsoft-packages:
        patterns:
          - "Microsoft.*"
          - "System.*"
      third-party-packages:
        patterns:
          - "Newtonsoft.*"
          - "log4net"
          - "Swashbuckle.*"
```

### Conceptos clave de la configuración

**`package-ecosystem: nuget`**
Indica que Dependabot debe analizar el archivo `.csproj` buscando paquetes NuGet.
Otros valores posibles: `npm`, `pip`, `maven`, `gradle`, `bundler`, `github-actions`.

**`directory: "/src/UsersApi"`**
Ruta donde se encuentra el archivo de dependencias (`.csproj`).
Si tienes múltiples proyectos en subdirectorios, agrega una entrada por cada uno.

**`schedule`**
Define cuándo Dependabot revisa si hay actualizaciones disponibles.
- `interval: weekly` + `day: monday` → revisa cada lunes
- Independiente de si hay CVEs nuevas o no

**`open-pull-requests-limit: 5`**
Dependabot no abrirá más de 5 PRs simultáneos para evitar saturar al equipo.
Una vez que se cierran o mergean PRs existentes, abre nuevos.

**`versioning-strategy: auto`**
Determina cómo Dependabot modifica el número de versión en el `.csproj`:
- `auto` → Dependabot elige la estrategia óptima
- `increase` → solo sube versiones, nunca las baja
- `lockfile-only` → solo actualiza el lock file sin tocar la versión declarada

**`groups`**
Agrupa múltiples actualizaciones en un solo PR.
Sin grupos: un PR por cada paquete → mucho ruido.
Con grupos: todos los paquetes de `Microsoft.*` en un solo PR → más manejable.

---

## Paso 4 — Ejercicio: simular una actualización

```bash
# Desde la raíz del repo
cd src/UsersApi

# Verifica la versión actual de Newtonsoft.Json
dotnet list package --vulnerable

# Actualiza a la versión que parchea el CVE
dotnet add package Newtonsoft.Json --version 13.0.3

# Verifica que ya no aparece como vulnerable
dotnet list package --vulnerable
```

**Resultado esperado:**
- Antes: `Newtonsoft.Json 12.0.2  → High severity advisory`
- Después: sin alertas para ese paquete

---

## Paso 5 — Habilitar Dependabot Security Updates

Con Security Updates habilitadas, Dependabot abrirá PRs automáticos cuando detecte una CVE, sin esperar el ciclo semanal de Version Updates.

1. Settings → Code security → Dependabot
2. Activa **Dependabot security updates**

Cuando se publique una nueva CVE para una dependencia del proyecto, Dependabot abrirá un PR con el título:
```
chore(deps): bump Newtonsoft.Json from 12.0.2 to 13.0.3
```

---

## Paso 6 — GitHub Actions como ecosistema adicional

El `dependabot.yml` también monitorea los workflows de GitHub Actions:

```yaml
- package-ecosystem: "github-actions"
  directory: "/"
  schedule:
    interval: "monthly"
```

Esto actualiza automáticamente versiones como `actions/checkout@v3 → @v4`.

---

## Resumen

| Concepto | Descripción |
|---|---|
| Dependabot Alerts | Notificaciones pasivas de CVEs en dependencias activas |
| Security Updates | PRs automáticos para parchear CVEs detectadas |
| Version Updates | PRs periódicos para mantener dependencias actualizadas |
| Groups | Reduce el ruido agrupando múltiples PRs en uno |
| Push Protection | No aplica a Dependabot (es para Secret Scanning) |

---

## Siguiente paso

➡️ [Lab 03 — Secret Scanning: detección de secretos expuestos](./03-secret-scanning.md)
