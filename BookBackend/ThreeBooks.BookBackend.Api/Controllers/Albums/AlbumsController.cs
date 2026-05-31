using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Contracts.Albums.Requests;
using ThreeBooks.BookBackend.Contracts.Albums.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Albums;

/// <summary>
/// 相册创建、编辑与分享访问接口。
/// </summary>
[Route("api/albums")]
public sealed class AlbumsController(IAlbumService albumService) : ApiControllerBase
{
    /// <summary>
    /// 分页获取用户已创建的相册列表。
    /// </summary>
    /// <remarks>
    /// 当前项目内相册归属仍按业务 `UserId` 查询。前端应传当前业务用户标识，而不是登录服务 JWT 中的 GUID 主键。
    /// </remarks>
    /// <param name="request">相册列表查询参数，包含业务用户标识和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页相册列表。</response>
    /// <response code="400">查询参数非法。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<AlbumListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AlbumListItemResponse>>> GetListAsync(
        [FromQuery] ListAlbumsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetListAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 创建新的用户相册。
    /// </summary>
    /// <remarks>
    /// 适用于用户首次生成作品集。若未显式传入 `ShareCode`，通常建议由服务端生成，避免客户端自行维护冲突风险。
    /// </remarks>
    /// <param name="request">创建相册请求体，包含所属用户、标题、副标题和公开状态等信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">相册创建成功。</response>
    /// <response code="400">请求参数不合法，例如标题缺失或用户标识非法。</response>
    [HttpPost]
    [ProducesResponseType<CreateAlbumResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAlbumResponse>> CreateAsync(
        [FromBody] CreateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.CreateAlbumAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 保存相册页面内容。
    /// </summary>
    /// <remarks>
    /// 建议前端在编辑器中按页组织内容后整体提交。`Pages` 中的 `JsonSource` 应保存页面完整结构，图片资源使用已上传文件的 `FileId` 关联。
    /// </remarks>
    /// <param name="shareCode">相册分享码，用于标识要保存的相册。</param>
    /// <param name="request">页面保存请求体，包含页面集合、页面布局 JSON 和关联素材信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">页面保存成功。</response>
    /// <response code="404">指定分享码对应的相册不存在。</response>
    /// <response code="400">页面数据格式不合法。</response>
    [HttpPut("{shareCode}/pages")]
    [ProducesResponseType<SaveAlbumPagesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaveAlbumPagesResponse>> SavePagesAsync(
        string shareCode,
        [FromBody] SaveAlbumPagesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.SavePagesAsync(shareCode, request, BuildRequestContext(), cancellationToken);
            return response is null
                ? NotFound()
                : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取相册预览详情。
    /// </summary>
    /// <remarks>
    /// 适合在分享页或预览页首屏加载时调用，返回相册元数据以及预览所需的整体信息。
    /// </remarks>
    /// <param name="shareCode">相册分享码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回相册预览详情。</response>
    /// <response code="404">相册不存在或不可访问。</response>
    /// <response code="400">分享码格式不合法。</response>
    [HttpGet("{shareCode}")]
    [ProducesResponseType<AlbumPreviewDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumPreviewDetailResponse>> GetPreviewAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetPreviewAsync(shareCode, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取相册指定页的预览内容。
    /// </summary>
    /// <remarks>
    /// 适用于分页懒加载场景。`pageNumber` 建议从 1 开始传递，并与前端编辑器或阅读器的页码体系保持一致。
    /// </remarks>
    /// <param name="shareCode">相册分享码。</param>
    /// <param name="pageNumber">要读取的页码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回目标页内容。</response>
    /// <response code="404">相册或页面不存在。</response>
    /// <response code="400">分享码或页码非法。</response>
    [HttpGet("{shareCode}/pages/{pageNumber:int}")]
    [ProducesResponseType<AlbumPreviewPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumPreviewPageResponse>> GetPageAsync(
        string shareCode,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetPageAsync(shareCode, pageNumber, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取相册分享页所需的摘要信息。
    /// </summary>
    /// <remarks>
    /// 适用于生成分享卡片、社交分享标题与封面时调用，避免拉取完整相册数据。
    /// </remarks>
    /// <param name="shareCode">相册分享码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回相册分享摘要信息。</response>
    /// <response code="404">相册不存在。</response>
    /// <response code="400">分享码不合法。</response>
    [HttpGet("{shareCode}/share-info")]
    [ProducesResponseType<AlbumPreviewShareInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlbumPreviewShareInfoResponse>> GetShareInfoAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await albumService.GetShareInfoAsync(shareCode, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 记录相册浏览行为。
    /// </summary>
    /// <remarks>
    /// 建议在用户进入相册或切换页面时调用。`pageNumber` 可为空，为空时表示记录整本相册的访问。
    /// </remarks>
    /// <param name="shareCode">相册分享码。</param>
    /// <param name="pageNumber">被浏览的页码，可为空。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">浏览记录写入成功。</response>
    /// <response code="404">相册不存在。</response>
    /// <response code="400">参数不合法。</response>
    [HttpPost("{shareCode}/views")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordViewAsync(
        string shareCode,
        [FromQuery] int? pageNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var recorded = await albumService.RecordViewAsync(shareCode, pageNumber, BuildRequestContext(), cancellationToken);
            return recorded ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 记录相册分享行为。
    /// </summary>
    /// <remarks>
    /// `Channel` 建议使用稳定的渠道编码，例如 `wechat_session`、`wechat_timeline`。该接口适合在用户触发分享动作后异步调用。
    /// </remarks>
    /// <param name="shareCode">相册分享码。</param>
    /// <param name="request">分享记录请求体，包含分享渠道编码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">分享行为记录成功。</response>
    /// <response code="404">相册不存在。</response>
    /// <response code="400">参数不合法。</response>
    [HttpPost("{shareCode}/shares")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordShareAsync(
        string shareCode,
        [FromBody] RecordAlbumShareRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var recorded = await albumService.RecordShareAsync(shareCode, request.Channel, BuildRequestContext(), cancellationToken);
            return recorded ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}