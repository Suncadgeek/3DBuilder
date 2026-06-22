using System;
using System.Linq;
using System.Windows.Forms;
using ThreeDBuilder.Core.Config;
using ThreeDBuilder.Core.Model;
using ThreeDBuilder.Core.Preflight;
using ThreeDBuilder.Nx;

namespace ThreeDBuilder
{
    public partial class MainForm : Form
    {
        private readonly ConfigStore _store = new ConfigStore();
        private GenerationConfig _config;
        private GenerationService _service;
        private UiBuildLog _log;
        private bool _cancel;
        private bool _analyzed;

        public MainForm()
        {
            InitializeComponent();
            _log = new UiBuildLog(txtLog, progressBar);
            Load += OnLoad;
            FormClosing += OnClosingSave;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            _config = _store.Load();
            txtDict.Text = _config.DictionaryExcelPath;
            txtRing.Text = _config.StorageRingTcRef;
            txtNativeMagnets.Text = _config.NativeMagnetsFolder;
            txtNativeRing.Text = _config.NativeRingPath;
            cboMode.SelectedIndex = (int)_config.PdmMode; // Auto=0, Natif=1, Managé=2
            chkForce.Checked = _config.FillMode == FillMode.ForceRefill;
            UpdateNativeFieldsEnabled();
        }

        // --- Saisie config depuis l'UI ---
        private GenerationConfig ReadConfig()
        {
            _config.DictionaryExcelPath = txtDict.Text.Trim();
            _config.StorageRingTcRef = txtRing.Text.Trim();
            _config.NativeMagnetsFolder = txtNativeMagnets.Text.Trim();
            _config.NativeRingPath = txtNativeRing.Text.Trim();
            _config.PdmMode = (NxRunMode)cboMode.SelectedIndex;
            _config.FillMode = chkForce.Checked ? FillMode.ForceRefill : FillMode.Incremental;
            _config.SelectedCells = chkAllRing.Checked
                ? new System.Collections.Generic.List<string>()
                : lstCells.CheckedItems.Cast<object>().Select(o => o.ToString()).ToList();
            return _config;
        }

        // --- Phase 1 : Analyser ---
        private void OnAnalyze(object sender, EventArgs e)
        {
            try
            {
                SetBusy(true);
                progressBar.Value = 0;
                _log.Clear();
                if (_service == null) _service = new GenerationService(new NxContext(), _log);

                var cfg = ReadConfig();
                var result = _service.Analyze(cfg);

                lstCells.Items.Clear();
                foreach (var name in result.CellNames) lstCells.Items.Add(name, true);

                AppendReport(result.Report);
                _analyzed = true;
                btnGenerate.Enabled = !result.Report.HasBlockingErrors;
                if (result.Report.HasBlockingErrors)
                    _log.Error("Erreurs bloquantes : corrige le dictionnaire avant de générer.");
                else if (result.Report.HasWarnings)
                    _log.Warn("Des avertissements existent — la génération les ignorera (override implicite des aimants concernés).");
            }
            catch (Exception ex)
            {
                _log?.Error("Analyse impossible : " + ex.Message);
                MessageBox.Show(ex.Message, "Analyse", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }

        // --- Phase 2 : Générer ---
        private void OnGenerate(object sender, EventArgs e)
        {
            if (!_analyzed) { MessageBox.Show("Lance d'abord l'analyse.", "Générer"); return; }
            var cfg = ReadConfig();

            if (cfg.FillMode == FillMode.ForceRefill)
            {
                var ok = MessageBox.Show(
                    "Remplissage FORCÉ : les composants aimants des cellules sélectionnées seront RETIRÉS "
                    + "(le squelette est conservé) avant d'être reposés. Continuer ?",
                    "Confirmation purge", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (ok != DialogResult.Yes) return;
            }

            try
            {
                SetBusy(true);
                _cancel = false;
                btnCancel.Enabled = true;
                var summary = _service.Run(cfg, cfg.SelectedCells, cfg.FillMode, () => { Application.DoEvents(); return _cancel; });

                if (summary.Failures.Count > 0)
                    foreach (var f in summary.Failures) _log.Warn(f);
                var msg = $"{summary.Added} aimant(s) posé(s), {summary.Failed} échec(s), {summary.SkippedMissing} sauté(s)"
                          + (summary.Cancelled ? " — ANNULÉ" : "") + ".";
                _log.Info(msg);
                MessageBox.Show(msg, "Terminé", MessageBoxButtons.OK,
                    summary.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.Error("Génération interrompue : " + ex.Message);
                MessageBox.Show(ex.Message, "Générer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btnCancel.Enabled = false; SetBusy(false); }
        }

        private void OnCancel(object sender, EventArgs e) { _cancel = true; }

        // --- Browse / mode ---
        private void OnBrowseDict(object sender, EventArgs e)
        {
            using (var d = new OpenFileDialog { Filter = "Excel|*.xlsx;*.xls" })
                if (d.ShowDialog() == DialogResult.OK) txtDict.Text = d.FileName;
        }
        private void OnBrowseMagnets(object sender, EventArgs e)
        {
            using (var d = new FolderBrowserDialog())
                if (d.ShowDialog() == DialogResult.OK) txtNativeMagnets.Text = d.SelectedPath;
        }
        private void OnBrowseRing(object sender, EventArgs e)
        {
            using (var d = new OpenFileDialog { Filter = "Pièce NX|*.prt" })
                if (d.ShowDialog() == DialogResult.OK) txtNativeRing.Text = d.FileName;
        }
        private void OnModeChanged(object sender, EventArgs e) => UpdateNativeFieldsEnabled();
        private void OnAllRingChanged(object sender, EventArgs e) => lstCells.Enabled = !chkAllRing.Checked;
        private void OnLogLevelChanged(object sender, EventArgs e)
        {
            if (_log != null) _log.MinLevel = cboLogLevel.SelectedIndex;
        }

        private void UpdateNativeFieldsEnabled()
        {
            bool native = cboMode.SelectedIndex == (int)NxRunMode.Native;
            txtNativeMagnets.Enabled = btnBrowseMagnets.Enabled = native;
            txtNativeRing.Enabled = btnBrowseRing.Enabled = native;
            txtRing.Enabled = cboMode.SelectedIndex != (int)NxRunMode.Native;
        }

        private void AppendReport(PreflightReport report)
        {
            _log.Info("──── Rapport preflight ────");
            foreach (var f in report.Findings)
            {
                switch (f.Severity)
                {
                    case PreflightSeverity.Error: _log.Error(f.ToString()); break;
                    case PreflightSeverity.Warning: _log.Warn(f.ToString()); break;
                    default: _log.Info(f.ToString()); break;
                }
            }
            _log.Info($"→ {report.TotalToAdd} aimant(s) à poser sur le scope.");
        }

        private void SetBusy(bool busy)
        {
            btnAnalyze.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void OnClosingSave(object sender, FormClosingEventArgs e)
        {
            try { _store.Save(ReadConfig()); } catch { /* best effort */ }
        }
    }
}
