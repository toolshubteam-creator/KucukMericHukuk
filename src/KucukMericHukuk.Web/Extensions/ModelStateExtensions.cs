using KucukMericHukuk.Core.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Web.Extensions;

public static class ModelStateExtensions
{
    public static void AddErrors(this ModelStateDictionary modelState, Result result)
    {
        if (result.IsSuccess) return;

        foreach (var error in result.Errors)
        {
            var key = error.Field ?? string.Empty;
            modelState.AddModelError(key, error.Message);
        }
    }
}
