# GitHub Actions Deployment Setup for Railway

## Problem This Solves

Railway's Docker builder has persistent cache that survives:
- Multiple Dockerfile changes
- Service deletion and recreation
- .railway-trigger file modifications
- ARG-based cache invalidation

This results in production serving **stale Docker images** with old controllers (DailySummaryController) and missing new controllers (AuthController, BranchController).

## Solution: GitHub Actions Workflow

Build Docker image on GitHub Actions (fresh environment every time) and push verified image directly to Railway's registry. This **bypasses Railway's builder entirely**.

## Setup Instructions

### Step 1: Get Railway Service Information

1. **Get Railway Token**:
   ```bash
   # Install Railway CLI (if not already installed)
   curl -fsSL https://railway.app/install.sh | sh

   # Login
   railway login

   # Get token (save this for GitHub secrets)
   railway whoami --token
   ```

2. **Get Service ID**:
   ```bash
   # In your project directory
   railway status

   # Or get it from Railway dashboard URL:
   # https://railway.app/project/PROJECT_ID/service/SERVICE_ID
   ```

3. **Get Deployment URL**:
   ```bash
   railway domain

   # Or from Railway dashboard under "Settings" → "Domains"
   # Example: https://convoy-production-2969.up.railway.app
   ```

### Step 2: Add GitHub Secrets

1. Go to your GitHub repository: `https://github.com/YOUR_USERNAME/ConvoyV2`
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** and add these three secrets:

| Secret Name | Value | Example |
|-------------|-------|---------|
| `RAILWAY_TOKEN` | Token from `railway whoami --token` | `eyJhbGciOiJSUzI1...` |
| `RAILWAY_SERVICE_ID` | Service ID from Railway dashboard | `abc123-def456-ghi789` |
| `RAILWAY_DEPLOYMENT_URL` | Your Railway deployment URL | `https://convoy-production-2969.up.railway.app` |

**CRITICAL**: These must be **exact** names as shown above.

### Step 3: Configure Railway Service Settings

Since we're bypassing Railway's builder, update Railway configuration:

1. **Railway Dashboard** → Your Service → **Settings** → **Build**
2. Find **Builder** section
3. Change **Build Command** to:
   ```
   echo "Built by GitHub Actions - no build needed"
   ```
4. Change **Dockerfile Path** to:
   ```
   Dockerfile
   ```
5. **Important**: Disable Railway's automatic deployments on push:
   - **Settings** → **Triggers**
   - Uncheck **Deploy on push to main branch**
   - (GitHub Actions will handle deployments now)

### Step 4: Commit and Push Workflow

```bash
# Add the workflow file
git add .github/workflows/railway-deploy.yml

# Commit
git commit -m "ci: add GitHub Actions workflow for Railway deployment"

# Push to trigger first deployment
git push origin main
```

### Step 5: Monitor First Deployment

1. Go to your GitHub repository
2. Click **Actions** tab
3. You should see "Build and Deploy to Railway" workflow running
4. Click on the workflow run to see live logs

**Expected workflow steps**:
```
1. ✅ Checkout code
2. ✅ Set up Docker Buildx
3. ✅ Generate Build ID
4. ✅ Build Docker Image
5. ✅ Verify Built Image (Critical Check)
   - Should show: AuthController, BranchController, LocationController, UserController
   - Should NOT show: DailySummaryController
6. ✅ Login to Railway Registry
7. ✅ Tag and Push to Railway
8. ✅ Install Railway CLI
9. ✅ Deploy to Railway
10. ✅ Wait for Deployment
11. ✅ Verify Production Deployment
```

## Verification After Deployment

### 1. Check GitHub Actions Logs

Look for these confirmations in the workflow logs:

**Image Verification Step**:
```
🔍 VERIFYING DOCKER IMAGE CONTENTS
════════════════════════════════════════════════
DLL Size:
-rw-r--r-- 1 root root 84K Jan 20 17:30 /app/Convoy.Api.dll

Searching for controller classes...
AuthController
BranchController
LocationController
UserController

✅ IMAGE VERIFICATION PASSED
All required controllers present, no obsolete controllers
```

**Production Verification Step**:
```
🔍 VERIFYING PRODUCTION DEPLOYMENT
════════════════════════════════════════════════
Controllers found in production Swagger:
auth
branches
locations
users

✅ AuthController found
✅ BranchController found
```

### 2. Check Production Swagger

Visit: `https://convoy-production-2969.up.railway.app/swagger`

**Expected Controllers**:
- ✅ `Auth` - `/api/auth/*`
- ✅ `Branch` - `/api/branches/*`
- ✅ `Location` - `/api/locations/*`
- ✅ `User` - `/api/users/*`

**Must NOT appear**:
- ❌ `DailySummary` (deleted controller from old cache)

### 3. Test Endpoints Directly

```bash
# Test AuthController
curl -X POST https://convoy-production-2969.up.railway.app/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'

# Should return 200 OK with user verification response

# Test BranchController
curl https://convoy-production-2969.up.railway.app/api/branches

# Should return 200 OK with branches list (or 401 if auth required)
```

## How This Workflow Solves the Problem

### Railway's Cache Issue (Old Approach)
```
Git Push → Railway Builder (CACHED) → Railway Registry → Deploy
                ↑
         Problem: Uses old cached layers
         Even service deletion doesn't clear this cache
```

### GitHub Actions Approach (New)
```
Git Push → GitHub Actions (FRESH VM) → Build Image → Verify Controllers → Railway Registry → Deploy
                ↑                              ↑
         Always fresh build          Fails if old cache detected
```

**Key Advantages**:
1. **Fresh environment**: GitHub Actions spins up new Ubuntu VM for every build
2. **No cache pollution**: VM is destroyed after build, impossible to have stale cache
3. **Verification before push**: Workflow checks for correct controllers before pushing to Railway
4. **Build fails early**: If DailySummaryController detected, build fails before deployment
5. **Full control**: We control every step of the build and deployment process

## Workflow Triggers

The workflow runs on:

1. **Push to main branch**: Automatic deployment when you push changes
2. **Manual trigger**: Go to **Actions** tab → **Build and Deploy to Railway** → **Run workflow**

## Troubleshooting

### Issue 1: "RAILWAY_TOKEN secret not found"

**Cause**: Secret not configured or named incorrectly.

**Solution**:
1. Go to GitHub repository → **Settings** → **Secrets and variables** → **Actions**
2. Verify secret is named exactly `RAILWAY_TOKEN` (case-sensitive)
3. If missing, add it using token from `railway whoami --token`

### Issue 2: "Login to Railway Registry failed"

**Cause**: Invalid Railway token or expired session.

**Solution**:
1. Regenerate Railway token:
   ```bash
   railway logout
   railway login
   railway whoami --token
   ```
2. Update `RAILWAY_TOKEN` secret in GitHub with new token

### Issue 3: "DailySummaryController found in built image!"

**Cause**: Docker Buildx cache on GitHub Actions has stale layers (very rare).

**Solution**:
1. Go to repository **Settings** → **Actions** → **General**
2. Scroll to **Actions permissions**
3. Click **Clear caches** (removes all action caches)
4. Re-run workflow

### Issue 4: Production Still Shows DailySummaryController

**Cause**: Railway may have multiple services or old containers still running.

**Solution**:
1. **Railway Dashboard** → Your Service → **Deployments**
2. Check which deployment is active
3. If multiple, delete old deployments
4. Force restart: **Settings** → **Restart**

### Issue 5: "railway: command not found" in Deploy Step

**Cause**: Railway CLI installation failed or path not set.

**Solution**: Workflow includes Railway CLI installation step. If it fails:
1. Check GitHub Actions logs for installation errors
2. Verify Railway installation script is accessible: https://railway.app/install.sh
3. If Railway changed their CLI distribution, update the installation command

### Issue 6: Workflow Succeeds but Production Not Updated

**Cause**: Railway may be configured to ignore registry pushes.

**Solution**:
1. **Railway Dashboard** → Your Service → **Settings** → **Deploy**
2. Verify **Deployment Source** is set to **Docker Image**
3. Set **Image Repository** to `registry.railway.app/YOUR_SERVICE_ID`
4. Trigger workflow again

## Advanced Configuration

### Custom Branch Deployment

To deploy from a different branch (e.g., `develop`):

```yaml
on:
  push:
    branches:
      - main
      - develop  # Add this
```

### Manual Deployment with Input

To allow manual deployment with custom parameters:

```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Deployment environment'
        required: true
        default: 'production'
        type: choice
        options:
          - production
          - staging
```

### Slack/Discord Notifications

Add notification step at end of workflow:

```yaml
- name: Notify Deployment
  if: always()
  run: |
    STATUS="${{ job.status }}"
    if [ "$STATUS" == "success" ]; then
      MESSAGE="✅ Convoy API deployed successfully"
    else
      MESSAGE="❌ Convoy API deployment failed"
    fi

    # Send to Slack webhook
    curl -X POST ${{ secrets.SLACK_WEBHOOK_URL }} \
      -H 'Content-Type: application/json' \
      -d "{\"text\":\"$MESSAGE\"}"
```

## Comparison with Railway Native Deployment

| Feature | Railway Builder | GitHub Actions |
|---------|----------------|----------------|
| **Cache Control** | ❌ Persistent cache, hard to clear | ✅ Fresh VM every build |
| **Verification** | ❌ No pre-deployment checks | ✅ DLL verification before push |
| **Build Time** | ✅ Fast (cached layers) | ⚠️ Slower (fresh build) |
| **Reliability** | ❌ Cache pollution issues | ✅ Consistent results |
| **Debugging** | ⚠️ Limited build logs | ✅ Full GitHub Actions logs |
| **Control** | ⚠️ Limited customization | ✅ Full workflow control |

## Cost Considerations

- **GitHub Actions**: Free for public repositories, 2000 minutes/month for private repos
- **Railway**: No additional cost (same as before)
- **Build time**: ~3-5 minutes per deployment (fresh Docker build)

## Migration Back to Railway Native (If Needed)

If in the future Railway fixes their cache issue and you want to revert:

1. **Re-enable Railway automatic deployments**:
   - Railway Dashboard → **Settings** → **Triggers**
   - Check **Deploy on push to main branch**

2. **Disable GitHub Actions workflow**:
   ```bash
   # Rename workflow to disable it
   git mv .github/workflows/railway-deploy.yml .github/workflows/railway-deploy.yml.disabled
   git commit -m "ci: disable GitHub Actions deployment"
   git push
   ```

3. **Verify Railway builds correctly**:
   - Make a small change
   - Push to main
   - Check Railway build logs for correct controllers

## Summary

This GitHub Actions workflow provides:
- ✅ **Fresh builds** every time (no cache pollution)
- ✅ **Verification** before deployment (fails if old controllers detected)
- ✅ **Full control** over build and deployment process
- ✅ **Reliable** deployments (same result every time)
- ✅ **Visible** build logs and error messages

**Next Steps**:
1. Follow setup instructions above
2. Configure GitHub secrets
3. Push workflow to trigger first deployment
4. Verify production Swagger shows correct controllers

---

**Created**: 2026-01-20
**Author**: Claude Code
**Status**: RECOMMENDED SOLUTION for Railway cache issues
