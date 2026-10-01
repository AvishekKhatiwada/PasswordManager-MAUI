using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

public interface IPasswordGeneratorService
{
    GeneratedPassword Generate(PasswordGeneratorOptions options);

    GeneratedPassword Evaluate(string password);
}
