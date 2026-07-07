[CmdletBinding()]
param(
    [switch]$IncludeE2E
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "PrivateListings.sln"

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
    Invoke-Step "Restauration" {
        dotnet restore $solutionPath
        Assert-NativeCommandSucceeded "dotnet restore"
    }

    Invoke-Step "Assets npm" {
        $packages = Get-ChildItem -Path $repoRoot -Recurse -Filter "package.json" -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' }

        if (-not $packages) {
            Write-Host "Aucun package.json détecté."
            return
        }

        foreach ($package in $packages) {
            Push-Location $package.DirectoryName
            try {
                if (-not (Test-Path package-lock.json)) {
                    throw "package-lock.json est requis pour un build npm reproductible dans $($package.DirectoryName)."
                }

                npm ci
                Assert-NativeCommandSucceeded "npm ci"
                npm run build --if-present
                Assert-NativeCommandSucceeded "npm run build"
            }
            finally {
                Pop-Location
            }
        }
    }

    Invoke-Step "Compilation Release" {
        dotnet build $solutionPath --configuration Release --no-restore
        Assert-NativeCommandSucceeded "dotnet build"
    }

    Invoke-Step "Vérification du format" {
        dotnet format $solutionPath --verify-no-changes --no-restore --verbosity normal
        Assert-NativeCommandSucceeded "dotnet format"
    }

    Invoke-Step "Tests automatisés" {
        dotnet test $solutionPath --configuration Release --no-build --filter "Category!=E2E"
        Assert-NativeCommandSucceeded "dotnet test"
    }

    if ($IncludeE2E) {
        Invoke-Step "Tests E2E Playwright" {
            & (Join-Path $repoRoot "scripts/e2e.ps1") -Configuration Release -SkipBuild
        }
    }
}
finally {
    Pop-Location
}
