using System.Collections.Concurrent;

namespace HosvetKardex.Web.Services;

public sealed class SessionControlService
{
    private long _permisosVersion = 1;
    private readonly ConcurrentDictionary<int, DateTimeOffset> _revokedAfterUtc = new();

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
}
