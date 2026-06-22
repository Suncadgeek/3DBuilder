using System.Collections.Generic;
using ThreeDBuilder.Core.Model;

namespace ThreeDBuilder.Core.Config
{
    /// <summary>
    /// Entrées utilisateur d'une génération (remplace les 2 lignes du .ini plat — D5).
    /// Sérialisée en JSON par <see cref="ConfigStore"/>.
    /// </summary>
    public sealed class GenerationConfig
    {
        /// <summary>Chemin du dictionnaire aimants (.xlsx, 2 colonnes A=réf TC / B=code).</summary>
        public string DictionaryExcelPath { get; set; } = "";

        /// <summary>Réf TC de l'anneau de stockage à remplir (mode managé : ouvert via @DB/&lt;ref&gt;).</summary>
        public string StorageRingTcRef { get; set; } = "";

        /// <summary>Mode d'exécution (Auto/Natif/Managé — D11).</summary>
        public NxRunMode PdmMode { get; set; } = NxRunMode.Auto;

        /// <summary>Comportement sur cellule déjà remplie (incrémental/force — n°1).</summary>
        public FillMode FillMode { get; set; } = FillMode.Incremental;

        /// <summary>Cellules à remplir. Vide = tout l'anneau.</summary>
        public List<string> SelectedCells { get; set; } = new List<string>();

        // --- Mode natif (test) uniquement — D11 ---

        /// <summary>Dossier contenant les pièces aimants &lt;réf TC&gt;.prt (mode natif).</summary>
        public string NativeMagnetsFolder { get; set; } = "";

        /// <summary>Chemin de l'anneau / cellules en mode natif.</summary>
        public string NativeRingPath { get; set; } = "";
    }
}
