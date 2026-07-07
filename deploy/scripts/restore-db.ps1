[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,
    [string]$ComposeFile = (Join-Path $PSScriptRoot "..\docker-compose.geekom.yml"),
    [string]$EnvFile = (Join-Path $PSScriptRoot "..\.env"),
    [string]$Database,
    [string]$Username,
    [string]$ServiceName = "privatelistings-postgres",
    [switch]$RecreateDatabase,
    [switch]$KeepApplicationRunning,
    [switch]$Force
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

function ConvertTo-SqlStringLiteral {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return "'" + $Value.Replace("'", "''") + "'"
}

$resolvedBackupPath = Resolve-RequiredFile $BackupPath "Fichier de sauvegarde"
$resolvedComposeFile = Resolve-RequiredFile $ComposeFile "Fichier Compose"
$resolvedEnvFile = Resolve-RequiredFile $EnvFile "Fichier d'environnement"

if ([string]::IsNullOrWhiteSpace($Database)) {
    $Database = Get-EnvFileValue $resolvedEnvFile "POSTGRES_DB" "privatelistings"
}

if ([string]::IsNullOrWhiteSpace($Username)) {
    $Username = Get-EnvFileValue $resolvedEnvFile "POSTGRES_USER" "privatelistings"
}

if (-not $Force) {
    Write-Warning "Cette restauration va modifier la base PostgreSQL '$Database'."
    $confirmation = Read-Host "Tapez RESTAURER pour continuer"
    if ($confirmation -ne "RESTAURER") {
        throw "Restauration annulée."
    }
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$containerRestorePath = "/tmp/privatelistings-restore-$timestamp.dump"
$composeArguments = @("compose", "--env-file", $resolvedEnvFile, "-f", $resolvedComposeFile)
$applicationServices = @("privatelistings-web", "privatelistings-worker")
$applicationStopped = $false

Invoke-CheckedNativeCommand "docker" ($composeArguments + @("up", "-d", $ServiceName))
$containerId = Get-CheckedNativeOutput "docker" ($composeArguments + @("ps", "-q", $ServiceName))
if ([string]::IsNullOrWhiteSpace($containerId)) {
    throw "Le conteneur du service '$ServiceName' est introuvable."
}

try {
    if (-not $KeepApplicationRunning) {
        Invoke-CheckedNativeCommand "docker" ($composeArguments + @("stop") + $applicationServices)
        $applicationStopped = $true
    }

    Invoke-CheckedNativeCommand "docker" @("cp", $resolvedBackupPath, "${containerId}:$containerRestorePath")

    if ($RecreateDatabase) {
        $databaseLiteral = ConvertTo-SqlStringLiteral $Database
        Invoke-CheckedNativeCommand "docker" ($composeArguments + @(
                "exec",
                "-T",
                $ServiceName,
                "psql",
                "-U",
                $Username,
                "-d",
                "postgres",
                "-v",
                "ON_ERROR_STOP=1",
                "-c",
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $databaseLiteral AND pid <> pg_backend_pid();"
            ))

        Invoke-CheckedNativeCommand "docker" ($composeArguments + @(
                "exec",
                "-T",
                $ServiceName,
                "dropdb",
                "-U",
                $Username,
                "--if-exists",
                $Database
            ))

        Invoke-CheckedNativeCommand "docker" ($composeArguments + @(
                "exec",
                "-T",
                $ServiceName,
                "createdb",
                "-U",
                $Username,
                $Database
            ))

        Invoke-CheckedNativeCommand "docker" ($composeArguments + @(
                "exec",
                "-T",
                $ServiceName,
                "pg_restore",
                "-U",
                $Username,
                "-d",
                $Database,
                "--no-owner",
                "--no-acl",
                $containerRestorePath
            ))
    }
    else {
        Invoke-CheckedNativeCommand "docker" ($composeArguments + @(
                "exec",
                "-T",
                $ServiceName,
                "pg_restore",
                "-U",
                $Username,
                "-d",
                $Database,
                "--clean",
                "--if-exists",
                "--no-owner",
                "--no-acl",
                $containerRestorePath
            ))
    }
}
finally {
    & docker @($composeArguments + @("exec", "-T", $ServiceName, "rm", "-f", $containerRestorePath)) | Out-Null

    if ($applicationStopped) {
        & docker @($composeArguments + @("up", "-d") + $applicationServices) | Out-Null
    }
}

Write-Host "Restauration PostgreSQL terminée depuis : $resolvedBackupPath"
