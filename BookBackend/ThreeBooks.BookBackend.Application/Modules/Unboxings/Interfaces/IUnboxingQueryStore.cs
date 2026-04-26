using ThreeBooks.BookBackend.Application.Modules.Unboxings.Models;

namespace ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;

public interface IUnboxingQueryStore
{
    Task<AdminUnboxingListQueryResultModel> GetAdminListAsync(
        AdminUnboxingListFilter filter,
        CancellationToken cancellationToken);

    Task<AdminUnboxingDetailQueryModel?> GetAdminDetailAsync(
        string postNo,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        string postNo,
        CancellationToken cancellationToken);

    Task<bool> UserExistsAsync(
        long userId,
        CancellationToken cancellationToken);

    Task<bool> PostNoExistsAsync(
        string postNo,
        CancellationToken cancellationToken);

    Task<long> GetCurrentMaxNumericPostNoAsync(
        CancellationToken cancellationToken);

    Task<UnboxingLevelModel?> GetLevelByCodeAsync(
        string levelCode,
        CancellationToken cancellationToken);

    Task<UnboxingWriteResultModel> CreateAsync(
        UnboxingCreateCommandModel command,
        CancellationToken cancellationToken);

    Task<UnboxingWriteResultModel?> UpdateAsync(
        string postNo,
        UnboxingUpdateCommandModel command,
        CancellationToken cancellationToken);

    Task<UnboxingListQueryResultModel> GetListAsync(
        UnboxingListFilter filter,
        CancellationToken cancellationToken);

    Task<UnboxingDetailQueryModel?> GetDetailAsync(
        string postNo,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UnboxingLevelModel>> GetLevelsAsync(
        CancellationToken cancellationToken);
}