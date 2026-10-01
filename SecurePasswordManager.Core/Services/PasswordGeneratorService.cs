using System.Security.Cryptography;
using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

/// <summary>
/// Cryptographically secure password and passphrase generator.
/// </summary>
public sealed class PasswordGeneratorService : IPasswordGeneratorService
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Numbers = "23456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?";
    private const string Ambiguous = "0O1Il|`'\"";

    private static readonly string[] PassphraseWords =
    [
        "atlas", "bright", "cedar", "delta", "ember", "fable", "glacier", "harbor",
        "island", "juno", "kepler", "lantern", "meadow", "nova", "onyx", "prairie",
        "quartz", "river", "signal", "thunder", "umbra", "violet", "willow", "zenith"
    ];

    public GeneratedPassword Generate(PasswordGeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.PassphraseMode)
        {
            var count = Math.Clamp(options.PassphraseWords, 3, 12);
            var words = Enumerable.Range(0, count)
                .Select(_ => PassphraseWords[RandomNumberGenerator.GetInt32(PassphraseWords.Length)]);
            var passphrase = string.Join("-", words);
            return EvaluateWithEntropy(passphrase, count * Math.Log2(PassphraseWords.Length));
        }

        var pools = new List<string>();
        if (options.UseUppercase)
        {
            pools.Add(Uppercase);
        }

        if (options.UseLowercase)
        {
            pools.Add(Lowercase);
        }

        if (options.UseNumbers)
        {
            pools.Add(Numbers);
        }

        if (options.UseSymbols)
        {
            pools.Add(Symbols);
        }

        if (pools.Count == 0)
        {
            pools.Add(Lowercase);
        }

        var requiredChars = pools.Select(pool => pool[RandomNumberGenerator.GetInt32(pool.Length)]).ToList();
        var characterSet = string.Concat(pools);
        if (!options.ExcludeAmbiguous)
        {
            characterSet += Ambiguous;
        }

        var length = Math.Clamp(options.Length, Math.Max(8, requiredChars.Count), 128);
        var chars = new List<char>(requiredChars);
        while (chars.Count < length)
        {
            chars.Add(characterSet[RandomNumberGenerator.GetInt32(characterSet.Length)]);
        }

        Shuffle(chars);
        var password = new string(chars.ToArray());
        return EvaluateWithEntropy(password, length * Math.Log2(characterSet.Distinct().Count()));
    }

    public GeneratedPassword Evaluate(string password)
    {
        password ??= string.Empty;
        var pool = 0;
        if (password.Any(char.IsLower))
        {
            pool += 26;
        }

        if (password.Any(char.IsUpper))
        {
            pool += 26;
        }

        if (password.Any(char.IsDigit))
        {
            pool += 10;
        }

        if (password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            pool += 32;
        }

        var entropy = password.Length * Math.Log2(Math.Max(pool, 1));
        return EvaluateWithEntropy(password, entropy);
    }

    private static GeneratedPassword EvaluateWithEntropy(string value, double entropy)
    {
        var score = Math.Clamp(entropy / 100d, 0d, 1d);
        var label = entropy switch
        {
            >= 100 => "Excellent",
            >= 75 => "Strong",
            >= 50 => "Fair",
            _ => "Weak"
        };

        return new GeneratedPassword
        {
            Value = value,
            EntropyBits = Math.Round(entropy, 1),
            StrengthScore = score,
            StrengthLabel = label
        };
    }

    private static void Shuffle(IList<char> chars)
    {
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
