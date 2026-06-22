using System;
using System.Windows.Forms;
using ThreeDBuilder.Core;

namespace ThreeDBuilder
{
    /// <summary>
    /// Implémentation WinForms d'<see cref="IBuildLog"/> : journalise dans la zone de texte et pilote
    /// la barre de progression. DoEvents garde l'UI réactive (NX est mono-thread STA → pas de vrai
    /// background thread ; le bouton Annuler est traité via le pompage d'événements).
    /// </summary>
    public sealed class UiBuildLog : IBuildLog
    {
        private readonly TextBox _log;
        private readonly ProgressBar _bar;

        public UiBuildLog(TextBox log, ProgressBar bar)
        {
            _log = log;
            _bar = bar;
        }

        public void Info(string message) => Write("INFO", message);
        public void Warn(string message) => Write("AVERT.", message);
        public void Error(string message) => Write("ERREUR", message);

        public void Progress(int current, int total)
        {
            if (_bar == null) return;
            int v = total > 0 ? (int)(1000L * current / total) : 0;
            _bar.Value = Math.Max(0, Math.Min(_bar.Maximum, v));
            Application.DoEvents();
        }

        private void Write(string level, string message)
        {
            if (_log == null) return;
            _log.AppendText("[" + level + "] " + message + Environment.NewLine);
            Application.DoEvents();
        }
    }
}
