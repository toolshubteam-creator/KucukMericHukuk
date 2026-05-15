# Küçükmeriç Hukuk Bürosu — Web Sitesi & CMS

Küçükmeriç Hukuk Bürosu (Serdivan / Sakarya) için geliştirilen kurumsal web sitesi ve içerik yönetim sistemi.

## Özet

- **Teknoloji:** ASP.NET MVC Core 10 + EF Core 10 + MSSQL
- **Mimari:** N-Layer Architecture
- **Domain:** kucukmerichukuk.av.tr
- **Hedef:** SEO-odaklı, tam yönetilebilir, çok dile hazır kurumsal site

## Hızlı Başlangıç

### Gereksinimler

- .NET 10 SDK
- SQL Server 2022+ (veya LocalDB)
- Visual Studio 2022 / VS Code
- Node.js (frontend asset build için, opsiyonel)

### Kurulum

```bash
# 1) Repo klonla
git clone https://github.com/<org>/KucukMericHukuk.git
cd KucukMericHukuk

# 2) Bağımlılıkları yükle
dotnet restore

# 3) Geliştirme ayarlarını oluştur
#    appsettings.Development.example.json -> appsettings.Development.json
#    (Development.json .gitignore'da; sadece localde tutulur)
cp src/KucukMericHukuk.Web/appsettings.Development.example.json \
   src/KucukMericHukuk.Web/appsettings.Development.json

# 4) appsettings.Development.json içinde:
#    - ConnectionStrings:DefaultConnection — kendi SQL Server'ınız
#    - Seed:AdminPassword — güçlü bir parola koy
#    - EmailSettings:SmtpHost — opsiyonel; boş bırakılırsa NullEmailSender
#      devreye girer ve mail gönderimi log'a düşer (dev rahat)

# 5) Veritabanını oluştur
dotnet ef database update --project src/KucukMericHukuk.DataAccess \
                          --startup-project src/KucukMericHukuk.Web

# 6) Uygulamayı çalıştır (HTTPS profili zorunlu — cookie auth Secure)
dotnet run --project src/KucukMericHukuk.Web --launch-profile https
```

Tarayıcıda: `https://localhost:7082/tr-TR/`
Admin paneli: `https://localhost:7082/admin/account/login`

## Production Deploy

Production environment'ta uygulamayı çalıştırmadan önce aşağıdaki ortam değişkenleri (environment variables) **zorunlu** olarak set edilmelidir. Eksik bırakılırsa uygulama startup'ta `InvalidOperationException` ile başlamayı reddeder (`SeedOptions.ValidateForProduction`).

```bash
# ASP.NET Core environment
export ASPNETCORE_ENVIRONMENT="Production"

# Veritabanı bağlantısı (SQL Server)
export ConnectionStrings__DefaultConnection="Server=<host>;Database=KucukMericHukuk;User Id=<user>;Password=<pass>;TrustServerCertificate=true;Encrypt=true"

# İlk admin kullanıcısı (DB seed sırasında oluşturulur)
export Seed__AdminEmail="admin@kucukmerichukuk.av.tr"
export Seed__AdminPassword="<güçlü-bir-parola>"
export Seed__AdminFullName="Sistem Yöneticisi"

# Demo content seed kapalı (Production'da gerekmez)
export Seed__SeedDemoContent="false"

# E-posta servisi (iletişim formu bildirimleri için)
export EmailSettings__SmtpHost="<smtp.example.com>"
export EmailSettings__SmtpPort="587"
export EmailSettings__Username="<smtp-user>"
export EmailSettings__Password="<smtp-pass>"
export EmailSettings__AdminNotificationEmail="<bildirim@kucukmerichukuk.av.tr>"

# Cloudflare Turnstile (iletişim formu spam koruması)
export Turnstile__SiteKey="<cloudflare-site-key>"
export Turnstile__SecretKey="<cloudflare-secret-key>"
```

Windows PowerShell eşdeğeri: `$env:VAR_NAME = "value"`.

### Migration uygulama

```bash
dotnet ef database update --project src/KucukMericHukuk.DataAccess \
                          --startup-project src/KucukMericHukuk.Web
```

### Notlar

- `appsettings.Production.json` placeholder template'tir; **secret içermez**, hepsi env-var ile override edilir.
- ASP.NET Core config layering: `ConnectionStrings__DefaultConnection` env-var → `appsettings.Production.json` → `appsettings.json`.
- SiteInfo (telefon, adres, e-posta) `appsettings.json` üzerinden ilk seed'de DB'ye yazılır; sonradan `/admin/site-settings` panelinden müşteri tarafından güncellenir.
- HTTPS sertifikası reverse proxy (örn. Nginx, IIS, Cloudflare) tarafında sonlanmalıdır; uygulamanın kendisi `app.UseHsts()` ile HSTS header gönderir.

> Tam deploy checklist'i için bkz. [docs/deployment/PRODUCTION_DEPLOY.md](docs/deployment/PRODUCTION_DEPLOY.md) — Natro Windows + IIS odaklı operasyonel rehber.

## Klasör Yapısı

Detaylı yapı için `CLAUDE.md` dosyasına bakınız.

## Geliştirme Süreci

Faz bazlı ilerleme. Mevcut faz ve tamamlanan işler için `PROGRESS.md` dosyasına bakınız.

## Lisans

Bu proje Küçükmeriç Hukuk Bürosu'na özel olarak geliştirilmiştir. Tüm hakları saklıdır.

## İletişim

Geliştirici: [Geliştirici adı]
Müşteri: Küçükmeriç Hukuk Bürosu — Serdivan / Sakarya
