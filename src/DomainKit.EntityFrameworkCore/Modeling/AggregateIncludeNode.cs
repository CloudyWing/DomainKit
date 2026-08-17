namespace CloudyWing.DomainKit.EntityFrameworkCore.Modeling;

internal sealed class AggregateIncludeNode {
    public AggregateIncludeNode(string navigationName) {
        NavigationName = navigationName;
    }

    public string NavigationName { get; }

    public Dictionary<string, AggregateIncludeNode> Children { get; } =
        new(StringComparer.Ordinal);

    public IncludeTreeNode ToImmutable() {
        return new IncludeTreeNode {
            NavigationName = NavigationName,
            Children = Children.Values
                .Select(node => node.ToImmutable())
                .ToList()
        };
    }
}
