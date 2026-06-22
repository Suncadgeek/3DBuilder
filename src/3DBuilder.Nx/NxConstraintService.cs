using System.Collections.Generic;
using NXOpen;
using ThreeDBuilder.Core.Naming;
using Features = NXOpen.Features;
using Assemblies = NXOpen.Assemblies;
using Positioning = NXOpen.Positioning;

namespace ThreeDBuilder.Nx
{
    /// <summary>Issue d'une tentative de contrainte (pour le bilan post-génération — n°2).</summary>
    public sealed class ConstraintOutcome
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public static ConstraintOutcome Success() => new ConstraintOutcome { Ok = true };
        public static ConstraintOutcome Fail(string error) => new ConstraintOutcome { Ok = false, Error = error };
    }

    /// <summary>
    /// Pose la contrainte d'alignement entre le CSYS « RPM » de l'aimant et le CSYS de montage médian
    /// du squelette. Portage fidèle de l'ancien SetConstraints, mais le CSYS squelette est désigné par
    /// son nom EXACT (déjà sélectionné par <see cref="NxSkeletonReader"/>) au lieu d'un filtre InStr.
    /// </summary>
    public sealed class NxConstraintService
    {
        private const string DatumCsysType = "DATUM_CSYS";
        private readonly NamingService _naming;

        public NxConstraintService(NamingService naming) { _naming = naming; }

        public ConstraintOutcome Constrain(
            Assemblies.Component skelComp,
            Assemblies.Component magnetComp,
            Part assemblyPart,
            string skeletonCsysFeatureName)
        {
            // --- CSYS squelette : feature exacte → JournalIdentifier ---
            string skelJi = FindFeatureJournalId((Part)skelComp.Prototype.OwningPart, name =>
                name == skeletonCsysFeatureName);
            if (skelJi == null)
                return ConstraintOutcome.Fail("CSYS squelette introuvable : " + skeletonCsysFeatureName);

            // --- CSYS aimant : repère RPM, exactement un attendu (n°4) ---
            var rpmJis = FindFeatureJournalIds((Part)magnetComp.Prototype.OwningPart, name =>
                _naming.IsRpmCsys(name));
            if (rpmJis.Count == 0)
                return ConstraintOutcome.Fail("Aucun CSYS 'RPM' dans l'aimant " + magnetComp.DisplayName);
            if (rpmJis.Count > 1)
                return ConstraintOutcome.Fail("CSYS 'RPM' ambigu (" + rpmJis.Count + ") dans " + magnetComp.DisplayName);

            var skelCsys = (CartesianCoordinateSystem)skelComp.FindObject("PROTO#.Features|" + skelJi + "|CSYSTEM 1");
            var magnetCsys = (CartesianCoordinateSystem)magnetComp.FindObject("PROTO#.Features|" + rpmJis[0] + "|CSYSTEM 1");

            Positioning.ComponentPositioner positioner = assemblyPart.ComponentAssembly.Positioner;
            positioner.ClearNetwork();
            positioner.BeginAssemblyConstraints();

            Positioning.Network network = positioner.EstablishNetwork();
            var componentNetwork = (Positioning.ComponentNetwork)network;
            componentNetwork.MoveObjectsState = true;
            componentNetwork.DisplayComponent = null;

            Positioning.Constraint constraint = positioner.CreateConstraint(true);
            var cc = (Positioning.ComponentConstraint)constraint;
            cc.ConstraintAlignment = Positioning.Constraint.Alignment.InferAlign;
            cc.ConstraintType = Positioning.Constraint.Type.Touch;

            cc.CreateConstraintReference(skelComp, skelCsys, false, false);
            Positioning.ConstraintReference cr2 = cc.CreateConstraintReference(magnetComp, magnetCsys, false, false);
            cr2.SetFixHint(true);

            componentNetwork.Solve();

            positioner.ClearNetwork();
            positioner.DeleteNonPersistentConstraints();
            positioner.EndAssemblyConstraints();

            return ConstraintOutcome.Success();
        }

        private static string FindFeatureJournalId(Part part, System.Func<string, bool> match)
        {
            var all = FindFeatureJournalIds(part, match);
            return all.Count > 0 ? all[0] : null;
        }

        private static List<string> FindFeatureJournalIds(Part part, System.Func<string, bool> match)
        {
            var result = new List<string>();
            foreach (Features.Feature feat in part.Features)
            {
                if (feat.FeatureType != DatumCsysType) continue;
                if (match(feat.Name)) result.Add(feat.JournalIdentifier);
            }
            return result;
        }
    }
}
