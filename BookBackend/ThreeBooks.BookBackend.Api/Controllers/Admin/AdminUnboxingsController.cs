using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Unboxings.Requests;
using ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

[Authorize(Policy = BookBackendApiAuthorizationPolicies.UnboxingModerate)]
[Route("api/admin/unboxings")]
public sealed class AdminUnboxingsController(IUnboxingService unboxingService) : AdminApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUnboxingListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AdminUnboxingListItemResponse>>> GetListAsync(
        [FromQuery] AdminListUnboxingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetAdminListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{postNo}")]
    [ProducesResponseType<AdminUnboxingDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUnboxingDetailResponse>> GetDetailAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetAdminDetailAsync(postNo, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
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

    [HttpDelete("{postNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await unboxingService.DeleteAsync(postNo, BuildRequestContext(), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}