# Cross-platform PowerShell script to configure user secrets for Smtp2Go.NET test projects.
# Requires PowerShell Core (pwsh) to be installed.
# To run from the project root: pwsh -File ./scripts/setup-secrets.ps1

# --- Configuration ---
$ErrorActionPreference = "Stop"

# Define paths relative to the script's own location ($PSScriptRoot) to make it robust.
$scriptRoot = $PSScriptRoot
$IntegrationTestProject = Join-Path -Path $scriptRoot -ChildPath "..\tests\Smtp2Go.NET.IntegrationTests\Smtp2Go.NET.IntegrationTests.csproj"

$projects = @(
    $IntegrationTestProject
)

# Define the secrets with user-friendly prompts.
# Using [ordered] ensures that the prompts appear in the exact order they are defined here.
$secrets = [ordered]@{
    "Smtp2Go:ApiKey:Sandbox"  = "Enter your SMTP2GO Sandbox API Key (emails accepted, not delivered):";
    "Smtp2Go:ApiKey:Live"     = "Enter your SMTP2GO Live API Key (emails are actually delivered):";
    "Smtp2Go:TestSender"      = "Enter the verified sender email address (must be verified on your SMTP2GO account):";
    "Smtp2Go:TestRecipient"   = "Enter the test recipient email address for live delivery tests:";
}

# Optional secrets that are allowed to be empty.
$optionalSecrets = @()

# --- Script Body ---
Write-Host "--- Smtp2Go.NET Test Secret Setup ---" -ForegroundColor Yellow
Write-Host "This script will configure the necessary secrets for running integration tests."
Write-Host "The secrets will be stored securely using the .NET user-secrets tool."
Write-Host ""

# 1. Collect all secrets from the user first to avoid repetitive prompting.
$secretValues = @{}
foreach ($key in $secrets.Keys) {
    $prompt = $secrets[$key]
    # Determine if the secret is sensitive and should be read securely.
    $isSensitive = $key -like "*ApiKey*" -or $key -like "*Password*" -or $key -like "*AuthToken*"

    Write-Host $prompt -ForegroundColor Cyan

    if ($isSensitive) {
        $value = Read-Host -AsSecureString
    } else {
        $value = Read-Host -Prompt $prompt
    }

    # Check if the value is empty.
    $isEmpty = ($value -is [System.Security.SecureString] -and $value.Length -eq 0) -or
               ($value -isnot [System.Security.SecureString] -and [string]::IsNullOrWhiteSpace($value))

    if ($isEmpty) {
        if ($optionalSecrets -contains $key) {
            Write-Host "  (skipped)" -ForegroundColor DarkGray
            continue
        }
        Write-Error "Input cannot be empty. Aborting."
        return
    }

    $secretValues[$key] = $value
}

Write-Host ""
Write-Host "Secrets collected. Now applying to all test projects..." -ForegroundColor Green
Write-Host ""

# 2. Initialize and set secrets for each project.
foreach ($projectPath in $projects) {
    # Verify the project path exists before proceeding.
    if (-not (Test-Path -Path $projectPath -PathType Leaf)) {
        Write-Warning "Could not find project file at path: $projectPath. Skipping."
        continue
    }

    Write-Host "Configuring project: $projectPath" -ForegroundColor Magenta

    try {
        # Initialize user secrets for the project. This is idempotent.
        dotnet user-secrets init --project $projectPath | Out-Null
        Write-Host "  - Initialized user secrets."

        # Set each secret for the current project.
        foreach ($key in $secretValues.Keys) {
            $value = $secretValues[$key]

            # Special handling for SecureString to pass it to the command-line tool.
            if ($value -is [System.Security.SecureString]) {
                # Temporarily convert SecureString to plain text for the CLI command.
                $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
                $plainTextValue = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
                [System.Runtime.InteropServices.Marshal]::FreeBSTR($bstr)

                dotnet user-secrets set "$key" "$plainTextValue" --project $projectPath | Out-Null
                # Clear the plaintext variable immediately for security.
                Clear-Variable plainTextValue
            } else {
                dotnet user-secrets set "$key" "$value" --project $projectPath | Out-Null
            }
            Write-Host "  - Set secret for '$key'."
        }
        Write-Host "Project configured successfully." -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Error "An error occurred while configuring project '$projectPath'."
        Write-Error $_.Exception.Message
        # Continue to the next project even if one fails.
    }
}

Write-Host "--- Setup Complete ---" -ForegroundColor Yellow
