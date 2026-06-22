using System.Collections.Generic;
using NXOpen;
using ThreeDBuilder.Core.Naming;
using Features = NXOpen.Features;

namespace ThreeDBuilder.Nx
{
    /// <summary>Un point de montage d'aimant lu sur le squelette : code + feature CSYS porteuse.</summary>
    public sealed class SkeletonMount
    {
        /// <summary>Code aimant (à résoudre via le dictionnaire vers une réf TC).</summary>
        public string Code { get; set; }
        /// <summary>Nom de la feature DATUM_CSYS du squelette sur laquelle contraindre l'aimant.</summary>
        public string CsysFeatureName { get; set; }
    }

    /// <summary>
    /// Lit les CSYS de montage d'un squelette. Le « quoi est un montage » est entièrement délégué au
    /// <see cref="NamingService"/> (Core, testé) ; ici on ne fait que filtrer le type NXOpen DATUM_CSYS
    /// (notion qui n'existe pas dans le Core) et parcourir les features.
    /// </summary>
    public sealed class NxSkeletonReader
    {
        private const string DatumCsysType = "DATUM_CSYS";
        private readonly NamingService _naming;

        public NxSkeletonReader(NamingService naming) { _naming = naming; }

        public IReadOnlyList<SkeletonMount> ReadMounts(Part skeletonPart)
        {
            var mounts = new List<SkeletonMount>();
            foreach (Features.Feature feat in skeletonPart.Features.GetFeatures())
            {
                if (feat.FeatureType != DatumCsysType) continue;
                var cls = _naming.Classify(feat.Name);
                if (!cls.IsMagnetMount) continue;
                mounts.Add(new SkeletonMount { Code = cls.Code, CsysFeatureName = feat.Name });
            }
            return mounts;
        }

        /// <summary>Codes attendus (avec répétitions) — pour le preflight.</summary>
        public IReadOnlyList<string> ReadCodes(Part skeletonPart)
        {
            var codes = new List<string>();
            foreach (var m in ReadMounts(skeletonPart)) codes.Add(m.Code);
            return codes;
        }
    }
}
