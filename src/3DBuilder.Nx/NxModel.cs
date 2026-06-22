using System.Collections.Generic;
using NXOpen;
using Assemblies = NXOpen.Assemblies;

namespace ThreeDBuilder.Nx
{
    /// <summary>Un Ensemble Aimants (coquille SKBuilder) repéré dans l'arbre : sa coquille + son squelette.</summary>
    public sealed class NxMagnetAssembly
    {
        /// <summary>Composant « ...AIMANTS » (l'Ensemble Aimants).</summary>
        public Assemblies.Component Ensemble { get; set; }
        /// <summary>Pièce maître de l'Ensemble Aimants (= Ensemble.Prototype).</summary>
        public Part EnsemblePart { get; set; }
        /// <summary>Composant squelette « ...SQL » (1er sous-produit), null si coquille incomplète.</summary>
        public Assemblies.Component Skeleton { get; set; }
        /// <summary>Pièce maître du squelette (porte les CSYS de montage).</summary>
        public Part SkeletonPart { get; set; }

        public bool IsComplete => Ensemble != null && Skeleton != null && SkeletonPart != null;
    }

    /// <summary>Une cellule de l'anneau (enfant direct de la racine) + ses Ensembles Aimants.</summary>
    public sealed class NxCell
    {
        public string Name { get; set; }
        public Assemblies.Component Root { get; set; }
        public List<NxMagnetAssembly> MagnetAssemblies { get; } = new List<NxMagnetAssembly>();

        /// <summary>Vrai si la cellule a au moins une coquille Ensemble Aimants exploitable.</summary>
        public bool HasMagnetAssembly
        {
            get
            {
                foreach (var a in MagnetAssemblies) if (a.IsComplete) return true;
                return false;
            }
        }
    }
}
