using System.Collections.Generic;
using System.Linq;

namespace ThreeDBuilder.Core.Preflight
{
    /// <summary>
    /// Données d'entrée du preflight pour UNE cellule. Toute la partie NX (parcours d'arbre, lecture
    /// des features, composants déjà posés) est faite côté Nx ; le Core ne reçoit que ces faits, ce qui
    /// rend le <see cref="PreflightChecker"/> entièrement testable.
    /// </summary>
    public sealed class CellPreflightInput
    {
        /// <summary>Nom de la cellule (ex. ARC14).</summary>
        public string CellName { get; set; }

        /// <summary>La coquille « Ensemble Aimants » existe-t-elle ? (précondition SKBuilder — n°3).</summary>
        public bool HasMagnetAssembly { get; set; }

        /// <summary>Codes aimants attendus, extraits des CSYS de montage du squelette (avec répétitions).</summary>
        public IReadOnlyList<string> SkeletonCodes { get; set; } = new List<string>();

        /// <summary>Codes aimants déjà posés dans l'Ensemble Aimants (pour le diff incrémental — n°1).</summary>
        public IReadOnlyList<string> PlacedCodes { get; set; } = new List<string>();
    }

    public sealed class PreflightOptions
    {
        /// <summary>Préfixes de codes squelette dont l'absence du dico est ATTENDUE (D10). Défaut : OCT_, QCORR_.</summary>
        public IReadOnlyList<string> KnownExcludedPrefixes { get; set; } = new[] { "OCT_", "QCORR_" };
    }

    public enum PreflightSeverity { Info, Warning, Error }

    /// <summary>Une entrée du rapport, rattachée éventuellement à une cellule.</summary>
    public sealed class PreflightFinding
    {
        public PreflightSeverity Severity { get; }
        public string Category { get; }
        public string Cell { get; }
        public string Detail { get; }

        public PreflightFinding(PreflightSeverity severity, string category, string cell, string detail)
        {
            Severity = severity;
            Category = category;
            Cell = cell;
            Detail = detail;
        }

        public override string ToString()
        {
            var tag = Severity == PreflightSeverity.Error ? "ERREUR"
                    : Severity == PreflightSeverity.Warning ? "AVERT." : "INFO";
            var loc = string.IsNullOrEmpty(Cell) ? "" : "[" + Cell + "] ";
            return $"[{tag}] {loc}{Category}: {Detail}";
        }
    }

    /// <summary>Plan d'import calculé pour une cellule (ce qui sera réellement posé).</summary>
    public sealed class CellPlan
    {
        public string CellName { get; set; }
        public bool CanFill { get; set; }
        /// <summary>Codes à AJOUTER (manquants), en mode incrémental.</summary>
        public List<string> ToAdd { get; } = new List<string>();
        /// <summary>Codes déjà présents (laissés intacts en incrémental).</summary>
        public List<string> AlreadyPlaced { get; } = new List<string>();
        /// <summary>Codes squelette absents du dictionnaire (sautés).</summary>
        public List<string> Missing { get; } = new List<string>();
    }

    /// <summary>Résultat du preflight : findings (avec sévérité/override) + plan par cellule.</summary>
    public sealed class PreflightReport
    {
        public List<PreflightFinding> Findings { get; } = new List<PreflightFinding>();
        public List<CellPlan> Cells { get; } = new List<CellPlan>();

        /// <summary>Codes du dictionnaire jamais référencés par aucun squelette (info).</summary>
        public List<string> UnusedDictionaryEntries { get; } = new List<string>();

        /// <summary>Vrai s'il existe au moins une erreur bloquante (non contournable).</summary>
        public bool HasBlockingErrors => Findings.Any(f => f.Severity == PreflightSeverity.Error);

        /// <summary>Vrai s'il existe au moins un warning (contournable par override explicite).</summary>
        public bool HasWarnings => Findings.Any(f => f.Severity == PreflightSeverity.Warning);

        /// <summary>Nombre total d'aimants qui seront posés sur l'ensemble du scope.</summary>
        public int TotalToAdd => Cells.Sum(c => c.ToAdd.Count);

        public override string ToString()
            => string.Join("\n", Findings.Select(f => f.ToString()));
    }
}
