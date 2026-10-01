using SecurePasswordManager.Core.Data;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Core.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IDatabaseService _database;

    public CategoryRepository(IDatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.Connection.Table<Category>()
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.Connection.Table<Category>()
            .Where(category => category.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<Category> GetOrCreateAsync(string name, string colorHex = "#64748B", string iconGlyph = "+", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = name.Trim();
        var existing = await _database.Connection.Table<Category>()
            .Where(category => category.Name == normalized)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            return existing;
        }

        var category = new Category
        {
            Name = normalized,
            ColorHex = colorHex,
            IconGlyph = iconGlyph,
            IsDefault = false
        };
        await SaveAsync(category, cancellationToken);
        return category;
    }

    public async Task SaveAsync(Category category, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        category.UpdatedAt = DateTime.UtcNow;

        var existing = await GetByIdAsync(category.Id, cancellationToken);
        if (existing is null)
        {
            category.CreatedAt = DateTime.UtcNow;
            await _database.Connection.InsertAsync(category);
            return;
        }

        await _database.Connection.UpdateAsync(category);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        if (existing.IsDefault)
        {
            throw new InvalidOperationException("Default categories cannot be deleted.");
        }

        await _database.Connection.DeleteAsync(existing);
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await GetAllAsync(cancellationToken);
        foreach (var category in DefaultCategories.Create())
        {
            if (existing.Any(item => string.Equals(item.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            await _database.Connection.InsertAsync(category);
        }
    }
}
