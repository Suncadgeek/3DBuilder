using System.Collections.Generic;
using System.Linq;
using ThreeDBuilder.Core.Excel;
using ThreeDBuilder.Core.Preflight;
using Xunit;

namespace ThreeDBuilder.Core.Tests
{
    public class PreflightCheckerTests
    {
        private static MagnetDictionary Dict(params (string tc, string code)[] rows)
            => new MagnetDictionary(rows.Select(r => new KeyValuePair<string, string>(r.tc, r.code)));

        private static CellPreflightInput Cell(string name, bool hasAssembly, string[] skeleton, string[] placed = null)
            => new CellPreflightInput
            {
                CellName = name,
                HasMagnetAssembly = hasAssembly,
                SkeletonCodes = skeleton,
                PlacedCodes = placed ?? new string[0]
            };

        private readonly PreflightChecker _checker = new PreflightChecker();

        [Fact]
        public void Matched_And_Missing_AreClassified()
        {
            var dict = Dict(("CAO_Q", "QUAD_X"), ("CAO_S", "SXT_Y"));
            var cell = Cell("ARC14", true, new[] { "QUAD_X", "SXT_Y", "INCONNU_Z" });

            var report = _checker.Check(new[] { cell }, dict);

            var plan = report.Cells.Single();
            Assert.Equal(new[] { "QUAD_X", "SXT_Y" }, plan.ToAdd);
            Assert.Equal(new[] { "INCONNU_Z" }, plan.Missing);
            Assert.False(report.HasBlockingErrors);
            Assert.True(report.HasWarnings); // INCONNU_Z
            Assert.Contains(report.Findings, f => f.Category == PreflightChecker.CatMissingFromDict && f.Cell == "ARC14");
        }

        [Fact]
        public void OctAndQcorr_AbsentFromDict_AreKnownExcluded_NotWarnings()
        {
            var dict = Dict(("CAO_S", "SXT_Y"));
            var cell = Cell("ARC14", true, new[] { "SXT_Y", "OCT_21", "QCORR_22" });

            var report = _checker.Check(new[] { cell }, dict);

            Assert.DoesNotContain(report.Findings,
                f => f.Category == PreflightChecker.CatMissingFromDict);
            Assert.Equal(2, report.Findings.Count(f => f.Category == PreflightChecker.CatKnownExcluded));
            Assert.Empty(report.Cells.Single().Missing);
        }

        [Fact]
        public void DuplicateDictionaryKeys_IsBlockingError()
        {
            var dict = Dict(("CAO_A", "DUP"), ("CAO_B", "DUP"));
            var report = _checker.Check(new CellPreflightInput[0], dict);

            Assert.True(report.HasBlockingErrors);
            Assert.Contains(report.Findings, f => f.Category == PreflightChecker.CatDuplicateKeys);
        }

        [Fact]
        public void MissingMagnetAssembly_Warns_And_DisablesFill()
        {
            var dict = Dict(("CAO_Q", "QUAD_X"));
            var cell = Cell("ARC09", hasAssembly: false, skeleton: new[] { "QUAD_X" });

            var report = _checker.Check(new[] { cell }, dict);

            var plan = report.Cells.Single();
            Assert.False(plan.CanFill);
            Assert.Empty(plan.ToAdd); // rien posé sans coquille
            Assert.Contains(report.Findings, f => f.Category == PreflightChecker.CatMissingAssembly);
        }

        [Fact]
        public void Incremental_OnlyAddsMissing_KeepsPlaced()
        {
            var dict = Dict(("CAO_Q", "QUAD_X"), ("CAO_S", "SXT_Y"));
            var cell = Cell("ARC14", true, new[] { "QUAD_X", "SXT_Y" }, placed: new[] { "QUAD_X" });

            var report = _checker.Check(new[] { cell }, dict);

            var plan = report.Cells.Single();
            Assert.Equal(new[] { "SXT_Y" }, plan.ToAdd);
            Assert.Equal(new[] { "QUAD_X" }, plan.AlreadyPlaced);
            Assert.Contains(report.Findings, f => f.Category == PreflightChecker.CatAlreadyPopulated);
        }

        [Fact]
        public void ConventionDrift_WhenSkeletonsButZeroMatched()
        {
            var dict = Dict(("CAO_Q", "QUAD_X"));
            var cell = Cell("ARC14", true, new[] { "WEIRD_1", "WEIRD_2" });

            var report = _checker.Check(new[] { cell }, dict);

            Assert.Contains(report.Findings, f => f.Category == PreflightChecker.CatConventionDrift);
        }

        [Fact]
        public void UnusedDictionaryEntries_AreListed()
        {
            var dict = Dict(("CAO_Q", "QUAD_X"), ("CAO_S", "SXT_Y"));
            var cell = Cell("ARC14", true, new[] { "QUAD_X" });

            var report = _checker.Check(new[] { cell }, dict);

            Assert.Equal(new[] { "SXT_Y" }, report.UnusedDictionaryEntries);
        }

        [Fact]
        public void RepeatedCode_CountsPerOccurrence()
        {
            var dict = Dict(("CAO_Q", "QUAD_X"));
            var cell = Cell("ARC14", true, new[] { "QUAD_X", "QUAD_X", "QUAD_X" });

            var report = _checker.Check(new[] { cell }, dict);

            Assert.Equal(3, report.Cells.Single().ToAdd.Count);
            Assert.Equal(3, report.TotalToAdd);
        }
    }
}
