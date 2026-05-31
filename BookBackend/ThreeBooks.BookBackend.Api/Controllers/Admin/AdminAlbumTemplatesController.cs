using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

/// <summary>
/// 后台相册模板管理接口。
/// </summary>
[Authorize(Policy = BookBackendApiAuthorizationPolicies.TemplateManage)]
[Route("api/admin/album-templates")]
public sealed class AdminAlbumTemplatesController(IAlbumTemplateService albumTemplateService) : AdminApiControllerBase
{
    /// <summary>
    /// 分页查询后台相册模板列表。
    /// </summary>
    /// <remarks>
    /// 与前台模板列表接口类似，但更适合后台运营查看全部模板，包括未启用模板。
    /// </remarks>
    /// <param name="request">模板查询参数，包含筛选条件和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回模板分页列表。</response>
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
    /// 获取后台相册模板详情。
    /// </summary>
    /// <remarks>
    /// 适用于模板编辑器回填数据，通常会返回完整模板 JSON 结构与相关元信息。
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
    /// 创建后台相册模板。
    /// </summary>
    /// <remarks>
    /// 建议在保存前校验 `TemplateCode` 唯一性，以及 `JsonSource` 是否与前端模板编辑器导出的结构一致。
    /// </remarks>
    /// <param name="request">模板创建请求体，包含模板编码、展示信息、页面类型和模板定义 JSON。</param>
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

    /// <summary>
    /// 更新后台相册模板。
    /// </summary>
    /// <remarks>
    /// 适用于模板内容迭代、上下架和排序调整。更新时应提交模板完整目标状态。
    /// </remarks>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="request">模板更新请求体，包含模板名称、分类、模板 JSON、启用状态和排序。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">更新成功。</response>
    /// <response code="404">模板不存在。</response>
    /// <response code="400">请求数据不合法。</response>
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

    /// <summary>
    /// 删除指定后台相册模板。
    /// </summary>
    /// <remarks>
    /// 仅删除相册模板模块管理的模板记录，并同步清理默认相册中的模板关联。
    /// </remarks>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">删除成功。</response>
    /// <response code="404">模板不存在。</response>
    /// <response code="400">模板编码非法。</response>
    [HttpDelete("{templateCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAsync(
        string templateCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await albumTemplateService.DeleteAsync(templateCode, BuildRequestContext(), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 删除全部后台相册模板。
    /// </summary>
    /// <remarks>
    /// 仅清理相册模板模块管理的模板记录，并同步移除默认相册中的模板关联；封面模板由独立接口管理，不会在这里被删除。
    /// </remarks>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">删除完成。</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllAsync(CancellationToken cancellationToken)
    {
        await albumTemplateService.DeleteAllAsync(BuildRequestContext(), cancellationToken);
        return NoContent();
    }
}