Public Class TabletPageData
    Public strokes As New StrokeList

    Public WithEvents textFields As List(Of TKTextWriter)

    Public cursor As TabletPoint2D
    Public userViewPoint As RectangleF
    Public tabletViewPoint As RectangleF
    Public tabletAspectRatio As Double
    Public cursorType As StylusCursorState
    Public undoQueue As UndoQueue

    Public selection As New StrokeList

    Private tI As TabletInterpreter
    Private _lockTabletToView As Boolean

    Public Sub clearSelection()
        Dim s As Stroke2D

        For Each s In selection
            s.isSelected = False
        Next

        selection.Clear()
    End Sub

    Public Sub addToSelection(ByRef stroke As Stroke2D)
        stroke.isSelected = True
        selection.Add(stroke)
    End Sub

    Public Sub removeFromSelection(ByRef stroke As Stroke2D)
        stroke.isSelected = False
        selection.Remove(stroke)
    End Sub

    Public Sub selectObjectsInWorldRegion(worldRegion As RectangleF)

        Dim lines As Integer = strokes.Count
        For i = 0 To lines - 1
            'see if any points from any lines lie within that box
            If Drawing2D.isPointOnStrokeWithinRegion(strokes(i), worldRegion) = True Then
                If isStrokeInList(strokes(i), selection) = False Then
                    addToSelection(strokes(i))
                End If
            End If
        Next

        tI.request_Redraw()
    End Sub


    Public Sub selectSingleObjectInWorldRegion(worldRegion As RectangleF, Optional toggle As Boolean = False)

        Dim lines As Integer = strokes.Count
        For i = 0 To lines - 1
            'see if any points from any lines lie within that box
            If Drawing2D.isPointOnStrokeWithinRegion(strokes(i), worldRegion) = True Then
                If isStrokeInList(strokes(i), selection) = False Then
                    addToSelection(strokes(i))
                    Exit For
                Else
                    If toggle = True Then
                        removeFromSelection(strokes(i))
                        Exit For
                    End If
                End If
            End If
        Next

        tI.request_Redraw()
    End Sub

    Private Function isStrokeInList(s As Stroke2D, lst As StrokeList) As Boolean
        For Each st In lst
            If Object.ReferenceEquals(s, st) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Sub selectObjectsInList(lst As StrokeList)

        For Each s In lst
            If isStrokeInList(s, selection) = False Then
                addToSelection(s)
            End If
        Next

        tI.request_Redraw()
    End Sub

    Public Sub setTabletAspectRatioToHardwareRatio()
        tabletAspectRatio = tI.TabletAspectRatio
        tabletViewPoint = Drawing2D.extendRegionToMatchAspectRatio(tabletViewPoint, tabletAspectRatio)
        FitTabViewInUserView()
    End Sub

    Public Sub setTabletAspectRatioToScreenRatio()
        tabletAspectRatio = userViewPoint.Width / userViewPoint.Height
        tabletViewPoint = Drawing2D.extendRegionToMatchAspectRatio(tabletViewPoint, tabletAspectRatio)
        FitTabViewInUserView()
    End Sub

    Public Property lockTabletToUserView() As Boolean
        Get
            Return _lockTabletToView
        End Get
        Set(value As Boolean)
            If _lockTabletToView = False AndAlso value = True Then
                _lockTabletToView = True
                FitTabViewInUserView()
            ElseIf value = False Then
                _lockTabletToView = False
            End If
        End Set
    End Property

    Public Sub New(_ti As TabletInterpreter)
        tI = _ti
        undoQueue = New UndoQueue(Me)
    End Sub

    Public Sub New(fileName As String, _ti As TabletInterpreter)
        tI = _ti
        Dim bytes() As Byte = System.IO.File.ReadAllBytes(fileName)
        Me.strokes = StrokeList.fromBinary(bytes)
        undoQueue = New UndoQueue(Me)
    End Sub

    Public Sub FitTabViewInUserView()
        If _lockTabletToView = True Then
            tabletViewPoint = New RectangleF(tabletViewPoint.Location, Drawing2D.GetMaxSizeInRegionWithAspectRatio(userViewPoint.Size, tabletAspectRatio))
        Else
            'first check width and height, if bigger, restrict
            If tabletViewPoint.Width > userViewPoint.Width Then
                'adjust height
                tabletViewPoint.Width = userViewPoint.Width

                'scale height
                tabletViewPoint.Height = tabletViewPoint.Width / tabletAspectRatio
            End If

            'check height
            If tabletViewPoint.Height > userViewPoint.Height Then
                'adjust height
                tabletViewPoint.Height = userViewPoint.Height

                'scale height
                tabletViewPoint.Width = tabletViewPoint.Height * tabletAspectRatio
            End If
        End If

        'next, if it's too far to the left/top, align left
        If tabletViewPoint.Left < userViewPoint.Left Then tabletViewPoint.X = userViewPoint.X
        If tabletViewPoint.Top < userViewPoint.Top Then tabletViewPoint.Y = userViewPoint.Y

        'then check right and bottom
        If tabletViewPoint.Right > userViewPoint.Right Then tabletViewPoint.X = userViewPoint.Right - tabletViewPoint.Width
        If tabletViewPoint.Bottom > userViewPoint.Bottom Then tabletViewPoint.Y = userViewPoint.Bottom - tabletViewPoint.Height

        tI.request_Redraw()
    End Sub

    Public Sub AtomicDrawingOperation_Begin()
        undoQueue.AtomicDrawingOperation_Begin()
    End Sub

    Public Sub AtomicDrawingOperation_End(OperationName As String)
        undoQueue.AtomicDrawingOperation_End(OperationName)
        tI.UndoList_Update()
    End Sub

    Public Sub AtomicDrawingOperation_Cancel()
        undoQueue.AtomicDrawingOperation_Cancel()
    End Sub

    'sets the upper left position of the strokes bounding box
    Public Sub MoveStroke(stroke As Stroke2D, newLocation As PointF)
        Dim origStroke As Stroke2D = stroke.Clone()
        Drawing2D.setStrokeLocation(stroke, newLocation)
        undoQueue.ModifyStrokes(origStroke, stroke)
    End Sub

    Public Sub AddStrokes(stroke As Stroke2D)
        'call this whenever the user adds a single stroke in the program
        AddStrokes(New List(Of Stroke2D) From {stroke})
    End Sub

    Public Sub AddStrokes(_strokes As List(Of Stroke2D))
        'call this whenever the user adds multiple strokes in one operation in the program
        undoQueue.AddStrokes(_strokes)
        strokes.AddRange(_strokes)
    End Sub

    Public Sub DeleteStrokes(stroke As Stroke2D)
        'call this whenever the user deletes a single stroke in the program
        DeleteStrokes(New List(Of Stroke2D) From {stroke})
    End Sub

    Public Sub DeleteStrokes(_strokes As List(Of Stroke2D))
        'call this whenever the user deletes multiple strokes in one operation in the program
        undoQueue.DeleteStrokes(_strokes)

        Dim s As Stroke2D
        For Each s In _strokes
            strokes.Remove(s)
        Next
    End Sub

    Public Sub saveToFile(fileName As String)
        Dim bytes() As Byte = strokes.toBinary()
        System.IO.File.WriteAllBytes(fileName, bytes)
    End Sub

End Class

