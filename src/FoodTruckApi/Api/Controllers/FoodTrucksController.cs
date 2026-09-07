using FoodTruckApi.Api.Contracts;
using FoodTruckApi.Api.Validation;
using FoodTruckApi.Application.FindFoodTrucks;
using Microsoft.AspNetCore.Mvc;

namespace FoodTruckApi.Api.Controllers;

[ApiController]
[Route("api/food-trucks")]
[Produces("application/json")]
public sealed class FoodTrucksController : ControllerBase
{
    private readonly FindFoodTrucksRequestValidator _validator;
    private readonly FindFoodTrucksHandler _handler;

    public FoodTrucksController(FindFoodTrucksRequestValidator validator, FindFoodTrucksHandler handler)
    {
        _validator = validator;
        _handler = handler;
    }

    /// <summary>Find approved San Francisco food trucks near a location, ordered by distance.</summary>
    /// <remarks>
    /// The dataset is a fixed snapshot of the SF Mobile Food Facility Permit list, limited
    /// to approved trucks that have a valid location.
    /// </remarks>
    /// <response code="200">Matching trucks, nearest first. May be empty.</response>
    /// <response code="400">One or more query parameters were missing or out of range.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpGet]
    [ProducesResponseType(typeof(FindFoodTrucksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public ActionResult<FindFoodTrucksResponse> Get([FromQuery] FindFoodTrucksRequest request)
    {
        var query = _validator.Validate(request);
        if (query.IsFailure)
        {
            return query.ToProblem(this);
        }

        var result = _handler.Handle(query.Value);
        if (result.IsFailure)
        {
            return result.ToProblem(this);
        }

        return Ok(FoodTruckResponseMapper.ToResponse(query.Value, result.Value));
    }
}
