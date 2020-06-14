Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports Microsoft.Win32
Imports IWshRuntimeLibrary



Public Class Form1
    'public variable
    Public bufferSize As Int64 = 0

    'Version Info
    Dim VersionInfo As String = " v0.95, Built 09/27/2018"
    Dim ProgramName As String = "Auto Backup v0.95 --- Major Transfer Bug fixed"


    'For multithread transfer monitoring
    'Dim _bTransferDone As Boolean
    'Dim _bTransferSucceed As Boolean
    'Dim _ToolstripMessage As String
    'Dim _ListViewMessage(2) As String
    'Dim _bTransferTermination As Boolean
    Dim _ClientIP As String
    Dim _ClientMac As String
    Dim _ClientCommPath As String
    'Dim LogList As New ArrayList
    ' Dim _PreviousMessage As String = String.Empty
    'Dim _TotalAdjTransferSize As Int64 = 0
    'Dim _CurrentSumTransferSize As Int64 = 0
    Public WithEvents m_checkAndCopy As New CheckAndCopy

    'for network card identification
    Dim ipAddressArray As New ArrayList
    Dim MacAddressArray As New ArrayList
    Dim adaptorNameArray As New ArrayList

    'for Server turns on computer count
    Dim ComputerSleepTime As New Dictionary(Of String, Date) 'MAC Address->Sleeping time
    Dim MaxConnectRetry As Integer = 10
    Public WithEvents m_clienttest As New MutithreadClient
    Public WithEvents m_newListerner As New MultithreadListerner("127.0.0.1", "31316", 24)
    Dim WaitForComputerReadyInterval As Integer = 120
    Dim TimeCounterInterval As Integer = 1000

    Dim timeCount1 As Integer = 0


    Private Declare Function GetDiskFreeSpaceEx Lib "kernel32" Alias "GetDiskFreeSpaceExA" _
       (ByVal lpDirectoryName As String, ByRef lpFreeBytesAvailableToMe As IntPtr, _
       ByRef lpTotalNumberOfBytes As IntPtr, ByRef lpTotalNumberOfFreeBytes As IntPtr) As IntPtr

    'multithread event handler
    Private Sub m_checkAndCopy_UpdateListStatus(ByVal sender As Object, ByVal e As UpdateListStatusEventArgs) Handles m_checkAndCopy.UpdateListStatus
        PopulateListView(e.Message)
    End Sub

    Private Sub m_checkAndCopy_UpdateToolBarProgress(ByVal sender As Object, ByVal e As UpdateToolProgressEventArgs) Handles m_checkAndCopy.UpdateToolBarProgress
        ToolStripProgressBar1.Value = CInt(ToolStripProgressBar1.Maximum * e.ProgressMultiplier)
    End Sub

    'Private Sub m_checkAndCopy_RemoveItemOriListDir(ByVal sender As Object, ByVal e As RemoveItemFromListOriDir) Handles m_checkAndCopy.RemoveItemOriListDir
    '   lstTransferDirectories.Items.RemoveAt(e.PosToRemove)
    'DataGridView1.Rows.RemoveAt(e.PosToRemove)
    'End Sub

    Private Sub m_checkAndCopy_UpdateListDirCheckProgress(ByVal sender As Object, ByVal e As UpdateProgressEventArgs) Handles m_checkAndCopy.UpdateListDirCheckProgress
        ProgressBar1.Value = CInt(e.ProgressMultiplier * ProgressBar1.Maximum)
    End Sub

    Private Sub m_checkAndCopy_UpdateToolStripStatus(ByVal sender As Object, ByVal e As UpdateToolStripStatusEventArgs) Handles m_checkAndCopy.UpdateToolStripStatus
        ToolStripStatusLabel1.Text = e.Message
        'NotifyIcon1.Text = "Darwin's Backup Program: " & e.Message
    End Sub

    Private Sub m_checkAndCopy_StopCheckAndCopy(ByVal sender As Object, ByVal e As TerminateCopyEventArgs) Handles m_checkAndCopy.StopCheckAndCopy
        btnStop.PerformClick()
    End Sub

    'Private Sub m_checkAndCopy_UpdateDestDirTextAndColor(ByVal sender As Object, ByVal e As UpdatelbDestDirTextAndColorEventArgs) Handles m_checkAndCopy.UpdateDestDirTextAndColor
    '   lblDestDir.Text = e.UpdatedDir
    '    lblDestDir.ForeColor = e.UpdatedFontColor
    'End Sub

    Private Sub m_checkAndCopy_UpdateMovedStatus(ByVal sender As Object, ByVal e As UpdateMovedStatusEventArgs) Handles m_checkAndCopy.UpdateMovedStatus
        ' DataGridView1.Item(DataGridView1.Columns("Status").Index, CInt(_RowPosToMoveList(m_MoveFile.NumberOfFileMoved - 1))).Style.ForeColor = e.FontColor
        DataGridView1.Item(DataGridView1.Columns("Status").Index, e.CurrentRowIndex).Value = e.Message
        If (chbPSclient.Checked) Then
            m_clienttest.SendMessageSocketAsync(m_clienttest.IPadd & ": " & e.Message & " <" & DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, e.CurrentRowIndex).Value.ToString & ">")
        End If
    End Sub

    'listener handlers
    Private Sub m_newListerner_UpdateListStatus(ByVal sender As Object, ByVal e As UpdateListBoxEventArgs) Handles m_newListerner.UpdateListBoxStatus
        ConnectionLog.lstConnectionLog.Items.Add(e.Message.ToString)
        ConnectionLog.lstConnectionLog.SelectedIndex = ConnectionLog.lstConnectionLog.Items.Count - 1
        ConnectionLog.lstConnectionLog.ClearSelected()
        lstConnLog.Items.Add(e.Message.ToString)
        lstConnLog.SelectedIndex = ConnectionLog.lstConnectionLog.Items.Count - 1
        lstConnLog.ClearSelected()
    End Sub

    Private Sub m_newListerner_AddChatHandler(ByVal sender As Object, ByVal e As AddchatHandlerEventArgs) Handles m_newListerner.AddChatHandler
        AddHandler e.ReturnChatID.UpdateListBoxStatus, AddressOf MessageFromChat
    End Sub

    Friend Sub MessageFromChat(ByVal sender As Object, ByVal e As UpdateListBoxEventArgs)
        ConnectionLog.lstConnectionLog.Items.Add(e.Message.ToString)
        ConnectionLog.lstConnectionLog.SelectedIndex = ConnectionLog.lstConnectionLog.Items.Count - 1
        ConnectionLog.lstConnectionLog.ClearSelected()
        lstConnLog.Items.Add(e.Message.ToString)
        lstConnLog.SelectedIndex = ConnectionLog.lstConnectionLog.Items.Count - 1
        lstConnLog.ClearSelected()
    End Sub

    'client handlers
    Private Sub m_clienttest_UpdateListStatus(ByVal sender As Object, ByVal e As UpdateListBoxEventArgs) Handles m_clienttest.UpdateListBoxStatus
        Console.WriteLine(e.Message)
        ConnectionLog.lstConnectionLog.Items.Add(e.Message.ToString)
        ConnectionLog.lstConnectionLog.SelectedIndex = ConnectionLog.lstConnectionLog.Items.Count - 1
        ConnectionLog.lstConnectionLog.ClearSelected()
        lstConnLog.Items.Add(e.Message.ToString)
        lstConnLog.SelectedIndex = ConnectionLog.lstConnectionLog.Items.Count - 1
        lstConnLog.ClearSelected()
    End Sub

    Private Sub m_clienttest_UpdateTCPclient(ByVal sender As Object, ByVal e As UpdateTCPclient) Handles m_clienttest.UpdateTCPclient
        m_clienttest.NewTCP = e.UpdatedTCPclient
    End Sub

    Private Sub m_clienttest_ReconnectToServer(ByVal sender As Object, ByVal e As ConnectToServer) Handles m_clienttest.reConnectToServer
        Dim DestIParray As String() = Split(txtDestIP.Text, ":")
        m_clienttest.ClientConnectSocketAsync(DestIParray(0), DestIParray(1), MaxConnectRetry)
    End Sub

    Private Sub m_clienttest_UpdateClientSocket(ByVal sender As Object, ByVal e As UpdateClientSocket) Handles m_clienttest.UpdateClientSocket
        m_clienttest.NewSocket = e.UpdatedClientSocket
    End Sub

    Private Sub m_clienttest_StopBeingClient(ByVal sender As Object, ByVal e As QuitAsClient) Handles m_clienttest.StopAsClient
        chbPSclient.Checked = False
        'chbPSclient_CheckedChanged(Nothing, Nothing)
    End Sub

    Private Sub m_clienttest_StartTransfer(ByVal sender As Object, ByVal e As StartTransferEvent) Handles m_clienttest.StartTransfer
        m_clienttest.SendMessageSocketAsync(m_clienttest.IPadd & ": Transfer started!")
        m_clienttest.CurrentClientStatus = "Transferring"
        btnStart.PerformClick()
    End Sub

    Private Sub m_clienttest_ShutdownComputer(ByVal sender As Object, ByVal e As ShutdownEvent) Handles m_clienttest.ShutdownComputer
        ShutdownComputer()
    End Sub



    'functions
    Private Sub CloseToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CloseToolStripMenuItem.Click
        Me.Close()
    End Sub

    Private Sub VersionInfoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles VersionInfoToolStripMenuItem.Click
        MessageBox.Show("Auto File Transferer" & VersionInfo, "Version Info")
    End Sub

    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chbTransferMonitor.CheckedChanged
        If (chbTransferMonitor.Checked) Then
            txtHourInt.Enabled = True
            'lblHours.Enabled = True
        Else
            txtHourInt.Enabled = False
            'lblHours.Enabled = False
        End If
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddTransferDir.Click
        With FolderBrowserDialog1
            .Description = "Select folder to copy"
            .ShowNewFolderButton = False
        End With

        If (FolderBrowserDialog1.ShowDialog = Windows.Forms.DialogResult.OK) Then
            Dim nRowCount As Integer = DataGridView1.Rows.Count
            DataGridView1.Rows.Add()
            DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, nRowCount).Value = FolderBrowserDialog1.SelectedPath
            DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, nRowCount).Value = "Please Select the destination folder"


            If (DataGridView1.Rows.Count = 0) Then
                btnRemovedSelectedDir.Enabled = False
            Else
                btnRemovedSelectedDir.Enabled = True
            End If
        End If

        EnableStartButton()
    End Sub

    Private Sub DataGridView1_CellClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.CellClick
        With FolderBrowserDialog1
            .ShowNewFolderButton = True
            If (e.ColumnIndex = DataGridView1.Columns("Destination_Directory").Index And DataGridView1.SelectedCells.Count = 1) Then
                If (.ShowDialog = Windows.Forms.DialogResult.OK) Then
                    Dim oriPath As String = DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, e.RowIndex).Value.ToString
                    If (.SelectedPath.Equals(oriPath)) Then
                        DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, e.RowIndex).Value = "Path cannot be the same as original"
                    Else
                        DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, e.RowIndex).Value = .SelectedPath
                    End If

                End If
            End If
            .ShowNewFolderButton = False
        End With
    End Sub

    Private Sub PopulateListViewHeader()
        'Declare and construct the ColumnHeader objects
        Dim header1, header2 As ColumnHeader
        header1 = New ColumnHeader
        header2 = New ColumnHeader

        'Set the text, alignment and width for each column header
        header1.Text = "Description"
        header1.TextAlign = HorizontalAlignment.Left
        header1.Width = 520

        header2.TextAlign = HorizontalAlignment.Left
        header2.Text = "Status"
        header2.Width = 150

        'Add the headers to the ListView control
        lstStatus.Columns.Add(header1)
        lstStatus.Columns.Add(header2)
    End Sub

    Public Sub PopulateListView(ByVal row() As String)
        'populate a row of items in the listview
        Dim listItem As New ListViewItem(row(0))
        For nCount As Integer = 1 To row.Length - 1
            listItem.SubItems.Add(row(nCount))
        Next
        lstStatus.Items.Add(listItem)
        lstStatus.EnsureVisible(lstStatus.Items.Count - 1)
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Text = "Auto File Transferer" & VersionInfo
        bufferSize = CLng(Form2.txtBufferSize.Text) * 1073741824    'default 100GB
        PopulateListViewHeader()
        LoadSetting()
        EnableStartButton()
        'EnablePSserverClientSelection()
        GetIPAddress()
        'MessageBox.Show(MacAddressArray(0).ToString)
        Try
            m_newListerner.SetIP = ipAddressArray(0).ToString
            Console.WriteLine(ipAddressArray(0).ToString)
            Console.WriteLine(MacAddressArray(0).ToString)
        Catch ex As Exception
        End Try
        If (DataGridView1.Rows.Count = 0) Then
            btnRemovedSelectedDir.Enabled = False
        Else
            btnRemovedSelectedDir.Enabled = True
        End If
        If (chbAutoStartTransfer.Checked) Then
            btnStart.PerformClick()
        End If

        If (chbMinimizedTray.Checked) Then
            MinimizeToTray()
        End If

        If (StartAtStartupToolStripMenuItem.Checked) Then
            AddStartUpShortcut(True)
        Else
            AddStartUpShortcut(False)
        End If

        If (chbPowerServer.Checked) Then
            'chbPowerServer_CheckedChanged(Nothing, Nothing)
        End If

        If (chbPSclient.Checked) Then
            ' chbPSclient_CheckedChanged(Nothing, Nothing)
        End If

        'load network setting
        LoadSetting("Network")
    End Sub

    Private Sub btnRemovedSelectedDir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRemovedSelectedDir.Click
        If (DataGridView1.SelectedRows.Count > 0) Then
            Dim nCount As Integer = DataGridView1.SelectedRows.Count
            For Each row As DataGridViewRow In DataGridView1.SelectedRows
                DataGridView1.Rows.Remove(row)
            Next
        End If

        If (DataGridView1.Rows.Count > 0) Then
            btnRemovedSelectedDir.Enabled = True
        Else
            btnRemovedSelectedDir.Enabled = False
        End If
        EnableStartButton()
    End Sub

    Private Sub EnableStartButton()   'also enable/disable client checkbox
        'If ((lstTransferDirectories.Items.Count > 0) And (Not lblDestDir.Text.Equals("Select Destination Directory"))) Then
        If (DataGridView1.Rows.Count > 0) Then
            chbPSclient.Enabled = True
            btnStart.Enabled = True
            'chbPSclient.Enabled = True
        Else
            btnStart.Enabled = False
            chbPSclient.Checked = False
            chbPSclient.Enabled = False

            'chbPSclient.Enabled = False
        End If
    End Sub

    Private Sub EnablePSserverClientSelection()
        'If ((DataGridView1.Rows.Count > 0) And (Not lblPScomFolder.Text.Equals("No Communication Folder Selected!"))) Then
        If (True) Then
            chbPowerServer.Enabled = True
            chbPSclient.Enabled = True
            txtPScheckInterval.Enabled = True
        Else
            chbPowerServer.Enabled = False
            chbPSclient.Enabled = False
            txtPScheckInterval.Enabled = False
        End If
    End Sub

    Private Sub btnStop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStop.Click
        btnStop.Enabled = False
        EnableInputs(True)
        EnableStartButton()
        ProgressBar1.Value = 0
        ToolStripProgressBar1.Value = 0
        Timer1.Enabled = False
        ToolStripStatusLabel1.Text = "Monitor stopped!"
        NotifyIcon1.Text = "Darwin's Backup Program: " & ToolStripStatusLabel1.Text
    End Sub

    Private Function GatherDirToDict(ByVal d As DirectoryInfo, ByRef dict As Dictionary(Of String, Dictionary(Of String, String)), ByRef errorBit As Integer) As Int64   'return Dir Size
        'file name -> directory -> file size
        Dim Size As Int64 = 0
        Try
            'get all files in the current directory
            Dim fis As FileInfo() = d.GetFiles()
            Dim fi As FileInfo
            For Each fi In fis
                'MessageBox.Show(fi.DirectoryName.ToString)
                'MessageBox.Show(fi.Name.ToString)

                'Avoid Thumbs.db files
                If (fi.Name.ToString.Equals("Thumbs.db")) Then
                    PopulateListView({fi.Name.ToString & " found!", (fi.Length / 1048576).ToString & "MB"})
                    Continue For
                End If

                Dim temp As String = fi.DirectoryName.ToString
                If (temp.Substring(temp.Length - 1, 1).Equals("\")) Then
                Else
                    temp = temp & "\"
                End If

                If (dict.ContainsKey(fi.Name.ToString)) Then
                    dict(fi.Name.ToString).Add(temp, fi.Length.ToString)
                Else
                    Dim tempdict As New Dictionary(Of String, String)
                    tempdict.Add(temp, fi.Length.ToString)
                    dict.Add(fi.Name.ToString, tempdict)
                End If

                'add file size
                Size += fi.Length
            Next

            'look into subdirectory
            Dim dis As DirectoryInfo() = d.GetDirectories()
            Dim di As DirectoryInfo
            For Each di In dis
                'avoid $RECYCLE.BIN and System Volume Information folders
                If (di.Name.ToString.Equals("$RECYCLE.BIN") Or di.Name.ToString.Equals("System Volume Information")) Then
                    PopulateListView({di.Name.ToString & " found!", "Skipped!"})
                    Continue For
                End If
                Size += GatherDirToDict(di, dict, errorBit)
            Next

        Catch ex As Exception
            PopulateListView({ex.Message, "Error GatherDirToDict"})
            errorBit = errorBit + 1    'error count
        End Try
        Return Size
    End Function

    Private Sub EnableInputs(ByVal bEnable As Boolean)
        If (bEnable) Then
            btnAddTransferDir.Enabled = True
            ' btnAddDestDir.Enabled = True
            btnRemovedSelectedDir.Enabled = True
            chbTransferMonitor.Enabled = True
            lblHours.Enabled = True
            If (chbTransferMonitor.Checked) Then
                txtHourInt.Enabled = True
                'lblHours.Enabled = True
            Else
                txtHourInt.Enabled = False
                'lblHours.Enabled = False
            End If
            'chbPowerServer.Enabled = True
            If (DataGridView1.Rows.Count > 0) Then
                chbPSclient.Enabled = True
            End If
        Else
            btnAddTransferDir.Enabled = False
            'btnAddDestDir.Enabled = False
            btnRemovedSelectedDir.Enabled = False
            chbTransferMonitor.Enabled = False
            txtHourInt.Enabled = False
            lblHours.Enabled = False
            'chbPowerServer.Enabled = False
            chbPSclient.Enabled = False
        End If
    End Sub



    Private Sub btnStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStart.Click
        btnStart.Enabled = False
        'btnStop.Enabled = True
        EnableInputs(False)

        Dim originalDirList As New ArrayList
        Dim destinationDirList As New ArrayList
        For nI As Integer = 0 To DataGridView1.Rows.Count - 1
            originalDirList.Add(DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, nI).Value)
            destinationDirList.Add(DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, nI).Value)
        Next

        m_checkAndCopy.CheckAndCopyAsync(destinationDirList, originalDirList, bufferSize)
        'Start multi-thread timer
        TransferTimer.Interval = 50
        TransferTimer.Enabled = True
        'CheckAndCopy()
        'If (chbTransferMonitor.Checked And CheckAndCopy()) Then
        'Dim hour As Integer
        'Try
        'Hour = CInt(txtHourInt.Text)
        'Catch ex As Exception
        ' MessageBox.Show(ex.Message)
        ' btnStop.PerformClick()
        'Exit Sub
        'End Try
        'Timer1.Interval = CInt(txtHourInt.Text) * 3600 * 1000
        'Timer1.Enabled = True

        'ToolStripStatusLabel1.Text = "Monitor started on " & System.DateTime.Now.ToString
        'Else
        'btnStop.PerformClick()
        'End If

    End Sub

    Private Sub DeleteFileFromDestination(ByRef removeFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                               ByVal DestDir As String, ByVal targetedDestDirList As ArrayList, ByVal bRootDir As Boolean, ByRef bSuccess As Boolean, ByRef LogList As ArrayList)
        'remove file dict   'transfer directory -> file name -> destination dir -> size

        If (bSuccess) Then
            Dim tempPath As String
            Try
                Dim DestDirInfo As New DirectoryInfo(DestDir)
                Dim fis As FileInfo() = DestDirInfo.GetFiles
                For Each f As FileInfo In fis
                    For Each transDir As String In removeFileDict.Keys
                        If (removeFileDict(transDir).ContainsKey(f.Name)) Then
                            If (removeFileDict(transDir)(f.Name).ContainsKey(AddBackslash(f.DirectoryName))) Then
                                tempPath = f.FullName
                                If (System.IO.File.Exists(tempPath)) Then
                                    My.Computer.FileSystem.DeleteFile(f.FullName)
                                    ToolStripStatusLabel1.Text = "Deleting " & f.FullName
                                    PopulateListView({"Deleting " & tempPath, "Deleted"})
                                    LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, tempPath, "Deleted"}, vbTab))
                                Else
                                    PopulateListView({"Deleting " & tempPath, "Not Found!"})
                                    LogList.Add(Join({"Warning", System.DateTime.Now.ToString, tempPath, "Not Found!"}, vbTab))
                                End If

                            End If
                        End If
                    Next
                Next

                Dim dis As DirectoryInfo() = DestDirInfo.GetDirectories()
                For Each d As DirectoryInfo In dis
                    If (bRootDir) Then
                        For nA As Integer = 0 To targetedDestDirList.Count - 1
                            If (AddBackslash(d.FullName).Equals(AddBackslash(targetedDestDirList(nA).ToString))) Then
                                DeleteFileFromDestination(removeFileDict, d.FullName, targetedDestDirList, False, bSuccess, LogList)
                            End If
                        Next
                    Else
                        DeleteFileFromDestination(removeFileDict, d.FullName, targetedDestDirList, False, bSuccess, LogList)
                    End If
                Next

                If (DestDirInfo.GetFiles.Length = 0 And DestDirInfo.GetDirectories.Length = 0) Then
                    My.Computer.FileSystem.DeleteDirectory(DestDir, FileIO.DeleteDirectoryOption.ThrowIfDirectoryNonEmpty)
                    PopulateListView({"Deleting " & DestDir, "Deleted"})
                    LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, DestDir, "Deleted"}, vbTab))
                End If

            Catch ex As Exception
                bSuccess = False
                PopulateListView({ex.Message, "Error DeleteFileFromDestination"})
                LogList.Add(Join({"Failed", System.DateTime.Now.ToString, tempPath, ex.Message}, vbTab))
            End Try

        End If

        ToolStripStatusLabel1.Text = "Ready"

    End Sub
    Private Function BuildMoveFileDict(ByRef transferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef ToBeTransferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef removeFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef moveFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef totalMoveFileSize As Int64) As Boolean

        'build the moveFileDict first
        Dim tempToBeTransferFileDict As New Dictionary(Of String, Dictionary(Of String, String))
        Dim tempRemoveFileDict As New Dictionary(Of String, Dictionary(Of String, String))

        For Each transDir As String In ToBeTransferFileDict.Keys
            For Each filename As String In ToBeTransferFileDict(transDir).Keys
                For Each destDir As String In ToBeTransferFileDict(transDir)(filename).Keys
                    If (tempToBeTransferFileDict.ContainsKey(filename)) Then
                        tempToBeTransferFileDict(filename).Add(destDir, ToBeTransferFileDict(transDir)(filename)(destDir))
                    Else
                        Dim temp1 As New Dictionary(Of String, String)
                        temp1.Add(destDir, ToBeTransferFileDict(transDir)(filename)(destDir))
                        tempToBeTransferFileDict.Add(filename, temp1)
                    End If
                Next
            Next
        Next

        For Each transDir As String In removeFileDict.Keys
            For Each filename As String In removeFileDict(transDir).Keys
                For Each oriDestDir As String In removeFileDict(transDir)(filename).Keys
                    If (tempRemoveFileDict.ContainsKey(filename)) Then
                        tempRemoveFileDict(filename).Add(oriDestDir, removeFileDict(transDir)(filename)(oriDestDir))
                    Else
                        Dim temp2 As New Dictionary(Of String, String)
                        temp2.Add(oriDestDir, removeFileDict(transDir)(filename)(oriDestDir))
                        tempRemoveFileDict.Add(filename, temp2)
                    End If
                Next
            Next
        Next

        'Dim transferFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) 'transfer directory -> file name -> original dir -> destination dir
        'Dim ToBeTransferFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) 'transfer directory -> file name -> destination dir -> Size
        'Dim removeFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))    'transfer directory -> file name -> original dir in destination dir -> Size
        'Dim moveFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) '"dummy" -> file name -> original dir -> destination dir
        'tempToBeTransferFileDict --- file name -> destination dir -> size
        'tempRemoveFileDict --- file name -> original dir in destination dir -> Size

        For Each filename As String In tempToBeTransferFileDict.Keys
            If (tempRemoveFileDict.ContainsKey(filename)) Then
                For Each destDir As String In tempToBeTransferFileDict(filename).Keys
                    If (tempRemoveFileDict(filename).Count > 0) Then
                        For Each oriDestDir As String In tempRemoveFileDict(filename).Keys
                            If (tempToBeTransferFileDict(filename)(destDir).Equals(tempRemoveFileDict(filename)(oriDestDir))) Then
                                AddToDict(moveFileDict, "dummy", filename, oriDestDir, destDir)

                                'add to totalMoveFileSize
                                totalMoveFileSize = totalMoveFileSize + CLng(tempRemoveFileDict(filename)(oriDestDir))

                                'remove key/value from tempRemoveFileDict
                                tempRemoveFileDict(filename).Remove(oriDestDir)

                                'exit for loop
                                Exit For
                            End If
                        Next
                    Else
                        tempRemoveFileDict.Remove(filename)
                        Exit For
                    End If
                Next
            End If
        Next


        'if moveFileDict build successfully, rebuild transferFileDict and removeFileDict
        Dim NewTransferFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))
        If (moveFileDict.Count > 0) Then
            For Each transDir As String In transferFileDict.Keys
                For Each filename As String In transferFileDict(transDir).Keys
                    For Each oriDir As String In transferFileDict(transDir)(filename).Keys
                        If (moveFileDict("dummy").ContainsKey(filename)) Then
                            Dim bMatch As Boolean = False
                            For Each oriDestDir As String In moveFileDict("dummy")(filename).Keys
                                'MessageBox.Show(transferFileDict(transDir)(filename)(oriDir) & ", " & moveFileDict("dummy")(filename)(oriDestDir))

                                If (transferFileDict(transDir)(filename)(oriDir).Equals(moveFileDict("dummy")(filename)(oriDestDir))) Then
                                    bMatch = True
                                    Exit For
                                End If
                            Next

                            If (Not bMatch) Then
                                AddToDict(NewTransferFileDict, transDir, filename, oriDir, transferFileDict(transDir)(filename)(oriDir))
                            End If
                        Else
                            AddToDict(NewTransferFileDict, transDir, filename, oriDir, transferFileDict(transDir)(filename)(oriDir))
                        End If
                    Next
                Next
            Next

            transferFileDict.Clear()
            transferFileDict = NewTransferFileDict          'done rebuilding transferFileDict

            'rebuilding removeFileDict

            'For Each x As String In tempRemoveFileDict.Keys
            'For Each y As String In tempRemoveFileDict(x).Keys
            'PopulateListView({x & ", " & y, "BEFORE"})
            'Next
            'Next
            For Each filename As String In moveFileDict("dummy").Keys
                If (tempRemoveFileDict.ContainsKey(filename)) Then
                    If (tempRemoveFileDict(filename).Count > 0) Then
                        For Each oriDestDir As String In moveFileDict("dummy")(filename).Keys
                            PopulateListView({(moveFileDict("dummy")(filename)(oriDestDir)), filename})
                            If (tempRemoveFileDict(filename).ContainsKey(moveFileDict("dummy")(filename)(oriDestDir))) Then
                                tempRemoveFileDict(filename).Remove(moveFileDict("dummy")(filename)(oriDestDir))
                            End If
                        Next
                    Else
                        tempRemoveFileDict.Remove(filename)
                    End If

                End If
            Next

            'For Each x As String In tempRemoveFileDict.Keys
            'For Each y As String In tempRemoveFileDict(x).Keys
            'PopulateListView({x & ", " & y, "AFTER"})
            'Next
            'Next
            removeFileDict.Clear()
            removeFileDict.Add("dummy", tempRemoveFileDict)


            Return True
        End If

        Return False
    End Function
    Private Function TransferFileToDestination(ByRef transferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), ByVal bMove As Boolean, ByRef LogList As ArrayList) As Boolean
        'transferFileDict: transfer directory -> file name -> original dir -> destination dir
        Dim bSuccess As Boolean = True
        'bTransferSucceed = True
        For Each transDir As String In transferFileDict.Keys
            For Each fileName As String In transferFileDict(transDir).Keys
                For Each oriDir As String In transferFileDict(transDir)(fileName).Keys
                    Try
                        If (System.IO.File.Exists(Path.Combine({oriDir, fileName}))) Then
                            ToolStripStatusLabel1.Text = "Transferring " & Path.Combine({oriDir, fileName})
                            If (bMove) Then
                                My.Computer.FileSystem.MoveFile(Path.Combine({oriDir, fileName}), Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), True)
                                'My.Computer.FileSystem.DeleteFile(Path.Combine({oriDir, fileName}))
                                Dim ori As New DirectoryInfo(oriDir)
                                If (ori.GetDirectories.Length = 0 And ori.GetFiles.Length = 0) Then
                                    My.Computer.FileSystem.DeleteDirectory(oriDir, FileIO.DeleteDirectoryOption.ThrowIfDirectoryNonEmpty)
                                End If
                                PopulateListView({"Moving " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"})
                                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"}, vbTab))
                            Else
                                My.Computer.FileSystem.CopyFile(Path.Combine({oriDir, fileName}), Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                                PopulateListView({"Copying " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"})
                                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"}, vbTab))
                            End If
                        Else 'file not found in the original dir
                            PopulateListView({"Transferring " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"})
                            LogList.Add(Join({"Warning", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"}, vbTab))
                        End If

                    Catch ex As Exception
                        bSuccess = False
                        'bTransferSucceed = False
                        PopulateListView({ex.Message, "Error TransferFileToDestination"})
                        LogList.Add(Join({"Failed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), ex.Message}, vbTab))
                    End Try
                Next
            Next
        Next

        ToolStripStatusLabel1.Text = "Ready"
        'bTransferDone = True
        Return bSuccess
    End Function

    'Private Function TransferFileToDestination(ByRef transferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), ByVal bMove As Boolean, ByRef LogList As ArrayList) As Boolean
    'modify for multithread transfer
    Private Sub TransferFileToDestMulti(ByRef transferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), ByVal bMove As Boolean, ByRef LogList As ArrayList, _
                                          ByRef ListViewMessage As String(), ByRef ToolStripMessage As String, ByRef bTransferDone As Boolean, ByRef bTransferSucceed As Boolean, ByRef TransferSizeSum As Int64)
        'transferFileDict: transfer directory -> file name -> original dir -> destination dir
        'Dim bSuccess As Boolean = True
        bTransferSucceed = True
        For Each transDir As String In transferFileDict.Keys
            For Each fileName As String In transferFileDict(transDir).Keys
                For Each oriDir As String In transferFileDict(transDir)(fileName).Keys
                    Try
                        If (System.IO.File.Exists(Path.Combine({oriDir, fileName}))) Then
                            ToolStripMessage = "Transferring " & Path.Combine({oriDir, fileName})
                            If (bMove) Then
                                My.Computer.FileSystem.MoveFile(Path.Combine({oriDir, fileName}), Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), True)
                                'My.Computer.FileSystem.DeleteFile(Path.Combine({oriDir, fileName}))
                                Dim ori As New DirectoryInfo(oriDir)
                                If (ori.GetDirectories.Length = 0 And ori.GetFiles.Length = 0) Then
                                    My.Computer.FileSystem.DeleteDirectory(oriDir, FileIO.DeleteDirectoryOption.ThrowIfDirectoryNonEmpty)
                                End If
                                ListViewMessage = ({"Moving " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"})
                                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"}, vbTab))
                            Else
                                My.Computer.FileSystem.CopyFile(Path.Combine({oriDir, fileName}), Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, FileIO.UICancelOption.ThrowException)
                                Dim CurrentFile As New FileInfo(Path.Combine({oriDir, fileName}))
                                TransferSizeSum = TransferSizeSum + CurrentFile.Length
                                ListViewMessage = ({"Copying " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"})
                                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"}, vbTab))
                            End If
                        Else 'file not found in the original dir
                            ListViewMessage = ({"Transferring " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"})
                            LogList.Add(Join({"Warning", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"}, vbTab))
                        End If

                    Catch ex As Exception
                        'bSuccess = False
                        bTransferSucceed = False
                        ListViewMessage = ({ex.Message, "Error TransferFileToDestination"})
                        LogList.Add(Join({"Failed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), ex.Message}, vbTab))
                    End Try
                    System.Threading.Thread.Sleep(100)   'wait for 100 second
                Next
            Next
        Next

        ToolStripMessage = "Ready"
        bTransferDone = True
        'Return bSuccess
    End Sub

    Private Sub UpdateLog(ByRef list As ArrayList)
        Dim bFileExist As Boolean = System.IO.File.Exists("AutoBackup_Log.txt")
        Dim sw As New System.IO.StreamWriter("AutoBackup_Log.txt", True, System.Text.Encoding.GetEncoding("gb2312"))
        If (Not bFileExist) Then
            sw.WriteLine("Created on " & System.DateTime.Now.ToString)
            sw.WriteLine(Join({"Status", "Performed Time", "Data path", "Message"}, vbTab))
            sw.WriteLine()
        End If
        For Each line As String In list
            sw.WriteLine(line.ToString)
        Next

        sw.Close()
    End Sub

    Private Function AddBackslash(ByVal dir As String) As String
        If (Not dir.Substring(dir.Length - 1, 1).Equals("\")) Then
            dir = dir & "\"
        End If
        Return dir
    End Function

    Private Function AddSlashToRegexLiterals(ByVal dir As String) As String
        Dim literals As String() = {"\+", "\.", "\{", "\}", "\\", "\[", "\]", "\(", "\)"}
        For Each literal In literals
            Dim tempRegex As New Regex(literal)
            dir = tempRegex.Replace(dir, literal)
        Next
        Return dir
    End Function

    Private Function GetFullOriDirPath(ByVal FullDestPath As String, ByVal OriginalRootDir As String, ByVal newRootDir As String) As String
        'GetFullOriDirPath(DestDir, lstTransferDirectories.Items(nI).ToString, newDestDir)
        Dim retrievedOriDir As String = String.Empty
        'make sure OriginalRootDir and newRootDir end with \
        OriginalRootDir = AddBackslash(OriginalRootDir)
        newRootDir = AddBackslash(newRootDir)

        'replace fulldestpath with originalrootdir
        'Dim tempRegex1 As New Regex("\\")
        'Dim adjNewRootDir As String = tempRegex1.Replace(newRootDir, "\\")
        Dim adjNewRootDir As String = AddSlashToRegexLiterals(newRootDir)
        Dim tempRegex As New Regex("(" & adjNewRootDir & ")")

        'MessageBox.Show(FullDestPath & " " & adjNewRootDir & " " & OriginalRootDir)
        retrievedOriDir = tempRegex.Replace(FullDestPath, OriginalRootDir)
        'MessageBox.Show(retrievedOriDir)
        'PopulateListView({retrievedOriDir, "GetFullOriDirPath"})
        'Dim m As Match = tempRegex.Match(FullDestPath)
        'If (m.Success) Then
        'MessageBox.Show(m.Groups(1).ToString)
        'End If

        Return retrievedOriDir
    End Function

    Private Function GetFullDestDirPath(ByVal originalSubDir As String, ByVal originalRootDir As String, ByVal newRootDir As String) As String
        'if newRootDir is from a drive in the original dir, newRootDir will be missing ":\"
        'originalSubDir can be drive dir, eg: X:\

        Dim newFullDestDir As String = String.Empty

        'make sure all inputs end with "\"
        If (Not originalSubDir.Substring(originalSubDir.Length - 1, 1).Equals("\")) Then
            originalSubDir = originalSubDir & "\"
        End If
        If (Not originalRootDir.Substring(originalRootDir.Length - 1, 1).Equals("\")) Then
            originalRootDir = originalRootDir & "\"
        End If
        If (Not newRootDir.Substring(newRootDir.Length - 1, 1).Equals("\")) Then
            newRootDir = newRootDir & "\"
        End If

        'try replacing the originalSubDir with newRootDir
        'Dim tempRegex1 As New Regex("\\")
        'Dim adjOriginalRootDir As String = tempRegex1.Replace(originalRootDir, "\\")
        Dim adjOriginalRootDir As String = AddSlashToRegexLiterals(originalRootDir)
        'MessageBox.Show(adjOriginalRootDir)
        Dim tempRegex As New Regex("(" & adjOriginalRootDir & ")")
        newFullDestDir = tempRegex.Replace(originalSubDir, newRootDir)
        'MessageBox.Show(newFullDestDir)
        'PopulateListView({newFullDestDir, "GetFullDestDirPath"})
        'Dim m As Match = tempRegex.Match(originalSubDir)
        'If (m.Success) Then
        'MessageBox.Show(m.Groups(1).ToString)
        ' End If

        Return newFullDestDir
    End Function

    Private Sub AddToDict(ByRef dict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), ByVal key1 As String, ByVal key2 As String, ByVal key3 As String, _
                          ByVal value As String)
        'x -> y -> z -> a only
        If (dict.ContainsKey(key1)) Then
            If (dict(key1).ContainsKey(key2)) Then
                dict(key1)(key2).Add(key3, value)
            Else
                Dim temp1 As New Dictionary(Of String, String)
                temp1.Add(key3, value)
                dict(key1).Add(key2, temp1)
            End If
        Else
            Dim temp2 As New Dictionary(Of String, String)
            temp2.Add(key3, value)
            Dim temp1 As New Dictionary(Of String, Dictionary(Of String, String))
            temp1.Add(key2, temp2)
            dict.Add(key1, temp1)
        End If
    End Sub

    ' Private Sub btnAddDestDir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
    '  With FolderBrowserDialog1
    '       .Description = "Select Destination Folder"
    '      .ShowNewFolderButton = False
    '   End With

    '  If (FolderBrowserDialog1.ShowDialog = Windows.Forms.DialogResult.OK) Then
    '       lblDestDir.Text = FolderBrowserDialog1.SelectedPath
    '       lblDestDir.ForeColor = Color.BlueViolet
    'PopulateListView({"Free space in " & lblDestDir.Text, DiskFreeSpace(lblDestDir.Text) & "MB"})
    '   Else
    '      lblDestDir.Text = "Select Destination Directory"
    '       lblDestDir.ForeColor = Color.Red
    '   End If

    '   EnableStartButton()
    ' End Sub

    Private Function DirSize(ByVal d As DirectoryInfo, ByRef e As Integer) As Long              'error out and return size 0 if directories/files reading error
        Dim Size As Long = 0
        Try
            ' Add file sizes.
            Dim fis As FileInfo() = d.GetFiles()
            Dim fi As FileInfo
            For Each fi In fis
                Size += fi.Length
            Next fi
            ' Add subdirectory sizes.
            Dim dis As DirectoryInfo() = d.GetDirectories()
            Dim di As DirectoryInfo
            For Each di In dis
                Size += DirSize(di, e)
            Next di
        Catch ex As Exception
            PopulateListView({ex.Message, "Error"})
            e = e + 1      'error count
        End Try
        Return Size
    End Function 'DirSize

    Private Function DiskFreeSpace(ByVal strDestination As String) As String    'return available in bytes
        Dim FreeBytesAvailableToMe As IntPtr = IntPtr.Zero
        Dim TotalBytes As IntPtr = IntPtr.Zero
        Dim FreeBytes As IntPtr = IntPtr.Zero
        Dim DestinationFreeSpace As Int64

        If CBool((CInt(GetDiskFreeSpaceEx(strDestination, FreeBytesAvailableToMe, TotalBytes, FreeBytes)))) Then
            'MessageBox.Show(FreeBytesAvailableToMe.ToString & ", " & TotalBytes.ToString & ", " & FreeBytes.ToString)
            DestinationFreeSpace = CLng(FreeBytesAvailableToMe)
            'MessageBox.Show(TotalBytes.ToString)
            Return DestinationFreeSpace.ToString
        End If

        Return "N/A"
    End Function

    Private Sub ChangeDestinationDriveBufferSizeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ChangeDestinationDriveBufferSizeToolStripMenuItem.Click
        Form2.ShowDialog()
    End Sub

    Private Sub CheckLogToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckLogToolStripMenuItem.Click
        If (Not System.IO.File.Exists("AutoBackup_Log.txt")) Then
            Dim sw As New System.IO.StreamWriter("AutoBackup_Log.txt")
            sw.WriteLine("Created on " & System.DateTime.Now.ToString)
            sw.WriteLine(Join({"Status", "Performed Time", "Data path", "Message"}, vbTab))
            sw.WriteLine()
            sw.Close()
        End If
        System.Diagnostics.Process.Start("AutoBackup_Log.txt")
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Timer1.Enabled = False
        btnStop.Enabled = False
        Dim originalDirList As New ArrayList
        Dim destinationDirList As New ArrayList
        For nI As Integer = 0 To DataGridView1.Rows.Count - 1
            originalDirList.Add(DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, nI).Value)
            destinationDirList.Add(DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, nI).Value)
        Next

        m_checkAndCopy.CheckAndCopyAsync(destinationDirList, originalDirList, bufferSize)
        'Start multi-thread timer
        TransferTimer.Interval = 50
        TransferTimer.Enabled = True
        'If (CheckAndCopy()) Then
        'Timer1.Interval = CInt(txtHourInt.Text) * 3600 * 1000
        'Timer1.Enabled = True
        'ToolStripStatusLabel1.Text = "Last Check on " & System.DateTime.Now.ToString
        'Else
        'btnStop.PerformClick()
        'ToolStripStatusLabel1.Text = "CheckAndCopy Error!"
        'End If

    End Sub

    Private Sub SaveSettingToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveSettingToolStripMenuItem.Click
        'hour textbox input validation
        Dim hour As Integer
        Try
            hour = CInt(txtHourInt.Text)
        Catch ex As Exception
            MessageBox.Show("Input hours must be numeric!")
            txtHourInt.Focus()
            Exit Sub
        End Try

        Dim sw As New System.IO.StreamWriter("AutoBackup_Setting.cfg", False, System.Text.Encoding.Unicode)
        sw.WriteLine("Created on " & System.DateTime.Now.ToString)
        sw.WriteLine("Copy From:")
        For nI As Integer = 0 To DataGridView1.Rows.Count - 1
            sw.WriteLine(DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, nI).Value.ToString)
        Next
        sw.WriteLine("Copy To:")
        For nI As Integer = 0 To DataGridView1.Rows.Count - 1
            sw.WriteLine(DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, nI).Value.ToString)
        Next

        Dim strShouldMonitor As String = "F"
        If (chbTransferMonitor.Checked) Then
            strShouldMonitor = "T"
        End If

        Dim strAutoStart As String = "F"
        If (chbAutoStartTransfer.Checked) Then
            strAutoStart = "T"
        End If

        Dim strMinimizedTray As String = "F"
        If (chbMinimizedTray.Checked) Then
            strMinimizedTray = "T"
        End If

        Dim strCreateShortCut As String = "F"
        If (StartAtStartupToolStripMenuItem.Checked) Then
            strCreateShortCut = "T"
        End If

        Dim strWaitForReady As String = "F"
        If (WaitForComputerReadyToolStripMenuItem.Checked) Then
            strWaitForReady = "T"
        End If

        Dim strPSserver As String = "F"
        If (chbPowerServer.Checked) Then
            strPSserver = "T"
        End If

        Dim strPSclient As String = "F"
        If (chbPSclient.Checked) Then
            strPSclient = "T"
        End If

        sw.WriteLine("Monitor:" & hour.ToString & strShouldMonitor)
        sw.WriteLine("Auto Start:" & strAutoStart)
        sw.WriteLine("Minimized to Tray:" & strMinimizedTray)
        sw.WriteLine("Create Shortcut:" & strCreateShortCut)
        sw.WriteLine("Wait Computer Ready:" & strWaitForReady)
        sw.WriteLine("As Server:" & strPSserver)
        sw.WriteLine("As Client:" & strPSclient)
        sw.WriteLine("ServerIP:" & txtDestIP.Text)
        sw.WriteLine("Drive Buffer Size: " & bufferSize.ToString)
        sw.Close()
    End Sub

    Private Sub LoadSetting(Optional ByVal subSetting As String = "None")
        If (System.IO.File.Exists("AutoBackup_Setting.cfg")) Then
            Dim sr As New System.IO.StreamReader("AutoBackup_Setting.cfg", System.Text.Encoding.Unicode)
            Dim bCopyFrom As Boolean = False
            Dim bCopyTo As Boolean = False
            'Dim bMonitor As Boolean = False
            Dim monitorRegex As New Regex("^Monitor\:(\d*)([T|F])")
            Dim commFolderRegex As New Regex("^CommunicationFolder\:(.+)$")
            Dim buffersizeRegex As New Regex("^Drive Buffer Size: (\d+)")
            Dim AutoStartRegex As New Regex("^Auto Start\:([T|F])")
            Dim MinimizedTrayRegex As New Regex("^Minimized to Tray\:([T|F])")
            Dim AddShortCutRegex As New Regex("^Create Shortcut\:([T|F])")
            Dim WaitForReadyRegex As New Regex("^Wait Computer Ready\:([T|F])")
            Dim AsServerRegex As New Regex("^As Server\:([T|F])")
            Dim AsClientRegex As New Regex("^As Client\:([T|F])")
            Dim ServerIPRegex As New Regex("^ServerIP\:(.+)$")
            Dim nRow As Integer = 0
            Do Until sr.Peek = -1
                Dim currentLine As String = sr.ReadLine
                If (subSetting.Equals("None")) Then
                    If (currentLine.Equals("Copy From:")) Then
                        bCopyFrom = True
                        bCopyTo = False
                        Continue Do
                    End If
                    If (currentLine.Equals("Copy To:")) Then
                        bCopyFrom = False
                        bCopyTo = True
                        Continue Do
                    End If

                    Dim m As Match = monitorRegex.Match(currentLine)
                    If (m.Success) Then
                        'MessageBox.Show("yeah")
                        txtHourInt.Text = m.Groups(1).ToString
                        Dim strShouldMonitor As String = m.Groups(2).ToString
                        If (strShouldMonitor.Equals("T")) Then
                            chbTransferMonitor.Checked = True
                        End If
                        Continue Do
                    End If

                    Dim m3 As Match = buffersizeRegex.Match(currentLine)
                    If (m3.Success) Then
                        Dim savedBuffer As String = m3.Groups(1).ToString
                        Try
                            bufferSize = CLng(savedBuffer)
                            PopulateListView({"Drive Buffer Size: ", Math.Round(bufferSize / 1073741824, 2).ToString & "GB"})
                            Form2.txtBufferSize.Text = Math.Round(bufferSize / 1073741824, 2).ToString
                        Catch ex As Exception

                        End Try
                        Continue Do
                    End If

                    Dim m4 As Match = AutoStartRegex.Match(currentLine)
                    If (m4.Success) Then
                        If (m4.Groups(1).ToString.Equals("T")) Then
                            chbAutoStartTransfer.Checked = True
                        End If
                        Continue Do
                    End If

                    Dim m5 As Match = MinimizedTrayRegex.Match(currentLine)
                    If (m5.Success) Then
                        If (m5.Groups(1).ToString.Equals("T")) Then
                            chbMinimizedTray.Checked = True
                        End If
                        Continue Do
                    End If

                    Dim m6 As Match = AddShortCutRegex.Match(currentLine)
                    If (m6.Success) Then
                        If (m6.Groups(1).ToString.Equals("T")) Then
                            StartAtStartupToolStripMenuItem.Checked = True
                        End If
                        Continue Do
                    End If

                    Dim m7 As Match = WaitForReadyRegex.Match(currentLine)
                    If (m7.Success) Then
                        If (m7.Groups(1).ToString.Equals("T")) Then
                            'WaitForComputerReadyToolStripMenuItem.Checked = True
                            WaitForComputerReadyToolStripMenuItem.PerformClick()
                        End If
                        Continue Do
                    End If

                    Dim m10 As Match = ServerIPRegex.Match(currentLine)
                    If (m10.Success) Then
                        txtDestIP.Text = m10.Groups(1).ToString
                        Continue Do
                    End If

                ElseIf (subSetting.Equals("Network")) Then
                    Dim m8 As Match = AsServerRegex.Match(currentLine)
                    If (m8.Success) Then
                        If (m8.Groups(1).ToString.Equals("T")) Then
                            chbPowerServer.Checked = True
                        End If
                        Continue Do
                    End If

                    Dim m9 As Match = AsClientRegex.Match(currentLine)
                    If (m9.Success) Then
                        If (m9.Groups(1).ToString.Equals("T")) Then
                            chbPSclient.Checked = True
                            'chbPSclient.CheckState = CheckState.Checked
                        End If
                        Continue Do
                    End If
                End If
                If (bCopyFrom) Then
                    Try
                        'If (Directory.Exists(currentLine)) Then
                        Dim nRowCount As Integer = DataGridView1.Rows.Count
                        DataGridView1.Rows.Add()
                        DataGridView1.Item(DataGridView1.Columns("Original_Directory").Index, nRowCount).Value = currentLine
                        'End If
                    Catch ex As Exception

                    End Try
                End If

                If (bCopyTo) Then
                    Try
                        'Dim nRowCount As Integer = DataGridView1.Rows.Count
                        'DataGridView1.Rows.Add()
                        DataGridView1.Item(DataGridView1.Columns("Destination_Directory").Index, nRow).Value = currentLine
                        nRow = nRow + 1

                    Catch ex As Exception

                    End Try
                End If

            Loop

            sr.Close()
        End If
    End Sub

    

    Private Sub WriteToFile(ByVal filefullName As String, ByVal ipAddress As String, ByVal currentStatus As String)
        Dim sw As New StreamWriter(filefullName, False, System.Text.Encoding.Unicode)
        sw.WriteLine("IPaddress:" & ipAddress)
        sw.WriteLine("Status:" & currentStatus)
        sw.Close()
    End Sub


    Private Sub GetIPAddress()
        Dim nics As NetworkInterface() = NetworkInterface.GetAllNetworkInterfaces
        For Each networkCard In nics
            If networkCard.OperationalStatus = OperationalStatus.Up Then
                Dim multcast As UnicastIPAddressInformationCollection = networkCard.GetIPProperties.UnicastAddresses
                For Each multcastItem As UnicastIPAddressInformation In multcast
                    'For Each address As IPAddress In multcastItem.
                    If multcastItem.Address.AddressFamily = AddressFamily.InterNetwork Then
                        Dim adaptorName As String = networkCard.Description.ToString
                        Dim MacAddress As String = networkCard.GetPhysicalAddress.ToString
                        Dim ipAddress As String = multcastItem.Address.ToString

                        If (adaptorName.Equals(String.Empty)) Then
                            adaptorName = "Unknown_" & System.DateTime.Now.Millisecond.ToString
                        End If

                        If (MacAddress.Equals(String.Empty)) Then
                            MacAddress = "Unknown_" & System.DateTime.Now.Millisecond.ToString
                        End If

                        If (ipAddress.Equals(String.Empty)) Then
                            ipAddress = "Unknown_" & System.DateTime.Now.Millisecond.ToString
                        End If

                        Dim MACaddressFixRegex As New Regex("^(\w{2})(\w{2})(\w{2})(\w{2})(\w{2})(\w{2})$")
                        Dim m As Match = MACaddressFixRegex.Match(MacAddress)
                        If (m.Success) Then
                            MacAddress = m.Groups(1).ToString & "-" & m.Groups(2).ToString & "-" & m.Groups(3).ToString & "-" & m.Groups(4).ToString & "-" & m.Groups(5).ToString & "-" & m.Groups(6).ToString
                        End If

                        adaptorNameArray.Add(adaptorName)
                        ipAddressArray.Add(ipAddress)
                        MacAddressArray.Add(MacAddress)

                        cbNetworkAdaptor.Items.Add(adaptorName & "; " & ipAddress & "; " & MacAddress)
                        cbNetworkAdaptor.SelectedIndex = 0
                    End If
                    'Next
                Next
            End If
        Next
    End Sub

    Private Function CheckFileInUse(ByVal strFileName As String) As Boolean
        Try
            Dim F As Short = CShort(FreeFile())
            FileOpen(F, strFileName, OpenMode.Binary, OpenAccess.ReadWrite, OpenShare.LockReadWrite)
            FileClose(F)
            Return False
        Catch
            'MessageBox.Show(sFile)
            Return True
        End Try
    End Function

    Private Sub chbPSclient_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chbPSclient.CheckedChanged
        If (chbPSclient.Checked) Then
            chbPowerServer.Enabled = False
            txtPScheckInterval.Enabled = False
            chbTransferMonitor.Checked = False
            chbTransferMonitor.Enabled = False
            lblHours.Enabled = False
            Label1.Enabled = False
            Dim DestIParray As String() = Split(txtDestIP.Text, ":")
            m_clienttest.MaxConnectionAttempt = 10
            m_clienttest.CurrentClientStatus = "Wake Up"  'reset status if chbPSclient is physically checked
            m_clienttest.ClientConnectSocketAsync(DestIParray(0), DestIParray(1), MaxConnectRetry)
            m_clienttest.MACadd = MacAddressArray(0).ToString
            m_clienttest.IPadd = ipAddressArray(0).ToString
            chbTransferMonitor.Checked = False
            chbAutoStartTransfer.Checked = False
            chbTransferMonitor.Enabled = False
            chbAutoStartTransfer.Enabled = False
        Else
            chbPowerServer.Enabled = True
            txtPScheckInterval.Enabled = True
            chbTransferMonitor.Enabled = True
            lblHours.Enabled = True
            Label1.Enabled = True
            m_clienttest.CloseSocketConnection()
            chbTransferMonitor.Enabled = True
            chbAutoStartTransfer.Enabled = True
        End If
    End Sub

    Private Sub chbPowerServer_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chbPowerServer.CheckedChanged
        If (chbPowerServer.Checked) Then
            chbPSclient.Enabled = False
            txtPScheckInterval.Enabled = True
            chbTransferMonitor.Checked = True
            chbTransferMonitor.Enabled = True
            lblHours.Enabled = True
            Label1.Enabled = True
            m_newListerner.SetIP = ipAddressArray(0).ToString '"127.0.0.1" 
            m_newListerner.SetSleepHrs = CInt(txtPScheckInterval.Text)
            m_newListerner.ServerListenerAsync()

        Else
            chbPSclient.Enabled = True
            txtPScheckInterval.Enabled = True
            chbTransferMonitor.Enabled = True
            lblHours.Enabled = True
            m_newListerner.StopListenerAsync(True)
        End If
    End Sub


    Private Sub TransferTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TransferTimer.Tick
        Console.WriteLine("TransferTimer Started!")
        If (m_checkAndCopy.FinishMoving) Then
            'terminate transfer timer
            TransferTimer.Interval = 1000000000
            TransferTimer.Enabled = False

            'update log file
            'UpdateLog(LogList)
            'LogList.Clear()

            'update Progressbar
            ProgressBar1.Value = 0
            ToolStripProgressBar1.Value = 0

            'reset total file size count
            ' m_checkAndCopy.CurrentSumTransferSize = 0

            btnStop.Enabled = True

            'if Auto transfer is checked
            If (chbTransferMonitor.Checked) Then
                    Dim hour As Integer
                    Try
                        hour = CInt(txtHourInt.Text)
                    Catch ex As Exception
                        MessageBox.Show(ex.Message)
                        btnStop.PerformClick()
                        Exit Sub
                    End Try
                Timer1.Interval = CInt(txtHourInt.Text) * 3600 * 1000
                    Timer1.Enabled = True
                ToolStripStatusLabel1.Text = "Last Check On " & System.DateTime.Now.ToString
                NotifyIcon1.Text = "Darwin's Backup Program: " & ToolStripStatusLabel1.Text
            Else
                btnStop.PerformClick()
            End If

            'If Client mode is selected
            If (chbPSclient.Checked) Then
                m_clienttest.SendMessageSocketAsync(m_clienttest.IPadd & ": Transfer Done!")
                m_clienttest.CurrentClientStatus = "Transfer Done"
            End If

            'reset global variables
            m_checkAndCopy.FinishMoving = False
            m_checkAndCopy.MovingSucceed = True
            '_PreviousMessage = String.Empty
            '_ListViewMessage(0) = Nothing
            '_ToolstripMessage = String.Empty
        Else
            'If (Not (_ListViewMessage(0) Is Nothing)) Then
            'If ((Not _ListViewMessage(0).Equals(_PreviousMessage))) Then
            'ToolStripStatusLabel1.Text = _ToolstripMessage
            'PopulateListView(_ListViewMessage)
            '_PreviousMessage = _ListViewMessage(0)
            'Console.WriteLine(_PreviousMessage)
            'Console.WriteLine(_ListViewMessage(0))
            'ProgressBar1.Value = CInt(Math.Round(ProgressBar1.Maximum * _CurrentSumTransferSize / _TotalAdjTransferSize, 0))
            ' End If
            'End If

        End If
    End Sub

    Private Sub Button1_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Dim files As Integer = 0
        Dim folders As Integer = 0
        'getAllFiles(DataGridView1.Item(1, 0).Value.ToString, folders, files)
        'PopulateListView({"Folders: " & folders & ", Files: " & files, ""})
        PopulateListView({"CurrentPath: " & Application.StartupPath & ", ProgramName: " & Application.ProductName})
    End Sub

    Private Sub getAllFiles(ByRef targetDir As String, ByRef folderCount As Integer, ByRef fileCount As Integer)
        Dim di As New DirectoryInfo(targetDir)
        Dim dis As DirectoryInfo() = di.GetDirectories
        Dim fis As FileInfo() = di.GetFiles()

        For Each fi As FileInfo In fis
            PopulateListView({fi.FullName, ""})
            fileCount = fileCount + 1
        Next

        For Each folder As DirectoryInfo In dis
            getAllFiles(folder.FullName, folderCount, fileCount)
            folderCount = folderCount + 1
        Next
    End Sub

    Private Sub MinimizeToTray()
        Me.WindowState = FormWindowState.Minimized
        Me.ShowInTaskbar = False
        Me.Hide()
        NotifyIcon1.Visible = True

        NotifyIcon1.BalloonTipText = "Darwin's Backup Program Working in the background!!!"
        NotifyIcon1.ShowBalloonTip(250)
        'Me.WindowState = FormWindowState.Minimized
        'Else
        'NotifyIcon1.Visible = False
    End Sub

    Private Sub NotifyIcon1_MouseDoubleClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        Me.Show()
        Me.ShowInTaskbar = True
        Me.WindowState = FormWindowState.Normal
        NotifyIcon1.Visible = False
    End Sub

    Private Sub Form1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If (Me.WindowState = FormWindowState.Minimized) Then
            m_checkAndCopy.ShowTransferUI = False
            'Debug.Print(m_checkAndCopy.ShowTransferUI.ToString)
        Else
            m_checkAndCopy.ShowTransferUI = True
            'Debug.Print(m_checkAndCopy.ShowTransferUI.ToString)
        End If
        If (chbMinimizedTray.Checked And (Me.WindowState = FormWindowState.Minimized)) Then
            MinimizeToTray()
        End If
    End Sub

    Private Sub AddStartUpShortcut(ByVal bStartUp As Boolean)
        Dim SystemStartUpPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Startup)
        If (bStartUp) Then
            CreateShortCut(SystemStartUpPath)
        Else
            If (System.IO.File.Exists(Path.Combine({SystemStartUpPath, ProgramName & " - shortcut.lnk"}))) Then
                System.IO.File.Delete(Path.Combine({SystemStartUpPath, ProgramName & " - shortcut.lnk"}))
            End If
        End If
    End Sub

    Private Function CreateShortCut(ByVal DestDir As String) As Boolean
        Dim bNoError As Boolean = True
        Dim WshShell As New WshShell
        Dim MyShortcut As IWshRuntimeLibrary.IWshShortcut
        Try
            If (System.IO.File.Exists(Path.Combine({DestDir, ProgramName & " - shortcut.lnk"}))) Then
                System.IO.File.Delete(Path.Combine({DestDir, ProgramName & " - shortcut.lnk"}))
            End If
            MyShortcut = CType(WshShell.CreateShortcut(Path.Combine({DestDir, ProgramName & " - shortcut.lnk"})), IWshRuntimeLibrary.IWshShortcut)
            MyShortcut.TargetPath = Application.ExecutablePath
            MyShortcut.WorkingDirectory = Application.StartupPath
            MyShortcut.Save()

        Catch ex As Exception
            bNoError = False
        End Try

        Return bNoError
    End Function

    Private Sub StartAtStartupToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StartAtStartupToolStripMenuItem.Click
        If (StartAtStartupToolStripMenuItem.Checked) Then
            AddStartUpShortcut(True)
        Else
            AddStartUpShortcut(False)
        End If
    End Sub

    Private Sub btnConnectLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnConnectLog.Click
        ConnectionLog.Show()
    End Sub

    Private Sub btnTest_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTest.Click
        m_clienttest.SendMessageSocketAsync("Wake Up")
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        m_clienttest.CheckSocketConnectionStatus()
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Dim DestIParray As String() = Split(txtDestIP.Text, ":")
        m_clienttest.ClientConnectSocketAsync(DestIParray(0), DestIParray(1), MaxConnectRetry)
    End Sub

    Private Sub WaitForComputerReadyToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles WaitForComputerReadyToolStripMenuItem.Click
        If (WaitForComputerReadyToolStripMenuItem.Checked) Then
            WaitForComputerReady(WaitForComputerReadyInterval)
        End If
    End Sub

    Private Sub ShutdownComputer()
        m_clienttest.SendMessageSocketAsync(m_clienttest.IPadd & ": Shutdown Command Sent! Shutting Down...")
        Dim shutdownMode As String = "/r"  'restart is default
        If (True) Then
            shutdownMode = "/s"
        Else
            shutdownMode = "/r"
        End If
        System.Diagnostics.Process.Start("cmd.exe", "/C shutdown " & shutdownMode)  '/K execute and stay, /C execute and close
    End Sub

    Private Sub WaitForComputerReady(ByVal WaitingIntervalInSec As Integer)
        WaitForComputerReadyTimer.Interval = WaitingIntervalInSec * 1000
        WaitForComputerReadyTimer.Start()
        TimeCounter.Interval = TimeCounterInterval
        TimeCounter.Start()
        WaitForComputerReadyForm.ShowDialog()
    End Sub

    Private Sub WaitForComputerReadyTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles WaitForComputerReadyTimer.Tick
        timeCount1 = 0
        TimeCounter.Stop()
        WaitForComputerReadyForm.Close()
        WaitForComputerReadyTimer.Stop()
    End Sub

    Private Sub TimeCounter_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TimeCounter.Tick
        timeCount1 = timeCount1 + TimeCounterInterval
        WaitForComputerReadyForm.btnStopWaitingReady.Text = "Still have " & (WaitForComputerReadyInterval - (timeCount1 / 1000)).ToString & "s left (Press to stop waiting!)"
    End Sub

End Class
