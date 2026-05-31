using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

/// <summary>
/// 后台默认相册管理接口。
/// </summary>
[Authorize(Policy = BookBackendApiAuthorizationPolicies.TemplateManage)]
[Route("api/admin/default-albums")]
public sealed class AdminDefaultAlbumsController(IDefaultAlbumService defaultAlbumService) : AdminApiControllerBase
{
    /// <summary>
    /// 分页查询后台默认相册列表。
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<DefaultAlbumListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DefaultAlbumListItemResponse>>> GetListAsync(
        [FromQuery] ListDefaultAlbumsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await defaultAlbumService.GetListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取后台默认相册详情。
    /// </summary>
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
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 创建后台默认相册。
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CreateDefaultAlbumResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateDefaultAlbumResponse>> CreateAsync(
        [FromBody] CreateDefaultAlbumRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await defaultAlbumService.CreateAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 更新后台默认相册。
    /// </summary>
    [HttpPut("{albumCode}")]
    [ProducesResponseType<DefaultAlbumDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DefaultAlbumDetailResponse>> UpdateAsync(
        string albumCode,
        [FromBody] UpdateDefaultAlbumRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await defaultAlbumService.UpdateAsync(albumCode, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}