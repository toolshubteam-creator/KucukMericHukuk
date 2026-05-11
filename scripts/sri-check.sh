#!/usr/bin/env bash
# CDN dosyalarını indirip SHA384 SRI hash'lerini üretir.
# Sürüm güncellendiğinde URL'leri değiştir, scripti çalıştır, layout'lara yapıştır.
#
# Usage: ./scripts/sri-check.sh

set -euo pipefail

declare -A urls=(
    ["bootstrap-css"]="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"
    ["bootstrap-js"]="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"
    ["lucide"]="https://unpkg.com/lucide@1.14.0/dist/umd/lucide.min.js"
    ["quill-css"]="https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.snow.css"
    ["quill-js"]="https://cdn.jsdelivr.net/npm/quill@2.0.3/dist/quill.js"
    ["tabler-core-css"]="https://cdn.jsdelivr.net/npm/@tabler/core@1.4.0/dist/css/tabler.min.css"
    ["tabler-icons-webfont"]="https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@3.42.0/dist/tabler-icons.min.css"
    ["tabler-core-js"]="https://cdn.jsdelivr.net/npm/@tabler/core@1.4.0/dist/js/tabler.min.js"
    ["sweetalert2"]="https://cdn.jsdelivr.net/npm/sweetalert2@11.26.24/dist/sweetalert2.all.min.js"
    ["choices-css"]="https://cdn.jsdelivr.net/npm/choices.js@11.1.0/public/assets/styles/choices.min.css"
    ["choices-js"]="https://cdn.jsdelivr.net/npm/choices.js@11.1.0/public/assets/scripts/choices.min.js"
)

echo "═══════════════════════════════════════════════"
echo "  CDN SRI Hash Üretici (sha384)"
echo "═══════════════════════════════════════════════"

for name in "${!urls[@]}"; do
    url="${urls[$name]}"
    tmpfile=$(mktemp)
    if curl -sfL "$url" -o "$tmpfile"; then
        size=$(wc -c < "$tmpfile")
        hash=$(openssl dgst -sha384 -binary "$tmpfile" | openssl base64 -A)
        echo ""
        echo "▸ $name"
        echo "  URL:  $url"
        echo "  Size: $size bytes"
        echo "  SRI:  sha384-$hash"
    else
        echo ""
        echo "✗ $name → FETCH FAIL: $url"
    fi
    rm -f "$tmpfile"
done

echo ""
echo "═══════════════════════════════════════════════"
echo "  Hash'leri layout dosyalarına yapıştır:"
echo "  - _PublicLayout.cshtml + _BareLayout.cshtml (Bootstrap CSS+JS, Lucide)"
echo "  - _AdminLayout.cshtml (Bootstrap JS)"
echo "  - _QuillStyles.cshtml + _QuillScripts.cshtml (Quill CSS+JS)"
echo "═══════════════════════════════════════════════"
