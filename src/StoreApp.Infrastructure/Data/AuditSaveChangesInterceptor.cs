using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StoreApp.Domain.Common;
using StoreApp.Domain.Entities;

namespace StoreApp.Infrastructure.Data;

/// <summary>
/// Reusable module. Stamps CreatedAt/UpdatedAt, keeps Product.SearchText current,
/// and writes an AuditLog row for every added/modified/deleted IAuditable entity.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser user, AuditContext audit) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly HashSet<string> Skip = ["CreatedAt", "UpdatedAt", "SearchText"];
    private readonly List<(EntityEntry Entry, AuditLog Log)> _pending = [];
    private bool _busy;

    public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> result)
    { Prepare(e.Context); return result; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> result, CancellationToken ct = default)
    { Prepare(e.Context); return ValueTask.FromResult(result); }

    public override int SavedChanges(SaveChangesCompletedEventData e, int result)
    {
        if (FillIds() && e.Context is { } c)
        {
            _busy = true;
            try { c.SaveChanges(); } finally { _busy = false; }
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
    {
        if (FillIds() && e.Context is { } c)
        {
            _busy = true;
            try { await c.SaveChangesAsync(ct); } finally { _busy = false; }
        }
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData e) => _pending.Clear();

    public override Task SaveChangesFailedAsync(DbContextErrorEventData e, CancellationToken ct = default)
    { _pending.Clear(); return Task.CompletedTask; }

    // Ids of newly added rows exist only after the first save, so we fill them in and save the audit rows again.
    private bool FillIds()
    {
        if (_pending.Count == 0) return false;
        foreach (var (entry, log) in _pending)
            log.EntityId = entry.Property("Id").CurrentValue?.ToString();
        _pending.Clear();
        return true;
    }

    private void Prepare(DbContext? ctx)
    {
        if (ctx is null || _busy) return;
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>();

        foreach (var entry in ctx.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is Product p && entry.State is EntityState.Added or EntityState.Modified)
                p.SearchText = $"{p.Name} {p.Sku} {p.Barcode}".ToLowerInvariant();

            if (entry.Entity is not IAuditable) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            if (entry.Entity is BaseEntity be)
            {
                if (entry.State == EntityState.Added) be.CreatedAt = now;
                else if (entry.State == EntityState.Modified) be.UpdatedAt = now;
            }
            if (audit.Suppress) continue;

            var data = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties)
            {
                var name = prop.Metadata.Name;
                if (Skip.Contains(name) || prop.Metadata.IsPrimaryKey()) continue;
                switch (entry.State)
                {
                    case EntityState.Added: data[name] = prop.CurrentValue; break;
                    case EntityState.Deleted: data[name] = prop.OriginalValue; break;
                    case EntityState.Modified when prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue):
                        data[name] = new[] { prop.OriginalValue, prop.CurrentValue };
                        break;
                }
            }
            if (entry.State == EntityState.Modified && data.Count == 0) continue;

            var log = new AuditLog
            {
                Timestamp = now,
                UserId = user.UserId,
                UserName = user.UserName,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = entry.State == EntityState.Added ? null : entry.Property("Id").CurrentValue?.ToString(),
                Action = entry.State.ToString(),
                Changes = JsonSerializer.Serialize(data, Json),
                Reason = audit.Reason
            };
            logs.Add(log);
            if (entry.State == EntityState.Added) _pending.Add((entry, log));
        }

        if (logs.Count > 0) ctx.Set<AuditLog>().AddRange(logs);
    }
}
