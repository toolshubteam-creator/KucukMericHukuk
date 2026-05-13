// Faz 6.13 (pre-fix) vs Faz 6.14 (post-fix) skor karşılaştırması.
const fs = require('fs');
const path = require('path');

function loadScores(dir) {
  const out = {};
  for (const f of fs.readdirSync(dir).filter(f => f.endsWith('.report.json')).sort()) {
    const j = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf-8'));
    const name = f.replace('.report.json', '');
    const s = (k) => j.categories[k]?.score != null ? Math.round(j.categories[k].score * 100) : null;
    out[name] = { perf: s('performance'), a11y: s('accessibility'), bestp: s('best-practices'), seo: s('seo') };
  }
  return out;
}

const before = loadScores('docs/audits/lighthouse');
const after = loadScores('docs/audits/lighthouse/post-fix');

const fmtDelta = (b, a) => {
  if (b === null || a === null) return '—';
  const d = a - b;
  if (d === 0) return `${a}`;
  const arrow = d > 0 ? '+' : '';
  return `${b}→**${a}** (${arrow}${d})`;
};

console.log('## Önce / Sonra Skor Karşılaştırması (Faz 6.13 → 6.14)\n');
console.log('| Sayfa | Perf | A11y | Best | SEO |');
console.log('|---|---|---|---|---|');
const names = Object.keys(after).sort();
for (const name of names) {
  const b = before[name] || {};
  const a = after[name];
  console.log(`| ${name} | ${fmtDelta(b.perf, a.perf)} | ${fmtDelta(b.a11y, a.a11y)} | ${fmtDelta(b.bestp, a.bestp)} | ${fmtDelta(b.seo, a.seo)} |`);
}

// Aggregate
const avg = (arr) => Math.round(arr.reduce((s, v) => s + v, 0) / arr.length);
const beforePerfMobile = names.filter(n => n.endsWith('-mobile')).map(n => before[n]?.perf).filter(v => v != null);
const afterPerfMobile = names.filter(n => n.endsWith('-mobile')).map(n => after[n]?.perf).filter(v => v != null);
const beforePerfDesktop = names.filter(n => n.endsWith('-desktop')).map(n => before[n]?.perf).filter(v => v != null);
const afterPerfDesktop = names.filter(n => n.endsWith('-desktop')).map(n => after[n]?.perf).filter(v => v != null);
const beforeA11y = names.map(n => before[n]?.a11y).filter(v => v != null);
const afterA11y = names.map(n => after[n]?.a11y).filter(v => v != null);

console.log('\n### Toplulaştırılmış Ortalamalar');
console.log(`- **Perf (mobile)**: ${avg(beforePerfMobile)} → **${avg(afterPerfMobile)}**`);
console.log(`- **Perf (desktop)**: ${avg(beforePerfDesktop)} → **${avg(afterPerfDesktop)}**`);
console.log(`- **A11y**: ${avg(beforeA11y)} → **${avg(afterA11y)}**`);
