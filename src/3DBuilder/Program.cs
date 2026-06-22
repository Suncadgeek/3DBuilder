using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NXOpen;

namespace ThreeDBuilder
{
    /// <summary>Point d'entrée chargé par NX (Fichier ▸ Exécuter ▸ NX Open → 3DBuilder.dll).</summary>
    public static class Program
    {
        static Program()
        {
            // NX (ugraf.exe) charge ses propres System.* et n'applique pas nos binding redirects. On
            // résout donc nous-mêmes les dépendances (ClosedXML, System.Text.Json, System.Memory…)
            // depuis le dossier de l'add-in, en ignorant la version demandée. CRITIQUE : sans ça,
            // FileLoadException sous NX (cf. REFACTO_PLAYBOOK §3.1). Recopié de SKB2.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAddinFolder;
        }

        public static void Main(string[] args)
        {
            using (var form = new MainForm())
            {
                form.ShowDialog();
            }
        }

        /// <summary>Décharge l'image dès la fin de l'exécution (ex-GetUnloadOption).</summary>
        public static int GetUnloadOption(string dummy)
        {
            return (int)Session.LibraryUnloadOption.Immediately;
        }

        private static Assembly ResolveFromAddinFolder(object sender, ResolveEventArgs args)
        {
            var simpleName = new AssemblyName(args.Name).Name;

            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == simpleName);
            if (loaded != null) return loaded;

            try
            {
                var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var candidate = Path.Combine(dir, simpleName + ".dll");
                if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            }
            catch
            {
                // laisse le résolveur standard échouer proprement
            }
            return null;
        }
    }
}
