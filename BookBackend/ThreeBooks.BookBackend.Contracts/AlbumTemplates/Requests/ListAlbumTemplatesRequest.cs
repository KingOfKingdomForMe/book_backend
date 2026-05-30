namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;

/// <summary>
/// 相册模板列表查询参数。
/// </summary>
/// <param name="Keyword">关键字，通常匹配模板名称或描述。</param>
/// <param name="BookType">成册类型编码。</param>
/// <param name="PageType">页面类型编码。</param>
/// <param name="Category">模板分类编码。</param>
/// <param name="IsActive">是否只查询启用模板。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListAlbumTemplatesRequest(
	string? Keyword,
	string? BookType = null,
	string? PageType = null,
	string? Category = null,
	bool? IsActive = true,
	int PageNumber = 1,
	int PageSize = 20);