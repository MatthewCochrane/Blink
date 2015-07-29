Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL
Imports Microsoft.Ink

Public Class Drawing2D
    Public Shared Function isStrokeClosed(stroke As Stroke2D) As Boolean
        'Finds shape bounding box
        Dim strokeDomain As RectangleF = findStrokeRange(stroke)

        'auto close radius
        'set to 5% of the largest dimension
        Dim AutoCloseRadius As Double = Math.Max(strokeDomain.Width, strokeDomain.Height) * 0.05

        If dist(stroke.Last, stroke.First) < AutoCloseRadius Then
            Return True
        Else
            Return False
        End If
    End Function

    'finds the angle of a vector in the direction of point1 to point 2
    Public Shared Function angle(p1 As PointF, p2 As PointF) As Double
        Dim deltaX, deltaY, ang As Double
        deltaX = p2.X - p1.X
        deltaY = p2.Y - p1.Y
        ang = Math.Atan2(deltaY, deltaX)
        Return ang
    End Function

    Public Shared Function angle(p1 As TabletPoint2D, p2 As TabletPoint2D) As Double
        Return dist(New PointF(p1.x, p1.y), New PointF(p2.x, p2.y))
    End Function

    Public Shared Function dist(p1 As PointF, p2 As PointF) As Double
        Return ((p2.X - p1.X) ^ 2 + (p2.Y - p1.Y) ^ 2) ^ 0.5
    End Function

    Public Shared Function dist(p1 As TabletPoint2D, p2 As TabletPoint2D) As Double
        Return dist(New PointF(p1.x, p1.y), New PointF(p2.x, p2.y))
    End Function

    Public Shared Function findStrokeRange(stroke As Stroke2D) As RectangleF
        Dim xmin, xmax, ymin, ymax As Double
        xmin = Double.PositiveInfinity
        xmax = Double.NegativeInfinity
        ymin = Double.PositiveInfinity
        ymax = Double.NegativeInfinity

        Dim max As Integer = stroke.Count

        For i As Integer = 0 To max - 1
            Dim p As TabletPoint2D = stroke(i)
            If p.x > xmax Then xmax = p.x
            If p.x < xmin Then xmin = p.x
            If p.y > ymax Then ymax = p.y
            If p.y < ymin Then ymin = p.y
        Next

        Return New Rectangle(xmin, ymin, xmax - xmin, ymax - ymin)

    End Function

    Public Shared Function pointFSubtract(p1 As PointF, p2 As PointF) As PointF
        Return New PointF(p1.X - p2.X, p1.Y - p2.Y)
    End Function

    Public Shared Function pointFAdd(p1 As PointF, p2 As PointF) As PointF
        Return New PointF(p1.X + p2.X, p1.Y + p2.Y)
    End Function

    Public Shared Function tabletPoint2DSubtract(p1 As TabletPoint2D, p2 As TabletPoint2D, Optional pressure As Single = -1) As TabletPoint2D
        Return New TabletPoint2D(p1.x - p2.x, p1.y - p2.y, IIf(pressure = -1, p1.pressure, pressure))
    End Function

    Public Shared Function tabletPoint2DAdd(p1 As TabletPoint2D, p2 As TabletPoint2D, Optional pressure As Single = -1) As TabletPoint2D
        Return New TabletPoint2D(p1.x + p2.x, p1.y + p2.y, IIf(pressure = -1, p1.pressure, pressure))
    End Function

    Public Shared Function rectangleToStroke(rectangle As RectangleF, Optional pressure As Single = 1024) As Stroke2D
        Dim retval As New Stroke2D
        retval.Add(New TabletPoint2D(rectangle.Left, rectangle.Top, pressure))
        retval.Add(New TabletPoint2D(rectangle.Left, rectangle.Bottom, pressure))
        retval.Add(New TabletPoint2D(rectangle.Right, rectangle.Bottom, pressure))
        retval.Add(New TabletPoint2D(rectangle.Right, rectangle.Top, pressure))
        retval.Add(New TabletPoint2D(rectangle.Left, rectangle.Top, pressure))
        Return retval
    End Function

    Public Shared Function mapPointToSpace(p1 As PointF, fromSpace As SizeF, toSpace As SizeF) As PointF
        Return New PointF(p1.X / CSng(fromSpace.Width / toSpace.Width), p1.Y / CSng(fromSpace.Height / toSpace.Height))
    End Function

    Public Shared Function mapPointToSpace(p1 As PointF, fromSpaceWidth As Single, fromSpaceHeight As Single, toSpaceWidth As Single, toSpaceHeight As Single) As PointF
        Return MapPointToSpace(p1, New SizeF(fromSpaceWidth, fromSpaceHeight), New SizeF(toSpaceWidth, toSpaceHeight))
        'Return New PointF(p1.X * CSng(fromSpaceWidth / toSpaceWidth), p1.Y * CSng(fromSpaceHeight / toSpaceHeight))
    End Function

    Public Shared Function mapPointToSpace(p1 As Point, fromSpace As Size, toSpace As Size) As PointF
        Return New PointF(p1.X / CSng(fromSpace.Width / toSpace.Width), p1.Y / CSng(fromSpace.Height / toSpace.Height))
    End Function

    Public Shared Function mapPointToSpace(p1 As TabletPoint2D, fromSpace As Size, toSpace As Size) As TabletPoint2D
        Return New TabletPoint2D(p1.x / CDbl(fromSpace.Width / toSpace.Width), p1.y / CDbl(fromSpace.Height / toSpace.Height), p1.pressure)
    End Function

    Public Shared Function mapPointToSpace(p1 As TabletPoint2D, fromSpace As SizeF, toSpace As SizeF) As TabletPoint2D
        Return New TabletPoint2D(p1.x / CDbl(fromSpace.Width / toSpace.Width), p1.y / CDbl(fromSpace.Height / toSpace.Height), p1.pressure)
    End Function

    Public Shared Function invertAxis(p1 As TabletPoint2D, toSpace As SizeF, xAxisInvert As Boolean, yAxisInvert As Boolean) As TabletPoint2D
        Dim x, y As Double
        x = If(xAxisInvert = True, toSpace.Width - p1.x, p1.x)
        y = If(yAxisInvert = True, toSpace.Height - p1.y, p1.y)

        Return New TabletPoint2D(x, y, p1.pressure)
    End Function

    'Public Shared Function mapPointInSpaceToPointInRegion(p_in_space As PointF, fromSpace As SizeF, toRegion As RectangleF) As PointF
    '    Dim x, y As Single
    '    x = toRegion.X + p_in_space.X / CSng(fromSpace.Width / toRegion.Width)
    '    y = toRegion.Y + p_in_space.Y / CSng(fromSpace.Height / toRegion.Height)
    '    Return New PointF(x, y)
    'End Function

    'Public Shared Function mapPointInRegionToPointInSpace(p_in_region As PointF, toSpace As SizeF, fromRegion As RectangleF) As PointF
    '    Dim x, y As Single

    '    x = (p_in_region.X - fromRegion.X) * CSng(toSpace.Width / fromRegion.Width)
    '    y = (p_in_region.Y - fromRegion.Y) * CSng(toSpace.Height / fromRegion.Height)

    '    Return New PointF(x, y)
    'End Function

    Public Shared Function mapPointInSpaceToPointInRegion(p_in_space As TabletPoint2D, fromSpace As SizeF, toRegion As RectangleF) As TabletPoint2D
        Dim x, y As Double

        x = toRegion.X + p_in_space.x / CSng(fromSpace.Width / toRegion.Width)
        y = toRegion.Y + p_in_space.y / CSng(fromSpace.Height / toRegion.Height)

        Return New TabletPoint2D(x, y, p_in_space.pressure)
    End Function

    Public Shared Function mapPointInRegionToPointInSpace(p_in_region As TabletPoint2D, toSpace As SizeF, fromRegion As RectangleF) As TabletPoint2D
        Dim x, y As Double

        x = (p_in_region.X - fromRegion.X) * CSng(toSpace.Width / fromRegion.Width)
        y = (p_in_region.Y - fromRegion.Y) * CSng(toSpace.Height / fromRegion.Height)

        Return New TabletPoint2D(x, y, p_in_region.pressure)
    End Function

    Public Shared Function extendRegionToMatchAspectRatio(region As RectangleF, desiredAspectRatio As Double) As RectangleF
        Dim regionAspectRatio As Double = region.Width / region.Height
        If regionAspectRatio < desiredAspectRatio Then
            'Extend Width
            '                                               (      Width                       , Height        )
            Return New RectangleF(region.Location, New SizeF(region.Height * desiredAspectRatio, region.Height))
        ElseIf regionAspectRatio > desiredAspectRatio Then
            'Extend Height
            '                                               ( Width      ,     Height                       )
            Return New RectangleF(region.Location, New SizeF(region.Width, region.Width / desiredAspectRatio))
        Else
            'do nothing, already the correct aspect ratio
            Return region
        End If

    End Function

    Public Shared Function shrinkRegionToMatchAspectRatio(region As RectangleF, desiredaAspectRatio As Double) As RectangleF

    End Function

    Public Shared Function GetMaxSizeInRegionWithAspectRatio(regionToFitIn As SizeF, aspectRatio As Double) As SizeF
        If regionToFitIn.Width > regionToFitIn.Height Then
            If aspectRatio > 1 Then
                Return New SizeF(regionToFitIn.Width, regionToFitIn.Width / aspectRatio)
            Else
                Return New SizeF(regionToFitIn.Height * aspectRatio, regionToFitIn.Height)
            End If
        Else
            If aspectRatio > 1 Then
                Return New SizeF(regionToFitIn.Height * aspectRatio, regionToFitIn.Height)
            Else
                Return New SizeF(regionToFitIn.Width, regionToFitIn.Width / aspectRatio)
            End If
        End If
    End Function

    Public Shared Function isPointWithinRegion(p As TabletPoint2D, region As RectangleF) As Boolean
        If p.x > region.Left AndAlso p.x < region.Right AndAlso p.y > region.Top AndAlso p.y < region.Bottom Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function valueInRange(value As Double, min As Double, max As Double) As Boolean
        Return (value >= min) And (value <= max)
    End Function

    Public Shared Sub movePoint(ByRef p As TabletPoint2D, x As Double, y As Double)
        p.x += x
        p.y += y
    End Sub

    Public Shared Sub movePoint(ByRef p As TabletPoint2D, offset As PointF)
        p.x += offset.X
        p.y += offset.Y
    End Sub

    Public Shared Function doRegionsOverlap(A As RectangleF, B As RectangleF) As Boolean
        Dim xOverlap As Boolean = valueInRange(A.X, B.X, A.X + B.Width) Or _
                                  valueInRange(B.X, A.X, A.X + A.Width)
        Dim yOverlap As Boolean = valueInRange(A.Y, B.Y, A.Y + B.Height) Or _
                                  valueInRange(B.Y, A.Y, A.Y + B.Height)

        Return xOverlap And yOverlap
    End Function

    Public Shared Function isPointOnStrokeWithinRegion(stroke As Stroke2D, region As RectangleF) As Boolean
        'first check the bounds of the stroke
        'findStrokeDomain(stroke)

        'If doRegionsOverlap(stroke.range, region) Then
        If stroke.range.IntersectsWith(region) = True Then
            Dim strokePointCount As Integer = stroke.Count

            For i = 0 To strokePointCount - 1
                If isPointWithinRegion(stroke(i), region) = True Then
                    Return True
                End If
            Next
        End If

        Return False
    End Function

    Public Shared Function isPointOnStrokeInListWithinRegion(strokes As StrokeList, region As RectangleF) As Boolean
        For Each s In strokes
            If isPointOnStrokeWithinRegion(s, region) = True Then
                Return True
            End If
        Next

        Return False
    End Function

    Public Shared Function deletePointsOnStrokeWithinRegion(stroke As Stroke2D, region As RectangleF, ByRef newStrokes As List(Of Stroke2D)) As Boolean
        Dim i As Integer = -1
        Dim overlap As Stroke2DOverlap
        Dim workingStroke As Stroke2D = stroke
        Dim tmpStrokes() As Stroke2D
        Dim wasModified As Boolean = False
        newStrokes = New List(Of Stroke2D)

        Do
            'Find first overlap in region
            overlap = findFirstRegionOverlap(workingStroke, region)

            If overlap.overlapCount > 0 Then
                'split the line
                tmpStrokes = Stroke2D.splitStroke(workingStroke, overlap.lastIndex)

                'erase points from first line
                tmpStrokes(0).RemoveRange(overlap.firstIndex, overlap.overlapCount)

                'remove all strokes from original stroke.. we want to delete it.
                'no, don't do this, do it externally, we want to keep the points so we can preserve for undo
                'workingStroke.Clear()

                'save the two new lines
                If tmpStrokes(1).Count > 0 Then newStrokes.Add(tmpStrokes(1))
                If tmpStrokes(0).Count > 0 Then newStrokes.Add(tmpStrokes(0))

                'rerun loop with second line as main line
                workingStroke = tmpStrokes(1)

                'set wasModified flag
                wasModified = True
            End If
        Loop While overlap.overlapCount > 0 AndAlso tmpStrokes(1).Count > 0

        If wasModified = True Then
            'loop through and clean up empty strokes
            deleteNullStrokes(newStrokes)
            Return True
        Else
            Return False
        End If
    End Function

    Private Shared Sub deleteNullStrokes(lst As List(Of Stroke2D))
        Dim deleteList As New List(Of Stroke2D)
        Dim s As Stroke2D
        'find the strokes to delete
        For Each s In lst
            If s.Count <= 1 Then
                deleteList.Add(s)
            End If
        Next
        'delete the strokes
        For Each s In deleteList
            lst.Remove(s)
        Next
    End Sub

    Private Class Stroke2DOverlap
        Public firstIndex As Integer
        Public overlapCount As Integer

        Public Sub New(_firstIndex As Integer, _overlapCount As Integer)
            firstIndex = _firstIndex
            overlapCount = _overlapCount
        End Sub

        Public ReadOnly Property lastIndex As Integer
            Get
                Return firstIndex + overlapCount
            End Get
        End Property
    End Class

    Private Shared Function findFirstRegionOverlap(stroke As Stroke2D, region As RectangleF, Optional searchStart As Integer = 0) As Stroke2DOverlap
        Dim i As Integer = searchStart - 1
        Dim regionStartPoint As Integer = stroke.Count
        Dim regionOverlapCount As Integer = 0
        Dim lastPointInRegion As Boolean = False
        Dim currentPointInRegion As Boolean

        For i = searchStart To stroke.Count - 1
            currentPointInRegion = isPointWithinRegion(stroke(i), region)

            If currentPointInRegion = True Then
                If lastPointInRegion = False Then
                    'have gone from not in region to within region
                    'have entered a region
                    regionStartPoint = i
                    regionOverlapCount = 1
                ElseIf lastPointInRegion = True Then
                    'we were in the region last point and we're still in the region
                    'stayed in region
                    regionOverlapCount += 1
                End If
            ElseIf lastPointInRegion = True AndAlso currentPointInRegion = False Then
                'have gone from within region to not within region
                'have exited a region
                Return New Stroke2DOverlap(regionStartPoint, regionOverlapCount)
            End If

            lastPointInRegion = currentPointInRegion
        Next

        'exit region
        Return New Stroke2DOverlap(regionStartPoint, stroke.Count - regionStartPoint)
    End Function

    Public Shared Sub updateRange_AddPoint(newPoint As TabletPoint2D, ByRef range As RectangleF)
        If newPoint.x < range.Left Then
            range.X = newPoint.x
            range.Width = newPoint.x - range.X
        ElseIf newPoint.x > range.Right Then
            range.Width = newPoint.x - range.X
        End If
        If newPoint.y < range.Top Then
            range.Y = newPoint.y
            range.Height = newPoint.y - range.Y
        ElseIf newPoint.y > range.Bottom Then
            range.Height = newPoint.y - range.Y
        End If
    End Sub

    Public Shared Function pointAtRange(newPoint As TabletPoint2D, ByRef range As RectangleF) As Boolean
        If newPoint.x = range.Left Then
            Return True
        ElseIf newPoint.x = range.Right Then
            Return True
        End If

        If newPoint.y = range.Top Then
            Return True
        ElseIf newPoint.y = range.Bottom Then
            Return True
        End If

        Return False
    End Function

    Public Shared Sub updateRange_RemovePoint(newPoint As TabletPoint2D, ByRef range As RectangleF, stroke As Stroke2D)
        If pointAtRange(newPoint, range) Then
            updateRange(range, stroke)
        End If
    End Sub

    Public Shared Sub updateRange_RemovePoints(collection As System.Collections.Generic.IEnumerable(Of blInk.TabletPoint2D), ByRef range As RectangleF, stroke As Stroke2D)
        Dim needUpdate As Boolean = False

        For Each p In collection
            If pointAtRange(p, range) Then
                needUpdate = True
            End If
        Next

        If needUpdate = True Then updateRange(range, stroke)
    End Sub

    Public Shared Sub updateRange(ByRef range As RectangleF, stroke As Stroke2D)
        range = findStrokeRange(stroke)
    End Sub

    'sets the upper left position of the strokes bounding box
    Public Shared Sub setStrokeLocation(stroke As Stroke2D, newBoundingBoxTopLeft As PointF)
        Dim loc As PointF = stroke.range.Location
        Dim offset As PointF = pointFSubtract(newBoundingBoxTopLeft, loc)

        stroke.MoveStroke(offset)

    End Sub

    Public Shared Function normaliseRectf(r As RectangleF) As RectangleF
        If r.Width < 0 Then
            r.X = r.X + r.Width
            r.Width = -r.Width
        End If

        If r.Height < 0 Then
            r.Y = r.Y + r.Height
            r.Height = -r.Height
        End If

        Return r
    End Function

    Public Shared Function findRangeOfStrokes(strokes As List(Of Stroke2D)) As RectangleF
        Dim xmin, xmax, ymin, ymax As Double
        xmin = Double.PositiveInfinity
        xmax = Double.NegativeInfinity
        ymin = Double.PositiveInfinity
        ymax = Double.NegativeInfinity

        Dim max As Integer = strokes.Count

        For i As Integer = 0 To max - 1
            Dim s As RectangleF = strokes(i).range
            If s.x > xmax Then xmax = s.x
            If s.x < xmin Then xmin = s.x
            If s.y > ymax Then ymax = s.y
            If s.y < ymin Then ymin = s.y
        Next

        Return New Rectangle(xmin, ymin, xmax - xmin, ymax - ymin)

    End Function

    Public Shared Function getRegionBoundingStokes(strokes As List(Of Stroke2D)) As RectangleF
        Dim minx As Integer
        Dim miny As Integer
    End Function
End Class

Public Class StrokeList
    Inherits List(Of Stroke2D)
    Implements ICloneable

    Private Const headerSize_Bytes As Integer = 4

    'need functions:
    'SplitStroke: splits the stroke at a point and creates two new strokes
    'add colour and width to stroke
    'allow stroke to be a shape? - could be a good idea!  will make saving etc. simple

    Public ReadOnly Property totalSize_Binary
        Get
            Dim i As Integer
            Dim totalLen As Integer = headerSize_Bytes
            For i = 0 To Me.Count - 1
                totalLen += Me.Item(i).totalSize_Binary
            Next

            Return totalLen
        End Get
    End Property

    Public Function toBinary() As Byte()
        Dim retVal(Me.totalSize_Binary) As Byte
        'start bytes
        'Array.Copy({&HCC, &HCC, &HCC, &HCC}, 0, retVal, 0, 4)

        'header
        'Number of points
        Array.Copy(BitConverter.GetBytes(Me.Count), 0, retVal, 0, 4)

        'If you change the headder, don't forget to update headerSize_Bytes

        'points
        Dim byteOffset As Integer = headerSize_Bytes
        Dim partLen As Integer
        For i As Integer = 0 To Me.Count - 1
            partLen = Me.Item(i).totalSize_Binary
            Array.Copy(Me.Item(i).toBinary, 0, retVal, byteOffset, partLen)
            byteOffset += partLen
        Next

        'stop bytes
        'Array.Copy({&H55, &H55, &H55, &H55}, 0, retVal, 0, 4)

        Return retVal
    End Function

    Public Shared Function fromBinary(bytes() As Byte) As StrokeList
        'first we have the header
        Dim points As Integer = BitConverter.ToInt32(bytes, 0)

        Dim retVal As New StrokeList()

        Dim strokeData() As Byte
        Dim strokePoints As Integer
        Dim strokeSize_Bin As Integer
        Dim byteOffset As Integer = headerSize_Bytes

        For i = 0 To points - 1
            strokePoints = BitConverter.ToInt32(bytes, byteOffset)
            strokeSize_Bin = Stroke2D.totalSize_Binary(strokePoints)

            ReDim strokeData(strokeSize_Bin - 1)

            Array.Copy(bytes, byteOffset, strokeData, 0, strokeSize_Bin)
            retVal.Add(Stroke2D.fromBinary(strokeData))

            byteOffset += strokeSize_Bin
        Next

        Return retVal

    End Function

    Public Function addToMicrosoftInk(ByRef ink As Ink)
        'dim strokes As Strokes = ink.CreateStrokes()

        For Each myStroke As Stroke2D In Me
            Dim stroke As Stroke = myStroke.toMicrosoftInkStroke(ink)
            ink.Strokes.Add(stroke)
        Next

    End Function

    Public Function ShallowClone() As StrokeList
        Dim newObj As New StrokeList()

        For Each s In Me
            newObj.Add(s)
        Next

        Return newObj
    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Dim newObj As New StrokeList()

        For Each s In Me
            newObj.Add(s.Clone)
        Next

        Return newObj
    End Function
End Class

Public Enum Stroke2D_Quality As Integer
    Very_High = 1
    High = 2
    Medium = 5
    Low = 10
    Very_Low = 20
    Rough = 50
    Worst = 100000000
    DrawBoundingBox = -1
End Enum

Public Class Stroke2D
    Inherits List(Of TabletPoint2D)
    Implements ICloneable

    Private Const headerSize_Bytes As Integer = 4 + 4 + 4 + 4

    Public width As Single = 1
    Public color As Color4 = New Color4(1.0F, 1.0F, 1.0F, 1.0F)

    Public isSelected As Boolean = False
    Private _range As RectangleF = Nothing
    Private _rangeInitialised = False
    Public quality As Stroke2D_Quality = Stroke2D_Quality.Very_High

    Public ReadOnly Property range As RectangleF
        Get
            If _rangeInitialised = False Then
                Drawing2D.updateRange(_range, Me)
                _rangeInitialised = True
            End If
            Return _range
        End Get
    End Property

    Public Overloads Sub Add(item As TabletPoint2D)
        MyBase.Add(item)
        If _rangeInitialised = True Then
            Drawing2D.updateRange_AddPoint(item, _range)
        End If
    End Sub

    Public Overloads Sub AddRange(collection As System.Collections.Generic.IEnumerable(Of blInk.TabletPoint2D))
        MyBase.AddRange(collection)

        If _rangeInitialised = True Then
            For Each p In collection
                Drawing2D.updateRange_AddPoint(p, _range)
            Next
        End If
    End Sub

    Public Overloads Sub Clear()
        MyBase.Clear()
        _rangeInitialised = False
    End Sub

    Public Overloads Sub Remove(item As TabletPoint2D)
        MyBase.Remove(item)
        If _rangeInitialised = True Then Drawing2D.updateRange_RemovePoint(item, _range, Me)
    End Sub

    Public Overloads Sub RemoveRange(index As Integer, count As Integer)
        Dim tmpLst As List(Of TabletPoint2D)

        If _rangeInitialised = True Then
            tmpLst = New List(Of TabletPoint2D)
            For i = index To index + count - 1
                tmpLst.Add(MyBase.Item(i))
            Next
        End If

        MyBase.RemoveRange(index, count)

        If _rangeInitialised = True Then Drawing2D.updateRange_RemovePoints(tmpLst, _range, Me)
        tmpLst = Nothing
    End Sub

    Public Sub MoveStroke(offset As PointF)
        For Each s In Me
            Drawing2D.movePoint(s, offset)
        Next

        If _rangeInitialised = True Then
            _range.Offset(offset)
        End If

    End Sub

    'need functions:
    'SplitStroke: splits the stroke at a point and creates two new strokes
    'add colour and width to stroke
    'allow stroke to be a shape? - could be a good idea!  will make saving etc. simple

    'Splits a stroke into two strokes, the first stroke will be the same object, 
    'the second stroke is what is returned.
    Public Function splitStroke(lastPointInFirstStroke As Integer) As Stroke2D
        'copy all points after 'lastPointInFirstStroke' to a new stroke 
        Dim secondStroke As New Stroke2D()
        secondStroke.AddRange(Me.GetRange(lastPointInFirstStroke, Me.Count - 1 - lastPointInFirstStroke + 1))
        Me.RemoveRange(lastPointInFirstStroke, Me.Count - 1 - lastPointInFirstStroke + 1)

        Return secondStroke
    End Function

    'returns a list of 2 NEW strokes -> ie not the same stroke as the first stroke.  The passed (original) stroke is unmodified
    Public Shared Function splitStroke(stroke As Stroke2D, lastPointInFirstStroke As Integer) As Stroke2D()
        'copy all points after 'lastPointInFirstStroke' to a new stroke 
        Dim firstStroke As Stroke2D = stroke.Clone()
        Dim secondStroke As New Stroke2D()
        secondStroke.AddRange(firstStroke.GetRange(lastPointInFirstStroke, firstStroke.Count - 1 - lastPointInFirstStroke + 1))
        firstStroke.RemoveRange(lastPointInFirstStroke, firstStroke.Count - 1 - lastPointInFirstStroke + 1)

        Return {firstStroke, secondStroke}
    End Function

    Public Shared Function totalSize_Binary(points As Integer) As Integer
        Return points * TabletPoint2D.totalSize_Binary + headerSize_Bytes
    End Function

    Public Function totalSize_Binary() As Integer
        Return totalSize_Binary(Me.Count)
    End Function

    Public Function toBinary() As Byte()
        Dim retVal(Me.totalSize_Binary) As Byte
        'start bytes
        'Array.Copy({&HCC, &HCC, &HCC, &HCC}, 0, retVal, 0, 4)

        'header
        'Number of points
        Array.Copy(BitConverter.GetBytes(CType(Me.Count, Int32)), 0, retVal, 0, 4)
        Array.Copy(BitConverter.GetBytes(color.R), 0, retVal, 4, 4)
        Array.Copy(BitConverter.GetBytes(color.G), 0, retVal, 8, 4)
        Array.Copy(BitConverter.GetBytes(color.B), 0, retVal, 12, 4)
        'If you change the headder, don't forget to update headerSize_Bytes

        'points
        For i = 0 To Me.Count - 1
            Array.Copy(Me.Item(i).toBinary, 0, retVal, i * TabletPoint2D.totalSize_Binary + headerSize_Bytes, TabletPoint2D.totalSize_Binary)
        Next

        'stop bytes
        'Array.Copy({&H55, &H55, &H55, &H55}, 0, retVal, 0, 4)

        Return retVal
    End Function

    Public Shared Function fromBinary(bytes() As Byte) As Stroke2D
        Dim R, G, B As Single
        'first we have the header
        Dim points As Integer = BitConverter.ToInt32(bytes, 0)
        R = BitConverter.ToSingle(bytes, 4)
        G = BitConverter.ToSingle(bytes, 8)
        B = BitConverter.ToSingle(bytes, 12)

        Dim retVal As New Stroke2D()
        retVal.color = New Color4(R, G, B, 1.0F)

        Dim pointData(TabletPoint2D.totalSize_Binary - 1) As Byte

        For i = 0 To points - 1
            Array.Copy(bytes, i * TabletPoint2D.totalSize_Binary + headerSize_Bytes, pointData, 0, TabletPoint2D.totalSize_Binary)
            retVal.Add(TabletPoint2D.fromBinary(pointData))
        Next

        Return retVal

    End Function

    Public Function toMicrosoftInkStroke(ByRef ink As Ink) As Stroke

        'create points
        Dim i As Integer = 0
        Dim points(Me.Count-1) As Point
        For Each p As TabletPoint2D In Me
            points(i) = p.toPoint
            i += 1
        Next

        Return ink.CreateStroke(points)

    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Dim newstroke As New Stroke2D()
        Dim p As TabletPoint2D

        For Each p In Me
            newstroke.Add(p.Clone)
        Next

        Return newstroke
    End Function
End Class

Public Class TabletPoint2D
    Implements ICloneable

    Public Const totalSize_Binary As Integer = 8 + 8 + 4
    Public x As Double
    Public y As Double
    Public pressure As Single

    Public Sub New()

    End Sub

    Public Sub New(_x, _y, _pressure)
        x = _x
        y = _y
        pressure = _pressure
    End Sub

    Public Sub New(p As PointF, Optional _pressure As Single = 1024)
        x = p.X
        y = p.Y
        pressure = _pressure
    End Sub

    Public Function toPointF() As PointF
        Return New PointF(x, y)
    End Function

    Public Function toPoint(Optional scale As Double = 1) As Point
        Return New Point(x * scale, y * scale)
    End Function

    Public Sub New(bytes() As Byte)
        Dim a As TabletPoint2D = fromBinary(bytes)
        x = a.x
        y = a.y
        pressure = a.pressure
    End Sub

    Public Function toBinary() As Byte()

        Dim retVal(totalSize_Binary - 1) As Byte

        Array.Copy(BitConverter.GetBytes(x), 0, retVal, 0, 8)
        Array.Copy(BitConverter.GetBytes(y), 0, retVal, 8, 8)
        Array.Copy(BitConverter.GetBytes(pressure), 0, retVal, 16, 4)

        Return retVal

    End Function

    Public Shared Function fromBinary(bytes() As Byte) As TabletPoint2D
        If bytes.Count = totalSize_Binary Then
            Dim x, y As Double
            Dim pressure As Single

            x = BitConverter.ToDouble(bytes, 0)
            y = BitConverter.ToDouble(bytes, 8)
            pressure = BitConverter.ToSingle(bytes, 16)

            Return New TabletPoint2D(x, y, pressure)
        Else
            Throw New Exception("Invalid length of passed bytes")
        End If
    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Return New TabletPoint2D(x, y, pressure)
    End Function
End Class

Public Class MiscToBin
    Private Const RectangleF_totalSize_Binary As Integer = 16

    Public Shared Function rectangleFtoBinary(rect As RectangleF) As Byte()
        Dim retVal(RectangleF_totalSize_Binary - 1) As Byte

        Array.Copy(BitConverter.GetBytes(rect.Left), 0, retVal, 0, 4)
        Array.Copy(BitConverter.GetBytes(rect.Top), 0, retVal, 4, 4)
        Array.Copy(BitConverter.GetBytes(rect.Width), 0, retVal, 8, 4)
        Array.Copy(BitConverter.GetBytes(rect.Height), 0, retVal, 12, 4)

        Return retVal
    End Function

    Public Shared Function rectangleFfromBinary(bytes() As Byte) As RectangleF
        If Len(bytes) = RectangleF_totalSize_Binary Then
            Dim x, y, width, height As Single

            x = BitConverter.ToSingle(bytes, 0)
            y = BitConverter.ToSingle(bytes, 4)
            width = BitConverter.ToSingle(bytes, 8)
            height = BitConverter.ToSingle(bytes, 12)

            Return New RectangleF(x, y, width, height)
        Else
            Throw New Exception("Invalid length of passed bytes")
        End If
    End Function
End Class