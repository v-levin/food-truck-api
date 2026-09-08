using FoodTruckApi.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodTruckApi.Api;

/// <summary>
/// The single place where domain failures become HTTP responses. Everything inside the
/// Application/Domain layers stays HTTP-agnostic and just returns a <see cref="Result"/>.
/// </summary>
internal static class ResultActionExtensions
{
    public static ActionResult ToProblem(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("ToProblem was called on a successful result.");
        }

        if (result.Error.Type == ErrorType.Validation)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in result.Errors)
            {
                modelState.AddModelError(error.Code, error.Message);
            }

            return controller.ValidationProblem(modelState);
        }

        return controller.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            detail: result.Error.Message);
    }
}
