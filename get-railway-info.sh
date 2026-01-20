#!/bin/bash
# Bash script to get Railway information for GitHub Actions setup
# Run this script to collect all information needed for GitHub secrets

echo "════════════════════════════════════════════════"
echo "Railway Information Collection for GitHub Actions"
echo "════════════════════════════════════════════════"
echo ""

# Check if Railway CLI is installed
if ! command -v railway &> /dev/null; then
    echo "❌ Railway CLI not found!"
    echo ""
    echo "Install Railway CLI:"
    echo "  curl -fsSL https://railway.app/install.sh | sh"
    echo ""
    exit 1
fi

echo "✅ Railway CLI found: $(which railway)"
echo ""

# Get Railway token
echo "Step 1: Railway Token"
echo "────────────────────────────────────────────────"

TOKEN=$(railway whoami --token 2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Token retrieved successfully"
    echo ""
    echo "RAILWAY_TOKEN (copy this to GitHub secret):"
    echo "$TOKEN"
    echo ""
else
    echo "❌ Failed to get Railway token"
    echo "Run 'railway login' first, then try again"
    echo ""
fi

# Get service status
echo "Step 2: Service Information"
echo "────────────────────────────────────────────────"

STATUS=$(railway status 2>&1)

if echo "$STATUS" | grep -q "Service:"; then
    SERVICE_ID=$(echo "$STATUS" | grep "Service:" | awk '{print $2}')
    echo "✅ Service ID found"
    echo ""
    echo "RAILWAY_SERVICE_ID (copy this to GitHub secret):"
    echo "$SERVICE_ID"
    echo ""
else
    echo "⚠️ Could not parse service ID from status"
    echo "Raw output:"
    echo "$STATUS"
    echo ""
    echo "Get Service ID manually from Railway dashboard URL:"
    echo "https://railway.app/project/PROJECT_ID/service/SERVICE_ID"
    echo "                                            ^^^^^^^^^^"
    echo ""
fi

# Get deployment URL
echo "Step 3: Deployment URL"
echo "────────────────────────────────────────────────"

DOMAIN=$(railway domain 2>&1)

if echo "$DOMAIN" | grep -qE "https?://"; then
    DEPLOYMENT_URL=$(echo "$DOMAIN" | grep -oE "https?://[^\s]+")
    echo "✅ Deployment URL found"
    echo ""
    echo "RAILWAY_DEPLOYMENT_URL (copy this to GitHub secret):"
    echo "$DEPLOYMENT_URL"
    echo ""
else
    echo "⚠️ Could not find deployment URL"
    echo "Raw output:"
    echo "$DOMAIN"
    echo ""
    echo "Get deployment URL from Railway dashboard:"
    echo "Settings → Domains → Copy the public URL"
    echo ""
fi

# Summary
echo "════════════════════════════════════════════════"
echo "Next Steps"
echo "════════════════════════════════════════════════"
echo ""
echo "1. Go to GitHub repository Settings → Secrets and variables → Actions"
echo "2. Click 'New repository secret' and add these three secrets:"
echo ""
echo "   RAILWAY_TOKEN"
echo "   RAILWAY_SERVICE_ID"
echo "   RAILWAY_DEPLOYMENT_URL"
echo ""
echo "3. Copy the values from above"
echo ""
echo "4. Commit and push the GitHub Actions workflow:"
echo "   git add .github/workflows/railway-deploy.yml"
echo "   git commit -m 'ci: add GitHub Actions deployment'"
echo "   git push origin main"
echo ""
echo "5. Check GitHub Actions tab to monitor deployment"
echo ""
echo "See GITHUB_ACTIONS_DEPLOYMENT.md for complete instructions"
echo ""
