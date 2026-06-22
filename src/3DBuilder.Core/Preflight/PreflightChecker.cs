using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDBuilder.Core.Excel;

namespace ThreeDBuilder.Core.Preflight
{
    /// <summary>
    /// Contrôle pré-génération (lecture seule, OBLIGATOIRE avant tout remplissage). Croise les codes
    /// extraits des squelettes du scope choisi avec le dictionnaire, et produit un rapport classé par
    /// sévérité (cf. REFACTO_MAPPING §0bis). Entièrement pur → testable sans NX.
    /// </summary>
    public sealed class PreflightChecker
    {
        // Catégories (libellés stables, réutilisés par l'UI / le journal).
        public const string CatDuplicateKeys = "DuplicateDictionaryKeys";
        public const string CatMissingAssembly = "MissingMagnetAssembly";
        public const string CatMissingFromDict = "MissingFromDictionary";
        public const string CatKnownExcluded = "KnownExcluded";
        public const string CatConventionDrift = "ConventionDrift";
        public const string CatAlreadyPopulated = "AlreadyPopulated";
        public const string CatMatched = "Matched";

        public PreflightReport Check(IEnumerable<CellPreflightInput> cells, MagnetDictionary dictionary, PreflightOptions options = null)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            options = options ?? new PreflightOptions();
            var report = new PreflightReport();
            var cellList = (cells ?? Enumerable.Empty<CellPreflightInput>()).ToList();

            // --- Erreur bloquante : doublons de clés dans le dictionnaire (D1). ---
            foreach (var dup in dictionary.DuplicateCodes)
                report.Findings.Add(new PreflightFinding(PreflightSeverity.Error, CatDuplicateKeys, null,
                    $"Code « {dup} » présent plusieurs fois dans le dictionnaire (un code = une réf TC)."));

            var referencedCodes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var cell in cellList)
            {
                var plan = new CellPlan { CellName = cell.CellName, CanFill = cell.HasMagnetAssembly };
                report.Cells.Add(plan);

                // --- Précondition : Ensemble Aimants absent (n°3) → warning, cellule non remplie. ---
                if (!cell.HasMagnetAssembly)
                {
                    report.Findings.Add(new PreflightFinding(PreflightSeverity.Warning, CatMissingAssembly, cell.CellName,
                        "Aucun Ensemble Aimants (coquille SKBuilder manquante) — cellule exclue du remplissage."));
                }

                var skeletonCodes = cell.SkeletonCodes ?? new List<string>();
                var placed = new HashSet<string>(cell.PlacedCodes ?? new List<string>(), StringComparer.Ordinal);
                int matchedCount = 0;

                // Diff par code (en conservant les répétitions : N occurrences = N aimants attendus).
                foreach (var code in skeletonCodes)
                {
                    referencedCodes.Add(code);

                    if (dictionary.Contains(code))
                    {
                        matchedCount++;
                        if (placed.Contains(code))
                            plan.AlreadyPlaced.Add(code);          // déjà posé → laissé intact (incrémental)
                        else if (cell.HasMagnetAssembly)
                            plan.ToAdd.Add(code);                  // manquant → à poser
                        continue;
                    }

                    // Absent du dico : attendu (D10) ou véritable manque ?
                    if (IsKnownExcluded(code, options.KnownExcludedPrefixes))
                    {
                        report.Findings.Add(new PreflightFinding(PreflightSeverity.Info, CatKnownExcluded, cell.CellName,
                            $"Code « {code} » absent du dictionnaire mais reconnu volontaire (préfixe exclu)."));
                    }
                    else
                    {
                        plan.Missing.Add(code);
                        report.Findings.Add(new PreflightFinding(PreflightSeverity.Warning, CatMissingFromDict, cell.CellName,
                            $"Code « {code} » absent du dictionnaire — aimant sauté."));
                    }
                }

                // --- Dérive de convention : des squelettes mais 0 code matché (n°7). ---
                if (skeletonCodes.Count > 0 && matchedCount == 0)
                {
                    report.Findings.Add(new PreflightFinding(PreflightSeverity.Warning, CatConventionDrift, cell.CellName,
                        $"{skeletonCodes.Count} CSYS de montage mais aucun code reconnu — convention de nommage probablement obsolète."));
                }

                // --- Idempotence : cellule déjà (partiellement) peuplée (n°1). ---
                if (placed.Count > 0)
                {
                    report.Findings.Add(new PreflightFinding(PreflightSeverity.Info, CatAlreadyPopulated, cell.CellName,
                        $"Cellule déjà peuplée ({plan.AlreadyPlaced.Count} présents) — mode incrémental : {plan.ToAdd.Count} à ajouter."));
                }
                else if (plan.ToAdd.Count > 0)
                {
                    report.Findings.Add(new PreflightFinding(PreflightSeverity.Info, CatMatched, cell.CellName,
                        $"{plan.ToAdd.Count} aimant(s) à poser."));
                }
            }

            // --- Entrées du dictionnaire jamais utilisées (info). ---
            report.UnusedDictionaryEntries.AddRange(
                dictionary.Codes.Where(c => !referencedCodes.Contains(c)).OrderBy(c => c, StringComparer.Ordinal));

            return report;
        }

        private static bool IsKnownExcluded(string code, IReadOnlyList<string> prefixes)
        {
            if (prefixes == null) return false;
            foreach (var p in prefixes)
                if (!string.IsNullOrEmpty(p) && code.StartsWith(p, StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
