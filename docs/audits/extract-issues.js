// Lighthouse JSON raporlarından color-contrast / render-blocking / cache TTL detaylarını çıkarır.
const fs = require('fs');
const path = require('path');

const dir = 'docs/audits/lighthouse';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.report.json')).sort();

const auditId = process.argv[2] || 'color-contrast';
const limit = parseInt(process.argv[3] || '5', 10);

console.log(`# Audit: ${auditId} (top ${limit} per page)`);
console.log('');

for (const f of files) {
  const j = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf-8'));
  const name = f.replace('.report.json', '');
  const a = j.audits[auditId];
  if (!a) continue;
  console.log(`## ${name} — score ${a.score !== null ? Math.round(a.score * 100) : '—'}/100`);
  if (a.displayValue) console.log(`*${a.displayValue}*`);
  console.log('');
  const items = a.details?.items || [];
  for (const item of items.slice(0, limit)) {
    const sel = item.node?.selector || item.url || item.statistic || '';
    const snippet = (item.node?.snippet || '').replace(/\s+/g, ' ').slice(0, 140);
    const fg = item.node?.explanation || '';
    const wm = item.wastedMs !== undefined ? `${Math.round(item.wastedMs)}ms` : '';
    const wb = item.wastedBytes !== undefined ? `${Math.round(item.wastedBytes / 1024)} KiB` : '';
    const ttl = item.cacheLifetimeMs !== undefined ? `TTL ${item.cacheLifetimeMs}ms` : '';
    console.log(`- **${sel}**${wm ? ` — ${wm}` : ''}${wb ? ` — ${wb}` : ''}${ttl ? ` — ${ttl}` : ''}`);
    if (snippet) console.log(`  - snippet: \`${snippet}\``);
    if (fg) console.log(`  - ${fg}`);
  }
  console.log('');
}
