# PowerShell script to encode Firebase credentials to base64
# Usage: .\encode-firebase-credentials.ps1 firebase-adminsdk.json

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath
)

if (-not (Test-Path $FilePath)) {
    Write-Host "❌ Error: File not found: $FilePath" -ForegroundColor Red
    exit 1
}

try {
    # Read file content
    $content = Get-Content $FilePath -Raw

    # Convert to base64
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $base64 = [Convert]::ToBase64String($bytes)

    Write-Host "✅ Firebase credentials encoded successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Copy this value and add to your docker-compose.yml:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "FIREBASE_CREDENTIALS_BASE64: `"$base64`"" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "💡 Or add it in Portainer UI -> Environment Variables section" -ForegroundColor Yellow

    # Save to file for easy copying
    $outputFile = "firebase-credentials-base64.txt"
    $base64 | Out-File -FilePath $outputFile -Encoding utf8 -NoNewline
    Write-Host ""
    Write-Host "✅ Also saved to: $outputFile" -ForegroundColor Green

} catch {
    Write-Host "❌ Error encoding file: $_" -ForegroundColor Red
    exit 1
}
