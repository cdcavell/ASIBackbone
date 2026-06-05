using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Tests;

public sealed class AsiBackboneModelBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that ApplyAsiBackboneConfigurations returns the same ModelBuilder instance for fluent host-owned configuration.
    /// </summary>
    [Fact]
    public void ApplyAsiBackboneConfigurationsReturnsModelBuilder()
    {
        ModelBuilder modelBuilder = new();

        ModelBuilder result = modelBuilder.ApplyAsiBackboneConfigurations();

        Assert.Same(modelBuilder, result);
    }

    /// <summary>
    /// Verifies that ApplyAsiBackboneConfigurations guards against null model builders.
    /// </summary>
    [Fact]
    public void ApplyAsiBackboneConfigurationsThrowsForNullModelBuilder()
    {
        ModelBuilder? modelBuilder = null;

        _ = Assert.Throws<ArgumentNullException>(() => modelBuilder!.ApplyAsiBackboneConfigurations());
    }
}
