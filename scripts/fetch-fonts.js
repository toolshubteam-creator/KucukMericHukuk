#!/usr/bin/env node
// Google Fonts CSS'inden latin + latin-ext subset'lerini ayıklar,
// woff2 dosyalarını wwwroot/fonts/ altına indirir, relative-path'li fonts.css üretir.
// Türkçe için latin-ext yeterli (ç ş ğ ı İ Ş Ğ Ü Ö hepsi orada).
const fs = require('fs');
const https = require('https');
const path = require('path');

const CSS_PATH = path.join(__dirname, '.fonts-source.css');
const FONTS_DIR = 'src/KucukMericHukuk.Web/wwwroot/fonts';
const OUTPUT_CSS = 'src/KucukMericHukuk.Web/wwwroot/css/fonts.css';
const KEEP_SUBSETS = ['latin', 'latin-ext'];

const css = fs.readFileSync(CSS_PATH, 'utf-8');

// CSS'i `/* subset-name */\n@font-face { ... }` bloklarına böl
const blocks = [];
const re = /\/\*\s*([\w-]+)\s*\*\/\s*(@font-face\s*\{[^}]+\})/g;
let m;
while ((m = re.exec(css)) !== null) {
  blocks.push({ subset: m[1], rule: m[2] });
}

const kept = blocks.filter(b => KEEP_SUBSETS.includes(b.subset));
console.log(`Total blocks: ${blocks.length}, kept (${KEEP_SUBSETS.join('+')}): ${kept.length}`);

function download(url, dest) {
  return new Promise((resolve, reject) => {
    https.get(url, (res) => {
      if (res.statusCode !== 200) return reject(new Error(`HTTP ${res.statusCode} ${url}`));
      const fws = fs.createWriteStream(dest);
      res.pipe(fws);
      fws.on('finish', () => fws.close(() => resolve(dest)));
    }).on('error', reject);
  });
}

(async () => {
  if (!fs.existsSync(FONTS_DIR)) fs.mkdirSync(FONTS_DIR, { recursive: true });

  const familyCount = {};  // disambiguation suffix için

  let localCss = '/* Self-hosted Google Fonts — Faz 6.14\n';
  localCss += ' * Source: fonts.googleapis.com/css2?family=Playfair+Display:wght@400;500;700&family=Inter:wght@400;500;600&display=swap\n';
  localCss += ' * Filtered subsets: ' + KEEP_SUBSETS.join(', ') + '\n';
  localCss += ' */\n\n';

  for (const b of kept) {
    const family = (b.rule.match(/font-family:\s*'([^']+)'/) || [, ''])[1];
    const weight = (b.rule.match(/font-weight:\s*(\d+)/) || [, ''])[1];
    const url = (b.rule.match(/url\(([^)]+)\)/) || [, ''])[1];
    if (!family || !weight || !url) continue;

    const slug = family.toLowerCase().replace(/\s+/g, '-');
    const baseName = `${slug}-${weight}-${b.subset}`;
    familyCount[baseName] = (familyCount[baseName] || 0) + 1;
    const fname = `${baseName}.woff2`;
    const dest = path.join(FONTS_DIR, fname);

    process.stdout.write(`  ↓ ${fname} ... `);
    try {
      await download(url, dest);
      const size = fs.statSync(dest).size;
      console.log(`${(size / 1024).toFixed(1)} KB`);
    } catch (e) {
      console.log(`FAIL: ${e.message}`);
      continue;
    }

    // unicode-range satırını yakala
    const ur = (b.rule.match(/unicode-range:[^;]+;/) || [''])[0];
    localCss += `/* ${family} ${weight} ${b.subset} */\n`;
    localCss += `@font-face {\n`;
    localCss += `  font-family: '${family}';\n`;
    localCss += `  font-style: normal;\n`;
    localCss += `  font-weight: ${weight};\n`;
    localCss += `  font-display: swap;\n`;
    localCss += `  src: url(/fonts/${fname}) format('woff2');\n`;
    if (ur) localCss += `  ${ur}\n`;
    localCss += `}\n\n`;
  }

  fs.writeFileSync(OUTPUT_CSS, localCss);
  console.log(`\n✅ ${OUTPUT_CSS} (${(fs.statSync(OUTPUT_CSS).size / 1024).toFixed(1)} KB)`);
})();
