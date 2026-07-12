<#
.SYNOPSIS
    One-time hardening script for a new GitHub repo, using the gh CLI.

.DESCRIPTION
    Run this once after `git push`-ing this repo to GitHub, from a user with
    admin rights on the repo. It configures what's controllable at the repo
    level: branch protection, required PR review count, required signed
    commits, and Dependabot alerts/security updates.

    It does NOT and CANNOT configure SSO enforcement or audit log retention --
    those are GitHub Enterprise org/enterprise-owner settings made in the
    browser. Manual steps are printed at the end.

.PARAMETER Repo
    "owner/repo" slug, e.g. "myorg/Sm_API". Defaults to the repo the gh CLI
    is currently authenticated against in this directory.

.PARAMETER RequiredReviewers
    Minimum number of required PR approvals. Defaults to 1.

.EXAMPLE
    ./scripts/setup-github-security.ps1 -Repo "myorg/Sm_API"
#>
param(
    [string]$Repo,
    [int]$RequiredReviewers = 1
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI ('gh') is not installed or not on PATH. Install from https://cli.github.com/ and run 'gh auth login' first."
    exit 1
}

if (-not $Repo) {
    $Repo = (gh repo view --json nameWithOwner -q .nameWithOwner)
    if (-not $Repo) {
        Write-Error "Could not determine repo. Pass -Repo 'owner/repo' explicitly."
        exit 1
    }
}

Write-Host "Configuring security hardening for $Repo" -ForegroundColor Cyan

# --- Branch protection on main: required reviews, required status checks, enforce for admins ---
Write-Host "`n[1/4] Enabling branch protection on main..." -ForegroundColor Yellow

$protectionBody = @{
    required_status_checks        = @{
        strict   = $true
        contexts = @("build-and-test", "diff-scan")
    }
    enforce_admins                = $true
    required_pull_request_reviews  = @{
        required_approving_review_count = $RequiredReviewers
        dismiss_stale_reviews            = $true
    }
    restrictions                  = $null
    required_linear_history       = $true
    allow_force_pushes            = $false
    allow_deletions                = $false
} | ConvertTo-Json -Depth 5

$protectionBody | gh api `
    --method PUT `
    -H "Accept: application/vnd.github+json" `
    "/repos/$Repo/branches/main/protection" `
    --input -

# --- Required signed commits ---
Write-Host "`n[2/4] Requiring signed commits on main..." -ForegroundColor Yellow
gh api --method POST -H "Accept: application/vnd.github+json" "/repos/$Repo/branches/main/protection/required_signatures" | Out-Null

# --- Dependabot vulnerability alerts + automated security fixes ---
Write-Host "`n[3/4] Enabling Dependabot alerts + automated security fixes..." -ForegroundColor Yellow
gh api --method PUT -H "Accept: application/vnd.github+json" "/repos/$Repo/vulnerability-alerts" | Out-Null
gh api --method PUT -H "Accept: application/vnd.github+json" "/repos/$Repo/automated-security-fixes" | Out-Null

# --- Secret scanning + push protection (only takes effect on GH Advanced Security / public repos) ---
Write-Host "`n[4/4] Attempting to enable native secret scanning (requires GH Advanced Security or a public repo)..." -ForegroundColor Yellow
try {
    $secBody = @{ security_and_analysis = @{ secret_scanning = @{ status = "enabled" }; secret_scanning_push_protection = @{ status = "enabled" } } } | ConvertTo-Json -Depth 5
    $secBody | gh api --method PATCH -H "Accept: application/vnd.github+json" "/repos/$Repo" --input - | Out-Null
    Write-Host "Native secret scanning request sent (verify it actually took effect in repo Settings -> Code security)." -ForegroundColor Green
} catch {
    Write-Host "Could not enable native secret scanning via API -- this repo/plan likely doesn't support GH Advanced Security. gitleaks in Actions is already covering this." -ForegroundColor DarkYellow
}

Write-Host "`nDone with what's automatable from here." -ForegroundColor Cyan
Write-Host @"

MANUAL STEPS -- these are org/enterprise-owner actions, not repo settings:

  * SSO enforcement (requires GitHub Enterprise + SAML/OIDC IdP configured):
      Org Settings -> Authentication security -> "Require SAML SSO"
      https://docs.github.com/en/enterprise-cloud@latest/organizations/managing-saml-single-sign-on-for-your-organization/enforcing-saml-single-sign-on-for-your-organization

  * Audit log retention (GitHub Enterprise Cloud only):
      Enterprise Settings -> Audit log -> configure retention / streaming to external storage
      https://docs.github.com/en/enterprise-cloud@latest/admin/monitoring-activity-in-your-enterprise/reviewing-audit-logs-for-your-enterprise/about-the-audit-log-for-your-enterprise

Run scripts/security-compliance-audit workflow (or `gh workflow run security-compliance-audit.yml`) to verify what was actually applied.
"@ -ForegroundColor White
