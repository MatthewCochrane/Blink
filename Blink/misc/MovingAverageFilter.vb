Public Class MovingAverageFilter '(Of Type)
    Dim taps As Integer

    Dim sum As Double 'Type
    Dim samples As New List(Of Double) 'Type)

    Public Sub New(taps As Integer)
        Me.taps = taps
    End Sub

    Public Function add(sample As Double) As Double 'Type)
        If samples.Count >= taps Then
            'remove first sample in list
            sum -= samples(0)
            samples.RemoveAt(0)
        End If
        samples.Add(sample)
        sum += (sample)
        Return getResult()
    End Function

    Public Function getResult()
        Return sum / taps
    End Function

End Class
