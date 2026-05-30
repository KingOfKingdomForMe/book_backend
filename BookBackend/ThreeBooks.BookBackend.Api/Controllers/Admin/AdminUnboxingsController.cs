using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Unboxings.Requests;
using ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

/// <summary>
/// 后台晒单审核与维护接口。
/// </summary>
[Authorize(Policy = BookBackendApiAuthorizationPolicies.UnboxingModerate)]
[Route("api/admin/unboxings")]
public sealed class AdminUnboxingsController(IUnboxingService unboxingService) : AdminApiControllerBase
{
    /// <summary>
    /// 分页查询后台晒单列表。
    /// </summary>
    /// <remarks>
    /// 可按关键字、等级、精选标记与状态筛选，适合运营审核台或内容管理列表。
    /// </remarks>
    /// <param name="request">后台晒单查询参数，包含筛选条件和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回后台晒单分页列表。</response>
    /// <response code="400">查询参数非法。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUnboxingListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AdminUnboxingListItemResponse>>> GetListAsync(
        [FromQuery] AdminListUnboxingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetAdminListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取后台晒单详情。
    /// </summary>
    /// <remarks>
    /// 适用于审核时查看完整内容、媒体资源和业务关联信息。
    /// </remarks>
    /// <param name="postNo">晒单业务编号。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回后台晒单详情。</response>
    /// <response code="404">晒单不存在。</response>
    /// <response code="400">业务编号非法。</response>
    [HttpGet("{postNo}")]
    [ProducesResponseType<AdminUnboxingDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminUnboxingDetailResponse>> GetDetailAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetAdminDetailAsync(postNo, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 更新晒单内容或审核状态。
    /// </summary>
    /// <remarks>
    /// 该接口可同时用于运营修正文案、调整精选标记和审核上下架状态。建议客户端提交完整目标状态，避免覆盖不全。
    /// </remarks>
    /// <param name="postNo">晒单业务编号。</param>
    /// <param name="request">晒单更新请求体，包含正文、媒体、标签与状态字段。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">更新成功。</response>
    /// <response code="404">目标晒单不存在。</response>
    /// <response code="400">请求数据不合法。</response>
    [HttpPut("{postNo}")]
    [ProducesResponseType<SaveUnboxingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveUnboxingResponse>> UpdateAsync(
        string postNo,
        [FromBody] UpdateUnboxingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.UpdateAsync(postNo, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 删除指定晒单。
    /// </summary>
    /// <remarks>
    /// 删除后内容将不再对前台可见，建议仅在确认违规或无效数据时调用。
    /// </remarks>
    /// <param name="postNo">晒单业务编号。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">删除成功。</response>
    /// <response code="404">晒单不存在。</response>
    /// <response code="400">业务编号非法。</response>
    [HttpDelete("{postNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await unboxingService.DeleteAsync(postNo, BuildRequestContext(), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}