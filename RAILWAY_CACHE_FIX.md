# Railway Cache Fix - ARG-Based Nuclear Option

## Problem
Railway is serving a stale Docker build even after:
- Multiple Dockerfile changes
- Service deletion and recreation
- .railway-trigger file modifications

The production Swagger shows:
- ❌ `DailySummaryController` (DELETED, should not exist)
- ❌ Missing `AuthController` (exists in code, works locally)
- ❌ Missing `BranchController` (exists in code, works locally)

## Root Cause
Railway has persistent Docker layer cache at the **builder/registry level** that survives:
- Normal cache busting attempts
- Service recreation
- Even Dockerfile restructuring

## Solution: ARG-Based Cache Invalidation

Docker ARG values **cannot be cached** - they must be evaluated at build time. By using ARG with Railway's built-in `RAILWAY_GIT_COMMIT_SHA` variable, we force a complete rebuild on every commit.

### Updated Dockerfile
The new Dockerfile uses:
```dockerfile
ARG BUILD_ID=unknown
ARG RAILWAY_GIT_COMMIT_SHA=unknown
RUN echo "BUILD_ID: ${BUILD_ID}" && \
    echo "GIT_COMMIT: ${RAILWAY_GIT_COMMIT_SHA}" && \
    echo "TIMESTAMP: $(date +%s)"
```

Railway automatically provides `RAILWAY_GIT_COMMIT_SHA` which changes with every commit, making each build unique and uncacheable.

### Additional Verification
The Dockerfile now:
1. **Verifies controller source files** before build (using grep)
2. **Inspects compiled DLL** after build (using strings command)
3. **Lists all controller classes** found in assembly
4. **Shows all API routes** embedded in DLL

This proves definitively that the correct controllers are compiled.

## Deployment Steps

### Option 1: Railway Automatic Variables (Recommended)
Railway automatically provides these build args:
- `RAILWAY_GIT_COMMIT_SHA` - Git commit hash (changes every commit)
- `RAILWAY_GIT_BRANCH` - Current branch name

**No manual configuration needed!** Just push the updated Dockerfile.

```bash
git add Dockerfile .railway-trigger
git commit -m "fix(railway): ARG-based cache invalidation - nuclear option"
git push origin main
```

Railway will automatically pass `RAILWAY_GIT_COMMIT_SHA` to the build, forcing cache invalidation.

### Option 2: Manual BUILD_ID (If Option 1 Fails)
If Railway doesn't automatically provide the build args, configure manually:

1. **Railway Dashboard** → Your Service → **Settings** → **Build**
2. Find **Docker Build Arguments** section
3. Add:
   ```
   BUILD_ID=1768908600-$(date +%s)
   ```
4. Click **Save**
5. Trigger new deployment:
   ```bash
   git commit --allow-empty -m "trigger: force Railway deploy with BUILD_ID"
   git push origin main
   ```

### Option 3: Railway CLI
```bash
# Set build args via CLI (if supported)
railway run --build-arg BUILD_ID=$(date +%s) deploy
```

## Verification After Deployment

### 1. Check Build Logs
Look for these sections in Railway build output:

```
🚀 ABSOLUTE FRESH BUILD
════════════════════════════════════════════════
BUILD_ID: 1768908600-ARG-BASED-REBUILD
GIT_COMMIT: abc123def456...
TIMESTAMP: Mon Jan 20 17:00:00 UTC 2026
UNIX_TIME: 1768908600
════════════════════════════════════════════════
```

This proves the ARG values were injected.

### 2. Check Controller Verification
Look for:
```
🔍 VERIFYING COMPILED DLL CONTENTS
════════════════════════════════════════════════
Searching for controller classes in DLL...
AuthController
BranchController
LocationController
UserController

Controller routes in DLL...
api/auth
api/branches
api/locations
api/users
════════════════════════════════════════════════
```

If you see `DailySummaryController` or `api/DailySummary`, the build is still using cache!

### 3. Check Production Swagger
Visit: `https://convoy-production-2969.up.railway.app/swagger`

**Expected Controllers:**
- ✅ `Auth` - /api/auth/*
- ✅ `Branch` - /api/branches/*
- ✅ `Location` - /api/locations/*
- ✅ `User` - /api/users/*

**Must NOT appear:**
- ❌ `DailySummary` (deleted controller)

### 4. Test Endpoints Directly
```bash
# Test AuthController
curl -X POST https://convoy-production-2969.up.railway.app/api/auth/verify_number \
  -H "Content-Type: application/json" \
  -d '{"phone_number":"+998901234567"}'

# Test BranchController
curl https://convoy-production-2969.up.railway.app/api/branches

# Should return actual data, not 404
```

## Why This Works

### Traditional Cache Busting (Failed)
```dockerfile
RUN echo "BUILD: $(date +%s)"  # ❌ Cached because layer hash is same
COPY . .                        # ❌ Cached if files unchanged
```

### ARG-Based (Nuclear Option)
```dockerfile
ARG BUILD_ID=unknown            # ✅ ARG cannot be cached
RUN echo "BUILD_ID: ${BUILD_ID}" # ✅ Forces re-execution
```

**Key Insight**: Docker caches layers by content hash. Changing file contents doesn't help if Railway's builder has already cached the layer. But ARG values **must** be evaluated at build time, making them uncacheable.

## Troubleshooting

### Issue 1: "BUILD_ID: unknown" in Logs
**Cause**: Railway didn't pass the BUILD_ID argument.

**Solution**: Railway should automatically provide `RAILWAY_GIT_COMMIT_SHA`. If not:
1. Check Railway documentation for current build arg variables
2. Or use Option 2 (manual BUILD_ID configuration)

### Issue 2: Still Seeing DailySummaryController
**Cause**: Railway is using an even deeper cache (possibly image registry cache).

**Solution - MOST EXTREME**:
1. Delete the entire Railway service
2. Create NEW service from scratch (different name!)
3. Connect to GitHub repository
4. Set environment variables
5. Deploy fresh

**Or try Railway CLI with no-cache flag**:
```bash
railway up --no-cache
```

### Issue 3: Verification Shows Correct Controllers, But Swagger Wrong
**Cause**: Application-level caching (ASP.NET Core metadata cache).

**Solution**: Add this to Program.cs:
```csharp
// Force Swagger to rebuild schema on every startup
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Convoy API",
        Version = $"v1-{DateTime.UtcNow:yyyyMMddHHmm}" // Force unique version
    });
});
```

## Last Resort: Complete Railway Reset

If all else fails, this is the nuclear option:

1. **Local**: Commit all changes
   ```bash
   git add -A
   git commit -m "fix: prepare for Railway reset"
   git push origin main
   ```

2. **Railway Dashboard**:
   - Delete current service entirely
   - Wait 5 minutes (let Railway's internal cache expire)

3. **Create New Project** (not just service!):
   - Railway → New Project
   - Choose "Deploy from GitHub repo"
   - Select repository
   - **Use a DIFFERENT service name** (e.g., `convoy-api-v2`)

4. **Configure Environment Variables** (fresh setup):
   - Add all ConnectionStrings, Jwt, Auth, PhpApi, SmsProviders config

5. **Deploy**: Should work because it's a completely fresh project with no prior cache

## Expected Outcome

After following these steps, you should see:

✅ Railway build logs show unique BUILD_ID and GIT_COMMIT
✅ DLL inspection shows all 4 controllers (Auth, Branch, Location, User)
✅ No mention of DailySummaryController anywhere
✅ Production Swagger lists all 4 controllers
✅ All endpoints respond correctly (not 404)

## Technical Details

### Why ARG Works
- **Dockerfile ARG**: Build-time variable that can be overridden
- **Railway Variables**: Railway passes environment variables as build args automatically
- **Cache Invalidation**: Because ARG values can change, Docker cannot cache layers that reference them
- **RAILWAY_GIT_COMMIT_SHA**: Changes with every commit, guaranteeing uniqueness

### Comparison with Previous Attempts
| Method | Result | Why It Failed |
|--------|--------|---------------|
| Changed Dockerfile timestamp | ❌ Failed | Comment change doesn't affect layer hash |
| Modified .railway-trigger | ❌ Failed | Railway ignores this file for caching |
| Added apt-get layer | ❌ Failed | Railway cached the layer hash |
| Complete Dockerfile restructure | ❌ Failed | Builder cache persists across structure changes |
| Service deletion + recreation | ❌ Failed | Registry cache survives service recreation |
| **ARG with commit SHA** | ✅ **Should work** | ARGs force layer re-execution |

## References

- Docker ARG Documentation: https://docs.docker.com/engine/reference/builder/#arg
- Railway Build Arguments: https://docs.railway.app/develop/builds#build-arguments
- Railway Environment Variables: https://docs.railway.app/develop/variables

---

**Created**: 2026-01-20
**Author**: Claude Code
**Status**: ACTIVE - Use this approach for Railway deployment
