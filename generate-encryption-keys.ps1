# AES-256 encryption keys generator
# Run: powershell -ExecutionPolicy Bypass -File generate-encryption-keys.ps1

Write-Host "=== Generating AES-256 Encryption Keys ===" -ForegroundColor Cyan
Write-Host ""

# Generate 32 byte (256-bit) key
$keyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
$key = [Convert]::ToBase64String($keyBytes)

# Generate 16 byte (128-bit) IV
$ivBytes = New-Object byte[] 16
$rng.GetBytes($ivBytes)
$iv = [Convert]::ToBase64String($ivBytes)

Write-Host "[OK] Keys generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Add these to your appsettings.json:" -ForegroundColor Yellow
Write-Host ""
Write-Host '  "Encryption": {' -ForegroundColor White
Write-Host '    "Enabled": true,' -ForegroundColor White
Write-Host "    `"Key`": `"$key`"," -ForegroundColor Green
Write-Host "    `"IV`": `"$iv`"" -ForegroundColor Green
Write-Host '  }' -ForegroundColor White
Write-Host ""
Write-Host "[WARNING] IMPORTANT SECURITY NOTES:" -ForegroundColor Red
Write-Host "   1. Keep these keys SECRET - never commit to Git!" -ForegroundColor Yellow
Write-Host "   2. Use different keys for Development and Production" -ForegroundColor Yellow
Write-Host "   3. Store production keys in secure vault (Azure Key Vault, AWS Secrets Manager)" -ForegroundColor Yellow
Write-Host "   4. Share keys with Flutter team via secure channel (NOT email/Slack)" -ForegroundColor Yellow
Write-Host ""
Write-Host "[INFO] For Flutter (Dart), use these same Base64 strings" -ForegroundColor Cyan
Write-Host ""

# Also save to file
$outputFile = "encryption-keys-$(Get-Date -Format 'yyyy-MM-dd-HHmmss').txt"
$content = @"
Generated: $(Get-Date)

appsettings.json:
  "Encryption": {
    "Enabled": true,
    "Key": "$key",
    "IV": "$iv"
  }

Flutter (Dart):
  final String encryptionKey = '$key';
  final String encryptionIV = '$iv';
"@

$content | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "[SAVED] Keys saved to: $outputFile" -ForegroundColor Green
Write-Host ""
