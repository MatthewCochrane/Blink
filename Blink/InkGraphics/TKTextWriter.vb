Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL

Public Structure TextWriterMode
    Public Enum TextWriterMode_Enum
        None = 0
        Mouse_Over = 1
        Editing = 2
        Selecting = 4
    End Enum

    Public Shared Mode As TextWriterMode_Enum = TextWriterMode_Enum.None

    Public Sub addMode(m As TextWriterMode_Enum)
        Mode = Mode Or m
    End Sub

    Public Sub removeMode(m As TextWriterMode_Enum)
        Mode = Mode And Not (m)
    End Sub

    Public Sub clearModes()
        Mode = TextWriterMode_Enum.None
    End Sub

    Public Function isModeActivated(m As TextWriterMode_Enum) As Boolean
        Return Mode And m
    End Function
End Structure

Public Class TKTextWriter
    Public Position As TabletPoint2D
    Public Mode As New TextWriterMode

    Private TextFont As Font
    Private TextBitmap As Bitmap
    Private _positions_onBitmap As List(Of PointF)
    Private _lines As List(Of String)
    Private _colour As Brush
    Private _textureId As Integer
    Private _clientSize As SizeF
    Private _textHeight_inWorld As Single = 0
    Private _lineSpacing As Single = 0.0 'as a percentage of _textHeight
    Private _cursor As TKTextCursor = New TKTextCursor(0, 0)
    Private _strCursor As Integer = 0
    Private __strCursor_selStart As Integer = 0
    Private __strCursor_selLength As Integer = 0
    Public Event RequiresRedraw()
    Private _textSizeToScreenSizeRatio As Double = 30 'text size/screen size

    Public Class TKTextCursor
        Public RowIndex As Integer
        Public ColIndex As Integer

        Public Sub New(RowInd As Integer, ColInd As Integer)
            RowIndex = RowInd
            ColIndex = ColInd
        End Sub
    End Class

    Public Sub clearTextSelection()
        setTextSelLength(0)
    End Sub

    Private Sub setTextSelLength(len As Integer)
        __strCursor_selLength = len
        setTextSelEnd(StringLocToCursorLoc(CleanUpCursor(getSelectionEnd())))
        _strCursor = __strCursor_selStart + __strCursor_selLength
        _cursor = StringLocToCursorLoc(_strCursor)
    End Sub

    Private Sub setTextCursor(strPos As Integer)
        strPos = CleanUpCursor(strPos)
        _strCursor = strPos
        _cursor = StringLocToCursorLoc(_strCursor)
        __strCursor_selStart = strPos
        __strCursor_selLength = 0
    End Sub

    Private Sub setTextCursor(pos As TKTextCursor)
        pos = CleanUpCursor(pos)
        _cursor = pos
        _strCursor = CursorLocToStringLoc(_cursor)
        __strCursor_selStart = _strCursor
        __strCursor_selLength = 0
    End Sub

    Private Sub setTextSelEnd(pos As TKTextCursor)
        'need to find new length
        'end pos - start pos
        pos = CleanUpCursor(pos)
        _strCursor = CursorLocToStringLoc(pos)
        _cursor = StringLocToCursorLoc(_strCursor)
        __strCursor_selLength = CursorLocToStringLoc(_cursor) - __strCursor_selStart
    End Sub

    Private Function getSelectionEnd() As Integer
        Return __strCursor_selStart + __strCursor_selLength
    End Function

    Private Sub setTextSelStart(strPos As Integer)
        'don't move end... need to recalculate length
        'TODO[ ]: NOT TESTED... TEST!!!!!
        __strCursor_selLength += (__strCursor_selStart - strPos)
        __strCursor_selStart = strPos
        '__strCursor_selLength = CursorLocToStringLoc(__cursor_selEnd) - __strCursor_selStart
    End Sub

    Private Sub moveTextSelStart(strPos As Integer)
        'move end as well based on length.. basically just don't touch the selLength variable
        __strCursor_selStart = strPos
    End Sub

    Private ReadOnly Property _textHeight_inBitmap
        Get
            Return _textHeight_inWorld * _textSizeToScreenSizeRatio
        End Get
    End Property

    Private Function mapBitmapPosToLocalWorldPos(bmPos As PointF) As PointF
        Return Drawing2D.mapPointToSpace(bmPos, TextBitmap.Size, _clientSize)
    End Function

    Private Function mapLocalWorldPosToBitmapPos(localWorldPos As PointF) As PointF
        Return Drawing2D.mapPointToSpace(localWorldPos, _clientSize, TextBitmap.Size)
    End Function

    Public Property cursor As TKTextCursor
        Get
            Return _cursor
        End Get
        Set(value As TKTextCursor)
            _cursor = value
            CleanUpCursor()
        End Set
    End Property

    Public Sub SetCursorPos(RowInd As Integer, ColInd As Integer)
        cursor = New TKTextCursor(RowInd, ColInd)
    End Sub

    Public Property Text As String
        Get
            Dim retStr As String = ""
            For Each s In _lines
                retStr &= s & vbCr
            Next
            Return retStr.Substring(0, retStr.Length - 1)
        End Get
        Set(value As String)
            'value.Replace(vbCrLf, vbCr)
            'value.Replace(vbLf, vbCr)
            Dim lines() As String = value.Split(vbCr)

            _lines.Clear()
            _positions_onBitmap.Clear()

            For Each s In lines
                AddLine(s, False)
            Next
            UpdateSize()
            CleanUpCursor()
        End Set
    End Property

    Public ReadOnly Property range As RectangleF
        Get
            Return New RectangleF(Position.toPointF, _clientSize)
        End Get
    End Property

    Public Sub Update(ind As Integer, newText As String)
        If ind < _lines.Count Then
            _lines(ind) = newText
            UpdateText()
        End If
    End Sub

    Public Sub New(_position As TabletPoint2D, areaSize As SizeF, Optional font As FontFamily = Nothing, Optional fontSize As Single = 8, Optional colour As Brush = Nothing)
        If IsNothing(font) Then
            font = FontFamily.GenericSansSerif
        End If
        TextFont = New Font(font, fontSize)
        Position = _position
        _positions_onBitmap = New List(Of PointF)()
        _lines = New List(Of String)()
        _colour = IIf(IsNothing(colour), Brushes.Red, colour)
        _clientSize = areaSize

        TextBitmap = New Bitmap(CInt(areaSize.Width * _textSizeToScreenSizeRatio), CInt(areaSize.Height * _textSizeToScreenSizeRatio))
        _textureId = CreateTexture()
    End Sub

    Private ReadOnly Property TextLineHeight
        Get
            If _textHeight_inWorld = 0 Then
                _textHeight_inWorld = CalcTextHeight()
            End If
            Return _textHeight_inWorld
        End Get
    End Property

    Public Function GetCursorLocationInLocalSpace(colIndex As Integer, rowIndex As Integer) As TabletPoint2D
        Dim m As SizeF

        Using gfx As Drawing.Graphics = Drawing.Graphics.FromImage(TextBitmap)
            If _lines(rowIndex).Count > 0 Then
                m = MeasureString(gfx, _lines(rowIndex).Substring(0, Math.Min(colIndex, _lines(rowIndex).Count)), TextFont)
                If colIndex > 0 Then m.Width -= TextFont.Size / 5 Else m.Width += TextFont.Size / 5
            Else
                m = New SizeF(0, 0)
            End If
        End Using

        Return New TabletPoint2D(m.Width, mapBitmapPosToLocalWorldPos(_positions_onBitmap(rowIndex)).Y, 1024)

    End Function

    Public Function GetCursorRect(colIndex As Integer, rowIndex As Integer) As RectangleF
        Dim m, n As SizeF

        Using gfx As Drawing.Graphics = Drawing.Graphics.FromImage(TextBitmap)
            If _lines(rowIndex).Count > 0 Then
                m = MeasureString(gfx, _lines(rowIndex).Substring(0, Math.Min(colIndex, _lines(rowIndex).Count)), TextFont)
                If colIndex > 0 Then m.Width -= TextFont.Size / 5 'Else m.Width += TextFont.Size / 5
                If colIndex < _lines(rowIndex).Count Then
                    n = MeasureString(gfx, _lines(rowIndex).Substring(0, Math.Min(colIndex + 1, _lines(rowIndex).Count)), TextFont)
                    If colIndex > 0 AndAlso colIndex < _lines(rowIndex).Length - 1 Then n.Width -= TextFont.Size / 5 Else n.Width -= TextFont.Size / 5
                Else
                    n = New SizeF(m)
                End If
            Else
                m = New SizeF(0, 0)
            End If
        End Using

        Return New RectangleF(New PointF(m.Width, mapBitmapPosToLocalWorldPos(_positions_onBitmap(rowIndex)).Y), New SizeF(n.Width - m.Width, n.Height))

    End Function

    Private Function CreateTexture() As Integer
        Dim textureId As Integer
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, CSng(TextureEnvMode.Replace))
        'Important, or wrong color on some computers
        Dim bitmap As Bitmap = TextBitmap
        GL.GenTextures(1, textureId)
        GL.BindTexture(TextureTarget.Texture2D, textureId)

        Dim data As Imaging.BitmapData = bitmap.LockBits(New System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), Imaging.ImageLockMode.[ReadOnly], System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, _
            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, CInt(TextureMinFilter.Linear))
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, CInt(TextureMagFilter.Linear))
        '    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
        'GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
        GL.Finish()
        bitmap.UnlockBits(data)
        Return textureId
    End Function

    Public Sub Dispose()
        If _textureId > 0 Then
            GL.DeleteTexture(_textureId)
        End If
    End Sub

    Public Sub Clear()
        _lines.Clear()
        _positions_onBitmap.Clear()
    End Sub

    Public Sub AddLine(s As String, pos As PointF)
        _lines.Add(s)
        _positions_onBitmap.Add(pos)
        UpdateSize()
    End Sub

    Public Sub AddLine(s As String, Optional update As Boolean = True)
        _lines.Add(s)
        If _positions_onBitmap.Count = 0 Then
            _positions_onBitmap.Add(New PointF(0, 0))
        Else
            _positions_onBitmap.Add(New PointF(_positions_onBitmap.Last.X, _positions_onBitmap.Last.Y + _textHeight_inBitmap * (1 + _lineSpacing)))
        End If

        If update = True Then UpdateSize()
    End Sub

    Public Property Size As SizeF
        Get
            Return _clientSize
        End Get
        Set(value As SizeF)
            _clientSize = value
        End Set
    End Property

    Public Function CalcTextHeight() As Single
        Using gfx As Drawing.Graphics = Drawing.Graphics.FromImage(TextBitmap)
            Return MeasureString(gfx, "gf", TextFont).Height
        End Using
    End Function

    Public Sub UpdateSize()
        Dim boundingRect As RectangleF
        Using gfx As Drawing.Graphics = Drawing.Graphics.FromImage(TextBitmap)
            boundingRect = New RectangleF(mapBitmapPosToLocalWorldPos(_positions_onBitmap(0)), MeasureString(gfx, _lines(0), TextFont))
            'New RectangleF(_positions(0), gfx.MeasureString(_lines(0), TextFont))
            For i As Integer = 1 To _lines.Count - 1
                boundingRect = RectangleF.Union(New RectangleF(mapBitmapPosToLocalWorldPos(_positions_onBitmap(i)), MeasureString(gfx, _lines(i), TextFont)), boundingRect)
            Next
        End Using
        'update position
        Position = Drawing2D.tabletPoint2DAdd(Position, New TabletPoint2D(boundingRect.Location.X, boundingRect.Location.Y, 0))
        If boundingRect.Width = 0 Then boundingRect.Width = 10
        If boundingRect.Height = 0 Then boundingRect.Height = TextLineHeight
        _clientSize = boundingRect.Size
        TextBitmap = New Bitmap(CInt(_clientSize.Width * _textSizeToScreenSizeRatio), CInt(_clientSize.Height * _textSizeToScreenSizeRatio))
        GL.DeleteTexture(_textureId)
        _textureId = CreateTexture()
        UpdateText()
    End Sub

    Public Sub UpdateText()
        If _lines.Count > 0 Then
            Dim tmptextsize As Font = New Font(TextFont.FontFamily, TextFont.Size * _textSizeToScreenSizeRatio)
            Using gfx As Drawing.Graphics = Drawing.Graphics.FromImage(TextBitmap)
                gfx.Clear(Color.Black)
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
                For i As Integer = 0 To _lines.Count - 1
                    gfx.DrawString(_lines(i), tmptextsize, _colour, _positions_onBitmap(i))
                Next
            End Using

            Dim data As System.Drawing.Imaging.BitmapData = TextBitmap.LockBits(New Rectangle(0, 0, TextBitmap.Width, TextBitmap.Height), System.Drawing.Imaging.ImageLockMode.[ReadOnly], System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, TextBitmap.Width, TextBitmap.Height, _
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0)
            TextBitmap.UnlockBits(data)
        End If
    End Sub

    Public Sub Draw()
        GL.Enable(EnableCap.Blend)
        GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.DstColor)
        GL.Enable(EnableCap.Texture2D)
        GL.BindTexture(TextureTarget.Texture2D, _textureId)

        Dim rect As New RectangleF(Position.toPointF, _clientSize)

        GL.Begin(BeginMode.Quads)
        'Top-Right
        GL.TexCoord2(1.0F, 0.0F)
        GL.Vertex2(rect.Right, rect.Top)
        'Top-Left
        GL.TexCoord2(0.0F, 0.0F)
        GL.Vertex2(rect.Left, rect.Top)
        'Bottom-Left
        GL.TexCoord2(0.0F, 1.0F)
        GL.Vertex2(rect.Left, rect.Bottom)
        'Bottom-Right
        GL.TexCoord2(1.0F, 1.0F)
        GL.Vertex2(rect.Right, rect.Bottom)
        Debug.Print(TextBitmap.Width)
        GL.End()

        GL.Disable(EnableCap.Blend)
        GL.Disable(EnableCap.Texture2D)
        GL.BindTexture(TextureTarget.Texture2D, 0)

        DrawBox()
        DrawCursor()

        'DrawBoxesAroundCharacters()
        drawHilightedRegion()
    End Sub

    Private Sub DrawBoxesAroundCharacters()
        Dim m As RectangleF

        For i = 0 To _lines.Count - 1
            For j = 0 To _lines(i).Count - 1
                m = GetCursorRect(j, i)
                m.X += Position.x
                m.Y += Position.y
                InkGraphics.drawRectangle(m, Color.Red)
            Next j
        Next i
    End Sub

    Private Sub DrawBox()
        Dim rect As New RectangleF(Position.toPointF, _clientSize)
        If Mode.isModeActivated(TextWriterMode.TextWriterMode_Enum.Editing) Then
            InkGraphics.drawRectangle(rect, Color.Orange)
        ElseIf Mode.isModeActivated(TextWriterMode.TextWriterMode_Enum.Mouse_Over) Then
            InkGraphics.drawRectangle(rect, Color.Yellow)
        End If
    End Sub

    Public Sub DrawCursor()
        If Mode.isModeActivated(TextWriterMode.TextWriterMode_Enum.Editing) Then
            Dim st As New Stroke2D()
            st.Add(GetCursorLocationInLocalSpace(_cursor.ColIndex, _cursor.RowIndex))
            st.Last.x += Position.x
            st.Last.y += Position.y
            st.Add(New TabletPoint2D(st.Last.x, st.Last.y + TextLineHeight, 1024))
            st.width = 2
            InkGraphics.GL_DrawLine(st, Color.White)
        End If
    End Sub

    Private Sub drawHilightedRegion()


        'If Mode.isModeActivated(TextWriterMode.TextWriterMode_Enum.Selecting) Then
        'Debug.Print("draw hilighted region")
        Dim m As RectangleF
            'Dim tr As RectangleF = GetCursorRect(__cursor_selStart.ColIndex, __cursor_selStart.RowIndex)
            Dim cpos As TKTextCursor

            If __strCursor_selLength < 0 Then
                For i = __strCursor_selStart + __strCursor_selLength To __strCursor_selStart - 1
                    cpos = StringLocToCursorLoc(i)
                    m = GetCursorRect(cpos.ColIndex, cpos.RowIndex)
                    m.Location = Drawing2D.pointFAdd(m.Location, Position.toPointF)
                    'tr = RectangleF.Union(tr, m)
                    InkGraphics.drawFilledRectangle(m, Color.Blue, 100)
                Next i
            Else
                For i = __strCursor_selStart To __strCursor_selStart + __strCursor_selLength - 1
                    cpos = StringLocToCursorLoc(i)
                    m = GetCursorRect(cpos.ColIndex, cpos.RowIndex)
                    m.Location = Drawing2D.pointFAdd(m.Location, Position.toPointF)
                    'tr = RectangleF.Union(tr, m)
                    InkGraphics.drawFilledRectangle(m, Color.Blue, 100)
                Next i
            End If
        'End If

    End Sub

    Public Sub HandleKeyPress(e As Windows.Forms.KeyEventArgs)

        If e.Shift = True Then
            Select Case e.KeyCode
                Case Keys.Left
                    setTextSelLength(__strCursor_selLength - 1)
                    RaiseEvent RequiresRedraw()
                Case Keys.Right
                    setTextSelLength(__strCursor_selLength + 1)
                    RaiseEvent RequiresRedraw()
                Case Keys.Up
                    setTextSelEnd(New TKTextCursor(_cursor.RowIndex - 1, _cursor.ColIndex))
                    '_cursor = StringLocToCursorLoc(_strCursor) 'to test...
                    RaiseEvent RequiresRedraw()
                Case Keys.Down
                    setTextSelEnd(New TKTextCursor(_cursor.RowIndex + 1, _cursor.ColIndex))
                    '_cursor = StringLocToCursorLoc(_strCursor) 'to test...
                    RaiseEvent RequiresRedraw()
                Case Keys.Home
                    setTextSelEnd(New TKTextCursor(_cursor.RowIndex, 0))
                    RaiseEvent RequiresRedraw()
                Case Keys.End
                    setTextSelEnd(New TKTextCursor(_cursor.RowIndex, _lines(_cursor.RowIndex).Length))
                    RaiseEvent RequiresRedraw()
            End Select
        Else
            Select Case e.KeyCode
                Case Keys.Left
                    setTextCursor(_strCursor - 1)
                    RaiseEvent RequiresRedraw()
                Case Keys.Right
                    setTextCursor(_strCursor + 1)
                    RaiseEvent RequiresRedraw()
                Case Keys.Up
                    setTextCursor(New TKTextCursor(_cursor.RowIndex - 1, _cursor.ColIndex))
                    '_cursor = StringLocToCursorLoc(_strCursor) 'to test...
                    RaiseEvent RequiresRedraw()
                Case Keys.Down
                    setTextCursor(New TKTextCursor(_cursor.RowIndex + 1, _cursor.ColIndex))
                    '_cursor.RowIndex += 1
                    '__strCursor_selLength = 0
                    'CleanUpCursor()
                    '_strCursor = CursorLocToStringLoc(_cursor)
                    '_cursor = StringLocToCursorLoc(_strCursor) 'to test...
                    RaiseEvent RequiresRedraw()
                Case Keys.Back
                    If Math.Abs(__strCursor_selLength) > 0 Then
                        deleteSelection()
                    ElseIf _strCursor > 0 Then
                        _strCursor -= 1
                        Text = Text.Remove(_strCursor, 1)
                        _cursor = StringLocToCursorLoc(_strCursor)
                        RaiseEvent RequiresRedraw()
                    End If
                Case Keys.Delete
                    If Math.Abs(__strCursor_selLength) > 0 Then
                        deleteSelection()
                    ElseIf _strCursor < Text.Length Then
                        Text = Text.Remove(_strCursor, 1)
                        _cursor = StringLocToCursorLoc(_strCursor)
                        RaiseEvent RequiresRedraw()
                    End If
                Case Keys.Home
                    setTextCursor(New TKTextCursor(_cursor.RowIndex, 0))
                    RaiseEvent RequiresRedraw()
                Case Keys.End
                    setTextCursor(New TKTextCursor(_cursor.RowIndex, _lines(_cursor.RowIndex).Length))
                    RaiseEvent RequiresRedraw()
            End Select
        End If
    End Sub

    Private Sub deleteSelection(Optional redraw As Boolean = True)
        If __strCursor_selLength < 0 Then
            Text = Text.Remove(__strCursor_selStart + __strCursor_selLength, -__strCursor_selLength)
            setTextCursor(_strCursor)
        Else
            Text = Text.Remove(__strCursor_selStart, __strCursor_selLength)
            setTextCursor(__strCursor_selStart)
        End If
        __strCursor_selLength = 0
        '_cursor = StringLocToCursorLoc(__strCursor_selStart)
        'setTextCursor(_cur)
        'CleanUpCursor()
        If redraw = True Then RaiseEvent RequiresRedraw()
    End Sub

    Public Function getSelectionText() As String
        If __strCursor_selLength < 0 Then
            Return Text.Substring(__strCursor_selStart + __strCursor_selLength, -__strCursor_selLength)
        Else
            Return Text.Substring(__strCursor_selStart, __strCursor_selLength)
        End If
    End Function

    Private Function StringLocToCursorLoc(strLoc As Integer) As TKTextCursor
        'We know our location, now, which line is it on?
        Dim tstr As String = Text
        Dim line As Integer = 0
        Dim col As Integer = 0

        If strLoc = 0 Then Return New TKTextCursor(0, 0)

        If strLoc >= tstr.Length Then
            Return New TKTextCursor(_lines.Count - 1, _lines.Last.Length)
        End If

        For i = 1 To strLoc
            If tstr(i - 1) = vbCr Then
                line += 1
                col = 0
            Else
                col += 1
            End If
        Next

        Return New TKTextCursor(line, col)

    End Function

    Private Function CursorLocToStringLoc(curLoc As TKTextCursor) As Integer
        'add the number of characters that make up each line plus one for each 

        Dim retLoc As Integer = 0
        For i = 0 To curLoc.RowIndex - 1 '_lines.Count - 2
            retLoc += _lines(i).Count + 1 '(the +1 is for the CR character)
        Next i

        retLoc += curLoc.ColIndex

        Return retLoc

    End Function

    Private Function isStandardChar(c As Char) As Boolean
        'If (c >= "a" And c <= "z") OrElse _
        '    (c >= "A" And c <= "Z") OrElse _
        '    (c >= "0" And c <= "9") OrElse _
        '    c = " " Or c = vbCr Then
        '    Return True
        'End If
        If (c >= Chr(32) And c <= Chr(126)) Or _
           (c >= Chr(128) And c <= Chr(255)) Or _
            c = vbCr Then
            Return True
        End If
        Return False
    End Function

    Public Sub HandleKeyPress(keyChar As Char)
        If isStandardChar(keyChar) = True Then
            If Math.Abs(__strCursor_selLength) > 0 Then
                deleteSelection(False)
            End If
            Text = Text.Insert(_strCursor, keyChar)
            setTextCursor(_strCursor + 1)
            CleanUpCursor()

            RaiseEvent RequiresRedraw()
        End If
    End Sub

    Public Sub HandleMouseDown(local_mouse_pos As PointF, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            SetCursorLocationToPoint(local_mouse_pos)
            Mode.addMode(TextWriterMode.TextWriterMode_Enum.Selecting)
        End If
    End Sub

    Public Sub HandleMouseMove(local_mouse_pos As PointF, e As MouseEventArgs)
        If Mode.isModeActivated(TextWriterMode.TextWriterMode_Enum.Selecting) Then
            setTextSelEnd(GetCursorLocationFromPoint(local_mouse_pos))
            RaiseEvent RequiresRedraw()
        End If
    End Sub

    Public Sub HandleMouseUp(local_mouse_pos As PointF, e As MouseEventArgs)
        Mode.removeMode(TextWriterMode.TextWriterMode_Enum.Selecting)
    End Sub

    Public Sub HandleMouseClick(local_mouse_pos As PointF)
        SetCursorLocationToPoint(local_mouse_pos)
    End Sub

    Private Sub SetCursorLocationToPoint(local_point As PointF)
        setTextCursor(GetCursorLocationFromPoint(local_point))
    End Sub

    Private Function GetCursorLocationFromPoint(local_point As PointF) As TKTextCursor
        Dim charRect As RectangleF
        Dim localTabPoint = New TabletPoint2D(local_point)

        Using gfx As Drawing.Graphics = Drawing.Graphics.FromImage(TextBitmap)
            For i = 0 To _lines.Count - 1
                charRect = GetCursorRect(0, i)
                'are we on the correct line?
                If Drawing2D.valueInRange(localTabPoint.y, charRect.Top, charRect.Bottom) Then

                    'If we're out left of the first region, return zero cursor pos on the line.
                    If (localTabPoint.x <= GetCursorRect(0, i).Left) Then
                        Return New TKTextCursor(i, 0)
                    End If

                    'if we're out right, past the end of the line, return the max cursor pos on the line.
                    charRect = GetCursorRect(_lines(i).Count - 1, i)
                    If (localTabPoint.x > charRect.Right) Then
                        Return New TKTextCursor(i, _lines(i).Count)
                    End If

                    For j = 0 To _lines(i).Count - 1
                        charRect = GetCursorRect(j, i)
                        If Drawing2D.isPointWithinRegion(localTabPoint, charRect) Then
                            'If Drawing2D.pointFSubtract(local_point, New PointF(charRect.Width, 0)).X < charRect.Width / 2 Then
                            'in left-hand half of character, choose previous cursor location.
                            Return New TKTextCursor(i, j)
                            'Else
                            'in right-hand half of character, choose next cursor location.
                            '_cursor = New TKTextCursor(i, j + 1)
                            'End If
                        End If
                    Next j

                End If
            Next i
        End Using

        Return New TKTextCursor(0, 0)
    End Function

    Private Sub CleanUpCursor()
        If _strCursor < 0 Then
            _strCursor = 0
        ElseIf _strCursor > Text.Length Then
            _strCursor = Text.Length
        End If

        If _cursor.RowIndex < 0 Then
            _cursor.RowIndex = 0
        ElseIf _cursor.RowIndex >= _lines.Count Then
            _cursor.RowIndex = _lines.Count - 1
        End If
        If _cursor.ColIndex < 0 Then
            _cursor.ColIndex = 0
        ElseIf _cursor.ColIndex > _lines(_cursor.RowIndex).Length Then
            _cursor.ColIndex = _lines(_cursor.RowIndex).Length
        End If
    End Sub

    Private Function CleanUpCursor(textCursorPos As Integer) As Integer
        If textCursorPos < 0 Then
            textCursorPos = 0
        ElseIf textCursorPos > Text.Length Then
            textCursorPos = Text.Length
        End If

        Return textCursorPos
    End Function

    Private Function CleanUpCursor(cursorPos As TKTextCursor) As TKTextCursor
        Dim retCurPos As New TKTextCursor(cursorPos.RowIndex, cursorPos.ColIndex)

        If retCurPos.RowIndex < 0 Then
            retCurPos.RowIndex = 0
        ElseIf retCurPos.RowIndex >= _lines.Count Then
            retCurPos.RowIndex = _lines.Count - 1
        End If

        If retCurPos.ColIndex < 0 Then
            retCurPos.ColIndex = 0
        ElseIf retCurPos.ColIndex > _lines(retCurPos.RowIndex).Length Then
            retCurPos.ColIndex = _lines(retCurPos.RowIndex).Length
        End If

        Return retCurPos
    End Function

    Private Function CountTrailingSpaces(str As String) As Integer
        Dim spaces As Integer = 0

        Dim i As Integer = 0
        For i = 0 To str.Length - 1
            If str(i) = " "c Then
                spaces += 1
            Else
                Exit For
            End If
        Next

        For j As Integer = str.Length - 1 To i + 1 Step -1
            If str(j) = " "c Then
                spaces += 1
            Else
                Exit For
            End If
        Next
        Return spaces
    End Function

    Private Function MeasureString(gfx As Drawing.Graphics, str As String, font As Font) As SizeF
        ' these are the times you hate MS..
        Dim size As SizeF = gfx.MeasureString(str, font)

        If (str.EndsWith(" ") OrElse (str.StartsWith(" "))) Then
            Dim spacesize As SizeF = gfx.MeasureString("! !", font)
            Dim spacesizemin As SizeF = gfx.MeasureString("!!", font)

            Dim spacewidth As Single = spacesize.Width - spacesizemin.Width

            Dim spaces As Integer = CountTrailingSpaces(str)

            size.Width += (spacewidth * CSng(spaces))
        End If

        'size.Width /= _textSizeToScreenSizeRatio
        'size.Height /= _textSizeToScreenSizeRatio

        Return size
    End Function
End Class

Public Enum TextWriterMode_Enum
    None = 0
    Mouse_Over = 1
    Editing = 2
    Selecting = 4
End Enum

'Public Structure CustomMode(Of T As Integer)


'    Public Shared Mode As T = 0

'    Public Sub addMode(m As T)
'        Mode = Mode Or m
'    End Sub

'    Public Sub removeMode(m As T)
'        Mode = Mode And Not (m)
'    End Sub

'    Public Sub clearModes()
'        Mode = 0
'    End Sub

'    Public Function isModeActivated(m As T) As Boolean
'        Return Mode And m
'    End Function
'End Structure
