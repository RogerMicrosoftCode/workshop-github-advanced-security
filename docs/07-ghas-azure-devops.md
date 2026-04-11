# Lab 07 — GHAS en Azure DevOps (GHAzDO)

## Objetivos

- Entender qué es GitHub Advanced Security for Azure DevOps (GHAzDO) y en qué se diferencia de GHAS en GitHub.com
- Habilitar GHAS a nivel de repositorio, proyecto y organización en Azure DevOps
- Configurar Secret Scanning, Dependency Scanning y Code Scanning con pipelines YAML
- Configurar PR annotations y status checks para bloquear merges con vulnerabilidades
- Entender el modelo de permisos y de billing por active committers

---

## ¿Qué es GitHub Advanced Security for Azure DevOps?

**GitHub Advanced Security for Azure DevOps (GHAzDO)** lleva las capacidades de seguridad de GHAS a repositorios Git en **Azure Repos**. Está disponible como dos productos independientes:

| Producto | Features incluidas |
|---|---|
| **GitHub Secret Protection for Azure DevOps** | Push protection · Secret scanning alerts · Security overview |
| **GitHub Code Security for Azure DevOps** | Dependency scanning alerts · CodeQL code scanning · Third-party SARIF tools · Security overview |

> **📌 Diferencia clave:** GHAzDO funciona **únicamente con Azure Repos** (repositorios Git en Azure DevOps). Para repositorios en GitHub.com, se usa GHAS de GitHub directamente.
>
> Fuente: [Configure GitHub Advanced Security for Azure DevOps — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/configure-github-advanced-security-features)

### GHAzDO vs GHAS en GitHub.com

| Aspecto | GHAS en GitHub.com | GHAzDO |
|---|---|---|
| **Repositorios** | GitHub | Azure Repos (Git) |
| **Code Scanning config** | Workflow YAML en `.github/workflows/` | Pipeline YAML en Azure Pipelines |
| **Dependency Review** | Action en PR (`pull_request` trigger) | Tarea de pipeline + PR annotations |
| **Secret Scanning** | Automático al habilitar | Automático al habilitar Secret Protection |
| **Billing** | Por licencia GHAS (usuario) | Por active committer/mes por producto |
| **Security Configurations** | Org Settings → Advanced Security → Configurations | Project/Org Settings → Repos |
| **Push Protection** | Bloquea el `git push` | Bloquea el `git push` |

---

## Billing: Active Committers

GHAzDO se factura por **active committers** — committers únicos con al menos un push en los últimos 90 días en repositorios con Secret Protection o Code Security habilitados.

**Puntos clave del modelo de billing:**
- Se cobra mensualmente en la suscripción Azure asociada a la organización de Azure DevOps
- Los committers se **deduplican** entre repositorios y organizaciones dentro de la misma suscripción Azure
- Un usuario que hace push a 5 repos cuenta como **1 active committer** (no 5)
- Antes de activar, Azure DevOps muestra una **estimación** del número de committers que se facturarán

> Fuente: [Advanced Security billing — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/github-advanced-security-billing)

---

## Parte 1 — Habilitar GHAS en Azure DevOps

GHAzDO se puede habilitar en tres niveles. Cada nivel más amplio incluye los repos de los niveles inferiores.

### Nivel 1 — Repositorio individual

```
Azure DevOps UI:
Project Settings → Repos → Repositories → [selecciona el repo]
→ Activa "Secret Protection" y/o "Code Security"
→ Click "Begin billing"
```

Pasos:
1. Ve a **Project Settings** de tu proyecto Azure DevOps
2. Selecciona **Repos → Repositories**
3. Selecciona el repositorio destino
4. Activa **Secret Protection** y/o **Code Security** con el toggle
5. Click **Begin billing** — aparecerá un icono de escudo en la vista del repo
6. Opcional: en **Options**, habilita **Dependency scanning default setup** para escaneo automático en la rama por defecto

### Nivel 2 — Proyecto completo

```
Azure DevOps UI:
Project Settings → Repos → Settings tab
→ Click "Enable all"
→ Activa los productos deseados
→ Click "Begin billing"
```

Pasos:
1. Ve a **Project Settings → Repos**
2. Selecciona el tab **Settings**
3. Click **Enable all** — verás la estimación de active committers
4. Activa **Secret Protection** y/o **Code Security** y sus sub-features
5. Click **Begin billing** — activa ambos productos en todos los repos existentes del proyecto
6. Opcional: activa **"Automatically enable Advanced Security for new repositories"** para nuevos repos del proyecto

### Nivel 3 — Organización completa (todos los proyectos)

```
Azure DevOps UI:
Organization Settings → Repositories
→ Click "Enable all"
→ Activa los productos deseados
→ Click "Begin billing"
```

Pasos:
1. Ve a **Organization Settings → Repositories**
2. Click **Enable all** — estimación de committers de toda la organización
3. Activa **Secret Protection** y/o **Code Security**
4. Click **Begin billing** — activa en todos los repos de todos los proyectos
5. Opcional: activa **"Automatically enable Advanced Security for new projects"**

> ⚠️ Solo usuarios con permiso **"Advanced Security: manage settings"** pueden habilitar GHAS (es una acción facturable).

---

## Parte 2 — Secret Scanning

**Secret scanning** y **push protection** se activan automáticamente al habilitar **Secret Protection** para un repositorio.

- Secret scanning realiza un escaneo de todo el historial Git del repositorio al activarse
- Push protection bloquea pushes que contengan secretos conocidos antes de que lleguen al servidor

Para gestionar push protection por repositorio:
```
Project Settings → Repos → Repositories → [repo] → Secret Protection options
```

> El comportamiento es equivalente al de GHAS en GitHub.com — mismos patrones de detección (~200 tipos de tokens/secretos conocidos por GitHub).

---

## Parte 3 — Dependency Scanning

Dependency scanning en GHAzDO es una **tarea de pipeline** — no es automático por defecto como en GitHub.com. Los resultados se agregan por repositorio.

### Opción A — Default setup (rama por defecto únicamente)

```
Project Settings → Repos → Repositories → [repo] → Options
→ Enable "Dependency scanning default setup"
```

Esto añade automáticamente la tarea de dependency scanning a cualquier pipeline que apunte a la rama por defecto.

### Opción B — Pipeline YAML (recomendado para todas las ramas)

```yaml
# azure-pipelines.yml
trigger:
  - main
  - develop

pool:
  vmImage: ubuntu-latest

steps:
  - task: AdvancedSecurity-Dependency-Scanning@1
```

La tarea `AdvancedSecurity-Dependency-Scanning@1` analiza los manifiestos de dependencias y publica los resultados en la pestaña **Security** del repositorio.

---

## Parte 4 — Code Scanning con CodeQL

Code scanning también es una **tarea de pipeline**. Requiere tres tareas en orden:

```
1. AdvancedSecurity-Codeql-Init@1      ← inicializa CodeQL, especifica el lenguaje
2. [tus pasos de build]                ← build del proyecto
3. AdvancedSecurity-Codeql-Analyze@1  ← analiza y publica resultados
```

> **💡 Recomendación de Microsoft:** añade las tareas de code scanning a un pipeline separado (clon del pipeline principal), ya que CodeQL puede ser tiempo-intensivo.

### Pipeline YAML completo para C# (.NET)

```yaml
# azure-pipelines-codeql.yml
trigger:
  - main

pool:
  vmImage: ubuntu-latest

steps:
  - task: AdvancedSecurity-Codeql-Init@1
    inputs:
      languages: "csharp"
      # Lenguajes soportados: csharp, cpp, go, java, javascript, python, ruby, swift
      # java analiza Java y Kotlin; javascript analiza JS y TypeScript
      enableAutomaticCodeQLInstall: true   # instala CodeQL en self-hosted agents

  # Build del proyecto — CodeQL monitorea el proceso de compilación
  - task: DotNetCoreCLI@2
    inputs:
      command: "build"
      projects: "**/*.csproj"
      arguments: "--configuration Release /p:UseSharedCompilation=false"
      # /p:UseSharedCompilation=false es igual que en GitHub Actions —
      # desactiva el servidor Roslyn compartido para que CodeQL capture todos los procesos

  - task: AdvancedSecurity-Dependency-Scanning@1  # opcional: incluir dep scanning en el mismo pipeline

  - task: AdvancedSecurity-Codeql-Analyze@1
    # Esta tarea sube los resultados a la pestaña Security del repositorio
```

### Notas importantes

- Para `swift`, los custom build steps son **obligatorios** (no hay autobuild)
- `java` analiza código Java **y** Kotlin
- `javascript` analiza JavaScript **y** TypeScript
- En self-hosted agents: usa `enableAutomaticCodeQLInstall: true` o instala el CodeQL bundle manualmente en el agent tool cache

---

## Parte 5 — PR Annotations y Status Checks

### PR Annotations

Al añadir las tareas de dependency scanning y/o code scanning a un pipeline con **build validation policy**, Azure DevOps añade automáticamente **anotaciones al PR** con los resultados.

**Requisitos:**
- Build validation policy configurada en la rama destino
- Las tareas de Advanced Security deben existir en el pipeline de la policy
- El repositorio debe tener al menos un escaneo previo en la rama base y en la rama destino

### Status Checks — bloquear merges con vulnerabilidades

Los status checks permiten **bloquear PRs** cuando se detectan vulnerabilidades de alta/crítica severidad. Hay dos opciones:

| Status Check | Comportamiento |
|---|---|
| `AdvancedSecurity/AllHighAndCritical` | Bloquea si existe **cualquier** alerta de severidad High o Critical en el repo |
| `AdvancedSecurity/NewHighAndCritical` | Bloquea solo si el PR **introduce nuevas** alertas High o Critical |

**Configurar como Branch Policy:**

1. **Project Settings → Repos → Policies** → selecciona la rama a proteger (ej. `main`)
2. Añade una **Build validation policy** con el pipeline que incluye las tareas de Advanced Security
3. Bajo **Status checks** → click **+** para añadir un nuevo status check
4. En **Status to check**:
   - Genre: `AdvancedSecurity`
   - Name: `AllHighAndCritical` o `NewHighAndCritical`
5. Selecciona **Required** como policy requirement
6. Click **Save**

> ⚠️ Los status checks solo aparecen en el menú después del **primer pipeline run exitoso** con las tareas de Advanced Security. No cambies el "authorized identity" ni el "iteration ID" en Advanced Options — esto impide que los status checks se publiquen.

---

## Parte 6 — Permisos

GHAzDO introduce tres permisos especializados:

| Permiso | Capacidad | Asignado por defecto a |
|---|---|---|
| **Advanced Security: Read alerts** | Ver alertas y resultados de escaneo | Contributors |
| **Advanced Security: Manage and dismiss alerts** | Cerrar falsos positivos, gestionar el ciclo de vida de alertas | Project Administrators |
| **Advanced Security: Manage settings** | Habilitar/deshabilitar Advanced Security (acción facturable) | Project Collection Administrators |

**Configurar permisos por repositorio:**
```
Project Settings → Repositories → [repo] → Security
→ Selecciona el grupo → modifica los permisos de Advanced Security
```

**Autenticación para las APIs de Advanced Security:**
- **Recomendado:** Microsoft Entra ID tokens (OAuth 2.0, cumple con políticas de acceso condicional y MFA)
- **Alternativa:** Personal Access Tokens (PAT) con scope `Advanced Security: read`, `read and write`, o `read, write, and manage`

---

## Resumen — Comparación de configuración

| Feature | GHAS en GitHub.com | GHAzDO |
|---|---|---|
| **Habilitar** | Repo/Org Settings → Security configurations | Project/Org Settings → Repos |
| **Secret Scanning** | Automático al habilitar GHAS | Automático al habilitar Secret Protection |
| **Push Protection** | Automático al habilitar / configurable | Automático al habilitar Secret Protection |
| **Dependency Scanning** | GitHub Actions `pull_request` trigger | Tarea `AdvancedSecurity-Dependency-Scanning@1` |
| **Code Scanning** | `codeql.yml` workflow | Pipeline con `AdvancedSecurity-Codeql-Init@1` + `Analyze@1` |
| **Bloquear PRs** | Branch protection rules + required status checks | Build validation policy + status checks `AdvancedSecurity/*` |
| **Escala (org)** | Security Configurations aplicadas a múltiples repos | Organization Settings → Repositories → Enable all |
| **Billing** | GHAS license por usuario | Active committer/mes por producto (Secret Protection o Code Security) |

---

## Referencias

- [Configure GitHub Advanced Security for Azure DevOps — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/configure-github-advanced-security-features)
- [Advanced Security billing — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/github-advanced-security-billing)
- [Manage Advanced Security permissions — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/github-advanced-security-permissions)
- [Code scanning alerts for GHAzDO — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/github-advanced-security-code-scanning)
- [Dependency scanning alerts for GHAzDO — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/github-advanced-security-dependency-scanning)
- [Secret scanning alerts for GHAzDO — Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/repos/security/github-advanced-security-secret-scanning)

---

## Siguiente paso

Vuelve al índice del workshop:

[README.md — Workshop: GitHub Advanced Security](../README.md)
