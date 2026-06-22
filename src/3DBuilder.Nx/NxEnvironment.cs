using System;
using NXOpen;
using ThreeDBuilder.Core.Model;

namespace ThreeDBuilder.Nx
{
    /// <summary>
    /// Détection du mode d'exécution NX (natif vs managé/Teamcenter — D11). On interroge la session
    /// PDM : en managé, la connexion Teamcenter est renseignée. Repli sur les variables d'environnement.
    /// Sert de valeur PAR DÉFAUT ; l'utilisateur peut forcer le mode dans l'UI (Auto/Natif/Managé).
    /// </summary>
    public static class NxEnvironment
    {
        /// <summary>Résout un mode demandé (éventuellement Auto) en mode effectif Native/Managed.</summary>
        public static NxRunMode Resolve(NxRunMode requested)
        {
            if (requested == NxRunMode.Native || requested == NxRunMode.Managed) return requested;
            return Detect();
        }

        public static NxRunMode Detect()
        {
            try
            {
                string connectString, discriminator;
                Session.GetSession().PdmSession.GetTcserverSettings(out connectString, out discriminator);
                if (!string.IsNullOrEmpty(connectString)) return NxRunMode.Managed;
                return NxRunMode.Native;
            }
            catch
            {
                return LooksManaged() ? NxRunMode.Managed : NxRunMode.Native;
            }
        }

        public static bool LooksManaged()
        {
            var ugmgr = Env("UGII_UGMGR");
            if (!string.IsNullOrEmpty(ugmgr) && ugmgr.IndexOf("yes", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (!string.IsNullOrEmpty(Env("UGII_TC_INSTALL_DIR"))) return true;
            if (!string.IsNullOrEmpty(Env("TC_ROOT")) && !string.IsNullOrEmpty(Env("FMS_HOME"))) return true;
            return false;
        }

        private static string Env(string name)
        {
            try { return Environment.GetEnvironmentVariable(name); }
            catch { return null; }
        }
    }
}
