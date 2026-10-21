using System.Text;
using CommandForge.Desktop.ViewModels;
using CommandForge.Domain;

namespace CommandForge.Tests;

/// <summary>Tests the confirmation gating, especially the type-to-confirm for Dangerous commands.</summary>
public sealed class ConfirmationViewModelTests
{
    private static CommandDefinition Command(DangerLevel level, bool requiresAdmin = false, string id = "x") => new()
    {
        Id = id,
        CategoryId = "c",
        TitleKey = "t",
        DescriptionKey = "d",
        Executable = "x",
        DangerLevel = level,
        RequiresAdmin = requiresAdmin,
    };

    [Fact]
    public void Caution_CanRun_IsAlwaysTrue()
    {
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Caution), "Repair image", "desc", defaultCreateRestorePoint: true);

        Assert.True(viewModel.CanRun);
        Assert.True(viewModel.RunCommand.CanExecute(null));
    }

    [Fact]
    public void Dangerous_CanRun_RequiresTypedTitle()
    {
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Dangerous), "Minimize telemetry", "desc", defaultCreateRestorePoint: true);

        Assert.False(viewModel.CanRun);
        Assert.False(viewModel.RunCommand.CanExecute(null));

        viewModel.ConfirmationInput = "wrong";
        Assert.False(viewModel.CanRun);

        viewModel.ConfirmationInput = "Minimize telemetry";
        Assert.True(viewModel.CanRun);
        Assert.True(viewModel.RunCommand.CanExecute(null));
    }

    [Fact]
    public void Dangerous_TypedTitle_IgnoresCaseAndSurroundingSpace()
    {
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Dangerous), "Minimize telemetry", "desc", defaultCreateRestorePoint: true)
        {
            ConfirmationInput = "  minimize TELEMETRY ",
        };

        Assert.True(viewModel.CanRun);
    }

    [Fact]
    public void Dangerous_TypedTitle_MatchesDecomposedVietnamese()
    {
        // Some Vietnamese input methods produce decomposed (NFD) accents that look identical.
        const string title = "Đặt lại tường lửa";
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Dangerous), title, "desc", defaultCreateRestorePoint: true)
        {
            ConfirmationInput = title.Normalize(NormalizationForm.FormD),
        };

        Assert.True(viewModel.CanRun);
    }

    [Fact]
    public void Dangerous_CommandId_IsAcceptedInsteadOfTitle()
    {
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Dangerous, id: "sec.firewall.off"), "ファイアウォールを無効にする", "desc", defaultCreateRestorePoint: true)
        {
            ConfirmationInput = "sec.firewall.off",
        };

        Assert.True(viewModel.CanRun);
    }

    [Fact]
    public void CreateRestorePoint_DefaultsToProvidedValue_ForRiskyAdminCommands()
    {
        Assert.True(new ConfirmationViewModel(Command(DangerLevel.Caution, requiresAdmin: true), "t", "d", true).CreateRestorePoint);
        Assert.False(new ConfirmationViewModel(Command(DangerLevel.Caution, requiresAdmin: true), "t", "d", false).CreateRestorePoint);
    }

    [Fact]
    public void RestorePoint_IsNotOffered_ForPerUserCommands()
    {
        // Creating a restore point needs admin rights: offering it for a per-user tweak would add a UAC prompt.
        var viewModel = new ConfirmationViewModel(Command(DangerLevel.Caution, requiresAdmin: false), "t", "d", true);

        Assert.False(viewModel.ShowRestorePointOption);
        Assert.False(viewModel.CreateRestorePoint);
    }
}
