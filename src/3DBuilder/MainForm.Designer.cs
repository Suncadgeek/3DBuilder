using System;
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

        private TextBox txtDict; private Button btnBrowseDict;
        private TextBox txtRing;
        private ComboBox cboMode;
        private TextBox txtNativeMagnets; private Button btnBrowseMagnets;
        private TextBox txtNativeRing; private Button btnBrowseRing;
        private CheckBox chkAllRing;
        private CheckedListBox lstCells;
        private CheckBox chkForce;
        private Button btnAnalyze; private Button btnGenerate; private Button btnCancel;
        private ComboBox cboLogLevel;
        private ProgressBar progressBar;
        private TextBox txtLog;

        // ---- petits constructeurs de contrôles (mise en page adaptative, DPI-safe) ----
        private static Label Lbl(string text)
            => new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 1) };

        private static TextBox Tb()
            => new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 2, 3, 6) };

        private static Button Browse(EventHandler onClick)
        {
            var b = new Button { Text = "Parcourir…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(6, 1, 3, 4), Padding = new Padding(6, 1, 6, 1) };
            b.Click += onClick;
            return b;
        }

        private static GroupBox Group(string title)
            => new GroupBox { Text = title, Dock = DockStyle.Fill, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(10, 4, 10, 8) };

        private static TableLayoutPanel Grid(int cols)
            => new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = cols, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // racine : une colonne, lignes empilées. PAS d'AutoSize : la racine remplit la fenêtre,
            // ce qui permet à la ligne « Journal » (en pourcentage) d'absorber l'espace restant.
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // =================== Source ===================
            var gbSource = Group("Source");
            var gs = Grid(2);
            gs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gs.Controls.Add(Lbl("Dictionnaire aimants (.xlsx — colonne A = réf TC, colonne B = code)"), 0, 0);
            gs.SetColumnSpan(gs.GetControlFromPosition(0, 0), 2);
            this.txtDict = Tb(); this.btnBrowseDict = Browse(this.OnBrowseDict);
            gs.Controls.Add(this.txtDict, 0, 1); gs.Controls.Add(this.btnBrowseDict, 1, 1);
            gs.Controls.Add(Lbl("Réf TC de l'anneau de stockage"), 0, 2);
            gs.SetColumnSpan(gs.GetControlFromPosition(0, 2), 2);
            this.txtRing = Tb();
            gs.Controls.Add(this.txtRing, 0, 3); gs.SetColumnSpan(this.txtRing, 2);
            gbSource.Controls.Add(gs);

            // =================== Mode ===================
            var gbMode = Group("Mode d'exécution");
            var gm = Grid(2);
            gm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var modeRow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false, Margin = new Padding(0), FlowDirection = FlowDirection.LeftToRight };
            this.cboMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170,
                Margin = new Padding(8, 3, 3, 3) };
            this.cboMode.Items.AddRange(new object[] { "Auto", "Natif (test)", "Managé (prod)" });
            this.cboMode.SelectedIndexChanged += this.OnModeChanged;
            modeRow.Controls.Add(new Label { Text = "Mode :", AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 7, 3, 3) });
            modeRow.Controls.Add(this.cboMode);
            gm.Controls.Add(modeRow, 0, 0); gm.SetColumnSpan(modeRow, 2);
            gm.Controls.Add(Lbl("Natif — dossier des aimants (.prt)"), 0, 1);
            gm.SetColumnSpan(gm.GetControlFromPosition(0, 1), 2);
            this.txtNativeMagnets = Tb(); this.btnBrowseMagnets = Browse(this.OnBrowseMagnets);
            gm.Controls.Add(this.txtNativeMagnets, 0, 2); gm.Controls.Add(this.btnBrowseMagnets, 1, 2);
            gm.Controls.Add(Lbl("Natif — anneau / cellules (.prt)"), 0, 3);
            gm.SetColumnSpan(gm.GetControlFromPosition(0, 3), 2);
            this.txtNativeRing = Tb(); this.btnBrowseRing = Browse(this.OnBrowseRing);
            gm.Controls.Add(this.txtNativeRing, 0, 4); gm.Controls.Add(this.btnBrowseRing, 1, 4);
            gbMode.Controls.Add(gm);

            // =================== Périmètre ===================
            var gbScope = Group("Périmètre");
            var gp = Grid(1);
            gp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            this.chkAllRing = new CheckBox { Text = "Tout l'anneau", AutoSize = true, Checked = true, Margin = new Padding(3, 4, 3, 2) };
            this.chkAllRing.CheckedChanged += this.OnAllRingChanged;
            this.lstCells = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Enabled = false,
                Height = 70, IntegralHeight = false, Margin = new Padding(3, 2, 3, 4) };
            this.chkForce = new CheckBox { Text = "Remplissage forcé (purge des aimants présents, puis repose)",
                AutoSize = true, Margin = new Padding(3, 4, 3, 2) };
            gp.Controls.Add(this.chkAllRing, 0, 0);
            gp.Controls.Add(Lbl("Cellules à remplir :"), 0, 1);
            gp.Controls.Add(this.lstCells, 0, 2);
            gp.Controls.Add(this.chkForce, 0, 3);
            gbScope.Controls.Add(gp);

            // =================== Actions ===================
            var actions = Grid(3);
            actions.Margin = new Padding(0, 0, 0, 10);
            for (int i = 0; i < 3; i++) actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            this.btnAnalyze = new Button { Text = "1 — Analyser", Dock = DockStyle.Fill, Height = 36, Margin = new Padding(0, 0, 6, 0) };
            this.btnAnalyze.Click += this.OnAnalyze;
            this.btnGenerate = new Button { Text = "2 — Générer", Dock = DockStyle.Fill, Height = 36, Enabled = false, Margin = new Padding(3, 0, 3, 0) };
            this.btnGenerate.Click += this.OnGenerate;
            this.btnCancel = new Button { Text = "Annuler", Dock = DockStyle.Fill, Height = 36, Enabled = false, Margin = new Padding(6, 0, 0, 0) };
            this.btnCancel.Click += this.OnCancel;
            actions.Controls.Add(this.btnAnalyze, 0, 0);
            actions.Controls.Add(this.btnGenerate, 1, 0);
            actions.Controls.Add(this.btnCancel, 2, 0);

            // =================== Journal ===================
            var gbJournal = Group("Journal");
            gbJournal.Margin = new Padding(0);
            gbJournal.AutoSize = false; // doit REMPLIR la ligne en pourcentage, pas se réduire au contenu
            var gj = Grid(2);
            gj.AutoSize = false;        // idem : la zone de log s'étire
            gj.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gj.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var logRow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false, Margin = new Padding(0), FlowDirection = FlowDirection.LeftToRight };
            this.cboLogLevel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new Padding(8, 3, 3, 3) };
            this.cboLogLevel.Items.AddRange(new object[] { "Info", "Avertissements", "Erreurs" });
            this.cboLogLevel.SelectedIndex = 0;
            this.cboLogLevel.SelectedIndexChanged += this.OnLogLevelChanged;
            logRow.Controls.Add(new Label { Text = "Niveau :", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 3) });
            logRow.Controls.Add(this.cboLogLevel);
            gj.Controls.Add(logRow, 0, 0); gj.SetColumnSpan(logRow, 2);
            this.progressBar = new ProgressBar { Dock = DockStyle.Fill, Height = 18, Maximum = 1000, Margin = new Padding(3, 4, 3, 4) };
            gj.Controls.Add(this.progressBar, 0, 1); gj.SetColumnSpan(this.progressBar, 2);
            this.txtLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, MinimumSize = new Size(0, 120),
                BackColor = Color.FromArgb(250, 250, 252), Margin = new Padding(3, 2, 3, 2) };
            gj.Controls.Add(this.txtLog, 0, 2); gj.SetColumnSpan(this.txtLog, 2);
            gbJournal.Controls.Add(gj);

            // racine : empile les blocs, le Journal absorbe l'espace restant
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(gbSource, 0, 0);
            root.Controls.Add(gbMode, 0, 1);
            root.Controls.Add(gbScope, 0, 2);
            root.Controls.Add(actions, 0, 3);
            root.Controls.Add(gbJournal, 0, 4);

            // =================== Form ===================
            this.AutoScaleMode = AutoScaleMode.Dpi; // mise à l'échelle uniforme selon le DPI de la session NX
            this.Font = new Font("Segoe UI", 9F);
            this.ClientSize = new Size(600, 780);
            this.MinimumSize = new Size(560, 700);
            this.Controls.Add(root);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "3DBuilder — assemblage des aimants";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
