Imports VBTablet

Public Enum PenType
    None = 0
    Pen = 1
    Eraser = 2
End Enum

Public Class HWTablet
    Private WithEvents Tab As VBTablet.Tablet
    Private attachedControl As Control
    Private screenDrawingRegion As Rectangle
    Private _tabletCursorCaptured As Boolean
    Private tablet_Space As SizeF
    Private toRegion As RectangleF

    Private CursorState() As StylusCursorState
    Private MaxTapTime As Integer = 400 'in ms
    Private MaxLongTapTime As Integer = 1000 'in ms

    Public Event PenDown(position As TabletPoint2D, buttons As Integer, cursorID As PenType)
    Public Event PenUp(position As TabletPoint2D, buttons As Integer, cursorID As PenType)
    Public Event PenTap(position As TabletPoint2D, cursorID As PenType)
    Public Event PenLongTap(position As TabletPoint2D, cursorID As PenType)

    Public Event ButtonDown(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType)
    Public Event ButtonUp(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType)
    Public Event ButtonClick(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType)
    Public Event ButtonLongClick(position As TabletPoint2D, eventButton As Integer, zDist As Integer, cursorID As PenType)

    Public Event Draw(position As TabletPoint2D, buttons As Integer, cursorID As PenType)
    Public Event Hover(position As TabletPoint2D, buttons As Integer, zDist As Integer, cursorID As PenType)
    Public Event PenLeftProximity()
    Public Event PenEnteredProximity()


    Public ReadOnly Property ScreenSpace As SizeF
        Get
            Return attachedControl.Size
        End Get
    End Property

    Public ReadOnly Property TabletSpace As SizeF
        Get
            Return New Size(Tab.Device.X.Max, Tab.Device.Y.Max)
        End Get
    End Property

    Public ReadOnly Property TabletCursorCaptured
        Get
            Return _tabletCursorCaptured
        End Get
    End Property

    Public Sub New(control As Control)
        attachedControl = control

        Tab = New VBTablet.Tablet()
        Tab.PktGranularity = 2
        Tab.UnavailableIsError = False
        'AddHandler Tab.PacketArrival, AddressOf myTablet_PacketArrival

        Tab.AddContext("FirstContext")
        Tab.SelectContext("FirstContext")
        Tab.hWnd = control.Handle
        Tab.Connected = True
        Tab.Context.QueueSize = 32

        Tab.Context.TrackingMode = True
        Tab.Context.Enabled = True

        ReDim CursorState(Tab.Device.NumCursorTypes + Tab.Device.FirstCursor)
        For i As Integer = Tab.Device.FirstCursor To Tab.Device.NumCursorTypes
            CursorState(i) = New StylusCursorState(Tab.Cursor)
        Next i

    End Sub

    Public Sub CapturePointer(capture As Boolean)
        Static origSystemTabletSpace As SizeF

        If capture = True Then
            'capture cursor
            With Tab.Context
                origSystemTabletSpace = New SizeF(.OutputExtentX, .OutputExtentY)
                tablet_Space = New Size(Tab.Device.X.Max, Tab.Device.Y.Max)

                .Options.IsSystemCtx = False
                .OutputExtentX = tablet_Space.Width
                .OutputExtentY = tablet_Space.Height
                .Update()
            End With
            _tabletCursorCaptured = True
        Else
            'release cursor
            With Tab.Context
                tablet_Space = origSystemTabletSpace

                .Options.IsSystemCtx = True
                .OutputExtentX = tablet_Space.Width
                .OutputExtentY = tablet_Space.Height
                .Update()
            End With
            _tabletCursorCaptured = False
        End If
    End Sub

    Private Function MapStylusOutputToControl(p As TabletPoint2D) As TabletPoint2D
        'Return Drawing2D.mapPointToRegionInSpace(p, tabletSpace, toRegion)
        Return Drawing2D.mapPointToSpace(Drawing2D.invertAxis(p, tablet_Space, False, True), tablet_Space, attachedControl.Size)
    End Function

    Private Sub Tab_PacketArrival(ByRef ContextHandle As System.IntPtr, ByRef CursorID As Integer, _
                                   ByRef X As Integer, ByRef Y As Integer, ByRef Z As Integer, _
                                   ByRef Buttons As Integer, ByRef NormalPressure As Integer, _
                                   ByRef TangentPressure As Integer, ByRef Azimuth As Integer, ByRef Altitude As Integer, _
                                   ByRef Twist As Integer, ByRef Pitch As Integer, ByRef Roll As Integer, _
                                   ByRef Yaw As Integer, ByRef PacketSerial As Integer, ByRef PacketTime As Integer) Handles Tab.PacketArrival
        'Debug.Print("Received Packet " & vbNewLine & _
        '             "X: " & X & ", Y: " & Y & ", Z: " & Z & vbNewLine & _
        '             "Buttons: " & Buttons & ", NormalPressure: " & NormalPressure & vbNewLine & _
        '             "TangentPressure: " & TangentPressure & ", Azimuth: " & Azimuth & ", Altitude: " & Altitude & vbNewLine & _
        '             "Twist: " & Twist & ", Roll: " & Roll & ", Yaw: " & Yaw & vbNewLine & _
        '             "PacketSerial: " & PacketSerial & ", PacketTime: " & PacketTime & ", Cursor: " & CursorID)
        'yep this has to handle everything..!

        Dim p As TabletPoint2D = MapStylusOutputToControl(New TabletPoint2D(X, Y, NormalPressure))

        'here's the magic...

        If CBool(Buttons And 1 << CursorState(CursorID).cursorObj.NormalPressureButton) <> CBool(CursorState(CursorID).Buttons And 1 << CursorState(CursorID).cursorObj.NormalPressureButton) Then
            'Normal pressure button has changd state.
            If CBool(Buttons And 1 << CursorState(CursorID).cursorObj.NormalPressureButton) = True Then
                'pen is now touching the tablet
                RaiseEvent PenDown(p, Buttons, CursorID)
            Else
                'pen has been released from tablet
                RaiseEvent PenUp(p, Buttons, CursorID)

                'If the time beweeen the down and up events are close enough, raise a PenTap event.
                Dim tapDuration As Integer = PacketTime - CursorState(CursorID).ButtonLastEventTime(CursorState(CursorID).cursorObj.NormalPressureButton)
                If tapDuration < MaxTapTime Then
                    RaiseEvent PenTap(p, CursorID)
                ElseIf tapDuration < MaxLongTapTime Then
                    RaiseEvent PenLongTap(p, CursorID)
                End If
            End If
            CursorState(CursorID).ButtonLastEventTime(CursorState(CursorID).cursorObj.NormalPressureButton) = PacketTime
        End If

        For i As Integer = 0 To CursorState(CursorID).cursorObj.NumButtons - 1
            If i <> CursorState(CursorID).cursorObj.NormalPressureButton Then
                If CBool(Buttons And 1 << i) <> CBool(CursorState(CursorID).Buttons And 1 << i) Then
                    'Normal pressure button has changd state.
                    If CBool(Buttons And 1 << i) = True Then
                        'Button is in pressed state
                        RaiseEvent ButtonDown(p, i, Z, CursorID)
                    Else
                        'Button is not in pressed state
                        RaiseEvent ButtonUp(p, i, Z, CursorID)

                        'If the time beweeen the down and up events are close enough, raise a Click event.
                        Dim tapDuration As Integer = PacketTime - CursorState(CursorID).ButtonLastEventTime(i)
                        If tapDuration < MaxTapTime Then
                            RaiseEvent ButtonClick(p, i, Z, CursorID)
                        ElseIf tapDuration < MaxLongTapTime Then
                            RaiseEvent ButtonLongClick(p, i, Z, CursorID)
                        End If
                    End If
                    CursorState(CursorID).ButtonLastEventTime(i) = PacketTime
                End If
            End If
        Next

        If CBool(Buttons And 1 << CursorState(CursorID).cursorObj.NormalPressureButton) = True Then
            RaiseEvent Draw(p, Buttons, CursorID)
        Else
            RaiseEvent Hover(p, Buttons, Z, CursorID)
        End If

        CursorState(CursorID).Buttons = Buttons

    End Sub

    Private Sub Tab_ProximityChange(ByRef IsInContext As Boolean, ByRef IsPhysical As Boolean, ByRef ContextHandle As IntPtr, ByRef ContextName As String) Handles Tab.ProximityChange
        If IsInContext = False Then
            RaiseEvent PenLeftProximity()
        Else
            RaiseEvent PenEnteredProximity()
        End If
    End Sub

    Private Sub ErrHandle()
        If Err.Number = (vbObjectError Or 19997) Then
            'Debug.Print "One or more attributes are not available. Oh well."
        ElseIf Err.Number = (vbObjectError Or 19998) Then
            MsgBox("The WinTab DLL (wintab32.dll) was not found. Please install a WinTab-compatable driver.")
            'GoTo bigerror
        ElseIf Err.Number = (vbObjectError Or 19999) Then
            MsgBox("The WinTab DLL reports that no devices are avaliable. Please turn on/connect a WinTab-compatable device.")
            'GoTo bigerror
        ElseIf Err.Number = 380 Then
            'An invalid property has been set. This is probably prgZ.value, for which 0 is invalid. Ignore.
        ElseIf Err.Number = 429 Then
            MsgBox("An ActiveX control cannot be loaded - most likely, VBTablet.dll has not been correctly installed and registered. TabletDemo cannot continue.", MsgBoxStyle.Critical)
            End
        Else
            MsgBox("Error in Form_Load:" & vbNewLine & "Number: " & Err.Number & vbNewLine & Err.Description)
        End If
    End Sub
End Class

Public Class StylusCursorState
    Public ContextHandle As System.IntPtr
    Public CursorID As Integer
    Public Position As Point
    'Public X As Integer
    'Public Y As Integer
    Public Z As Integer
    Public Buttons As Integer = 0
    Public ButtonLastEventTime() As Integer
    Public NormalPressure As Integer
    Public TangentPressure As Integer
    Public Azimuth As Integer
    Public Altitude As Integer
    Public Twist As Integer
    Public Pitch As Integer
    Public Roll As Integer
    Public Yaw As Integer

    Public cursorObj As TabletCursor

    Public Sub New(buttons As Integer)
        CreateButtonStates(buttons - 1)
    End Sub

    Public Sub New(cur As TabletCursor)
        CreateButtonStates(cur.NumButtons - 1)
        cursorObj = cur
        CursorID = cur.Index
        Position = New Point(0, 0)
        'cur.AvailableData -> can use this to determine what data is available to us...!
    End Sub

    Private Sub CreateButtonStates(num As Integer)
        ReDim ButtonLastEventTime(num)

        For i = 0 To num
            ButtonLastEventTime(i) = 0
        Next
    End Sub
End Class