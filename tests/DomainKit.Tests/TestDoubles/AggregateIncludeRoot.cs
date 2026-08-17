using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class AggregateIncludeRoot : EntityBase {
    public string Name { get; set; } = "";

    public List<AggregateIncludeChild> Children { get; } = [];

    public List<AggregateIncludeTag> Tags { get; } = [];

    public AggregateIncludeChild AddChild(string name) {
        AggregateIncludeChild child = new() {
            Name = name
        };
        Children.Add(child);

        return child;
    }

    public void AddTag(string name) {
        Tags.Add(new AggregateIncludeTag {
            Name = name
        });
    }
}
