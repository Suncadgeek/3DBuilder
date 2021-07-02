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
Imports Excel = Microsoft.Office.Interop.Excel


Module Module1


    Public theSession As Session = Session.GetSession()
    Public ufs As UFSession = UFSession.GetUFSession()
    Public workPart As Part = theSession.Parts.Work
    Public lw As ListingWindow = theSession.ListingWindow
    Public displayPart As NXOpen.Part = theSession.Parts.Display
    Public referenceSet1 As NXOpen.ReferenceSet
    Public Sub Main()

        Form1.Show()

    End Sub

    Public Sub Outline()
        Dim ListOfParts() As NXOpen.BasePart = theSession.Parts.ToArray()
        If ListOfParts.Length <> 0 Then
            MsgBox("Veuillez fermer toute les pièces avant d'exécuter le programme")
            Exit Sub
        End If
        Dim xlerror As Integer = Open_Parts()
        If xlerror = 1 Then Exit Sub

        Dim Skelcomp As NXObject = Create_Assembly()
        Import_Magnets(Skelcomp)
        Form1.ProgressBar1.Value = 100


    End Sub

    Function Open_Parts()


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




            Return 1
        End Try
        Dim CAOitem(30) As String
        Dim CAOrev(30) As String
        Dim i As Integer = 0
        While xlWorkSheet.Cells(i + 2, 1).value <> ""
            CAOitem(i) = xlWorkSheet.Cells(i + 2, 1).value.ToString

            CAOrev(i) = xlWorkSheet.Cells(i + 2, 2).value.ToString

            i += 1
        End While
        'ReDim CAOitem(i - 2)
        'ReDim CAOrev(i - 2)

        Form1.ProgressBar1.Value = 70
        Dim skeletonpath, magnetname As String
        Dim basePart1 As NXOpen.BasePart = Nothing
        Dim partLoadStatus1 As NXOpen.PartLoadStatus = Nothing
        'loading skeleton
        skeletonpath = "@DB/" & CAOitem(0) & "/" & CAOrev(0)
        basePart1 = theSession.Parts.OpenActiveDisplay(skeletonpath, NXOpen.DisplayPartOption.AllowAdditional, partLoadStatus1)
        workPart = theSession.Parts.Work
        displayPart = theSession.Parts.Display
        partLoadStatus1.Dispose()
        Form1.ProgressBar1.Value = 100
        'loading magnets

        ' Dim files() As String = IO.Directory.GetFiles(magnetspath)
        Dim j = i
        For i = 1 To j - 1
            magnetname = "@DB/" & CAOitem(i) & "/" & CAOrev(i)
            'MsgBox(magnetname)
            basePart1 = theSession.Parts.OpenActiveDisplay(magnetname, NXOpen.DisplayPartOption.AllowAdditional, partLoadStatus1)
            workPart = theSession.Parts.Work
            displayPart = theSession.Parts.Display
            partLoadStatus1.Dispose()


        Next
        'MsgBox("all magnets loaded")
        Form1.ProgressBar1.Value = 150
        xlWorkBook.Close()
        xlApp.Quit()
        Return 0

    End Function
    Function Create_Assembly()

        Dim theSession As NXOpen.Session = NXOpen.Session.GetSession()
        ' ----------------------------------------------
        '    Menu: Fichier->Nouveau->Elément...
        ' ----------------------------------------------
        Dim markId1 As NXOpen.Session.UndoMarkId
        markId1 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Départ")

        Dim fileNew1 As NXOpen.FileNew
        fileNew1 = theSession.Parts.FileNew()

        fileNew1.TemplateFileName = "@DB/000120/A"

        fileNew1.UseBlankTemplate = False

        fileNew1.ApplicationName = "AssemblyTemplate"

        fileNew1.Units = NXOpen.Part.Units.Millimeters

        fileNew1.RelationType = "master"

        fileNew1.UsesMasterModel = "No"

        fileNew1.TemplateType = NXOpen.FileNewTemplateType.Item

        fileNew1.TemplatePresentationName = "Assemblage"

        fileNew1.ItemType = "SO8_CAD"

        fileNew1.Specialization = ""

        fileNew1.SetCanCreateAltrep(False)

        Dim partOperationCreateBuilder1 As NXOpen.PDM.PartOperationCreateBuilder
        partOperationCreateBuilder1 = theSession.PdmSession.CreateCreateOperationBuilder(NXOpen.PDM.PartOperationBuilder.OperationType.Create)

        fileNew1.SetPartOperationCreateBuilder(partOperationCreateBuilder1)

        partOperationCreateBuilder1.SetOperationSubType(NXOpen.PDM.PartOperationCreateBuilder.OperationSubType.FromTemplate)

        partOperationCreateBuilder1.SetModelType("master")

        partOperationCreateBuilder1.SetItemType("SO8_CAD")

        Dim logicalobjects1() As NXOpen.PDM.LogicalObject = Nothing
        partOperationCreateBuilder1.CreateLogicalObjects(logicalobjects1)

        Dim sourceobjects1() As NXOpen.NXObject
        sourceobjects1 = logicalobjects1(0).GetUserAttributeSourceObjects()

        partOperationCreateBuilder1.DefaultDestinationFolder = ":Newstuff"

        Dim sourceobjects2() As NXOpen.NXObject
        sourceobjects2 = logicalobjects1(0).GetUserAttributeSourceObjects()

        partOperationCreateBuilder1.SetOperationSubType(NXOpen.PDM.PartOperationCreateBuilder.OperationSubType.FromTemplate)

        Dim sourceobjects3() As NXOpen.NXObject
        sourceobjects3 = logicalobjects1(0).GetUserAttributeSourceObjects()

        theSession.SetUndoMarkName(markId1, "Boîte de dialogue Nouvel élément")

        Dim attributetitles1(0) As String
        attributetitles1(0) = "DB_PART_NO"
        Dim titlepatterns1(0) As String
        titlepatterns1(0) = """CAO""nnnnnnnnn"
        Dim nXObject1 As NXOpen.NXObject
        nXObject1 = partOperationCreateBuilder1.CreateAttributeTitleToNamingPatternMap(attributetitles1, titlepatterns1)

        Dim objects1(0) As NXOpen.NXObject
        objects1(0) = logicalobjects1(0)
        Dim properties1(0) As NXOpen.NXObject
        properties1(0) = nXObject1
        Dim errorList1 As NXOpen.ErrorList
        errorList1 = partOperationCreateBuilder1.AutoAssignAttributesWithNamingPattern(objects1, properties1)

        errorList1.Dispose()
        Dim errorMessageHandler1 As NXOpen.PDM.ErrorMessageHandler
        errorMessageHandler1 = partOperationCreateBuilder1.GetErrorMessageHandler(True)

        Dim markId2 As NXOpen.Session.UndoMarkId
        markId2 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "Nouvel élément")

        theSession.DeleteUndoMark(markId2, Nothing)

        Dim markId3 As NXOpen.Session.UndoMarkId
        markId3 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "Nouvel élément")

        fileNew1.MasterFileName = "3DBuild"

        fileNew1.MakeDisplayedPart = True

        fileNew1.DisplayPartOption = NXOpen.DisplayPartOption.AllowAdditional

        partOperationCreateBuilder1.ValidateLogicalObjectsToCommit()

        Dim logicalobjects2(0) As NXOpen.PDM.LogicalObject
        logicalobjects2(0) = logicalobjects1(0)
        partOperationCreateBuilder1.CreateSpecificationsForLogicalObjects(logicalobjects2)

        Dim errorMessageHandler2 As NXOpen.PDM.ErrorMessageHandler
        errorMessageHandler2 = partOperationCreateBuilder1.GetErrorMessageHandler(True)

        Dim errorMessageHandler3 As NXOpen.PDM.ErrorMessageHandler
        errorMessageHandler3 = partOperationCreateBuilder1.GetErrorMessageHandler(True)

        Dim nXObject2 As NXOpen.NXObject
        nXObject2 = fileNew1.Commit()

        Dim workPart As NXOpen.Part = theSession.Parts.Work

        Dim displayPart As NXOpen.Part = theSession.Parts.Display

        Dim errorMessageHandler4 As NXOpen.PDM.ErrorMessageHandler
        errorMessageHandler4 = partOperationCreateBuilder1.GetErrorMessageHandler(True)

        theSession.DeleteUndoMark(markId3, Nothing)

        fileNew1.Destroy()

        theSession.ApplicationSwitchImmediate("UG_APP_MODELING")
        Form1.ProgressBar1.Value = 200

        Dim ListOfParts() As NXOpen.BasePart = theSession.Parts.ToArray()
        Dim AssemblyPart As Part = ListOfParts(0)
        Array.Reverse(ListOfParts)
        Dim SkelPart As Part = ListOfParts(0)
        Dim addComponentBuilder1 As NXOpen.Assemblies.AddComponentBuilder
        addComponentBuilder1 = AssemblyPart.AssemblyManager.CreateAddComponentBuilder()
        addComponentBuilder1.ReferenceSet = "MODEL"
        Dim TempPartList(0) As BasePart
        TempPartList(0) = SkelPart
        addComponentBuilder1.SetPartsToAdd(TempPartList)
        Dim SkelComp As NXObject = addComponentBuilder1.Commit()
        addComponentBuilder1.Destroy()
        Form1.ProgressBar1.Value = 250

        ' set fix anchor for skeleton

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
        componentNetwork1.NetworkArrangementsMode = NXOpen.Positioning.ComponentNetwork.ArrangementsMode.Existing
        componentNetwork1.MoveObjectsState = True
        'Dim constraint1 As NXOpen.Positioning.Constraint
        'constraint1 = componentPositioner1.CreateConstraint(True)
        'Dim componentConstraint1 As NXOpen.Positioning.ComponentConstraint = CType(constraint1, NXOpen.Positioning.ComponentConstraint)
        'componentConstraint1.ConstraintAlignment = NXOpen.Positioning.Constraint.Alignment.InferAlign
        'componentConstraint1.ConstraintType = NXOpen.Positioning.Constraint.Type.Touch
        Dim constraint2 As NXOpen.Positioning.Constraint
        constraint2 = componentPositioner1.CreateConstraint(True)
        Dim componentConstraint2 As NXOpen.Positioning.ComponentConstraint = CType(constraint2, NXOpen.Positioning.ComponentConstraint)
        componentConstraint2.ConstraintType = NXOpen.Positioning.Constraint.Type.Fix
        Dim constraintReference3 As NXOpen.Positioning.ConstraintReference = componentConstraint2.CreateConstraintReference(SkelComp, SkelComp, False, False, False)
        constraintReference3.SetFixHint(True)
        componentNetwork1.Solve()
        componentNetwork1.Solve()
        componentPositioner1.ClearNetwork()
        componentPositioner1.DeleteNonPersistentConstraints()
        componentPositioner1.EndAssemblyConstraints()
        Dim displayedConstraint2 As NXOpen.Positioning.DisplayedConstraint = constraint2.GetDisplayedConstraint
        displayedConstraint2.Blank()



        Return SkelComp

    End Function
    Sub Import_Magnets(SkelComp As NXObject)

        Dim i As Integer
        Dim partIndex, octuIndex, sextIndex, bendIndex, revIndex, quadIndex As Integer

        Dim ListOfParts() As NXOpen.BasePart = theSession.Parts.ToArray()
        Dim AssemblyPart As Part
        Dim TempPartList(0) As BasePart

        AssemblyPart = ListOfParts(0) 'Defining the Main Assembly part
        'MsgBox("main assembly part is " & AssemblyPart.Name)
        Dim SkelPart As Part = ListOfParts(ListOfParts.Length - 1) 'defining te skeleton part
        'MsgBox("skel  part is " & SkelPart.Name)
        Dim nbmagnets As Integer = CInt((SkelPart.Features.GetFeatures.Length - 1) / 5)
        Dim pgBarInc As Integer = CInt(700 / nbmagnets)
        'indexing magnets parts opened :
        For i = 0 To ListOfParts.Length - 1
            'MsgBox("ListOfParts(" & i & ") is " & ListOfParts(i).Name)

        Next

        For i = 0 To ListOfParts.Length - 1
            If InStr(ListOfParts(i).Name, "OCTUPOLE") <> 0 Then
                octuIndex = i
                'MsgBox(ListOfParts(i).Name & " is OCTUPOLE and index is " & octuIndex)
            End If
        Next

        For i = 0 To ListOfParts.Length - 1
            If InStr(ListOfParts(i).Name, "SEXTUPOLE") <> 0 Then
                sextIndex = i
                'MsgBox(ListOfParts(i).Name & " is SEXTUPOLE and index is " & sextIndex)
            End If
        Next

        For i = 0 To ListOfParts.Length - 1
            If InStr(ListOfParts(i).Name, "REVERSE") <> 0 Or InStr(ListOfParts(i).Name, "ANTI") <> 0 Then
                revIndex = i
                'MsgBox(ListOfParts(i).Name & " is REVERSE and index is " & revIndex)
            End If
        Next

        For i = 0 To ListOfParts.Length - 1
            If InStr(ListOfParts(i).Name, "DIPOLE") <> 0 Then
                bendIndex = i
                'MsgBox(ListOfParts(i).Name & " is DIPOLE and index is " & bendIndex)
            End If
        Next

        For i = 0 To ListOfParts.Length - 1
            If InStr(ListOfParts(i).Name, "QUADRUPOLE") <> 0 Then
                quadIndex = i
                'MsgBox(ListOfParts(i).Name & " is QUADRUPOLE and index is " & quadIndex)
            End If
        Next
        ' magnet parts indexed

        For i = 0 To SkelPart.Features.GetFeatures.Length - 1 'parsing skeleton feature to import the right magnet
            Dim CurrentFeat As Features.Feature = SkelPart.Features.GetFeatures(i)
            ' when a magnet CSYS feature is parsed :
            If CurrentFeat.FeatureType.ToString = "DATUM_CSYS" And InStr(1, CurrentFeat.Name, "Entrée", 1) = 0 And InStr(1, CurrentFeat.Name, "Sortie", 1) = 0 Then


                Dim FeatName As String = Left(CurrentFeat.Name, 4)

                Dim MatchedFound As Boolean = True
                Select Case FeatName
                    Case "QUAD"
                        partIndex = quadIndex
                    Case "SEXT"
                        partIndex = sextIndex
                    Case "BEND"
                        partIndex = bendIndex
                    Case "REVE"
                        partIndex = revIndex
                    Case "OCTU"
                        partIndex = octuIndex
                    Case Else
                        MatchedFound = False

                End Select

                'adding the right magnet as component to the main assembly
                If MatchedFound = True Then

                    Dim addComponentBuilder1 As NXOpen.Assemblies.AddComponentBuilder
                    addComponentBuilder1 = AssemblyPart.AssemblyManager.CreateAddComponentBuilder()

                    addComponentBuilder1.ReferenceSet = "MODEL"
                    TempPartList(0) = ListOfParts(partIndex)
                    ' MsgBox("featname is " & FeatName) 'sextsextupole 1
                    'MsgBox("tempartlist(0).name is " & TempPartList(0).Name) '73750/AA
                    'MsgBox("part index is " & partIndex) '0
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
                    If (Form1.ProgressBar1.Value + pgBarInc < 1000) Then Form1.ProgressBar1.Value += pgBarInc
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
        componentNetwork1.NetworkArrangementsMode = NXOpen.Positioning.ComponentNetwork.ArrangementsMode.Existing
        componentNetwork1.MoveObjectsState = True
        Dim constraint1 As NXOpen.Positioning.Constraint
        constraint1 = componentPositioner1.CreateConstraint(True)
        Dim componentConstraint1 As NXOpen.Positioning.ComponentConstraint = CType(constraint1, NXOpen.Positioning.ComponentConstraint)
        componentConstraint1.ConstraintAlignment = NXOpen.Positioning.Constraint.Alignment.InferAlign
        componentConstraint1.ConstraintType = NXOpen.Positioning.Constraint.Type.Touch


        'parsing journal identifier of CSYS feature
        For Each tempfeature As Features.Feature In SkelComp.Prototype.OwningPart.Features
            If tempfeature.Name = SkelCSYSName And tempfeature.FeatureType.ToString = "DATUM_CSYS" And InStr(1, tempfeature.Name, "Entrée", 1) = 0 And InStr(1, tempfeature.Name, "Sortie", 1) = 0 Then
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
        displayedConstraint1.Blank()


    End Sub
    Public Function GetUnloadOption() As Integer

        'Unloads the image when the NX session terminates
        GetUnloadOption = NXOpen.Session.LibraryUnloadOption.Immediately

    End Function
End Module

