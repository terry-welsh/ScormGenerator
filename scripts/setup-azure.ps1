#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-time setup script to configure Azure OIDC authentication for GitHub Actions.
    Safe to re-run — all steps are idempotent.

.EXAMPLE
    .\scripts\setup-azure.ps1
    .\scripts\setup-azure.ps1 -AppName my-app -ResourceGroup my-rg -Location westus
#>

param(
    [string]$AppName = "scorm-generator",
    [string]$ResourceGroup = "scorm-generator-rg",
    [string]$Location = "eastus"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) { Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "   $msg" -ForegroundColor Green }
function Write-Skip([string]$msg) { Write-Host "   $msg (already exists, skipping)" -ForegroundColor Yellow }

# ---------------------------------------------------------------------------
# Prerequisites
# ---------------------------------------------------------------------------
Write-Step "Checking prerequisites"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI not found. Install from https://aka.ms/installazurecli"
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI not found. Install from https://cli.github.com"
}

$null = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "   Not logged in to Azure — opening browser..."
    az login
}

$null = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "   Not logged in to GitHub — opening browser..."
    gh auth login
}

Write-Ok "Prerequisites satisfied"

# ---------------------------------------------------------------------------
# Gather context
# ---------------------------------------------------------------------------
Write-Step "Gathering context"

$subscriptionId = az account show --query id -o tsv
$tenantId       = az account show --query tenantId -o tsv

$gitRemote = git remote get-url origin 2>&1
if ($LASTEXITCODE -ne 0) { Write-Error "No git remote found — run this from inside the repo." }

if ($gitRemote -match "github\.com[:/](.+?)(?:\.git)?$") {
    $githubRepo = $Matches[1]
} else {
    Write-Error "Could not parse GitHub repo from remote URL: $gitRemote"
}

Write-Ok "Subscription : $subscriptionId"
Write-Ok "Tenant       : $tenantId"
Write-Ok "GitHub repo  : $githubRepo"

# ---------------------------------------------------------------------------
# Resource group
# ---------------------------------------------------------------------------
Write-Step "Resource group '$ResourceGroup'"

$rgExists = az group exists --name $ResourceGroup
if ($rgExists -eq "true") {
    Write-Skip $ResourceGroup
} else {
    az group create --name $ResourceGroup --location $Location | Out-Null
    Write-Ok "Created"
}

# ---------------------------------------------------------------------------
# App Registration
# ---------------------------------------------------------------------------
Write-Step "App Registration '$AppName-deploy'"

$clientId = az ad app list --display-name "$AppName-deploy" --query "[0].appId" -o tsv 2>$null
if ($clientId) {
    Write-Skip $clientId
} else {
    $clientId = az ad app create --display-name "$AppName-deploy" --query appId -o tsv
    Write-Ok "Created: $clientId"
}

# ---------------------------------------------------------------------------
# Service Principal
# ---------------------------------------------------------------------------
Write-Step "Service Principal"

$null = az ad sp show --id $clientId 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Skip $clientId
} else {
    az ad sp create --id $clientId | Out-Null
    Write-Ok "Created"
    # Propagation delay — role assignment can fail if SP isn't visible yet
    Write-Host "   Waiting for propagation..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
}

# ---------------------------------------------------------------------------
# Federated Credential
# ---------------------------------------------------------------------------
Write-Step "Federated credential (main branch)"

$credName = "$AppName-main"
$existing = az ad app federated-credential list --id $clientId --query "[?name=='$credName'].id" -o tsv 2>$null
if ($existing) {
    Write-Skip $credName
} else {
    $credJson = @{
        name      = $credName
        issuer    = "https://token.actions.githubusercontent.com"
        subject   = "repo:${githubRepo}:ref:refs/heads/main"
        audiences = @("api://AzureADTokenExchange")
    } | ConvertTo-Json -Compress

    # Write to a temp file to avoid shell escaping issues on Windows
    $tmpFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tmpFile -Value $credJson
    az ad app federated-credential create --id $clientId --parameters "@$tmpFile" | Out-Null
    Remove-Item $tmpFile
    Write-Ok "Created for repo:${githubRepo}:ref:refs/heads/main"
}

# ---------------------------------------------------------------------------
# Role assignment
# ---------------------------------------------------------------------------
Write-Step "Contributor role on resource group"

$scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup"
$existing = az role assignment list --assignee $clientId --role Contributor --scope $scope --query "[0].id" -o tsv 2>$null
if ($existing) {
    Write-Skip "Contributor on $ResourceGroup"
} else {
    az role assignment create --assignee $clientId --role Contributor --scope $scope | Out-Null
    Write-Ok "Assigned"
}

# ---------------------------------------------------------------------------
# Resource provider registration
# ---------------------------------------------------------------------------
Write-Step "Registering Microsoft.App resource provider"

$providerState = az provider show --namespace Microsoft.App --query registrationState -o tsv 2>$null
if ($providerState -eq "Registered") {
    Write-Skip "Microsoft.App"
} else {
    az provider register --namespace Microsoft.App --wait | Out-Null
    Write-Ok "Registered"
}

# ---------------------------------------------------------------------------
# GitHub Actions variables
# ---------------------------------------------------------------------------
Write-Step "GitHub Actions variables"

gh variable set AZURE_CLIENT_ID       --repo $githubRepo --body $clientId
gh variable set AZURE_TENANT_ID       --repo $githubRepo --body $tenantId
gh variable set AZURE_SUBSCRIPTION_ID --repo $githubRepo --body $subscriptionId
gh variable set AZURE_RESOURCE_GROUP  --repo $githubRepo --body $ResourceGroup
gh variable set AZURE_APP_NAME        --repo $githubRepo --body $AppName
gh variable set AZURE_LOCATION        --repo $githubRepo --body $Location

Write-Ok "AZURE_CLIENT_ID       = $clientId"
Write-Ok "AZURE_TENANT_ID       = $tenantId"
Write-Ok "AZURE_SUBSCRIPTION_ID = $subscriptionId"
Write-Ok "AZURE_RESOURCE_GROUP  = $ResourceGroup"
Write-Ok "AZURE_APP_NAME        = $AppName"
Write-Ok "AZURE_LOCATION        = $Location"

# ---------------------------------------------------------------------------
# GHCR pull token (Container Apps runtime credential)
# ---------------------------------------------------------------------------
Write-Step "GitHub secret: GHCR_TOKEN"

$existingSecret = gh secret list --repo $githubRepo --json name --jq '.[] | select(.name=="GHCR_TOKEN") | .name' 2>$null
if ($existingSecret) {
    Write-Skip "GHCR_TOKEN"
} else {
    Write-Host "   Container Apps needs a PAT with 'read:packages' scope to pull images at runtime." -ForegroundColor Yellow
    Write-Host "   Create one at: https://github.com/settings/tokens/new?scopes=read:packages" -ForegroundColor Yellow
    $pat = Read-Host "   Paste PAT"
    gh secret set GHCR_TOKEN --repo $githubRepo --body $pat
    Write-Ok "GHCR_TOKEN set"
}

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Setup complete." -ForegroundColor Green
Write-Host "Push to main to trigger your first deployment."
Write-Host "The app URL will appear at the end of the deploy workflow log."
