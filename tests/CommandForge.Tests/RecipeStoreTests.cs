using CommandForge.Domain;
using CommandForge.Infrastructure.Recipes;

namespace CommandForge.Tests;

/// <summary>Verifies the recipe store round-trips, overwrites by name, and falls back on corruption.</summary>
public sealed class RecipeStoreTests
{
    [Fact]
    public void Save_Get_Delete_Persists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-recipes-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonRecipeStore(path);
            store.Save(new Recipe { Name = "Maintenance", CommandIds = ["dism.checkhealth", "sfc.scannow"] });
            store.Save(new Recipe { Name = "Network", CommandIds = ["net.flushdns"] });

            Assert.Equal(["Maintenance", "Network"], store.GetAll().Select(r => r.Name));

            // Reload from disk — recipes persist with their ordered steps.
            var reloaded = new JsonRecipeStore(path);
            Assert.Equal(2, reloaded.GetAll().Count);
            Assert.Equal(
                ["dism.checkhealth", "sfc.scannow"],
                reloaded.GetAll().First(r => r.Name == "Maintenance").CommandIds);

            reloaded.Delete("Network");
            Assert.Single(reloaded.GetAll());
            Assert.DoesNotContain(new JsonRecipeStore(path).GetAll(), r => r.Name == "Network");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Save_OverwritesByName_CaseInsensitive()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-recipes-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonRecipeStore(path);
            store.Save(new Recipe { Name = "Maint", CommandIds = ["a"] });
            store.Save(new Recipe { Name = "maint", CommandIds = ["b", "c"] });

            Assert.Single(store.GetAll());
            Assert.Equal(["b", "c"], store.GetAll()[0].CommandIds);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CorruptFile_FallsBackToEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cf-recipes-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ not json ");
            Assert.Empty(new JsonRecipeStore(path).GetAll());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
