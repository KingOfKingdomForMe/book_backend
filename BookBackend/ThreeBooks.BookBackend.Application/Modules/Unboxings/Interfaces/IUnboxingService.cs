using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Unboxings.Requests;
using ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;

public interface IUnboxingService
{
    Task<SaveUnboxingResponse> CreateAsync(
        CreateUnboxingRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<SaveUnboxingResponse?> UpdateAsync(
        string postNo,
        UpdateUnboxingRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<PagedResult<UnboxingListItemResponse>> GetListAsync(
        ListUnboxingsRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<UnboxingDetailResponse?> GetDetailAsync(
        string postNo,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UnboxingLevelResponse>> GetLevelsAsync(
        RequestContext context,
        CancellationToken cancellationToken);
}