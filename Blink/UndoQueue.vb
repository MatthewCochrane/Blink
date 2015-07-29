Public Class UndoQueue
    Private Const headerSize_Bytes As Integer = 4
    Private undoOperations As New List(Of UndoOperation)
    Private redoOperations As New List(Of UndoOperation)
    Private tabPage As TabletPageData

    Public Sub New(_tabPage As TabletPageData)
        tabPage = _tabPage
    End Sub

    Public Function GetUndoNames() As List(Of String)
        Dim retList As New List(Of String)
        Dim op As UndoOperation

        For Each op In undoOperations
            retList.Add(op.OperationName)
        Next

        Return retList
    End Function

    Public Function GetRedoNames() As List(Of String)
        Dim retList As New List(Of String)
        Dim op As UndoOperation

        For Each op In redoOperations
            retList.Add(op.OperationName)
        Next

        Return retList
    End Function

    Public Sub Undo()
        If undoOperations.Count > 0 Then
            'perform operation
            PerformUndoOperation(undoOperations.Last)
            'move into redo operations list
            redoOperations.Add(undoOperations.Last)
            undoOperations.Remove(undoOperations.Last)
        End If

        'If tabPage.strokes.Count > 0 Then
        '    strokes.Add(tabPage.strokes.Last)
        '    tabPage.strokes.Remove(tabPage.strokes.Last)
        'End If
    End Sub

    Public Sub Redo()
        If redoOperations.Count > 0 Then
            'perform operation
            RevertUndoOperation(redoOperations.Last)
            'move into redo operations list
            undoOperations.Add(redoOperations.Last)
            redoOperations.Remove(redoOperations.Last)
        End If

        'If strokes.Count > 0 Then
        '    tabPage.strokes.Add(strokes.Last)
        '    strokes.Remove(strokes.Last)
        'End If
    End Sub

    Private atomicOperationData As UndoOperation
    Private inAtomicOperation As Boolean = False

    Public Sub AtomicDrawingOperation_Begin()
        If inAtomicOperation = False Then
            atomicOperationData = New UndoOperation("Unknown")
            inAtomicOperation = True
        End If
    End Sub

    Public Sub AtomicDrawingOperation_End(OperationName As String)
        atomicOperationData.OperationName = OperationName
        undoOperations.Add(atomicOperationData)

        'once we perform an operation, all redo's become void
        redoOperations.Clear()
        inAtomicOperation = False
    End Sub


    Public Sub AtomicDrawingOperation_Cancel()
        redoOperations.Clear()
        inAtomicOperation = False
    End Sub

    Public Sub PerformUndoOperation(undoOp As UndoOperation)
        'eg. by pressing crtl-z
        Dim el As UndoOperationElement

        For Each el In undoOp
            PerformUndoOperationElement(el)
        Next
    End Sub

    Private Sub PerformUndoOperationElement(el As UndoOperationElement)
        Select Case el.Type
            Case UndoOperationElementType.Create
                'we create the specified new line
                tabPage.strokes.Add(el.newStroke)
            Case UndoOperationElementType.Delete
                'we delete the specified old line
                tabPage.strokes.Remove(el.oldStroke)
            Case UndoOperationElementType.Modify
                'we delete the specified old line and create the specified new line
                'instead, copy the points from the newstroke to the oldstroke
                swapStrokePoints(el.oldStroke, el.newStroke)
                'also, copy the oldstroke points to the newstroke
                'tabPage.strokes.Remove(el.oldStroke)
                'tabPage.strokes.Add(el.newStroke)
        End Select
    End Sub

    Private Sub swapStrokePoints(s1 As Stroke2D, s2 As Stroke2D)
        Dim stmp As Stroke2D = s1.Clone
        'copy all s2 points to s1
        s1.Clear()
        s1.AddRange(s2)
        'copy all the points from stmp to s2
        s2.Clear()
        s2.AddRange(stmp)
    End Sub

    Public Sub RevertUndoOperation(undoOp As UndoOperation)
        Dim el As UndoOperationElement

        For Each el In undoOp
            RevertUndoOperationElement(el)
        Next
    End Sub

    Public Sub RevertUndoOperationElement(el As UndoOperationElement)
        Select Case el.Type
            Case UndoOperationElementType.Create
                'we revert the creation of the specified new line
                tabPage.strokes.Remove(el.newStroke)
            Case UndoOperationElementType.Delete
                'we revert the deletion the specified old line
                tabPage.strokes.Add(el.oldStroke)
            Case UndoOperationElementType.Modify
                'we revert the deletion the specified old line and also revert the creation the specified new line
                swapStrokePoints(el.oldStroke, el.newStroke)
                'tabPage.strokes.Add(el.oldStroke)
                'tabPage.strokes.Remove(el.newStroke)
        End Select
    End Sub

    Public Sub ModifyStrokes(originalStroke As Stroke2D, newStroke As Stroke2D)
        'call this whenever the user adds a single stroke in the program
        ModifyStrokes(New List(Of Stroke2D) From {originalStroke}, New List(Of Stroke2D) From {newStroke})
    End Sub

    Public Sub ModifyStrokes(originalStrokes As List(Of Stroke2D), newStrokes As List(Of Stroke2D))
        'call this whenever the user adds multiple strokes in one operation in the program

        If newStrokes.Count > 0 Then
            If inAtomicOperation = False Then
                'perform a whole atomic operation since we're not in one
                ModifyStrokesAtomic(originalStrokes, newStrokes)
            Else
                Dim op As New UndoOperation("Modify Stroke" & IIf(newStrokes.Count > 1, "s", ""))
                Dim el As UndoOperationElement
                Dim cnt As Integer = Math.Min(originalStrokes.Count, newStrokes.Count)

                'say we modified a stroke during an atomic operation
                'then later we modified it again
                'instead of having two modify undoOperationElements, this function
                'will remove duplicate modify undoOperationElements from the atomic opertion.

                'when you modify an element, you have an oldElement and a newElement
                'the oldElement is the original element (a deep copy of it).
                'the newElement is the modified element which resides in the TabletPageData's strokes collection.
                'if we modify a stroke more than once, we are talking about the same stroke when both of the modify undoOperationElement's
                'newElements are equal by reference.

                'in fact, we don't need to do anything fancy at all. Once the modify undoOperationElement is added, we have a preserved version
                'of the original stroke, we also have a pointer to the new stroke which will be modified automatically!

                For i = 0 To cnt - 1
                    'describes what to do when we perform the undo
                    el = New UndoOperationElement(UndoOperationElementType.Modify, newStrokes(i), originalStrokes(i))
                    atomicOperationData.Add(el)
                Next
            End If
        End If
    End Sub

    Private Sub ModifyStrokesAtomic(originalStrokes As List(Of Stroke2D), newStrokes As List(Of Stroke2D))
        Dim op As New UndoOperation("Modify Stroke" & IIf(newStrokes.Count > 1, "s", ""))
        Dim el As UndoOperationElement
        Dim cnt As Integer = Math.Min(originalStrokes.Count, newStrokes.Count)

        For i = 0 To cnt - 1
            'describes what to do when we perform the undo
            el = New UndoOperationElement(UndoOperationElementType.Modify, originalStrokes(i), newStrokes(i))
            op.Add(el)
        Next

        undoOperations.Add(op)

        'once we perform an operation, all redo's become void
        redoOperations.Clear()
    End Sub

    Public Sub AddStrokes(stroke As Stroke2D)
        'call this whenever the user adds a single stroke in the program
        AddStrokes(New List(Of Stroke2D) From {stroke})
    End Sub

    Public Sub AddStrokes(strokes As List(Of Stroke2D))
        'call this whenever the user adds multiple strokes in one operation in the program


        If strokes.Count > 0 Then
            If inAtomicOperation = False Then
                'perform a whole atomic operation since we're not in one
                AddStrokesAtomic(strokes)
            Else
                Dim op As New UndoOperation("Add Stroke" & IIf(strokes.Count > 1, "s", ""))
                Dim el As UndoOperationElement
                Dim s As Stroke2D

                For Each s In strokes
                    'describes what to do when we perform the undo
                    el = New UndoOperationElement(UndoOperationElementType.Delete, s, Nothing)
                    atomicOperationData.Add(el)
                Next
            End If
        End If
    End Sub

    Private Sub AddStrokesAtomic(strokes As List(Of Stroke2D))
        Dim op As New UndoOperation("Add Stroke" & IIf(strokes.Count > 1, "s", ""))
        Dim el As UndoOperationElement
        Dim s As Stroke2D

        For Each s In strokes
            'describes what to do when we perform the undo
            el = New UndoOperationElement(UndoOperationElementType.Delete, s, Nothing)
            op.Add(el)
        Next

        undoOperations.Add(op)

        'once we perform an operation, all redo's become void
        redoOperations.Clear()
    End Sub

    Public Sub DeleteStrokes(stroke As Stroke2D)
        'call this whenever the user deletes a single stroke in the program
        AddStrokes(New List(Of Stroke2D) From {stroke})
    End Sub

    Public Sub DeleteStrokes(strokes As List(Of Stroke2D))
        'call this whenever the user deletes multiple strokes in one operation in the program
        If strokes.Count > 0 Then
            If inAtomicOperation = False Then
                'perform a whole atomic operation since we're not in one
                DeleteStrokesAtomic(strokes)
            Else
                Dim op As New UndoOperation("Delete Stroke" & IIf(strokes.Count > 1, "s", ""))
                Dim el As UndoOperationElement
                Dim s As Stroke2D

                For Each s In strokes
                    If DeleteCreatedStrokeFromAtomicOperation(s) = False Then
                        'if we didn't remove a create undoOperationElement from the atomic operation data list then we need
                        'to actually add a remove undoOperationElement

                        'describes what to do when we perform the undo
                        el = New UndoOperationElement(UndoOperationElementType.Create, Nothing, s)
                        atomicOperationData.Add(el)
                    End If
                Next
            End If
        End If
    End Sub

    Public Function DeleteCreatedStrokeFromAtomicOperation(s As Stroke2D) As Boolean
        'say we created a stroke during an atomic operation
        'then later we removed it
        'instead of having a create undoOperationElement and also a delete undoOperationElement, this
        'function will just remove the create undoOperationElement from the atomic opertion, essentially deleting the element.
        Dim el As UndoOperationElement
        For Each el In atomicOperationData
            If Object.ReferenceEquals(el.oldStroke, s) Then
                atomicOperationData.Remove(el)
                Return True
            End If
        Next
        Return False
    End Function

    Public Function DeleteModifiedStrokeFromAtomicOperation(s As Stroke2D) As Boolean
        'say we modified a stroke during an atomic operation
        'then later we modified it again
        'instead of having two modify undoOperationElements, this function
        'will remove duplicate modify undoOperationElements from the atomic opertion.

        'when you modify an element, you have an oldElement and a newElement
        'the oldElement is the original element (a deep copy of it).
        'the newElement is the modified element which resides in the TabletPageData's strokes collection.
        'if we modify a stroke more than once, we are talking about the same stroke when both of the modify undoOperationElement's
        'newElements are equal by reference.

        'in fact, we don't need to do anything fancy at all. Once the modify undoOperationElement is added, we have a preserved version
        'of the original stroke, we also have a pointer to the new stroke which will be modified automatically!

        'Dim el As UndoOperationElement
        'For Each el In atomicOperationData
        '    If Object.ReferenceEquals(el.oldStroke, s) Then
        '        atomicOperationData.Remove(el)
        '        Return True
        '    End If
        'Next
        'Return False
    End Function

    Private Sub DeleteStrokesAtomic(strokes As List(Of Stroke2D))
        Dim op As New UndoOperation("Delete Stroke" & IIf(strokes.Count > 1, "s", ""))
        Dim el As UndoOperationElement
        Dim s As Stroke2D

        For Each s In strokes
            'describes what to do when we perform the undo
            el = New UndoOperationElement(UndoOperationElementType.Create, Nothing, s)
            op.Add(el)
        Next

        undoOperations.Add(op)

        'once we perform an operation, all redo's become void
        redoOperations.Clear()
    End Sub
End Class

Public Class UndoOperation
    Inherits List(Of UndoOperationElement)
    Public OperationName As String
    'this is one atomic undo operation
    Public Sub New(_operationName As String)
        MyBase.New()
        OperationName = _operationName
    End Sub
End Class

Public Enum UndoOperationElementType
    Create
    Delete
    Modify '(replace with new)
End Enum

Public Class UndoOperationElement
    'this is one element of an undo operation
    Public Type As UndoOperationElementType
    Public oldStroke As Stroke2D
    Public newStroke As Stroke2D

    Public Sub New(_type As UndoOperationElementType, _oldStroke As Stroke2D, _newStroke As Stroke2D)
        Type = _type
        oldStroke = _oldStroke
        newStroke = _newStroke
    End Sub
End Class