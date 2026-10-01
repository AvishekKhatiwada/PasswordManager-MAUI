using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.ViewModels;

public sealed record QuickAddShortcut(string Label, string Glyph, VaultItemKind Kind);
