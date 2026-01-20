# Manual Railway Information Collection

Railway CLI requires login. Get these values manually from Railway Dashboard.

## Step 1: Get RAILWAY_TOKEN

### Option A: Via Railway CLI (if logged in)
```bash
railway login
railway whoami --token
```

### Option B: Via Railway Dashboard
1. Go to: https://railway.app/account/tokens
2. Click **Create Token**
3. Name: `GitHub Actions Deploy`
4. Click **Create**
5. Copy the token (starts with `eyJhbGc...`)

**Save as GitHub secret**: `RAILWAY_TOKEN`

---

## Step 2: Get RAILWAY_SERVICE_ID

1. Go to your Railway project: https://railway.app/project/YOUR_PROJECT
2. Click on your **Convoy API service**
3. Look at the URL in browser address bar:
   ```
   https://railway.app/project/abc123def456/service/xyz789ghi012
                                                     ^^^^^^^^^^^^
   ```
4. Copy the SERVICE_ID part after `/service/`

**Save as GitHub secret**: `RAILWAY_SERVICE_ID`

---

## Step 3: Get RAILWAY_DEPLOYMENT_URL

1. In Railway Dashboard, open your service
2. Click **Settings** tab
3. Scroll to **Domains** section
4. Copy the public URL (e.g., `https://convoy-production-2969.up.railway.app`)

**Alternative**: If you already know your production URL, use that.

**Save as GitHub secret**: `RAILWAY_DEPLOYMENT_URL`

---

## Add Secrets to GitHub

1. Go to: `https://github.com/YOUR_USERNAME/ConvoyV2/settings/secrets/actions`
2. Click **New repository secret** three times:

### Secret 1
- **Name**: `RAILWAY_TOKEN`
- **Value**: Token from Step 1

### Secret 2
- **Name**: `RAILWAY_SERVICE_ID`
- **Value**: Service ID from Step 2

### Secret 3
- **Name**: `RAILWAY_DEPLOYMENT_URL`
- **Value**: Deployment URL from Step 3

---

## Push Workflow

```bash
git push origin main
```

GitHub Actions will automatically start deploying.

---

## Verify

Go to: `https://github.com/YOUR_USERNAME/ConvoyV2/actions`

You should see "Build and Deploy to Railway" running.
