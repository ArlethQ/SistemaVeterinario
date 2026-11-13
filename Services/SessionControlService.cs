using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace HosvetKardex.Web.Services;

public sealed class SessionControlService
{
    public string ServerInstanceId { get; } = Guid.NewGuid().ToString("N");
    private long _permisosVersion = 1;
    private readonly ConcurrentDictionary<int, DateTimeOffset> _revokedAfterUtc = new();
    private readonly byte[] _fotoKey = RandomNumberGenerator.GetBytes(32);

    public long PermisosVersion => Interlocked.Read(ref _permisosVersion);

    public void InvalidatePermissions() => Interlocked.Increment(ref _permisosVersion);

    public void RevokeUser(int idUser)
    {
        if (idUser <= 0) return;
        _revokedAfterUtc[idUser] = DateTimeOffset.UtcNow;
        InvalidatePermissions();
    }

    public bool IsUserRevoked(int idUser, DateTimeOffset sessionIssuedUtc)
    {
        if (idUser <= 0) return false;
        return _revokedAfterUtc.TryGetValue(idUser, out var revokedAt) && sessionIssuedUtc <= revokedAt;
    }

    public string FirmarFotoUrl(int nhcl, long version = 0)
    {
        long exp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
        string firma = CalcularFirmaFoto(nhcl, exp);
        return version > 0
            ? $"/pacientes-fotos/{nhcl}?v={version}&t={exp}&s={firma}"
            : $"/pacientes-fotos/{nhcl}?t={exp}&s={firma}";
    }

    public bool ValidarFotoToken(int nhcl, long exp, string? firma)
    {
        if (nhcl <= 0 || string.IsNullOrWhiteSpace(firma)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;
        string esperada = CalcularFirmaFoto(nhcl, exp);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(esperada),
            Encoding.ASCII.GetBytes(firma.Trim()));
    }

    private string CalcularFirmaFoto(int nhcl, long exp)
    {
        var payload = Encoding.UTF8.GetBytes($"{nhcl}:{exp}");
        var hash = HMACSHA256.HashData(_fotoKey, payload);
        return Convert.ToHexString(hash);
    }
}
