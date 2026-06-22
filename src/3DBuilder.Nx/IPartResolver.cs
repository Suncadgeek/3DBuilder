using System;
using System.IO;

namespace ThreeDBuilder.Nx
{
    /// <summary>
    /// Produit les identifiants de pièce passés à <c>Parts.Open*</c>. Seule différence entre les modes
    /// Natif et Managé (D11) : la façon de nommer l'anneau et les aimants. L'ouverture elle-même est
    /// identique dans les deux modes.
    /// </summary>
    public interface IPartResolver
    {
        string Mode { get; }
        /// <summary>Identifiant de l'anneau de stockage à ouvrir.</summary>
        string RingSpec();
        /// <summary>Identifiant de la pièce aimant pour une réf TC.</summary>
        string MagnetSpec(string tcRef);
    }

    /// <summary>Managé (prod) : pièces résolues via @DB/&lt;réf TC&gt; (Teamcenter, latest working).</summary>
    public sealed class ManagedPartResolver : IPartResolver
    {
        private readonly string _ringTcRef;
        public ManagedPartResolver(string ringTcRef) { _ringTcRef = (ringTcRef ?? "").Trim(); }

        public string Mode => "Managé";
        public string RingSpec() => "@DB/" + _ringTcRef;
        public string MagnetSpec(string tcRef) => "@DB/" + (tcRef ?? "").Trim();
    }

    /// <summary>Natif (test) : pièces résolues depuis un dossier fichier-système.</summary>
    public sealed class NativePartResolver : IPartResolver
    {
        private readonly string _ringPath;
        private readonly string _magnetsFolder;

        public NativePartResolver(string ringPath, string magnetsFolder)
        {
            _ringPath = ringPath ?? "";
            _magnetsFolder = magnetsFolder ?? "";
        }

        public string Mode => "Natif";
        public string RingSpec() => _ringPath;
        public string MagnetSpec(string tcRef) => Path.Combine(_magnetsFolder, (tcRef ?? "").Trim() + ".prt");
    }
}
