# Admin Form Binding Audit — 2026-05-12

> **Faz 6.9** — Faz 6.5 (IsPublic) ve Faz 6.8 (ConfirmPassword) bug'larından
> sonra DEFERRED'a eklenen sistematik audit.

## Kapsam

Tüm admin Create/Edit Razor view'ları ve POST action'larında **validation bypass
pattern** arama.

İki katman:

1. **Katman 1 — Manuel hidden + `asp-for` bool çakışması** (Faz 6.5 IsPublic
   pattern): `<input type="hidden" name="X" value="false" />` + `<input
   asp-for="X" />` (bool checkbox) aynı `name` ile çakışırsa MVC bool model
   binder ilk değeri alır → checkbox işaretli olsa bile DB `false`.
2. **Katman 2 — `ModelState.IsValid` eksik server-side check** (Faz 6.8
   ConfirmPassword pattern): ViewModel'de DataAnnotations (`[Compare]`,
   `[Required]`, `[StringLength]` vb.) varsa ve POST action'da
   `if (!ModelState.IsValid) return View(form);` check'i yoksa, client-side
   validation atlatılabilir → invalid form DB'ye geçer.

## Inventory

- **9 Create view** (Tags, Categories, Attorneys, Pages, Services, Articles,
  Testimonials, Faqs, Users)
- **10 Edit view** (yukarıdakiler + Medias, Users)
- **28 hidden input** kullanımı
- **13 admin controller**, ~30 POST action

## Katman 1 — Bulgular

**0 gerçek bug.**

- 28 hidden input tarandı; hepsi `asp-for="X"` formatında (MVC standart, manuel
  `name` attribute kullanan tek istisna `SiteSettings/Index.cshtml` bool render
  branch'i).
- **`SiteSettings/Index.cshtml:89` incelendi**: manuel `<input type="hidden"
  name="Values[Key]" value="false" />` + manuel `<input type="checkbox"
  name="Values[Key]" value="true" />`. `asp-for` KULLANMIYOR — bu **standart
  MVC bool checkbox pattern**: browser checkbox işaretli → `"false,true"`,
  işaretsiz → `"false"`; `Dictionary<string, string?>` binder son değeri alır.
  Bu Faz 6.5'teki bug'dan farklı: Faz 6.5'te `asp-for` + manuel hidden 3 input
  üretiyordu (auto-hidden çift), burada 2 input (manuel hidden + manuel
  checkbox). **Bug değil.**
- Diğer view'larda bu pattern yok.

## Katman 2 — Bulgular

**0 gerçek bug.**

ViewModel'lerde DataAnnotations kullanan **4 dosya**:

| ViewModel | Annotation | Controller ModelState Check |
|---|---|---|
| `LoginViewModel` | `[Required]`, `[EmailAddress]` | ✓ (AccountController.Login) |
| `UserCreateFormViewModel` | `[Compare]` | ✓ (UsersController.Create, Faz 6.8 fix) |
| `ResetPasswordFormViewModel` | `[Compare]` | ✓ (UsersController.ResetPassword, Faz 6.8 fix) |
| `ArticleFormViewModel` | `[StringLength]` | ✓ (ArticlesController.Create + Edit) |
| `MediaEditFormViewModel` | `[StringLength]` | ✓ (MediasController.Edit, Faz 6.5 fix) |

**Diğer 9 ViewModel** (Tags/Categories/Attorneys/Pages/Services/Testimonials/Faqs/ContactMessage/SiteSettings) DataAnnotations
KULLANMIYOR — sadece `[Display(Name = "...")]`. Bu nedenle bypass edilecek
client-side validation yok; server-side validation tamamen **FluentValidation**
ile (service katmanında `_validator.ValidateAsync(input, ct)`) yapılıyor. Bu
**Faz 2.4a mimari kararı**:

> "FluentValidation auto-MVC YOK. AddFluentValidationAutoValidation çağrılmaz;
> validation Action katmanında değil **service** katmanında manuel yapılır."

Bu kararın sonucu: ModelState bypass yüzeyi minimum, audit Katman 2'de sistemik
risk yok.

## Sonuç

**Sistem temiz.** Faz 6.5 ve 6.8 bug'ları **sistemik değil, izole** durumlardı:

- Faz 6.5 IsPublic: bool field için yanlışlıkla `asp-for` üstüne manuel hidden
  eklenmiş — tekil hata.
- Faz 6.8 ConfirmPassword: `[Compare]` ViewModel'e eklendi ama controller
  `ModelState.IsValid` check'i unutuldu — tekil hata.

Mevcut mimari (`FluentValidation` service katmanı + MVC standart `asp-for`
pattern'i) bu tip bug'lara karşı yapısal koruma sağlar. Yeni admin formları
eklenirken bu iki pattern dışına çıkılmadığı sürece güvende.

## Öneriler (DEFERRED kalan)

- **Yeni admin formu eklenirken kontrol listesi** (örn. PR template):
  - [ ] `asp-for="X"` (bool) varsa manuel `<input type="hidden">` EKLEME
  - [ ] ViewModel'de DataAnnotations varsa controller'da
    `if (!ModelState.IsValid)` check'i ekle
  - [ ] FluentValidation kullanılıyorsa service katmanında validation yeterli
