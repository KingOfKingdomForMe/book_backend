using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Catalogs;

[Route("api/bundles")]
public sealed class BundlesController(ICatalogService catalogService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<BundleListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BundleListItemResponse>>> GetListAsync(
        [FromQuery] ListBundlesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetBundlesAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }
}
