# Secure Password Manager

A local-first, zero-knowledge password manager built with .NET 9 MAUI, MVVM, SQLite, and SQLCipher.

## Projects

- `SecurePasswordManager` - .NET MAUI app for Android, iOS, Mac Catalyst, and Windows.
- `SecurePasswordManager.Core` - testable domain, security, repository, database, backup, and generator services.
- `SecurePasswordManager.Tests` - xUnit coverage for encryption, authentication, SQLCipher vault persistence, and password generation.

## Security Model

- The master password is never stored.
- Vault metadata stores only a PBKDF2-SHA512 authentication hash, salt, iteration count, and lockout state.
- PBKDF2 uses `Rfc2898DeriveBytes` with 210,000 iterations by default.
- Derived key material is split into:
  - a 256-bit SQLCipher database key
  - a 256-bit authentication hash
- The database is encrypted with SQLCipher through `SQLitePCLRaw.bundle_e_sqlcipher`.
- Encrypted backups use JSON wrapped in AES-256-GCM with PBKDF2-SHA512.
- In-memory vault session keys are zeroed on lock.
- Biometric unlock stores only the derived database key in the OS secure store; the master password is still never persisted.
- Clipboard copies are automatically cleared after the configured timeout.

## Features

- Onboarding with master-password strength validation.
- Master-password and biometric/device unlock.
- Auto-lock on inactivity and lock on app resume.
- Searchable, categorized credential dashboard.
- Add, edit, duplicate, favorite, archive, soft delete, and restore-ready credential model.
- Secure notes with Markdown-friendly storage.
- Password generator with entropy estimates and passphrase mode.
- Settings for theme, auto-lock, clipboard timeout, biometrics, backup/export, import, reset, and lock.
- SQLCipher-backed repositories with GUID primary keys and audit timestamps.
- Encrypted export/import backups.

## Build

```powershell
dotnet restore SecurePasswordManager.sln
dotnet build SecurePasswordManager\SecurePasswordManager.csproj -f net9.0-windows10.0.19041.0
dotnet build SecurePasswordManager\SecurePasswordManager.csproj -f net9.0-android
dotnet build SecurePasswordManager\SecurePasswordManager.csproj -f net9.0-ios
dotnet test SecurePasswordManager.Tests\SecurePasswordManager.Tests.csproj
```

## Run

Windows:

```powershell
dotnet build SecurePasswordManager\SecurePasswordManager.csproj -f net9.0-windows10.0.19041.0
```

For Android/iOS, open the solution in Visual Studio with the .NET MAUI workload installed, choose a simulator or device, and run the `SecurePasswordManager` project.

## Notes

`Plugin.Maui.Biometric` currently targets .NET 10, so this .NET 9 implementation uses a compatible native biometric wrapper (`Plugin.Fingerprint`) plus MAUI `SecureStorage`. The biometric path is optional and always falls back to the master password.
