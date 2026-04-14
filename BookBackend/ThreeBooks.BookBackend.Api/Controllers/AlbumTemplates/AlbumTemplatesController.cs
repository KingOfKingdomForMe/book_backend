using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.AlbumTemplates;

[Route("api/album-templates")]
public sealed class AlbumTemplatesController(IAlbumTemplateService albumTemplateService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AlbumTemplateListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AlbumTemplateListItemResponse>>> GetListAsync(
        [FromQuery] ListAlbumTemplatesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await albumTemplateService.GetListAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }
}