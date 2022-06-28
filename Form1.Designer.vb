<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requise par le Concepteur Windows Form
    Private components As System.ComponentModel.IContainer

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.Button2 = New System.Windows.Forms.Button()
        Me.BindingSource1 = New System.Windows.Forms.BindingSource(Me.components)
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        CType(Me.BindingSource1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(295, 126)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(92, 29)
        Me.Button2.TabIndex = 9
        Me.Button2.Text = "Générer Cellule"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 18)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(270, 13)
        Me.Label1.TabIndex = 11
        Me.Label1.Text = "Fichier Excel vers la liste d'aimants et squelette dans TC"
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(11, 126)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(85, 29)
        Me.Button1.TabIndex = 12
        Me.Button1.Text = "Parcourir"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        Me.OpenFileDialog1.Filter = "All Excel files|*.xls;*.xlsx;*.xlsxm"
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(12, 42)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(375, 20)
        Me.TextBox1.TabIndex = 13
        Me.TextBox1.Text = "C:\Users\pinty.000\Desktop\NX OUT\Liste_Aimants.xlsx"
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Location = New System.Drawing.Point(101, 126)
        Me.ProgressBar1.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.ProgressBar1.Maximum = 1000
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(188, 29)
        Me.ProgressBar1.TabIndex = 15
        Me.ProgressBar1.Visible = False
        '
        'TextBox2
        '
        Me.TextBox2.Location = New System.Drawing.Point(12, 100)
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.Size = New System.Drawing.Size(375, 20)
        Me.TextBox2.TabIndex = 13
        Me.TextBox2.Text = "CAO000083492"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(12, 75)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(194, 13)
        Me.Label2.TabIndex = 11
        Me.Label2.Text = "Référence TC du squelette à assembler"
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(396, 173)
        Me.Controls.Add(Me.ProgressBar1)
        Me.Controls.Add(Me.TextBox2)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Button2)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "Form1"
        Me.RightToLeftLayout = True
        Me.Text = "3DBuilder"
        Me.TopMost = True
        CType(Me.BindingSource1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Button2 As Windows.Forms.Button
    Friend WithEvents BindingSource1 As Windows.Forms.BindingSource
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents Button1 As Windows.Forms.Button
    Friend WithEvents OpenFileDialog1 As Windows.Forms.OpenFileDialog
    Friend WithEvents TextBox1 As Windows.Forms.TextBox
    Friend WithEvents ProgressBar1 As Windows.Forms.ProgressBar
    Friend WithEvents TextBox2 As Windows.Forms.TextBox
    Friend WithEvents Label2 As Windows.Forms.Label
End Class
