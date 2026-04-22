using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Contracts.Albums.Requests;
using ThreeBooks.BookBackend.Contracts.Albums.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Albums;

[Route("api/albums")]
public sealed class AlbumsController(IAlbumService albumService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateAlbumResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAlbumResponse>> CreateAsync(
        [FromBody] CreateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.CreateAlbumAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{projectId:long}/pages")]
    [ProducesResponseType<CreateAlbumPageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAlbumPageResponse>> AddPageAsync(
        long projectId,
        [FromBody] CreateAlbumPageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.AddPageAsync(projectId, request, BuildRequestContext(), cancellationToken);
            return response is null
                ? NotFound()
                : StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{shareCode}")]
    [ProducesResponseType<AlbumPreviewDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumPreviewDetailResponse>> GetPreviewAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetPreviewAsync(shareCode, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{shareCode}/pages/{pageNumber:int}")]
    [ProducesResponseType<AlbumPreviewPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumPreviewPageResponse>> GetPageAsync(
        string shareCode,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetPageAsync(shareCode, pageNumber, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{shareCode}/share-info")]
    [ProducesResponseType<AlbumPreviewShareInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumPreviewShareInfoResponse>> GetShareInfoAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetShareInfoAsync(shareCode, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{shareCode}/views")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordViewAsync(
        string shareCode,
        [FromQuery] int? pageNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var recorded = await albumService.RecordViewAsync(shareCode, pageNumber, BuildRequestContext(), cancellationToken);
            return recorded ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{shareCode}/shares")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordShareAsync(
        string shareCode,
        [FromBody] RecordAlbumShareRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var recorded = await albumService.RecordShareAsync(shareCode, request.Channel, BuildRequestContext(), cancellationToken);
            return recorded ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}