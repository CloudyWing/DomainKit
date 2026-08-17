namespace CloudyWing.DomainKit.EntityFrameworkCore.Auditing;

internal static class AuditScope {
    private static readonly AsyncLocal<Guid?> CurrentActorId = new();

    public static Guid? ActorId => CurrentActorId.Value;

    public static IDisposable Begin(Guid? actorId) {
        Guid? originalActorId = CurrentActorId.Value;
        CurrentActorId.Value = actorId;

        return new RestoreScope(originalActorId);
    }

    private sealed class RestoreScope : IDisposable {
        private readonly Guid? originalActorId;
        private bool isDisposed;

        public RestoreScope(Guid? originalActorId) {
            this.originalActorId = originalActorId;
        }

        public void Dispose() {
            if (isDisposed) {
                return;
            }

            CurrentActorId.Value = originalActorId;
            isDisposed = true;
        }
    }
}
