Imports System.Text
Imports System.IO
Imports HomeSeerAPI
Imports System.Runtime.Serialization.Formatters
Imports Scheduler
Imports System.Web

Public Module util
    Public Const IFACE_NAME = "EVCStat"
    Public Instance As String = ""
    Public hs As IHSApplication
    Public callback As IAppCallbackAPI
    Public action As New action
    Public trigger As New trigger
    Public ddTable As DataTable = Nothing
    Public dtThermostats As DataTable = Nothing
    Public arrThermostats As New hsCollection
    Public gDebug As Boolean
    Public ComThread As New CommThread
    Public OurInstanceFriendlyName As String = ""
    Public bShutDown As Boolean
    Public INI_File As String = Replace(IFACE_NAME, " ", "") & ".ini"
    Public Tempscale As Thermostat.eTempScale

    Public Enum LogLevel As Integer
        Normal
        Debug
        Err
    End Enum

    Public Enum eDataTableAction
        Update
        Delete
    End Enum

    Public Function AddThermostat(ByVal Name As String, ByVal Address As Integer, Optional ByVal RefID As Integer = 0) As Thermostat
        Dim oThermostat As Thermostat
        oThermostat = New Thermostat(Name, Address, RefID, Tempscale)
        arrThermostats.Add(CObj(oThermostat), oThermostat.RefID.ToString)
        SaveThermostats()
        Return oThermostat
    End Function

    Public Sub RemoveThermostat(ByVal ParentRefID As Integer)
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim RefIDs() As Integer
        Dim RefID As Integer
        arrThermostats.Remove(ParentRefID.ToString)
        dv = hs.GetDeviceByRef(ParentRefID)
        If dv IsNot Nothing Then
            RefIDs = dv.AssociatedDevices(hs)
            For Each RefID In RefIDs
                hs.DeleteDevice(RefID) 'delete the child devices
            Next
            hs.DeleteDevice(dv.Ref(hs)) 'delete the parent device
        Else 'the parent device has already been deleted, so loop through and find the child devices, and delete those
            Dim EN As Scheduler.Classes.clsDeviceEnumeration
            EN = hs.GetDeviceEnumerator
            If EN Is Nothing Then Throw New Exception(IFACE_NAME & " failed to get a device enumerator from HomeSeer.")
            Do
                dv = EN.GetNext
                If dv Is Nothing Then Continue Do
                If dv.Interface(Nothing) IsNot Nothing Then
                    If dv.Interface(Nothing).Trim = IFACE_NAME Then
                        RefIDs = dv.AssociatedDevices(hs) 'the parent ref should be in here
                        'dv.AssociatedDevices_List(Nothing)
                        For Each RefID In RefIDs
                            If RefID = ParentRefID Then 'we found a child device, so get rid of it.
                                hs.DeleteDevice(RefID)
                            End If
                        Next
                    End If
                End If
            Loop Until EN.Finished
        End If
        ChangeThermostatTable(eDataTableAction.Delete, ParentRefID)
        SaveThermostats() 'update the .ini list
    End Sub

    Public Sub SaveThermostats()
        'this list is kept to eliminate the need to iterate through all hs devices to find the ones needed for this plugin.
        Dim oThermostat As Thermostat
        Dim RefIDs As String = ""
        For Each oThermostat In arrThermostats.Values
            RefIDs &= oThermostat.RefID & "|"
        Next
        If RefIDs.Length > 0 Then RefIDs = Strings.Left(RefIDs, RefIDs.Length - 1)
        hs.SaveINISetting("Settings", "RefIDs", RefIDs, INI_File)
    End Sub

    Public Sub SaveData(ByVal data As String)
        Dim sKey As String
        Dim parts As Collections.Specialized.NameValueCollection
        Dim Interval As Integer = 0
        Dim Debug As Boolean = False
        Dim BaudRate As Integer = 0


        parts = HttpUtility.ParseQueryString(data)

        For Each sKey In parts.Keys
            If sKey Is Nothing Then Continue For
            If String.IsNullOrEmpty(sKey.Trim) Then Continue For
            Select Case True
                Case InStr(sKey, "TextBoxPoll") > 0
                    Interval = CInt(parts(sKey))
                Case InStr(sKey, "DropDownList") > 0
                    BaudRate = CInt(DDText("DropDownList", parts(sKey)))
                Case InStr(sKey, "CheckBoxDebug") > 0
                    Debug = CheckBoxValue(parts(sKey))
            End Select
        Next
        hs.SaveINISetting("Settings", "PollInterval", Interval, INI_File)
        hs.SaveINISetting("Settings", "BaudRate", BaudRate, INI_File)
        hs.SaveINISetting("Settings", "Debug", Debug, INI_File)
    End Sub

    Public Sub BuildThermostatDataTable()
        'this is to keep all the different ways of referencing the devices in one area.
        If (dtThermostats Is Nothing) Then
            dtThermostats = New DataTable
            dtThermostats.Columns.Add("DeviceType", GetType(Integer))
            dtThermostats.Columns.Add("RefID", GetType(Integer))
            dtThermostats.Columns.Add("ParentRefID", GetType(Integer))
            dtThermostats.Columns.Add("Value", GetType(Integer))
        End If
    End Sub

    Public Sub ChangeThermostatTable(ByVal Action As eDataTableAction, ByVal ParentRefID As Integer, Optional ByVal ChildRefID As Integer = 0, Optional ByVal Value As Integer = 0)
        Dim Rows() As DataRow
        Dim Row As DataRow

        Select Case Action
            Case eDataTableAction.Update
                Rows = dtThermostats.Select("ParentRefID='" & ParentRefID & "' And RefID='" & ChildRefID & "'")
                If Rows.Count > 0 Then Rows(0)("Value") = Value
            Case eDataTableAction.Delete
                Rows = dtThermostats.Select("ParentRefID='" & ParentRefID & "'")
                For Each Row In Rows
                    Row.Delete()
                Next
        End Select
        dtThermostats.AcceptChanges()
    End Sub

    Public Sub RegisterWebPage(ByVal link As String, Optional linktext As String = "", Optional page_title As String = "")
        Try
            hs.RegisterPage(link, IFACE_NAME, "")

            If linktext = "" Then linktext = link
            linktext = linktext.Replace("_", " ")
            If page_title = "" Then page_title = linktext
            Dim wpd As New HomeSeerAPI.webPageDesc
            wpd.plugInName = IFACE_NAME
            wpd.link = link
            wpd.linktext = linktext
            wpd.page_title = page_title
            callback.RegisterConfigLink(wpd)
            callback.RegisterLink(wpd)
        Catch ex As Exception
            Log("Error in InitIR - Registering Web Links: " & ex.Message, LogLevel.Normal)
        End Try
    End Sub

    Public Sub Log(ByVal Msg As String, Optional ByVal type As LogLevel = LogLevel.Normal)
        If gDebug And type = LogLevel.Debug Then
            hs.WriteLog(IFACE_NAME & " DEBUG", Msg)
        ElseIf type = LogLevel.Err Then
            hs.WriteLog(IFACE_NAME & " ERROR", Msg)
        ElseIf type <> LogLevel.Debug Then
            hs.WriteLog(IFACE_NAME, Msg)
        End If
    End Sub

    Function StringToBytes(ByVal inString As String) As Byte()
        If String.IsNullOrEmpty(inString) Then Return Nothing
        Dim TempBytes() As Byte = Nothing
        ReDim TempBytes(Len(inString) - 1)
        Dim i As Integer
        For i = 0 To Len(inString) - 1
            TempBytes(i) = Asc(Mid(inString, i + 1, 1))
        Next
        Return TempBytes
    End Function

    Function ByteArrayToString(ByVal byte_arr() As Byte) As String
        Dim i As Integer
        Dim outstr As String = ""
        Try
            For i = 0 To (byte_arr.Length - 1)
                outstr = outstr & Chr(byte_arr(i))
            Next
        Catch ex As Exception
            Log("Error in ByteArrayToString, " & ex.Message)
        End Try
        Return outstr

    End Function

    Sub PEDAdd(ByRef PED As clsPlugExtraData, ByVal PEDName As String, ByVal PEDValue As Object)
        Dim ByteObject() As Byte = Nothing
        If PED Is Nothing Then PED = New clsPlugExtraData
        SerializeObject(PEDValue, ByteObject)
        If Not PED.AddNamed(PEDName, ByteObject) Then
            PED.RemoveNamed(PEDName)
            PED.AddNamed(PEDName, ByteObject)
        End If
    End Sub

    Function PEDGet(ByRef PED As clsPlugExtraData, ByVal PEDName As String) As Object
        Dim ByteObject() As Byte
        Dim ReturnValue As New Object
        ByteObject = PED.GetNamed(PEDName)
        If ByteObject Is Nothing Then Return Nothing
        DeSerializeObject(ByteObject, ReturnValue)
        Return ReturnValue
    End Function

    Public Function SerializeObject(ByRef ObjIn As Object, ByRef bteOut() As Byte) As Boolean
        If ObjIn Is Nothing Then Return False
        Dim str As New MemoryStream
        Dim sf As New Binary.BinaryFormatter

        Try
            sf.Serialize(str, ObjIn)
            ReDim bteOut(CInt(str.Length - 1))
            bteOut = str.ToArray
            Return True
        Catch ex As Exception
            Log(IFACE_NAME & " Error: Serializing object " & ObjIn.ToString & " :" & ex.Message, LogLevel.Err)
            Return False
        End Try

    End Function

    Public Function DeSerializeObject(ByRef bteIn() As Byte, ByRef ObjOut As Object) As Boolean
        If bteIn Is Nothing Then Return False
        If bteIn.Length < 1 Then Return False
        If ObjOut Is Nothing Then Return False
        Dim str As MemoryStream
        Dim sf As New Binary.BinaryFormatter
        Dim ObjTest As Object
        Try
            ObjOut = Nothing
            str = New MemoryStream(bteIn)
            ObjTest = sf.Deserialize(str)
            If ObjTest Is Nothing Then Return False
            ObjOut = ObjTest
            If ObjOut Is Nothing Then Return False
            Return True
        Catch exIC As InvalidCastException
            Return False
        Catch ex As Exception
            Log(IFACE_NAME & " Error: DeSerializing object: " & ex.Message, LogLevel.Err)
            Return False
        End Try

    End Function

    Public Function FormDropDown(ByRef dd As clsJQuery.jqDropList, ByVal Name As String, ByRef options() As Pair, ByRef selected As Integer, _
                                 Optional ByVal width As Integer = 150, Optional ByVal SubmitForm As Boolean = True, Optional ByVal AddBlankRow As Boolean = True, _
                                 Optional ByVal AutoPostback As Boolean = True, Optional ByVal Tooltip As String = "", Optional ByVal Enabled As Boolean = True, _
                                 Optional ByVal ddMsg As String = "&nbsp;&nbsp;", Optional ByVal SelectedValue As String = Nothing) As String
        Dim i As Integer
        Dim sel As Boolean
        Dim Rows() As DataRow
        Dim Row As DataRow

        dd.ClearItems()
        dd.selectedItemIndex = -1
        dd.id = "o" & Name
        dd.name = Name
        dd.submitForm = SubmitForm
        dd.autoPostBack = AutoPostback
        dd.toolTip = Tooltip
        dd.style = "width: " & width & "px;"
        dd.enabled = Enabled
        'Add a blank area to the top of the list
        If AddBlankRow Then dd.AddItem(ddMsg, "", False)

        'save the visible text of the options for later use
        If (ddTable Is Nothing) Then
            ddTable = New DataTable
            ddTable.Columns.Add("ObjectName", GetType(String))
            ddTable.Columns.Add("OptionName", GetType(String))
            ddTable.Columns.Add("OptionValue", GetType(String))
        End If

        Rows = ddTable.Select("ObjectName='" & Name & "'")

        For Each Row In Rows
            Row.Delete()
        Next
        ddTable.AcceptChanges()

        If Not (options Is Nothing) Then
            For i = 0 To UBound(options)
                If i = selected Then
                    sel = True
                ElseIf options(i).Value = SelectedValue Then
                    sel = True
                Else
                    sel = False
                End If
                dd.AddItem(options(i).Name, options(i).Value, sel)
                ddTable.Rows.Add(Name, options(i).Name, options(i).Value)
            Next
        Else
            dd.selectedItemIndex = -1
        End If

        ddTable.AcceptChanges()

        Return dd.Build
    End Function

    Public Function FormListBox(ByRef lb As clsJQuery.jqListBox, ByVal Name As String, ByRef data() As Pair, _
                                 Optional ByVal height As Integer = 150, Optional ByVal Enabled As Boolean = True) As String
        Dim i As Integer
        Dim Rows() As DataRow
        Dim Row As DataRow

        lb.items.Clear()
        lb.id = "o" & Name
        lb.name = Name
        lb.style = "height: " & height & "px; width: 150px;"
        lb.enabled = Enabled

        'save the visible text of the options for later use
        If (ddTable Is Nothing) Then
            ddTable = New DataTable
            ddTable.Columns.Add("ObjectName", GetType(String))
            ddTable.Columns.Add("OptionName", GetType(String))
            ddTable.Columns.Add("OptionValue", GetType(String))
        End If

        Rows = ddTable.Select("ObjectName='" & Name & "'")

        For Each Row In Rows
            Row.Delete()
        Next
        ddTable.AcceptChanges()

        If Not (data Is Nothing) Then
            For i = 0 To UBound(data)
                lb.items.Add(data(i).Name)
                ddTable.Rows.Add(Name, data(i).Name, data(i).Value)
            Next
        End If

        ddTable.AcceptChanges()

        Return lb.Build
    End Function

    Public Function FormLabel(Name As String, Optional Message As String = "", Optional ByVal Visible As Boolean = True) As String
        Dim Content As String
        If Visible Then
            Content = Message & "<input id='" & Name & "' Name='" & Name & "' Type='hidden'>"
        Else
            Content = "<input id='" & Name & "' Name='" & Name & "' Type='hidden' value='" & Message & "'>"
        End If
        Return Content
    End Function

    Public Function FormButton(ByRef b As clsJQuery.jqButton, ByVal Name As String, Optional ByVal label As String = "Submit", Optional ByVal SubmitForm As Boolean = True, _
                               Optional ByVal ImagePathNormal As String = "", Optional ByVal ImagePathPressed As String = "", Optional ByVal ToolTip As String = "", _
                               Optional ByVal Enabled As Boolean = True, Optional ByVal Style As String = "") As String
        Dim Button As String
        b.id = "o" & Name
        b.name = Name
        b.submitForm = SubmitForm
        b.label = label
        b.imagePathNormal = ImagePathNormal
        b.imagePathPressed = IIf(ImagePathPressed = "", b.imagePathNormal, ImagePathPressed)
        b.toolTip = ToolTip
        b.enabled = Enabled
        b.style = Style

        Button = b.Build
        Button = Trim(Replace(Button, "</button>" & vbCrLf, "</button>"))
        Return Button
    End Function

    Public Function FormTextBox(ByRef tb As clsJQuery.jqTextBox, ByVal Name As String, Optional ByVal DefaultText As String = "", Optional ByVal SubmitForm As Boolean = True, Optional ByVal Size As Integer = 150, Optional ByVal Tooltip As String = "") As String
        tb.id = "o" & Name
        tb.name = Name
        tb.inputType = ""
        tb.defaultText = DefaultText
        tb.size = Size
        tb.submitForm = SubmitForm
        tb.toolTip = Tooltip
        Return (tb.Build)
    End Function

    Public Function HTMLTextBox(ByVal Name As String, ByVal DefaultText As String, ByVal Size As Integer, Optional ByVal AllowEdit As Boolean = True) As String
        Dim ObjectString As String
        Dim Style As String
        Dim sReadOnly As String
        If AllowEdit Then
            Style = ""
            sReadOnly = ""
        Else
            Style = "color:#F5F5F5; background-color:#C0C0C0;"
            sReadOnly = "readonly='readonly'"
        End If
        ObjectString = "<input type='text' id='o" & Name & "' style='" & Style & "' size='" & Size & "' name='" & Name & "' " & sReadOnly & " value='" & DefaultText & "'>"
        Return ObjectString
    End Function

    Public Function FormCheckBox(ByRef cb As clsJQuery.jqCheckBox, ByVal Name As String, Optional ByVal Checked As Boolean = False, Optional ByVal AutoPostBack As Boolean = True, _
                                 Optional ByVal SubmitForm As Boolean = True) As String
        cb.id = "o" & Name
        cb.name = Name
        cb.checked = Checked
        cb.autoPostBack = AutoPostBack
        cb.submitForm = SubmitForm
        Return cb.Build
    End Function

    Public Function DDText(ByVal DDName As String, DDValue As String) As String
        Dim Rows() As DataRow
        Dim Row As DataRow
        Dim ReturnValue As String = ""
        Rows = ddTable.Select("ObjectName='" & DDName & "' AND OptionValue='" & DDValue & "'")
        For Each Row In Rows
            ReturnValue = Row.Item("OptionName")
        Next
        Return ReturnValue
    End Function

    Public Function DDValue(ByVal DDName As String, DDText As String) As String
        Dim Rows() As DataRow
        Dim Row As DataRow
        Dim ReturnValue As String = ""
        Rows = ddTable.Select("ObjectName='" & DDName & "' AND OptionName='" & DDText & "'")
        For Each Row In Rows
            ReturnValue = Row.Item("OptionValue")
        Next
        Return ReturnValue
    End Function

    Public Function DDCount(ByVal DDName) As String
        Dim Rows() As DataRow
        Rows = ddTable.Select("ObjectName='" & DDName & "'")
        Return Rows.Count
    End Function

    Public Function CheckBoxValue(ByVal value As String) As Integer
        If value = "on" Then
            Return 1
        Else
            Return 0
        End If
    End Function

#Region "Sub Procedures"
    Public Sub CheckTriggers(ByVal Address As String, ByVal Zone As String, ByVal Command As String, ByVal Value As String)
        Dim TrigInfo As New HomeSeerAPI.IPlugInAPI.strTrigActInfo
        Dim TrigsToCheck() As IAllRemoteAPI.strTrigActInfo = Nothing
        Dim Configured As Boolean = False
        Dim sKey As String
        Dim itemsConfigured As Integer = 0
        Dim itemsToConfigure As Integer = 4
        Dim UID As String
        Try
            TrigsToCheck = callback.TriggerMatches(IFACE_NAME, 1, -1)
        Catch ex As Exception
        End Try
        If TrigsToCheck IsNot Nothing Then
            For Each TrigInfo In TrigsToCheck
                UID = TrigInfo.UID.ToString

                If TrigInfo.DataIn IsNot Nothing Then
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
                If Configured Then 'we had a match. fire the trigger and reset the criteria in order to look for other triggers to match
                    callback.TriggerFire(IFACE_NAME, TrigInfo)
                    itemsConfigured = 0
                    Configured = False
                End If
            Next
        End If
    End Sub


#End Region
End Module