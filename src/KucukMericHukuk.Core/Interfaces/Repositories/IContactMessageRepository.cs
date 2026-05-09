using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IContactMessageRepository : IGenericRepository<ContactMessage>
{
    // Faz 4.8: generic'in AddAsync'i yeter.
    // Faz 5'te admin liste için method'lar eklenecek.
}
