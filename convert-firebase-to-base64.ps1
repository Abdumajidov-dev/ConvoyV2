# Firebase credentials file'ni Base64'ga o'girish
# Railway environment variable uchun

$firebaseFile = "firebase-adminsdk.json"

# File mavjudligini tekshirish
if (-Not (Test-Path $firebaseFile)) {
    Write-Host "❌ Error: $firebaseFile topilmadi!" -ForegroundColor Red
    Write-Host "Firebase credentials file'ni project root'ga joylashtiring." -ForegroundColor Yellow
    exit 1
}

Write-Host "📄 File topildi: $firebaseFile" -ForegroundColor Green
Write-Host ""

# Base64'ga o'girish
try {
    $bytes = [IO.File]::ReadAllBytes($firebaseFile)
    $base64 = [Convert]::ToBase64String($bytes)

    # File'ga saqlash
    $base64 | Out-File "firebase-base64.txt" -NoNewline -Encoding ASCII

    Write-Host "✅ Base64 string muvaffaqiyatli yaratildi!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Firebase Base64 String:" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host $base64 -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "💾 Base64 string saqlab qo'yildi: firebase-base64.txt" -ForegroundColor Green
    Write-Host ""
    Write-Host "📌 Keyingi qadamlar:" -ForegroundColor Yellow
    Write-Host "   1. Yuqoridagi string'ni copy qiling (Ctrl+C)" -ForegroundColor White
    Write-Host "   2. Railway dashboard'ga kiring: https://railway.app/" -ForegroundColor White
    Write-Host "   3. Project > Variables tab'ga o'ting" -ForegroundColor White
    Write-Host "   4. New Variable tugmasini bosing" -ForegroundColor White
    Write-Host "   5. Name: FIREBASE_CREDENTIALS_BASE64" -ForegroundColor White
    Write-Host "   6. Value: <paste_string>" -ForegroundColor White
    Write-Host "   7. Add tugmasini bosing" -ForegroundColor White
    Write-Host ""
    Write-Host "✅ Deployment avtomatik restart bo'ladi!" -ForegroundColor Green

    # Clipboard'ga copy qilish (optional)
    try {
        Set-Clipboard -Value $base64
        Write-Host "📋 String clipboard'ga copy qilindi!" -ForegroundColor Cyan
    } catch {
        Write-Host "⚠️ Clipboard'ga avtomatik copy bo'lmadi. Yuqoridan qo'lda copy qiling." -ForegroundColor Yellow
    }

} catch {
    Write-Host "❌ Error: Base64 conversion failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
