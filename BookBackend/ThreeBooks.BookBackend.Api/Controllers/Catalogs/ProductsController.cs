using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Catalogs;

[Route("api/products")]
public sealed class ProductsController(ICatalogService catalogService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListItemResponse>>> GetListAsync(
        [FromQuery] ListProductsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetProductsAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailResponse>> GetDetailAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetProductDetailAsync(id, BuildRequestContext(), cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }
}
