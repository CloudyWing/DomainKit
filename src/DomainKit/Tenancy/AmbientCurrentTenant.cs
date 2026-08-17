namespace CloudyWing.DomainKit.Tenancy;

/// <summary>
/// 使用 <see cref="AsyncLocal{T}" /> 保存目前租戶脈絡。
/// </summary>
public sealed class AmbientCurrentTenant : ICurrentTenant {
    private readonly AsyncLocal<TenantScope?> currentScope = new();

    /// <inheritdoc />
    public Guid? TenantId => currentScope.Value?.TenantId;

    /// <inheritdoc />
    public IDisposable Change(Guid? tenantId) {
        TenantScope? parent = currentScope.Value;
        TenantScope scope = new(tenantId, parent, Restore);
        currentScope.Value = scope;
        return scope;
    }

    private void Restore(TenantScope scope) {
        if (ReferenceEquals(currentScope.Value, scope)) {
            currentScope.Value = scope.Parent;
        }
    }

    private sealed class TenantScope : IDisposable {
        private readonly Action<TenantScope> restore;
        private bool isDisposed;

        public TenantScope(Guid? tenantId, TenantScope? parent, Action<TenantScope> restore) {
            TenantId = tenantId;
            Parent = parent;
            this.restore = restore;
        }

        public Guid? TenantId { get; }

        public TenantScope? Parent { get; }

        public void Dispose() {
            if (isDisposed) {
                return;
            }

            isDisposed = true;
            restore(this);
        }
    }
}
