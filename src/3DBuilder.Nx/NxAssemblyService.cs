using System;
using System.Collections.Generic;
using NXOpen;
using ThreeDBuilder.Core;
using Assemblies = NXOpen.Assemblies;

namespace ThreeDBuilder.Nx
{
    /// <summary>
    /// Adapter NXOpen pour le remplissage : ouverture de l'anneau, parcours des cellules / Ensembles
    /// Aimants, lecture des aimants déjà posés, ajout et purge de composants. Porte les routines VB
    /// Outline / Open_Parts / Import_Magnets (partie assemblage) en services isolés.
    ///
    /// La traversée identifie les sous-produits par NOM (« _AIMANTS » / « _SQL ») plutôt que par index
    /// fixe (GetChildren(1).GetChildren(0) du VB) — corrige la fragilité du constat n°4.
    /// </summary>
    public sealed class NxAssemblyService
    {
        public const string EnsembleToken = "AIMANTS";
        public const string SkeletonToken = "SQL";

        private readonly NxContext _ctx;
        private readonly IBuildLog _log;

        public NxAssemblyService(NxContext ctx, IBuildLog log)
        {
            _ctx = ctx;
            _log = log ?? NullBuildLog.Instance;
        }

        // ------------------------------------------------------------------
        // Ouverture de l'anneau (ex-Outline début).
        // ------------------------------------------------------------------
        public Part OpenStorageRing(IPartResolver resolver, string matchToken)
        {
            var theSession = _ctx.Session;

            // Si l'anneau est DÉJÀ ouvert (ex. réanalyse après mise à jour du dico, sans fermer la
            // pièce), on le réutilise au lieu de tenter une réouverture. Remplace le garde « aucune
            // pièce ouverte → abandon » du VB, trop strict.
            var existing = FindLoadedPart(matchToken);
            if (existing != null)
            {
                PartLoadStatus plsReuse;
                theSession.Parts.SetActiveDisplay(existing, DisplayPartOption.AllowAdditional,
                    PartDisplayPartWorkPartOption.UseLast, out plsReuse);
                plsReuse.Dispose();
                _ctx.StorageRing = existing;
                _ctx.RefreshParts();
                _log.Info("Anneau déjà ouvert : réutilisé sans réouverture.");
                return existing;
            }

            theSession.Parts.LoadOptions.UsePartialLoading = false;
            PartLoadStatus pls;
            var ring = (Part)theSession.Parts.OpenActiveDisplay(
                resolver.RingSpec(), DisplayPartOption.AllowAdditional, out pls);
            pls.Dispose();
            _ctx.StorageRing = ring;
            _ctx.RefreshParts();
            return ring;
        }

        /// <summary>Cherche une pièce déjà chargée dont le nom/leaf contient le jeton (réf TC ou nom de fichier).</summary>
        private Part FindLoadedPart(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            foreach (BasePart bp in _ctx.Session.Parts)
            {
                var p = bp as Part;
                if (p == null) continue;
                if ((p.Name ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0
                    || (p.Leaf ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Énumération des cellules (enfants directs de la racine) + leurs Ensembles Aimants.
        // ------------------------------------------------------------------
        public IReadOnlyList<NxCell> EnumerateCells()
        {
            var cells = new List<NxCell>();
            var root = _ctx.StorageRing.ComponentAssembly.RootComponent;
            if (root == null) return cells;

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var child in root.GetChildren())
            {
                var name = ResolveCellName(child);
                // Garantit des noms UNIQUES (sinon la conservation de sélection et le scope ne peuvent
                // pas distinguer deux cellules homonymes).
                int k;
                if (seen.TryGetValue(name, out k)) { seen[name] = k + 1; name = name + " #" + (k + 1); }
                else seen[name] = 1;

                var cell = new NxCell { Name = name, Root = child };
                foreach (var ens in FindDescendants(child, EnsembleToken))
                    cell.MagnetAssemblies.Add(BuildMagnetAssembly(ens));
                cells.Add(cell);
            }
            // L'arbre remonte les cellules en ordre inverse → on rétablit l'ordre naturel (01 en premier).
            cells.Reverse();
            return cells;
        }

        /// <summary>
        /// Nom lisible d'une cellule. DisplayName retombe parfois sur un libellé générique (ex. le
        /// dossier en natif) → on privilégie le nom d'instance puis le leaf du prototype (nom de pièce).
        /// </summary>
        private static string ResolveCellName(Assemblies.Component cell)
        {
            var inst = cell.Name;
            if (!string.IsNullOrWhiteSpace(inst)) return inst.Trim();
            var proto = cell.Prototype as Part;
            if (proto != null && !string.IsNullOrWhiteSpace(proto.Leaf)) return proto.Leaf.Trim();
            if (proto != null && !string.IsNullOrWhiteSpace(proto.Name)) return FirstSegment(proto.Name);
            var dn = cell.DisplayName;
            return string.IsNullOrWhiteSpace(dn) ? "Cellule" : dn.Trim();
        }

        private NxMagnetAssembly BuildMagnetAssembly(Assemblies.Component ensemble)
        {
            var a = new NxMagnetAssembly
            {
                Ensemble = ensemble,
                EnsemblePart = ensemble.Prototype as Part
            };
            foreach (var sub in ensemble.GetChildren())
            {
                if (NameContains(sub, SkeletonToken))
                {
                    a.Skeleton = sub;
                    a.SkeletonPart = sub.Prototype as Part;
                    break;
                }
            }
            return a;
        }

        /// <summary>Réfs TC des aimants déjà posés dans un Ensemble Aimants (tout sauf le squelette).</summary>
        public IReadOnlyList<string> GetPlacedMagnetTcRefs(NxMagnetAssembly assembly)
        {
            var refs = new List<string>();
            if (assembly?.Ensemble == null) return refs;
            foreach (var sub in assembly.Ensemble.GetChildren())
            {
                if (assembly.Skeleton != null && ReferenceEquals(sub, assembly.Skeleton)) continue;
                if (NameContains(sub, SkeletonToken)) continue; // sécurité
                var proto = sub.Prototype as Part;
                var name = proto != null ? proto.Name : sub.DisplayName;
                refs.Add(FirstSegment(name));
            }
            return refs;
        }

        // ------------------------------------------------------------------
        // Ouverture d'une pièce aimant (la charge dans la session pour l'ajout).
        // ------------------------------------------------------------------
        public Part OpenMagnetPart(IPartResolver resolver, string tcRef)
        {
            var theSession = _ctx.Session;
            PartLoadStatus pls;
            var part = (Part)theSession.Parts.OpenActiveDisplay(
                resolver.MagnetSpec(tcRef), DisplayPartOption.AllowAdditional, out pls);
            pls.Dispose();
            _ctx.RefreshParts();
            return part;
        }

        // ------------------------------------------------------------------
        // Ajout d'un aimant comme composant dans l'Ensemble Aimants (ex-Import_Magnets, bloc add).
        // ------------------------------------------------------------------
        public Assemblies.Component AddMagnet(NxMagnetAssembly assembly, Part magnetPart)
        {
            var theSession = _ctx.Session;
            PartLoadStatus pls;

            theSession.Parts.SetActiveDisplay(_ctx.StorageRing, DisplayPartOption.AllowAdditional,
                PartDisplayPartWorkPartOption.UseLast, out pls);
            _ctx.RefreshParts();
            pls.Dispose();

            theSession.Parts.SetWorkComponent(assembly.Ensemble, PartCollection.RefsetOption.Entire,
                PartCollection.WorkComponentOption.Visible, out pls);
            _ctx.RefreshParts();
            pls.Dispose();

            var acb = assembly.EnsemblePart.AssemblyManager.CreateAddComponentBuilder();
            try
            {
                acb.ReferenceSet = PreferredReferenceSet(magnetPart);
                acb.SetPartsToAdd(new Part[] { magnetPart });
                var committed = acb.Commit();
                return (Assemblies.Component)committed;
            }
            finally
            {
                acb.Destroy();
            }
        }

        // ------------------------------------------------------------------
        // Purge des aimants d'un Ensemble Aimants (mode forcé n°1) — le squelette est CONSERVÉ.
        // ------------------------------------------------------------------
        public int PurgeMagnets(NxMagnetAssembly assembly)
        {
            var theSession = _ctx.Session;
            var toDelete = new List<TaggedObject>();
            foreach (var sub in assembly.Ensemble.GetChildren())
            {
                if (assembly.Skeleton != null && ReferenceEquals(sub, assembly.Skeleton)) continue;
                if (NameContains(sub, SkeletonToken)) continue;
                toDelete.Add(sub);
            }
            if (toDelete.Count == 0) return 0;

            var markId = theSession.SetUndoMark(Session.MarkVisibility.Visible, "Purge aimants");
            theSession.UpdateManager.ClearErrorList();
            theSession.UpdateManager.AddObjectsToDeleteList(toDelete.ToArray());
            theSession.UpdateManager.DoUpdate(markId);
            return toDelete.Count;
        }

        // ------------------------------------------------------------------
        // Retour de la pièce de travail sur la racine de l'anneau (ex-Outline fin).
        // ------------------------------------------------------------------
        public void SetWorkToRoot()
        {
            var theSession = _ctx.Session;
            PartLoadStatus pls;
            Assemblies.Component nullComp = null;
            theSession.Parts.SetWorkComponent(nullComp, PartCollection.RefsetOption.Entire,
                PartCollection.WorkComponentOption.Visible, out pls);
            pls.Dispose();
            _ctx.RefreshParts();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private IEnumerable<Assemblies.Component> FindDescendants(Assemblies.Component root, string token)
        {
            foreach (var child in root.GetChildren())
            {
                if (NameContains(child, token))
                    yield return child;
                else
                    foreach (var d in FindDescendants(child, token))
                        yield return d;
            }
        }

        private static bool NameContains(Assemblies.Component comp, string token)
        {
            if (comp == null) return false;
            var dn = comp.DisplayName ?? "";
            if (dn.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var proto = comp.Prototype as Part;
            var pn = proto != null ? proto.Name : "";
            return pn.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Ensemble de référence de l'aimant ajouté : « MODEL » par défaut (prod). Si la pièce n'a pas
        /// de MODEL (cas des templates de test), on retombe sur « Entire Part » pour que la géométrie
        /// reste visible plutôt qu'un composant vide. Les squelettes ont leurs propres règles (exclusion
        /// du MODEL) côté SKBuilder — non concernés ici.
        /// </summary>
        private string PreferredReferenceSet(Part magnetPart)
        {
            const string Model = "MODEL";
            const string Entire = "Entire Part";
            try
            {
                var sets = magnetPart.GetAllReferenceSets();
                if (sets != null)
                    foreach (var rs in sets)
                        if (rs != null && rs.Name == Model) return Model;
            }
            catch { /* ignore : on retombe sur Entire Part */ }
            _log.Warn("Aimant sans ensemble de référence MODEL (" + (magnetPart.Leaf ?? magnetPart.Name)
                      + ") → 'Entire Part'.");
            return Entire;
        }

        private static string FirstSegment(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int slash = name.IndexOf('/');
            return slash >= 0 ? name.Substring(0, slash) : name;
        }
    }
}
