// Lighthouse JSON raporlarını özet markdown'a çevirir.
// Çıktı: stdout (markdown). jq alternatifi.
const fs = require('fs');
const path = require('path');

const dir = 'docs/audits/lighthouse';
const files = fs.readdirSync(dir)
  .filter(f => f.endsWith('.report.json'))
  .sort();

const rows = [];
const issuesByPage = {};

for (const f of files) {
  const j = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf-8'));
  const name = f.replace('.report.json', '');
  const cat = j.categories;
  const score = (k) => cat[k]?.score != null ? Math.round(cat[k].score * 100) : '—';

  rows.push({
    name,
    perf: score('performance'),
    a11y: score('accessibility'),
    bestp: score('best-practices'),
    seo: score('seo'),
  });

  // Top failing audits (score < 0.9, sorted by impact)
  const failing = Object.entries(j.audits)
    .filter(([_, a]) => a.score !== null && a.score !== undefined && a.score < 0.9)
    .map(([id, a]) => ({
      id,
      title: a.title,
      score: a.score === null ? '—' : Math.round(a.score * 100),
      displayValue: a.displayValue || '',
    }))
    .sort((a, b) => (a.score === '—' ? 1 : a.score) - (b.score === '—' ? 1 : b.score))
    .slice(0, 8);

  issuesByPage[name] = failing;
}

// Table
console.log('| Sayfa | Form | Perf | A11y | Best | SEO |');
console.log('|---|---|---:|---:|---:|---:|');
for (const r of rows) {
  const [page, fmt] = r.name.match(/^(.+)-(mobile|desktop)$/)?.slice(1) || [r.name, ''];
  const cell = (v, threshold) => {
    if (v === '—') return v;
    if (v >= threshold) return `**${v}** ✅`;
    if (v >= 70) return `${v} 🟡`;
    return `${v} 🔴`;
  };
  console.log(`| ${page} | ${fmt} | ${cell(r.perf, 90)} | ${cell(r.a11y, 90)} | ${cell(r.bestp, 90)} | ${cell(r.seo, 90)} |`);
}

console.log('');
console.log('## Sayfa Bazlı Top Issue\'lar');
console.log('');
console.log('(Lighthouse audit score < 0.9 olanlar, ilk 8\'i)');
console.log('');

for (const [name, issues] of Object.entries(issuesByPage)) {
  console.log(`### ${name}`);
  if (issues.length === 0) {
    console.log('✅ Tüm audit\'ler >= 90.');
  } else {
    for (const i of issues) {
      const dv = i.displayValue ? ` — *${i.displayValue}*` : '';
      console.log(`- **${i.score}/100** ${i.title} (\`${i.id}\`)${dv}`);
    }
  }
  console.log('');
}
