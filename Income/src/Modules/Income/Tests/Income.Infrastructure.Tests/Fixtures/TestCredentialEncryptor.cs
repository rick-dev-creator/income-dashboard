using Income.Application.Services;
using Income.Infrastructure.Services;

namespace Income.Infrastructure.Tests.Fixtures;

internal static class TestCredentialEncryptor
{
    private static readonly AesCredentialEncryptor Instance = new("TestEncryptionKey_12345!");

    internal static ICredentialEncryptor Create() => Instance;
}
