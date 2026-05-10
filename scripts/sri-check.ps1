# CDN dosyalarını indirip SHA384 SRI hash'lerini üretir.
# Sürüm güncellendiğinde URL'leri değiştir, scripti çalıştır, layout'lara yapıştır.
#
# Usage: .\scripts\sri-check.ps1

$urls = [ordered]@{
    "bootstrap-css" = "https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"
    "bootstrap-js"  = "https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"
    "lucide"        = "https://unpkg.com/lucide@1.14.0/dist/umd/lucide.min.js"
    "quill-css"     = "https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.snow.css"
    "quill-js"      = "https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.js"
}

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  CDN SRI Hash Üretici (sha384)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan

foreach ($name in $urls.Keys) {
    $url = $urls[$name]
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing
        $bytes = $resp.RawContentStream.ToArray()
        $sha384 = [System.Security.Cryptography.SHA384]::Create()
        $hashBytes = $sha384.ComputeHash($bytes)
        $b64 = [System.Convert]::ToBase64String($hashBytes)
        Write-Host ""
        Write-Host "▸ $name" -ForegroundColor Yellow
        Write-Host "  URL:  $url"
        Write-Host "  Size: $($bytes.Length) bytes"
        Write-Host "  SRI:  sha384-$b64" -ForegroundColor Green
    } catch {
        Write-Host ""
        Write-Host "✗ $name → FETCH FAIL: $url" -ForegroundColor Red
        Write-Host "  $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Hash'leri layout dosyalarına yapıştır:" -ForegroundColor Cyan
Write-Host "  - _PublicLayout.cshtml + _BareLayout.cshtml (Bootstrap CSS+JS, Lucide)" -ForegroundColor Cyan
Write-Host "  - _AdminLayout.cshtml (Bootstrap JS)" -ForegroundColor Cyan
Write-Host "  - _QuillStyles.cshtml + _QuillScripts.cshtml (Quill CSS+JS)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
