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

            // Dictionnaire
            this.lblDict.AutoSize = true; this.lblDict.Location = new Point(12, 12);
            this.lblDict.Text = "Dictionnaire aimants (.xlsx) — A=réf TC, B=code";
            this.txtDict.Location = new Point(12, 30); this.txtDict.Size = new Size(360, 20);
            this.btnBrowseDict.Location = new Point(378, 28); this.btnBrowseDict.Size = new Size(90, 24);
            this.btnBrowseDict.Text = "Parcourir…"; this.btnBrowseDict.Click += this.OnBrowseDict;

            // Réf TC anneau
            this.lblRing.AutoSize = true; this.lblRing.Location = new Point(12, 58);
            this.lblRing.Text = "Réf TC de l'anneau de stockage";
            this.txtRing.Location = new Point(12, 76); this.txtRing.Size = new Size(200, 20);

            // Mode
            this.lblMode.AutoSize = true; this.lblMode.Location = new Point(230, 58); this.lblMode.Text = "Mode";
            this.cboMode.Location = new Point(230, 76); this.cboMode.Size = new Size(120, 21);
            this.cboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboMode.Items.AddRange(new object[] { "Auto", "Natif", "Managé" });
            this.cboMode.SelectedIndexChanged += this.OnModeChanged;

            // Natif : dossier aimants
            this.lblNativeMagnets.AutoSize = true; this.lblNativeMagnets.Location = new Point(12, 104);
            this.lblNativeMagnets.Text = "Natif : dossier des aimants (.prt)";
            this.txtNativeMagnets.Location = new Point(12, 122); this.txtNativeMagnets.Size = new Size(360, 20);
            this.btnBrowseMagnets.Location = new Point(378, 120); this.btnBrowseMagnets.Size = new Size(90, 24);
            this.btnBrowseMagnets.Text = "Parcourir…"; this.btnBrowseMagnets.Click += this.OnBrowseMagnets;

            // Natif : chemin anneau
            this.lblNativeRing.AutoSize = true; this.lblNativeRing.Location = new Point(12, 148);
            this.lblNativeRing.Text = "Natif : anneau / cellules (.prt)";
            this.txtNativeRing.Location = new Point(12, 166); this.txtNativeRing.Size = new Size(360, 20);
            this.btnBrowseRing.Location = new Point(378, 164); this.btnBrowseRing.Size = new Size(90, 24);
            this.btnBrowseRing.Text = "Parcourir…"; this.btnBrowseRing.Click += this.OnBrowseRing;

            // Scope
            this.chkAllRing.AutoSize = true; this.chkAllRing.Location = new Point(12, 196);
            this.chkAllRing.Text = "Tout l'anneau"; this.chkAllRing.Checked = true;
            this.chkAllRing.CheckedChanged += this.OnAllRingChanged;

            this.lblCells.AutoSize = true; this.lblCells.Location = new Point(12, 216); this.lblCells.Text = "Cellules à remplir :";
            this.lstCells.Location = new Point(12, 234); this.lstCells.Size = new Size(220, 130);
            this.lstCells.CheckOnClick = true; this.lstCells.Enabled = false;

            this.chkForce.AutoSize = true; this.chkForce.Location = new Point(250, 234);
            this.chkForce.Text = "Remplissage forcé (purge puis repose)";

            // Boutons
            this.btnAnalyze.Location = new Point(250, 264); this.btnAnalyze.Size = new Size(100, 28);
            this.btnAnalyze.Text = "Analyser"; this.btnAnalyze.Click += this.OnAnalyze;
            this.btnGenerate.Location = new Point(356, 264); this.btnGenerate.Size = new Size(100, 28);
            this.btnGenerate.Text = "Générer"; this.btnGenerate.Enabled = false; this.btnGenerate.Click += this.OnGenerate;
            this.btnCancel.Location = new Point(356, 296); this.btnCancel.Size = new Size(100, 24);
            this.btnCancel.Text = "Annuler"; this.btnCancel.Enabled = false; this.btnCancel.Click += this.OnCancel;

            // Filtre niveau de journal
            this.lblLogLevel.AutoSize = true; this.lblLogLevel.Location = new Point(250, 300); this.lblLogLevel.Text = "Niveau journal :";
            this.cboLogLevel.Location = new Point(250, 318); this.cboLogLevel.Size = new Size(106, 21);
            this.cboLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboLogLevel.Items.AddRange(new object[] { "Info", "Avertissements", "Erreurs" });
            this.cboLogLevel.SelectedIndex = 0;
            this.cboLogLevel.SelectedIndexChanged += this.OnLogLevelChanged;

            // Progression + journal
            this.progressBar.Location = new Point(12, 372); this.progressBar.Size = new Size(456, 18);
            this.progressBar.Maximum = 1000;
            this.txtLog.Location = new Point(12, 396); this.txtLog.Size = new Size(456, 150);
            this.txtLog.Multiline = true; this.txtLog.ReadOnly = true; this.txtLog.ScrollBars = ScrollBars.Vertical;

            // Form
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(480, 558);
            this.Controls.Add(this.lblDict); this.Controls.Add(this.txtDict); this.Controls.Add(this.btnBrowseDict);
            this.Controls.Add(this.lblRing); this.Controls.Add(this.txtRing);
            this.Controls.Add(this.lblMode); this.Controls.Add(this.cboMode);
            this.Controls.Add(this.lblNativeMagnets); this.Controls.Add(this.txtNativeMagnets); this.Controls.Add(this.btnBrowseMagnets);
            this.Controls.Add(this.lblNativeRing); this.Controls.Add(this.txtNativeRing); this.Controls.Add(this.btnBrowseRing);
            this.Controls.Add(this.chkAllRing);
            this.Controls.Add(this.lblCells); this.Controls.Add(this.lstCells);
            this.Controls.Add(this.chkForce);
            this.Controls.Add(this.btnAnalyze); this.Controls.Add(this.btnGenerate); this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblLogLevel); this.Controls.Add(this.cboLogLevel);
            this.Controls.Add(this.progressBar); this.Controls.Add(this.txtLog);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.Text = "3DBuilder";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
