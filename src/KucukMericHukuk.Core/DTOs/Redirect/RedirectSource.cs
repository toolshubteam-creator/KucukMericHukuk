namespace KucukMericHukuk.Core.DTOs.Redirect;

/// <summary>
/// "Yönlendirmeler" listesindeki satırın kaynağı (Faz 7.4.3a-ek).
/// Birleşik liste iki tabloyu (`Redirects` manuel + `SlugHistories` otomatik)
/// tek görünüme getirir; UI satır başına bu enum'a göre render eder.
/// </summary>
public enum RedirectSource
{
    /// <summary>Admin manuel oluşturdu — düzenle/sil/toggle erişilebilir.</summary>
    Manual = 0,
    /// <summary>6 servisin update akışı otomatik üretti — salt-okunur.</summary>
    SlugHistory = 1
}
