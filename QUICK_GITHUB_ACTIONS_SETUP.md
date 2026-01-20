# Quick GitHub Actions Setup - 5 Minutes

Railway cache issue causing production to serve old controllers? This workflow fixes it permanently.

## What You Get

✅ AuthController appears in production
✅ BranchController appears in production
✅ DailySummaryController removed from production
✅ Fresh Docker build every deployment (no cache issues)
✅ Automatic verification before deployment

## Setup (3 Steps)

### Step 1: Get Railway Information (2 minutes)

**Windows**:
```powershell
.\get-railway-info.ps1
```

**Linux/Mac**:
```bash
chmod +x get-railway-info.sh
./get-railway-info.sh
```

This script outputs three values you'll need for GitHub secrets.

### Step 2: Add GitHub Secrets (2 minutes)

1. Go to: `https://github.com/YOUR_USERNAME/ConvoyV2/settings/secrets/actions`
2. Click **New repository secret** three times and add:

| Secret Name | Value | Where to Get |
|-------------|-------|--------------|
| `RAILWAY_TOKEN` | Token from script | `railway whoami --token` |
| `RAILWAY_SERVICE_ID` | Service ID from script | Script output or Railway dashboard URL |
| `RAILWAY_DEPLOYMENT_URL` | Production URL | `https://convoy-production-2969.up.railway.app` |

### Step 3: Push Workflow (1 minute)

```bash
# Add workflow file
git add .github/workflows/railway-deploy.yml
git add GITHUB_ACTIONS_DEPLOYMENT.md
git add QUICK_GITHUB_ACTIONS_SETUP.md
git add get-railway-info.ps1
git add get-railway-info.sh

# Commit
git commit -m "ci: add GitHub Actions deployment workflow to fix Railway cache"

# Push and deploy
git push origin main
```

**Done!** GitHub Actions will automatically build and deploy.

## Verify Deployment

### 1. Check GitHub Actions (30 seconds)

Go to: `https://github.com/YOUR_USERNAME/ConvoyV2/actions`

You should see "Build and Deploy to Railway" running. Click it to watch live logs.

**Look for**:
```
✅ IMAGE VERIFICATION PASSED
All required controllers present, no obsolete controllers
```

### 2. Check Production Swagger (30 seconds)

Go to: `https://convoy-production-2969.up.railway.app/swagger`

**You should see**:
- ✅ Auth
- ✅ Branch
- ✅ Location
- ✅ User

**Should NOT see**:
- ❌ DailySummary (old controller from cache)

### 3. Test API Endpoint (30 seconds)

```bash
curl -X POST https://convoy-production-2969.up.railway.app/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'
```

Should return 200 OK (not 404).

## What This Workflow Does

```
Push to main → GitHub Actions starts
  ↓
1. Checkout code
  ↓
2. Build Docker image (fresh Ubuntu VM, no cache)
  ↓
3. Verify controllers in built DLL
   - Check AuthController exists ✅
   - Check BranchController exists ✅
   - Check DailySummaryController NOT exists ✅
   - FAIL build if verification fails ❌
  ↓
4. Push verified image to Railway registry
  ↓
5. Deploy to Railway
  ↓
6. Verify production Swagger
  ↓
7. Report success or failure
```

## Troubleshooting

### "RAILWAY_TOKEN not found"
- Go to GitHub repo → Settings → Secrets → Actions
- Verify secret is named exactly `RAILWAY_TOKEN` (case-sensitive)
- Re-run script to get token: `railway whoami --token`

### "railway: command not found"
```bash
# Install Railway CLI
curl -fsSL https://railway.app/install.sh | sh

# Login
railway login
```

### Workflow fails at "Login to Railway Registry"
- Token expired, regenerate:
```bash
railway logout
railway login
railway whoami --token  # Copy new token
```
- Update `RAILWAY_TOKEN` secret in GitHub

### Production still shows DailySummaryController
- Check GitHub Actions logs - did verification step pass?
- Railway may have multiple deployments, delete old ones
- Force restart: Railway Dashboard → Settings → Restart

## Future Deployments

Just push to main:
```bash
git add .
git commit -m "your changes"
git push origin main
```

GitHub Actions will automatically:
1. Build fresh Docker image
2. Verify controllers
3. Deploy to Railway
4. Confirm production is correct

## Compare: Old vs New

| Problem | Railway Native | GitHub Actions |
|---------|----------------|----------------|
| Cache issues | ❌ Persistent | ✅ Fresh every time |
| Missing controllers | ❌ AuthController, BranchController not appearing | ✅ All controllers verified |
| Old controllers | ❌ DailySummaryController still showing | ✅ Removed, fails build if detected |
| Verification | ❌ None | ✅ DLL checked before deployment |
| Debugging | ⚠️ Limited logs | ✅ Full GitHub Actions logs |

## Cost

- **GitHub Actions**: Free for public repos, 2000 minutes/month for private
- **This workflow**: ~3-5 minutes per deployment
- **Monthly**: ~60-100 deployments with free tier

## Support

See full documentation: `GITHUB_ACTIONS_DEPLOYMENT.md`

Questions? Check the troubleshooting section in the full guide.

---

**Status**: Ready to use
**Time to setup**: 5 minutes
**Fixes**: Railway cache issue permanently
