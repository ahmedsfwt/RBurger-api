namespace RBurger.Domain.Entities;

public class BuilderOptionGroup
{
    public int Id { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public bool IsSingleSelect { get; set; }

    // §6.1 navigation property
    public ICollection<BuilderOption> Options { get; set; } = new List<BuilderOption>();
}
