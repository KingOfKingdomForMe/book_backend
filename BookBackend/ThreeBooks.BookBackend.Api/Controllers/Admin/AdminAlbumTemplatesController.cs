using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

[Authorize(Policy = BookBackendApiAuthorizationPolicies.TemplateManage)]
[Route("api/admin/album-templates")]
public sealed class AdminAlbumTemplatesController(IAlbumTemplateService albumTemplateService) : AdminApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AlbumTemplateListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AlbumTemplateListItemResponse>>> GetListAsync(
        [FromQuery] ListAlbumTemplatesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumTemplateService.GetListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{templateCode}")]
    [ProducesResponseType<AlbumTemplateDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumTemplateDetailResponse>> GetDetailAsync(
        string templateCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumTemplateService.GetDetailAsync(templateCode, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost]
    [ProducesResponseType<CreateAlbumTemplateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAlbumTemplateResponse>> CreateAsync(
        [FromBody] CreateAlbumTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumTemplateService.CreateAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{templateCode}")]
    [ProducesResponseType<AlbumTemplateDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumTemplateDetailResponse>> UpdateAsync(
        string templateCode,
        [FromBody] UpdateAlbumTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumTemplateService.UpdateAsync(templateCode, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}