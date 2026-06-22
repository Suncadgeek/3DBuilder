using NXOpen;
using ThreeDBuilder.Core;
using Assemblies = NXOpen.Assemblies;

namespace ThreeDBuilder.Nx
{
    /// <summary>
    /// Renomme les instances de composants par le nom de leur prototype (ex-rename_instances /
    /// reportComponentChildren). Parcours récursif de l'arbre d'assemblage.
    /// </summary>
    public sealed class NxRenameService
    {
        private readonly NxContext _ctx;
        private readonly IBuildLog _log;

        public NxRenameService(NxContext ctx, IBuildLog log)
        {
            _ctx = ctx;
            _log = log ?? NullBuildLog.Instance;
        }

        public void RenameInstances()
        {
            var dispPart = _ctx.Session.Parts.Display;
            var ca = dispPart.ComponentAssembly;
            if (ca.RootComponent == null) return;
            Rename(ca.RootComponent);
        }

        private void Rename(Assemblies.Component comp)
        {
            foreach (var child in comp.GetChildren())
            {
                try
                {
                    var proto = child.Prototype as Part;
                    var newName = proto != null ? proto.Name : child.DisplayName;
                    var builder = _ctx.WorkPart.PropertiesManager
                        .CreateObjectGeneralPropertiesBuilder(new NXObject[] { child });
                    builder.Name = newName;
                    builder.Commit();
                    builder.Destroy();
                }
                catch (System.Exception ex)
                {
                    _log.Warn("Renommage impossible pour " + child.DisplayName + " : " + ex.Message);
                }
                Rename(child);
            }
        }
    }
}
