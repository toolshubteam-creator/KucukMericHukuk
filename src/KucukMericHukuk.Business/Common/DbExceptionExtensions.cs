using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Common;

public static class DbExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message;
        if (string.IsNullOrEmpty(msg)) return false;

        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
