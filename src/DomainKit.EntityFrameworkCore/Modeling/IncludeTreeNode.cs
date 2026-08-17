namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

internal sealed class IncludeTreeNode {
    public required string NavigationName { get; init; }

    public IReadOnlyList<IncludeTreeNode> Children { get; init; } = [];
}
