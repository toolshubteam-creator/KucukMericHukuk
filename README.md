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

## Klasör Yapısı

Detaylı yapı için `CLAUDE.md` dosyasına bakınız.

## Geliştirme Süreci

Faz bazlı ilerleme. Mevcut faz ve tamamlanan işler için `PROGRESS.md` dosyasına bakınız.

## Lisans

Bu proje Küçükmeriç Hukuk Bürosu'na özel olarak geliştirilmiştir. Tüm hakları saklıdır.

## İletişim

Geliştirici: [Geliştirici adı]
Müşteri: Küçükmeriç Hukuk Bürosu — Serdivan / Sakarya
