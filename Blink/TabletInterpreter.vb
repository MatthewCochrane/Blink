Public Enum EraserType
    StrokeEraser
    PointEraser
End Enum

Public Class TabletInterpreter
    Private WithEvents Tab As HWTablet

    Public pageData As TabletPageData

    Private currentStrokeStylusID As PenType = PenType.None
    Private currentCursorStylusID As PenType = PenType.None
    Public eraserSize As Double = 10.0
    Public eraserType As EraserType = blInk.EraserType.StrokeEraser

    Public penColor As Color = Color.White

    Public Event RequestRedraw()
    Public Event DrawComplete() 'finish drawing a line -> undo list updated

    Public ReadOnly Property cursorType As PenType
        Get
            Return currentCursorStylusID
        End Get
    End Property

    Public Sub New(_Tab As HWTablet)
        Tab = _Tab
        pageData = New TabletPageData(Me)
        pageData.tabletAspectRatio = TabletAspectRatio
    End Sub

    Public ReadOnly Property TabletAspectRatio As Double
        Get
            Return Tab.TabletSpace.Width / Tab.TabletSpace.Height
        End Get
    End Property

    Public Sub request_Redraw()
        RaiseEvent RequestRedraw()
    End Sub

    Public Sub UndoList_Update()
        RaiseEvent DrawComplete()
    End Sub

    Private Sub Tab_PenDown(position As TabletPoint2D, buttons As Integer, cursorID As PenType) Handles Tab.PenDown
        'only create a new stroke if no pen is down, or of the pen that is 'apparently' currently down is
        'the same one that the parameter data.Stylus is referring to. (in that second case there was some error
        'and we still want to be able to draw.)
        If currentStrokeStylusID = PenType.None OrElse cursorID = currentStrokeStylusID Then
            Dim modData As TabletPoint2D = mapTabletViewToWorld(position)

            currentStrokeStylusID = cursorID

            If currentStrokeStylusID = PenType.Pen Then
                createStroke(modData)
            ElseIf currentCursorStylusID = PenType.Eraser Then
                eraserDown(modData, eraserType)
                eraserErase(modData, eraserType)
            End If
        End If
    End Sub

    Private Sub Tab_PenUp(position As TabletPoint2D, buttons As Integer, cursorID As PenType) Handles Tab.PenUp
        'Debug.Print("Pen Up")
        If Not currentStrokeStylusID = PenType.None AndAlso currentStrokeStylusID = cursorID Then
            Dim modData As TabletPoint2D = mapTabletViewToWorld(position)

            If currentStrokeStylusID = PenType.Pen Then
                finishStroke(modData)
                RaiseEvent DrawComplete()
            ElseIf currentCursorStylusID = PenType.Eraser Then
                eraserErase(modData, eraserType)
                eraserUp(modData, eraserType)
                RaiseEvent DrawComplete()
            End If

            currentStrokeStylusID = PenType.None

            RaiseEvent RequestRedraw()
        End If
    End Sub

    Private Sub Tab_PenTap(position As TabletPoint2D, cursorID As PenType) Handles Tab.PenTap
        'Debug.Print("Pen Tap")
    End Sub

    Private Sub Tab_PenLongTap(position As TabletPoint2D, cursorID As PenType) Handles Tab.PenLongTap
        'Debug.Print("Pen Long Tap")
    End Sub

    Private Sub Tab_ButtonDown(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType) Handles Tab.ButtonDown
        'Debug.Print("Button " & eventButton & " Down")
    End Sub

    Private Sub Tab_ButtonUp(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType) Handles Tab.ButtonUp
        'Debug.Print("Button " & eventButton & " Up")
    End Sub

    Private Sub Tab_ButtonClick(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType) Handles Tab.ButtonClick
        'Debug.Print("Button " & eventButton & " Click")
    End Sub

    Private Sub Tab_ButtonLongClick(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType) Handles Tab.ButtonLongClick
        'Debug.Print("Button " & eventButton & " Long Click")
    End Sub

    Private Sub Tab_PenLeftProximity() Handles Tab.PenLeftProximity
        currentCursorStylusID = PenType.None
    End Sub

    Private Sub Tab_Draw(position As TabletPoint2D, buttons As Integer, cursorID As PenType) Handles Tab.Draw
        'Debug.Print("Draw X: " & position.x & " Y: " & position.y & " Pressure: " & position.pressure)
        If Not currentStrokeStylusID = PenType.None AndAlso currentStrokeStylusID = cursorID Then
            Dim modData As TabletPoint2D = mapTabletViewToWorld(position)

            If currentStrokeStylusID = PenType.Pen Then
                addToStroke(modData)
            ElseIf currentCursorStylusID = PenType.Eraser Then
                eraserErase(modData, eraserType)
            End If

            pageData.cursor = modData

            RaiseEvent RequestRedraw()
        End If
    End Sub

    Private Sub Tab_Hover(position As TabletPoint2D, buttons As Integer, zDist As Integer, cursorID As PenType) Handles Tab.Hover
        'Debug.Print("Hover X: " & position.x & " Y: " & position.y & " Z: " & zDist)
        Dim modData As TabletPoint2D = mapTabletViewToWorld(position)

        If currentCursorStylusID = PenType.None OrElse currentCursorStylusID <> cursorID Then
            'Stylus has changed
            currentCursorStylusID = cursorID
            'RaiseEvent InputDeviceChanged(currentCursorStylus, currentCursorTabletProps)
        Else
            'Stylus has not changed
        End If

        'If currentStrokeStylusID = PenType.None Then
        pageData.cursor = modData
        'attachedControl.Invalidate()
        RaiseEvent RequestRedraw()
        'End If
    End Sub

    Public Function mapScreenToWorld(ScreenPoint As TabletPoint2D) As TabletPoint2D
        Return Drawing2D.mapPointInSpaceToPointInRegion(ScreenPoint, Tab.ScreenSpace, pageData.userViewPoint)
    End Function

    Public Function mapWorldToScreen(WorldPoint As TabletPoint2D) As TabletPoint2D
        Return Drawing2D.mapPointInRegionToPointInSpace(WorldPoint, Tab.ScreenSpace, pageData.userViewPoint)
    End Function

    Public Function mapWorldSizeToScreenSize(WorldSize As SizeF) As SizeF
        Dim p As PointF = Drawing2D.mapPointToSpace(WorldSize.ToPointF, pageData.userViewPoint.Size, Tab.ScreenSpace)
        Return New SizeF(p.X, p.Y)
    End Function

    Public Function mapScreenSizeToWorldSize(ScreenSize As SizeF) As SizeF
        Dim p As PointF = Drawing2D.mapPointToSpace(ScreenSize.ToPointF, Tab.ScreenSpace, pageData.userViewPoint.Size)
        Return New SizeF(p.X, p.Y)
    End Function

    Public Function mapTabletViewToWorld(ScreenPoint As TabletPoint2D) As TabletPoint2D
        Return Drawing2D.mapPointInSpaceToPointInRegion(ScreenPoint, Tab.ScreenSpace, pageData.tabletViewPoint)
    End Function

    Public Function mapWorldToTabletView(WorldPoint As TabletPoint2D) As TabletPoint2D
        Return Drawing2D.mapPointInRegionToPointInSpace(WorldPoint, Tab.ScreenSpace, pageData.tabletViewPoint)
    End Function

    Public Function mapWorldSizeToTabletViewSize(WorldSize As SizeF) As SizeF
        Dim p As PointF = Drawing2D.mapPointToSpace(WorldSize.ToPointF, pageData.tabletViewPoint.Size, Tab.ScreenSpace)
        Return New SizeF(p.X, p.Y)
    End Function

    Public Function mapTabletViewSizeToWorldSize(WorldSize As SizeF) As SizeF
        Dim p As PointF = Drawing2D.mapPointToSpace(WorldSize.ToPointF, Tab.ScreenSpace, pageData.tabletViewPoint.Size)
        Return New SizeF(p.X, p.Y)
    End Function

    Private Function createStroke(modData As TabletPoint2D) As Stroke2D
        'creating a new stroke
        pageData.strokes.Add(New Stroke2D)
        pageData.strokes.Last.color = penColor

        pageData.strokes.Last.Add(modData)

        Return pageData.strokes.Last
    End Function

    Private Sub finishStroke(modData As TabletPoint2D)
        pageData.strokes.Last.Add(modData)

        If Drawing2D.isStrokeClosed(pageData.strokes.Last) Then
            Debug.Print("Stroke is closed polygon!")
            Dim tmpStroke As Stroke2D = StrokeInterpreter.strokeToShape(pageData.strokes.Last)
            'Drawing2D.rectangleToStroke(Drawing2D.findStrokeRange(pageData.strokes.Last))
            pageData.strokes.Remove(pageData.strokes.Last)
            pageData.strokes.Add(tmpStroke)
        End If

        'allow to be undone!
        pageData.undoQueue.AddStrokes(pageData.strokes.Last)
    End Sub

    Private Sub addToStroke(modData As TabletPoint2D)
        pageData.strokes.Last.Add(modData)
    End Sub

    Private Sub eraserDown(modData As TabletPoint2D, eraseType As EraserType)
        pageData.AtomicDrawingOperation_Begin()
    End Sub

    Private Sub eraserUp(modData As TabletPoint2D, eraseType As EraserType)
        Dim opName As String
        If eraseType = EraserType.PointEraser Then : opName = "Erase Points"
        ElseIf eraseType = EraserType.StrokeEraser Then : opName = "Erase Strokes"
        Else : opName = "Unknown Operation"
        End If
        pageData.AtomicDrawingOperation_End(opName)
    End Sub

    Private Sub eraserErase(modData As TabletPoint2D, eraseType As EraserType)
        'TODO[ ]: Split into two functions, one for point eraser and one for stroke eraser!
        'create a box around the cursor
        Dim size As SizeF = mapScreenSizeToWorldSize(New SizeF(eraserSize, eraserSize))
        Dim rect As New RectangleF(modData.toPointF, size)
        Dim deleteList As New List(Of Stroke2D)

        'eraseType = EraserType.StrokeEraser
        If eraseType = EraserType.StrokeEraser Then
            Dim lines As Integer = pageData.strokes.Count
            For i = 0 To lines - 1
                'see if any points from any lines lie within that box
                If Drawing2D.isPointOnStrokeWithinRegion(pageData.strokes(i), rect) = True Then
                    deleteList.Add(pageData.strokes(i))
                End If
            Next
            pageData.DeleteStrokes(deleteList)
        ElseIf eraseType = EraserType.PointEraser Then
            'See if any points from any lines lie within that box
            Dim lines As Integer = pageData.strokes.Count
            Dim newLines As List(Of Stroke2D) = Nothing
            Dim addList As New List(Of Stroke2D)
            For i = 0 To lines - 1
                If pageData.strokes(i).Count > 1 Then
                    If Drawing2D.deletePointsOnStrokeWithinRegion(pageData.strokes(i), rect, newLines) = True Then
                        'add to undo list -> old strokes, new strokes
                        'deleteList.Add(pageData.strokes(i)) (
                        If newLines.Count > 0 Then
                            'add to strokes list
                            addList.AddRange(newLines.GetRange(0, newLines.Count))
                        End If
                        'if the stroke was split then it was replaced by 
                        'one or two new strokes.  We need to delete the original stroke
                        deleteList.Add(pageData.strokes(i))
                    End If
                Else
                    deleteList.Add(pageData.strokes(i))
                End If
            Next
            pageData.DeleteStrokes(deleteList)
            pageData.AddStrokes(addList)
        Else
            'do nothing...
        End If
    End Sub

End Class
