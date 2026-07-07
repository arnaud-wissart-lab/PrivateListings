[CmdletBinding()]
param(
    [string]$ComposeFile = (Join-Path $PSScriptRoot "..\docker-compose.geekom.yml"),
    [string]$EnvFile = (Join-Path $PSScriptRoot "..\.env"),
    [string]$RemoteHost,
    [string]$RemoteUser,
    [string]$RemotePath = "/opt/privatelistings",
    [string]$ImageTag,
    [switch]$PruneImages
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

function ConvertTo-ShellSingleQuoted {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return "'" + $Value.Replace("'", "'\''") + "'"
}

if (-not [string]::IsNullOrWhiteSpace($ImageTag)) {
    $env:PRIVATE_LISTINGS_IMAGE_TAG = $ImageTag
}

if (-not [string]::IsNullOrWhiteSpace($RemoteHost)) {
    $remoteTarget = if ([string]::IsNullOrWhiteSpace($RemoteUser)) {
        $RemoteHost
    }
    else {
        "$RemoteUser@$RemoteHost"
    }

    $remotePathQuoted = ConvertTo-ShellSingleQuoted $RemotePath
    $commands = [System.Collections.Generic.List[string]]::new()
    $commands.Add("set -euo pipefail")
    $commands.Add("cd $remotePathQuoted")

    if (-not [string]::IsNullOrWhiteSpace($ImageTag)) {
        $commands.Add("export PRIVATE_LISTINGS_IMAGE_TAG=$(ConvertTo-ShellSingleQuoted $ImageTag)")
    }

    $commands.Add("docker compose --env-file .env -f docker-compose.geekom.yml pull")
    $commands.Add("docker compose --env-file .env -f docker-compose.geekom.yml up -d --remove-orphans")

    if ($PruneImages) {
        $commands.Add("docker image prune -f")
    }

    Invoke-CheckedNativeCommand "ssh" @($remoteTarget, ($commands -join "; "))
    Write-Host "Déploiement distant terminé sur $remoteTarget."
    return
}

$resolvedComposeFile = Resolve-RequiredFile $ComposeFile "Fichier Compose"
$resolvedEnvFile = Resolve-RequiredFile $EnvFile "Fichier d'environnement"
$composeArguments = @("compose", "--env-file", $resolvedEnvFile, "-f", $resolvedComposeFile)

Invoke-CheckedNativeCommand "docker" ($composeArguments + @("pull"))
Invoke-CheckedNativeCommand "docker" ($composeArguments + @("up", "-d", "--remove-orphans"))

if ($PruneImages) {
    Invoke-CheckedNativeCommand "docker" @("image", "prune", "-f")
}

Write-Host "Déploiement local terminé."
