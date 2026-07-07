[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Headed,

    [switch]$SkipBuild,

    [switch]$InstallWithDeps,

    [string]$Filter
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "tests/PrivateListings.E2ETests/PrivateListings.E2ETests.csproj"
$resultsDirectory = Join-Path $repoRoot "TestResults/E2E"
$playwrightScript = Join-Path $repoRoot "tests/PrivateListings.E2ETests/bin/$Configuration/net10.0/playwright.ps1"

function Invoke-Step {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Name"
    & $Command
}

function Assert-NativeCommandSucceeded {
    param(
        [Parameter(Mandatory)]
        [string]$CommandName
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$CommandName a échoué avec le code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    if (-not $SkipBuild) {
        Invoke-Step "Compilation du projet E2E" {
            dotnet build $projectPath --configuration $Configuration
            Assert-NativeCommandSucceeded "dotnet build"
        }
    }

    if (-not (Test-Path $playwrightScript)) {
        throw "Le script Playwright est introuvable : $playwrightScript. Lancez sans -SkipBuild pour générer les artefacts du projet."
    }

    Invoke-Step "Installation du navigateur Playwright" {
        $installArguments = @("install")
        if ($InstallWithDeps) {
            $installArguments += "--with-deps"
        }

        $installArguments += "chromium"
        & $playwrightScript @installArguments
        Assert-NativeCommandSucceeded "playwright install"
    }

    Invoke-Step "Tests E2E Playwright" {
        New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null

        $env:E2E_HEADED = if ($Headed) { "1" } else { "0" }
        $env:E2E_APP_CONFIGURATION = $Configuration

        $testArguments = @(
            "test",
            $projectPath,
            "--configuration",
            $Configuration,
            "--no-build",
            "--logger",
            "trx;LogFileName=e2e-results.trx",
            "--results-directory",
            $resultsDirectory
        )

        if (-not [string]::IsNullOrWhiteSpace($Filter)) {
            $testArguments += "--filter"
            $testArguments += $Filter
        }

        dotnet @testArguments
        Assert-NativeCommandSucceeded "dotnet test E2E"
    }
}
finally {
    Remove-Item Env:E2E_HEADED -ErrorAction SilentlyContinue
    Remove-Item Env:E2E_APP_CONFIGURATION -ErrorAction SilentlyContinue
    Pop-Location
}
