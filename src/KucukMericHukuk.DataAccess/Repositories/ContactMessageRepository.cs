using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;

namespace KucukMericHukuk.DataAccess.Repositories;

public class ContactMessageRepository : GenericRepository<ContactMessage>, IContactMessageRepository
{
    public ContactMessageRepository(AppDbContext context) : base(context) { }
}
