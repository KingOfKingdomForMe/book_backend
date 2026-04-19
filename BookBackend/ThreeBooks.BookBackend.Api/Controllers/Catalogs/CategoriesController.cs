using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Catalogs;

[Route("api/categories")]
public sealed class CategoriesController(ICatalogService catalogService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> GetListAsync(
        [FromQuery] ListCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetCategoriesAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }
}
