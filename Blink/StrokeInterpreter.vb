Imports OpenTK.Graphics.OpenGL
Imports Microsoft.Ink

Public Class StrokeInterpreter
    Dim allRecognizers As Recognizers
    Dim mainRecognizer As Recognizer
    Dim recognizerContext As RecognizerContext
    Dim inkCollector As InkCollector

    Public Sub New()
        allRecognizers = New Recognizers
        mainRecognizer = allRecognizers.GetDefaultRecognizer()
        recognizerContext = mainRecognizer.CreateRecognizerContext()
        inkCollector = New InkCollector()
        inkCollector.Enabled = False
    End Sub

    Public Function interpret(strokeList As StrokeList) As String
        'CheckBox the last N strokes to see if we match anything...

        Dim decodeStrokeList As StrokeList

        decodeStrokeList = New StrokeList()

        'Add the last 10 strokes (in forward order)
        Dim startIndex As Integer = strokeList.Count - 1 - 10
        If startIndex < 0 Then startIndex = 0
        For i As Integer = startIndex To strokeList.Count - 1
            decodeStrokeList.Add(strokeList(i))
        Next

        Return recognise(decodeStrokeList)

    End Function

    Private Function recognise(strokeList As StrokeList) As String
        inkCollector.Ink.DeleteStrokes()

        strokeList.addToMicrosoftInk(inkCollector.Ink)

        Try
            Return inkCollector.Ink.Strokes.ToString()
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Private Function recognise_findMany(strokeList As StrokeList) As List(Of String)
        inkCollector.Ink.DeleteStrokes()

        strokeList.addToMicrosoftInk(inkCollector.Ink)

        recognizerContext.Strokes = inkCollector.Ink.Strokes

        Dim status As RecognitionStatus
        Dim result As RecognitionResult = recognizerContext.Recognize(status)
        Dim retList As New List(Of String)

        If Not (result Is Nothing) Then
            If status = RecognitionStatus.NoError AndAlso Not (result Is Nothing) Then
                'display options
                Dim alternate As RecognitionAlternate
                For Each alternate In result.GetAlternatesFromSelection()
                    retList.Add(alternate.ToString())
                Next
            Else
                Debug.Print("Error in recognition:" + status.ToString())
            End If
        End If

        Return retList
    End Function


    ''If result.Trim.EndsWith("=") Then
    ''result = result.Remove(result.Length - 1, 1)
    'Try
    'Dim exp As NCalc.Expression = New NCalc.Expression(resultStr)

    '            MsgBox(resultStr + " = " + exp.Evaluate().ToString)
    '        Catch ex As Exception
    '            MsgBox("Could not evaluate'" + resultStr + "'")
    '        End Try
    ''Else
    ''MsgBox(result)
    ''End If

    Public Function wrapAngle(ang As Double, lastAng As Double) As Double
        Return ang
    End Function

    Public Shared Function strokeToShape(stroke As Stroke2D) As Stroke2D
        Dim directionFilter As New MovingAverageFilter(10)
        Dim lastAng As Double = -1
        Dim ang As Double = -1
        Dim filteredAngles As New List(Of Double)
        Dim outShape As New Stroke2D
        Dim sideStart As TabletPoint2D = stroke(0)
        Dim sideLength As Double
        Dim range As RectangleF = Drawing2D.findStrokeRange(stroke)
        Dim dists As New List(Of Double)

        outShape.Add(stroke(0))



        'distance and angle.
        'know the total shape size...
        'if the side length is > 50% of the smallest

        For i As Integer = 1 To stroke.Count - 1
            lastAng = ang

            'find direction
            ang = Drawing2D.angle(stroke(i - 1), stroke(i))
            dists.Add(Drawing2D.dist(stroke(i - 1), stroke(i)))
            sideLength = Drawing2D.dist(sideStart, stroke(i))
            'ang = wrapAngle(ang, lastAng)

            'filter
            If (i > 1) Then
                filteredAngles.Add(directionFilter.add(ang) * 180 / Math.PI)
                'filteredAngles.Add(ang * 180 / Math.PI)
            End If

            'calculate filtered direction (don in above step)


            'check if direction has changed
            If filteredAngles.Count > 3 Then
                Dim sideAng = Drawing2D.angle(sideStart, stroke(i))

                'if the turn rate > 100 degrees per (width/10)
                Dim angularRate = (filteredAngles.Last - filteredAngles(filteredAngles.Count - 3)) / ((dists(dists.Count - 1) + dists(dists.Count - 2) + dists(dists.Count - 3)) / range.Width)
                'in degrees per length unit (degrees per pixel?)

                Debug.Print("angularRate rate: " + angularRate.ToString)

                'Dim angDiff As Double = filteredAngles(filteredAngles.Count - 5) - filteredAngles.Last

                'If 

                If sideLength > (Math.Min(range.Width, range.Height) / 2) And Math.Abs(angularRate) > 800 Then
                    'we have a vertex!
                    outShape.Add(stroke(i))
                    sideStart = stroke(i)
                End If
            End If

            'if so specify the vertex position

        Next

        outShape.Add(stroke(0))
        Return outShape
    End Function
End Class

