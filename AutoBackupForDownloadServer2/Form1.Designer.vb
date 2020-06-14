<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveSettingToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CloseToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OptionToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ChangeDestinationDriveBufferSizeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CheckLogToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.StartAtStartupToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.WaitForComputerReadyToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VersionInfoToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.ToolStripStatusLabel1 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.ToolStripProgressBar1 = New System.Windows.Forms.ToolStripProgressBar()
        Me.btnStart = New System.Windows.Forms.Button()
        Me.btnStop = New System.Windows.Forms.Button()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.chbMinimizedTray = New System.Windows.Forms.CheckBox()
        Me.chbAutoStartTransfer = New System.Windows.Forms.CheckBox()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.cbNetworkAdaptor = New System.Windows.Forms.ComboBox()
        Me.lblHours = New System.Windows.Forms.Label()
        Me.txtHourInt = New System.Windows.Forms.TextBox()
        Me.chbTransferMonitor = New System.Windows.Forms.CheckBox()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.btnConnectLog = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.txtDestIP = New System.Windows.Forms.TextBox()
        Me.chbPSclient = New System.Windows.Forms.CheckBox()
        Me.txtPScheckInterval = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.chbPowerServer = New System.Windows.Forms.CheckBox()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.btnTest = New System.Windows.Forms.Button()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.FolderBrowserDialog1 = New System.Windows.Forms.FolderBrowserDialog()
        Me.lstStatus = New System.Windows.Forms.ListView()
        Me.TransferTimer = New System.Windows.Forms.Timer(Me.components)
        Me.btnAddTransferDir = New System.Windows.Forms.Button()
        Me.btnRemovedSelectedDir = New System.Windows.Forms.Button()
        Me.DataGridView1 = New System.Windows.Forms.DataGridView()
        Me.Status = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Original_Directory = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Destination_Directory = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.NotifyIcon1 = New System.Windows.Forms.NotifyIcon(Me.components)
        Me.WaitForComputerReadyTimer = New System.Windows.Forms.Timer(Me.components)
        Me.TimeCounter = New System.Windows.Forms.Timer(Me.components)
        Me.lstConnLog = New System.Windows.Forms.ListBox()
        Me.MenuStrip1.SuspendLayout()
        Me.StatusStrip1.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.OptionToolStripMenuItem, Me.AboutToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(1074, 24)
        Me.MenuStrip1.TabIndex = 0
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SaveSettingToolStripMenuItem, Me.CloseToolStripMenuItem})
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
        Me.FileToolStripMenuItem.Text = "&File"
        '
        'SaveSettingToolStripMenuItem
        '
        Me.SaveSettingToolStripMenuItem.Name = "SaveSettingToolStripMenuItem"
        Me.SaveSettingToolStripMenuItem.Size = New System.Drawing.Size(138, 22)
        Me.SaveSettingToolStripMenuItem.Text = "&Save Setting"
        '
        'CloseToolStripMenuItem
        '
        Me.CloseToolStripMenuItem.Name = "CloseToolStripMenuItem"
        Me.CloseToolStripMenuItem.Size = New System.Drawing.Size(138, 22)
        Me.CloseToolStripMenuItem.Text = "&Close"
        '
        'OptionToolStripMenuItem
        '
        Me.OptionToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ChangeDestinationDriveBufferSizeToolStripMenuItem, Me.CheckLogToolStripMenuItem, Me.StartAtStartupToolStripMenuItem, Me.WaitForComputerReadyToolStripMenuItem})
        Me.OptionToolStripMenuItem.Name = "OptionToolStripMenuItem"
        Me.OptionToolStripMenuItem.Size = New System.Drawing.Size(56, 20)
        Me.OptionToolStripMenuItem.Text = "&Option"
        '
        'ChangeDestinationDriveBufferSizeToolStripMenuItem
        '
        Me.ChangeDestinationDriveBufferSizeToolStripMenuItem.Name = "ChangeDestinationDriveBufferSizeToolStripMenuItem"
        Me.ChangeDestinationDriveBufferSizeToolStripMenuItem.Size = New System.Drawing.Size(266, 22)
        Me.ChangeDestinationDriveBufferSizeToolStripMenuItem.Text = "&Change Destination Drive Buffer Size"
        '
        'CheckLogToolStripMenuItem
        '
        Me.CheckLogToolStripMenuItem.Name = "CheckLogToolStripMenuItem"
        Me.CheckLogToolStripMenuItem.Size = New System.Drawing.Size(266, 22)
        Me.CheckLogToolStripMenuItem.Text = "C&heck Log"
        '
        'StartAtStartupToolStripMenuItem
        '
        Me.StartAtStartupToolStripMenuItem.CheckOnClick = True
        Me.StartAtStartupToolStripMenuItem.Name = "StartAtStartupToolStripMenuItem"
        Me.StartAtStartupToolStripMenuItem.Size = New System.Drawing.Size(266, 22)
        Me.StartAtStartupToolStripMenuItem.Text = "Start At Startup"
        '
        'WaitForComputerReadyToolStripMenuItem
        '
        Me.WaitForComputerReadyToolStripMenuItem.CheckOnClick = True
        Me.WaitForComputerReadyToolStripMenuItem.Name = "WaitForComputerReadyToolStripMenuItem"
        Me.WaitForComputerReadyToolStripMenuItem.Size = New System.Drawing.Size(266, 22)
        Me.WaitForComputerReadyToolStripMenuItem.Text = "Wait For Computer Ready"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.VersionInfoToolStripMenuItem})
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(52, 20)
        Me.AboutToolStripMenuItem.Text = "&About"
        '
        'VersionInfoToolStripMenuItem
        '
        Me.VersionInfoToolStripMenuItem.Name = "VersionInfoToolStripMenuItem"
        Me.VersionInfoToolStripMenuItem.Size = New System.Drawing.Size(152, 22)
        Me.VersionInfoToolStripMenuItem.Text = "&Version Info"
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Location = New System.Drawing.Point(977, 503)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(89, 21)
        Me.ProgressBar1.TabIndex = 3
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripStatusLabel1, Me.ToolStripProgressBar1})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 535)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(1074, 22)
        Me.StatusStrip1.TabIndex = 4
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'ToolStripStatusLabel1
        '
        Me.ToolStripStatusLabel1.AutoSize = False
        Me.ToolStripStatusLabel1.Name = "ToolStripStatusLabel1"
        Me.ToolStripStatusLabel1.Size = New System.Drawing.Size(940, 17)
        Me.ToolStripStatusLabel1.Text = "Ready"
        Me.ToolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.TopLeft
        '
        'ToolStripProgressBar1
        '
        Me.ToolStripProgressBar1.Name = "ToolStripProgressBar1"
        Me.ToolStripProgressBar1.Size = New System.Drawing.Size(100, 16)
        '
        'btnStart
        '
        Me.btnStart.Enabled = False
        Me.btnStart.Location = New System.Drawing.Point(977, 430)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Size = New System.Drawing.Size(89, 26)
        Me.btnStart.TabIndex = 5
        Me.btnStart.Text = "Start"
        Me.btnStart.UseVisualStyleBackColor = True
        '
        'btnStop
        '
        Me.btnStop.Enabled = False
        Me.btnStop.Location = New System.Drawing.Point(977, 462)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(89, 26)
        Me.btnStop.TabIndex = 6
        Me.btnStop.Text = "Stop"
        Me.btnStop.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.chbMinimizedTray)
        Me.GroupBox3.Controls.Add(Me.chbAutoStartTransfer)
        Me.GroupBox3.Controls.Add(Me.GroupBox5)
        Me.GroupBox3.Controls.Add(Me.lblHours)
        Me.GroupBox3.Controls.Add(Me.txtHourInt)
        Me.GroupBox3.Controls.Add(Me.chbTransferMonitor)
        Me.GroupBox3.Location = New System.Drawing.Point(13, 294)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(1053, 108)
        Me.GroupBox3.TabIndex = 7
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Option"
        '
        'chbMinimizedTray
        '
        Me.chbMinimizedTray.AutoSize = True
        Me.chbMinimizedTray.Location = New System.Drawing.Point(296, 19)
        Me.chbMinimizedTray.Name = "chbMinimizedTray"
        Me.chbMinimizedTray.Size = New System.Drawing.Size(102, 17)
        Me.chbMinimizedTray.TabIndex = 6
        Me.chbMinimizedTray.Text = "Minimize to Tray"
        Me.chbMinimizedTray.UseVisualStyleBackColor = True
        '
        'chbAutoStartTransfer
        '
        Me.chbAutoStartTransfer.AutoSize = True
        Me.chbAutoStartTransfer.Location = New System.Drawing.Point(175, 19)
        Me.chbAutoStartTransfer.Name = "chbAutoStartTransfer"
        Me.chbAutoStartTransfer.Size = New System.Drawing.Size(115, 17)
        Me.chbAutoStartTransfer.TabIndex = 5
        Me.chbAutoStartTransfer.Text = "Auto Start Transfer"
        Me.chbAutoStartTransfer.UseVisualStyleBackColor = True
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.cbNetworkAdaptor)
        Me.GroupBox5.Location = New System.Drawing.Point(0, 47)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(438, 61)
        Me.GroupBox5.TabIndex = 4
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Network Adaptor Selection"
        '
        'cbNetworkAdaptor
        '
        Me.cbNetworkAdaptor.FormattingEnabled = True
        Me.cbNetworkAdaptor.Location = New System.Drawing.Point(23, 23)
        Me.cbNetworkAdaptor.Name = "cbNetworkAdaptor"
        Me.cbNetworkAdaptor.Size = New System.Drawing.Size(396, 21)
        Me.cbNetworkAdaptor.TabIndex = 0
        '
        'lblHours
        '
        Me.lblHours.AutoSize = True
        Me.lblHours.Location = New System.Drawing.Point(136, 20)
        Me.lblHours.Name = "lblHours"
        Me.lblHours.Size = New System.Drawing.Size(33, 13)
        Me.lblHours.TabIndex = 2
        Me.lblHours.Text = "hours"
        '
        'txtHourInt
        '
        Me.txtHourInt.Enabled = False
        Me.txtHourInt.Location = New System.Drawing.Point(101, 17)
        Me.txtHourInt.Name = "txtHourInt"
        Me.txtHourInt.Size = New System.Drawing.Size(34, 20)
        Me.txtHourInt.TabIndex = 1
        Me.txtHourInt.Text = "24"
        Me.txtHourInt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'chbTransferMonitor
        '
        Me.chbTransferMonitor.AutoSize = True
        Me.chbTransferMonitor.Location = New System.Drawing.Point(10, 19)
        Me.chbTransferMonitor.Name = "chbTransferMonitor"
        Me.chbTransferMonitor.Size = New System.Drawing.Size(97, 17)
        Me.chbTransferMonitor.TabIndex = 0
        Me.chbTransferMonitor.Text = "Transfer every "
        Me.chbTransferMonitor.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.btnConnectLog)
        Me.GroupBox4.Controls.Add(Me.Label2)
        Me.GroupBox4.Controls.Add(Me.txtDestIP)
        Me.GroupBox4.Controls.Add(Me.chbPSclient)
        Me.GroupBox4.Controls.Add(Me.txtPScheckInterval)
        Me.GroupBox4.Controls.Add(Me.Label1)
        Me.GroupBox4.Controls.Add(Me.chbPowerServer)
        Me.GroupBox4.Location = New System.Drawing.Point(448, 294)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(618, 108)
        Me.GroupBox4.TabIndex = 3
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Power Saver Option"
        '
        'btnConnectLog
        '
        Me.btnConnectLog.Location = New System.Drawing.Point(487, 28)
        Me.btnConnectLog.Name = "btnConnectLog"
        Me.btnConnectLog.Size = New System.Drawing.Size(75, 23)
        Me.btnConnectLog.TabIndex = 6
        Me.btnConnectLog.Text = "Log"
        Me.btnConnectLog.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(301, 73)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(66, 13)
        Me.Label2.TabIndex = 5
        Me.Label2.Text = "Connect To:"
        '
        'txtDestIP
        '
        Me.txtDestIP.Location = New System.Drawing.Point(368, 70)
        Me.txtDestIP.Name = "txtDestIP"
        Me.txtDestIP.Size = New System.Drawing.Size(194, 20)
        Me.txtDestIP.TabIndex = 4
        Me.txtDestIP.Text = "127.0.0.1:8888"
        '
        'chbPSclient
        '
        Me.chbPSclient.AutoSize = True
        Me.chbPSclient.Location = New System.Drawing.Point(15, 70)
        Me.chbPSclient.Name = "chbPSclient"
        Me.chbPSclient.Size = New System.Drawing.Size(244, 17)
        Me.chbPSclient.TabIndex = 3
        Me.chbPSclient.Text = "Client: Transfer files automatically (2 min delay)"
        Me.chbPSclient.UseVisualStyleBackColor = True
        '
        'txtPScheckInterval
        '
        Me.txtPScheckInterval.Location = New System.Drawing.Point(206, 30)
        Me.txtPScheckInterval.Name = "txtPScheckInterval"
        Me.txtPScheckInterval.Size = New System.Drawing.Size(35, 20)
        Me.txtPScheckInterval.TabIndex = 2
        Me.txtPScheckInterval.Text = "24"
        Me.txtPScheckInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(247, 28)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(141, 23)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "hours (Server Port 31316)"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'chbPowerServer
        '
        Me.chbPowerServer.AutoSize = True
        Me.chbPowerServer.Location = New System.Drawing.Point(15, 32)
        Me.chbPowerServer.Name = "chbPowerServer"
        Me.chbPowerServer.Size = New System.Drawing.Size(194, 17)
        Me.chbPowerServer.TabIndex = 0
        Me.chbPowerServer.Text = "Server: Turn all computers on every"
        Me.chbPowerServer.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Enabled = False
        Me.Button3.Location = New System.Drawing.Point(968, 273)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(75, 23)
        Me.Button3.TabIndex = 9
        Me.Button3.Text = "Connect"
        Me.Button3.UseVisualStyleBackColor = True
        Me.Button3.Visible = False
        '
        'Button2
        '
        Me.Button2.Enabled = False
        Me.Button2.Location = New System.Drawing.Point(968, 244)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 8
        Me.Button2.Text = "CheckConnection"
        Me.Button2.UseVisualStyleBackColor = True
        Me.Button2.Visible = False
        '
        'btnTest
        '
        Me.btnTest.Enabled = False
        Me.btnTest.Location = New System.Drawing.Point(856, 225)
        Me.btnTest.Name = "btnTest"
        Me.btnTest.Size = New System.Drawing.Size(75, 23)
        Me.btnTest.TabIndex = 7
        Me.btnTest.Text = "test"
        Me.btnTest.UseVisualStyleBackColor = True
        Me.btnTest.Visible = False
        '
        'Timer1
        '
        '
        'lstStatus
        '
        Me.lstStatus.GridLines = True
        Me.lstStatus.Location = New System.Drawing.Point(13, 408)
        Me.lstStatus.Name = "lstStatus"
        Me.lstStatus.Size = New System.Drawing.Size(620, 116)
        Me.lstStatus.TabIndex = 8
        Me.lstStatus.UseCompatibleStateImageBehavior = False
        Me.lstStatus.View = System.Windows.Forms.View.Details
        '
        'TransferTimer
        '
        '
        'btnAddTransferDir
        '
        Me.btnAddTransferDir.Location = New System.Drawing.Point(977, 95)
        Me.btnAddTransferDir.Name = "btnAddTransferDir"
        Me.btnAddTransferDir.Size = New System.Drawing.Size(75, 43)
        Me.btnAddTransferDir.TabIndex = 1
        Me.btnAddTransferDir.Text = "Add Directory"
        Me.btnAddTransferDir.UseVisualStyleBackColor = True
        '
        'btnRemovedSelectedDir
        '
        Me.btnRemovedSelectedDir.Enabled = False
        Me.btnRemovedSelectedDir.Location = New System.Drawing.Point(977, 172)
        Me.btnRemovedSelectedDir.Name = "btnRemovedSelectedDir"
        Me.btnRemovedSelectedDir.Size = New System.Drawing.Size(75, 52)
        Me.btnRemovedSelectedDir.TabIndex = 2
        Me.btnRemovedSelectedDir.Text = "Remove Selected Directory"
        Me.btnRemovedSelectedDir.UseVisualStyleBackColor = True
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Status, Me.Original_Directory, Me.Destination_Directory})
        Me.DataGridView1.Location = New System.Drawing.Point(13, 28)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.ReadOnly = True
        Me.DataGridView1.Size = New System.Drawing.Size(931, 251)
        Me.DataGridView1.TabIndex = 9
        '
        'Status
        '
        Me.Status.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.Status.FillWeight = 200.0!
        Me.Status.HeaderText = "Status"
        Me.Status.Name = "Status"
        Me.Status.ReadOnly = True
        '
        'Original_Directory
        '
        Me.Original_Directory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.Original_Directory.FillWeight = 400.0!
        Me.Original_Directory.HeaderText = "Original Directory"
        Me.Original_Directory.Name = "Original_Directory"
        Me.Original_Directory.ReadOnly = True
        '
        'Destination_Directory
        '
        Me.Destination_Directory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.Destination_Directory.FillWeight = 400.0!
        Me.Destination_Directory.HeaderText = "Destination Directory"
        Me.Destination_Directory.Name = "Destination_Directory"
        Me.Destination_Directory.ReadOnly = True
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(977, 39)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 10
        Me.Button1.Text = "Button1"
        Me.Button1.UseVisualStyleBackColor = True
        Me.Button1.Visible = False
        '
        'NotifyIcon1
        '
        Me.NotifyIcon1.Icon = CType(resources.GetObject("NotifyIcon1.Icon"), System.Drawing.Icon)
        Me.NotifyIcon1.Text = "Darwin's Backup Program"
        '
        'WaitForComputerReadyTimer
        '
        '
        'TimeCounter
        '
        Me.TimeCounter.Interval = 5000
        '
        'lstConnLog
        '
        Me.lstConnLog.FormattingEnabled = True
        Me.lstConnLog.Location = New System.Drawing.Point(639, 416)
        Me.lstConnLog.Name = "lstConnLog"
        Me.lstConnLog.Size = New System.Drawing.Size(305, 108)
        Me.lstConnLog.TabIndex = 11
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1074, 557)
        Me.Controls.Add(Me.lstConnLog)
        Me.Controls.Add(Me.btnTest)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.DataGridView1)
        Me.Controls.Add(Me.btnRemovedSelectedDir)
        Me.Controls.Add(Me.lstStatus)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.btnAddTransferDir)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.btnStop)
        Me.Controls.Add(Me.btnStart)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.ProgressBar1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D
        Me.MainMenuStrip = Me.MenuStrip1
        Me.MaximizeBox = False
        Me.Name = "Form1"
        Me.Text = "AutoFileTransferer"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
    Friend WithEvents FileToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents SaveSettingToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents CloseToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents OptionToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents VersionInfoToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
    Friend WithEvents StatusStrip1 As System.Windows.Forms.StatusStrip
    Friend WithEvents ToolStripStatusLabel1 As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents btnStart As System.Windows.Forms.Button
    Friend WithEvents btnStop As System.Windows.Forms.Button
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents lblHours As System.Windows.Forms.Label
    Friend WithEvents txtHourInt As System.Windows.Forms.TextBox
    Friend WithEvents chbTransferMonitor As System.Windows.Forms.CheckBox
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents FolderBrowserDialog1 As System.Windows.Forms.FolderBrowserDialog
    Friend WithEvents lstStatus As System.Windows.Forms.ListView
    Friend WithEvents ChangeDestinationDriveBufferSizeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents CheckLogToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents chbPSclient As System.Windows.Forms.CheckBox
    Friend WithEvents txtPScheckInterval As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents chbPowerServer As System.Windows.Forms.CheckBox
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents cbNetworkAdaptor As System.Windows.Forms.ComboBox
    Friend WithEvents TransferTimer As System.Windows.Forms.Timer
    Friend WithEvents ToolStripProgressBar1 As System.Windows.Forms.ToolStripProgressBar
    Friend WithEvents btnAddTransferDir As System.Windows.Forms.Button
    Friend WithEvents btnRemovedSelectedDir As System.Windows.Forms.Button
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents Status As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Original_Directory As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Destination_Directory As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents NotifyIcon1 As System.Windows.Forms.NotifyIcon
    Friend WithEvents chbMinimizedTray As System.Windows.Forms.CheckBox
    Friend WithEvents chbAutoStartTransfer As System.Windows.Forms.CheckBox
    Friend WithEvents StartAtStartupToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtDestIP As System.Windows.Forms.TextBox
    Friend WithEvents btnConnectLog As System.Windows.Forms.Button
    Friend WithEvents btnTest As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents WaitForComputerReadyToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents WaitForComputerReadyTimer As System.Windows.Forms.Timer
    Friend WithEvents TimeCounter As System.Windows.Forms.Timer
    Friend WithEvents lstConnLog As System.Windows.Forms.ListBox

End Class
