namespace ThreeDBuilder.Core.Model
{
    /// <summary>Mode d'exécution NX : natif (test, fichiers .prt) ou managé (prod, Teamcenter @DB/). Cf. D11.</summary>
    public enum NxRunMode
    {
        /// <summary>Laisser 3DBuilder détecter (PdmSession), avec repli heuristique.</summary>
        Auto = 0,
        /// <summary>Test : pièces résolues depuis un dossier fichier-système.</summary>
        Native = 1,
        /// <summary>Prod : pièces résolues via @DB/&lt;réf TC&gt; (Teamcenter).</summary>
        Managed = 2
    }

    /// <summary>
    /// Comportement sur une cellule déjà (partiellement) remplie. Cf. feature n°1 (idempotence).
    /// </summary>
    public enum FillMode
    {
        /// <summary>Défaut : n'ajoute que les aimants manquants ; ne touche pas aux déjà-posés.</summary>
        Incremental = 0,
        /// <summary>Purge d'abord les composants aimants (squelette conservé) puis remplit à neuf.
        /// Exige une confirmation utilisateur explicite.</summary>
        ForceRefill = 1
    }
}
