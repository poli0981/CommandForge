using CommandForge.Domain;
using CommandForge.Wpf.ViewModels;

namespace CommandForge.Tests;

/// <summary>Tests the confirmation gating, especially the type-to-confirm for Dangerous commands.</summary>
public sealed class ConfirmationViewModelTests
{
    private static CommandDefinition Command(DangerLevel level) => new()
    {
        Id = "x",
        CategoryId = "c",
        TitleKey = "t",
        DescriptionKey = "d",
        Executable = "x",
        DangerLevel = level,
    };

    [Fact]
    public void Caution_CanRun_IsAlwaysTrue()
    {
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Caution), "Repair image", "desc", defaultCreateRestorePoint: true);

        Assert.True(viewModel.CanRun);
    }

    [Fact]
    public void Dangerous_CanRun_RequiresTypedTitle()
    {
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Dangerous), "Minimize telemetry", "desc", defaultCreateRestorePoint: true);

        Assert.False(viewModel.CanRun);

        viewModel.ConfirmationInput = "wrong";
        Assert.False(viewModel.CanRun);

        viewModel.ConfirmationInput = "Minimize telemetry";
        Assert.True(viewModel.CanRun);
    }

    [Fact]
    public void CreateRestorePoint_DefaultsToProvidedValue()
    {
        Assert.True(new ConfirmationViewModel(Command(DangerLevel.Caution), "t", "d", true).CreateRestorePoint);
        Assert.False(new ConfirmationViewModel(Command(DangerLevel.Caution), "t", "d", false).CreateRestorePoint);
    }
}
