[CmdletBinding()]
param(
    [string]$Root = "",
    # The scanner contains detection signatures and is the only default allow-list entry.
    [string[]]$AllowedFiles = @("scripts/scan-secrets.ps1")
)

$ErrorActionPreference = "Stop"
$Root = if ([string]::IsNullOrWhiteSpace($Root)) {
    Join-Path $PSScriptRoot ".."
} else {
    $Root
}
$rootPath = (Resolve-Path $Root).Path
$trackedFiles = & git -C $rootPath ls-files --cached --others --exclude-standard

if ($LASTEXITCODE -ne 0) {
    throw "Unable to enumerate tracked files."
}

$archiveExtensions = @(".zip", ".7z", ".rar", ".pfx", ".p12", ".pem", ".key")
$textExtensions = @(
    ".cs", ".csproj", ".json", ".md", ".props", ".ps1", ".sh",
    ".targets", ".xml", ".yaml", ".yml"
)
$rules = @(
    @{ Code = "PRIVATE_KEY_CONTENT"; Pattern = "-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----" },
    @{ Code = "JWT_TOKEN"; Pattern = "eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}" },
    @{ Code = "CONNECTION_STRING_CREDENTIAL"; Pattern = "(?i)(?:server|host|database)\s*=[^;]+;[^\r\n]*(?:password|pwd)\s*=[^;]+" },
    @{ Code = "CONNECTION_STRING_JSON"; Pattern = '(?i)"(?:defaultconnection|connectionstring)"\s*:\s*"[^"]*(?:password|pwd)=[^"]+"' },
    @{ Code = "NAMED_SECRET_VALUE"; Pattern = '(?i)"?(?:clientsecret|api[_-]?key|private[_-]?key|access[_-]?token|refresh[_-]?token)"?\s*[:=]\s*"[^"]{4,}"' }
)

$findings = [System.Collections.Generic.List[object]]::new()

foreach ($relativePath in $trackedFiles) {
    if ([string]::IsNullOrWhiteSpace($relativePath) -or $AllowedFiles -contains $relativePath) {
        continue
    }

    $extension = [System.IO.Path]::GetExtension($relativePath).ToLowerInvariant()
    if ($archiveExtensions -contains $extension) {
        $findings.Add([pscustomobject]@{
            File = $relativePath
            Line = 0
            RuleCode = "TRACKED_SENSITIVE_OR_ARCHIVE_FILE"
            Redacted = $true
        })
        continue
    }

    if ($textExtensions -notcontains $extension -and
        [System.IO.Path]::GetFileName($relativePath) -notin @(".env", ".gitignore")) {
        continue
    }

    $absolutePath = Join-Path $rootPath $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadLines($absolutePath)) {
        $lineNumber++
        foreach ($rule in $rules) {
            if ($line -match $rule.Pattern) {
                $findings.Add([pscustomobject]@{
                    File = $relativePath
                    Line = $lineNumber
                    RuleCode = $rule.Code
                    Redacted = $true
                })
            }
        }
    }
}

foreach ($finding in $findings) {
    "FILE={0} LINE={1} RULE_CODE={2} REDACTED=true" -f `
        $finding.File, $finding.Line, $finding.RuleCode
}

if ($findings.Count -gt 0) {
    exit 1
}

"SECRET_SCAN=PASS FINDINGS=0"
