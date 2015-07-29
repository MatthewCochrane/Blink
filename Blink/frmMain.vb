Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL

'Imports System.Windows.Input
'Imports System.Windows.Input.StylusPlugIns
'Imports System.Windows.Ink
Imports Microsoft.Ink
Imports Microsoft.StylusInput
'Imports WindowsHook
'Imports VBTablet

'http://www.code-magazine.com/article.aspx?quickid=0512052&page=4

Public Class frmMain
    Inherits Form

    Dim WithEvents Tab As HWTablet
    Private WithEvents tI As TabletInterpreter
    Private WithEvents inkGraphics As InkGraphics
    Private WithEvents GLScreen As GLControl

    Private strokeInterpreter As StrokeInterpreter

    Private requestRedraw As Boolean = True

    Private Sub GLScreen_KeyPress(sender As Object, e As Windows.Forms.KeyPressEventArgs) Handles GLScreen.KeyPress
        If isInputModeActivated(InputMode_Enum.Text_Entering) Then
            inkGraphics.tmpText.HandleKeyPress(e.KeyChar)
        End If

        If e.KeyChar = Chr(26) Then
            'ctrl-z = Undo
            tI.pageData.undoQueue.Undo()
            Redraw()
            UpdateTodoList()
        ElseIf e.KeyChar = Chr(25) Then
            'ctrl-y = Redo
            tI.pageData.undoQueue.Redo()
            Redraw()
            UpdateTodoList()
        ElseIf e.KeyChar = Chr(3) Then
            'ctrl-c = copy
            CopySelection()
        ElseIf e.KeyChar = Chr(22) Then
            'ctrl-v = paste
            PasteSelection()
        ElseIf e.KeyChar = Chr(24) Then
            'ctrl-x = cut
            CopySelection()
            DeleteSelection("Cut")
        ElseIf e.KeyChar = Chr(8) Then
            'backspace key (called on delete key pressed too, sent from keydown event) = Delete Selection
            DeleteSelection("Deleted")
        End If
    End Sub

    Public Sub CopySelection()
        If tI.pageData.selection.Count > 0 Then
            My.Computer.Clipboard.SetData("StrokeList", tI.pageData.selection.toBinary)
        End If
    End Sub

    Public Sub PasteSelection()
        If My.Computer.Clipboard.ContainsData("StrokeList") = True Then
            Dim tmpStrokeList As StrokeList = StrokeList.fromBinary(CType(My.Computer.Clipboard.GetData("StrokeList"), Byte()))
            Dim cursorWorldPos As PointF = tI.mapScreenToWorld(New TabletPoint2D(GLScreen.PointToClient(System.Windows.Forms.Control.MousePosition).X, GLScreen.PointToClient(System.Windows.Forms.Control.MousePosition).Y, 0)).toPointF
            'Dim backupOfSelection As StrokeList = tI.pageData.selection.ShallowClone()

            'get upper left
            Dim upperLeft As PointF = Drawing2D.findRangeOfStrokes(tmpStrokeList).Location

            For Each s In tmpStrokeList
                s.MoveStroke(Drawing2D.pointFSubtract(cursorWorldPos, upperLeft))
                'Drawing2D.setStrokeLocation(s, cursorWorldPos)
            Next

            tI.pageData.AtomicDrawingOperation_Begin()
            tI.pageData.AddStrokes(tmpStrokeList)
            tI.pageData.AtomicDrawingOperation_End("Paste " & tmpStrokeList.Count & " Object" & IIf(tmpStrokeList.Count > 1, "s", ""))
            Redraw()
        End If
    End Sub

    Public Sub DeleteSelection(Optional UndoName As String = "Deleted")
        If tI.pageData.selection.Count > 0 Then
            tI.pageData.AtomicDrawingOperation_Begin()
            For Each s In tI.pageData.selection
                tI.pageData.DeleteStrokes(s)
            Next
            tI.pageData.AtomicDrawingOperation_End(UndoName & " " & tI.pageData.selection.Count & " Object" & IIf(tI.pageData.selection.Count > 1, "s", ""))
            tI.pageData.clearSelection()
            Redraw()
        End If
    End Sub

    Private Sub GLScreen_KeyUp(sender As Object, e As KeyEventArgs) Handles GLScreen.KeyUp
        GLScreen_MouseMove(sender, New MouseEventArgs(System.Windows.Forms.Control.MouseButtons, 0, GLScreen.PointToClient(System.Windows.Forms.Control.MousePosition).X, GLScreen.PointToClient(System.Windows.Forms.Control.MousePosition).Y, 0))
    End Sub

    Private Sub GLScreen_KeyDown(sender As Object, e As KeyEventArgs) Handles GLScreen.KeyDown
        If e.KeyCode = Keys.Delete Then
            GLScreen_KeyPress(sender, New System.Windows.Forms.KeyPressEventArgs(Chr(8))) 'simulate pressing the backspace key
        End If
        GLScreen_MouseMove(sender, New MouseEventArgs(System.Windows.Forms.Control.MouseButtons, 0, GLScreen.PointToClient(System.Windows.Forms.Control.MousePosition).X, GLScreen.PointToClient(System.Windows.Forms.Control.MousePosition).Y, 0))
        If isInputModeActivated(InputMode_Enum.Text_Entering) = True Then
            inkGraphics.tmpText.HandleKeyPress(e)
        End If
    End Sub

    Public Enum ActiveMouseMode_Enum
        None = &H0
        Panning = &H1
        Selecting = &H2
        DragingSelection = &H4
        ScalingSelection = &H8
        DraggingTabletRegion = &H10
    End Enum

    Public Enum PassiveMouseMode_Enum
        None = &H0
        HoveringSelection = &H1
    End Enum

    Public Enum InputMode_Enum
        Normal = &H0
        Text_Entering = &H1
    End Enum

    Public activeMouseMode As ActiveMouseMode_Enum = ActiveMouseMode_Enum.None
    Public passiveMouseMode As PassiveMouseMode_Enum = PassiveMouseMode_Enum.None
    Public inputMode As InputMode_Enum = InputMode_Enum.Normal
    Private mousePos_start As PointF
    Private mouse_Pan_Start As PointF
    Private backupSelection As StrokeList
    Private moveStrokeOffsetList As List(Of PointF)

    Private textBoxSelected As Boolean = False

    Private Sub addActiveMouseMode(mode As ActiveMouseMode_Enum)
        activeMouseMode = activeMouseMode Or mode
    End Sub

    Private Sub removeActiveMouseMode(mode As ActiveMouseMode_Enum)
        activeMouseMode = activeMouseMode And Not (mode)
    End Sub

    Private Sub clearActiveMouseMode()
        activeMouseMode = ActiveMouseMode_Enum.None
    End Sub

    Private Function isActiveMouseModeActivated(mode As ActiveMouseMode_Enum) As Boolean
        Return activeMouseMode And mode
    End Function

    Private Sub addPassiveMouseMode(mode As PassiveMouseMode_Enum)
        passiveMouseMode = passiveMouseMode Or mode
    End Sub

    Private Sub removePassiveMouseMode(mode As PassiveMouseMode_Enum)
        passiveMouseMode = passiveMouseMode And Not (mode)
    End Sub

    Private Sub clearPassiveMouseMode()
        passiveMouseMode = PassiveMouseMode_Enum.None
    End Sub

    Private Function isPassiveMouseModeActivated(mode As PassiveMouseMode_Enum) As Boolean
        Return passiveMouseMode And mode
    End Function

    Private Sub addInputMode(mode As InputMode_Enum)
        inputMode = inputMode Or mode
    End Sub

    Private Sub removeInputMode(mode As InputMode_Enum)
        inputMode = passiveMouseMode And Not (mode)
    End Sub

    Private Sub clearInputMode()
        inputMode = InputMode_Enum.Normal
    End Sub

    Private Function isInputModeActivated(mode As InputMode_Enum) As Boolean
        Return inputMode And mode
    End Function

    Private Sub ReturnStrokeRenderMode()
        If Not IsNothing(moveStrokeOffsetList) Then
            Dim selStrCnt As Integer = Math.Min(tI.pageData.selection.Count, moveStrokeOffsetList.Count)
            For i = 0 To selStrCnt - 1
                tI.pageData.selection(i).quality = Stroke2D_Quality.High
            Next
        End If
    End Sub

    Private Sub GLScreen_MouseClick(sender As Object, e As MouseEventArgs) Handles GLScreen.MouseClick
        If Drawing2D.dist(mousePos_start, e.Location) < 2 Then
            Dim cursorWorldPos As PointF = tI.mapScreenToWorld(New TabletPoint2D(e.X, e.Y, 0)).toPointF

            Dim rect As New RectangleF(cursorWorldPos, tI.mapScreenSizeToWorldSize(New SizeF(nudEraserSize.Value, nudEraserSize.Value)))

            If My.Computer.Keyboard.ShiftKeyDown = False Then
                tI.pageData.clearSelection()
            End If

            tI.pageData.selectSingleObjectInWorldRegion(rect, True)

            If Drawing2D.isPointWithinRegion(New TabletPoint2D(cursorWorldPos), inkGraphics.tmpText.range) Then
                If isInputModeActivated(InputMode_Enum.Text_Entering) Then
                    inkGraphics.tmpText.HandleMouseClick(Drawing2D.pointFSubtract(cursorWorldPos, inkGraphics.tmpText.Position.toPointF))
                Else
                    textBoxSelected = True
                    inkGraphics.tmpText.Mode.clearModes()
                    inkGraphics.tmpText.Mode.addMode(TextWriterMode.TextWriterMode_Enum.Editing)
                    addInputMode(InputMode_Enum.Text_Entering)
                End If
            Else
                removeInputMode(InputMode_Enum.Text_Entering)
                inkGraphics.tmpText.clearTextSelection()
                textBoxSelected = False
            End If
        End If
    End Sub

    Private Sub GLScreen_MouseDown(sender As Object, e As MouseEventArgs) Handles GLScreen.MouseDown
        mousePos_start = e.Location
        'find cursor position (world coords)
        Dim cursorWorldPos As PointF = tI.mapScreenToWorld(New TabletPoint2D(e.X, e.Y, 0)).toPointF

        If isInputModeActivated(InputMode_Enum.Text_Entering) = True Then
            If Drawing2D.isPointWithinRegion(New TabletPoint2D(cursorWorldPos), inkGraphics.tmpText.range) Then
                inkGraphics.tmpText.HandleMouseDown(Drawing2D.pointFSubtract(cursorWorldPos, inkGraphics.tmpText.Position.toPointF), e)
                Return
            End If
        End If

        If e.Button = Windows.Forms.MouseButtons.Left Then
            'find offset in box (world coords)
            'Dim boxWorldPos As PointF = tI.pageData.tabletViewPoint.Location

            mouse_Pan_Start = cursorWorldPos 'Drawing2D.pointFSubtract(boxWorldPos, cursorWorldPos)

            If isPassiveMouseModeActivated(PassiveMouseMode_Enum.HoveringSelection) AndAlso My.Computer.Keyboard.ShiftKeyDown = False Then
                moveStrokeOffsetList = New List(Of PointF)

                tI.pageData.AtomicDrawingOperation_Begin()
                For Each s In tI.pageData.selection
                    s.quality = Stroke2D_Quality.Low
                    moveStrokeOffsetList.Add(Drawing2D.pointFSubtract(cursorWorldPos, s.range.Location))
                    tI.pageData.MoveStroke(s, s.range.Location)
                Next
                addActiveMouseMode(ActiveMouseMode_Enum.DragingSelection)
                Redraw()
            ElseIf activeMouseMode = ActiveMouseMode_Enum.None AndAlso Drawing2D.isPointWithinRegion(New TabletPoint2D(cursorWorldPos.X, cursorWorldPos.Y, 1024), tI.pageData.tabletViewPoint) = True AndAlso My.Computer.Keyboard.CtrlKeyDown = True Then
                activeMouseMode = ActiveMouseMode_Enum.DraggingTabletRegion
                mouse_Pan_Start = Drawing2D.pointFSubtract(tI.pageData.tabletViewPoint.Location, cursorWorldPos)
            Else
                inkGraphics.selectRect = New RectangleF(mouse_Pan_Start, New SizeF(0, 0))

                backupSelection = createSelectionCopy(tI.pageData.selection)

                addActiveMouseMode(ActiveMouseMode_Enum.Selecting)

                Redraw()
            End If
        ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
            mouse_Pan_Start = tI.pageData.userViewPoint.Location
            addActiveMouseMode(ActiveMouseMode_Enum.Panning)
        End If
    End Sub

    Private Function createSelectionCopy(sourceList As StrokeList) As StrokeList
        Dim retLst As New StrokeList
        For Each s In sourceList
            retLst.Add(s)
        Next

        Return retLst
    End Function

    Private Sub updateSelectionListBasedOnSelectRect()
        'clear all objects in selection
        tI.pageData.clearSelection()

        'add backup list items (ie items before drag action commenced)
        tI.pageData.selectObjectsInList(backupSelection)

        'normalise selectRect
        inkGraphics.selectRect = Drawing2D.normaliseRectf(inkGraphics.selectRect)

        'add new items
        tI.pageData.selectObjectsInWorldRegion(inkGraphics.selectRect)
    End Sub

    Private Sub GLScreen_MouseMove(sender As Object, e As MouseEventArgs) Handles GLScreen.MouseMove
        'find cursor position (world coords)
        Dim cursorWorldPos As PointF = tI.mapScreenToWorld(New TabletPoint2D(e.X, e.Y, 0)).toPointF

        If isActiveMouseModeActivated(ActiveMouseMode_Enum.Selecting) = True Then

            'move box to cursor pos + box offset
            inkGraphics.selectRect = New RectangleF(mouse_Pan_Start, New SizeF(cursorWorldPos.X - mouse_Pan_Start.X, cursorWorldPos.Y - mouse_Pan_Start.Y))

            updateSelectionListBasedOnSelectRect()
        ElseIf isActiveMouseModeActivated(ActiveMouseMode_Enum.DragingSelection) = True Then
            'find new offset
            'if some specific point was at x1, y1 last time this was called and the mouse has moved dx, dy from that point (world space)
            'then now we want to be at x1+dx,y1+dy

            'tricky bit is finding out how far the mouse has moved...

            'where is the point?

            'where is the mouse? - cursorWorldPos
            'how far from the point was the mouse originally. - moveStrokeOffsetList(0)
            'what do i need to add/subtract from the point to make it the same distance from the mouse?
            Dim selStrCnt As Integer = Math.Min(tI.pageData.selection.Count, moveStrokeOffsetList.Count)
            For i = 0 To selStrCnt - 1
                Drawing2D.setStrokeLocation(tI.pageData.selection(i), Drawing2D.pointFSubtract(cursorWorldPos, moveStrokeOffsetList(i)))
            Next

            Redraw()

            'move selection to cursor pos + box offset
            'tI.pageData.tabletViewPoint.Location = Drawing2D.pointFAdd(viewpoint_Pan_start, cursorWorldPos)
        ElseIf isActiveMouseModeActivated(ActiveMouseMode_Enum.DraggingTabletRegion) Then
            'this code doesn't ever get called at the moment, just here for completeness..
            'move box to cursor pos + box offset
            tI.pageData.tabletViewPoint.Location = Drawing2D.pointFAdd(mouse_Pan_Start, cursorWorldPos)
            Redraw()
        ElseIf isActiveMouseModeActivated(ActiveMouseMode_Enum.Panning) = True Then
            Dim posDelta As PointF
            posDelta.X = e.Location.X - mousePos_start.X
            posDelta.Y = e.Location.Y - mousePos_start.Y

            'map the distance moved into world coordinates.
            Dim iX, iY As Double
            iX = posDelta.X / (GLScreen.Width / tI.pageData.userViewPoint.Width)
            iY = posDelta.Y / (GLScreen.Height / tI.pageData.userViewPoint.Height)

            tI.pageData.userViewPoint.X = mouse_Pan_Start.X - iX
            tI.pageData.userViewPoint.Y = mouse_Pan_Start.Y - iY

            'if tabletViewPoint is outside of userViewPopint, move it within view
            tI.pageData.FitTabViewInUserView()
            Redraw()
        Else
            Dim selBox As New RectangleF(cursorWorldPos, tI.mapScreenSizeToWorldSize(New SizeF(10, 10)))


            If isInputModeActivated(InputMode_Enum.Text_Entering) = True Then
                If Drawing2D.isPointWithinRegion(New TabletPoint2D(cursorWorldPos), inkGraphics.tmpText.range) Then
                    'GLScreen.Cursor = Windows.Forms.Cursors.SizeAll
                    GLScreen.Cursor = Windows.Forms.Cursors.IBeam
                    addPassiveMouseMode(PassiveMouseMode_Enum.HoveringSelection)
                    inkGraphics.tmpText.HandleMouseMove(Drawing2D.pointFSubtract(cursorWorldPos, inkGraphics.tmpText.Position.toPointF), e)
                Else
                    GLScreen.Cursor = Windows.Forms.Cursors.Arrow
                    removePassiveMouseMode(PassiveMouseMode_Enum.HoveringSelection)
                End If
            Else
                If Drawing2D.isPointWithinRegion(New TabletPoint2D(cursorWorldPos), inkGraphics.tmpText.range) Then
                    inkGraphics.tmpText.Mode.clearModes()
                    inkGraphics.tmpText.Mode.addMode(TextWriterMode.TextWriterMode_Enum.Mouse_Over)
                    Redraw()
                Else
                    inkGraphics.tmpText.Mode.clearModes()
                    Redraw()
                End If

                'choose appropriate cursor
                If Drawing2D.isPointOnStrokeInListWithinRegion(tI.pageData.selection, selBox) = True AndAlso My.Computer.Keyboard.ShiftKeyDown = False Then
                    GLScreen.Cursor = Windows.Forms.Cursors.SizeAll
                    addPassiveMouseMode(PassiveMouseMode_Enum.HoveringSelection)
                Else
                    GLScreen.Cursor = Windows.Forms.Cursors.Arrow
                    removePassiveMouseMode(PassiveMouseMode_Enum.HoveringSelection)
                End If

            End If
        End If

    End Sub

    Private Sub GLScreen_MouseUp(sender As Object, e As MouseEventArgs) Handles GLScreen.MouseUp
        'find cursor position (world coords)
        Dim cursorWorldPos As PointF = tI.mapScreenToWorld(New TabletPoint2D(e.X, e.Y, 0)).toPointF

        If isInputModeActivated(InputMode_Enum.Text_Entering) = True Then
            inkGraphics.tmpText.HandleMouseUp(Drawing2D.pointFSubtract(cursorWorldPos, inkGraphics.tmpText.Position.toPointF), e)
        End If

        If isActiveMouseModeActivated(ActiveMouseMode_Enum.DragingSelection) Then
            If moveStrokeOffsetList.Count = 0 Or cursorWorldPos = mouse_Pan_Start Then
                tI.pageData.AtomicDrawingOperation_Cancel()
            Else
                Dim name As String = "Move " & moveStrokeOffsetList.Count & " Object" & IIf(moveStrokeOffsetList.Count > 1, "s", "")
                tI.pageData.AtomicDrawingOperation_End(name)
                tI.UndoList_Update()
            End If
        End If
        inkGraphics.selectRect = Nothing
        ReturnStrokeRenderMode()
        clearActiveMouseMode()
        Redraw()
    End Sub

    Private Sub GLScreen_MouseWheel(sender As Object, e As MouseEventArgs) Handles GLScreen.MouseWheel
        Dim n As Single = 1 - e.Delta * 0.001

        Dim moveRegion As RectangleF = tI.pageData.userViewPoint

        If My.Computer.Keyboard.CtrlKeyDown = True Then
            moveRegion = tI.pageData.tabletViewPoint
        ElseIf activeMouseMode = ActiveMouseMode_Enum.None Then
            moveRegion = tI.pageData.userViewPoint
        Else
            Exit Sub
        End If

        'zoom on centre of viewpoint
        'Dim cp As New PointF 'centerpoint of current piewpoint
        'cp.X = moveRegion.X + moveRegion.Width / 2
        'cp.Y = moveRegion.Y + moveRegion.Height / 2

        'zoom on pointer location
        'map the distance moved into world coordinates.
        Dim cp As New PointF 'centerpoint of current piewpoint
        cp.X = moveRegion.X + e.X / (GLScreen.Width / moveRegion.Width)
        cp.Y = moveRegion.Y + e.Y / (GLScreen.Height / moveRegion.Height)

        Dim newPos As New PointF 'top left corner of new viewpoint
        Dim newSize As New SizeF 'size of new viewpoint

        'for centre viewpoint zoom
        'newPos.X = cp.X - moveRegion.Width / 2 * n
        'newPos.Y = cp.Y - moveRegion.Height / 2 * n

        'for pointer location zoom
        newPos.X = (e.X * moveRegion.Width / GLScreen.Width) * (1 - n) + moveRegion.X
        newPos.Y = (e.Y * moveRegion.Height / GLScreen.Height) * (1 - n) + moveRegion.Y

        newSize.Width = moveRegion.Width * n
        newSize.Height = moveRegion.Height * n

        moveRegion = New RectangleF(newPos, newSize)

        If My.Computer.Keyboard.CtrlKeyDown = True Then
            tI.pageData.tabletViewPoint = moveRegion
            If n <> 0 Then
                chkLockTabSize.CheckState = False
                tI.pageData.lockTabletToUserView = False
            End If
        ElseIf activeMouseMode = ActiveMouseMode_Enum.None Then
            tI.pageData.userViewPoint = moveRegion
        End If

        tI.pageData.FitTabViewInUserView()

        Redraw()
    End Sub

    Private Sub GLScreen_Paint(sender As Object, e As PaintEventArgs) Handles GLScreen.Paint
        inkGraphics.DrawAll()
        'GLScreen.SwapBuffers()
    End Sub

    Public tmpStroke As New Stroke2D()
    'Public txt1 As MyTextWriter

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'mouseStealer.Show()

        'virtualViewpoint = New Rectangle(New Point(0, 0), GLScreen.Size)
        GLScreen = New customGLControl()
        GLScreen.Location = GlControl1.Location
        GLScreen.Size = GlControl1.Size
        GLScreen.Anchor = GlControl1.Anchor
        Me.Controls.Add(GLScreen)
        Me.Controls.Remove(GlControl1)

        Tab = New HWTablet(GLScreen)
        Tab.CapturePointer(True)
        tI = New TabletInterpreter(Tab)

        tI.pageData.userViewPoint = New RectangleF(New Point(0, 0), GLScreen.Size)
        tI.pageData.tabletViewPoint = New RectangleF(New Point(0, 0), New SizeF(GLScreen.Width, GLScreen.Height / tI.pageData.tabletAspectRatio))
        tI.pageData.cursor = New TabletPoint2D(0, 0, 0)

        inkGraphics = New InkGraphics(tI, GLScreen)

        tmpStroke.Add(New TabletPoint2D(10, 10, 1024))
        tmpStroke.Add(New TabletPoint2D(110, 110, 1024))
        Drawing2D.setStrokeLocation(tmpStroke, New PointF(0, 0))
        'tmpStroke.MoveStroke(New PointF(-10, -10))

        'txt1 = New MyTextWriter(GLScreen.Size, New Size(300, 100))
        'txt1.AddLine("Hello World", New PointF(0, 0), Brushes.Orange)


        tI.pageData.AddStrokes(tmpStroke)

        strokeInterpreter = New StrokeInterpreter()

        Redraw()

    End Sub

    Public Sub Redraw(Optional force As Boolean = False)
        If force = True Then
            GLScreen.Invalidate()
            requestRedraw = False
        Else
            requestRedraw = True
        End If
    End Sub

    Private Sub tmrRedraw_Tick(sender As Object, e As EventArgs) Handles tmrRedraw.Tick
        If requestRedraw Then
            Redraw(True)
        End If
    End Sub

    Private Sub tI_RequestRedraw() Handles tI.RequestRedraw
        Redraw()
    End Sub

    Private Sub ti_DrawComplete() Handles tI.DrawComplete
        UpdateTodoList()
        'txtStrokeInterpret.Text = strokeInterpreter.interpret(tI.pageData.strokes)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        If tI.pageData.userViewPoint.X = 0 Then
            tI.pageData.userViewPoint = New Rectangle(New Point(10, 10), New Size(tI.pageData.userViewPoint.Width * 2, tI.pageData.userViewPoint.Height * 2))
        Else
            tI.pageData.userViewPoint = New Rectangle(New Point(0, 0), New Size(tI.pageData.userViewPoint.Width / 2, tI.pageData.userViewPoint.Height / 2))
        End If
        Redraw()
    End Sub

    Private Sub GLScreen_Resize(sender As Object, e As EventArgs) Handles GLScreen.Resize
        GL.Viewport(GLScreen.ClientRectangle)
        Redraw()
    End Sub

    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click
        tI.pageData.saveToFile("E:\myInkSave1.mink")
        GLScreen.Focus()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        tI.pageData = New TabletPageData("E:\myInkSave1.mink", tI)
        tI.pageData.userViewPoint = New RectangleF(New Point(0, 0), GLScreen.Size)
        tI.pageData.tabletViewPoint = New RectangleF(New Point(0, 0), New SizeF(GLScreen.Width, GLScreen.Height / tI.TabletAspectRatio))
        tI.pageData.cursor = New TabletPoint2D(0, 0, 0)
        tI.pageData.tabletAspectRatio = tI.TabletAspectRatio
        Redraw()
        GLScreen.Focus()
    End Sub

    Private Sub UpdateTodoList()
        Dim lst As List(Of String) = tI.pageData.undoQueue.GetUndoNames()

        lbUndoList.BeginUpdate()
        lbUndoList.Items.Clear()
        lbUndoList.Items.AddRange(lst.ToArray)
        lbUndoList.EndUpdate()
    End Sub

    Private Sub nudEraserSize_ValueChanged(sender As Object, e As EventArgs) Handles nudEraserSize.ValueChanged
        If IsNothing(inkGraphics) OrElse inkGraphics.loaded = False Then Exit Sub
        tI.eraserSize = nudEraserSize.Value
    End Sub

    Private Sub cbEraserType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbEraserType.SelectedIndexChanged
        If IsNothing(inkGraphics) OrElse inkGraphics.loaded = False Then Exit Sub
        Select Case cbEraserType.SelectedItem
            Case "Stroke"
                tI.eraserType = EraserType.StrokeEraser
            Case "Point"
                tI.eraserType = EraserType.PointEraser
            Case Else
                cbEraserType.SelectedValue = "Stroke"
                tI.eraserType = EraserType.StrokeEraser
        End Select
    End Sub

    Private Sub chkLockTabSize_CheckedChanged(sender As Object, e As EventArgs) Handles chkLockTabSize.CheckedChanged
        tI.pageData.lockTabletToUserView = chkLockTabSize.CheckState
        LockTabletSizeToolStripMenuItem.CheckState = chkLockTabSize.CheckState
    End Sub

    Private Sub TabletHardwareToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TabletHardwareToolStripMenuItem.Click
        tI.pageData.setTabletAspectRatioToHardwareRatio()
    End Sub

    Private Sub ScreenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ScreenToolStripMenuItem.Click
        tI.pageData.setTabletAspectRatioToScreenRatio()
    End Sub

    Private Sub LockTabletSizeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LockTabletSizeToolStripMenuItem.Click
        chkLockTabSize.CheckState = LockTabletSizeToolStripMenuItem.CheckState
        tI.pageData.lockTabletToUserView = LockTabletSizeToolStripMenuItem.CheckState
    End Sub

    Private Sub GLScreen_Load(sender As Object, e As EventArgs) Handles GLScreen.Load

    End Sub

    Private Sub txtTest_TextChanged(sender As Object, e As EventArgs) Handles txtTest.TextChanged
        inkGraphics.tmpText.Text = txtTest.Text
        Redraw()
    End Sub

    Private Sub btnRecogniseSelection_Click(sender As Object, e As EventArgs) Handles btnRecogniseSelection.Click
        Dim allRecognizers As New Recognizers
        Dim theRecognizer As Recognizer = allRecognizers.GetDefaultRecognizer()
        'Dim context As RecognizerContext = theRecognizer.CreateRecognizerContext()

        Dim resultStr As String = ""

        Dim myInkCollector As New InkCollector()

        myInkCollector.Enabled = False

        tI.pageData.selection.addToMicrosoftInk(myInkCollector.Ink)

        'context.Strokes = myInkCollector.Ink.Strokes


        'Dim status As RecognitionStatus
        'Dim result As RecognitionResult = context.Recognize(status)

        'If Not (result Is Nothing) Then
        '    If status = RecognitionStatus.NoError AndAlso Not (result Is Nothing) Then
        '        'display options
        '        Dim alternate As RecognitionAlternate
        '        For Each alternate In result.GetAlternatesFromSelection()
        '            resultStr += alternate.ToString() + vbCrLf
        '        Next
        '        MsgBox(resultStr)
        '    Else
        '        MsgBox("Error in recognition:" + status.ToString())
        '    End If
        'End If



        resultStr = myInkCollector.Ink.Strokes.ToString()

        Try
            'If result.Trim.EndsWith("=") Then
            'result = result.Remove(result.Length - 1, 1)
            Try
                Dim exp As NCalc.Expression = New NCalc.Expression(resultStr)

                MsgBox(resultStr + " = " + exp.Evaluate().ToString)
            Catch ex As Exception
                MsgBox("Could not evaluate'" + resultStr + "'")
            End Try
            'Else
            'MsgBox(result)
            'End If
        Catch ex As Exception
            MsgBox("Could not interpret!")
        End Try





    End Sub

    Private Sub cbColor_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbColor.SelectedIndexChanged
        If Not IsNothing(tI) Then
            Select Case cbColor.Text
                Case "White"
                    tI.penColor = Color.White
                Case "Red"
                    tI.penColor = Color.Red
                Case "Green"
                    tI.penColor = Color.Green
                Case "Blue"
                    tI.penColor = Color.Blue
                Case "Yellow"
                    tI.penColor = Color.Yellow
                Case "Gray"
                    tI.penColor = Color.Gray
                Case "Cyan"
                    tI.penColor = Color.Cyan
                Case "Pink"
                    tI.penColor = Color.Pink
                Case "Orange"
                    tI.penColor = Color.Orange
            End Select
        End If
    End Sub
End Class