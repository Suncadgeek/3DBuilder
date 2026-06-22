using System;
using System.Collections.Generic;
using System.Linq;
using NXOpen;
using ThreeDBuilder.Core;
using ThreeDBuilder.Core.Config;
using ThreeDBuilder.Core.Excel;
using ThreeDBuilder.Core.Model;
using ThreeDBuilder.Core.Naming;
using ThreeDBuilder.Core.Preflight;
using ThreeDBuilder.Nx;
using Assemblies = NXOpen.Assemblies;

namespace ThreeDBuilder
{
    /// <summary>Résultat de la phase d'analyse (preflight, lecture seule).</summary>
    public sealed class AnalyzeResult
    {
        public PreflightReport Report { get; set; }
        public IReadOnlyList<string> CellNames { get; set; }
        public NxRunMode ResolvedMode { get; set; }
    }

    /// <summary>Bilan de la phase de remplissage (post-génération — n°2).</summary>
    public sealed class GenerationSummary
    {
        public int Added { get; set; }
        public int Failed { get; set; }
        public int SkippedMissing { get; set; }
        public bool Cancelled { get; set; }
        public List<string> Failures { get; } = new List<string>();
    }

    /// <summary>
    /// Orchestrateur unique (ex-Outline). Flux en 2 temps : Analyze (preflight lecture seule) puis Run
    /// (remplissage du scope confirmé, sous undo mark). Zéro logique dans l'UI.
    /// </summary>
    public sealed class GenerationService
    {
        private readonly NxContext _ctx;
        private readonly IBuildLog _log;
        private readonly NamingService _naming = new NamingService();

        private NxAssemblyService _assembly;
        private NxConstraintService _constraints;
        private NxSkeletonReader _skeleton;
        private NxRenameService _rename;

        // État conservé entre Analyze et Run.
        private IPartResolver _resolver;
        private MagnetDictionary _dictionary;
        private Dictionary<string, string> _tcRefToCode;
        private IReadOnlyList<NxCell> _cells;

        public GenerationService(NxContext ctx, IBuildLog log)
        {
            _ctx = ctx;
            _log = log ?? NullBuildLog.Instance;
        }

        // ------------------------------------------------------------------
        // Phase 1 — Analyse (lecture seule, AUCUNE écriture NX).
        // ------------------------------------------------------------------
        public AnalyzeResult Analyze(GenerationConfig config)
        {
            var mode = NxEnvironment.Resolve(config.PdmMode);
            _ctx.RunMode = mode;
            _log.Info("Mode d'exécution : " + mode);

            _resolver = (mode == NxRunMode.Native)
                ? (IPartResolver)new NativePartResolver(config.NativeRingPath, config.NativeMagnetsFolder)
                : new ManagedPartResolver(config.StorageRingTcRef);

            _assembly = new NxAssemblyService(_ctx, _log);
            _constraints = new NxConstraintService(_naming);
            _skeleton = new NxSkeletonReader(_naming);
            _rename = new NxRenameService(_ctx, _log);

            _dictionary = new MagnetDictionaryReader().Read(config.DictionaryExcelPath);
            _tcRefToCode = BuildReverse(_dictionary);
            _log.Info($"Dictionnaire : {_dictionary.CodeToTcRef.Count} codes.");

            _log.Info("Ouverture de l'anneau : " + _resolver.RingSpec());
            var ringToken = (mode == NxRunMode.Native)
                ? System.IO.Path.GetFileNameWithoutExtension(config.NativeRingPath)
                : config.StorageRingTcRef;
            _assembly.OpenStorageRing(_resolver, ringToken);
            _cells = _assembly.EnumerateCells();
            _log.Info($"{_cells.Count} cellule(s) trouvée(s) : " + string.Join(", ", _cells.Select(c => c.Name)));

            var inputs = BuildPreflightInputs(_cells, config.SelectedCells);
            var report = new PreflightChecker().Check(inputs, _dictionary);

            return new AnalyzeResult
            {
                Report = report,
                CellNames = _cells.Select(c => c.Name).ToList(),
                ResolvedMode = mode
            };
        }

        // ------------------------------------------------------------------
        // Phase 2 — Remplissage du scope confirmé.
        // ------------------------------------------------------------------
        public GenerationSummary Run(GenerationConfig config, IReadOnlyCollection<string> confirmedCells,
            FillMode fillMode, Func<bool> cancelRequested)
        {
            if (_cells == null) throw new InvalidOperationException("Analyze() doit être appelé avant Run().");
            cancelRequested = cancelRequested ?? (() => false);
            var summary = new GenerationSummary();

            var scope = SelectScope(_cells, confirmedCells);
            var theSession = _ctx.Session;
            var markId = theSession.SetUndoMark(Session.MarkVisibility.Visible, "3DBuilder — remplissage");

            int totalMounts = scope.Sum(c => c.MagnetAssemblies.Where(a => a.IsComplete)
                .Sum(a => _skeleton.ReadMounts(a.SkeletonPart).Count));
            int done = 0;
            var openedMagnets = new Dictionary<string, Part>(StringComparer.Ordinal);

            try
            {
                foreach (var cell in scope)
                {
                    foreach (var asm in cell.MagnetAssemblies.Where(a => a.IsComplete))
                    {
                        if (fillMode == FillMode.ForceRefill)
                        {
                            int purged = _assembly.PurgeMagnets(asm);
                            if (purged > 0) _log.Info($"[{cell.Name}] purge : {purged} aimant(s) retiré(s).");
                        }

                        var remaining = PlacedCodeCounts(asm, fillMode);

                        foreach (var mount in _skeleton.ReadMounts(asm.SkeletonPart))
                        {
                            if (cancelRequested()) { summary.Cancelled = true; break; }
                            done++;
                            _log.Progress(done, totalMounts);

                            string tcRef;
                            if (!_dictionary.TryGetTcRef(mount.Code, out tcRef))
                            {
                                summary.SkippedMissing++;
                                continue; // déjà signalé au preflight
                            }

                            // Incrémental : si un aimant de ce code est déjà posé, on le « consomme ».
                            int n;
                            if (remaining.TryGetValue(mount.Code, out n) && n > 0)
                            {
                                remaining[mount.Code] = n - 1;
                                continue;
                            }

                            try
                            {
                                Part magnetPart;
                                if (!openedMagnets.TryGetValue(tcRef, out magnetPart))
                                {
                                    magnetPart = _assembly.OpenMagnetPart(_resolver, tcRef);
                                    openedMagnets[tcRef] = magnetPart;
                                }
                                var comp = _assembly.AddMagnet(asm, magnetPart);
                                var outcome = _constraints.Constrain(asm.Skeleton, comp, asm.EnsemblePart, mount.CsysFeatureName);
                                if (outcome.Ok) summary.Added++;
                                else { summary.Failed++; summary.Failures.Add($"[{cell.Name}] {mount.Code} : {outcome.Error}"); }
                            }
                            catch (Exception ex)
                            {
                                summary.Failed++;
                                summary.Failures.Add($"[{cell.Name}] {mount.Code} : {ex.Message}");
                            }
                        }
                        if (summary.Cancelled) break;
                    }
                    if (summary.Cancelled) break;
                }

                _assembly.SetWorkToRoot();
                _rename.RenameInstances();
            }
            catch (Exception ex)
            {
                _log.Error("Échec de la génération : " + ex.Message);
                theSession.UndoToMark(markId, null);
                throw;
            }

            _log.Info($"Bilan : {summary.Added} posés, {summary.Failed} échec(s), {summary.SkippedMissing} sauté(s)"
                      + (summary.Cancelled ? " — ANNULÉ" : "") + ".");
            return summary;
        }

        // ------------------------------------------------------------------
        private List<CellPreflightInput> BuildPreflightInputs(IReadOnlyList<NxCell> cells, List<string> selected)
        {
            var inputs = new List<CellPreflightInput>();
            foreach (var cell in SelectScope(cells, selected))
            {
                var codes = new List<string>();
                var placed = new List<string>();
                foreach (var asm in cell.MagnetAssemblies)
                {
                    if (asm.SkeletonPart != null) codes.AddRange(_skeleton.ReadCodes(asm.SkeletonPart));
                    foreach (var tc in _assembly.GetPlacedMagnetTcRefs(asm))
                        if (_tcRefToCode.TryGetValue(tc, out var code)) placed.Add(code);
                }
                inputs.Add(new CellPreflightInput
                {
                    CellName = cell.Name,
                    HasMagnetAssembly = cell.HasMagnetAssembly,
                    SkeletonCodes = codes,
                    PlacedCodes = placed
                });
            }
            return inputs;
        }

        private Dictionary<string, int> PlacedCodeCounts(NxMagnetAssembly asm, FillMode fillMode)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (fillMode == FillMode.ForceRefill) return counts; // purgé → rien de placé
            foreach (var tc in _assembly.GetPlacedMagnetTcRefs(asm))
                if (_tcRefToCode.TryGetValue(tc, out var code))
                    counts[code] = counts.TryGetValue(code, out var n) ? n + 1 : 1;
            return counts;
        }

        private static IReadOnlyList<NxCell> SelectScope(IReadOnlyList<NxCell> cells, IReadOnlyCollection<string> selected)
        {
            if (selected == null || selected.Count == 0) return cells; // vide = tout l'anneau
            var set = new HashSet<string>(selected, StringComparer.Ordinal);
            return cells.Where(c => set.Contains(c.Name)).ToList();
        }

        private static Dictionary<string, string> BuildReverse(MagnetDictionary dict)
        {
            var rev = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in dict.CodeToTcRef)
                if (!rev.ContainsKey(kv.Value)) rev[kv.Value] = kv.Key;
            return rev;
        }
    }
}
