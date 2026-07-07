[CmdletBinding()]
param(
    [string]$ComposeFile = (Join-Path $PSScriptRoot "..\docker-compose.geekom.yml"),
    [string]$EnvFile = (Join-Path $PSScriptRoot "..\.env"),
    [string]$BackupDirectory,
    [string]$Database,
    [string]$Username,
    [string]$ServiceName = "privatelistings-postgres",
    [int]$RetentionDays = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RequiredFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    if (-not (Test-Path -LiteralPath $resolved.Path -PathType Leaf)) {
        throw "$Description introuvable : $Path"
    }

    return $resolved.Path
}

function Get-EnvFileValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Key,
        [string]$DefaultValue
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $DefaultValue
    }

    $pattern = "^\s*$([regex]::Escape($Key))\s*=\s*(.*)\s*$"
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match "^\s*#") {
            continue
        }

        if ($line -match $pattern) {
            $value = $Matches[1].Trim()
            if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                $value = $value.Substring(1, $value.Length - 2)
            }

            return $value
        }
    }

    return $DefaultValue
}

function Resolve-PathFromEnvDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$EnvDirectory
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $EnvDirectory $Path))
}

function Invoke-CheckedNativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "La commande '$FilePath' a échoué avec le code $LASTEXITCODE."
    }
}

function Get-CheckedNativeOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "La commande '$FilePath' a échoué avec le code $LASTEXITCODE."
    }

    return ($output | Select-Object -First 1)
}

$resolvedComposeFile = Resolve-RequiredFile $ComposeFile "Fichier Compose"
$resolvedEnvFile = Resolve-RequiredFile $EnvFile "Fichier d'environnement"
$envDirectory = Split-Path -Parent $resolvedEnvFile

if ([string]::IsNullOrWhiteSpace($Database)) {
    $Database = Get-EnvFileValue $resolvedEnvFile "POSTGRES_DB" "privatelistings"
}

if ([string]::IsNullOrWhiteSpace($Username)) {
    $Username = Get-EnvFileValue $resolvedEnvFile "POSTGRES_USER" "privatelistings"
}

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Get-EnvFileValue $resolvedEnvFile "PRIVATE_LISTINGS_BACKUP_DIRECTORY" "./backups"
}

if ($RetentionDays -lt 0) {
    $retentionValue = Get-EnvFileValue $resolvedEnvFile "PRIVATE_LISTINGS_BACKUP_RETENTION_DAYS" "14"
    if (-not [int]::TryParse($retentionValue, [ref]$RetentionDays)) {
        throw "PRIVATE_LISTINGS_BACKUP_RETENTION_DAYS doit être un entier."
    }
}

$resolvedBackupDirectory = Resolve-PathFromEnvDirectory $BackupDirectory $envDirectory
New-Item -ItemType Directory -Path $resolvedBackupDirectory -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $resolvedBackupDirectory "privatelistings-db-$timestamp.dump"
$containerDumpPath = "/tmp/privatelistings-db-$timestamp.dump"
$composeArguments = @("compose", "--env-file", $resolvedEnvFile, "-f", $resolvedComposeFile)

$containerId = Get-CheckedNativeOutput "docker" ($composeArguments + @("ps", "-q", $ServiceName))
if ([string]::IsNullOrWhiteSpace($containerId)) {
    throw "Le conteneur du service '$ServiceName' est introuvable. Lancez d'abord PostgreSQL avec Docker Compose."
}

try {
    Invoke-CheckedNativeCommand "docker" ($composeArguments + @(
            "exec",
            "-T",
            $ServiceName,
            "pg_dump",
            "-U",
            $Username,
            "-d",
            $Database,
            "--format=custom",
            "--no-owner",
            "--no-acl",
            "--file=$containerDumpPath"
        ))

    Invoke-CheckedNativeCommand "docker" @("cp", "${containerId}:$containerDumpPath", $backupPath)
}
finally {
    & docker @($composeArguments + @("exec", "-T", $ServiceName, "rm", "-f", $containerDumpPath)) | Out-Null
}

if ($RetentionDays -gt 0) {
    $backupRoot = [System.IO.Path]::GetFullPath($resolvedBackupDirectory).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $threshold = (Get-Date).AddDays(-$RetentionDays)

    Get-ChildItem -LiteralPath $resolvedBackupDirectory -Filter "privatelistings-db-*.dump" -File |
        Where-Object { $_.LastWriteTime -lt $threshold } |
        ForEach-Object {
            $candidate = [System.IO.Path]::GetFullPath($_.FullName)
            if (-not $candidate.StartsWith($backupRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Refus de supprimer un fichier hors du dossier de sauvegarde : $candidate"
            }

            Remove-Item -LiteralPath $candidate -Force
        }
}

Write-Host "Sauvegarde PostgreSQL créée : $backupPath"
