
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Net
Imports System.Text

Public Class MultithreadListerner
    'variables
    Private serverIP As String = String.Empty
    Private serverPort As String = String.Empty
    Private bConnected As Boolean = False
    Private serverSocket As TcpListener
    Private ConnectionCount As Integer = 0
    'Public ConnectionArray() As ServerDoChat
    Public ConnectionDict As New Dictionary(Of String, ServerDoChat)
    'Private ClientSocketList As New ArrayList
    Private ClientSocketList As New Dictionary(Of String, Socket) 'ip->socket
    Private clientSocket As Socket
    Private bContinueListening As Boolean = True
    Private bServerStartedAlready As Boolean = False
    Private SleepingHours As Integer = 0 'hrs

    'Property Function
    Public ReadOnly Property IsConnect As Boolean
        Get
            Return bConnected
        End Get
    End Property
    Public WriteOnly Property SetIP As String
        Set(ByVal value As String)
            serverIP = value
        End Set
    End Property
    Public WriteOnly Property SetSleepHrs As Integer
        Set(ByVal value As Integer)
            SleepingHours = value
        End Set
    End Property


    'constructor
    Public Sub New(ByVal IPtarget1 As String, ByVal PortTarget1 As String, ByVal SleepHours As Integer)
        serverIP = IPtarget1
        serverPort = PortTarget1
        SleepingHours = SleepHours
    End Sub

    'multithread synchronization
    Private context As Threading.SynchronizationContext = Threading.SynchronizationContext.Current

    Public Event UpdateListBoxStatus As EventHandler(Of UpdateListBoxEventArgs)
    Protected Overridable Sub OnUpdateListBoxStatus(ByVal e As UpdateListBoxEventArgs)
        RaiseEvent UpdateListBoxStatus(Me, e)
    End Sub

    Public Event AddChatHandler As EventHandler(Of AddChatHandlerEventArgs)
    Protected Overridable Sub OnAddChatHandler(ByVal e As AddChatHandlerEventArgs)
        RaiseEvent AddChatHandler(Me, e)
    End Sub

    'Multithread function
    Public Sub ServerListenerAsync()
        ThreadExtensions.QueueUserWorkItem(New Action(Of String, String)(AddressOf ServerListener), serverIP, serverPort)
    End Sub

    Public Sub StopListenerAsync(ByVal disconnectALLClient As Boolean)
        ThreadExtensions.QueueUserWorkItem(New Action(Of Boolean)(AddressOf StopListener), disconnectALLClient)
    End Sub

    Public Sub WakeSleepingComputersAsync()
        ThreadExtensions.QueueUserWorkItem(New Action(Of String)(AddressOf WakeSleepingComputers), "haha")
    End Sub
    

    'function
    Private Sub AddToClientSocketList(ByRef tempDict As Dictionary(Of String, Socket), ByVal NewKey As String, ByVal NewValue As Socket)
        'check if key exists
        If (tempDict.ContainsKey(NewKey)) Then
            tempDict.Remove(NewKey)
        End If

        'Add the new socket
        tempDict.Add(NewKey, NewValue)
    End Sub

    Private Sub AddToConnectionDict(ByRef tempDict As Dictionary(Of String, ServerDoChat), ByVal NewKey As String, ByVal NewValue As ServerDoChat)
        'check if key exists
        If (tempDict.ContainsKey(NewKey)) Then
            tempDict(NewKey).stopWakeUpTimer(NewKey)   'stop the wakeuptimer and remove handler
            tempDict.Remove(NewKey)
        End If

        'Add the new socket
        tempDict.Add(NewKey, NewValue)
    End Sub

    Private Function GetClientIP(ByRef clientSocket1 As Socket) As String
        Dim temp As String() = Split(clientSocket1.RemoteEndPoint.ToString, ":")
        Return temp(0).ToString
    End Function

    Private Sub ServerListener(ByVal destIP As String, ByVal destPort As String)
        If (bServerStartedAlready) Then
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Server Started already!"))
        Else
            serverSocket = New TcpListener(IPAddress.Parse(destIP), CInt(destPort))
            serverSocket.Start()
            bServerStartedAlready = True
            bContinueListening = True
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Server Started!"))

            'After Server Started, Wakeup computer that was sleeping 
            Me.WakeSleepingComputersAsync()

            While (bContinueListening)
                Try
                    clientSocket = serverSocket.AcceptSocket
                    Dim currentClientIP As String = GetClientIP(clientSocket)
                    AddToClientSocketList(ClientSocketList, currentClientIP, clientSocket)
                    ConnectionCount = ConnectionCount + 1
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Accept connection from client " & ConnectionCount.ToString & " IP: " & ClientSocketList(currentClientIP).RemoteEndPoint.ToString))
                    Dim tempConnection As New ServerDoChat(ClientSocketList(currentClientIP), ConnectionCount, context, SleepingHours)
                    AddToConnectionDict(ConnectionDict, currentClientIP, tempConnection)
                    ThreadExtensions.ScSend(context, New Action(Of AddchatHandlerEventArgs)(AddressOf OnAddChatHandler), New AddchatHandlerEventArgs(ConnectionDict(currentClientIP)))
                    ConnectionDict(currentClientIP).DoChatAsync()
                Catch ex As Exception
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
                End Try
            End While
        End If
    End Sub

    Private Sub StopListener(ByVal disconnectAllClient As Boolean)
        bContinueListening = False
        bServerStartedAlready = False
        Try
            If (disconnectAllClient) Then
                For Each ClientIP As String In ClientSocketList.Keys
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Disconnect " & ClientSocketList(ClientIP).RemoteEndPoint.ToString))
                    ClientSocketList(ClientIP).Close()
                    If (ConnectionDict.ContainsKey(ClientIP)) Then
                        ConnectionDict(ClientIP).stopWakeUpTimer(ClientIP)
                    End If
                Next
                ClientSocketList.Clear()
                ConnectionDict.Clear()
            End If
            serverSocket.Stop()
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Stop Server"))
        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
        End Try

    End Sub

    Private Sub WakeSleepingComputers(ByVal placeHolder As String)
        Try
            Dim bInSleepingSection As Boolean = False
            Dim CompADDDict As New Dictionary(Of String, String)
            Dim sectionRegex As New Regex("^(.+):")
            Dim sr As New System.IO.StreamReader("AutoBackup_Setting.cfg", System.Text.Encoding.Unicode)
            Do Until sr.Peek = -1
                Dim currentline As String = sr.ReadLine
                Dim m1 As Match = sectionRegex.Match(currentline)
                If (m1.Success) Then
                    If (m1.Groups(1).ToString.Equals("Sleeping Computers")) Then
                        bInSleepingSection = True
                    Else
                        bInSleepingSection = False
                    End If
                Else
                    If (bInSleepingSection) Then
                        Dim temp() As String = Split(currentline, "|")
                        If (CompADDDict.ContainsKey(temp(0))) Then
                            CompADDDict(temp(0)) = temp(1)
                        Else
                            CompADDDict.Add(temp(0), temp(1))
                        End If
                    End If
                End If
            Loop
            sr.Close()

            'Wake up all sleeping computers
            For Each CompIP As String In CompADDDict.Keys
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(CompIP))
                SendWOLPacket(CompIP, CompADDDict(CompIP))
            Next

        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Error Waking Computers!"))
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
        End Try
        
    End Sub

    Private Sub SendWOLPacket(ByVal targetIP As String, ByVal targetMAC As String)
        Dim udpClient As New UdpClient
        Dim buf(101) As Char
        Dim sendBytes As [Byte]() = Encoding.ASCII.GetBytes(buf)
        For x As Integer = 0 To 5
            sendBytes(x) = CInt("&HFF")
        Next

        Dim MacAddress As String
        MacAddress = Replace(targetMAC, "-", "")

        Dim i As Integer = 6
        For x As Integer = 1 To 16
            sendBytes(i) = CInt("&H" + MacAddress.Substring(0, 2))
            sendBytes(i + 1) = CInt("&H" + MacAddress.Substring(2, 2))
            sendBytes(i + 2) = CInt("&H" + MacAddress.Substring(4, 2))
            sendBytes(i + 3) = CInt("&H" + MacAddress.Substring(6, 2))
            sendBytes(i + 4) = CInt("&H" + MacAddress.Substring(8, 2))
            sendBytes(i + 5) = CInt("&H" + MacAddress.Substring(10, 2))
            i += 6
        Next

        Dim myAddress As String

        '' Split user IP address
        Dim myIpArray() As String
        Dim a, b, c, d As Int64
        myIpArray = targetIP.Split(".")
        For i = 0 To myIpArray.GetUpperBound(0)
            Select Case i
                Case Is = 0
                    a = Convert.ToInt64(myIpArray(i))
                Case Is = 1
                    b = Convert.ToInt64(myIpArray(i))
                Case Is = 2
                    c = Convert.ToInt64(myIpArray(i))
                Case Is = 3
                    d = Convert.ToInt64(myIpArray(i))
            End Select
        Next

        Dim mySubnetArray() As String
        Dim sm1, sm2, sm3, sm4 As Int64
        mySubnetArray = "255.255.255.0".Split(".")
        For i = 0 To mySubnetArray.GetUpperBound(0)
            Select Case i
                Case Is = 0
                    sm1 = Convert.ToInt64(mySubnetArray(i))
                Case Is = 1
                    sm2 = Convert.ToInt64(mySubnetArray(i))
                Case Is = 2
                    sm3 = Convert.ToInt64(mySubnetArray(i))
                Case Is = 3
                    sm4 = Convert.ToInt64(mySubnetArray(i))
            End Select
        Next
        myAddress = ToInteger(OrIt(ToBinary(a), InvertBinary(ToBinary(sm1)))) & "." & ToInteger(OrIt(ToBinary(b), InvertBinary(ToBinary(sm2)))) & _
    "." & ToInteger(OrIt(ToBinary(c), InvertBinary(ToBinary(sm3)))) & "." & ToInteger(OrIt(ToBinary(d), InvertBinary(ToBinary(sm4))))


        udpClient.Send(sendBytes, sendBytes.Length, myAddress, CInt("7"))
        ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(System.DateTime.Now.ToString & "--Magic Packet Sent to " & targetIP))
    End Sub

    Private Function InvertBinary(ByVal x As String) As String
        Dim ch As Char
        Dim len As Integer = CStr(x).Length
        For Each ch In CStr(x)
            If ch = "1" Then
                InvertBinary += "0"
            Else
                InvertBinary += "1"
            End If
        Next
    End Function

    Private Function OrIt(ByVal x As Long, ByVal y As Long) As String
        'Pad out
        Dim xx As String
        xx = CStr(x)
        While xx.Length < 8
            xx = "0" + xx
        End While

        Dim yy As String
        yy = CStr(y)
        While yy.Length < 8
            yy = "0" + yy
        End While
        For c As Integer = 0 To 7
            If xx.Chars(c) = "1" Or yy.Chars(c) = "1" Then
                OrIt += "1"
            Else
                OrIt += "0"
            End If
        Next
    End Function

    Private Function ToBinary(ByVal x As Long) As String
        Dim temp As String = ""
        Do
            If x Mod 2 Then
                temp = "1" + temp
            Else
                temp = "0" + temp
            End If
            x = x \ 2
            If x < 1 Then Exit Do
        Loop

        While temp.Length < 8
            temp = "0" + temp
        End While

        Return temp
    End Function

    Private Function ToInteger(ByVal x As Long) As String
        Dim temp As String
        Dim ch As Char
        Dim multiply As Integer = 1
        Dim subtract As Integer = 1
        Dim len As Integer = CStr(x).Length
        For Each ch In CStr(x)
            For len = 1 To CStr(x).Length - subtract
                multiply = multiply * 2
            Next
            multiply = CInt(ch.ToString) * multiply
            temp = multiply + temp
            subtract = subtract + 1
            multiply = 1
        Next
        Return temp
    End Function
End Class

Public Class UpdateListBoxEventArgs
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

Public Class AddchatHandlerEventArgs
    Inherits EventArgs
    'Private _message As String()
    Private chatID As ServerDoChat

    Public Sub New(ByVal chatID1 As ServerDoChat)
        '_message = Split(msg, ",")
        chatID = chatID1
    End Sub
    Public ReadOnly Property ReturnChatID As ServerDoChat
        Get
            Return chatID
        End Get
    End Property
End Class




