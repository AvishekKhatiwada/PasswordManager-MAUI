using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Tests;

public sealed class PasswordGeneratorServiceTests
{
    [Fact]
    public void Generate_RespectsLengthAndCharacterOptions()
    {
        var service = new PasswordGeneratorService();

        var result = service.Generate(new PasswordGeneratorOptions
        {
            Length = 32,
            UseUppercase = true,
            UseLowercase = true,
            UseNumbers = true,
            UseSymbols = false
        });

        Assert.Equal(32, result.Value.Length);
        Assert.Contains(result.Value, char.IsUpper);
        Assert.Contains(result.Value, char.IsLower);
        Assert.Contains(result.Value, char.IsDigit);
        Assert.True(result.EntropyBits > 100);
    }

    [Fact]
    public void Generate_CreatesPassphrase()
    {
        var service = new PasswordGeneratorService();

        var result = service.Generate(new PasswordGeneratorOptions { PassphraseMode = true, PassphraseWords = 6 });

        Assert.Equal(6, result.Value.Split('-').Length);
        Assert.True(result.EntropyBits > 20);
    }
}
