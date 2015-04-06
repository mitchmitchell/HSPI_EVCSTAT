Imports System.IO
Imports System.Threading
Imports VB = Microsoft.VisualBasic

Public Class CommThread
    Dim PollTimer As New System.Timers.Timer
    Dim WithEvents rs232 As Ports.SerialPort
    Public Shared TransmitQ As New Queue
    Dim RThread As Thread
    Public Port As String
    Public BaudRate As Integer
    Public PollInterval As Integer
    Dim iRetries As Integer = 0

    Public Sub New()
        MyBase.New()
        AddHandler PollTimer.Elapsed, AddressOf PollTimer_Tick
    End Sub

    Public Function Start(Optional ByVal sPort As String = "", Optional ByVal iBaudRate As Integer = -1) As Boolean
        If sPort <> "" Then Port = sPort
        If iBaudRate > 0 Then BaudRate = iBaudRate
        Try
            rs232 = New Ports.SerialPort(Port, BaudRate, Ports.Parity.None, 8, Ports.StopBits.One)
            rs232.Open()
            rs232.ReceivedBytesThreshold = 1
            rs232.RtsEnable = True
            rs232.DtrEnable = True
            rs232.Handshake = Ports.Handshake.None
            rs232.DiscardNull = False

            RThread = New Threading.Thread(AddressOf Comm)
            RThread.Start()
            iRetries = 0
            Return True
        Catch ex As Exception
            Log("Error in COM Thread Start: " & ex.Message)
            If iRetries < 5 Then
                iRetries += 1
                Start()
            Else
                Log("Abandoning COM Thread Start: " & ex.Message)
                Return False
            End If
        End Try
    End Function

    Public Sub Halt()
        RThread.Abort()
        rs232.Close()
    End Sub

    Public Sub Restart()
        Halt()
        Start()
    End Sub

    Public Sub SendCommand(ByVal Command As String)
        Dim tqi As New TransmitQitem
        Try
            tqi.Buf = StringToBytes(Command)
            tqi.Count = Command.Length
            tqi.RetryCount = 0
            TransmitQ.Enqueue(tqi)
        Catch ex As Exception
            Log("Error in SendCMD: " & ex.Message)
        End Try
    End Sub

    Private Sub Comm()
        ' init ports here so all work is done in this thread
10:     Dim buf() As Byte
20:     Dim st As String = ""
30:     Dim stlen As String
40:     Dim Count As Integer
50:     Dim response As String
60:     Dim RetCnt As Integer
        Dim bNewData As Boolean = False
70:
90:
100:    Do
110:        Try
120:            Thread.Sleep(100)
130:
140:            If TransmitQ.Count > 0 Then
150:                Dim ToWrite As String = ""
160:                Do
170:                    Try
180:                        Dim tqi As TransmitQitem = TransmitQ.Dequeue
190:                        Log("Writing Data: " & ByteArrayToString(tqi.Buf), LogLevel.Debug)
200:                        rs232.Write(tqi.Buf, 0, tqi.Count)
210:                        Thread.Sleep(250)
220:                    Catch ex As Exception
230:                        Log("Error Writing Data: " & ex.Message)
240:                    End Try
250:                Loop While TransmitQ.Count > 0
260:            End If
270:
280:            Do
290:                Count = rs232.BytesToRead
300:                If Count > 0 Then
310:                    ReDim buf(Count - 1)
320:                    rs232.Read(buf, 0, Count)
330:                    st = st & ByteArrayToString(buf)
                        bNewData = True
340:                End If
350:            Loop While rs232.BytesToRead > 0
360:
370:            If bNewData Then
380:                If InStr(st, vbCr) Then
                        Log("Recieving Data: " & Left(st, st.Length - 1), LogLevel.Debug)
                    Else
                        Log("Recieving Data: " & st, LogLevel.Debug)
                    End If
                    bNewData = False
390:            End If
400:
410:            If InStr(st, vbCr) Then
420:                Do
430:                    ' handle processing of COM data here
440:                    stlen = InStr(st, vbCr)
450:                    response = Strings.Left(st, stlen - 1)
460:                    ProcessResponse(response)
470:                    st = st.Remove(0, stlen)
480:
490:                Loop While InStr(st, vbCr)
500:            End If
510:
520:            RetCnt = 0
530:
540:        Catch ex As Exception
550:            Log("Error in Poll Thread, " & ex.Message & " Line Number:" & Err.Erl)
560:            st = ""
570:            Thread.Sleep(10000)
580:            RetCnt += 1
590:            If RetCnt = 10 Then
600:                Log("Poll Thread Error, plugin is shutting down. " & ex.Message)
610:                HSPI.ShutdownIO()
620:            End If
630:        End Try
640:    Loop

    End Sub

    Private Sub ProcessResponse(ByVal text As String)
        'this area is custom for each thermostat manufacturer
10:     Dim addr As String
20:     Dim iStart As Integer
30:     Dim iLength As Integer
40:     Dim Rows() As DataRow
50:     Dim oThermostat As Thermostat = Nothing
60:     Try
70:         'extract the qualifier (address) for the thermostat
80:         iStart = InStr(text, "A=") - 1
            'just keep the data you need
90:         text = text.Substring(iStart, text.Length - iStart)
95:         iLength = InStr(1, text, Chr(32)) - 1
100:        addr = text.Substring(0, iLength)
110:        addr = Strings.Right(addr, addr.Length - 2)
120:        addr = addr.Trim
130:        Log("Processing Dataline - " & text, LogLevel.Debug)
            If addr = "255" Then 'global message, update all thermostats.
                For Each oThermostat In arrThermostats.Values
                    oThermostat.ProcessDataReceived(text)
                Next
            Else 'find the correct thermostat refID the data is for
                Rows = dtThermostats.Select("DeviceType=0 And Value='" & addr & "'")
150:            If Rows IsNot Nothing AndAlso Rows.Count > 0 Then
160:                'get the thermostat and send the data to it to be processed.
170:                oThermostat = arrThermostats(Rows(0)("RefID").ToString)
180:                If oThermostat IsNot Nothing Then oThermostat.ProcessDataReceived(text)
                Else
                    Log("A thermostat with address " & addr & " was not found in the list of devices.", LogLevel.Err)
                    Log("Return dataline: " & text, LogLevel.Err)
                End If
            End If
140:       
190:    Catch ex As Exception
200:        Log("Error in ProcessResponse, " & ex.Message & " Line Number:" & Err.Erl, LogLevel.Err)
            Log("Error in ProcessResponse, return dataline: " & text, LogLevel.Err)
210:    End Try
    End Sub

    Public Sub SetPolling(Optional ByVal Interval As Integer = -1)
        'the polling timer is seperate from the comm thread loop
        If Interval >= 0 Then PollInterval = Interval
        If PollInterval < 0 Then PollInterval = 0
        PollTimer.Enabled = False
        If PollInterval > 0 Then
            PollTimer.Interval = PollInterval * 1000 'interval is in milliseconds, so adjust the interval number
            PollTimer.Enabled = True
        End If
    End Sub

    Private Sub PollTimer_Tick()
        Dim oThermostat As Thermostat
        For Each oThermostat In arrThermostats.Values
            oThermostat.Poll()
        Next
    End Sub

End Class
