# PowerShell script to get Railway information for GitHub Actions setup
# Run this script to collect all information needed for GitHub secrets

Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Railway Information Collection for GitHub Actions" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if Railway CLI is installed
$railwayPath = Get-Command railway -ErrorAction SilentlyContinue

if (-not $railwayPath) {
    Write-Host "❌ Railway CLI not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install Railway CLI:" -ForegroundColor Yellow
    Write-Host "  Windows: iwr https://railway.app/install.ps1 | iex" -ForegroundColor White
    Write-Host "  Or download from: https://railway.app/cli" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "✅ Railway CLI found: $($railwayPath.Source)" -ForegroundColor Green
Write-Host ""

# Get Railway token
Write-Host "Step 1: Railway Token" -ForegroundColor Yellow
Write-Host "────────────────────────────────────────────────" -ForegroundColor DarkGray

try {
    $token = railway whoami --token 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to get token"
    }

    Write-Host "✅ Token retrieved successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "RAILWAY_TOKEN (copy this to GitHub secret):" -ForegroundColor Cyan
    Write-Host $token.Trim() -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "❌ Failed to get Railway token" -ForegroundColor Red
    Write-Host "Run 'railway login' first, then try again" -ForegroundColor Yellow
    Write-Host ""
}

# Get service status
Write-Host "Step 2: Service Information" -ForegroundColor Yellow
Write-Host "────────────────────────────────────────────────" -ForegroundColor DarkGray

try {
    $status = railway status 2>&1 | Out-String

    if ($status -match "Service:\s+([a-f0-9\-]+)") {
        $serviceId = $matches[1]
        Write-Host "✅ Service ID found" -ForegroundColor Green
        Write-Host ""
        Write-Host "RAILWAY_SERVICE_ID (copy this to GitHub secret):" -ForegroundColor Cyan
        Write-Host $serviceId -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "⚠️ Could not parse service ID from status" -ForegroundColor Yellow
        Write-Host "Raw output:" -ForegroundColor DarkGray
        Write-Host $status
        Write-Host ""
        Write-Host "Get Service ID manually from Railway dashboard URL:" -ForegroundColor Yellow
        Write-Host "https://railway.app/project/PROJECT_ID/service/SERVICE_ID" -ForegroundColor White
        Write-Host "                                            ^^^^^^^^^^" -ForegroundColor Cyan
        Write-Host ""
    }
} catch {
    Write-Host "❌ Failed to get service status" -ForegroundColor Red
    Write-Host "Make sure you're in the project directory and linked to Railway" -ForegroundColor Yellow
    Write-Host ""
}

# Get deployment URL
Write-Host "Step 3: Deployment URL" -ForegroundColor Yellow
Write-Host "────────────────────────────────────────────────" -ForegroundColor DarkGray

try {
    $domain = railway domain 2>&1 | Out-String

    if ($domain -match "(https?://[^\s]+)") {
        $deploymentUrl = $matches[1].Trim()
        Write-Host "✅ Deployment URL found" -ForegroundColor Green
        Write-Host ""
        Write-Host "RAILWAY_DEPLOYMENT_URL (copy this to GitHub secret):" -ForegroundColor Cyan
        Write-Host $deploymentUrl -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "⚠️ Could not find deployment URL" -ForegroundColor Yellow
        Write-Host "Raw output:" -ForegroundColor DarkGray
        Write-Host $domain
        Write-Host ""
        Write-Host "Get deployment URL from Railway dashboard:" -ForegroundColor Yellow
        Write-Host "Settings → Domains → Copy the public URL" -ForegroundColor White
        Write-Host ""
    }
} catch {
    Write-Host "❌ Failed to get deployment URL" -ForegroundColor Red
    Write-Host "Get it from Railway dashboard: Settings → Domains" -ForegroundColor Yellow
    Write-Host ""
}

# Summary
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Next Steps" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Go to GitHub repository Settings → Secrets and variables → Actions" -ForegroundColor White
Write-Host "2. Click 'New repository secret' and add these three secrets:" -ForegroundColor White
Write-Host ""
Write-Host "   RAILWAY_TOKEN" -ForegroundColor Yellow
Write-Host "   RAILWAY_SERVICE_ID" -ForegroundColor Yellow
Write-Host "   RAILWAY_DEPLOYMENT_URL" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Copy the values from above (highlighted in white)" -ForegroundColor White
Write-Host ""
Write-Host "4. Commit and push the GitHub Actions workflow:" -ForegroundColor White
Write-Host "   git add .github/workflows/railway-deploy.yml" -ForegroundColor DarkGray
Write-Host "   git commit -m 'ci: add GitHub Actions deployment'" -ForegroundColor DarkGray
Write-Host "   git push origin main" -ForegroundColor DarkGray
Write-Host ""
Write-Host "5. Check GitHub Actions tab to monitor deployment" -ForegroundColor White
Write-Host ""
Write-Host "See GITHUB_ACTIONS_DEPLOYMENT.md for complete instructions" -ForegroundColor Cyan
Write-Host ""
