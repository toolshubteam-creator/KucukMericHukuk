// Lightweight meta extractor — sade regex, parser bağımlılığı YOK.
// Kullanım: node meta-extract.js <html-file> <label>
const fs = require('fs');

const [,, htmlPath, label] = process.argv;
if (!htmlPath) {
  console.error('Usage: node meta-extract.js <html-file> <label>');
  process.exit(1);
}

const html = fs.readFileSync(htmlPath, 'utf-8');
const grab = (re) => {
  const m = html.match(re);
  return m ? m[1].trim() : '';
};
const ascii = (s) => s.replace(/[^\x20-\x7E]/g, '·').slice(0, 90);

const out = {
  label,
  title: grab(/<title[^>]*>([\s\S]*?)<\/title>/i),
  metaDescription: grab(/<meta[^>]+name=["']description["'][^>]+content=["']([^"']+)["']/i),
  canonical: grab(/<link[^>]+rel=["']canonical["'][^>]+href=["']([^"']+)["']/i),
  ogTitle: grab(/<meta[^>]+property=["']og:title["'][^>]+content=["']([^"']+)["']/i),
  ogImage: grab(/<meta[^>]+property=["']og:image["'][^>]+content=["']([^"']+)["']/i),
  ogType: grab(/<meta[^>]+property=["']og:type["'][^>]+content=["']([^"']+)["']/i),
  jsonLdCount: (html.match(/application\/ld\+json/g) || []).length,
  h1Count: (html.match(/<h1\b/gi) || []).length,
  imgTotal: (html.match(/<img\b/gi) || []).length,
  imgWithoutAlt: (() => {
    const imgs = html.match(/<img\b[^>]*>/gi) || [];
    return imgs.filter(t => !/\balt\s*=/i.test(t)).length;
  })(),
  lang: grab(/<html[^>]+lang=["']([^"']+)["']/i),
  viewport: grab(/<meta[^>]+name=["']viewport["'][^>]+content=["']([^"']+)["']/i),
};

console.log(`### ${out.label}`);
console.log(`- Title (${out.title.length} chars): ${ascii(out.title)}`);
console.log(`- Meta description (${out.metaDescription.length} chars): ${ascii(out.metaDescription)}`);
console.log(`- Canonical: ${out.canonical || '— YOK'}`);
console.log(`- og:title: ${ascii(out.ogTitle) || '— YOK'}`);
console.log(`- og:image: ${out.ogImage || '— YOK'}`);
console.log(`- og:type: ${out.ogType || '— YOK'}`);
console.log(`- JSON-LD blocks: **${out.jsonLdCount}**`);
console.log(`- H1 count: **${out.h1Count}**${out.h1Count !== 1 ? ' ⚠️' : ''}`);
console.log(`- Img: ${out.imgTotal} total, **${out.imgWithoutAlt} without alt**${out.imgWithoutAlt > 0 ? ' ⚠️' : ''}`);
console.log(`- <html lang>: ${out.lang || '— YOK ⚠️'}`);
console.log(`- viewport: ${out.viewport || '— YOK ⚠️'}`);
console.log('');
