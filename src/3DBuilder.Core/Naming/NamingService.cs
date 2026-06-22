using System;
using System.Text.RegularExpressions;

namespace ThreeDBuilder.Core.Naming
{
    /// <summary>Raison pour laquelle un CSYS du squelette n'est pas un point de montage d'aimant.</summary>
    public enum CsysExclusion
    {
        None = 0,
        /// <summary>CSYS de position le long de la maille (« Entrée » / « Sortie »).</summary>
        EntrySortie,
        /// <summary>Espaceur (« DRIFT »), pas un aimant.</summary>
        Drift,
        /// <summary>Fraction non médiane d'un aimant long découpé (seule la médiane porte le montage).</summary>
        NonMedianFraction
    }

    /// <summary>Résultat de classification d'un nom de feature CSYS du squelette.</summary>
    public sealed class CsysClassification
    {
        /// <summary>Vrai si ce CSYS est un point de montage d'aimant (→ on importe un aimant dessus).</summary>
        public bool IsMagnetMount { get; }

        /// <summary>Code aimant extrait (matché contre la colonne B du dictionnaire) si <see cref="IsMagnetMount"/>.</summary>
        public string Code { get; }

        /// <summary>Raison d'exclusion sinon.</summary>
        public CsysExclusion Exclusion { get; }

        /// <summary>Fraction (a, b) si le nom en portait une, sinon null.</summary>
        public (int A, int B)? Fraction { get; }

        private CsysClassification(bool mount, string code, CsysExclusion exclusion, (int, int)? fraction)
        {
            IsMagnetMount = mount;
            Code = code;
            Exclusion = exclusion;
            Fraction = fraction;
        }

        public static CsysClassification Mount(string code, (int, int)? fraction)
            => new CsysClassification(true, code, CsysExclusion.None, fraction);

        public static CsysClassification Excluded(CsysExclusion reason, (int, int)? fraction = null)
            => new CsysClassification(false, null, reason, fraction);
    }

    /// <summary>
    /// Centralise la convention de nommage des squelettes (cf. memory 3dbuilder-naming-convention).
    /// Remplace les listes <c>InStr</c> codées en dur et divergentes des 3 routines VB d'origine.
    ///
    /// Un CSYS de montage a un nom de la forme <c>&lt;CODE&gt;.NN</c> ou <c>&lt;CODE&gt;_a/b.NN</c> où :
    ///  - <c>.NN</c> = numéro d'instance (suffixe terminal),
    ///  - <c>_a/b</c> = fraction d'un aimant long ; seule la fraction MÉDIANE ⌈b/2⌉/b porte le montage.
    /// Sont exclus : les CSYS « Entrée » / « Sortie » (position) et « DRIFT » (espaceur).
    /// </summary>
    public sealed class NamingService
    {
        // <base>.<NN>  — le suffixe d'instance est le dernier segment « .chiffres ».
        private static readonly Regex InstanceSuffix = new Regex(@"^(?<base>.*)\.(?<inst>\d+)$", RegexOptions.Compiled);
        // <code>_<a>/<b>  — fraction terminale.
        private static readonly Regex FractionSuffix = new Regex(@"^(?<code>.*)_(?<a>\d+)/(?<b>\d+)$", RegexOptions.Compiled);

        /// <summary>Fraction médiane d'un découpage en b morceaux : ⌈b/2⌉ (ex. b=3→2, 5→3, 9→5, 11→6).</summary>
        public static int MedianFraction(int b) => (b + 1) / 2;

        /// <summary>
        /// Classe un nom de feature CSYS du squelette. L'appelant (couche Nx) ne transmet QUE des
        /// features de type DATUM_CSYS (le type est une notion NXOpen, hors Core).
        /// </summary>
        public CsysClassification Classify(string featureName)
        {
            var name = (featureName ?? "").Trim().Trim('"').Trim();
            if (name.Length == 0) return CsysClassification.Excluded(CsysExclusion.None);

            // 1) CSYS de position : « Entrée » / « Sortie » (insensible à la casse).
            if (ContainsCi(name, "Entrée") || ContainsCi(name, "Sortie"))
                return CsysClassification.Excluded(CsysExclusion.EntrySortie);

            // 2) Retirer le suffixe d'instance « .NN ».
            var baseName = name;
            var mi = InstanceSuffix.Match(baseName);
            if (mi.Success) baseName = mi.Groups["base"].Value;

            // 3) DRIFT = espaceur.
            if (string.Equals(baseName, "DRIFT", StringComparison.OrdinalIgnoreCase))
                return CsysClassification.Excluded(CsysExclusion.Drift);

            // 4) Fraction éventuelle : ne garder que la médiane.
            var mf = FractionSuffix.Match(baseName);
            if (mf.Success)
            {
                int a = int.Parse(mf.Groups["a"].Value);
                int b = int.Parse(mf.Groups["b"].Value);
                var frac = (a, b);
                if (b <= 0 || a != MedianFraction(b))
                    return CsysClassification.Excluded(CsysExclusion.NonMedianFraction, frac);
                return CsysClassification.Mount(mf.Groups["code"].Value, frac);
            }

            // 5) Pas de fraction → CSYS de montage simple.
            return CsysClassification.Mount(baseName, null);
        }

        /// <summary>Vrai si le nom de feature CSYS aimant désigne le repère de montage « RPM » (côté pièce aimant).</summary>
        public bool IsRpmCsys(string featureName)
            => ContainsCi(featureName ?? "", "RPM");

        private static bool ContainsCi(string haystack, string needle)
            => haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
