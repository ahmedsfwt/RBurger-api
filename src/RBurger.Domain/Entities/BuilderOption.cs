namespace RBurger.Domain.Entities;

public class BuilderOption
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }

    // §6.1 states BuilderOptionGroup (1) -- (∞) BuilderOption, but §6.2 does not explicitly
    // name a FK column on BuilderOptions for it. BuilderOptionGroupId added as a Day 2
    // implementation decision (approved) to resolve the undocumented FK-name gap;
    // see discrepancy report.
    public int BuilderOptionGroupId { get; set; }
    public BuilderOptionGroup Group { get; set; } = null!;
}
