using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Contact;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IContactMessageService
{
    Task<Result<int>> SaveAsync(ContactFormDto form, CancellationToken ct = default);
}
