using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.AlbumTemplates;

/// <summary>
/// 相册模板查询与创建接口。
/// </summary>
[Route("api/album-templates")]
public sealed class AlbumTemplatesController(IAlbumTemplateService albumTemplateService) : ApiControllerBase
{
    /// <summary>
    /// 分页查询相册模板列表。
    /// </summary>
    /// <remarks>
    /// 可结合 `Keyword`、`BookType`、`PageType` 和 `Category` 做筛选。推荐前台场景只查询 `IsActive=true` 的模板。
    /// </remarks>
    /// <param name="request">列表查询参数，包含关键字、模板分类和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页模板列表。</response>
    /// <response code="400">查询参数非法。</response>
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

    /// <summary>
    /// 获取单个相册模板详情。
    /// </summary>
    /// <remarks>
    /// 返回模板完整结构与预览配置，适合在模板详情页或编辑器载入模板时调用。
    /// </remarks>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回模板详情。</response>
    /// <response code="404">模板不存在。</response>
    /// <response code="400">模板编码非法。</response>
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

    /// <summary>
    /// 创建相册模板。
    /// </summary>
    /// <remarks>
    /// `TemplateCode` 建议使用稳定且可读的唯一编码。`JsonSource` 应保存模板页面结构定义，便于后续渲染与编辑。
    /// </remarks>
    /// <param name="request">创建模板请求体，包含模板编码、名称、页面类型、模板 JSON 和排序信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">模板创建成功。</response>
    /// <response code="400">请求数据不合法。</response>
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
}