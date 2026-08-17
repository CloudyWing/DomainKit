using CloudyWing.DomainKit.Entities;

namespace CloudyWing.DomainKit.Tests.TestDoubles;

internal sealed class AggregateIncludeChild : EntityBase {
    public string Name { get; set; } = "";

    public List<AggregateIncludeOption> Options { get; } = [];

    public List<AggregateIncludeRule> ValidationRules { get; } = [];

    public void AddOption(string name) {
        Options.Add(new AggregateIncludeOption {
            Name = name
        });
    }

    public void AddValidationRule(string name) {
        ValidationRules.Add(new AggregateIncludeRule {
            Name = name
        });
    }
}
