Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Security.Cryptography
Imports System.Net.NetworkInformation
Imports System.Net.Sockets

Public Class CheckAndCopy
    Private Declare Function GetDiskFreeSpaceEx Lib "kernel32" Alias "GetDiskFreeSpaceExA" _
       (ByVal lpDirectoryName As String, ByRef lpFreeBytesAvailableToMe As IntPtr, _
       ByRef lpTotalNumberOfBytes As IntPtr, ByRef lpTotalNumberOfFreeBytes As IntPtr) As IntPtr

    'Structure for overlapping files
    Structure CoExistFileCondition
        Public OriginalFileInfo As FileInfo
        Public DestinationFileInfo As FileInfo
        Public OriginalFullFilePath As String
        Public DestinationFullFilePath As String

        Public bOriginalFileExists As Boolean
        Public bDestinationFileExists As Boolean
        Public bDestinationFileDeletable As Boolean
        Public bDestinationFileNeedDelete As Boolean
        Public bCopyReady As Boolean
    End Structure

    'Structure for gathering file lists
    Structure FileListInfo
        Public FileCount As Integer
        Public FolderCount As Integer
        Public FileList As ArrayList
        Public bError As Boolean
        Public ErrorMessage As String
    End Structure

    'Private bTerminate As Boolean = False
    Private CurrentStatus As String = ""
    Private MovedCount As Integer = 0
    Private bCheckCopyNormal As Boolean = True
    Private bFinish As Boolean = False
    Private bShowTransferUI As Boolean = True
    Private bufferSize As Int64 = 0 'CLng(Form2.txtBufferSize.Text) * 1073741824    default 100GB
    Dim _TotalAdjTransferSize As Int64 = 0
    Dim _CurrentSumTransferSize As Int64 = 0
    Dim LogList As New ArrayList


    Private _lblDestDir As String = ""
    Private _lstTransferDirectories As New ArrayList

    Public Property DestinationDir() As String
        Get
            Return _lblDestDir
        End Get
        Set(ByVal value As String)
            _lblDestDir = value
        End Set
    End Property

    Public Property TransferDirectories() As ArrayList
        Get
            Return _lstTransferDirectories
        End Get
        Set(ByVal value As ArrayList)
            For nI As Integer = 0 To value.Count - 1
                _lstTransferDirectories.Add(value(nI))
            Next
        End Set
    End Property

    Public Property FinishMoving() As Boolean
        Get
            Return bFinish
        End Get
        Set(ByVal value As Boolean)
            bFinish = value
        End Set
    End Property

    Public Property ShowTransferUI() As Boolean
        Get
            Return bShowTransferUI
        End Get
        Set(ByVal value As Boolean)
            bShowTransferUI = value
        End Set
    End Property

    Public Property MovingSucceed() As Boolean
        Get
            Return bCheckCopyNormal
        End Get
        Set(ByVal value As Boolean)
            bCheckCopyNormal = value
        End Set
    End Property

    Public Property NumberOfFileMoved() As Integer
        Get
            Return MovedCount
        End Get
        Set(ByVal value As Integer)
            MovedCount = value
        End Set
    End Property

    Public ReadOnly Property TotalAdjustTransferSize As Int64
        Get
            Return _TotalAdjTransferSize
        End Get
    End Property

    Public ReadOnly Property CurrentSumTransferSize As Int64
        Get
            Return _CurrentSumTransferSize
        End Get
    End Property

    'multithread synchronization
    Private context As Threading.SynchronizationContext = Threading.SynchronizationContext.Current

    Public Event UpdateListDirCheckProgress As EventHandler(Of UpdateProgressEventArgs)
    Protected Overridable Sub OnUpdateListDirCheckProgress(ByVal e As UpdateProgressEventArgs)
        RaiseEvent UpdateListDirCheckProgress(Me, e)
    End Sub

    Public Event UpdateToolBarProgress As EventHandler(Of UpdateToolProgressEventArgs)
    Protected Overridable Sub OnUpdateToolBarProgress(ByVal e As UpdateToolProgressEventArgs)
        RaiseEvent UpdateToolBarProgress(Me, e)
    End Sub

    Public Event RemoveItemOriListDir As EventHandler(Of RemoveItemFromListOriDir)
    Protected Overridable Sub OnRemoveItemOriListDir(ByVal e As RemoveItemFromListOriDir)
        RaiseEvent RemoveItemOriListDir(Me, e)
    End Sub

    Public Event UpdateListStatus As EventHandler(Of UpdateListStatusEventArgs)
    Protected Overridable Sub OnUpdateListStatus(ByVal e As UpdateListStatusEventArgs)
        RaiseEvent UpdateListStatus(Me, e)
    End Sub

    Public Event UpdateToolStripStatus As EventHandler(Of UpdateToolStripStatusEventArgs)
    Protected Overridable Sub OnUpdateToolStripStatus(ByVal e As UpdateToolStripStatusEventArgs)
        RaiseEvent UpdateToolStripStatus(Me, e)
    End Sub

    Public Event StopCheckAndCopy As EventHandler(Of TerminateCopyEventArgs)
    Protected Overridable Sub OnStopCheckAndCopy(ByVal e As TerminateCopyEventArgs)
        RaiseEvent StopCheckAndCopy(Me, e)
    End Sub

    Public Event UpdateDestDirTextAndColor As EventHandler(Of UpdatelbDestDirTextAndColorEventArgs)
    Protected Overridable Sub OnUpdateDestDirTextAndColor(ByVal e As UpdatelbDestDirTextAndColorEventArgs)
        RaiseEvent UpdateDestDirTextAndColor(Me, e)
    End Sub

    Public Event UpdateMovedStatus As EventHandler(Of UpdateMovedStatusEventArgs)
    Protected Overridable Sub OnUpdateMovedStatus(ByVal e As UpdateMovedStatusEventArgs)
        RaiseEvent UpdateMovedStatus(Me, e)
    End Sub

    'Multithread function
    Public Sub CheckAndCopyAsync(ByVal DestDirList As ArrayList, ByVal OriList As ArrayList, ByVal buffer As Int64)
        'ThreadExtensions.QueueUserWorkItem(New Action(Of String, ArrayList)(AddressOf CheckAndCopy), DestDir, OriList)
        bufferSize = buffer
        ThreadExtensions.QueueUserWorkItem(New Action(Of ArrayList, ArrayList)(AddressOf MultipleCheckAndCopy), DestDirList, OriList)
    End Sub

    'function to handle multiple directory copy
    Private Sub MultipleCheckAndCopy(ByVal Destination As ArrayList, ByVal Original As ArrayList)
        'reset global variables
        bFinish = False
        For nI As Integer = 0 To Destination.Count - 1
            ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Working...", nI))

            If (Not (Directory.Exists(Destination(nI).ToString) Or Directory.Exists(Original(nI).ToString))) Then
                ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Destination/Original Dir not exist", nI))
            ElseIf (Not Directory.Exists(Destination(nI).ToString)) Then
                ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Destination Dir not exist", nI))
            ElseIf (Not Directory.Exists(Original(nI).ToString)) Then
                ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Original Dir not exist", nI))
            Else
                Dim tempArraylist As New ArrayList
                tempArraylist.Add(Original(nI))
                CheckAndCopy(Destination(nI).ToString, tempArraylist, nI)

            End If

            ThreadExtensions.ScSend(context, New Action(Of UpdateProgressEventArgs)(AddressOf OnUpdateListDirCheckProgress), New UpdateProgressEventArgs((CDbl(nI + 1) / CDbl(Destination.Count))))

        Next

        bFinish = True
    End Sub

    'Main function
    Private Sub CheckAndCopy(ByVal DestinationDir As String, ByVal OriginalDirectoryList As ArrayList, ByVal nCurrentRowIndex As Integer)

        'reset global variables
        Dim bError As Boolean = False
        bCheckCopyNormal = True
        _lblDestDir = DestinationDir
        _lstTransferDirectories.Clear()
        For nA As Integer = 0 To OriginalDirectoryList.Count - 1
            _lstTransferDirectories.Add(OriginalDirectoryList(nA))    'inherit from old transfer method, only contain one directory
        Next
        CurrentStatus = ""
        MovedCount = 0
        bFinish = False
        _TotalAdjTransferSize = 0
        _CurrentSumTransferSize = 0
        LogList.Clear()

        'If (TurnOnBothDrive(_lstTransferDirectories(0).ToString, DestinationDir)) Then
        If (True) Then
            '''''check the total transfer directory size and make sure it is less than the transfer-to drive freespace'''''
            Dim totalTransferSize As Int64 = 0
            Dim adjustTransferSize As Int64 = 0
            Dim DriveFreeSpace As Int64 = 0

            Try
                DriveFreeSpace = CLng(DiskFreeSpace(_lblDestDir))  'free disk space

            Catch ex As Exception
            End Try

            'check to see if files and directories of the destination can be accessed by the program, also try to build the file dict for the destination folder
            'Dim destinationFileDict As New Dictionary(Of String, Dictionary(Of String, String))  'file name -> directory -> file size
            'Dim transferFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) 'transfer directory -> file name -> original dir -> destination dir
            ' Dim ToBeTransferFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, FileInfo))) 'transfer directory -> file name -> destination dir -> Size         ####102715 changed to fileinfo
            'Dim removeFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, FileInfo)))    'transfer directory -> file name -> destination dir -> Size          ####102715 changed to fileinfo

            Dim destError As Integer = 0
            'Dim destDirInfo As New DirectoryInfo(lblDestDir.Text)
            'GatherDirToDict(destDirInfo, destinationFileDict, destError)     'gather destination file location and size


            'total dir space and remove the one that is the same as the destination folder, also build the transferFileDict
            Dim originallstItemCount As Integer = _lstTransferDirectories.Count  'only one directory so always equal to 1
            Dim destRootDirList As New ArrayList


            'If (_lstTransferDirectories(nI).ToString.Equals(_lblDestDir)) Then                  'if the transfer directory is the same as destination directory, warn and ignore it

            'PopulateListView({"Remove " & lstTransferDirectories.Items(nI).ToString, "Same as Destination"})
            'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Removing " & _lstTransferDirectories(nI).ToString & ", Removed"))
            'ThreadExtensions.ScSend(context, New Action(Of RemoveItemFromListOriDir)(AddressOf OnRemoveItemOriListDir), New RemoveItemFromListOriDir(nI))
            'lstTransferDirectories.Items.RemoveAt(nI)
            '_lstTransferDirectories.RemoveAt(nI)

            'Dim d As New DirectoryInfo(_lstTransferDirectories(nI).ToString)
            'Dim nError As Integer = 0
            'Dim eachTransDirDict As New Dictionary(Of String, Dictionary(Of String, FileInfo))              'fileName -> original dir -> dest dir

            'get all file list from destination and original dir
            Dim AllFilesOriginalDir As New FileListInfo
            Dim tempArrayList As New ArrayList
            AllFilesOriginalDir.FileList = tempArrayList
            Dim AllFilesDestinationDir As New FileListInfo
            Dim tempArrayList2 As New ArrayList
            AllFilesDestinationDir.FileList = tempArrayList2

            Dim newRootDir As String = ""
            Try
                ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Getting File List...."))
                GetAllFilesInDirectory(_lstTransferDirectories(0).ToString, AllFilesOriginalDir.FileList, AllFilesOriginalDir.FolderCount, AllFilesOriginalDir.FileCount)

                newRootDir = GetDestinationRootDir(_lstTransferDirectories(0).ToString, DestinationDir)   ''''''''''''''''''''''''ERROR CONTROL PLEASE"""""""""""""""""""""DONE
                'MessageBox.Show(AllFilesOriginalDir.FileList(0).ToString)
                GetAllFilesInDirectory(newRootDir, AllFilesDestinationDir.FileList, AllFilesDestinationDir.FolderCount, AllFilesDestinationDir.FileCount)
                'MessageBox.Show("yeah")
            Catch ex As Exception
                ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Getting File Error: " & ex.Message, nCurrentRowIndex))
                bError = True
                bCheckCopyNormal = False
                Exit Sub      '<------ terminate start sub
            End Try


            'build a potential new destination file path dict
            Dim newDestFileDict As New Dictionary(Of String, String)      'newDestFilePath -> originalFilePath
            For Each originalFile In AllFilesOriginalDir.FileList
                Dim temp As String = GetFullDestFilePath(originalFile.ToString, _lstTransferDirectories(0).ToString, newRootDir)
                newDestFileDict.Add(temp, originalFile.ToString)
            Next

            'build a transfer file list
            ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Building File List for Transfer..."))
            Dim FINALtransferList As New ArrayList
            For Each newFilePath As String In newDestFileDict.Keys
                ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Building File List: " & newDestFileDict(newFilePath)))
                Dim temp1 As New CoExistFileCondition
                temp1.OriginalFullFilePath = newDestFileDict(newFilePath)
                temp1.DestinationFullFilePath = newFilePath
                If (File.Exists(temp1.OriginalFullFilePath)) Then
                    temp1.OriginalFileInfo = New FileInfo(newDestFileDict(newFilePath))
                    temp1.bOriginalFileExists = True
                Else
                    temp1.bOriginalFileExists = False
                End If
                If (File.Exists(temp1.DestinationFullFilePath)) Then
                    temp1.DestinationFileInfo = New FileInfo(newFilePath)
                    temp1.bDestinationFileExists = True
                Else
                    temp1.bDestinationFileExists = False
                End If
                'check file ready to copy
                If (temp1.bOriginalFileExists) Then
                    If (temp1.bDestinationFileExists) Then
                        'compare last modified time if not the same delete the destination file and copy over
                        If (temp1.OriginalFileInfo.LastWriteTime.ToString.Equals(temp1.DestinationFileInfo.LastWriteTime.ToString)) Then

                            'If (FileCheckSumMatch(temp1.OriginalFileInfo.FullName, temp1.DestinationFileInfo.FullName)) Then 'modified to file checksum...change back to file modified because too slow
                            temp1.bCopyReady = False
                        Else
                            temp1.bDestinationFileNeedDelete = True
                            temp1.bCopyReady = False
                        End If
                    Else
                        temp1.bCopyReady = True
                    End If
                Else
                    temp1.bCopyReady = False
                End If

                'add to FINALtransferList
                FINALtransferList.Add(temp1)
            Next


            'build a delete file list      (Please go through FINALtransferList and FINALdeleteList when trying to delete files from destination dir)
            Dim FINALdeleteList As New ArrayList
            For Each DestinationFilePath In AllFilesDestinationDir.FileList
                If (Not newDestFileDict.ContainsKey(DestinationFilePath.ToString)) Then
                    ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Building Delete file list: " & DestinationFilePath.ToString))
                    Dim temp2 As New CoExistFileCondition
                    temp2.DestinationFullFilePath = DestinationFilePath.ToString
                    If (File.Exists(temp2.DestinationFullFilePath)) Then
                        temp2.DestinationFileInfo = New FileInfo(temp2.DestinationFullFilePath)
                        temp2.bDestinationFileNeedDelete = True
                        temp2.bDestinationFileExists = True
                    Else
                        temp2.bDestinationFileExists = False
                        temp2.bDestinationFileDeletable = False
                    End If
                    FINALdeleteList.Add(temp2)
                End If
            Next

            'calculate file transfer size
            ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Calculating Total File Transfer Size..."))
            For Each thing As CoExistFileCondition In FINALtransferList
                If (thing.bCopyReady) Then
                    totalTransferSize = totalTransferSize + thing.OriginalFileInfo.Length
                ElseIf (thing.bDestinationFileNeedDelete) Then
                    totalTransferSize = totalTransferSize + thing.OriginalFileInfo.Length - thing.DestinationFileInfo.Length
                End If
            Next
            adjustTransferSize = totalTransferSize


            'calculate file delete size
            Dim totalDeleteFileSize As Int64 = 0
            For Each thing As CoExistFileCondition In FINALdeleteList
                If (thing.bDestinationFileNeedDelete) Then
                    totalDeleteFileSize = totalDeleteFileSize + thing.DestinationFileInfo.Length
                End If
            Next



            'build the moveFileDict and adjust transferFileDict and removeFileDict, also calculate totalMoveFileSize         #####Suspend this as of 102715#####

            Dim moveFileDict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) 'transfer directory -> file name -> original dir -> destination dir
            Dim totalMoveFileSize As Int64 = 0
            If (False) Then
                'Dim bMoveFileDict As Boolean = BuildMoveFileDict(transferFileDict, ToBeTransferFileDict, removeFileDict, moveFileDict, totalMoveFileSize)
            End If
            'adjustTransferSize = adjustTransferSize - totalMoveFileSize

            'Populate list view status
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Free Space in " & _lblDestDir & ":", Math.Round(DriveFreeSpace / 1048576, 2).ToString & "MB"}, ",")))
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Total file transfer size: ", Math.Round(totalTransferSize / 1048576, 2).ToString & "MB"}, ",")))
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Total file delete size: ", Math.Round(totalDeleteFileSize / 1048576, 2).ToString & "MB"}, ",")))
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Adjusted Total file transfer size: ", Math.Round((adjustTransferSize - totalDeleteFileSize) / 1048576, 2).ToString & "MB"}, ",")))
            _TotalAdjTransferSize = adjustTransferSize - totalMoveFileSize



            ''''''''Delete the files from the destination folder first, then copy the files to the destination folder''''''''
            If (((adjustTransferSize - totalDeleteFileSize + bufferSize) < DriveFreeSpace) And (adjustTransferSize > 0 Or totalMoveFileSize > 0) And (_lstTransferDirectories.Count > 0) And (destError = 0)) Then
                'PopulateListView({"Directory size check", "Okay"})
                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Directory size check", "Okay"}, ",")))
                Try
                    'If (bMoveFileDict) Then
                    'TransferFileToDestination(moveFileDict, True, LogList)    'move the files
                    'MessageBox.Show("Moving")
                    'End If

                    'Delete files/folder from the destination directory
                    'Dim bDeleteFileFromDest As Boolean = True

                    DeleteFileFromDestination2(FINALtransferList, FINALdeleteList, LogList)
                    UpdateLog(LogList)

                    'delete empty folder in the newDestDir
                    If (Directory.Exists(newRootDir)) Then
                        DeleteEmptyFolderInDestDir(newRootDir, True)
                    End If


                    ' If (bDeleteFileFromDest) Then

                    'Start multi-thread timer
                    'TransferTimer.Interval = 10 '0.1 second
                    'TransferTimer.Enabled = True

                    'copy file to the destination folder
                    'Dim th As New System.Threading.Thread(Sub()
                    'TransferFileToDestMulti(transferFileDict, False, LogList, _ListViewMessage, _ToolstripMessage, _bTransferDone, _bTransferSucceed, _CurrentSumTransferSize)
                    '                                     End Sub)
                    'th.Start()

                    'update destination Drive space
                    Dim UpdatedDestinationDriveFreeSpace As Int64 = CLng(DiskFreeSpace(_lblDestDir))
                    If (totalTransferSize + bufferSize < UpdatedDestinationDriveFreeSpace) Then
                        'MessageBox.Show("Transferring")
                        TransferFileToDestination2(FINALtransferList, LogList, 5, 10000)    'transfer the files

                        UpdateLog(LogList)
                    Else
                        ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Directory size check", "Need " & Math.Round((adjustTransferSize - totalDeleteFileSize + bufferSize - DriveFreeSpace) / 1048576, 2) & "MB"}, ",")))
                        ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Destination Drive Not Enough Space ", nCurrentRowIndex))
                        bError = True
                    End If
                    ' Else
                    'PopulateListView({"Error deleting files from the Destination folder! Check log...", "DeleteFileFromDestinationError"})
                    'btnStop.PerformClick()    '<----- stop the timer
                    'bNormal = False
                    '  ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Error deleting files from the Destination folder! Check log...", "DeleteFileFromDestinationError"}, ",")))
                    'ThreadExtensions.ScSend(context, New Action(Of TerminateCopyEventArgs)(AddressOf OnStopCheckAndCopy), New TerminateCopyEventArgs(True))
                    '   ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Error deleting files from the Destination folder! Check log...", nCurrentRowIndex))
                    '    bCheckCopyNormal = False
                    'bFinish = True
                    '    Exit Sub      '<------ terminate start sub
                    'End If
                Catch ex As Exception
                    ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs(ex.Message, nCurrentRowIndex))
                    bCheckCopyNormal = False
                    'bFinish = True
                    Exit Sub      '<------ terminate start sub
                End Try

            ElseIf (adjustTransferSize <= 0) Then              ''''''if total transfer size is 0MB
                'PopulateListView({"Total transfer size is zero! ", "No Files"})
                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Total transfer size is zero! ", "No Files"}, ",")))
                ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Total transfer size is zero! ", nCurrentRowIndex))
                'delete files from destination folder
                'Dim bTemp As Boolean = True
                DeleteFileFromDestination2(FINALtransferList, FINALdeleteList, LogList)
                UpdateLog(LogList)

                'delete empty folder in the newDestDir
                If (Directory.Exists(newRootDir)) Then
                    DeleteEmptyFolderInDestDir(newRootDir, True)
                End If

                'If (chbPSclient.Checked) Then
                'change timer2 to 5min
                'Timer2.Interval = 300000
                'Timer2.Enabled = True

                'System.Threading.Thread.Sleep(3000)
                'WriteToFile(_ClientCommPath, _ClientIP, "Sleeping")
                'System.Threading.Thread.Sleep(10000)
                '''''''''''''''''''''''''''''''''''''put computer to sleep here''''''''''''''''''''''''''''''
                'System.Diagnostics.Process.Start("cmd.exe", "/C powercfg -hibernate off")
                'System.Diagnostics.Process.Start("cmd.exe", "/C rundll32.exe powrprof.dll,SetSuspendState 0,1,0")

                'End If

            Else
                If (destError > 0) Then
                    'PopulateListView({"Destination folder read error!", "Error"})
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Destination folder read error!", "Error"}, ",")))
                    ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Destination folder read error! ", nCurrentRowIndex))
                    bError = True
                    'ThreadExtensions.ScSend(context, New Action(Of UpdatelbDestDirTextAndColorEventArgs)(AddressOf OnUpdateDestDirTextAndColor), New UpdatelbDestDirTextAndColorEventArgs("Select Destination Directory", Color.Red))
                    'lblDestDir.Text = "Select Destination Directory"
                    'lblDestDir.ForeColor = Color.Red
                End If
                If (_lstTransferDirectories.Count <= 0) Then
                    'PopulateListView({"No item on the transfer list!", "Error"})
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"No item on the transfer list!", "Error"}, ",")))
                End If
                If ((adjustTransferSize + bufferSize) >= DriveFreeSpace) Then
                    'PopulateListView({"Directory size check", "Need " & Math.Round((totalTransferSize + bufferSize - DriveFreeSpace) / 1048576, 2) & "MB"})
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Directory size check", "Need " & Math.Round((adjustTransferSize - totalDeleteFileSize + bufferSize - DriveFreeSpace) / 1048576, 2) & "MB"}, ",")))
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Destination folder read error!", "Error"}, ",")))
                    ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Destination folder not enough space! ", nCurrentRowIndex))
                    bError = True
                    'ThreadExtensions.ScSend(context, New Action(Of UpdatelbDestDirTextAndColorEventArgs)(AddressOf OnUpdateDestDirTextAndColor), New UpdatelbDestDirTextAndColorEventArgs("Select Destination Directory", Color.Red))
                    'lblDestDir.Text = "Select Destination Directory"
                    'lblDestDir.ForeColor = Color.Red
                End If

                'chbPSclient.Checked = False   '<----- disable auto shutdown
                'btnStop.PerformClick()    '<----- stop the timer
                'ThreadExtensions.ScSend(context, New Action(Of TerminateCopyEventArgs)(AddressOf OnStopCheckAndCopy), New TerminateCopyEventArgs(True))
                bCheckCopyNormal = False
                'bFinish = True
                Exit Sub      '<------ terminate start sub
            End If


                ' bFinish = True
        Else
            ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Spinning Up Drive Error ", nCurrentRowIndex))
            bError = True
        End If

        If (Not bError) Then
            ThreadExtensions.ScSend(context, New Action(Of UpdateMovedStatusEventArgs)(AddressOf OnUpdateMovedStatus), New UpdateMovedStatusEventArgs("Done " & System.DateTime.Now.ToString, nCurrentRowIndex))
        End If
    End Sub

    Private Function GetDestinationRootDir(ByVal OriginalDir As String, ByVal DestDir As String) As String
        'get the folder name from the input string
        Dim inputFolderRegex As New Regex("([^\\]+)\\?$")
        Dim m As Match = inputFolderRegex.Match(OriginalDir)
        If (m.Success) Then
            'get folder name
            Dim folderName As String = m.Groups(1).ToString
            If (folderName.Substring(folderName.Length - 1, 1).Equals(":")) Then
                folderName = folderName.Substring(0, folderName.Length - 1) & " Drive"     'folder name for drive letter is "x Drive"
            End If
            'build destination folder dir
            Dim newDestDir As String = DestDir
            If (Not newDestDir.Substring(newDestDir.Length - 1, 1).Equals("\")) Then
                newDestDir = newDestDir & "\"
            End If
            newDestDir = newDestDir & folderName & "\"
            Return newDestDir
        End If
        Return "#N/A"
    End Function

    Private Sub DeleteEmptyFolderInDestDir(ByVal DestDir As String, ByVal rootdir As Boolean)
        '(di.Name.ToString.Equals("$RECYCLE.BIN") Or di.Name.ToString.Equals("System Volume Information"))
        '(fi.Name.ToString.Equals("Thumbs.db"))
        'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({di.Name.ToString & " found!", "Skipped!"}, ",")))

        Dim currentDirInfo As New DirectoryInfo(DestDir)
        Dim currentDirArray As DirectoryInfo() = currentDirInfo.GetDirectories()

        For Each di As DirectoryInfo In currentDirArray
            If (di.Name.Equals("$RECYCLE.BIN") Or di.Name.Equals("System Volume Information")) Then
                Continue For
            Else
                DeleteEmptyFolderInDestDir(di.FullName, False)
                Try
                    Directory.Delete(di.FullName)
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleted " & di.FullName, "DeleteEmptyFolder"}, ",")))
                Catch ex As Exception
                    'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message & "( " & di.FullName & " )", "DeleteEmptyFolderError!"}, ",")))
                End Try
            End If
        Next

        If (Not rootdir) Then
            Dim currentFileArray As FileInfo() = currentDirInfo.GetFiles()
            For Each fi As FileInfo In currentFileArray
                If (fi.Name.Equals("Thumbs.db")) Then
                    Try
                        File.Delete(fi.FullName)
                        ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleted " & fi.FullName, "DeleteEmptyFolder"}, ",")))
                    Catch ex As Exception
                        'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message & "( " & fi.FullName & " )", "DeleteEmptyFolderError!"}, ",")))
                    End Try
                End If
            Next
        End If

    End Sub

    Private Function DiskFreeSpace(ByVal strDestination As String) As String    'return available in bytes
        Try
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
        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message, "DiskFreeSpaceError"}, ",")))
        End Try

        Return "N/A"
    End Function

    Private Function GatherDirToDict(ByVal d As DirectoryInfo, ByRef dict As Dictionary(Of String, Dictionary(Of String, FileInfo)), ByRef errorBit As Integer) As Int64   'return Dir Size
        'file name -> directory -> file size   ####102715 changed to fileinfo
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
                    'PopulateListView({fi.Name.ToString & " found!", (fi.Length / 1048576).ToString & "MB"})
                    Continue For
                End If

                Dim temp As String = fi.DirectoryName.ToString
                If (temp.Substring(temp.Length - 1, 1).Equals("\")) Then
                Else
                    temp = temp & "\"
                End If

                If (dict.ContainsKey(fi.Name.ToString)) Then
                    'dict(fi.Name.ToString).Add(temp, fi.Length.ToString)
                    dict(fi.Name.ToString).Add(temp, fi)
                Else
                    Dim tempdict As New Dictionary(Of String, FileInfo)
                    'tempdict.Add(temp, fi.Length.ToString)
                    tempdict.Add(temp, fi)
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
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({di.Name.ToString & " found!", "Skipped!"}, ",")))
                    'PopulateListView({di.Name.ToString & " found!", "Skipped!"})
                    Continue For
                End If
                Size += GatherDirToDict(di, dict, errorBit)
            Next

        Catch ex As Exception
            'PopulateListView({ex.Message, "Error GatherDirToDict"})
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message, "Error GatherDirToDict"}, ",")))
            errorBit = errorBit + 1    'error count
        End Try
        Return Size
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
        'Dim tempRegex As New Regex("(" & adjNewRootDir & ")")
        Dim tempRegex As New Regex(adjNewRootDir)

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

    Private Function GetFullDestFilePath(ByVal originalFullFilePath As String, ByVal originalRootDir As String, ByVal newRootDir As String) As String
        'if newRootDir is from a drive in the original dir, newRootDir will be missing ":\"
        'originalSubDir can be drive dir, eg: X:\

        Dim newFullDestFilePath As String = String.Empty

        'make sure all dir inputs end with "\"
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
        newFullDestFilePath = tempRegex.Replace(originalFullFilePath, newRootDir)
        'MessageBox.Show(newFullDestDir)
        'PopulateListView({newFullDestDir, "GetFullDestDirPath"})
        'Dim m As Match = tempRegex.Match(originalSubDir)
        'If (m.Success) Then
        'MessageBox.Show(m.Groups(1).ToString)
        ' End If

        Return newFullDestFilePath
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

    Private Sub AddFileinfoToDict(ByRef dict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, FileInfo))), ByVal key1 As String, ByVal key2 As String, ByVal key3 As String, _
                          ByVal value As FileInfo)
        'x -> y -> z -> a only
        If (dict.ContainsKey(key1)) Then
            If (dict(key1).ContainsKey(key2)) Then
                dict(key1)(key2).Add(key3, value)
            Else
                Dim temp1 As New Dictionary(Of String, FileInfo)
                temp1.Add(key3, value)
                dict(key1).Add(key2, temp1)
            End If
        Else
            Dim temp2 As New Dictionary(Of String, FileInfo)
            temp2.Add(key3, value)
            Dim temp1 As New Dictionary(Of String, Dictionary(Of String, FileInfo))
            temp1.Add(key2, temp2)
            dict.Add(key1, temp1)
        End If
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

    Private Function AddBackslash(ByVal dir As String) As String
        If (Not dir.Substring(dir.Length - 1, 1).Equals("\")) Then
            dir = dir & "\"
        End If
        Return dir
    End Function

    Private Function AddSlashToRegexLiterals(ByVal dir As String) As String
        'Dim literals As String() = {"\+", "\.", "\{", "\}", "\\", "\[", "\]", "\(", "\)"}
        ' For Each literal In literals
        'Dim tempRegex As New Regex(literal)
        ' dir = tempRegex.Replace(dir, literal)
        'Next
        ' Return dir

        Dim strFinal As String = String.Empty
        For nI As Integer = 0 To dir.Length - 1
            Dim currentChar As String = dir.Substring(nI, 1)
            If (currentChar.Equals("!") Or currentChar.Equals("@") Or currentChar.Equals("#") Or currentChar.Equals("$") Or currentChar.Equals("%") Or _
               currentChar.Equals("^") Or currentChar.Equals("&") Or currentChar.Equals("(") Or currentChar.Equals(")") Or currentChar.Equals("-") Or _
               currentChar.Equals("+") Or currentChar.Equals("~") Or currentChar.Equals("+") Or currentChar.Equals("[") Or _
               currentChar.Equals("]") Or currentChar.Equals("{") Or currentChar.Equals("}") Or currentChar.Equals(".") Or currentChar.Equals(",") Or _
               currentChar.Equals(";") Or currentChar.Equals("'") Or currentChar.Equals("\")) Then
                currentChar = "\" & currentChar
            End If

            strFinal = strFinal & currentChar
        Next
        'MessageBox.Show(strFinal)
        Return strFinal
    End Function

    Private Sub DeleteFileFromDestination2(ByRef FINALtransferList As ArrayList, ByRef FINALdeleteList As ArrayList, ByRef LogList As ArrayList)
        'go over FINALtransferList first
        Dim filecount As Integer = 0
        ' For Each x As CoExistFileCondition In FINALtransferList
        For nI As Integer = 0 To FINALtransferList.Count - 1
            Dim x As CoExistFileCondition = CType(FINALtransferList(nI), CoExistFileCondition)
            Try
                If (x.bDestinationFileNeedDelete) Then
                    My.Computer.FileSystem.DeleteFile(x.DestinationFullFilePath)
                    ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Deleting " & x.DestinationFullFilePath))
                    'PopulateListView({"Deleting " & tempPath, "Deleted"})
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleting " & x.DestinationFullFilePath, "Deleted"}, ",")))
                    LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, x.DestinationFullFilePath, "Deleted"}, vbTab))
                    x.bDestinationFileDeletable = True
                    x.bCopyReady = True
                End If
            Catch ex As Exception
                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({x.DestinationFullFilePath & " not deletable!", ex.Message}, ",")))
                LogList.Add(Join({"Warning", System.DateTime.Now.ToString, x.DestinationFullFilePath, ex.Message}, vbTab))
                x.bDestinationFileDeletable = False
                x.bCopyReady = False
            End Try
            FINALtransferList(nI) = x
            filecount = filecount + 1
            ThreadExtensions.ScSend(context, New Action(Of UpdateToolProgressEventArgs)(AddressOf OnUpdateToolBarProgress), New UpdateToolProgressEventArgs(CDbl(filecount / (FINALtransferList.Count + FINALdeleteList.Count))))
        Next

        'then go over the FINALdeleteList
        'For Each x As CoExistFileCondition In FINALdeleteList
        For nI As Integer = 0 To FINALdeleteList.Count - 1
            Dim x As CoExistFileCondition = CType(FINALdeleteList(nI), CoExistFileCondition)
            Try
                My.Computer.FileSystem.DeleteFile(x.DestinationFullFilePath)
                ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Deleting " & x.DestinationFullFilePath))
                'PopulateListView({"Deleting " & tempPath, "Deleted"})
                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleting " & x.DestinationFullFilePath, "Deleted"}, ",")))
                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, x.DestinationFullFilePath, "Deleted"}, vbTab))
                x.bDestinationFileDeletable = True
            Catch ex As Exception
                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({x.DestinationFullFilePath & " not deletable!", ex.Message}, ",")))
                LogList.Add(Join({"Warning", System.DateTime.Now.ToString, x.DestinationFullFilePath, ex.Message}, vbTab))
                x.bDestinationFileDeletable = False
                x.bCopyReady = False
            End Try
            FINALdeleteList(nI) = x
            filecount = filecount + 1
            ThreadExtensions.ScSend(context, New Action(Of UpdateToolProgressEventArgs)(AddressOf OnUpdateToolBarProgress), New UpdateToolProgressEventArgs(CDbl(filecount / (FINALtransferList.Count + FINALdeleteList.Count))))
        Next
        ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Ready"))
        'MessageBox.Show("done delete")
    End Sub

    Private Sub DeleteFileFromDestination(ByRef removeFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, FileInfo))), _
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
                                If (File.Exists(tempPath)) Then
                                    My.Computer.FileSystem.DeleteFile(f.FullName)
                                    'ToolStripStatusLabel1.Text = "Deleting " & f.FullName
                                    ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Deleting " & f.FullName))
                                    'PopulateListView({"Deleting " & tempPath, "Deleted"})
                                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleting " & tempPath, "Deleted"}, ",")))
                                    LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, tempPath, "Deleted"}, vbTab))
                                Else
                                    'PopulateListView({"Deleting " & tempPath, "Not Found!"})
                                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleting " & tempPath, "Not Found!"}, ",")))
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
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Deleting " & DestDir, "Deleted"}, ",")))
                    'PopulateListView({"Deleting " & DestDir, "Deleted"})
                    LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, DestDir, "Deleted"}, vbTab))
                End If

            Catch ex As Exception
                bSuccess = False
                'PopulateListView({ex.Message, "Error DeleteFileFromDestination"})
                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message, "Error DeleteFileFromDestination"}, ",")))
                LogList.Add(Join({"Failed", System.DateTime.Now.ToString, tempPath, ex.Message}, vbTab))
            End Try

        End If

        'ToolStripStatusLabel1.Text = "Ready"
        ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Ready"))

    End Sub

    Private Function BuildMoveFileDict(ByRef transferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef ToBeTransferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef removeFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef moveFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), _
                                        ByRef totalMoveFileSize As Int64) As Boolean

        Try
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
                                'PopulateListView({(moveFileDict("dummy")(filename)(oriDestDir)), filename})
                                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({(moveFileDict("dummy")(filename)(oriDestDir)), filename}, ",")))
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

        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message, "BuildMoveFileDictError"}, ",")))
            Return False
        End Try

    End Function

    Private Sub UpdateLog(ByRef list As ArrayList)
        Dim bFileExist As Boolean = File.Exists("AutoBackup_Log.txt")
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

    Private Sub TransferFileToDestination2(ByRef FINALtransferList As ArrayList, ByRef Loglist As ArrayList, ByVal nAttempts As Integer, ByVal nWaitBetweenAttempt As Integer)   'disable ShowUI permanantly and add copy attempts
        'transfer the file after deleting file according to FINALtransferList
        Dim filecount As Integer = 0

        For Each x As CoExistFileCondition In FINALtransferList
            If (x.bCopyReady) Then
                For nI As Integer = 1 To nAttempts
                    Try
                        ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Transferring " & x.OriginalFullFilePath))
                        'If (bShowTransferUI) Then
                        '    My.Computer.FileSystem.CopyFile(x.OriginalFullFilePath, x.DestinationFullFilePath, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                        'Else
                        My.Computer.FileSystem.CopyFile(x.OriginalFullFilePath, x.DestinationFullFilePath)
                        'End If

                        ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Copying to " & x.DestinationFullFilePath, "Copied"}, ",")))
                        Loglist.Add(Join({"Succeed", System.DateTime.Now.ToString, x.DestinationFullFilePath, "Copied"}, vbTab))

                        Exit For
                    Catch ex As Exception
                        If (nI = nAttempts) Then
                            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"ERROR COPYING: " & x.OriginalFullFilePath, ex.Message}, ",")))
                            Loglist.Add(Join({"Failed", System.DateTime.Now.ToString, x.OriginalFullFilePath, ex.Message}, vbTab))
                        Else
                            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Attempt " & nI.ToString & ": " & x.OriginalFullFilePath, ex.Message}, ",")))
                            System.Threading.Thread.Sleep(nWaitBetweenAttempt)
                        End If

                    End Try
                Next

            End If

            filecount = filecount + 1
            ThreadExtensions.ScSend(context, New Action(Of UpdateToolProgressEventArgs)(AddressOf OnUpdateToolBarProgress), New UpdateToolProgressEventArgs(CDbl(filecount / FINALtransferList.Count)))
        Next

        ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Ready"))
    End Sub

    Private Function TransferFileToDestination(ByRef transferFileDict As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))), ByVal bMove As Boolean, ByRef LogList As ArrayList) As Boolean
        'transferFileDict: transfer directory -> file name -> original dir -> destination dir
        Dim bSuccess As Boolean = True
        'bTransferSucceed = True
        Dim filecount As Integer = 0
        For Each transDir As String In transferFileDict.Keys
            For Each fileName As String In transferFileDict(transDir).Keys
                For Each oriDir As String In transferFileDict(transDir)(fileName).Keys
                    Try
                        If (File.Exists(Path.Combine({oriDir, fileName}))) Then
                            'ToolStripStatusLabel1.Text = "Transferring " & Path.Combine({oriDir, fileName})
                            ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Transferring " & Path.Combine({oriDir, fileName})))
                            If (bMove) Then
                                My.Computer.FileSystem.MoveFile(Path.Combine({oriDir, fileName}), Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), True)
                                'My.Computer.FileSystem.DeleteFile(Path.Combine({oriDir, fileName}))
                                Dim ori As New DirectoryInfo(oriDir)
                                If (ori.GetDirectories.Length = 0 And ori.GetFiles.Length = 0) Then
                                    My.Computer.FileSystem.DeleteDirectory(oriDir, FileIO.DeleteDirectoryOption.ThrowIfDirectoryNonEmpty)
                                End If
                                'PopulateListView({"Moving " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"})
                                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Moving " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"}, ",")))
                                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Moved"}, vbTab))
                            Else
                                My.Computer.FileSystem.CopyFile(Path.Combine({oriDir, fileName}), Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, FileIO.UICancelOption.ThrowException)
                                'PopulateListView({"Copying " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"})
                                ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Copying " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"}, ",")))
                                LogList.Add(Join({"Succeed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Copied"}, vbTab))
                            End If
                        Else 'file not found in the original dir
                            'PopulateListView({"Transferring " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"})
                            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({"Transferring " & Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"}, ",")))
                            LogList.Add(Join({"Warning", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), "Not Found"}, vbTab))
                        End If

                    Catch ex As Exception
                        bSuccess = False
                        'bTransferSucceed = False
                        'PopulateListView({ex.Message, "Error TransferFileToDestination"})
                        ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message, "Error TransferFileToDestination"}, ",")))
                        LogList.Add(Join({"Failed", System.DateTime.Now.ToString, Path.Combine({transferFileDict(transDir)(fileName)(oriDir), fileName}), ex.Message}, vbTab))
                    End Try
                    filecount = filecount + 1
                    ThreadExtensions.ScSend(context, New Action(Of UpdateToolProgressEventArgs)(AddressOf OnUpdateToolBarProgress), New UpdateToolProgressEventArgs(CDbl(filecount / (transferFileDict.Keys.Count * transferFileDict(transDir).Keys.Count))))
                Next
            Next
        Next

        'ToolStripStatusLabel1.Text = "Ready"
        ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Ready"))
        'bTransferDone = True
        Return bSuccess
    End Function


    Private Function TurnOnBothDrive(ByVal originalDir As String, ByVal destDir As String) As Boolean
        Try
            Dim sw1 As New StreamWriter(Path.Combine(originalDir, "spinup.tmp"))
            Dim sw2 As New StreamWriter(Path.Combine(destDir, "spinup.tmp"))

            sw1.WriteLine("Spinning Up Drive yeah")
            sw2.WriteLine("Spinning Up Drive yeah")

            sw1.Close()
            sw2.Close()

            ThreadExtensions.ScSend(context, New Action(Of UpdateToolStripStatusEventArgs)(AddressOf OnUpdateToolStripStatus), New UpdateToolStripStatusEventArgs("Spinning Up " & originalDir & "," & destDir))
            System.Threading.Thread.Sleep(60000)

            File.Delete(Path.Combine(originalDir, "spinup.tmp"))
            File.Delete(Path.Combine(destDir, "spinup.tmp"))
        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs(Join({ex.Message, "Spin Drive Error"}, ",")))
            Return False
        End Try
        Return True
    End Function


    Private Sub GetAllFilesInDirectory(ByRef targetDir As String, ByRef fileArrayList As ArrayList, ByRef folderCount As Integer, ByRef fileCount As Integer)
        If (Directory.Exists(targetDir)) Then
            Dim di As New DirectoryInfo(targetDir)
            Dim dis As DirectoryInfo() = di.GetDirectories
            Dim fis As FileInfo() = di.GetFiles()

            For Each fi As FileInfo In fis
                'PopulateListView({fi.FullName, ""})
                If (fi.Name.Equals("Thumbs.db")) Then
                Else
                    fileArrayList.Add(fi.FullName)
                    fileCount = fileCount + 1
                End If
            Next

            For Each folder As DirectoryInfo In dis
                If (folder.Name.Equals("$RECYCLE.BIN") Or folder.Name.Equals("System Volume Information")) Then
                    Continue For
                Else
                    GetAllFilesInDirectory(folder.FullName, fileArrayList, folderCount, fileCount)
                    folderCount = folderCount + 1
                End If
            Next
        Else
            'MessageBox.Show(targetDir & " not existed")
        End If
    End Sub

    'Real hash generation happen here
    Private Function FileCheckSumMatch(ByVal originalFile As String, ByVal destFile As String) As Boolean
        If (GenerateHash(originalFile).Equals(GenerateHash(destFile))) Then
            Return True
        End If
        Return False
    End Function


    Private Function GenerateHash(ByVal runningCommand As String) As String
        Dim FinalHash As String = GetRandom(1, 1000000).ToString
        ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Generating Hash: " & runningCommand))
        Try
            Dim myMD5 As MD5 = MD5.Create
            Dim fInfo As New FileInfo(runningCommand)
            Dim fStream As FileStream = fInfo.Open(FileMode.Open)
            fStream.Position = 0
            Dim hashValue() As Byte = myMD5.ComputeHash(fStream)
            ByteArrayToString(hashValue, FinalHash)
            fStream.Close()
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Success: " & FinalHash))
        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Failure: " & ex.Message))
        End Try

        Return FinalHash
    End Function

    Private Function GetRandom(ByVal Min As Integer, ByVal Max As Integer) As Integer
        ' by making Generator static, we preserve the same instance '
        ' (i.e., do not create new instances with the same seed over and over) '
        ' between calls '
        Static Generator As System.Random = New System.Random()
        Return Generator.Next(Min, Max)
    End Function

    Private Shared Sub ByteArrayToString(ByVal array() As Byte, ByRef output As String)
        Dim i As Integer
        output = String.Empty
        For i = 0 To array.Length - 1
            output = output & String.Format("{0:X2}", array(i))
            'If i Mod 4 = 3 Then
            'Console.Write(" ")
            ' End If
        Next i
        ' Console.WriteLine()

    End Sub
End Class

Public Class UpdatelbDestDirTextAndColorEventArgs
    Inherits EventArgs
    Private _message As String
    Private _fontcolor As Color

    Public Sub New(ByVal msg As String, ByVal fontcolor As Color)
        _message = msg
        _fontcolor = fontcolor
    End Sub
    Public ReadOnly Property UpdatedDir As String
        Get
            Return _message
        End Get
    End Property
    Public ReadOnly Property UpdatedFontColor As Color
        Get
            Return _fontcolor
        End Get
    End Property
End Class

Public Class UpdateListStatusEventArgs
    Inherits EventArgs
    Private _message As String()

    Public Sub New(ByVal msg As String)
        _message = Split(msg, ",")
    End Sub
    Public ReadOnly Property Message As String()
        Get
            Return _message
        End Get
    End Property
End Class

Public Class TerminateCopyEventArgs
    Inherits EventArgs
    Private bStop As Boolean

    Public Sub New(ByVal temp As Boolean)
        bStop = temp
    End Sub
    Public ReadOnly Property StopCopy As Boolean
        Get
            Return bStop
        End Get
    End Property
End Class

Public Class UpdateToolStripStatusEventArgs
    Inherits EventArgs
    Private _message As String

    Public Sub New(ByVal msg As String)
        _message = msg
    End Sub
    Public ReadOnly Property Message As String
        Get
            Return _message
        End Get
    End Property
End Class

Public Class RemoveItemFromListOriDir
    Inherits EventArgs
    Private _removePos As Integer

    Public Sub New(ByVal removePos As Integer)
        _removePos = removePos
    End Sub
    Public ReadOnly Property PosToRemove As Integer
        Get
            Return _removePos
        End Get
    End Property
End Class

Public Class UpdateProgressEventArgs
    Inherits EventArgs
    Private _currentCount As Double
    Public Sub New(ByVal currentCount As Double)
        _currentCount = currentCount
    End Sub
    Public ReadOnly Property ProgressMultiplier As Double
        Get
            Return _currentCount
        End Get
    End Property
End Class

Public Class UpdateToolProgressEventArgs
    Inherits EventArgs
    Private _currentCount As Double
    Public Sub New(ByVal currentCount As Double)
        _currentCount = currentCount
    End Sub
    Public ReadOnly Property ProgressMultiplier As Double
        Get
            Return _currentCount
        End Get
    End Property
End Class

Public Class UpdateMovedStatusEventArgs
    Inherits EventArgs
    Private _message As String
    Private _fontcolor As Color = Color.Blue
    Private _currentRowIndex As Integer

    Public Sub New(ByVal msg As String, ByVal rowIndex As Integer)
        _message = msg
        _currentRowIndex = rowIndex
        If (_message.Equals("Moving")) Then
            _fontcolor = Color.Orange
        ElseIf (_message.Equals("Ready To Move")) Then
            _fontcolor = Color.Green
        ElseIf (_message.Equals("Moved")) Then
            _fontcolor = Color.Blue
        ElseIf (_message.Equals("Error")) Then
            _fontcolor = Color.Red
        End If
    End Sub
    Public ReadOnly Property Message() As String
        Get
            Return _message
        End Get
    End Property
    Public ReadOnly Property FontColor() As Color
        Get
            Return _fontcolor
        End Get
    End Property
    Public ReadOnly Property CurrentRowIndex() As Integer
        Get
            Return _currentRowIndex
        End Get
    End Property
End Class

Public Class ThreadExtensions
    Private args() As Object
    Private DelegateToInvoke As [Delegate]

    Public Shared Function QueueUserWorkItem(ByVal method As [Delegate], ByVal ParamArray args() As Object) As Boolean
        Return Threading.ThreadPool.QueueUserWorkItem(AddressOf ProperDelegate, New ThreadExtensions With {.args = args, .DelegateToInvoke = method})
    End Function

    Public Shared Sub ScSend(ByVal sc As Threading.SynchronizationContext, ByVal del As [Delegate], ByVal ParamArray args() As Object)
        sc.Send(New Threading.SendOrPostCallback(AddressOf ProperDelegate), New ThreadExtensions With {.args = args, .DelegateToInvoke = del})
    End Sub

    Private Shared Sub ProperDelegate(ByVal state As Object)
        Dim sd As ThreadExtensions = DirectCast(state, ThreadExtensions)

        sd.DelegateToInvoke.DynamicInvoke(sd.args)
    End Sub
End Class


