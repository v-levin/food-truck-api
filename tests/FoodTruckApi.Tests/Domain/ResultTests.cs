using FoodTruckApi.Domain.Common;

namespace FoodTruckApi.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_result_has_no_errors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_result_exposes_its_error()
    {
        var error = Error.Validation("some.code", "some message");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Equal(new[] { error }, result.Errors);
    }

    [Fact]
    public void Failure_result_keeps_every_error()
    {
        var errors = new[]
        {
            Error.Validation("a", "first"),
            Error.Validation("b", "second"),
        };

        var result = Result.Failure<int>(errors);

        Assert.Equal(errors, result.Errors);
        Assert.Equal(errors[0], result.Error);
    }

    [Fact]
    public void Generic_success_carries_the_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_throws()
    {
        var result = Result.Failure<int>(Error.Unexpected("x", "boom"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Value_implicitly_converts_to_a_success_result()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Success_cannot_be_constructed_with_errors()
    {
        Assert.Throws<InvalidOperationException>(() => new Result<int>(0, isSuccess: true,
            new[] { Error.Validation("a", "b") }));
    }
}
