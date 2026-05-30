using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Unboxings.Requests;
using ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Unboxings;

/// <summary>
/// 用户晒单内容发布与查询接口。
/// </summary>
[Route("api/unboxings")]
public sealed class UnboxingsController(IUnboxingService unboxingService) : ApiControllerBase
{
    /// <summary>
    /// 发布新的晒单内容。
    /// </summary>
    /// <remarks>
    /// 适合用户提交图文晒单。`Media` 建议按展示顺序传入，`Tags` 可用于前端搜索和聚合展示。
    /// </remarks>
    /// <param name="request">晒单创建请求体，包含标题、正文、作者信息、关联商品、媒体资源和标签。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">晒单创建成功。</response>
    /// <response code="400">请求数据不合法。</response>
    [HttpPost]
    [ProducesResponseType<SaveUnboxingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveUnboxingResponse>> CreateAsync(
        [FromBody] CreateUnboxingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.CreateAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 分页查询晒单列表。
    /// </summary>
    /// <remarks>
    /// 可按 `LevelCode` 和是否精选筛选。列表页通常只需要基础卡片信息，建议通过分页逐步加载。
    /// </remarks>
    /// <param name="request">晒单列表查询参数，包含等级、精选标记和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页晒单列表。</response>
    /// <response code="400">查询参数非法。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<UnboxingListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UnboxingListItemResponse>>> GetListAsync(
        [FromQuery] ListUnboxingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 更新已有晒单内容。
    /// </summary>
    /// <remarks>
    /// `postNo` 为晒单业务编号。更新时应提交完整的正文、媒体与标签目标状态，避免前后端状态不一致。
    /// </remarks>
    /// <param name="postNo">晒单业务编号。</param>
    /// <param name="request">晒单更新请求体，字段含义与创建接口一致。</param>
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
    /// 获取晒单等级定义列表。
    /// </summary>
    /// <remarks>
    /// 可用于前端筛选项、标签展示或发帖时选择晒单等级。
    /// </remarks>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回全部晒单等级定义。</response>
    [HttpGet("levels")]
    [ProducesResponseType<IReadOnlyCollection<UnboxingLevelResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UnboxingLevelResponse>>> GetLevelsAsync(
        CancellationToken cancellationToken)
    {
        var response = await unboxingService.GetLevelsAsync(BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 获取晒单详情。
    /// </summary>
    /// <remarks>
    /// 适用于晒单详情页加载。若详情页需要评论、点赞等附加数据，可在前端再组合其它接口结果。
    /// </remarks>
    /// <param name="postNo">晒单业务编号。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回晒单详情。</response>
    /// <response code="404">晒单不存在。</response>
    /// <response code="400">业务编号不合法。</response>
    [HttpGet("{postNo}")]
    [ProducesResponseType<UnboxingDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UnboxingDetailResponse>> GetDetailAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await unboxingService.GetDetailAsync(postNo, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}