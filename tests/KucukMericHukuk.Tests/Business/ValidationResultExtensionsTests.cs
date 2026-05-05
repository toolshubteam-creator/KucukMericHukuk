using FluentAssertions;
using FluentValidation.Results;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Tests.Business;

public class ValidationResultExtensionsTests
{
    [Fact]
    public void ToFailureResult_WithErrorCode_ShouldUseProvidedCode()
    {
        var vr = new ValidationResult(new[]
        {
            new ValidationFailure("Slug", "Slug zorunlu.") { ErrorCode = "Page.SlugRequired" }
        });

        var result = vr.ToFailureResult<int>();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError!.Code.Should().Be("Page.SlugRequired");
        result.FirstError!.Field.Should().Be("Slug");
        result.FirstError!.Message.Should().Be("Slug zorunlu.");
    }

    [Fact]
    public void ToFailureResult_WithoutErrorCode_ShouldFallBackToCommonValidation()
    {
        var vr = new ValidationResult(new[]
        {
            new ValidationFailure("Title", "Başlık zorunlu.")
        });

        var result = vr.ToFailureResult();

        result.FirstError!.Code.Should().Be(ErrorCodes.Common.Validation);
    }

    [Fact]
    public void ToFailureResult_OnValidResult_ShouldThrow()
    {
        var vr = new ValidationResult();

        var act = () => vr.ToFailureResult<string>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToFailureResult_NonGeneric_OnValidResult_ShouldThrow()
    {
        var vr = new ValidationResult();

        var act = () => vr.ToFailureResult();

        act.Should().Throw<InvalidOperationException>();
    }
}
