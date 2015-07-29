Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL
'Imports System.Drawing.Imaging

Public Class InkGraphics
    Private _loaded As Boolean = False
    Private cursor_tex_source As Bitmap
    Private cursor_tex As Integer
    Private WithEvents tI As TabletInterpreter
    Public GLCtrl As GLControl
    Public selectRect As RectangleF = Nothing

    Public WithEvents tmpText As TKTextWriter

    Private Sub tmptext_RequiresRedraw() Handles tmpText.RequiresRedraw
        DrawAll()
    End Sub

    Public Sub New(_tI As TabletInterpreter, _GLCtrl As GLControl)
        tI = _tI
        GLCtrl = _GLCtrl

        GL.ClearColor(Color.Black)

        'Generate empty texture
        cursor_tex = LoadTexture(My.Resources.cursor)

        'Enable textures from texture2d target
        GL.Enable(EnableCap.Texture2D)
        'Basically enables the alpha channel to be used in the color buffer
        GL.Enable(EnableCap.Blend)
        'The operation/order to blend
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha)
        'Use for pixel depth comparing before storing in the depth buffer
        'GL.Enable(EnableCap.DepthTest)
        'GL.Viewport(GLCtrl.ClientRectangle)

        tmpText = New TKTextWriter(New TabletPoint2D(50, 0, 0), New Size(200, 20), , , Brushes.Yellow)
        tmpText.AddLine("Hello World", New PointF(0, 0))
        'tmpText.AddLine("aaa", New PointF(0, 0), Brushes.White)
        tmpText.UpdateSize()

        _loaded = True
    End Sub

    Public ReadOnly Property loaded
        Get
            Return _loaded
        End Get
    End Property

    Public Sub DrawAll()
        GL.MatrixMode(MatrixMode.Projection)

        GL.LoadIdentity()
        GL.Ortho(tI.pageData.userViewPoint.Left, tI.pageData.userViewPoint.Right, tI.pageData.userViewPoint.Bottom, tI.pageData.userViewPoint.Top, 0, 1)
        GL.MatrixMode(MatrixMode.Projection) 'MatrixMode.Modelview)
        GL.Disable(EnableCap.DepthTest)
        GL.Disable(EnableCap.Texture2D)

        GL.Clear(ClearBufferMask.ColorBufferBit)

        Try
            For Each st As Stroke2D In tI.pageData.strokes
                If st.Count > 1 Then
                    If st.isSelected = False Then
                        If Drawing2D.isPointOnStrokeWithinRegion(st, tI.pageData.userViewPoint) Then
                            GL_DrawLine(st, st.color) 'Color.White)
                        End If
                    Else
                        If Drawing2D.isPointOnStrokeWithinRegion(st, tI.pageData.userViewPoint) Then
                            GL_DrawLine(st, Color.Green) 'New Color4(1 - st.color.R, 1 - st.color.G, 1 - st.color.B, st.color.A)) 'Color.Green)
                        End If
                    End If
                End If
            Next

            If Not IsNothing(selectRect) Then drawRectangle(selectRect, Color.LightGray, , , DashStyle.Dots)

            'GL_DrawCircleFill(tI.pageData.cursor.x, tI.pageData.cursor.y, 1, Color.Red)
            'GL.Ortho(0, GlControl1.Width, GlControl1.Height, 0, 0, 1)
            'DrawTexture(New RectangleF(tI.pageData.cursor.x, tI.pageData.cursor.y, My.Resources.cursor.Width, My.Resources.cursor.Height), cursor_tex)
            drawRectangle(tI.pageData.tabletViewPoint, Color.Red)

            'drawRectangle(New RectangleF(0, 0, 100, 100), Color.Yellow)

            DrawCursor()

            tmpText.Draw()

        Catch ex As Exception
            'Stop
            Exit Sub
        End Try

        'Debug.Print(StylusPlugin.Strokes.Count)
        GraphicsContext.CurrentContext.VSync = True 'Caps frame rate as to not over run GPU
        GLCtrl.SwapBuffers() 'Takes from the 'GL' and puts into control
    End Sub

    Private Function LoadTexture(path As String, Optional quality As Integer = 0, Optional repeat As Boolean = False, Optional flip_y As Boolean = False) As Integer
        Dim bitmap As New Bitmap(path)
        Return LoadTexture(bitmap, quality, repeat, flip_y)
    End Function

    Private Function LoadTexture(bitmap As Bitmap, Optional quality As Integer = 0, Optional repeat As Boolean = False, Optional flip_y As Boolean = False) As Integer
        'Dim bitmap As New Bitmap(path)

        'Flip the image
        If flip_y Then
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY)
        End If

        'Generate a new texture target in gl
        Dim texture As Integer = GL.GenTexture()

        'Will bind the texture newly/empty created with GL.GenTexture
        'All gl texture methods targeting Texture2D will relate to this texture
        GL.BindTexture(TextureTarget.Texture2D, texture)

        'The reason why your texture will show up glColor without setting these parameters is actually
        'TextureMinFilters fault as its default is NearestMipmapLinear but we have not established mipmapping
        'We are only using one texture at the moment since mipmapping is a collection of textures pre filtered
        'I'm assuming it stops after not having a collection to check.
        If quality <> 1 And quality <> 0 Then quality = 0 'ie, default is 0 - low quality
        Select Case quality
            Case 0
                'Low quality
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, CInt(All.Linear))
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, CInt(All.Linear))
                Exit Select
            Case 1
                'High quality
                'This is in my opinion the best since it doesnt average the result and not blurred to shit
                'but most consider this low quality...
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, CInt(All.Nearest))
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, CInt(All.Nearest))
                Exit Select
        End Select

        If repeat Then
            'This will repeat the texture past its bounds set by TexImage2D
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, CInt(All.Repeat))
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, CInt(All.Repeat))
        Else
            'This will clamp the texture to the edge, so manipulation will result in skewing
            'It can also be useful for getting rid of repeating texture bits at the borders
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, CInt(All.ClampToEdge))
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, CInt(All.ClampToEdge))
        End If

        'Creates a definition of a texture object in opengl
        ' Parameters
        '     * Target - Since we are using a 2D image we specify the target Texture2D
        '     * MipMap Count / LOD - 0 as we are not using mipmapping at the moment
        '     * InternalFormat - The format of the gl texture, Rgba is a base format it works all around
        '     * Width;
        '     * Height;
        '     * Border - must be 0;
        '     * 
        '     * Format - this is the images format not gl's the format Bgra i believe is only language specific
        '     *          C# uses little-endian so you have ARGB on the image A 24 R 16 G 8 B, B is the lowest
        '     *          So it gets counted first, as with a language like Java it would be PixelFormat.Rgba
        '     *          since Java is big-endian default meaning A is counted first.
        '     *          but i could be wrong here it could be cpu specific :P
        '     *          
        '     * PixelType - The type we are using, eh in short UnsignedByte will just fill each 8 bit till the pixelformat is full
        '     *             (don't quote me on that...)
        '     *             you can be more specific and say for are RGBA to little-endian BGRA -> PixelType.UnsignedInt8888Reversed
        '     *             this will mimic are 32bit uint in little-endian.
        '     *             
        '     * Data - No data at the moment it will be written with TexSubImage2D
        '     

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0,
            PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero)

        'Load the data from are loaded image into virtual memory so it can be read at runtime
        Dim bitmap_data As System.Drawing.Imaging.BitmapData = bitmap.LockBits(New Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.[ReadOnly], System.Drawing.Imaging.PixelFormat.Format32bppArgb)

        'Writes data to are texture target
        ' Target;
        '     * MipMap;
        '     * X Offset - Offset of the data on the x axis
        '     * Y Offset - Offset of the data on the y axis
        '     * Width;
        '     * Height;
        '     * Format;
        '     * Type;
        '     * Data - Now we have data from the loaded bitmap image we can load it into are texture data
        '     

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bitmap.Width, bitmap.Height,
            PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0)

        'Release from memory
        bitmap.UnlockBits(bitmap_data)

        'get rid of bitmap object its no longer needed in this method
        bitmap.Dispose()

        'Binding to 0 is telling gl to use the default or null texture target
        '    *This is useful to remember as you may forget that a texture is targeted
        '    *And may overflow to functions that you dont necessarily want to
        '    *Say you bind a texture
        '    *
        '    * Bind(Texture);
        '    * DrawObject1();
        '    *                <-- Insert Bind(NewTexture) or Bind(0)
        '    * DrawObject2();
        '    * 
        '    * Object2 will use Texture if not set to 0 or another.
        '    

        GL.BindTexture(TextureTarget.Texture2D, 0)

        Return texture
    End Function

    Shared Sub GL_DrawCircleFill(x, y, radius, color)
        GL.Begin(BeginMode.TriangleFan)

        GL.Color3(color)
        GL.Vertex2(x, y)

        For angle = 0 To 360 Step 90
            GL.Vertex2(x + Math.Sin(angle) * radius, y + Math.Cos(angle) * radius)
        Next

        GL.End()
    End Sub

    Shared Sub GL_DrawLine(stroke As Stroke2D, col As Color)
        If stroke.quality = Stroke2D_Quality.DrawBoundingBox Then
            drawRectangle(stroke.range, col)
        Else
            GL.LineWidth(stroke.width)
            GL.Begin(BeginMode.LineStrip)

            GL.Color4(col)

            Dim cnt As Integer = stroke.Count
            Dim p As TabletPoint2D

            'all but the last point
            For i = 0 To cnt - 2 Step stroke.quality
                p = stroke(i)

                GL.Color4(col.R, col.G, col.B, p.pressure / 256)
                GL.Vertex2(p.x, p.y)
            Next

            'now draw the last point
            p = stroke(cnt - 1)
            GL.Color4(col.R, col.G, col.B, p.pressure / 256)
            GL.Vertex2(p.x, p.y)

            GL.End()

            'For i = 0 To cnt - 2 Step stroke.quality
            '    p = stroke(i)

            '    GL_DrawCircleFill(p.x, p.y, 1, col)
            'Next
        End If
    End Sub

    Public Enum DashStyle As UShort
        Solid = &HFFFF
        Dots = &HAAAA
    End Enum

    Public Shared Sub drawRectangle(rectangle As RectangleF, colour As Color, Optional width As Single = 1, Optional lineOpacity As Byte = 255, Optional dashStyle As DashStyle = DashStyle.Solid)

        GL.LineWidth(width)

        If dashStyle <> dashStyle.Solid Then
            GL.LineStipple(1, dashStyle)
            GL.Enable(EnableCap.LineStipple)
        End If

        GL.Begin(BeginMode.LineStrip)

        GL.Color4(colour.R, colour.G, colour.B, lineOpacity)

        GL.Vertex2(rectangle.Left, rectangle.Top)
        GL.Vertex2(rectangle.Left, rectangle.Bottom)
        GL.Vertex2(rectangle.Right, rectangle.Bottom)
        GL.Vertex2(rectangle.Right, rectangle.Top)
        GL.Vertex2(rectangle.Left, rectangle.Top)

        GL.End()

        If dashStyle <> dashStyle.Solid Then GL.Disable(EnableCap.LineStipple)

    End Sub

    Public Shared Sub drawFilledRectangle(rectangle As RectangleF, fillColour As Color, Optional Opacity As Byte = 255)

        GL.Enable(EnableCap.Blend)
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha)
        GL.Begin(BeginMode.Quads)

        GL.Color4(fillColour.R, fillColour.G, fillColour.B, Opacity)

        GL.Vertex2(rectangle.Left, rectangle.Top)
        GL.Vertex2(rectangle.Left, rectangle.Bottom)
        GL.Vertex2(rectangle.Right, rectangle.Bottom)
        GL.Vertex2(rectangle.Right, rectangle.Top)
        GL.Vertex2(rectangle.Left, rectangle.Top)

        GL.End()

    End Sub

    Public Sub DrawTexture(rect As RectangleF, texId As Integer)
        GL.BindTexture(TextureTarget.Texture2D, texId)
        GL.Enable(EnableCap.Texture2D)
        GL.Begin(BeginMode.Quads)

        GL.Color3(Color.White)

        'Bind texture coordinates to vertices in ccw order

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

        GL.End()

        GL.Disable(EnableCap.Texture2D)

        GL.BindTexture(TextureTarget.Texture2D, 0)

    End Sub

    Public Sub DrawCursor()
        Select Case tI.cursorType
            Case PenType.None
                Exit Sub
            Case PenType.Pen
                Dim curSize As SizeF = tI.mapScreenSizeToWorldSize(New SizeF(1, 1))
                'curSize.Width /= 20
                'curSize.Height /= 20

                'DrawTexture(New RectangleF(tI.pageData.cursor.toPointF, curSize), cursor_tex)

                GL_DrawCircleFill(tI.pageData.cursor.x, tI.pageData.cursor.y, curSize.Width, Color.White)

            Case PenType.Eraser
                Dim curSize As SizeF = tI.mapScreenSizeToWorldSize(New SizeF(tI.eraserSize, tI.eraserSize))
                'curSize.Width /= 20
                'curSize.Height /= 20

                'DrawTexture(New RectangleF(tI.pageData.cursor.toPointF, curSize), cursor_tex)
                drawRectangle(New RectangleF(tI.pageData.cursor.toPointF, curSize), Color.Red)

            Case Else
                Dim curSize As SizeF = tI.mapScreenSizeToWorldSize(New SizeF(My.Resources.cursor.Width, My.Resources.cursor.Height))
                curSize.Width /= 20
                curSize.Height /= 20

                DrawTexture(New RectangleF(tI.pageData.cursor.toPointF, curSize), cursor_tex)
        End Select
    End Sub
End Class



Public Class customGLControl
    Inherits GLControl

    Public Sub New()
        MyBase.New(New GraphicsMode(32, 24, 8, 4))
    End Sub

    Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
        Return True
        'If keyData = Keys.Left OrElse keyData = Keys.Right OrElse keyData = Keys.Up OrElse keyData = Keys.Down OrElse keyData = Keys.Shift OrElse keyData = Keys.ShiftKey Then
        '    Return True
        'Else
        '    Return MyBase.IsInputKey(keyData)
        'End If
    End Function
End Class