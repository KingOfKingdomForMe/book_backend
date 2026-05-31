using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.DefaultAlbums;

/// <summary>
/// 默认相册查询接口。
/// </summary>
[Route("api/default-albums")]
public sealed class DefaultAlbumsController(IDefaultAlbumService defaultAlbumService) : ApiControllerBase
{
    /// <summary>
    /// 分页查询默认相册列表。
    /// </summary>
    /// <remarks>
    /// 前台默认只返回启用中的默认相册，可结合 `Keyword`、`BookType` 和 `Category` 筛选。
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<PagedResult<DefaultAlbumListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DefaultAlbumListItemResponse>>> GetListAsync(
        [FromQuery] ListDefaultAlbumsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedRequest = request with { IsActive = true };
            var response = await defaultAlbumService.GetListAsync(normalizedRequest, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取单个默认相册详情。
    /// </summary>
    /// <remarks>
    /// 返回默认相册基础信息以及按排序组织的模板内容列表。
    /// </remarks>
    [HttpGet("{albumCode}")]
    [ProducesResponseType<DefaultAlbumDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DefaultAlbumDetailResponse>> GetDetailAsync(
        string albumCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await defaultAlbumService.GetDetailAsync(albumCode, BuildRequestContext(), cancellationToken);
            return response is null || !response.IsActive ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}