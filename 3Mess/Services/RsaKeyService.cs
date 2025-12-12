using System;
using System.IO;
using System.Security.Cryptography;

namespace ThreeMess.Services;

public static class RsaKeyService
{
    public static (string publicKeyPem, string privateKeyPath) GenerateAndStoreForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("userId must not be empty", nameof(userId));
        }

        using var rsa = RSA.Create(2048);

        byte[] publicDer = rsa.ExportSubjectPublicKeyInfo();
        string publicPem = ToPem("PUBLIC KEY", publicDer);

        byte[] privateDer = rsa.ExportPkcs8PrivateKey();
        byte[] protectedPrivate = ProtectedData.Protect(privateDer, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);

        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "3Mess", "keys");
        Directory.CreateDirectory(root);

        string privatePath = Path.Combine(root, $"{userId}.private.dpapi");
        File.WriteAllBytes(privatePath, protectedPrivate);

        // Optional: store public key too
        string publicPath = Path.Combine(root, $"{userId}.public.pem");
        File.WriteAllText(publicPath, publicPem);

        return (publicPem, privatePath);
    }

    private static string ToPem(string label, byte[] der)
    {
        string b64 = Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN {label}-----\n{b64}\n-----END {label}-----\n";
    }
}

