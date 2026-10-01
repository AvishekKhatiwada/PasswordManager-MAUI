using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Category> GetOrCreateAsync(string name, string colorHex = "#64748B", string iconGlyph = "+", CancellationToken cancellationToken = default);

    Task SaveAsync(Category category, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
}
