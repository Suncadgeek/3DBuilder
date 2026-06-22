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

        private const int ContentW = 548; // largeur fixe du contenu (déterministe, DPI-safe)

        private TextBox txtDict; private Button btnBrowseDict;
        private TextBox txtRing;
        private ComboBox cboMode;
        private TextBox txtNativeMagnets; private Button btnBrowseMagnets;
        private TextBox txtNativeRing; private Button btnBrowseRing;
        private CheckBox chkAllRing;
        private CheckedListBox lstCells;
        private Button btnCheckAll; private Button btnUncheckAll;
        private CheckBox chkForce;
        private Button btnAnalyze; private Button btnGenerate; private Button btnCancel;
        private Label lblStatus;
        private ComboBox cboLogLevel;
        private ProgressBar progressBar;
        private TextBox txtLog;

        // ---- petits constructeurs (layout vertical AutoSize → aucun chevauchement quel que soit le DPI) ----
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

        private static FlowLayoutPanel HRow()
            => new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false, Margin = new Padding(0), FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // racine : 1 colonne de LARGEUR FIXE (les Dock=Fill internes ont alors une vraie largeur),
            // toutes les lignes en AutoSize. PAS de Dock : root se dimensionne à son contenu (colonne
            // 548 + marges) et la fenêtre AutoSize l'enveloppe → largeur correcte, rien ne se replie.
            var root = new TableLayoutPanel { ColumnCount = 1, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Location = new Point(0, 0), Padding = new Padding(10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ContentW));

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
            var modeRow = HRow();
            this.cboMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new Padding(8, 3, 3, 3) };
            this.cboMode.Items.AddRange(new object[] { "Auto", "Natif (test)", "Managé (prod)" });
            this.cboMode.SelectedIndexChanged += this.OnModeChanged;
            modeRow.Controls.Add(new Label { Text = "Mode :", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 3) });
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
            var cellHeader = HRow();
            cellHeader.Controls.Add(new Label { Text = "Cellules à remplir :", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 12, 0) });
            this.btnCheckAll = new Button { Text = "Tout cocher", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Enabled = false, Margin = new Padding(0, 3, 4, 0), Padding = new Padding(4, 1, 4, 1) };
            this.btnCheckAll.Click += this.OnCheckAll;
            this.btnUncheckAll = new Button { Text = "Tout décocher", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Enabled = false, Margin = new Padding(0, 3, 0, 0), Padding = new Padding(4, 1, 4, 1) };
            this.btnUncheckAll.Click += this.OnUncheckAll;
            cellHeader.Controls.Add(this.btnCheckAll);
            cellHeader.Controls.Add(this.btnUncheckAll);
            this.lstCells = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Enabled = false,
                Height = 130, IntegralHeight = false, Margin = new Padding(3, 2, 3, 4) };
            this.chkForce = new CheckBox { Text = "Remplissage forcé (purge des aimants présents, puis repose)",
                AutoSize = true, Margin = new Padding(3, 4, 3, 2) };
            gp.Controls.Add(this.chkAllRing, 0, 0);
            gp.Controls.Add(cellHeader, 0, 1);
            gp.Controls.Add(this.lstCells, 0, 2);
            gp.Controls.Add(this.chkForce, 0, 3);
            gbScope.Controls.Add(gp);

            // =================== Actions ===================
            var actions = Grid(3);
            actions.Margin = new Padding(0, 0, 0, 6);
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

            // =================== État ===================
            this.lblStatus = new Label { Text = "Prêt.", AutoSize = true, Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(40, 90, 40),
                Margin = new Padding(3, 2, 3, 8) };

            // =================== Journal ===================
            var gbJournal = Group("Journal");
            gbJournal.Margin = new Padding(0);
            var gj = Grid(2);
            gj.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gj.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); // hauteur de log FIXE → toujours visible
            var logRow = HRow();
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
                ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(250, 250, 252), Margin = new Padding(3, 2, 3, 2) };
            gj.Controls.Add(this.txtLog, 0, 2); gj.SetColumnSpan(this.txtLog, 2);
            gbJournal.Controls.Add(gj);

            // racine : empile les blocs (toutes lignes AutoSize → fenêtre dimensionnée au contenu)
            for (int i = 0; i < 6; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(gbSource, 0, 0);
            root.Controls.Add(gbMode, 0, 1);
            root.Controls.Add(gbScope, 0, 2);
            root.Controls.Add(actions, 0, 3);
            root.Controls.Add(this.lblStatus, 0, 4);
            root.Controls.Add(gbJournal, 0, 5);

            // Conteneur défilant : si le contenu (à fort DPI) dépasse la fenêtre, un ascenseur apparaît
            // → tout reste atteignable, jamais de chevauchement ni de repli. root garde sa largeur fixe.
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scroll.Controls.Add(root);

            // =================== Form ===================
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9F);
            this.ClientSize = new Size(596, 808);
            this.MinimumSize = new Size(590, 460);
            this.Controls.Add(scroll);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "3DBuilder — assemblage des aimants";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
