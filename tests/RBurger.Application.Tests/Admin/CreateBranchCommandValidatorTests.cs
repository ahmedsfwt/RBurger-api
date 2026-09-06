using FluentValidation.TestHelper;
using RBurger.Application.Admin.Branches.Commands.CreateBranch;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class CreateBranchCommandValidatorTests
{
    private readonly CreateBranchCommandValidator _validator = new();

    private static CreateBranchCommand ValidCommand() => new()
    {
        NameAr = "أسيوط",
        NameEn = "Assiut",
        DeliveryFee = 22,
        EtaMinMinutes = 25,
        EtaMaxMinutes = 40
    };

    [Fact]
    public void Should_not_have_error_when_command_is_valid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_NameAr_is_empty()
    {
        var command = ValidCommand();
        command.NameAr = "";
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.NameAr);
    }

    [Fact]
    public void Should_have_error_when_EtaMinMinutes_is_greater_than_EtaMaxMinutes()
    {
        var command = ValidCommand();
        command.EtaMinMinutes = 50;
        command.EtaMaxMinutes = 40;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.EtaMinMinutes);
    }

    [Fact]
    public void Should_have_error_when_DeliveryFee_is_negative()
    {
        var command = ValidCommand();
        command.DeliveryFee = -1;
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.DeliveryFee);
    }
}
