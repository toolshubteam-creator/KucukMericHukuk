using Mapster;

namespace KucukMericHukuk.Business.Mappings;

/// <summary>
/// Faz 7.2b-1: Newsletter DTO mapping'leri şu an servis içinde manuel yapılıyor
/// (Article translation flatten + subscriber count compose karmaşıklığı nedeniyle).
/// Bu config sınıfı IRegister scan'i için placeholder — ileride 7.2b-2'de batch
/// processor sırasında basit Job → DetailDto mapping gerekirse buraya eklenecek.
/// </summary>
public class NewsletterMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Şimdilik boş — manual mapping NewsletterService içinde.
    }
}
