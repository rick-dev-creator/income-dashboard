namespace Income.Application.Services;

internal interface ICredentialEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
