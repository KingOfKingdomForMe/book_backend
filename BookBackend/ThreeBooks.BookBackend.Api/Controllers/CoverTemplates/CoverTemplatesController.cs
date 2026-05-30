using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.CoverTemplates;

/// <summary>
/// 封面模板查询与封面图生成接口。
/// </summary>
[Route("api/cover-templates")]
public sealed class CoverTemplatesController(ICoverTemplateService coverTemplateService) : ApiControllerBase
{
    /// <summary>
    /// 分页查询封面模板列表。
    /// </summary>
    /// <remarks>
    /// 通常用于封面模板选择器。可以通过 `Keyword` 与 `IsActive` 控制展示范围，并配合分页参数做滚动加载。
    /// </remarks>
    /// <param name="request">模板列表查询参数，包含关键字、启用状态和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页模板列表。</response>
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
    /// 获取单个封面模板详情。
    /// </summary>
    /// <remarks>
    /// 返回背景图、字段布局等完整信息，适合在封面编辑器初始化时调用。
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
    /// 根据封面模板与字段值生成封面图。
    /// </summary>
    /// <remarks>
    /// `FieldValues` 应与模板中的字段定义一一对应。若希望结果可长期访问，建议显式传入目标 `Bucket`、`Directory` 与 `FileName`。
    /// </remarks>
    /// <param name="templateCode">要使用的封面模板编码。</param>
    /// <param name="request">生成请求体，包含字段值以及可选的目标存储位置。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">封面图生成成功。</response>
    /// <response code="404">模板不存在。</response>
    /// <response code="400">字段值不合法或缺失必填字段。</response>
    /// <response code="503">封面生成依赖的文件或图形服务当前不可用。</response>
    [HttpPost("{templateCode}/generate")]
    [ProducesResponseType<GeneratedCoverImageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GeneratedCoverImageResponse>> GenerateAsync(
        string templateCode,
        [FromBody] GenerateCoverImageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await coverTemplateService.GenerateAsync(templateCode, request, BuildRequestContext(), cancellationToken);
            return response is null
                ? NotFound()
                : StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Cover generation is unavailable.");
        }
    }
}