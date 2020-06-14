
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Timers

Public Class ServerDoChat
    'variables
    Private serverIP As String = String.Empty
    Private serverPort As String = String.Empty
    Private bConnected As Boolean = False
    Private clientSocket As Socket
    Private clientNumber As Integer = 0
    Private clientMAC As String = String.Empty
    Private clientIP As String = String.Empty
    Private wakeUPtimer As New Timer(600000)
    Private wakeInterval As Integer = 10000 'hours

    'constructor
    Public Sub New(ByVal clientSocket1 As Socket, ByVal clientNumber1 As Integer, ByVal temp As Threading.SynchronizationContext, ByVal Sleephours As Integer)
        clientNumber = clientNumber1
        clientSocket = clientSocket1
        context = temp
        wakeInterval = Sleephours * 1000 * 3600
    End Sub

    'Property Function
    Public WriteOnly Property SetClientSocket As Socket
        Set(ByVal value As Socket)
            clientSocket = value
        End Set
    End Property

    'multithread synchronization
    'Private context As Threading.SynchronizationContext = Threading.SynchronizationContext.Current
    Private context As Threading.SynchronizationContext

    Public Event UpdateListBoxStatus As EventHandler(Of UpdateListBoxEventArgs)
    Protected Overridable Sub OnUpdateListBoxStatus(ByVal e As UpdateListBoxEventArgs)
        RaiseEvent UpdateListBoxStatus(Me, e)
    End Sub

    'Multithread function
    Public Sub DoChatAsync()
        ThreadExtensions.QueueUserWorkItem(New Action(Of Integer)(AddressOf doChat), clientNumber)
    End Sub

    'function
    Private Sub doChat(ByVal clNo As Integer)
        Dim requestCount As Integer
        Dim rCount As String
        requestCount = 0

        While (True)
            Try
                Dim bytesFrom(10024) As Byte
                Dim dataFromClient As String
                Dim sendBytes As [Byte]()
                Dim serverResponse As String
                Dim serverCommand As String = "Okay"
                requestCount = requestCount + 1
                clientSocket.Receive(bytesFrom, bytesFrom.Length, SocketFlags.None)
                dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom)

                'Check for Client MAC
                Dim clientMACregex As New Regex("MAC\-([A-Z0-9\-]+)")
                Dim m2 As Match = clientMACregex.Match(dataFromClient)
                If (m2.Success) Then
                    clientMAC = m2.Groups(1).ToString
                End If

                'Check for client IP
                Dim temp() As String = Split(clientSocket.RemoteEndPoint.ToString, ":")
                clientIP = temp(0)

                'Check for client connection checking message
                Dim CheckConnRegex As New Regex("Check connection status")
                Dim m5 As Match = CheckConnRegex.Match(dataFromClient)
                If (Not m5.Success) Then
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("From client-" + clNo.ToString + ": " + dataFromClient))
                End If
                rCount = Convert.ToString(requestCount)

                'Check for client response
                Dim WakeUpRegex As New Regex("Wake Up")
                Dim DoneTransferRegex As New Regex("Transfer Done")
                Dim m3 As Match = WakeUpRegex.Match(dataFromClient)
                Dim m4 As Match = DoneTransferRegex.Match(dataFromClient)
                If (m3.Success) Then
                    RecordSleepingComputer(clientIP, clientMAC, True)
                    serverCommand = "Start Transfer"
                End If
                If (m4.Success) Then
                    serverCommand = "Shut Down"
                End If

                serverResponse = "Server to client " + clNo.ToString + " (" + clientIP + ") " + rCount + ": " + serverCommand
                sendBytes = Encoding.ASCII.GetBytes(serverResponse)
                clientSocket.Send(sendBytes)

                If (Not m5.Success) Then
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(serverResponse))
                End If

                'check for shutting down phase
                Dim clientShutdownRegex As New Regex("Shutting Down")
                Dim m1 As Match = clientShutdownRegex.Match(dataFromClient)
                If (m1.Success) Then
                    AddHandler wakeUPtimer.Elapsed, AddressOf HandleTimer
                    'start the wakeUPtimer
                    wakeUPtimer.Interval = wakeInterval
                    wakeUPtimer.Start()
                    RecordSleepingComputer(clientIP, clientMAC)
                    ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(System.DateTime.Now.ToString & "--WakeUp Timer Started for " & clientSocket.RemoteEndPoint.ToString))
                End If
            Catch ex As Exception
                ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(ex.Message))
                Exit While
            End Try
        End While
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
        ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs("Magic Packet Sent to " & targetIP))
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

    Friend Sub stopWakeUpTimer(ByVal TimerIP As String)
        ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(System.DateTime.Now.ToString & "--Stopping Timer and removing handler for " & TimerIP))
        wakeUPtimer.Stop()
        RemoveHandler wakeUPtimer.Elapsed, AddressOf HandleTimer
    End Sub

    Private Sub HandleTimer(ByVal sender As Object, ByVal e As EventArgs)
        ThreadExtensions.ScSend(context, New Action(Of UpdateListBoxEventArgs)(AddressOf OnUpdateListBoxStatus), New UpdateListBoxEventArgs(System.DateTime.Now.ToString & "--Sending Packet to " & clientIP & " " & clientMAC))
        SendWOLPacket(clientIP, clientMAC)
        wakeUPtimer.Stop()
    End Sub

    Private Sub RecordSleepingComputer(ByVal CompIP As String, ByVal CompMAC As String, Optional ByVal bErase As Boolean = False)
        'record or erase sleepingComputer Record
        'gather all line until sleepingComputer Data into arraylist
        Dim originalLine As New ArrayList
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
                    originalLine.Add(currentline)
                End If
            Else
                If (bInSleepingSection) Then
                    Dim temp() As String = Split(currentline, "|")
                    If (CompADDDict.ContainsKey(temp(0))) Then
                        CompADDDict(temp(0)) = temp(1)
                    Else
                        CompADDDict.Add(temp(0), temp(1))
                    End If
                Else
                    originalLine.Add(currentline)
                End If
            End If
        Loop
        sr.Close()

        'add or remove ip/mac to CompADDDICT
        If (bErase) Then
            CompADDDict.Remove(CompIP)
        Else
            If (CompADDDict.ContainsKey(CompIP)) Then
                CompADDDict(CompIP) = CompMAC
            Else
                CompADDDict.Add(CompIP, CompMAC)
            End If
        End If

        'Add the lines back to originalLine arraylist
        originalLine.Add("Sleeping Computers:")
        For Each ComputerIP As String In CompADDDict.Keys
            originalLine.Add(ComputerIP & "|" & CompADDDict(ComputerIP))
        Next

        'Write all lines back to AutoBackup Setting
        Dim sw As New System.IO.StreamWriter("AutoBackup_Setting.cfg", False, System.Text.Encoding.Unicode)
        For nI As Integer = 0 To originalLine.Count - 1
            sw.WriteLine(originalLine(nI).ToString)
        Next
        sw.Close()

    End Sub

End Class




