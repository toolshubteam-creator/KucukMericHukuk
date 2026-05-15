# Production Deploy Guide — Natro Windows + IIS

> Operasyonel referans. Hedef hosting: **Natro Windows hosting paketi** (IIS + MSSQL). Linux/Nginx varyantı bu dokümanda yok. Adımlar sırayla uygulanır; gerçek secret'lar env-var üzerinden tanımlanır, repoya commit edilmez.

---

## Ön gereksinimler (deploy günü öncesi)

- [ ] Natro Windows hosting paketi aktif (.NET 10 desteği, MSSQL dahil)
- [ ] Domain (`kucukmerichukuk.av.tr`) Natro'ya yönlendirilmiş
- [ ] SSL sertifikası Natro panelinden kurulmuş (Let's Encrypt veya Comodo)
- [ ] MSSQL veritabanı oluşturulmuş, connection string Natro panelinden alınmış
- [ ] Cloudflare Turnstile gerçek key'leri (SiteKey + SecretKey) — Cloudflare dashboard
- [ ] SMTP credential (e-posta bildirimleri için)
- [ ] Google Search Console için Google hesabı erişimi

---

## 1. Build + publish (yerel makine)

```bash
cd src/KucukMericHukuk.Web
dotnet publish -c Release -o ./publish-output --self-contained false -r win-x64
```

Output: `publish-output/` klasörü → bu klasörün tamamı Natro'ya yüklenir.

---

## 2. Migration script üret (idempotent SQL)

Production'da `dotnet ef database update` çalıştırmak yerine **idempotent SQL script** üretip Natro MSSQL Management üzerinden çalıştır:

```bash
dotnet ef migrations script --idempotent \
  --project src/KucukMericHukuk.DataAccess \
  --startup-project src/KucukMericHukuk.Web \
  -o migration.sql
```

`migration.sql` Natro MSSQL panelinden execute edilir. Idempotent — defalarca çalıştırılsa bozulmaz.

---

## 3. Natro panel: env-var (Application Settings) tanımla

Natro Windows panel → site → Configuration → **Application Settings**:

| Key | Değer |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Server=<natro-mssql-host>;Database=<db>;User Id=<user>;Password=<pass>;TrustServerCertificate=true;Encrypt=true` |
| `Seed__AdminEmail` | `admin@kucukmerichukuk.av.tr` |
| `Seed__AdminPassword` | `<güçlü-parola — burada saklama, deploy sırasında set>` |
| `Seed__AdminFullName` | `Sistem Yöneticisi` |
| `Seed__SeedDemoContent` | `false` |
| `EmailSettings__SmtpHost` | `<smtp.natro.com veya benzeri>` |
| `EmailSettings__SmtpPort` | `587` |
| `EmailSettings__Username` | `<smtp-user>` |
| `EmailSettings__Password` | `<smtp-pass>` |
| `EmailSettings__AdminNotificationEmail` | `<bildirim@kucukmerichukuk.av.tr>` |
| `Turnstile__SiteKey` | `<cloudflare-site-key>` |
| `Turnstile__SecretKey` | `<cloudflare-secret-key>` |

> Eksik bir `Seed__` veya `Turnstile__` env-var olursa uygulama startup'ta `InvalidOperationException` ile fail eder (Faz 6.12 + 6.25 enforcement).

---

## 4. Publish output'u Natro'ya yükle

FTP/FileZilla veya Natro File Manager üzerinden `publish-output/` klasörünün tüm içeriği → site root'a kopyala.

`appsettings.Production.json` zaten publish output içinde — değiştirme, secret'lar env-var'dan geliyor.

---

## 5. IIS site recycle

Natro Windows panel → site → **Restart** (veya App Pool Recycle). Bu, env-var değişikliklerini uygulamaya yansıtır.

---

## 6. Smoke test (deploy sonrası ilk 10 dakika)

Kanıt-temelli kontroller — her birini gör + işaretle:

- [ ] `https://kucukmerichukuk.av.tr/tr-TR/` → ana sayfa açılıyor, görsel olarak doğru
- [ ] Tarayıcı F12 → Console → kırmızı hata YOK
- [ ] Network sekmesi → ana sayfa response header → `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload` GÖRÜNÜYOR
- [ ] `/admin/account/login` → form açılıyor, Turnstile widget render oluyor (demo değil, gerçek Cloudflare widget — Natro IP'sini Cloudflare tanıyor mu)
- [ ] Admin login (`Seed__` env-var ile set edilen parola) → `/admin` dashboard açılıyor
- [ ] `/tr-TR/Contact` → iletişim formu submit → `admin@kucukmerichukuk.av.tr`'ye e-posta geliyor
- [ ] `/tr-TR/Appointment` → randevu formu submit → e-posta + admin'de görünüyor
- [ ] `/sitemap.xml` ve `/robots.txt` → erişilebilir, doğru içerik
- [ ] `https://www.ssllabs.com/ssltest/analyze.html?d=kucukmerichukuk.av.tr` → A veya A+ skoru
- [ ] `https://securityheaders.com/?q=kucukmerichukuk.av.tr` → A veya üzeri (HSTS preload, CSP, X-Frame, vb.)

---

## 7. Yayın sonrası — operasyonel

### Google Search Console
- [ ] https://search.google.com/search-console → property ekle (`https://kucukmerichukuk.av.tr/`)
- [ ] Domain verification (HTML tag — admin `/admin/site-settings/Entegrasyonlar` bölümünden eklenir, veya DNS TXT — Natro DNS panelinden)
- [ ] Sitemap submit: `https://kucukmerichukuk.av.tr/sitemap.xml`

### HSTS Preload Submit
- [ ] HSTS header SSL Labs'te onaylandı (max-age >= 1 yıl, preload, includeSubDomains)
- [ ] https://hstspreload.org/ → domain submit
- [ ] Kabul süreci haftalar sürebilir, Chrome'a hardcoded olur

### Cloudflare (opsiyonel ama önerilir)
- [ ] Natro DNS → Cloudflare nameserver'a değiştir
- [ ] Cloudflare SSL/TLS mode: **Full (strict)**
- [ ] Cloudflare Turnstile zaten aktif (Faz 6.25 production key)

---

## Sorun giderme

**Startup fail — "Seed__AdminPassword zorunlu":** env-var Natro panelden eksik, ekle + recycle.

**Login sayfası açılmıyor, 500 hatası:** connection string yanlış, Natro MSSQL erişimini kontrol et.

**Turnstile widget görünmüyor:** SiteKey yanlış veya Cloudflare domain whitelist'ine `kucukmerichukuk.av.tr` eklenmemiş.

**HSTS header görünmüyor:** Production env'da olduğundan emin ol (`ASPNETCORE_ENVIRONMENT=Production`), site recycle gerekiyor olabilir.
