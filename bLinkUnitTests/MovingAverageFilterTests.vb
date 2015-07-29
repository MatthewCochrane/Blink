Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports blInk

'<TestClass()> Public Class StrokeInterpreterTests

'    <TestMethod()> Public Sub TestMethod1()
'    End Sub

'End Class


<TestClass()> Public Class MovingAverageFilterTests

    <TestMethod()> Public Sub All()
        Dim maf = New MovingAverageFilter(10)

        For i As Integer = 0 To 100
            maf.add(10)
            If i > 10 Then
                Assert.AreEqual(10.0, maf.getResult())
            End If
        Next

    End Sub

End Class