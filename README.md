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
# Repo klonla
git clone https://github.com/<org>/KucukMericHukuk.git
cd KucukMericHukuk

# Bağımlılıkları yükle
dotnet restore

# Connection string ayarla (User Secrets önerilir)
cd src/KucukMericHukuk.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=KucukMericHukuk;..."

# Veritabanını oluştur
dotnet ef database update --project ../KucukMericHukuk.DataAccess

# Uygulamayı çalıştır
dotnet run
```

Tarayıcıda: `https://localhost:5001`
Admin paneli: `https://localhost:5001/admin`

## Klasör Yapısı

Detaylı yapı için `CLAUDE.md` dosyasına bakınız.

## Geliştirme Süreci

Faz bazlı ilerleme. Mevcut faz ve tamamlanan işler için `PROGRESS.md` dosyasına bakınız.

## Lisans

Bu proje Küçükmeriç Hukuk Bürosu'na özel olarak geliştirilmiştir. Tüm hakları saklıdır.

## İletişim

Geliştirici: [Geliştirici adı]
Müşteri: Küçükmeriç Hukuk Bürosu — Serdivan / Sakarya
