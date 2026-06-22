using System.Windows.Forms;
using System.Drawing;

namespace ThreeDBuilder
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private GroupBox gbSource, gbMode, gbScope, gbJournal;
        private Label lblDict; private TextBox txtDict; private Button btnBrowseDict;
        private Label lblRing; private TextBox txtRing;
        private Label lblMode; private ComboBox cboMode;
        private Label lblNativeMagnets; private TextBox txtNativeMagnets; private Button btnBrowseMagnets;
        private Label lblNativeRing; private TextBox txtNativeRing; private Button btnBrowseRing;
        private CheckBox chkAllRing;
        private Label lblCells; private CheckedListBox lstCells;
        private CheckBox chkForce;
        private Button btnAnalyze; private Button btnGenerate; private Button btnCancel;
        private Label lblLogLevel; private ComboBox cboLogLevel;
        private ProgressBar progressBar;
        private TextBox txtLog;

        private void InitializeComponent()
        {
            this.gbSource = new GroupBox(); this.gbMode = new GroupBox();
            this.gbScope = new GroupBox(); this.gbJournal = new GroupBox();
            this.lblDict = new Label(); this.txtDict = new TextBox(); this.btnBrowseDict = new Button();
            this.lblRing = new Label(); this.txtRing = new TextBox();
            this.lblMode = new Label(); this.cboMode = new ComboBox();
            this.lblNativeMagnets = new Label(); this.txtNativeMagnets = new TextBox(); this.btnBrowseMagnets = new Button();
            this.lblNativeRing = new Label(); this.txtNativeRing = new TextBox(); this.btnBrowseRing = new Button();
            this.chkAllRing = new CheckBox();
            this.lblCells = new Label(); this.lstCells = new CheckedListBox();
            this.chkForce = new CheckBox();
            this.btnAnalyze = new Button(); this.btnGenerate = new Button(); this.btnCancel = new Button();
            this.lblLogLevel = new Label(); this.cboLogLevel = new ComboBox();
            this.progressBar = new ProgressBar();
            this.txtLog = new TextBox();
            this.SuspendLayout();

            // ----- GroupBox Source -----
            this.gbSource.Text = "Source"; this.gbSource.Location = new Point(12, 8); this.gbSource.Size = new Size(480, 124);
            this.lblDict.AutoSize = true; this.lblDict.Location = new Point(12, 22);
            this.lblDict.Text = "Dictionnaire aimants (.xlsx — A = réf TC, B = code)";
            this.txtDict.Location = new Point(14, 42); this.txtDict.Size = new Size(354, 23);
            this.btnBrowseDict.Location = new Point(376, 41); this.btnBrowseDict.Size = new Size(92, 25);
            this.btnBrowseDict.Text = "Parcourir…"; this.btnBrowseDict.Click += this.OnBrowseDict;
            this.lblRing.AutoSize = true; this.lblRing.Location = new Point(12, 74);
            this.lblRing.Text = "Réf TC de l'anneau de stockage";
            this.txtRing.Location = new Point(14, 94); this.txtRing.Size = new Size(220, 23);
            this.gbSource.Controls.Add(this.lblDict); this.gbSource.Controls.Add(this.txtDict);
            this.gbSource.Controls.Add(this.btnBrowseDict); this.gbSource.Controls.Add(this.lblRing);
            this.gbSource.Controls.Add(this.txtRing);

            // ----- GroupBox Mode -----
            this.gbMode.Text = "Mode d'exécution"; this.gbMode.Location = new Point(12, 140); this.gbMode.Size = new Size(480, 152);
            this.lblMode.AutoSize = true; this.lblMode.Location = new Point(12, 25); this.lblMode.Text = "Mode :";
            this.cboMode.Location = new Point(120, 22); this.cboMode.Size = new Size(150, 23);
            this.cboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboMode.Items.AddRange(new object[] { "Auto", "Natif (test)", "Managé (prod)" });
            this.cboMode.SelectedIndexChanged += this.OnModeChanged;
            this.lblNativeMagnets.AutoSize = true; this.lblNativeMagnets.Location = new Point(12, 54);
            this.lblNativeMagnets.Text = "Natif — dossier des aimants (.prt)";
            this.txtNativeMagnets.Location = new Point(14, 74); this.txtNativeMagnets.Size = new Size(354, 23);
            this.btnBrowseMagnets.Location = new Point(376, 73); this.btnBrowseMagnets.Size = new Size(92, 25);
            this.btnBrowseMagnets.Text = "Parcourir…"; this.btnBrowseMagnets.Click += this.OnBrowseMagnets;
            this.lblNativeRing.AutoSize = true; this.lblNativeRing.Location = new Point(12, 102);
            this.lblNativeRing.Text = "Natif — anneau / cellules (.prt)";
            this.txtNativeRing.Location = new Point(14, 122); this.txtNativeRing.Size = new Size(354, 23);
            this.btnBrowseRing.Location = new Point(376, 121); this.btnBrowseRing.Size = new Size(92, 25);
            this.btnBrowseRing.Text = "Parcourir…"; this.btnBrowseRing.Click += this.OnBrowseRing;
            this.gbMode.Controls.Add(this.lblMode); this.gbMode.Controls.Add(this.cboMode);
            this.gbMode.Controls.Add(this.lblNativeMagnets); this.gbMode.Controls.Add(this.txtNativeMagnets);
            this.gbMode.Controls.Add(this.btnBrowseMagnets); this.gbMode.Controls.Add(this.lblNativeRing);
            this.gbMode.Controls.Add(this.txtNativeRing); this.gbMode.Controls.Add(this.btnBrowseRing);

            // ----- GroupBox Périmètre -----
            this.gbScope.Text = "Périmètre"; this.gbScope.Location = new Point(12, 300); this.gbScope.Size = new Size(480, 150);
            this.chkAllRing.AutoSize = true; this.chkAllRing.Location = new Point(12, 24);
            this.chkAllRing.Text = "Tout l'anneau"; this.chkAllRing.Checked = true;
            this.chkAllRing.CheckedChanged += this.OnAllRingChanged;
            this.lblCells.AutoSize = true; this.lblCells.Location = new Point(12, 48); this.lblCells.Text = "Cellules à remplir :";
            this.lstCells.Location = new Point(14, 68); this.lstCells.Size = new Size(220, 70);
            this.lstCells.CheckOnClick = true; this.lstCells.Enabled = false;
            this.chkForce.AutoSize = true; this.chkForce.Location = new Point(248, 68);
            this.chkForce.Text = "Remplissage forcé (purge puis repose)";
            this.gbScope.Controls.Add(this.chkAllRing); this.gbScope.Controls.Add(this.lblCells);
            this.gbScope.Controls.Add(this.lstCells); this.gbScope.Controls.Add(this.chkForce);

            // ----- Actions -----
            this.btnAnalyze.Location = new Point(12, 460); this.btnAnalyze.Size = new Size(150, 34);
            this.btnAnalyze.Text = "1 — Analyser"; this.btnAnalyze.Click += this.OnAnalyze;
            this.btnGenerate.Location = new Point(174, 460); this.btnGenerate.Size = new Size(150, 34);
            this.btnGenerate.Text = "2 — Générer"; this.btnGenerate.Enabled = false; this.btnGenerate.Click += this.OnGenerate;
            this.btnCancel.Location = new Point(336, 460); this.btnCancel.Size = new Size(156, 34);
            this.btnCancel.Text = "Annuler"; this.btnCancel.Enabled = false; this.btnCancel.Click += this.OnCancel;

            // ----- GroupBox Journal -----
            this.gbJournal.Text = "Journal"; this.gbJournal.Location = new Point(12, 504); this.gbJournal.Size = new Size(480, 168);
            this.lblLogLevel.AutoSize = true; this.lblLogLevel.Location = new Point(12, 25); this.lblLogLevel.Text = "Niveau :";
            this.cboLogLevel.Location = new Point(120, 22); this.cboLogLevel.Size = new Size(150, 23);
            this.cboLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboLogLevel.Items.AddRange(new object[] { "Info", "Avertissements", "Erreurs" });
            this.cboLogLevel.SelectedIndex = 0;
            this.cboLogLevel.SelectedIndexChanged += this.OnLogLevelChanged;
            this.progressBar.Location = new Point(14, 52); this.progressBar.Size = new Size(452, 18); this.progressBar.Maximum = 1000;
            this.txtLog.Location = new Point(14, 78); this.txtLog.Size = new Size(452, 80);
            this.txtLog.Multiline = true; this.txtLog.ReadOnly = true; this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.BackColor = Color.FromArgb(250, 250, 252);
            this.gbJournal.Controls.Add(this.lblLogLevel); this.gbJournal.Controls.Add(this.cboLogLevel);
            this.gbJournal.Controls.Add(this.progressBar); this.gbJournal.Controls.Add(this.txtLog);

            // ----- Form -----
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9F);
            this.ClientSize = new Size(504, 684);
            this.Controls.Add(this.gbSource); this.Controls.Add(this.gbMode); this.Controls.Add(this.gbScope);
            this.Controls.Add(this.btnAnalyze); this.Controls.Add(this.btnGenerate); this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.gbJournal);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "3DBuilder — assemblage des aimants";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
