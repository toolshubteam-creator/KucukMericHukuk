using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;

namespace KucukMericHukuk.Business.Services;

public class FaqService : IFaqService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public FaqService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<FaqListDto>> GetActiveOrderedAsync(
        string languageCode, CancellationToken ct = default)
    {
        var faqs = await _uow.Faqs.GetActiveOrderedAsync(languageCode, ct);
        return _mapper.Map<List<FaqListDto>>(faqs);
    }
}
