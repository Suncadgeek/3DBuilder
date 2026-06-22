using System;
using System.Windows.Forms;

namespace ThreeDBuilder.Preview
{
    /// <summary>Point d'entrée d'aperçu : ouvre la fenêtre 3DBuilder hors NX pour juger la mise en page.</summary>
    internal static class PreviewProgram
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ThreeDBuilder.MainForm());
        }
    }
}
