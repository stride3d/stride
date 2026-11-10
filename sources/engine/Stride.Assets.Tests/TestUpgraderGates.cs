// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Reflection;
using Stride.Core;
using Stride.Core.Assets;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The rules of the code and project upgrader gates, which compare only the number of a version. See
    /// docs/build/versioning.md.
    /// </summary>
    public class TestUpgraderGates
    {
        private static UpgradeRegistry DeclaredUpgrades()
        {
            var registry = new UpgradeRegistry();
            new StridePackageUpgrader().DeclareUpgrades(registry);
            return registry;
        }

        // A project is stamped with the committed number: a gate above it would run again at each upgrade.
        [Fact]
        public void NoGateIsAboveTheCommittedNumber()
        {
            var committed = typeof(StridePackageUpgrader).GetCustomAttribute<PackageUpgraderAttribute>().UpdatedVersionRange.MinVersion.Version;
            var registry = DeclaredUpgrades();
            var gates = registry.CodeUpgrades.Select(x => x.GateVersion).Concat(registry.ProjectUpgrades.Select(x => x.GateVersion));

            Assert.All(gates, gate => Assert.True(gate.Version <= committed, $"Gate {gate} is above the committed version {committed}: bump Patch in SharedAssemblyInfo.cs"));
        }

        // Gates run in declaration order, so a later migration can rely on an earlier one.
        [Fact]
        public void GatesOfOneKindAreInOrder()
        {
            var registry = DeclaredUpgrades();
            AssertInOrder(registry.CodeUpgrades.Select(x => x.GateVersion).ToArray());
            AssertInOrder(registry.ProjectUpgrades.Select(x => x.GateVersion).ToArray());
        }

        private static void AssertInOrder(PackageVersion[] gates)
        {
            for (var i = 1; i < gates.Length; i++)
                Assert.True(gates[i - 1].Version <= gates[i].Version, $"Gate {gates[i - 1]} is placed before the lower gate {gates[i]}");
        }

        // Gates and project versions compare on four parts, whichever way they are written.
        [Theory]
        [InlineData("4.5.1", "4.5.1.0")]
        [InlineData("4.5.1-beta", "4.5.1.0")]
        [InlineData("4.5.1-dev3", "4.5.1.0")]
        [InlineData("4.5.1-nightly.20261014.3", "4.5.1.0")]
        public void AProjectOnTheNumberHasTheGateOfThatNumber(string project, string gate)
        {
            Assert.False(new PackageVersion(project).Version < new PackageVersion(gate).Version);
        }

        [Theory]
        [InlineData("4.5.0-beta", "4.5.1.0")]
        [InlineData("4.5.0", "4.5.1.0")]
        [InlineData("4.5.0-nightly.20261014.3", "4.5.1.0")]
        public void AProjectBelowTheNumberGetsTheGateOfThatNumber(string project, string gate)
        {
            Assert.True(new PackageVersion(project).Version < new PackageVersion(gate).Version);
        }
    }
}
