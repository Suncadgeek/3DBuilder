using NXOpen;
using NXOpen.UF;
using ThreeDBuilder.Core.Model;

namespace ThreeDBuilder.Nx
{
    /// <summary>
    /// Contexte de session NX partagé par les services de l'adapter. Remplace l'état global du
    /// Module VB (theSession, ufs, workPart, displayPart, storagering_part).
    /// </summary>
    public sealed class NxContext
    {
        public Session Session { get; }
        public UFSession Uf { get; }
        public ListingWindow ListingWindow { get; }

        public Part WorkPart { get; set; }
        public Part DisplayPart { get; set; }

        /// <summary>Anneau de stockage ouvert (ex-storagering_part du VB).</summary>
        public Part StorageRing { get; set; }

        /// <summary>Mode effectif résolu (Native/Managed) — pilote la résolution de pièce.</summary>
        public NxRunMode RunMode { get; set; }

        public bool TeamCenter => RunMode == NxRunMode.Managed;

        public NxContext()
        {
            Session = Session.GetSession();
            Uf = UFSession.GetUFSession();
            ListingWindow = Session.ListingWindow;
            WorkPart = Session.Parts.Work;
            DisplayPart = Session.Parts.Display;
        }

        /// <summary>Resynchronise WorkPart/DisplayPart depuis la session.</summary>
        public void RefreshParts()
        {
            WorkPart = Session.Parts.Work;
            DisplayPart = Session.Parts.Display;
        }
    }
}
