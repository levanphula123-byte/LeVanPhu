using System.Security.Cryptography;
using System.Text;

namespace LeVanPhu.Api.Services;

public static class JwtSigningKey
{
    public static string Resolve(ConfigurationManager configuration, bool development, string contentRoot)
    {
        var configured = configuration["Jwt:Secret"];
        if (Valid(configured)) return configured!;

        if (!development)
            throw new InvalidOperationException("JWT signing key is missing or invalid. Configure Jwt:Secret for this environment.");

        // A local, persistent development key also works when VS cannot load User Secrets.
        // Never use a fixed/shared key, and never enable this fallback in production.
        var directory = Path.Combine(contentRoot, ".local");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "jwt-signing.key");
        if (!File.Exists(path))
        {
            var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temporary, Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
                try { File.Move(temporary, path, overwrite: false); }
                catch (IOException) when (File.Exists(path)) { }
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        var secret = File.ReadAllText(path).Trim();
        if (!Valid(secret))
            throw new InvalidOperationException("The local development JWT key is damaged. Remove .local/jwt-signing.key and restart.");

        // Both token issuance and token validation must use exactly the same key.
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = secret });
        return secret;
    }

    private static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value)
        && !value.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase)
        && Encoding.UTF8.GetByteCount(value) >= 32;
}
