using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Unboxings.Requests;
using ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Unboxings;

[Route("api/unboxings")]
public sealed class UnboxingsController(IUnboxingService unboxingService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<SaveUnboxingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveUnboxingResponse>> CreateAsync(
        [FromBody] CreateUnboxingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.CreateAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<UnboxingListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UnboxingListItemResponse>>> GetListAsync(
        [FromQuery] ListUnboxingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{postNo}")]
    [ProducesResponseType<SaveUnboxingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveUnboxingResponse>> UpdateAsync(
        string postNo,
        [FromBody] UpdateUnboxingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.UpdateAsync(postNo, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("levels")]
    [ProducesResponseType<IReadOnlyCollection<UnboxingLevelResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UnboxingLevelResponse>>> GetLevelsAsync(
        CancellationToken cancellationToken)
    {
        var response = await unboxingService.GetLevelsAsync(BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{postNo}")]
    [ProducesResponseType<UnboxingDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UnboxingDetailResponse>> GetDetailAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetDetailAsync(postNo, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}