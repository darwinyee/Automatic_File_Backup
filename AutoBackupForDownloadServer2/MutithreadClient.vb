Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Timers

Public Class MutithreadClient
    'variables
    Private serverIP As String = String.Empty
    Private serverPort As String = String.Empty
    Private bConnected As Boolean = False
    Private tcpClient As TcpClient 'varible for new client
    Private ClientSocket As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    Private MaxAttemptCount As Integer = 10  'Max number of connection attempt
    Private CheckActiveConnectionTimer As New Timer(10000)
    Private ClientMAC As String = String.Empty
    Private ClientIP As String = String.Empty
    Private ClientStatus As String = "Wake Up"


    'Property Function
    Public ReadOnly Property IsConnect As Boolean
        Get
            Return bConnected
        End Get
    End Property

    Public WriteOnly Property NewTCP As TcpClient
        Set(ByVal value As TcpClient)
            tcpClient = value
        End Set
    End Property

    Public WriteOnly Property NewSocket As Socket
        Set(ByVal value As Socket)
            ClientSocket = value
        End Set
    End Property

    Public WriteOnly Property MACadd As String
        Set(ByVal value As String)
            ClientMAC = value
        End Set
    End Property

    Public Property IPadd As String
        Get
            Return ClientIP
        End Get
        Set(ByVal value As String)
            ClientIP = value
        End Set
    End Property

    Public WriteOnly Property MaxConnectionAttempt As Integer
        Set(ByVal value As Integer)
            MaxAttemptCount = value
        End Set
    End Property

    Public WriteOnly Property CurrentClientStatus As String
        Set(ByVal value As String)
            ClientStatus = value
        End Set
    End Property

    'multithread synchronization
    Private context As Threading.SynchronizationContext = Threading.SynchronizationContext.Current

    Public Event UpdateListBoxStatus As EventHandler(Of UpdateListBoxEventArgs)
    Protected Overridable Sub OnUpdateListBoxStatus(ByVal e As UpdateListBoxEventArgs)
        RaiseEvent UpdateListBoxStatus(Me, e)
    End Sub

    Public Event UpdateTCPclient As EventHandler(Of UpdateTCPclient)
    Protected Overridable Sub OnUpdateTCPclient(ByVal e As UpdateTCPclient)
        RaiseEvent UpdateTCPclient(Me, e)
    End Sub

    Public Event UpdateClientSocket As EventHandler(Of UpdateClientSocket)
    Protected Overridable Sub OnUpdateClientSocket(ByVal e As UpdateClientSocket)
        RaiseEvent UpdateClientSocket(Me, e)
    End Sub

    Public Event reConnectToServer As EventHandler(Of ConnectToServer)
    Protected Overridable Sub OnReConnectToServer(ByVal e As ConnectToServer)
        RaiseEvent reConnectToServer(Me, e)
    End Sub

    Public Event StopAsClient As EventHandler(Of QuitAsClient)
    Protected Overridable Sub OnStopAsClient(ByVal e As QuitAsClient)
        RaiseEvent StopAsClient(Me, e)
    End Sub

    Public Event StartTransfer As EventHandler(Of StartTransferEvent)
    Protected Overridable Sub OnStartTransfer(ByVal e As StartTransferEvent)
        RaiseEvent StartTransfer(Me, e)
    End Sub

    Public Event ShutdownComputer As EventHandler(Of ShutdownEvent)
    Protected Overridable Sub OnShutdownComputer(ByVal e As ShutdownEvent)
        RaiseEvent ShutdownComputer(Me, e)
    End Sub

    'Multithread function
    Public Sub ClientConnectAsync(ByVal destIP As String, ByVal destPort As String)
        serverIP = destIP
        serverPort = destPort
        ThreadExtensions.QueueUserWorkItem(New Action(Of String, String)(AddressOf ClientConnect), destIP, destPort)
    End Sub

    Public Sub SendMessageAsync(ByVal message As String)
        ThreadExtensions.QueueUserWorkItem(New Action(Of String)(AddressOf SendMessage), message)
    End Sub

    Public Sub ClientConnectSocketAsync(ByVal destIP As String, ByVal destPort As String, ByVal MaximumConnectRetry As Integer)
        serverIP = destIP
        serverPort = destPort
        ThreadExtensions.QueueUserWorkItem(New Action(Of String, String)(AddressOf ClientConnectSocket), destIP, destPort)
    End Sub

    Public Sub SendMessageSocketAsync(ByVal message As String)
        ThreadExtensions.QueueUserWorkItem(New Action(Of String)(AddressOf SendMessageSocket), message)
    End Sub

    'function
    Private Sub ClientConnectSocket(ByVal ipADD As String, ByVal portNum As String)
        'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("InClientConnectSocket"))
        If (Not CheckSocketConnectionStatus()) Then
            Dim newSocket As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            Dim attemptCount As Integer = 0
            While ((attemptCount <= MaxAttemptCount))
                Try
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Connecting to " & ipADD.ToString & ":" & portNum.ToString & "..."))
                    newSocket.Connect(ipADD, CInt(portNum)) 'connecting the client the server

                    'port is same as in the server
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Connected"))
                    bConnected = True
                    AddHandler CheckActiveConnectionTimer.Elapsed, AddressOf HandleTimer
                    'start the wakeUPtimer
                    CheckActiveConnectionTimer.Start()
                    ThreadExtensions.ScSend(context, New Action(Of UpdateClientSocket)(AddressOf OnUpdateClientSocket), New UpdateClientSocket(newSocket))
                    attemptCount = MaxAttemptCount + 1
                Catch ex As Exception
                    'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Error..." + ex.StackTrace.ToString()))
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
                End Try
                attemptCount = attemptCount + 1
                'MaxAttemptCount = attemptCount + 1   'unlimited trying to connect
            End While

            'reset being a client if still not connect
            If (Not bConnected) Then
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Failed to Connect"))
                ThreadExtensions.ScSend(context, New Action(Of QuitAsClient)(AddressOf OnStopAsClient), New QuitAsClient)
            End If

            'only successful connection will exit while loop
            SendMessageSocket("MAC-" & ClientMAC & " Client Status:" & ClientStatus)
        Else
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Already Connected"))
        End If
    End Sub

    Private Sub ClientConnect(ByVal ipADD As String, ByVal portNum As String)
        If (Not CheckConnectionStatus()) Then
            Dim newTCPclient As New TcpClient
            Dim attemptCount As Integer = 0
            While ((Not newTCPclient.Connected) Or (attemptCount <= MaxAttemptCount))
                Try
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Connecting to " & ipADD.ToString & ":" & portNum.ToString & "..."))
                    newTCPclient.Connect(ipADD, CInt(portNum)) 'connecting the client the server

                    'port is same as in the server
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Connected"))
                    bConnected = True
                    ThreadExtensions.ScSend(context, New Action(Of UpdateTCPclient)(AddressOf OnUpdateTCPclient), New UpdateTCPclient(newTCPclient))
                    attemptCount = MaxAttemptCount + 1
                Catch ex As Exception
                    'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Error..." + ex.StackTrace.ToString()))
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
                End Try
                attemptCount = attemptCount + 1
            End While
        Else
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Already Connected"))
        End If
    End Sub

    Private Sub SendMessageSocket(ByVal strMessage As String)
        If (CheckSocketConnectionStatus()) Then
            Try
                Dim ascenc As New ASCIIEncoding
                Dim byteData() As Byte = ascenc.GetBytes(strMessage) 'converting the data into bytes
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(byteData.Length.ToString))
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("To Server: " & strMessage))
                ClientSocket.Send(byteData, byteData.Length, SocketFlags.None) 'writing/transmitting the message
                Dim replymsg(1000) As Byte
                Dim size As Integer = ClientSocket.Receive(replymsg, replymsg.Length, SocketFlags.None) 'reading the reply message and getting its size
                Dim ServerReply As String = System.Text.Encoding.UTF8.GetString(replymsg)
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("From Server: " & ServerReply))

                'perform action according to server message
                Dim StartTransferRegex As New Regex("Start Transfer")
                'raise Start Transfer event
                Dim m1 As Match = StartTransferRegex.Match(ServerReply)
                If (m1.Success) Then
                    ThreadExtensions.ScSend(context, New Action(Of StartTransferEvent)(AddressOf OnStartTransfer), New StartTransferEvent())
                End If

                Dim ShutdownRegex As New Regex("Shut Down")
                'raise shutdown event
                Dim m2 As Match = ShutdownRegex.Match(ServerReply)
                If (m2.Success) Then
                    ThreadExtensions.ScSend(context, New Action(Of ShutdownEvent)(AddressOf OnShutdownComputer), New ShutdownEvent())
                End If

            Catch ex As Exception
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Error..." + ex.StackTrace.ToString()))
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
            End Try
        Else
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Not connected"))
        End If
    End Sub

    Private Sub SendMessage(ByVal strMessage As String)
        If (CheckConnectionStatus()) Then
            Try
                Dim stm As Stream = tcpClient.GetStream() 'getting the stream of the client
                Dim ascenc As New ASCIIEncoding
                Dim byteData() As Byte = ascenc.GetBytes(strMessage) 'converting the data into bytes
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Transmitting"))
                stm.Write(byteData, 0, byteData.Length()) 'writing/transmitting the message
                Dim replymsg(1000) As Byte
                Dim size As Integer = stm.Read(replymsg, 0, 1000) 'reading the reply message and getting its size
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Acknoledgement from Server"))
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(System.Text.Encoding.UTF8.GetString(replymsg)))
                'stm.Close()

            Catch ex As Exception
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Error..." + ex.StackTrace.ToString()))
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
            End Try
        Else
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Not connected"))
        End If
    End Sub

    Private Function CheckConnectionStatus() As Boolean
        Try
            Dim stm As Stream = tcpClient.GetStream
            'Dim ascenc As New ASCIIEncoding
            ' Dim byteData() As Byte = ascenc.GetBytes("test connection")
            'stm.Write(byteData, 0, byteData.Length)
            'stm.Close()
        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Connection lost"))
            'CloseTCPconnection()
            Return False
        End Try
        'ThreadExtensions.ScSend(context, New Action(Of UpdateListStatusEventArgs)(AddressOf OnUpdateListStatus), New UpdateListStatusEventArgs("Connection Okay"))
        Return True
    End Function

    Public Function CheckSocketConnectionStatus() As Boolean
        ' This is how you can determine whether a socket is still connected.
        'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("InCheckSocketConnectionStatus"))
        ' Dim blockingState As Boolean = False
        Dim bResult As Boolean = False
        Try
            'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("InCheckSocketConnectionStatus_1stTry"))
            ' blockingState = ClientSocket.Blocking
            Try
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("InCheckSocketConnectionStatus_2ndTry"))
                Dim ascenc As New ASCIIEncoding
                Dim tmp As Byte() = ascenc.GetBytes("Check connection status")

                ' ClientSocket.Blocking = False
                ClientSocket.Send(tmp, tmp.Length, SocketFlags.None)
                Dim replymsg(1000) As Byte
                Dim size As Integer = ClientSocket.Receive(replymsg, replymsg.Length, SocketFlags.None) 'reading the reply message and getting its size
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Server Acknowledgement for Checking"))
                'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(System.Text.Encoding.UTF8.GetString(replymsg)))

                bResult = True
            Catch e As SocketException
                ' 10035 == WSAEWOULDBLOCK
                If e.NativeErrorCode.Equals(10035) Then
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Still Connected, but the Send would block"))
                Else
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Disconnected: error code " & e.NativeErrorCode & "!"))
                End If
                bResult = False
                bConnected = False
                'Finally
                ' ClientSocket.Blocking = blockingState
            End Try
        Catch ex As Exception
            ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("InCheckSocketConnectionStatus_1stTry " & ex.Message))
            bResult = False
            bConnected = False
        End Try

        Return bResult
    End Function

    Public Sub CloseTCPconnection()
        Try
            tcpClient.Close()
        Catch ex As Exception
        End Try
    End Sub

    Public Sub CloseSocketConnection()
        Try
            CheckActiveConnectionTimer.Stop()
            RemoveHandler CheckActiveConnectionTimer.Elapsed, AddressOf HandleTimer
            bConnected = False
            MaxConnectionAttempt = -1
            ClientSocket.Close()
        Catch ex As Exception

        End Try
    End Sub

    Private Sub HandleTimer(ByVal sender As Object, ByVal e As EventArgs)
        'ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Checking Connection Status..."))
        If (Not CheckSocketConnectionStatus()) Then
            CheckActiveConnectionTimer.Stop()
            RemoveHandler CheckActiveConnectionTimer.Elapsed, AddressOf HandleTimer
            ThreadExtensions.ScSend(context, New Action(Of ConnectToServer)(AddressOf OnReConnectToServer), New ConnectToServer)
            'Me.ClientConnectSocketAsync(serverIP, serverPort)
        End If
    End Sub
End Class



Public Class UpdateTCPclient
    Inherits EventArgs
    'Private _message As String()
    Private NewTCPclient As TcpClient

    Public Sub New(ByVal tcpClient1 As TcpClient)
        '_message = Split(msg, ",")
        NewTCPclient = tcpClient1
    End Sub
    Public ReadOnly Property UpdatedTCPclient As TcpClient
        Get
            Return NewTCPclient
        End Get
    End Property
End Class

Public Class UpdateClientSocket
    Inherits EventArgs
    'Private _message As String()
    Private NewClientSocket As Socket

    Public Sub New(ByVal ClientSocket1 As Socket)
        '_message = Split(msg, ",")
        NewClientSocket = ClientSocket1
    End Sub
    Public ReadOnly Property UpdatedClientSocket As Socket
        Get
            Return NewClientSocket
        End Get
    End Property
End Class

Public Class ConnectToServer
    Inherits EventArgs
End Class

Public Class QuitAsClient
    Inherits EventArgs
End Class

Public Class StartTransferEvent
    Inherits EventArgs
End Class

Public Class ShutdownEvent
    Inherits EventArgs
End Class