# Lab 03 — Secret Scanning: Detección de Secretos Expuestos

## Objetivos

- Identificar los tipos de secretos que GitHub detecta por defecto
- Revisar las alertas de Secret Scanning generadas por el proyecto
- Entender cómo funciona Push Protection para bloquear commits con secretos
- Configurar Custom Patterns para detectar secretos internos de la empresa

---

## Contexto: ¿Qué es Secret Scanning?

GitHub Secret Scanning analiza cada commit buscando patrones que coincidan con tokens y credenciales conocidos. Cuando encuentra uno:

1. Crea una alerta en **Security → Secret scanning alerts**
2. (Opcional) Notifica al proveedor del token para que lo revoque automáticamente
3. (Con Push Protection) Bloquea el commit antes de que llegue al repositorio

---

## Paso 1 — Secretos integrados detectables en el proyecto

Abre `src/UsersApi/Services/AuthService.cs` y observa las constantes:

```csharp
// GitHub Personal Access Token — prefijo "ghp_" reconocido por GitHub
private const string GitHubToken = "ghp_ReallySecretTokenThatShouldNotBeHere123456";

// AWS Access Key — prefijo "AKIA" reconocido por GitHub
private const string AwsAccessKey = "AKIAIOSFODNN7EXAMPLEKEY";
private const string AwsSecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
```

Abre `src/UsersApi/appsettings.json` y observa:

```json
{
  "ExternalServices": {
    "PaymentApiKey": "pk_live_51MzExampleStripeKeyThatShouldBeInVault",
    "SendGridApiKey": "SG.ExampleSendGridKey.AAAAAABBBBBBCCCCCCDDDDDDEEEEEE"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db.internal;...Password=Prod@ssw0rd2024!..."
  }
}
```

GitHub reconoce estos patrones porque tiene acuerdos con los proveedores (GitHub, AWS, Stripe, SendGrid, etc.) y conoce el formato exacto de sus tokens.

---

## Paso 2 — Ver las alertas en GitHub

1. Ve al repositorio → pestaña **Security**
2. Selecciona **Secret scanning** en el menú lateral

Para cada alerta verás:
- Tipo de secreto detectado (ej: `GitHub Personal Access Token`)
- Archivo y línea donde fue encontrado
- Estado: `Open` / `Closed` / `Revoked`
- Opción para marcarla como falso positivo o como resuelta

---

## Paso 2b — Validity Checks: priorizar alertas de riesgo inmediato

> **📌 Concepto clave (GH-500 Q30):** **Secret validation** (validity checks) verifica si un secreto encontrado en el repositorio **sigue activo y válido** con el proveedor emisor (AWS, GitHub, Stripe, etc.). Si el secreto está activo, la alerta se marca como **"Active"** — lo que indica un riesgo de seguridad inmediato y explotable. Esto permite al equipo priorizar esas alertas sobre las de secretos ya expirados o revocados.

### Estados de validación

| Badge en la alerta | Estado | Significado |
|---|---|---|
| `Active` | Verificado activo | El proveedor confirmó que el secreto es válido — **atender de inmediato** |
| `Inactive` | Verificado inactivo | El secreto fue revocado — verificar que no hubo acceso no autorizado |
| `Unknown` | No verificable | GitHub no puede validar este tipo de secreto con el proveedor |

### Cómo habilitarlo

**GitHub tokens** — activo por defecto en todos los repos con Secret Scanning habilitado, sin configuración adicional.

**Partner patterns** (AWS, Stripe, SendGrid, Google, etc.) — requiere habilitación explícita:

```
Settings → Code security → Secret scanning → Validity checks → Enable
```

O a nivel organización vía Security Configurations:
```
Organization Settings → Code security → Configurations → [tu config] → Validity checks
```

> **Requisito de licencia:** GitHub Team, o GitHub Enterprise Cloud/Server con **GitHub Secret Protection**.

### On-demand validity check

Una vez habilitado, en cada alerta aparece el botón **"Verify secret"** para lanzar una verificación inmediata contra el proveedor sin esperar el siguiente scan automático.

### Secretos del proyecto y su validación

| Secreto | Tipo | Validación disponible |
|---|---|---|
| `ghp_...` en `AuthService.cs` | GitHub PAT | ✅ Activa por defecto |
| `AKIA...` en `AuthService.cs` | AWS Access Key | ✅ Con validity checks habilitado |
| `pk_live_...` en `appsettings.json` | Stripe Live Key | ✅ Con validity checks habilitado |
| `SG....` en `appsettings.json` | SendGrid API Key | ✅ Con validity checks habilitado |
| `JWT_SECRET` en `appsettings.json` | Custom JWT secret | ❌ `Unknown` — sin endpoint de validación |
| Azure Storage connection string | Azure Storage | ❌ `Unknown` — no soportado actualmente |

### Otras features de evaluación de alertas

- **GitHub token metadata** *(preview)*: para tokens activos muestra owner, fecha de creación, último uso y si tiene acceso a organizaciones.
- **Extended metadata** *(preview)*: para OpenAI API, Google OAuth y Slack — muestra owner ID, email, nombre de org y fecha de expiración.
- **Alert labels**: etiquetas adicionales en la alerta:
  - `public leak` → el mismo secreto fue encontrado en código público
  - `multi-repo` → el secreto existe en múltiples repos de la organización

---

## Paso 3 — Entender el flujo de detección

```
Commit con secreto
        ↓
GitHub analiza el diff con ~200 patrones integrados
        ↓
¿Coincidencia?
        ↓
Alerta en Security → Secret scanning
        ↓
(Si el proveedor participa en el programa de notificación)
GitHub notifica al proveedor → el token puede ser revocado automáticamente
```

### Proveedores con revocación automática (selección)

| Proveedor | Tipo de token |
|---|---|
| GitHub | Personal Access Tokens, Fine-grained tokens, OAuth tokens |
| AWS | Access Keys |
| Azure | Storage keys, Cosmos DB keys |
| Stripe | Live API keys |
| SendGrid | API keys |
| Google | API keys, Service account credentials |

---

## Paso 4 — Push Protection

> **📌 Concepto clave:** Secret scanning push protection es una feature **proactiva** que escanea el código **durante el proceso de push**. Si detecta un secreto, **bloquea el push antes de que el código sea agregado al repositorio**, previniendo la exposición accidental de información sensible.
>
> Esto la diferencia de Secret Scanning estándar, que detecta secretos **después** de que ya están en el historial de Git.

Push Protection bloquea el push **antes** de que el secreto llegue al repositorio. Es la capa de prevención, mientras que Secret Scanning es la capa de detección.

### Habilitar Push Protection

```
Settings → Code security → Secret scanning → Push protection → Enable
```

### Demostración del bloqueo

Intenta hacer commit de un archivo con un token real:

```bash
# Crea un archivo de prueba con un secreto
echo 'GITHUB_TOKEN=ghp_TestTokenForPushProtectionDemo12345' > test-secret.txt
git add test-secret.txt
git commit -m "test: push protection demo"
git push
```

**Resultado esperado con Push Protection activo:**

```
remote: Push rejected.
remote: 
remote:  — Secret scanning found a GitHub Personal Access Token:
remote:    test-secret.txt:1: GITHUB_TOKEN=ghp_TestTokenForPushProtectionDemo12345
remote: 
remote: To push this commit, either remove the secret, or allow it via the UI.
```

El desarrollador tiene tres opciones:
1. **Eliminar el secreto** del código (opción recomendada)
2. **Marcar como falso positivo** en la UI de GitHub
3. **Marcar como "used in tests"** (solo para entornos de prueba)

---

## Paso 5 — Custom Patterns para secretos internos

Los secretos con formato propietario de tu empresa no son detectados por los patrones integrados. Para ello se usan **Custom Patterns**.

Consulta la guía completa en [custom-patterns.md](./custom-patterns.md).

### Secretos internos del proyecto demo

Abre `src/UsersApi/Services/CustomPatternDemoService.cs`:

```csharp
// Formato interno de empresa — NO detectado por GitHub sin custom pattern
private const string InternalApiKey  = "MYCO-PRD-1042-a3f9c21b";
private const string DbAccessToken   = "DB-TOKEN-20260101-Xk92mNpQ7rLwVjT4";
private const string ServiceAccountKey = "SVC-payments-prod-aB3cD4eF5gH6iJ7kL8mN9oP0";
private const string WebhookSecret   = "whsec_MyCompanyWebhookSecret1234567890AbCdEf";
```

Para que GitHub los detecte, debes crear un Custom Pattern con la regex correspondiente:

| Secreto | Regex del Custom Pattern |
|---|---|
| `MYCO-PRD-1042-a3f9c21b` | `MYCO-[A-Z]{3}-[0-9]{4}-[a-f0-9]{8}` |
| `DB-TOKEN-20260101-...` | `DB-TOKEN-[0-9]{8}-[A-Za-z0-9]{16}` |
| `SVC-payments-prod-...` | `SVC-[a-z]+-[a-z]+-[A-Za-z0-9]{24}` |
| `whsec_...` | `whsec_[A-Za-z0-9]{40}` |

### Crear un Custom Pattern

1. Settings → Security → Secret scanning → Custom patterns → **New pattern**
2. Ingresa el nombre y la regex en **Secret format**
3. Haz clic en **Save and dry run** para probar contra el historial
4. Revisa los resultados y ajusta la regex si hay falsos positivos
5. Haz clic en **Publish pattern** para activarlo

> ⚠️ Restricción de Hyperscan (el motor regex de GitHub):
> Los campos Before/After secret **no aceptan** patrones de longitud cero como `["']?` o `\s*`.
> Si necesitas contexto, deja esos campos vacíos y usa el default de GitHub.

---

## Paso 6 — Resolución de alertas

Para cada alerta de Secret Scanning debes:

1. **Revocar el secreto** en el proveedor (aunque GitHub lo haga automáticamente, confírmalo)
2. **Rotar el secreto** — generar uno nuevo y actualizar los sistemas que lo usan
3. **Cerrar la alerta** en GitHub marcándola como resuelta
4. **Remediar el código** — eliminar el secreto hardcodeado y moverlo a:
   - Variables de entorno
   - Azure Key Vault / AWS Secrets Manager
   - GitHub Secrets (para CI/CD)

---

## Buenas prácticas

| Práctica | Descripción |
|---|---|
| Nunca almacenar secretos en código | Aunque sea en una rama temporal o un commit que se va a revertir |
| Habilitar Push Protection | Previene antes de que llegue al historial de Git |
| Usar `.gitignore` | Excluye archivos `.env`, `appsettings.Production.json`, etc. |
| Rotar secretos periódicamente | Incluso si no hay evidencia de exposición |
| Usar secretos con ámbito mínimo | Principio de mínimo privilegio en tokens y API keys |

---

## Resumen

| Feature | Secret Scanning | Push Protection |
|---|---|---|
| ¿Cuándo actúa? | Después del push | Antes del push |
| ¿Qué detecta? | ~200 tipos de tokens conocidos + Custom Patterns | Los mismos patrones |
| ¿Dónde se ve? | Security → Secret scanning alerts | Error en el terminal del dev |
| ¿Bloquea el merge? | No (solo alerta) | Sí (bloquea el push) |

---

## Siguiente paso

➡️ [Lab 04 — Code Scanning: análisis estático con CodeQL](./04-code-scanning.md)
