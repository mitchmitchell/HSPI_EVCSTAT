Imports System.Text
Imports System.IO
Imports System.Threading
Imports HomeSeerAPI
Imports Scheduler

''' <summary>
''' The main class that HomeSeer will make function calls on
''' </summary>
''' <remarks></remarks>
<Serializable()>
Public Class HSPI
    Private Const sConfigPage As String = "EVCStat_Config"
    Private Const sHelpPage As String = "EVCStat_Help"
    Private ConfigPage As New EVCSTATConfig(sConfigPage)
    Dim actions As New hsCollection
    Dim action As New action
    Dim triggers As New hsCollection
    Dim trigger As New trigger
    Const Pagename = "Events"


#Region "HomeSeer-Required Functions"
    Public Function InterfaceStatus() As HomeSeerAPI.IPlugInAPI.strInterfaceStatus
        Dim es As New IPlugInAPI.strInterfaceStatus
        es.intStatus = IPlugInAPI.enumInterfaceStatus.OK
        Return es
    End Function

    Public Function SupportsMultipleInstances() As Boolean
        Return False
    End Function

    Public Function SupportsMultipleInstancesSingleEXE() As Boolean
        Return False
    End Function

    Function Name() As String
        Name = IFACE_NAME
    End Function

    Function InstanceFriendlyName() As String
        InstanceFriendlyName = Instance
    End Function

    Public Function AccessLevel() As Integer
        AccessLevel = 1
    End Function

    Public Function SupportsConfigDevice() As Boolean
        Return True
    End Function

    Public Function HSCOMPort() As Boolean
        Return False
    End Function

    Public Function Capabilities() As Integer
        Return HomeSeerAPI.Enums.eCapabilities.CA_IO Or Enums.eCapabilities.CA_Thermostat
    End Function

#End Region

#Region "Device Interface"

    Public Function ConfigDevice(ref As Integer, user As String, userRights As Integer, newDevice As Boolean) As String
        Return True
    End Function

    Public Function ConfigDevicePost(ref As Integer, data As String, user As String, userRights As Integer) As Enums.ConfigDevicePostReturn
        Return True
    End Function

    Public Sub SetIOMulti(colSend As System.Collections.Generic.List(Of HomeSeerAPI.CAPI.CAPIControl))
        Dim CC As CAPIControl
        Dim oStat As Thermostat
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim PED As clsPlugExtraData
        Dim DeviceType As Integer
        Dim RefIDs() As Integer
        Dim RefID As Integer
        Try
            For Each CC In colSend
                dv = hs.GetDeviceByRef(CC.Ref) 'get the device
                RefIDs = dv.AssociatedDevices(hs) 'get the parent ref
                RefID = RefIDs(0) 'first(and only) one in the list
                oStat = arrThermostats(RefID.ToString) ' use the parent ref to get the right thermostat
                PED = dv.PlugExtraData_Get(hs) 'the device type is referenced inside the PED
                DeviceType = PEDGet(PED, CC.Ref.ToString)

                Select Case DeviceType
                    Case Thermostat.eDeviceTypes.Heat_SetPoint
                        oStat.HeatSetPoint = CC.ControlValue
                    Case Thermostat.eDeviceTypes.Cool_SetPoint
                        oStat.CoolSetPoint = CC.ControlValue
                    Case Thermostat.eDeviceTypes.Fan
                        oStat.Fan = CC.ControlValue
                    Case Thermostat.eDeviceTypes.Hold
                        oStat.Hold = CC.ControlValue
                    Case Thermostat.eDeviceTypes.Mode
                        oStat.Mode = CC.ControlValue
                    Case Thermostat.eDeviceTypes.Filter_Remind
                        oStat.FilterRemind = CC.ControlValue
                    Case Thermostat.eDeviceTypes.RunTime
                        oStat.TotalRunTime = CC.ControlValue
                    Case Thermostat.eDeviceTypes.Damper_Status
                        oStat.Damper = CC.ControlValue
                End Select
                oStat.UpdatePhysicalDevice(DeviceType, CC.ControlValue) 'do this here to prevent the physical device from getting caught in a loop updating itself.
            Next
        Catch ex As Exception
            Log("A thermostat for device " & RefID & " was not found in the list of devices. Line# " & Erl(), LogLevel.Err)
        End Try
    End Sub

    Public Sub HSEvent(ByVal EventType As Enums.HSEvent, ByVal parms() As Object)
        Dim temp As String = ""
        Dim oThermostat As Thermostat
        Dim TempScaleF As Boolean
        Dim iTempScale
        Select Case EventType
            Case Enums.HSEvent.CONFIG_CHANGE
                Select Case parms(1)
                    Case Scheduler.Classes.ConfigChangeType.change_device
                        If arrThermostats.ContainsKey(parms(3).ToString) And parms(4) = Scheduler.Classes.Delete_Add_Change.Delete Then
                            'someone deleted the parent device of a thermostat from the device management page, so remove the thermostat
                            RemoveThermostat(parms(3))
                        End If
                End Select
            Case Enums.HSEvent.SETUP_CHANGE
                Select Case parms(1)
                    Case "gGlobalTempScaleF"
                        TempScaleF = CBool(parms(2))
                        iTempScale = IIf(TempScaleF, Thermostat.eTempScale.Fahrenheit, Thermostat.eTempScale.Celsius)
                        If Tempscale <> iTempScale Then 'the temp scale in HS3 was changed Fahrenheit/Celsius. Update all the thermostats.
                            For Each oThermostat In arrThermostats.Values
                                oThermostat.TempScale = iTempScale
                            Next
                            Tempscale = iTempScale
                        End If
                End Select
        End Select
    End Sub

#End Region


#Region "Action Properties"

    Sub SetActions()
        Dim o As Object = Nothing
        If actions.Count = 0 Then
            actions.Add(o, "Send Command")
        End If
    End Sub

    Function ActionCount() As Integer
        SetActions()
        Return actions.Count
    End Function

    ReadOnly Property ActionName(ByVal ActionNumber As Integer) As String
        Get
            SetActions()
            If ActionNumber > 0 AndAlso ActionNumber <= actions.Count Then
                Return IFACE_NAME & ": " & actions.Keys(ActionNumber - 1)
            Else
                Return ""
            End If
        End Get
    End Property

#End Region

#Region "Trigger Properties"

    Sub SetTriggers()
        Dim o As Object = Nothing
        If triggers.Count = 0 Then
            triggers.Add(o, "Recieve Command")
        End If
    End Sub

    Public ReadOnly Property HasTriggers() As Boolean
        Get
            SetTriggers()
            Return IIf(triggers.Count > 0, True, False)
        End Get
    End Property

    Public Function TriggerCount() As Integer
        SetTriggers()
        Return triggers.Count
    End Function

    Public ReadOnly Property SubTriggerCount(ByVal TriggerNumber As Integer) As Integer
        Get
            Dim trigger As trigger
            If ValidTrig(TriggerNumber) Then
                trigger = triggers(TriggerNumber - 1)
                If Not (trigger Is Nothing) Then
                    Return trigger.Count
                Else
                    Return 0
                End If
            Else
                Return 0
            End If
        End Get
    End Property

    Public ReadOnly Property TriggerName(ByVal TriggerNumber As Integer) As String
        Get
            If Not ValidTrig(TriggerNumber) Then
                Return ""
            Else
                Return IFACE_NAME & ": " & triggers.Keys(TriggerNumber - 1)
            End If
        End Get
    End Property

    Public ReadOnly Property SubTriggerName(ByVal TriggerNumber As Integer, ByVal SubTriggerNumber As Integer) As String
        Get
            Dim trigger As trigger
            If ValidSubTrig(TriggerNumber, SubTriggerNumber) Then
                trigger = triggers(TriggerNumber)
                Return IFACE_NAME & ": " & trigger.Keys(SubTriggerNumber)
            Else
                Return ""
            End If
        End Get
    End Property

    Friend Function ValidTrig(ByVal TrigIn As Integer) As Boolean
        SetTriggers()
        If TrigIn > 0 AndAlso TrigIn <= triggers.Count Then
            Return True
        End If
        Return False
    End Function

    Public Function ValidSubTrig(ByVal TrigIn As Integer, ByVal SubTrigIn As Integer) As Boolean
        Dim trigger As trigger = Nothing
        If TrigIn > 0 AndAlso TrigIn <= triggers.Count Then
            trigger = triggers(TrigIn)
            If Not (trigger Is Nothing) Then
                If SubTrigIn > 0 AndAlso SubTrigIn <= trigger.Count Then Return True
            End If
        End If
        Return False
    End Function

#End Region

#Region "Action Interface"

    Public Function HandleAction(ByVal ActInfo As IPlugInAPI.strTrigActInfo) As Boolean
        Dim Address As String = ""
        Dim Zone As String = ""
        Dim Command As String = ""
        Dim Value As String = ""
        Dim UID As String
        Dim CommandString As String

        UID = ActInfo.UID.ToString

        Try
            If Not (ActInfo.DataIn Is Nothing) Then
                DeSerializeObject(ActInfo.DataIn, action)
            Else
                Return False
            End If
            For Each sKey In action.Keys
                Select Case True
                    Case InStr(sKey, "Address_" & UID) > 0
                        Address = action(sKey)
                    Case InStr(sKey, "Zone_" & UID) > 0
                        Zone = action(sKey)
                    Case InStr(sKey, "Command_" & UID) > 0
                        Command = action(sKey)
                    Case InStr(sKey, "Value_" & UID) > 0
                        Value = action(sKey)
                End Select
            Next

            CommandString = "A=" & Address & " O=" & Zone & " " & Command & "=" & Value & vbCr

            ComThread.SendCommand(CommandString)

        Catch ex As Exception
            hs.WriteLog(IFACE_NAME, "Error executing action: " & ex.Message)
        End Try
        Return True
    End Function

    Public Function ActionConfigured(ByVal ActInfo As IPlugInAPI.strTrigActInfo) As Boolean
        Dim Configured As Boolean = False
        Dim sKey As String
        Dim itemsConfigured As Integer = 0
        Dim itemsToConfigure As Integer = 4
        Dim UID As String
        UID = ActInfo.UID.ToString

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
            For Each sKey In action.Keys
                Select Case True
                    Case InStr(sKey, "Address_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                    Case InStr(sKey, "Zone_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                    Case InStr(sKey, "Command_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                    Case InStr(sKey, "Value_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                End Select
            Next
            If itemsConfigured = itemsToConfigure Then Configured = True
        End If
        Return Configured
    End Function

    Public Function ActionBuildUI(ByVal sUnique As String, ByVal ActInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As String
        Dim UID As String
        UID = ActInfo.UID.ToString
        Dim stb As New StringBuilder
        Dim Address As String = ""
        Dim Zone As String = ""
        Dim Command As String = ""
        Dim Value As String = ""
        Dim txtAddress As New clsJQuery.jqTextBox("Address_" & UID & sUnique, "", "", Pagename, 20, True)
        Dim txtZone As New clsJQuery.jqTextBox("Zone_" & UID & sUnique, "", "", Pagename, 20, True)
        Dim txtCommand As New clsJQuery.jqTextBox("Command_" & UID & sUnique, "", "", Pagename, 20, True)
        Dim txtValue As New clsJQuery.jqTextBox("Value_" & UID & sUnique, "", "", Pagename, 50, True)
        Dim sKey As String


        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        Else 'new event, so clean out the action object
            action = New action
        End If

        For Each sKey In action.Keys
            Select Case True
                Case InStr(sKey, "Address_" & UID) > 0
                    Address = action(sKey)
                Case InStr(sKey, "Zone_" & UID) > 0
                    Zone = action(sKey)
                Case InStr(sKey, "Command_" & UID) > 0
                    Command = action(sKey)
                Case InStr(sKey, "Value_" & UID) > 0
                    Value = action(sKey)
            End Select
        Next

        txtAddress.defaultText = Address
        txtZone.defaultText = Zone
        txtCommand.defaultText = Command
        txtValue.defaultText = Value

        stb.Append("Address:" & txtAddress.Build & " Zone:" & txtZone.Build & "<br>")
        stb.Append("Command:" & txtCommand.Build & " Value:" & txtValue.Build)

        Return stb.ToString
    End Function

    Public Function ActionProcessPostUI(ByVal PostData As Collections.Specialized.NameValueCollection,
                                        ByVal ActInfo As IPlugInAPI.strTrigActInfo) As IPlugInAPI.strMultiReturn

        Dim Ret As New HomeSeerAPI.IPlugInAPI.strMultiReturn
        Dim UID As String
        UID = ActInfo.UID.ToString

        Ret.sResult = ""
        ' We cannot be passed info ByRef from HomeSeer, so turn right around and return this same value so that if we want, 
        '   we can exit here by returning 'Ret', all ready to go.  If in this procedure we need to change DataOut or TrigInfo,
        '   we can still do that.
        Ret.DataOut = ActInfo.DataIn
        Ret.TrigActInfo = ActInfo

        If PostData Is Nothing Then Return Ret
        If PostData.Count < 1 Then Return Ret

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        End If

        Dim parts As Collections.Specialized.NameValueCollection

        Dim sKey As String

        parts = PostData

        Try
            For Each sKey In parts.Keys
                If sKey Is Nothing Then Continue For
                If String.IsNullOrEmpty(sKey.Trim) Then Continue For
                Select Case True
                    Case InStr(sKey, "Address_" & UID) > 0, InStr(sKey, "Zone_" & UID) > 0, InStr(sKey, "Command_" & UID) > 0, InStr(sKey, "Value_" & UID) > 0
                        action.Add(CObj(parts(sKey)), sKey)
                End Select
            Next
            If Not SerializeObject(action, Ret.DataOut) Then
                Ret.sResult = IFACE_NAME & " Error, Serialization failed. Signal Action not added."
                Return Ret
            End If
        Catch ex As Exception
            Ret.sResult = "ERROR, Exception in Action UI of " & IFACE_NAME & ": " & ex.Message
            Return Ret
        End Try

        ' All OK
        Ret.sResult = ""
        Return Ret

    End Function

    Public Function ActionFormatUI(ByVal ActInfo As IPlugInAPI.strTrigActInfo) As String
        Dim stb As New StringBuilder
        Dim sKey As String
        Dim Address As String = ""
        Dim Zone As String = ""
        Dim Command As String = ""
        Dim Value As String = ""
        Dim CommandString
        Dim UID As String
        UID = ActInfo.UID.ToString

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        End If

        For Each sKey In action.Keys
            Select Case True
                Case InStr(sKey, "Address_" & UID) > 0
                    Address = action(sKey)
                Case InStr(sKey, "Zone_" & UID) > 0
                    Zone = action(sKey)
                Case InStr(sKey, "Command_" & UID) > 0
                    Command = action(sKey)
                Case InStr(sKey, "Value_" & UID) > 0
                    Value = action(sKey)
            End Select
        Next

        CommandString = "A=" & Address & " O=" & Zone & " " & Command & "=" & Value

        stb.Append(" send the command string: " & CommandString)


        Return stb.ToString
    End Function
#End Region

#Region "Trigger Interface"

    Public ReadOnly Property TriggerConfigured(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As Boolean
        Get
            Dim Configured As Boolean = False
            Dim sKey As String
            Dim itemsConfigured As Integer = 0
            Dim itemsToConfigure As Integer = 4
            Dim UID As String
            UID = TrigInfo.UID.ToString

            If Not (TrigInfo.DataIn Is Nothing) Then
                DeSerializeObject(TrigInfo.DataIn, trigger)
                For Each sKey In trigger.Keys
                    Select Case True
                        Case InStr(sKey, "Address_" & UID) > 0 AndAlso trigger(sKey) <> ""
                            itemsConfigured += 1
                        Case InStr(sKey, "Zone_" & UID) > 0 AndAlso trigger(sKey) <> ""
                            itemsConfigured += 1
                        Case InStr(sKey, "Command_" & UID) > 0 AndAlso trigger(sKey) <> ""
                            itemsConfigured += 1
                        Case InStr(sKey, "Value_" & UID) > 0 AndAlso trigger(sKey) <> ""
                            itemsConfigured += 1
                    End Select
                Next
                If itemsConfigured = itemsToConfigure Then Configured = True
            End If
            Return Configured
        End Get
    End Property

    Public Function TriggerBuildUI(ByVal sUnique As String, ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As String
        Dim UID As String
        UID = TrigInfo.UID.ToString
        Dim stb As New StringBuilder
        Dim Address As String = ""
        Dim Zone As String = ""
        Dim Command As String = ""
        Dim Value As String = ""
        Dim txtAddress As New clsJQuery.jqTextBox("Address_" & UID & sUnique, "", "", Pagename, 20, True)
        Dim txtZone As New clsJQuery.jqTextBox("Zone_" & UID & sUnique, "", "", Pagename, 20, True)
        Dim txtCommand As New clsJQuery.jqTextBox("Command_" & UID & sUnique, "", "", Pagename, 20, True)
        Dim txtValue As New clsJQuery.jqTextBox("Value_" & UID & sUnique, "", "", Pagename, 50, True)
        Dim sKey As String


        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, trigger)
        Else 'new event, so clean out the action object
            trigger = New trigger
        End If

        For Each sKey In action.Keys
            Select Case True
                Case InStr(sKey, "Address_" & UID) > 0
                    Address = trigger(sKey)
                Case InStr(sKey, "Zone_" & UID) > 0
                    Zone = trigger(sKey)
                Case InStr(sKey, "Command_" & UID) > 0
                    Command = trigger(sKey)
                Case InStr(sKey, "Value_" & UID) > 0
                    Value = trigger(sKey)
            End Select
        Next

        txtAddress.defaultText = Address
        txtZone.defaultText = Zone
        txtCommand.defaultText = Command
        txtValue.defaultText = Value

        stb.Append("Address:" & txtAddress.Build & " Zone:" & txtZone.Build & "<br>")
        stb.Append("Command:" & txtCommand.Build & " Value:" & txtValue.Build)

        Return stb.ToString
    End Function

    Public Function TriggerProcessPostUI(ByVal PostData As System.Collections.Specialized.NameValueCollection,
                                                     ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As HomeSeerAPI.IPlugInAPI.strMultiReturn
        Dim Ret As New HomeSeerAPI.IPlugInAPI.strMultiReturn
        Dim UID As String
        UID = TrigInfo.UID.ToString

        Ret.sResult = ""
        ' We cannot be passed info ByRef from HomeSeer, so turn right around and return this same value so that if we want, 
        '   we can exit here by returning 'Ret', all ready to go.  If in this procedure we need to change DataOut or TrigInfo,
        '   we can still do that.
        Ret.DataOut = TrigInfo.DataIn
        Ret.TrigActInfo = TrigInfo

        If PostData Is Nothing Then Return Ret
        If PostData.Count < 1 Then Return Ret

        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, trigger)
        End If

        Dim parts As Collections.Specialized.NameValueCollection

        Dim sKey As String

        parts = PostData

        Try
            For Each sKey In parts.Keys
                If sKey Is Nothing Then Continue For
                If String.IsNullOrEmpty(sKey.Trim) Then Continue For
                Select Case True
                    Case InStr(sKey, "Address_" & UID) > 0, InStr(sKey, "Zone_" & UID) > 0, InStr(sKey, "Command_" & UID) > 0, InStr(sKey, "Value_" & UID) > 0
                        trigger.Add(CObj(parts(sKey)), sKey)
                End Select
            Next
            If Not SerializeObject(trigger, Ret.DataOut) Then
                Ret.sResult = IFACE_NAME & " Error, Serialization failed. Signal Action not added."
                Return Ret
            End If
        Catch ex As Exception
            Ret.sResult = "ERROR, Exception in Action UI of " & IFACE_NAME & ": " & ex.Message
            Return Ret
        End Try

        ' All OK
        Ret.sResult = ""
        Return Ret

    End Function

    Public Function TriggerFormatUI(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As String
        Dim stb As New StringBuilder
        Dim sKey As String
        Dim Address As String = ""
        Dim Zone As String = ""
        Dim Command As String = ""
        Dim Value As String = ""
        Dim UID As String
        UID = TrigInfo.UID.ToString

        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, action)
        End If

        For Each sKey In action.Keys
            Select Case True
                Case InStr(sKey, "Address_" & UID) > 0
                    Address = action(sKey)
                Case InStr(sKey, "Zone_" & UID) > 0
                    Zone = action(sKey)
                Case InStr(sKey, "Command_" & UID) > 0
                    Command = action(sKey)
                Case InStr(sKey, "Value_" & UID) > 0
                    Value = action(sKey)
            End Select
        Next

        stb.Append(" the command " & Command & " with a value of " & Value & " was recieved from address " & Address & " for zone " & Zone & " ")


        Return stb.ToString
    End Function

#End Region


    Public Function InitIO(ByVal port As String) As String
        Dim dv As New Scheduler.Classes.DeviceClass
        Dim Response As String
        '        Dim BaudRate As String = "9600"
        Dim Interval As String = "0"
        Dim RefIDs() As String
        Dim RefID As String
        Dim TempScaleF As Boolean
        Dim MQTT_HostAddr As String = "127.0.0.1"
        Dim MQTT_SendTopic As String = "homeseer/evc/out"
        Dim MQTT_RecvTopic As String = "homeseer/evc/in"

        Console.WriteLine("InitIO starting.")
        BuildThermostatDataTable() 'initialize the data table

        TempScaleF = CBool(hs.GetINISetting("Settings", "GlobalTempScaleF", "True").Trim) 'get temp scale from HS
        Tempscale = IIf(TempScaleF, Thermostat.eTempScale.Fahrenheit, Thermostat.eTempScale.Celsius) 'set the global variable
        If Instance <> "" Then
            INI_File = Replace(IFACE_NAME, " ", "") & "_" & Replace(Instance, " ", "") & ".ini"
        Else
            INI_File = Replace(IFACE_NAME, " ", "") & ".ini"
        End If
        Response = hs.GetINISetting("Settings", "RefIDs", "", INI_File) 'this is a list of all the parent RefID's
        If Response.Length > 0 Then
            RefIDs = Response.Split("|")
            For Each RefID In RefIDs
                dv = hs.GetDeviceByRef(CInt(RefID))
                If Not dv Is Nothing Then
                    AddThermostat(dv.Name(hs), dv.Address(hs), dv.Ref(hs))
                    Log("A thermostat for device " & RefID & " was found in the list of devices:" & dv.Name(hs), LogLevel.Debug)
                    Console.WriteLine("A thermostat for device " & RefID & " was found in the list of devices:" & dv.Name(hs))
                Else
                    Log("A thermostat for device " & RefID & " was NOT found in the list of devices.", LogLevel.Err)
                    Console.WriteLine("A thermostat for device " & RefID & " was NOT found in the list of devices.")
                End If
            Next
        Else
            Log("No thermostat devices were found in the Plug-in INI file: " + INI_File + ".", LogLevel.Err)
            Console.WriteLine("No thermostat devices were found in the Plug-in INI file.")
        End If

        RegisterWebPage(sConfigPage)
        RegisterWebPage(sHelpPage, "Help")

        callback.RegisterEventCB(Enums.HSEvent.CONFIG_CHANGE, Me.Name, Me.InstanceFriendlyName)
        callback.RegisterEventCB(Enums.HSEvent.SETUP_CHANGE, Me.Name, Me.InstanceFriendlyName)

        '        BaudRate = hs.GetINISetting("Settings", "BaudRate", "9600", INI_File)
        gDebug = hs.GetINISetting("Settings", "Debug", False, INI_File)
        Interval = hs.GetINISetting("Settings", "PollInterval", "0", INI_File)
        MQTT_HostAddr = hs.GetINISetting("Settings", "MQTTHost", Main.sIp, INI_File)
        MQTT_SendTopic = hs.GetINISetting("Settings", "MQTTSend", "homeseer/evc/out", INI_File)
        MQTT_RecvTopic = hs.GetINISetting("Settings", "MQTTRecv", "homeseer/evc/in", INI_File)

        ComThread.SetMQTTHost(MQTT_HostAddr)
        ComThread.SetSendTopic(MQTT_SendTopic)
        ComThread.SetRecvTopic(MQTT_RecvTopic)

        '        ComThread.Start(port, CInt(BaudRate))
        ComThread.Start(port)
        ComThread.SetPolling(CInt(Interval))
        Console.WriteLine("InitIO completeing.")
        Return Nothing
    End Function

    Public Shared Sub ShutdownIO()
        Try
            ComThread.Halt()
        Catch ex As Exception
        End Try
        bShutDown = True
    End Sub

#Region "Web Page Processing"
    Public Function postBackProc(page As String, data As String, user As String, userRights As Integer) As String
        Return ConfigPage.postBackProc(page, data, user, userRights)
    End Function

    ' build and return the actual page
    Public Function GetPagePlugin(ByVal pageName As String, ByVal user As String, ByVal userRights As Integer, ByVal queryString As String) As String
        Return ConfigPage.GetPagePlugin(pageName, user, userRights, queryString)
    End Function
#End Region
End Class
