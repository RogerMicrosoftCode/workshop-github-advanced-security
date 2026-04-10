# Security Policy

> ⚠️ **This repository contains intentional vulnerabilities** for GitHub Advanced Security (GHAS) workshop purposes.
> Do **not** deploy this code to any environment. See [Purpose](#purpose) below.

## Purpose

This project is a demo application used to showcase GitHub Advanced Security features:

- **Code Scanning (CodeQL)** — SQL Injection, Path Traversal, SSRF, XXE
- **Secret Scanning** — Hardcoded tokens, API keys, connection strings
- **Dependabot** — Vulnerable NuGet packages with known CVEs
- **Custom Patterns** — Internal secret formats not covered by default rules

The vulnerabilities are **deliberate and documented**. They are not bugs to be fixed.

---

## Supported Versions

Only the latest commit on `main` is supported for workshop use.

| Branch  | Supported |
|---------|-----------|
| `main`  | ✅ Yes     |
| Others  | ❌ No      |

---

## Reporting a Vulnerability

If you find an **unintentional** vulnerability (i.e., not part of the GHAS demo), please report it responsibly:

### Private Vulnerability Reporting (Recommended)

Use GitHub's built-in private reporting:

1. Go to the **Security** tab of this repository
2. Click **"Report a vulnerability"**
3. Fill in the details — GitHub will create a private advisory draft

This keeps the report confidential until a fix is released (coordinated disclosure).

### What to Include in Your Report

- Description of the vulnerability and its potential impact
- Steps to reproduce
- Affected file(s) and line numbers
- Suggested fix (optional but appreciated)

### Response Timeline

| Stage | Timeline |
|---|---|
| Acknowledgement | Within **48 hours** |
| Initial assessment | Within **5 business days** |
| Fix or mitigation | Within **30 days** for critical/high severity |
| Public disclosure | After fix is released and verified |

### Out of Scope

The following are **intentional** and should not be reported:

- SQL Injection in `AuthService.cs`
- Path Traversal in `ReportService.cs`
- SSRF in `ReportService.cs`
- XXE in `ReportService.cs`
- Hardcoded secrets in `AuthService.cs` and `appsettings.json`
- CVEs in `Newtonsoft.Json 12.0.2`, `log4net 2.0.10`, `Microsoft.Data.SqlClient 2.0.0`

---

## Security Features Enabled on This Repository

| Feature | Status |
|---|---|
| Code Scanning (CodeQL) | ✅ Configured via `.github/workflows/codeql.yml` |
| Secret Scanning | ✅ Enable in Settings → Security |
| Push Protection | ✅ Recommended — enable in Settings → Security |
| Dependabot Alerts | ✅ Configured via `.github/dependabot.yml` |
| Dependabot Security Updates | ✅ Recommended — enable in Settings → Security |
| Private Vulnerability Reporting | ✅ Enabled |

---

## Security Best Practices (for production use)

If you adapt this project for production, apply these fixes:

### Secrets
- Store all secrets in **environment variables** or **Azure Key Vault** — never in source code
- Use **GitHub Secrets** for CI/CD workflows
- Enable **Push Protection** to block accidental secret commits

### SQL Injection
- Always use **parameterized queries** (`SqlParameter`) or an ORM like EF Core
- Never concatenate user input into SQL strings

### Path Traversal
- Validate that the resolved path starts with the expected base directory:
  ```csharp
  var fullPath = Path.GetFullPath(Path.Combine(basePath, userInput));
  if (!fullPath.StartsWith(basePath)) throw new UnauthorizedAccessException();
  ```

### SSRF
- Implement an **allowlist** of permitted domains — never make HTTP requests to URLs controlled by users

### XXE
- Disable external entity processing in `XmlDocument`:
  ```csharp
  var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
  ```

### Dependencies
- Keep packages updated — enable **Dependabot Security Updates** to get automatic PRs for CVE fixes
