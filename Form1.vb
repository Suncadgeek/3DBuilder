Imports System.Windows.Forms
Imports Excel = Microsoft.Office.Interop.Excel
Imports System.Text
Imports System.IO
Imports NXOpen
Imports NXOpen.UF
Public Class Form1

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Call Read_config()


    End Sub
    Sub Read_config()
        Dim configfilepath As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\Documents\3DBuilder_config.ini"

        If System.IO.File.Exists(configfilepath) Then 'le fichier existe on remplit le form
            Dim sr As New System.IO.StreamReader(configfilepath)
            Dim configr(6) As String


            TextBox1.Text = sr.ReadLine()
            'TextBox2.Text = sr.ReadLine()
            'TextBox3.Text = sr.ReadLine()
            ' TextBox4.Text = sr.ReadLine()
            'TextBox5.Text = sr.ReadLine()
            'If sr.ReadLine = "True" Then CheckBox1.Checked = True : Else CheckBox1.Checked = False
            'If sr.ReadLine = "True" Then CheckBox2.Checked = True : Else CheckBox2.Checked = False


            sr.Close()


        End If
    End Sub
    Sub Write_config()
        Dim configfilepath As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\Documents\3DBuilder_config.ini"
        Dim sw As New System.IO.StreamWriter(configfilepath) 'le fichier est ensuite sauvegardé

        sw.Flush()
        Dim configw(0) As String
        configw(0) = TextBox1.Text


        For i = 0 To 0
            sw.WriteLine(configw(i))
        Next
        sw.Close()
    End Sub


    Private Sub Form1_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Write_config()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'Dim outputdir As String = "C:\Users\pinty.000\Desktop\NX OUT"
        'Dim skeletonpath As String = "C:\Users\pinty.000\Desktop\NX OUT\SQUELETTE2.prt"
        'Dim magnetspath As String = "C:\Users\pinty.000\Desktop\NX OUT\MAGNETS"
        ProgressBar1.Visible = True
        ProgressBar1.Value = 0
        Outline()


        MsgBox("Opération terminée")
        Close()
    End Sub

    Private Sub FolderBrowserDialog1_HelpRequest(sender As Object, e As EventArgs) 

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OpenFileDialog1.ShowDialog()

    End Sub

    Private Sub OpenFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        TextBox1.Text = OpenFileDialog1.FileName
        'Write_config()
    End Sub
End Class

