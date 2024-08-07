'NXJournaling.com
'create associative line example journal
'creates line from (1,2,0) to (4,5,0)
'tested on NX 7.5 and 8

Option Infer On
Option Strict Off
Imports System.Threading
Imports System
Imports NXOpen
Imports NXOpen.UF
Imports NXOpen.Assemblies
Imports Excel = Microsoft.Office.Interop.Excel


Module Module1


    Public theSession As Session = Session.GetSession()
    Public ufs As UFSession = UFSession.GetUFSession()
    Public workPart As Part = theSession.Parts.Work
    Public lw As ListingWindow = theSession.ListingWindow
    Public displayPart As NXOpen.Part = theSession.Parts.Display
    Public referenceSet1 As NXOpen.ReferenceSet
    Public storagering_part As Part


    Public Sub Main()

        Form1.Show()

    End Sub

    Public Sub Outline()
        Dim ListOfParts() As NXOpen.BasePart = theSession.Parts.ToArray()
        If ListOfParts.Length <> 0 Then
            MsgBox("Veuillez fermer toute les pièces avant d'exécuter le programme")
            Exit Sub
        End If

        Dim partLoadStatus1 As NXOpen.PartLoadStatus = Nothing
        Dim storagering As String
        ' Dim storagering_part As NXOpen.BasePart = Nothing
        theSession.Parts.LoadOptions.UsePartialLoading = False
        storagering = "@DB/" & Form1.TextBox2.Text
        storagering_part = theSession.Parts.OpenActiveDisplay(storagering, NXOpen.DisplayPartOption.AllowAdditional, partLoadStatus1)
        partLoadStatus1.Dispose()
        ' opening all necessary parts
        Dim CAOcomp(200, 2)
        CAOcomp = Open_Parts(storagering_part)

        ' creating master assembly
        'Dim Skelcomp As NXObject = Create_Assembly()

        ' importing magnets as component in master assembly



        Dim pgBarInc As Integer = CInt(850 / storagering_part.ComponentAssembly.RootComponent.GetChildren.Length - 1)

        For j = 0 To storagering_part.ComponentAssembly.RootComponent.GetChildren.Length - 1

            For k = 0 To 1
                'MsgBox(storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).Name)
                'MsgBox(storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren(0).Name)
                'theSession.Parts.SetWorkComponent(storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren(1), NXOpen.PartCollection.RefsetOption.Entire, NXOpen.PartCollection.WorkComponentOption.Visible, partLoadStatus1)
                'workPart = theSession.Parts.Work
                'Dim inte As Integer = storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren.Length
                'MsgBox(inte)
                If storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren.Length > 1 Then
                    Dim SkelComp As Component = storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren(1).GetChildren(0) 'defining te skeleton part
                    Dim AssemblyPart As Part = storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren(1).Prototype

                    Import_Magnets(CAOcomp, SkelComp, AssemblyPart)
                    If (Form1.ProgressBar1.Value + pgBarInc < 1000) Then Form1.ProgressBar1.Value += pgBarInc
                End If
            Next k
        Next j


        Dim markId1 As NXOpen.Session.UndoMarkId = Nothing
        markId1 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Make Work Part")

        Dim nullNXOpen_Assemblies_Component As NXOpen.Assemblies.Component = Nothing


        theSession.Parts.SetWorkComponent(nullNXOpen_Assemblies_Component, NXOpen.PartCollection.RefsetOption.Entire, NXOpen.PartCollection.WorkComponentOption.Visible, partLoadStatus1)

        workPart = theSession.Parts.Work ' CAO000084478/AA-ANNEAU DE STOCKAGE
        partLoadStatus1.Dispose()
        theSession.SetUndoMarkName(markId1, "Make Work Part")












        Form1.ProgressBar1.Value = 1000
        rename_instances()

    End Sub

    Function Open_Parts(storagering_part As Part)


        Form1.ProgressBar1.Value = 1
        Dim xlApp As Excel.Application
        Dim xlWorkBook As Excel.Workbook
        Dim xlWorkSheet As Excel.Worksheet
        Dim path_to_excel_file As String = Form1.TextBox1.Text

        Try
            xlApp = New Excel.Application
            Form1.ProgressBar1.Value = 20
            xlWorkBook = xlApp.Workbooks.Open(path_to_excel_file)
            Form1.ProgressBar1.Value = 40
            xlWorkSheet = xlWorkBook.Worksheets(1)
            Form1.ProgressBar1.Value = 60
        Catch
            MsgBox("Chemin vers tableur non valide, abandon")




            End
        End Try
        Dim CAOitem(200, 2) As String
        Dim CAOcomp(200, 2) As String
        Dim CAOdic As New Dictionary(Of String, Integer)

        Dim i As Integer = 0
        While xlWorkSheet.Cells(i + 1, 1).value <> ""
            CAOitem(i, 0) = xlWorkSheet.Cells(i + 1, 1).value.ToString
            CAOitem(i, 1) = xlWorkSheet.Cells(i + 1, 2).value.ToString


            CAOitem(i, 2) = xlWorkSheet.Cells(i + 1, 3).value.ToString

            i += 1
        End While
        Dim nbMagnets As Integer = i



        Form1.ProgressBar1.Value = 70


        theSession.Parts.LoadOptions.UsePartialLoading = False
        'loading main storage ring assembly

        workPart = theSession.Parts.Work
        displayPart = theSession.Parts.Display
        Dim magnetname As String

        Form1.ProgressBar1.Value = 100
        'loading magnets

        ' Dim files() As String = IO.Directory.GetFiles(magnetspath)

        'For Each child As Component In storagering_part.ComponentAssembly.RootComponent.GetChildren
        '    MsgBox(child.Name & " and owning part is " & child.OwningPart.Name)
        '    For Each child2 As Component In child.GetChildren
        '        MsgBox(child2.Name & " and owning part is " & child2.OwningPart.Name)
        '    Next
        'Next
        'End
        For j = 0 To storagering_part.ComponentAssembly.RootComponent.GetChildren.Length - 1
            For k = 0 To 1
                'MsgBox("@DB/" & storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).DisplayName)
                'For Each Object as part in thesession.parts
                ' MsgBox(Object.)
                'Next
                'Dim storagecomp As Component = storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k)
                'Dim inte As Integer = storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren.Length
                'MsgBox(inte)
                If storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren.Length > 1 Then
                    Dim SkelPart As Part = storagering_part.ComponentAssembly.RootComponent.GetChildren(j).GetChildren(k).GetChildren(1).GetChildren(0).Prototype 'defining te skeleton part
                    SkelPart.Features.GetFeatures()

                    'End

                    For i = 0 To SkelPart.Features.GetFeatures.Length - 1 'par
                        Dim CurrentFeat As Features.Feature = SkelPart.Features.GetFeatures(i)
                        'Dim CurrentFeatName As String = Split(CurrentFeat.Name, ".")(0) 'ancienne façon de récupérer le code
                        Dim CurrentFeatName As String = Left(CurrentFeat.Name, InStrRev(CurrentFeat.Name, ".") - 1)
                        'If InStr(CurrentFeatName, "_") <> 0 Then CurrentFeatName = Split(CurrentFeatName, "_")(0) 'ancienne façon de récupérer le code dipole
                        If InStr(CurrentFeatName, "/") <> 0 Then CurrentFeatName = Left(CurrentFeat.Name, InStrRev(CurrentFeat.Name, "_") - 1)
                        If CurrentFeat.FeatureType.ToString = "DATUM_CSYS" _
                            And InStr(1, CurrentFeat.Name, "Entrée", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "Sortie", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "1/3", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "3/3", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "1/5", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "2/5", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "4/5", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "5/5", 1) = 0 _
                            And InStr(1, CurrentFeat.Name, "2/2", 1) = 0 _
                            Then
                            If CAOdic.ContainsKey(CurrentFeatName) Then
                                CAOdic.Item(CurrentFeatName) += 1
                            Else
                                CAOdic.Add(CurrentFeatName, 1)
                            End If
                        End If
                    Next
                End If
            Next k
            Next j
        i = 0
        For Each item In CAOdic



            For j = 0 To nbMagnets - 1
                If CAOitem(j, 2) = item.Key Then
                    CAOcomp(i, 0) = CAOitem(j, 0)
                    CAOcomp(i, 1) = CAOitem(j, 1)
                    CAOcomp(i, 2) = item.Key
                    i += 1
                End If
            Next

        Next
        nbMagnets = i
        Dim magnets As New Dictionary(Of String, Integer)



        For i = 0 To nbMagnets - 1
            magnetname = "@DB/" & CAOcomp(i, 0) & "/" & CAOcomp(i, 1)
            If magnets.ContainsKey(magnetname) Then
                magnets.Item(magnetname) += 1
            Else
                magnets.Add(magnetname, 1)
            End If


            Dim basepart1 As Part

            If magnets(magnetname) = 1 Then basepart1 = theSession.Parts.OpenActiveDisplay(magnetname, NXOpen.DisplayPartOption.AllowAdditional, Nothing)
            workPart = theSession.Parts.Work
            displayPart = theSession.Parts.Display



        Next

        Form1.ProgressBar1.Value = 150
        xlWorkBook.Close()
        xlApp.Quit()

        Return CAOcomp
    End Function

    Sub Import_Magnets(CAOcomp As Array, SkelComp As Component, AssemblyPart As Part)

        Dim i As Integer
        Dim partIndex As Integer

        Dim ListOfParts() As NXOpen.BasePart = theSession.Parts.ToArray()
        'Dim AssemblyPart As Part
        Dim TempPartList(0) As BasePart

        'AssemblyPart = ListOfParts(0) 'Defining the Main Assembly part
        'MsgBox("main assembly part is " & AssemblyPart.Name)
        Dim SkelPart As Part = SkelComp.Prototype 'defining te skeleton part
        'MsgBox("skel  part is " & SkelPart.Name)
        Dim nbmagnets As Integer = CInt((SkelPart.Features.GetFeatures.Length - 1) / 10)
        'Dim pgBarInc As Integer
        'If nbmagnets <> 0 Then pgBarInc = CInt(700 / nbmagnets)

        Dim xlApp As Excel.Application
        Dim xlWorkBook As Excel.Workbook
        Dim xlWorkSheet As Excel.Worksheet
        Dim path_to_excel_file As String = Form1.TextBox1.Text
        xlApp = New Excel.Application
        xlWorkBook = xlApp.Workbooks.Open(path_to_excel_file)
        xlWorkSheet = xlWorkBook.Worksheets(1)
        'Dim CAOitem(200) As String
        'Dim CAOrev(200) As String
        'Dim CAOcode(200) As String
        'i = 0
        'While xlWorkSheet.Cells(i + 2, 1).value <> ""
        '    CAOitem(i) = xlWorkSheet.Cells(i + 1, 1).value.ToString
        '    CAOcode(i) = xlWorkSheet.Cells(i + 1, 4).value.ToString
        '    CAOrev(i) = xlWorkSheet.Cells(i + 1, 2).value.ToString

        '    i += 1
        'End While
        'ReDim Preserve CAOcode(i)
        'ReDim Preserve CAOrev(i)
        'ReDim Preserve CAOitem(i)



        'MsgBox(ListOfParts.Length)

        Dim j, k As Integer
        For i = 0 To SkelPart.Features.GetFeatures.Length - 1 'parsing skeleton feature to import the right magnets
            Dim CurrentFeat As Features.Feature = SkelPart.Features.GetFeatures(i)
            ' when a magnet CSYS feature is parsed :
            'MsgBox(CurrentFeat.Name)
            'If InStr(CurrentFeat.Name, "SHF") <> 0 Then
            'Dim stopstr As String = "Stop"

            'End If
            If CurrentFeat.FeatureType.ToString = "DATUM_CSYS" _
                And InStr(1, CurrentFeat.Name, "Entrée", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "Sortie", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "Drift", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "1/3", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "3/3", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "1/5", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "2/5", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "4/5", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "2/2", 1) = 0 _
                And InStr(1, CurrentFeat.Name, "5/5", 1) = 0 Then


                'Dim FeatName As String = Split(CurrentFeat.Name, ".")(0)
                Dim FeatName As String = Left(CurrentFeat.Name, InStrRev(CurrentFeat.Name, ".") - 1)
                Dim MatchedFound As Boolean = False
                Dim test As String = "change"
                j = 0
                For Each item In CAOcomp

                    k = 0
                    'If CAOcomp(j, 2) = Split(FeatName, "_")(0) Then



                    If InStr(FeatName, "DIP") > 0 Then
                        FeatName = Left(CurrentFeat.Name, InStrRev(CurrentFeat.Name, "_") - 1)
                    End If





                    If CAOcomp(j, 2) = FeatName Then

                        For Each partItem In ListOfParts
                            If CAOcomp(j, 0) = Split(partItem.Name, "/")(0) Then
                                partIndex = k
                                MatchedFound = True
                                Exit For
                            End If
                            k += 1
                        Next
                        If MatchedFound Then Exit For
                    End If
                    j += 1
                    If j > 200 Then Exit For
                Next

                'adding the right magnet as component to the main assembly
                If MatchedFound = True Then

                    Dim partLoadStatus1 As NXOpen.PartLoadStatus = Nothing
                    Dim status1 As NXOpen.PartCollection.SdpsStatus = Nothing
                    status1 = theSession.Parts.SetActiveDisplay(storagering_part, NXOpen.DisplayPartOption.AllowAdditional, NXOpen.PartDisplayPartWorkPartOption.UseLast, partLoadStatus1)
                    workPart = theSession.Parts.Work ' CAO000084189/AA-ANNEAU DE STOCKAGE
                    displayPart = theSession.Parts.Display ' CAO000084189/AA-ANNEAU DE STOCKAGE
                    partLoadStatus1.Dispose()
                    theSession.Parts.SetWorkComponent(SkelComp.Parent, NXOpen.PartCollection.RefsetOption.Entire, NXOpen.PartCollection.WorkComponentOption.Visible, partLoadStatus1)
                    workPart = theSession.Parts.Work
                    partLoadStatus1.Dispose()






                    Dim addComponentBuilder1 As NXOpen.Assemblies.AddComponentBuilder
                    addComponentBuilder1 = AssemblyPart.AssemblyManager.CreateAddComponentBuilder()
                    'theSession.Parts.SetWork(AssemblyPart)

                    addComponentBuilder1.ReferenceSet = "MODEL"
                    TempPartList(0) = ListOfParts(partIndex)
                    ' MsgBox("featname Is " & FeatName) 'sextsextupole 1
                    'MsgBox("tempartlist(0).name Is " & TempPartList(0).Name) '73750/AA
                    'MsgBox("part index Is " & partIndex) '0
                    'MsgBox(AssemblyPart.Name) ''73750/AA
                    addComponentBuilder1.SetPartsToAdd(TempPartList)
                    Dim CurrentMagComp As NXObject = addComponentBuilder1.Commit()

                    addComponentBuilder1.Destroy()

                    'set constraint between skeleton comp and current magnet comp within Assembly at CurrentFeat (CSYS)")
                    'MsgBox(SkelComp.Name) '73714
                    'MsgBox(CurrentMagComp.Name)
                    'MsgBox(CurrentFeat.Name)
                    'MsgBox(AssemblyPart.Name)

                    SetConstraints(SkelComp, CurrentMagComp, AssemblyPart, CurrentFeat.Name)

                End If

            End If




        Next

    End Sub



    Sub SetConstraints(SkelComp As NXObject, CurrentMagComp As NXObject, AssemblyPart As Part, SkelCSYSName As String)
        Dim SkelCSYS As NXOpen.CartesianCoordinateSystem
        Dim SkelCSYSji As String = Nothing
        Dim MagnetCSYSji As String = Nothing
        'MsgBox(SkelComp.Name & CurrentMagComp.Name & AssemblyPart.Name & SkelCSYSName)
        Dim componentPositioner1 As NXOpen.Positioning.ComponentPositioner
        componentPositioner1 = AssemblyPart.ComponentAssembly.Positioner
        componentPositioner1.ClearNetwork()
        componentPositioner1.BeginAssemblyConstraints()
        Dim network1 As NXOpen.Positioning.Network
        network1 = componentPositioner1.EstablishNetwork()
        Dim componentNetwork1 As NXOpen.Positioning.ComponentNetwork = CType(network1, NXOpen.Positioning.ComponentNetwork)
        componentNetwork1.MoveObjectsState = True
        Dim nullNXOpen_Assemblies_Component As NXOpen.Assemblies.Component = Nothing
        componentNetwork1.DisplayComponent = nullNXOpen_Assemblies_Component
        componentNetwork1.MoveObjectsState = True
        Dim constraint1 As NXOpen.Positioning.Constraint
        constraint1 = componentPositioner1.CreateConstraint(True)
        Dim componentConstraint1 As NXOpen.Positioning.ComponentConstraint = CType(constraint1, NXOpen.Positioning.ComponentConstraint)
        componentConstraint1.ConstraintAlignment = NXOpen.Positioning.Constraint.Alignment.InferAlign
        componentConstraint1.ConstraintType = NXOpen.Positioning.Constraint.Type.Touch


        'parsing journal identifier of CSYS feature
        For Each tempfeature As Features.Feature In SkelComp.Prototype.OwningPart.Features
            If tempfeature.Name = SkelCSYSName And tempfeature.FeatureType.ToString = "DATUM_CSYS" _
            And InStr(1, tempfeature.Name, "Entrée", 1) = 0 _
            And InStr(1, tempfeature.Name, "Sortie", 1) = 0 _
            And InStr(1, tempfeature.Name, "Drift", 1) = 0 _
            And InStr(1, tempfeature.Name, "1/3", 1) = 0 _
            And InStr(1, tempfeature.Name, "3/3", 1) = 0 _
            And InStr(1, tempfeature.Name, "1/5", 1) = 0 _
            And InStr(1, tempfeature.Name, "2/5", 1) = 0 _
            And InStr(1, tempfeature.Name, "4/5", 1) = 0 _
            And InStr(1, tempfeature.Name, "5/5", 1) = 0 Then


                SkelCSYSji = tempfeature.JournalIdentifier
            End If

        Next
        SkelCSYS = CType(SkelComp.FindObject("PROTO#.Features|" & SkelCSYSji & "|CSYSTEM 1"), NXOpen.CartesianCoordinateSystem)

        Dim constraintReference1 As NXOpen.Positioning.ConstraintReference = componentConstraint1.CreateConstraintReference(SkelComp, SkelCSYS, False, False)


        'parsing journal identifier of CSYS feature
        Dim MagnetCSYS As NXOpen.CartesianCoordinateSystem
        For Each tempfeature As Features.Feature In CurrentMagComp.Prototype.OwningPart.Features
            'MsgBox("currentfeat is " & tempfeature.Name & " and instrbool is " & InStr(tempfeature.Name, "RPM"))
            If InStr(tempfeature.Name, "RPM") <> 0 And tempfeature.FeatureType.ToString = "DATUM_CSYS" Then

                MagnetCSYSji = tempfeature.JournalIdentifier
            End If

        Next
        MagnetCSYS = CType(CurrentMagComp.FindObject("PROTO#.Features|" & MagnetCSYSji & "|CSYSTEM 1"), NXOpen.CartesianCoordinateSystem)
        'MsgBox(MagnetCSYS.Name)
        Dim constraintReference2 As NXOpen.Positioning.ConstraintReference = componentConstraint1.CreateConstraintReference(CurrentMagComp, MagnetCSYS, False, False)
        constraintReference2.SetFixHint(True)
        componentNetwork1.Solve()


        componentPositioner1.ClearNetwork()
        componentPositioner1.DeleteNonPersistentConstraints()
        componentPositioner1.EndAssemblyConstraints()
        Dim displayedConstraint1 As NXOpen.Positioning.DisplayedConstraint = constraint1.GetDisplayedConstraint
        'displayedConstraint1.Blank()


    End Sub
    Public Function GetUnloadOption() As Integer

        'Unloads the image when the NX session terminates
        GetUnloadOption = NXOpen.Session.LibraryUnloadOption.Immediately

    End Function
    Sub rename_instances()

        Dim dispPart As Part = theSession.Parts.Display

        'lw.Open()

        Try
            Dim c As ComponentAssembly = dispPart.ComponentAssembly
            'to process the work part rather than the display part,
            '  comment the previous line and uncomment the following line
            'Dim c As ComponentAssembly = workPart.ComponentAssembly
            If Not IsNothing(c.RootComponent) Then
                '*** insert code to process 'root component' (assembly file)
                lw.WriteLine("Assembly: " & c.RootComponent.DisplayName)
                lw.WriteLine(" + Active Arrangement: " & c.ActiveArrangement.Name)
                '*** end of code to process root component
                reportComponentChildren(c.RootComponent, 0)
            Else
                '*** insert code to process piece part
                lw.WriteLine("Part has no components")
            End If
        Catch e As Exception
            theSession.ListingWindow.WriteLine("Failed: " & e.ToString)
        End Try
        'lw.Close()

    End Sub

    '**********************************************************
    Sub reportComponentChildren(ByVal comp As Component,
        ByVal indent As Integer)

        For Each child As Component In comp.GetChildren()

            Dim childarray(0) As NXOpen.NXObject
            childarray(0) = child
            Dim objectGeneralPropertiesBuilder1 As NXOpen.ObjectGeneralPropertiesBuilder = Nothing
            objectGeneralPropertiesBuilder1 = workPart.PropertiesManager.CreateObjectGeneralPropertiesBuilder(childarray)
            Dim selectNXObjectList2 As NXOpen.SelectNXObjectList = Nothing
            selectNXObjectList2 = objectGeneralPropertiesBuilder1.SelectedObjects
            objectGeneralPropertiesBuilder1.Name = child.Prototype.Name
            Dim nXObject3 As NXOpen.NXObject = Nothing
            nXObject3 = objectGeneralPropertiesBuilder1.Commit()

            '*** insert code to process component or subassembly
            lw.WriteLine(New String(" ", indent * 2) & child.DisplayName())
            '*** end of code to process component or subassembly
            If child.GetChildren.Length <> 0 Then
                '*** this is a subassembly, add code specific to subassemblies
                lw.WriteLine(New String(" ", indent * 2) &
                    "* subassembly with " &
                    child.GetChildren.Length & " components")
                lw.WriteLine(New String(" ", indent * 2) &
                    " + Active Arrangement: " &
                    child.OwningPart.ComponentAssembly.ActiveArrangement.Name)
                '*** end of code to process subassembly
            Else
                'this component has no children (it is a leaf node)
                'add any code specific to bottom level components
            End If
            reportComponentChildren(child, indent + 1)
        Next
    End Sub

End Module

