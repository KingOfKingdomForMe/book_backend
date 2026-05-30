using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

/// <summary>
/// 后台封面模板管理接口。
/// </summary>
[Authorize(Policy = BookBackendApiAuthorizationPolicies.TemplateManage)]
[Route("api/admin/cover-templates")]
public sealed class AdminCoverTemplatesController(ICoverTemplateService coverTemplateService) : AdminApiControllerBase
{
    /// <summary>
    /// 分页查询后台封面模板列表。
    /// </summary>
    /// <remarks>
    /// 适用于后台封面模板管理页，可查看全部模板并按关键字筛选。
    /// </remarks>
    /// <param name="request">封面模板查询参数，包含关键字、启用状态和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回模板分页列表。</response>
    /// <response code="400">查询参数非法。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<CoverTemplateListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CoverTemplateListItemResponse>>> GetListAsync(
        [FromQuery] ListCoverTemplatesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await coverTemplateService.GetListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取后台封面模板详情。
    /// </summary>
    /// <remarks>
    /// 用于模板编辑页面初始化，返回字段布局、背景图和状态信息。
    /// </remarks>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回模板详情。</response>
    /// <response code="404">模板不存在。</response>
    /// <response code="400">模板编码非法。</response>
    [HttpGet("{templateCode}")]
    [ProducesResponseType<CoverTemplateDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CoverTemplateDetailResponse>> GetDetailAsync(
        string templateCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await coverTemplateService.GetDetailAsync(templateCode, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 创建后台封面模板。
    /// </summary>
    /// <remarks>
    /// 建议先上传背景图，再把背景图文件标识填入 `BackgroundFileId`。字段定义中的坐标与尺寸应与设计稿保持同一单位体系。
    /// </remarks>
    /// <param name="request">封面模板创建请求体，包含模板编码、名称、背景图、字段布局和启用状态。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">模板创建成功。</response>
    /// <response code="400">请求数据不合法。</response>
    [HttpPost]
    [ProducesResponseType<CreateCoverTemplateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCoverTemplateResponse>> CreateAsync(
        [FromBody] CreateCoverTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await coverTemplateService.CreateAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 更新后台封面模板。
    /// </summary>
    /// <remarks>
    /// 适用于模板字段调整、背景图替换、启用状态和排序更新。建议提交完整字段集合，避免遗漏字段定义。
    /// </remarks>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="request">封面模板更新请求体，包含模板名称、背景图、字段定义、启用状态和排序。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">更新成功。</response>
    /// <response code="404">模板不存在。</response>
    /// <response code="400">请求数据不合法。</response>
    [HttpPut("{templateCode}")]
    [ProducesResponseType<CoverTemplateDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CoverTemplateDetailResponse>> UpdateAsync(
        string templateCode,
        [FromBody] UpdateCoverTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await coverTemplateService.UpdateAsync(templateCode, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}