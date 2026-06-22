using ThreeDBuilder.Core.Naming;
using Xunit;

namespace ThreeDBuilder.Core.Tests
{
    public class NamingServiceTests
    {
        private readonly NamingService _svc = new NamingService();

        // --- Aimants simples : <CODE>.NN → code sans le suffixe d'instance ---

        [Theory]
        [InlineData("SXT_6.2_21_75_AM_OCT_21-L127.01", "SXT_6.2_21_75_AM_OCT_21-L127")]
        [InlineData("QUAD_6.48_21_126.01", "QUAD_6.48_21_126")]
        [InlineData("QUAD_6.48_21_180.03", "QUAD_6.48_21_180")]
        [InlineData("OCT_21.02", "OCT_21")]
        [InlineData("QCORR_22.01", "QCORR_22")]
        // Le code contient lui-même des points (variantes DI) : seul le DERNIER segment .NN est retiré.
        [InlineData("DI_6.48_21_126_2.49.01", "DI_6.48_21_126_2.49")]
        [InlineData("DI_6.48_18_106_0.61.01", "DI_6.48_18_106_0.61")]
        public void Classify_SimpleMagnet_ExtractsCode(string feature, string expectedCode)
        {
            var c = _svc.Classify(feature);
            Assert.True(c.IsMagnetMount);
            Assert.Equal(expectedCode, c.Code);
            Assert.Null(c.Fraction);
        }

        // --- Fractions : seule la médiane ⌈b/2⌉/b porte le montage ---

        [Theory]
        [InlineData("DNC_7BA_2/3.01", "DNC_7BA")]   // médiane de 3
        [InlineData("DNL_7BA_2/3.01", "DNL_7BA")]
        [InlineData("DNL1P7T_7BA_6/11.01", "DNL1P7T_7BA")] // médiane de 11 (cas non géré par le VB)
        public void Classify_MedianFraction_IsMount(string feature, string expectedCode)
        {
            var c = _svc.Classify(feature);
            Assert.True(c.IsMagnetMount);
            Assert.Equal(expectedCode, c.Code);
            Assert.NotNull(c.Fraction);
        }

        [Theory]
        [InlineData("DNC_7BA_1/3.01")]
        [InlineData("DNC_7BA_3/3.01")]
        [InlineData("DNL1P7T_7BA_1/11.01")]
        [InlineData("DNL1P7T_7BA_5/11.01")]
        [InlineData("DNL1P7T_7BA_11/11.01")]
        public void Classify_NonMedianFraction_IsExcluded(string feature)
        {
            var c = _svc.Classify(feature);
            Assert.False(c.IsMagnetMount);
            Assert.Equal(CsysExclusion.NonMedianFraction, c.Exclusion);
        }

        [Theory]
        [InlineData(3, 2)]
        [InlineData(5, 3)]
        [InlineData(9, 5)]
        [InlineData(11, 6)]
        public void MedianFraction_OddCounts(int b, int expected)
            => Assert.Equal(expected, NamingService.MedianFraction(b));

        // --- Exclusions par mot-clé ---

        [Theory]
        [InlineData("SXT_6.2_21_75_AM_OCT_21-L127.01 Entrée", CsysExclusion.EntrySortie)]
        [InlineData("QUAD_6.48_21_126.01 Sortie", CsysExclusion.EntrySortie)]
        [InlineData("DRIFT.03", CsysExclusion.Drift)]
        [InlineData("DRIFT.10", CsysExclusion.Drift)]
        public void Classify_Excluded_ByKeyword(string feature, CsysExclusion reason)
        {
            var c = _svc.Classify(feature);
            Assert.False(c.IsMagnetMount);
            Assert.Equal(reason, c.Exclusion);
        }

        // Entrée/Sortie l'emportent même quand « Drift » est aussi présent (ordre de priorité).
        [Fact]
        public void Classify_DriftEntree_IsEntrySortie()
            => Assert.Equal(CsysExclusion.EntrySortie, _svc.Classify("Drift.01 Sortie").Exclusion);

        [Fact]
        public void Classify_StripsQuotesAndIndentation()
        {
            var c = _svc.Classify("    \"QUAD_6.48_21_126.01\"");
            Assert.True(c.IsMagnetMount);
            Assert.Equal("QUAD_6.48_21_126", c.Code);
        }

        [Theory]
        [InlineData("CSYS RPM", true)]
        [InlineData("RPM.01", true)]
        [InlineData("DATUM_CSYS Entrée", false)]
        public void IsRpmCsys(string name, bool expected)
            => Assert.Equal(expected, _svc.IsRpmCsys(name));
    }
}
