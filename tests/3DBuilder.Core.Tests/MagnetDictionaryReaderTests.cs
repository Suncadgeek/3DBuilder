using System.Collections.Generic;
using ClosedXML.Excel;
using ThreeDBuilder.Core.Excel;
using Xunit;

namespace ThreeDBuilder.Core.Tests
{
    public class MagnetDictionaryReaderTests
    {
        private static MagnetDictionary ReadRows(params (string tc, string code)[] rows)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("dico");
                int r = 1;
                foreach (var (tc, code) in rows)
                {
                    if (tc != null) ws.Cell(r, 1).Value = tc;
                    if (code != null) ws.Cell(r, 2).Value = code;
                    r++;
                }
                return new MagnetDictionaryReader().ReadWorksheet(ws);
            }
        }

        [Fact]
        public void Reads_TwoColumns_CodeToTcRef()
        {
            var d = ReadRows(("CAO000154690", "CHIC_100_11.8"), ("CAO000164655", "QUAD_10_23_106"));
            Assert.Equal(2, d.CodeToTcRef.Count);
            Assert.True(d.TryGetTcRef("CHIC_100_11.8", out var tc));
            Assert.Equal("CAO000154690", tc);
            Assert.Empty(d.DuplicateCodes);
        }

        [Fact]
        public void Matching_IsCaseSensitive_Strict()
        {
            var d = ReadRows(("CAO1", "DNL1p7T"));
            Assert.True(d.Contains("DNL1p7T"));
            Assert.False(d.Contains("DNL1P7T"));   // D9 : strict, sensible à la casse
        }

        [Fact]
        public void DuplicateCode_IsReported_FirstWins()
        {
            var d = ReadRows(("CAO_A", "CODE"), ("CAO_B", "CODE"));
            Assert.Contains("CODE", d.DuplicateCodes);
            d.TryGetTcRef("CODE", out var tc);
            Assert.Equal("CAO_A", tc);             // première occurrence conservée
        }

        [Fact]
        public void EmptyRows_AreSkipped()
        {
            var d = ReadRows(("CAO1", "A"), (null, null), ("CAO2", "B"));
            Assert.Equal(2, d.CodeToTcRef.Count);
        }
    }
}
