using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Business.Helpers;

/// <summary>
/// Faz 7.1.2 — Translation collection diff-based merge yardımcısı.
/// Önceki <c>Translations.Clear() + Add()</c> anti-pattern'i yerine LanguageCode bazlı
/// match/update/add/remove uygular. Sonuç: mevcut translation kayıtlarının Id + CreatedAt
/// alanları KORUNUR (audit trail bozulmaz, INSERT yerine UPDATE çıkar).
///
/// Generic constraint: <see cref="ITranslation"/> — Id + LanguageCode property'lerine
/// hizalanır. Caller scalar field kopyalama mantığını <paramref name="copyFields"/> Action
/// üzerinden iletir (entity-spesifik alanlar: Title/Name/Question/Slug/Content vb.).
/// Helper Id ve CreatedAt'a dokunmaz; UpdatedAt EF SaveChanges audit-field güncellemesiyle
/// otomatik atanır.
/// </summary>
public static class TranslationMergeHelper
{
    /// <summary>
    /// <paramref name="existing"/> koleksiyonunu <paramref name="incoming"/> sırasına göre günceller.
    /// LanguageCode eşleşmesi case-insensitive.
    /// </summary>
    /// <param name="copyFields">
    /// <c>(target, source)</c> — target mevcut tracked translation, source yeni gelen (untracked).
    /// Caller burada SADECE scalar field'ları kopyalar (Id/LanguageCode/CreatedAt'a DOKUNMA).
    /// </param>
    public static void Merge<TTranslation>(
        ICollection<TTranslation> existing,
        IEnumerable<TTranslation> incoming,
        Action<TTranslation, TTranslation> copyFields)
        where TTranslation : class, ITranslation
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(copyFields);

        var existingByLang = existing.ToDictionary(
            e => e.LanguageCode,
            StringComparer.OrdinalIgnoreCase);

        var incomingLangs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var src in incoming)
        {
            incomingLangs.Add(src.LanguageCode);

            if (existingByLang.TryGetValue(src.LanguageCode, out var target))
            {
                // Eşleşen translation — sadece scalar field'ları kopyala.
                // Id + LanguageCode + CreatedAt aynı kalır → EF Modified state, SQL UPDATE.
                copyFields(target, src);
            }
            else
            {
                // Yeni language — koleksiyona ekle. EF Added state, SQL INSERT.
                existing.Add(src);
            }
        }

        // Incoming'te artık yer almayan translation'ları kaldır → EF Deleted state, SQL DELETE.
        var toRemove = existing
            .Where(e => !incomingLangs.Contains(e.LanguageCode))
            .ToList();

        foreach (var r in toRemove)
        {
            existing.Remove(r);
        }
    }
}
