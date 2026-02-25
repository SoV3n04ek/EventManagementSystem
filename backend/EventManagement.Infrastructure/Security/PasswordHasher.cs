using System.Security.Cryptography;
using EventManagement.Domain.Interfaces.Security;

namespace EventManagement.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    const int SaltSize = 16;
    const int KeySize = 32;
    const int Iterations = 10000;

    public string HashPassword(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256);

        string key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        string salt = Convert.ToBase64String(algorithm.Salt);

        return $"{Iterations}.{salt}.{key}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        string[] parts = hashedPassword.Split('.', 3);
        if (parts.Length != 3)
            return false;

        int iterations = Convert.ToInt32(parts[0], System.Globalization.CultureInfo.InvariantCulture);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] key = Convert.FromBase64String(parts[2]);

        using var algorithm = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        byte[] keyToCheck = algorithm.GetBytes(KeySize);
        return keyToCheck.SequenceEqual(key);
    }
}
