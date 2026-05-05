using FluentAssertions;
using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Success_ShouldBeIsSuccessTrue_AndHaveNoErrors()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.FirstError.Should().BeNull();
    }

    [Fact]
    public void Failure_WithError_ShouldExposeFirstError()
    {
        var error = new Error("X.Test", "Bir hata.", "Field1");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void Failure_WithEmptyErrorList_ShouldThrow()
    {
        var act = () => Result.Failure(Enumerable.Empty<Error>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generic_Success_ShouldExposeValue()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Generic_Failure_AccessingValue_ShouldThrow()
    {
        var result = Result<string>.Failure(new Error("X", "msg"));

        var act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generic_FailureFactoryFromBase_ShouldReturnFailureResult()
    {
        var result = Result.Failure<int>(new Error("X", "msg"));

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be("X");
    }

    [Fact]
    public void Error_BlankCode_ShouldThrow()
    {
        var act = () => new Error("  ", "msg");

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void Error_BlankMessage_ShouldThrow()
    {
        var act = () => new Error("X", "  ");

        act.Should().Throw<ArgumentException>().WithParameterName("message");
    }

    [Fact]
    public void Error_ToString_WithField_ShouldIncludeField()
    {
        var error = new Error("X.Slug", "Boş olamaz.", "Slug");

        error.ToString().Should().Be("[X.Slug] (Slug) Boş olamaz.");
    }

    [Fact]
    public void Error_ToString_WithoutField_ShouldOmitField()
    {
        var error = new Error("X.General", "Genel hata.");

        error.ToString().Should().Be("[X.General] Genel hata.");
    }
}
