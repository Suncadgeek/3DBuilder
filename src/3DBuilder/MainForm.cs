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
            LoadIcon();
            _log = new UiBuildLog(txtLog, progressBar);
            Load += OnLoad;
            FormClosing += OnClosingSave;
        }

        private void LoadIcon()
        {
            try
            {
                using (var s = GetType().Assembly.GetManifestResourceStream("ThreeDBuilder.Resources.ico.ico"))
                    if (s != null) Icon = new System.Drawing.Icon(s);
            }
            catch { /* icône non bloquante */ }
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
                SetStatus("Analyse en cours…", System.Drawing.Color.FromArgb(150, 90, 0));
                if (_service == null) _service = new GenerationService(new NxContext(), _log);

                var cfg = ReadConfig();
                var result = _service.Analyze(cfg);

                // Conserve la sélection en cas de réanalyse (ex. après mise à jour du dico) :
                // au 1er passage tout est coché ; ensuite on restaure l'état coché par nom.
                var prevChecked = new System.Collections.Generic.HashSet<string>(
                    lstCells.CheckedItems.Cast<object>().Select(o => o.ToString()), StringComparer.Ordinal);
                bool hadItems = lstCells.Items.Count > 0;
                lstCells.Items.Clear();
                foreach (var name in result.CellNames)
                    lstCells.Items.Add(name, !hadItems || prevChecked.Contains(name));

                AppendReport(result.Report);
                _analyzed = true;
                btnGenerate.Enabled = !result.Report.HasBlockingErrors;
                progressBar.Value = progressBar.Maximum; // analyse terminée → barre pleine
                if (result.Report.HasBlockingErrors)
                {
                    _log.Error("Erreurs bloquantes : corrige le dictionnaire avant de générer.");
                    SetStatus("Analyse terminée — erreurs bloquantes (génération impossible).", System.Drawing.Color.FromArgb(170, 40, 40));
                }
                else
                {
                    if (result.Report.HasWarnings)
                        _log.Warn("Des avertissements existent — la génération les ignorera (override implicite des aimants concernés).");
                    SetStatus($"Analyse terminée — {result.Report.TotalToAdd} aimant(s) à poser. Prêt à générer.",
                        System.Drawing.Color.FromArgb(40, 110, 40));
                }
            }
            catch (Exception ex)
            {
                _log?.Error("Analyse impossible : " + ex.Message);
                SetStatus("Analyse échouée : " + ex.Message, System.Drawing.Color.FromArgb(170, 40, 40));
                MessageBox.Show(this,ex.Message, "Analyse", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }

        // --- Phase 2 : Générer ---
        private void OnGenerate(object sender, EventArgs e)
        {
            if (!_analyzed) { MessageBox.Show(this,"Lance d'abord l'analyse.", "Générer"); return; }
            var cfg = ReadConfig();

            if (cfg.FillMode == FillMode.ForceRefill)
            {
                var ok = MessageBox.Show(this,
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
                SetStatus("Génération en cours…", System.Drawing.Color.FromArgb(150, 90, 0));
                var summary = _service.Run(cfg, cfg.SelectedCells, cfg.FillMode, () => { Application.DoEvents(); return _cancel; });

                if (summary.Failures.Count > 0)
                    foreach (var f in summary.Failures) _log.Warn(f);
                var msg = $"{summary.Added} aimant(s) posé(s), {summary.Failed} échec(s), {summary.SkippedMissing} sauté(s)"
                          + (summary.Cancelled ? " — ANNULÉ" : "") + ".";
                _log.Info(msg);
                var color = summary.Cancelled ? System.Drawing.Color.FromArgb(150, 90, 0)
                          : summary.Failed > 0 ? System.Drawing.Color.FromArgb(170, 40, 40)
                          : System.Drawing.Color.FromArgb(40, 110, 40);
                SetStatus((summary.Cancelled ? "Génération annulée — " : "Génération terminée — ") + msg, color);
                MessageBox.Show(this,msg, "Terminé", MessageBoxButtons.OK,
                    summary.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _log.Error("Génération interrompue : " + ex.Message);
                SetStatus("Génération interrompue : " + ex.Message, System.Drawing.Color.FromArgb(170, 40, 40));
                MessageBox.Show(this,ex.Message, "Générer", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        private void OnAllRingChanged(object sender, EventArgs e)
        {
            bool pick = !chkAllRing.Checked;
            lstCells.Enabled = pick;
            btnCheckAll.Enabled = pick;
            btnUncheckAll.Enabled = pick;
        }
        private void OnCheckAll(object sender, EventArgs e) => SetAllCells(true);
        private void OnUncheckAll(object sender, EventArgs e) => SetAllCells(false);
        private void SetAllCells(bool value)
        {
            for (int i = 0; i < lstCells.Items.Count; i++) lstCells.SetItemChecked(i, value);
        }
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
                    case PreflightSeverity.Error: _log.Error(f.Message); break;
                    case PreflightSeverity.Warning: _log.Warn(f.Message); break;
                    default: _log.Info(f.Message); break;
                }
            }
            _log.Info($"→ {report.TotalToAdd} aimant(s) à poser sur le scope.");
        }

        private void SetBusy(bool busy)
        {
            btnAnalyze.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void SetStatus(string text, System.Drawing.Color color)
        {
            lblStatus.ForeColor = color;
            lblStatus.Text = text;
            Application.DoEvents();
        }

        private void OnClosingSave(object sender, FormClosingEventArgs e)
        {
            try { _store.Save(ReadConfig()); } catch { /* best effort */ }
        }
    }
}
