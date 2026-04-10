# Lab 04 — Code Scanning: Análisis Estático con CodeQL

## Objetivos

- Entender cómo CodeQL analiza el flujo de datos para detectar vulnerabilidades
- Identificar las vulnerabilidades del proyecto y sus patrones de código
- Comparar las dos estrategias de build: `autobuild` vs custom build steps
- Revisar las alertas generadas y aplicar las correcciones

---

## Contexto: ¿Cómo funciona CodeQL?

CodeQL no busca patrones de texto — analiza el **flujo de datos** a través del código:

```
Source (dato controlado por el atacante)
    ↓
Transformaciones intermedias (asignaciones, concatenaciones)
    ↓
Sink (operación sensible: query SQL, lectura de archivo, request HTTP)
    ↓
¿El dato llega al sink sin sanitizar? → ALERTA
```

Este enfoque se llama **taint analysis** y es superior a los linters tradicionales porque detecta vulnerabilidades aunque el código esté distribuido en múltiples métodos y archivos.

---

## Paso 1 — El workflow de CodeQL

### Nombre del archivo: `codeql.yml`

Cuando habilitas Code Scanning con **Advanced Setup** desde la UI de GitHub
(Settings → Code Security → Code scanning → Advanced), GitHub genera automáticamente
un archivo llamado `codeql.yml` en `.github/workflows/`.

Este nombre es el estándar del Advanced Setup — a diferencia del Default Setup
(que no genera archivo de workflow visible), el Advanced Setup te entrega el YAML
completo para que lo personalices.

Abre `.github/workflows/codeql.yml`. El archivo define dos jobs:

> **📌 Concepto clave (GH-500):** Para lenguajes compilados (C#, Java, C/C++, Go, Swift), CodeQL realiza la extracción **monitoreando el proceso de build**. Observa los comandos de compilación (`make`, `javac`, `dotnet build`) y extrae los datos relevantes de cada paso ejecutado. Con esta información construye una **base de datos semántica** del código.
>
> Este enfoque garantiza que CodeQL capture una representación precisa y real del código tal como se compila, incluyendo configuraciones específicas de plataforma o lógica condicional usada durante el build.

### Job 1: `analyze-autobuild`

```yaml
- name: Autobuild
  uses: github/codeql-action/autobuild@v3
```

CodeQL detecta automáticamente el sistema de build del proyecto y lo ejecuta.
Para .NET, identifica el `.csproj` o `.sln` y ejecuta `dotnet build`.

**Ventaja:** sin configuración extra.
**Limitación:** falla en proyectos con estructura no estándar.

### Job 2: `analyze-custom-build`

```yaml
- name: Restore NuGet packages
  run: dotnet restore src/UsersApi/UsersApi.csproj

- name: Build
  run: |
    dotnet build src/UsersApi/UsersApi.csproj \
      --configuration Release \
      --no-restore \
      /p:UseSharedCompilation=false
```

Comandos de compilación explícitos.
`/p:UseSharedCompilation=false` es crítico: desactiva el servidor de compilación compartido de Roslyn para que CodeQL pueda interceptar todos los procesos del compilador.

**Ventaja:** control total (versión del SDK, flags, proyectos específicos).
**Uso:** monorepos, proyectos con generación de código, CI/CD complejos.

---

## Paso 2 — Suite de queries

En ambos jobs se usa:

```yaml
queries: security-extended
```

| Suite | Descripción | Falsos positivos |
|---|---|---|
| `default` | Queries de alta precisión y bajo ruido | Bajo |
| `security-extended` | Más queries de seguridad, mayor cobertura | Medio |
| `security-and-quality` | Seguridad + problemas de calidad de código | Alto |

Para este workshop se usa `security-extended` para maximizar las alertas detectadas.

---

## Paso 3 — Vulnerabilidades del proyecto

### SQL Injection — `AuthService.cs`

```csharp
// ❌ VULNERABLE: el input del usuario se concatena directamente en la query
public async Task<bool> ValidateUserAsync(string username, string password)
{
    var query = "SELECT COUNT(*) FROM Users WHERE Name = '" + username
              + "' AND Password = '" + password + "'";
    // ...
}

// ❌ VULNERABLE: variante con string interpolation (también detectada)
public async Task<object?> FindUserByNameAsync(string name)
{
    var query = $"SELECT * FROM Users WHERE Name = '{name}'";
    // ...
}
```

**Exploit de ejemplo:**
```
username: ' OR '1'='1' --
password: anything
```
La query resultante devuelve todos los usuarios, bypasseando la autenticación.

**Fix:**
```csharp
// ✅ CORRECTO: parámetros SQL
var query = "SELECT COUNT(*) FROM Users WHERE Name = @Name AND Password = @Pass";
command.Parameters.AddWithValue("@Name", username);
command.Parameters.AddWithValue("@Pass", password);
```

---

### Path Traversal — `ReportService.cs`

```csharp
// ❌ VULNERABLE: el input del usuario controla la ruta del archivo
public string GetReportContent(string fileName)
{
    var filePath = Path.Combine(_reportsBasePath, fileName);
    return File.ReadAllText(filePath);
}
```

**Exploit de ejemplo:**
```
GET /api/reports/file?fileName=../../etc/passwd
```
`Path.Combine("reports", "../../etc/passwd")` resulta en `/etc/passwd`.

**Fix:**
```csharp
// ✅ CORRECTO: validar que la ruta está dentro del directorio permitido
public string GetReportContent(string fileName)
{
    var fullPath = Path.GetFullPath(Path.Combine(_reportsBasePath, fileName));
    if (!fullPath.StartsWith(_reportsBasePath))
        throw new UnauthorizedAccessException("Acceso denegado");
    return File.ReadAllText(fullPath);
}
```

---

### SSRF — Server-Side Request Forgery — `ReportService.cs`

```csharp
// ❌ VULNERABLE: la URL viene del usuario sin validación
public async Task<string> FetchExternalReportAsync(string url)
{
    var client = _httpClientFactory.CreateClient();
    var response = await client.GetAsync(url);
    return await response.Content.ReadAsStringAsync();
}
```

**Exploit de ejemplo:**
```
GET /api/reports/fetch?url=http://169.254.169.254/metadata/instance
```
Permite acceder al endpoint de metadata de instancias en Azure/AWS/GCP, obteniendo credenciales temporales de la identidad asignada.

**Fix:**
```csharp
// ✅ CORRECTO: allowlist de dominios permitidos
public async Task<string> FetchExternalReportAsync(string url)
{
    var allowedHosts = new[] { "reports.empresa.com", "api.proveedor.com" };
    var uri = new Uri(url);
    if (!allowedHosts.Contains(uri.Host))
        throw new ArgumentException("URL no permitida");

    var client = _httpClientFactory.CreateClient();
    var response = await client.GetAsync(uri);
    return await response.Content.ReadAsStringAsync();
}
```

---

### XXE — XML External Entity — `ReportService.cs`

```csharp
// ❌ VULNERABLE: XmlDocument resuelve entidades externas por defecto
public string ParseUserXmlData(string xmlInput)
{
    var xmlDoc = new XmlDocument();
    xmlDoc.LoadXml(xmlInput);
    return xmlDoc.SelectSingleNode("//Name")?.InnerText ?? "Unknown";
}
```

**Exploit de ejemplo:**
```xml
<?xml version="1.0"?>
<!DOCTYPE foo [
  <!ENTITY xxe SYSTEM "file:///etc/passwd">
]>
<User><Name>&xxe;</Name></User>
```
El servidor lee `/etc/passwd` y lo devuelve en la respuesta.

**Fix:**
```csharp
// ✅ CORRECTO: deshabilitar DTD y entidades externas
public string ParseUserXmlData(string xmlInput)
{
    var settings = new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };
    using var reader = XmlReader.Create(new StringReader(xmlInput), settings);
    var xmlDoc = new XmlDocument { XmlResolver = null };
    xmlDoc.Load(reader);
    return xmlDoc.SelectSingleNode("//Name")?.InnerText ?? "Unknown";
}
```

---

### Deserialización insegura — `ReportService.cs`

```csharp
// ❌ VULNERABLE: TypeNameHandling.All permite instanciar tipos arbitrarios
public object? DeserializeUserData(string json)
{
    var settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All  // ❌
    };
    return JsonConvert.DeserializeObject(json, settings);
}
```

**Por qué es peligroso:**
`TypeNameHandling.All` incluye metadatos de tipo en el JSON serializado y los lee al deserializar. Un atacante puede enviar un JSON que instancie tipos del sistema (como `System.IO.File`) para ejecutar código arbitrario.

**Fix:**
```csharp
// ✅ CORRECTO: nunca usar TypeNameHandling.All con datos externos
public UserData? DeserializeUserData(string json)
{
    return JsonConvert.DeserializeObject<UserData>(json); // tipo explícito y seguro
}
```

---

## Paso 4 — Ver las alertas en GitHub

1. Ve al repositorio → **Security** → **Code scanning**
2. Verás las alertas agrupadas por tipo de vulnerabilidad
3. Cada alerta muestra:
   - Archivo y línea exacta
   - Descripción del problema
   - El **data flow path** — cómo el dato viaja desde el source hasta el sink
   - Sugerencia de fix
   - CWE asociado

### Filtrar alertas

```
Tool: CodeQL
Severity: High, Critical
Rule: sql-injection, path-injection, ssrf
```

---

## Paso 5 — Importancia del workflow file en la rama

Para que Code Scanning se ejecute en una rama, el archivo `codeql.yml` **debe existir en esa rama**. Si la rama no lo contiene, GitHub Actions no ejecutará el análisis aunque Code Scanning esté habilitado en el repositorio.

**Demostración:**

```bash
# Crea un branch SIN el workflow
git checkout -b demo/sin-codeql
git rm .github/workflows/codeql.yml
git commit -m "demo: branch without CodeQL workflow"
git push origin demo/sin-codeql
```

Abre un PR de `demo/sin-codeql` → `main`.
Observa que el check de CodeQL **no aparece** en el PR porque la rama origen no tiene el workflow.

---

## Resumen de vulnerabilidades detectadas

| Vulnerabilidad | Archivo | CWE | Endpoint afectado |
|---|---|---|---|
| SQL Injection (concatenación) | `AuthService.cs` | CWE-89 | `POST /api/auth/login` |
| SQL Injection (interpolación) | `AuthService.cs` | CWE-89 | `GET /api/auth/search` |
| Path Traversal | `ReportService.cs` | CWE-22 | `GET /api/reports/file` |
| SSRF | `ReportService.cs` | CWE-918 | `GET /api/reports/fetch` |
| XXE Injection | `ReportService.cs` | CWE-611 | `POST /api/reports/parse-xml` |
| Deserialización insegura | `ReportService.cs` | CWE-502 | `POST /api/reports/deserialize` |
| Clave JWT hardcodeada | `AuthService.cs` | CWE-321 | `POST /api/auth/login` |

---

## Siguiente paso

➡️ [Lab 05 — Dependency Review: bloqueo de CVEs en Pull Requests](./05-dependency-review.md)
