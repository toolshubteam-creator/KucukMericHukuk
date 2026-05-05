using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Web.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Tests.Web;

public class ModelStateExtensionsTests
{
    [Fact]
    public void AddErrors_SuccessResult_ShouldNotMutateModelState()
    {
        var modelState = new ModelStateDictionary();
        var result = Result.Success();

        modelState.AddErrors(result);

        modelState.IsValid.Should().BeTrue();
        modelState.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void AddErrors_FieldError_ShouldAttachToFieldKey()
    {
        var modelState = new ModelStateDictionary();
        var result = Result.Failure(new Error("X.Slug", "Slug zorunlu.", "Slug"));

        modelState.AddErrors(result);

        modelState.IsValid.Should().BeFalse();
        modelState["Slug"]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Slug zorunlu.");
    }

    [Fact]
    public void AddErrors_FieldlessError_ShouldGoToSummary()
    {
        var modelState = new ModelStateDictionary();
        var result = Result.Failure(new Error("X.General", "Genel hata."));

        modelState.AddErrors(result);

        modelState[string.Empty]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Genel hata.");
    }

    [Fact]
    public void AddErrors_MultipleErrors_ShouldRouteEachToCorrectKey()
    {
        var modelState = new ModelStateDictionary();
        var result = Result.Failure(new[]
        {
            new Error("X.Slug", "Slug zorunlu.", "Slug"),
            new Error("X.Title", "Başlık zorunlu.", "Title"),
            new Error("X.General", "Genel hata.")
        });

        modelState.AddErrors(result);

        modelState["Slug"]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Slug zorunlu.");
        modelState["Title"]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Başlık zorunlu.");
        modelState[string.Empty]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Genel hata.");
    }
}
