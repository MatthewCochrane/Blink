<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.GlControl1 = New OpenTK.GLControl()
        Me.tmrRedraw = New System.Windows.Forms.Timer(Me.components)
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.lbUndoList = New System.Windows.Forms.ListBox()
        Me.chkLockTabSize = New System.Windows.Forms.CheckBox()
        Me.cbEraserType = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.nudEraserSize = New System.Windows.Forms.NumericUpDown()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ScreenTrackingToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LockTabletSizeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TabletAspectRatioToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TabletHardwareToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ScreenToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.nudPenSize = New System.Windows.Forms.NumericUpDown()
        Me.txtTest = New System.Windows.Forms.TextBox()
        Me.btnRecogniseSelection = New System.Windows.Forms.Button()
        Me.txtStrokeInterpret = New System.Windows.Forms.TextBox()
        Me.cbColor = New System.Windows.Forms.ComboBox()
        CType(Me.nudEraserSize, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip1.SuspendLayout()
        CType(Me.nudPenSize, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GlControl1
        '
        Me.GlControl1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GlControl1.BackColor = System.Drawing.Color.Black
        Me.GlControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.GlControl1.Location = New System.Drawing.Point(21, 74)
        Me.GlControl1.Name = "GlControl1"
        Me.GlControl1.Size = New System.Drawing.Size(662, 491)
        Me.GlControl1.TabIndex = 0
        Me.GlControl1.VSync = False
        '
        'tmrRedraw
        '
        Me.tmrRedraw.Enabled = True
        Me.tmrRedraw.Interval = 10
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(12, 37)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 1
        Me.Button1.Text = "Save"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(93, 37)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 2
        Me.Button2.Text = "Load"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'lbUndoList
        '
        Me.lbUndoList.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lbUndoList.FormattingEnabled = True
        Me.lbUndoList.Location = New System.Drawing.Point(689, 62)
        Me.lbUndoList.Name = "lbUndoList"
        Me.lbUndoList.Size = New System.Drawing.Size(127, 498)
        Me.lbUndoList.TabIndex = 4
        '
        'chkLockTabSize
        '
        Me.chkLockTabSize.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkLockTabSize.AutoSize = True
        Me.chkLockTabSize.Location = New System.Drawing.Point(583, 50)
        Me.chkLockTabSize.Name = "chkLockTabSize"
        Me.chkLockTabSize.Size = New System.Drawing.Size(100, 17)
        Me.chkLockTabSize.TabIndex = 6
        Me.chkLockTabSize.Text = "LockTabletSize"
        Me.chkLockTabSize.UseVisualStyleBackColor = True
        '
        'cbEraserType
        '
        Me.cbEraserType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append
        Me.cbEraserType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
        Me.cbEraserType.FormattingEnabled = True
        Me.cbEraserType.Items.AddRange(New Object() {"Stroke", "Point"})
        Me.cbEraserType.Location = New System.Drawing.Point(174, 48)
        Me.cbEraserType.Name = "cbEraserType"
        Me.cbEraserType.Size = New System.Drawing.Size(64, 21)
        Me.cbEraserType.TabIndex = 7
        Me.cbEraserType.Text = "Stroke"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(174, 32)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(67, 13)
        Me.Label1.TabIndex = 8
        Me.Label1.Text = "Eraser Type:"
        '
        'nudEraserSize
        '
        Me.nudEraserSize.DecimalPlaces = 2
        Me.nudEraserSize.Location = New System.Drawing.Point(244, 48)
        Me.nudEraserSize.Name = "nudEraserSize"
        Me.nudEraserSize.Size = New System.Drawing.Size(63, 20)
        Me.nudEraserSize.TabIndex = 9
        Me.nudEraserSize.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(241, 32)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(63, 13)
        Me.Label2.TabIndex = 10
        Me.Label2.Text = "Eraser Size:"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.ScreenTrackingToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(828, 24)
        Me.MenuStrip1.TabIndex = 11
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
        Me.FileToolStripMenuItem.Text = "File"
        '
        'ScreenTrackingToolStripMenuItem
        '
        Me.ScreenTrackingToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.LockTabletSizeToolStripMenuItem, Me.TabletAspectRatioToolStripMenuItem})
        Me.ScreenTrackingToolStripMenuItem.Name = "ScreenTrackingToolStripMenuItem"
        Me.ScreenTrackingToolStripMenuItem.Size = New System.Drawing.Size(103, 20)
        Me.ScreenTrackingToolStripMenuItem.Text = "Screen Tracking"
        '
        'LockTabletSizeToolStripMenuItem
        '
        Me.LockTabletSizeToolStripMenuItem.CheckOnClick = True
        Me.LockTabletSizeToolStripMenuItem.Name = "LockTabletSizeToolStripMenuItem"
        Me.LockTabletSizeToolStripMenuItem.Size = New System.Drawing.Size(176, 22)
        Me.LockTabletSizeToolStripMenuItem.Text = "Lock Tablet Size"
        '
        'TabletAspectRatioToolStripMenuItem
        '
        Me.TabletAspectRatioToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.TabletHardwareToolStripMenuItem, Me.ScreenToolStripMenuItem})
        Me.TabletAspectRatioToolStripMenuItem.Name = "TabletAspectRatioToolStripMenuItem"
        Me.TabletAspectRatioToolStripMenuItem.Size = New System.Drawing.Size(176, 22)
        Me.TabletAspectRatioToolStripMenuItem.Text = "Tablet Aspect Ratio"
        '
        'TabletHardwareToolStripMenuItem
        '
        Me.TabletHardwareToolStripMenuItem.Name = "TabletHardwareToolStripMenuItem"
        Me.TabletHardwareToolStripMenuItem.Size = New System.Drawing.Size(161, 22)
        Me.TabletHardwareToolStripMenuItem.Text = "Tablet Hardware"
        '
        'ScreenToolStripMenuItem
        '
        Me.ScreenToolStripMenuItem.Name = "ScreenToolStripMenuItem"
        Me.ScreenToolStripMenuItem.Size = New System.Drawing.Size(161, 22)
        Me.ScreenToolStripMenuItem.Text = "Screen"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(310, 32)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(52, 13)
        Me.Label3.TabIndex = 13
        Me.Label3.Text = "Pen Size:"
        '
        'nudPenSize
        '
        Me.nudPenSize.DecimalPlaces = 2
        Me.nudPenSize.Location = New System.Drawing.Point(313, 48)
        Me.nudPenSize.Name = "nudPenSize"
        Me.nudPenSize.Size = New System.Drawing.Size(63, 20)
        Me.nudPenSize.TabIndex = 12
        Me.nudPenSize.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'txtTest
        '
        Me.txtTest.Location = New System.Drawing.Point(382, 47)
        Me.txtTest.Multiline = True
        Me.txtTest.Name = "txtTest"
        Me.txtTest.Size = New System.Drawing.Size(100, 20)
        Me.txtTest.TabIndex = 14
        '
        'btnRecogniseSelection
        '
        Me.btnRecogniseSelection.Location = New System.Drawing.Point(689, 33)
        Me.btnRecogniseSelection.Name = "btnRecogniseSelection"
        Me.btnRecogniseSelection.Size = New System.Drawing.Size(127, 23)
        Me.btnRecogniseSelection.TabIndex = 15
        Me.btnRecogniseSelection.Text = "Recognise Selection"
        Me.btnRecogniseSelection.UseVisualStyleBackColor = True
        '
        'txtStrokeInterpret
        '
        Me.txtStrokeInterpret.Location = New System.Drawing.Point(382, 21)
        Me.txtStrokeInterpret.Name = "txtStrokeInterpret"
        Me.txtStrokeInterpret.Size = New System.Drawing.Size(301, 20)
        Me.txtStrokeInterpret.TabIndex = 16
        '
        'cbColor
        '
        Me.cbColor.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append
        Me.cbColor.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
        Me.cbColor.FormattingEnabled = True
        Me.cbColor.Items.AddRange(New Object() {"White", "Red", "Green", "Blue", "Yellow", "Gray", "Cyan", "Pink", "Orange"})
        Me.cbColor.Location = New System.Drawing.Point(488, 47)
        Me.cbColor.Name = "cbColor"
        Me.cbColor.Size = New System.Drawing.Size(64, 21)
        Me.cbColor.TabIndex = 17
        Me.cbColor.Text = "White"
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(828, 577)
        Me.Controls.Add(Me.cbColor)
        Me.Controls.Add(Me.txtStrokeInterpret)
        Me.Controls.Add(Me.btnRecogniseSelection)
        Me.Controls.Add(Me.txtTest)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.nudPenSize)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.nudEraserSize)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cbEraserType)
        Me.Controls.Add(Me.chkLockTabSize)
        Me.Controls.Add(Me.lbUndoList)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.GlControl1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "frmMain"
        Me.Text = "blInk"
        CType(Me.nudEraserSize, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        CType(Me.nudPenSize, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GlControl1 As OpenTK.GLControl
    Friend WithEvents tmrRedraw As System.Windows.Forms.Timer
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents lbUndoList As System.Windows.Forms.ListBox
    Friend WithEvents chkLockTabSize As System.Windows.Forms.CheckBox
    Friend WithEvents cbEraserType As System.Windows.Forms.ComboBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents nudEraserSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
    Friend WithEvents FileToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ScreenTrackingToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents LockTabletSizeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents TabletAspectRatioToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents TabletHardwareToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ScreenToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents nudPenSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents txtTest As System.Windows.Forms.TextBox
    Friend WithEvents btnRecogniseSelection As Button
    Friend WithEvents txtStrokeInterpret As TextBox
    Friend WithEvents cbColor As ComboBox
End Class
