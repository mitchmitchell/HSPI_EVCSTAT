Imports HomeSeerAPI
Imports Scheduler

Public Class Thermostat
#Region "Variables"
    Public RefID As Integer = 0
    Const eParent As Integer = 0 'this is not in the enumeration to exclude it from being included in the loop of the child devices
    Dim arrValues As New hsCollection
    Dim SetPoint_Low As Integer
    Dim SetPoint_High As Integer
    Dim Temp_Low As Integer
    Dim Temp_High As Integer
    Dim iTempScale As Integer
#End Region

#Region "Enumerations"
    Enum eDeviceValues
        'parent device values
        Address    'This is the default parent value
        'the rest use custom code
        Name
        Location
        Location2
        Message
    End Enum

    Enum eDeviceTypes
        Heat_SetPoint = 1
        Cool_SetPoint
        Mode
        Fan
        Hold
        Temperature
        Outside_Temp
        RunTime
        Filter_Remind
    End Enum

    Enum eMode As Integer
        Off
        Heat
        Cool
        Auto
        Aux
    End Enum

    Enum eFan As Integer
        Auto
        FanOn
    End Enum

    Enum eHold
        Run
        Hold
        Tmp
    End Enum

    Enum eTempScale
        Fahrenheit
        Celsius
    End Enum

    Enum eAddress
        Low = 1
        High = 15
    End Enum
#End Region

#Region "Object Initialization"
    Public Sub New(ByVal DeviceName As String, ByVal Address As Integer, Optional ByVal iRefID As Integer = 0, Optional ByVal TempScale As eTempScale = eTempScale.Fahrenheit)
        MyBase.New()
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim enumValues As Array = System.[Enum].GetValues(GetType(eDeviceTypes))
        Dim ChildRefID As Integer

        Me.TempScale = TempScale

        If iRefID > 0 Then 'the device already exists
            RefID = iRefID
        Else
            RefID = CreateDevice(DeviceName, Address)
        End If

        dv = GetDevice(eParent)

        dtThermostats.Rows.Add(eParent, RefID, RefID, dv.Address(hs)) 'set the child and parent ref ID's to the same
        dtThermostats.AcceptChanges()

        Dim PED As clsPlugExtraData = dv.PlugExtraData_Get(hs)
        If PED Is Nothing Then PED = New clsPlugExtraData
        For Each ChildDevice As eDeviceTypes In enumValues
            ChildRefID = PEDGet(PED, ChildDevice.ToString) 'get the ref for this child device from the parent data
            ChildRefID = IIf(ChildRefID = Nothing, 0, ChildRefID) 'qualify the result
            ChildRefID = CreateDevice(ChildDevice.ToString, Address, ChildDevice, ChildRefID) 'check for a device, if not found, create it
            dtThermostats.Rows.Add(ChildDevice, ChildRefID, RefID, 0) 'keep the data centrally located for multiple lookup types
            PEDAdd(PED, ChildDevice.ToString, ChildRefID) 'Keep a reference to the child ref using the enumeration for quicker retrieval.
            BindParentAndChild(dv, RefID, ChildRefID) 'in this loop the child is being bound to the parent device ('cuz we have it)
        Next
        dv.PlugExtraData_Set(hs) = PED
        Me.Name = DeviceName
        Poll()
    End Sub
#End Region

#Region "Properties"
    Public Property Name() As String
        Get
            Return GetDeviceValue(eParent, eDeviceValues.Name)
        End Get
        Set(ByVal value As String)
            SetDeviceValue(eParent, value, eDeviceValues.Name)
        End Set
    End Property

    Public Property Location() As String
        Get
            Return GetDeviceValue(eParent, eDeviceValues.Location)
        End Get
        Set(ByVal value As String)
            SetDeviceValue(eParent, value, eDeviceValues.Location)
        End Set
    End Property

    Public Property Location2() As String
        Get
            Return GetDeviceValue(eParent, eDeviceValues.Location2)
        End Get
        Set(ByVal value As String)
            SetDeviceValue(eParent, value, eDeviceValues.Location2)
        End Set
    End Property

    Public Property Address() As Integer
        Get
            Return GetDeviceValue(eParent, eDeviceValues.Address)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eParent, value, eDeviceValues.Address)
        End Set
    End Property

    Public Property HeatSetPoint() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Heat_SetPoint)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Heat_SetPoint, value)
        End Set
    End Property

    Public Property CoolSetPoint() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Cool_SetPoint)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Cool_SetPoint, value)
        End Set
    End Property

    Public Property Mode() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Mode)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Mode, value)
        End Set
    End Property

    Public Property Fan() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Fan)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Fan, value)
        End Set
    End Property

    Public Property Hold() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Hold)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Hold, value)
        End Set
    End Property

    Public Property Temperature() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Temperature)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Temperature, value)
        End Set
    End Property

    Public Property OutsideTemp() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Outside_Temp)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Outside_Temp, value)
        End Set
    End Property

    Public Property TempScale() As eTempScale
        Get
            Return iTempScale
        End Get
        Set(ByVal value As eTempScale)
            iTempScale = value
            SetTempScaleData(value)
        End Set
    End Property
    Public Property Message() As String
        Get
            Return GetDeviceValue(eParent, eDeviceValues.Message)
        End Get
        Set(ByVal value As String)
            SetDeviceValue(eParent, value, eDeviceValues.Message)
        End Set
    End Property

    Public Property FilterRemind() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.Filter_Remind)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.Filter_Remind, value)
        End Set
    End Property
    Public Property TotalRunTime() As Integer
        Get
            Return GetDeviceValue(eDeviceTypes.RunTime)
        End Get
        Set(ByVal value As Integer)
            SetDeviceValue(eDeviceTypes.RunTime, value)
        End Set
    End Property
#End Region

#Region "Private Subs/Functions"

    Private Sub SetTempScaleData(ByVal TempScale As eTempScale)
        Select Case TempScale
            Case eTempScale.Fahrenheit
                SetPoint_Low = 40
                SetPoint_High = 99
                Temp_Low = -67
                Temp_High = 257
            Case eTempScale.Celsius
                SetPoint_Low = 5
                SetPoint_High = 37
                Temp_Low = -55
                Temp_High = 125
        End Select
        UpdateTempControls()
    End Sub

    Private Sub UpdateTempControls()
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim RefIDs() As Integer
        Dim iRefID As Integer
        Dim PED As clsPlugExtraData
        Dim DeviceType As Integer
        dv = hs.GetDeviceByRef(RefID)
        If dv IsNot Nothing Then
            RefIDs = dv.AssociatedDevices(hs) 'get all the child devices
            For Each iRefID In RefIDs
                dv = hs.GetDeviceByRef(iRefID)
                PED = dv.PlugExtraData_Get(hs) 'the device type is referenced inside the PED
                DeviceType = PEDGet(PED, dv.Ref(hs))
                Select Case DeviceType 'clear out previous controls
                    Case Thermostat.eDeviceTypes.Heat_SetPoint, Thermostat.eDeviceTypes.Cool_SetPoint, Thermostat.eDeviceTypes.Temperature
                        hs.DeviceVSP_ClearAll(dv.Ref(hs), True)
                End Select
                Select Case DeviceType 'add new controls based on device type
                    Case Thermostat.eDeviceTypes.Heat_SetPoint
                        AddControl(dv.Ref(hs), "Heat Point", DeviceType, SetPoint_Low, SetPoint_High)
                    Case Thermostat.eDeviceTypes.Cool_SetPoint
                        AddControl(dv.Ref(hs), "Cool Point", DeviceType, SetPoint_Low, SetPoint_High)
                    Case Thermostat.eDeviceTypes.Temperature
                        AddControl(dv.Ref(hs), "Temp", DeviceType, Temp_Low, Temp_High)
                    Case Thermostat.eDeviceTypes.Outside_Temp
                        AddControl(dv.Ref(hs), "Outside Temp", DeviceType, Temp_Low, Temp_High)
                End Select
            Next
        End If
    End Sub

    Private Function GetDeviceValue(ByVal DeviceType As eDeviceTypes, Optional ByVal ParentData As eDeviceValues = eDeviceValues.Address) As String
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim ReturnValue As String = ""
        Dim ref As Integer
        Dim PED As clsPlugExtraData
        If arrValues.ContainsKey(DeviceType.ToString) Then 'we already have the value for this device, don't get the device, no need.
            ReturnValue = arrValues(DeviceType.ToString)
        Else
            dv = GetDevice(DeviceType)
            ref = dv.Ref(Nothing)
            Select Case DeviceType
                Case eParent 'the parent device holds multiple pieces of info that we need
                    Select Case ParentData
                        Case eDeviceValues.Name
                            ReturnValue = dv.Name(Nothing)
                        Case eDeviceValues.Address
                            PED = dv.PlugExtraData_Get(hs)
                            ReturnValue = PEDGet(PED, "Parent")
                        Case eDeviceValues.Location
                            ReturnValue = dv.Location(Nothing)
                        Case eDeviceValues.Location2
                            ReturnValue = dv.Location2(Nothing)
                    End Select
                Case Else 'get the default value from the device
                    PED = dv.PlugExtraData_Get(hs)
                    ReturnValue = PEDGet(PED, DeviceType.ToString)
            End Select
        End If
        Return ReturnValue
    End Function

    Private Sub SetDeviceValue(ByVal DeviceType As eDeviceTypes, ByVal Value As String, Optional ByVal ParentData As eDeviceValues = eDeviceValues.Address)
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim ref As Integer
        Dim PED As clsPlugExtraData
        Dim DeviceValue As String = ""

        Value = Value.Replace("_", " ")
        'because the parent device holds multiple pieces of info, we can't use the data list, as the key would be the same for all the parent data.
        Select Case DeviceType
            Case eParent 'the parent device holds multiple pieces of info that we need to set
                dv = GetDevice(DeviceType)
                ref = dv.Ref(Nothing)
                Select Case ParentData
                    Case eDeviceValues.Name
                        dv.Name(hs) = Value
                    Case eDeviceValues.Address
                        ChangeThermostatTable(eDataTableAction.Update, RefID, ref, Value)
                        PED = dv.PlugExtraData_Get(hs)
                        PEDAdd(PED, "Parent", Value)
                        dv.PlugExtraData_Set(hs) = PED
                    Case eDeviceValues.Location
                        dv.Location(hs) = Value
                        UpdateChildDevices(eDeviceValues.Location, Value)
                    Case eDeviceValues.Location2
                        dv.Location2(hs) = Value
                        UpdateChildDevices(eDeviceValues.Location2, Value)
                    Case eDeviceValues.Message
                        dv.AdditionalDisplayData(hs) = {Value}
                End Select
            Case Else 'set the default value for the device
                dv = GetDevice(DeviceType)
                ref = dv.Ref(Nothing)
                'add to our data list for quick internal retrieval.
                arrValues.Add(CObj(Value), DeviceType.ToString)
                'keep the data table current, we may use it at some point for data values.
                ChangeThermostatTable(eDataTableAction.Update, RefID, ref, Value)
                'update the HS3 device
                PED = dv.PlugExtraData_Get(hs)
                PEDAdd(PED, DeviceType.ToString, Value)
                dv.PlugExtraData_Set(hs) = PED
                DeviceValue = dv.devValue(Nothing)
                If DeviceValue <> Value AndAlso Value <> "" Then 'something has changed, so update
                    hs.SetDeviceValueByRef(ref, Value, True)
                End If
        End Select

    End Sub

    Public Sub UpdatePhysicalDevice(ByVal DeviceType As eDeviceTypes, ByVal Value As Integer)
        'This is custom based on the manufacturer
        Dim Addr As String = ""
        Dim Command As String = ""

        Select Case DeviceType
            Case eDeviceTypes.Heat_SetPoint
                Command = "SPH=" & CStr(Value)
            Case eDeviceTypes.Cool_SetPoint
                Command = "SPC=" & CStr(Value)
            Case eDeviceTypes.Fan
                Select Case Value
                    Case eFan.Auto
                        Command = "F=0"
                    Case eFan.FanOn
                        Command = "F=1"
                End Select
            Case eDeviceTypes.Hold
                Select Case Value
                    Case eHold.Run
                        Command = "SC=0"
                    Case eHold.Hold
                        Command = "SC=1"
                    Case eHold.Tmp ' dont really ever send this command out.
                        Command = "SC=2"
                End Select
            Case eDeviceTypes.Mode
                Select Case Value
                    Case eMode.Auto
                        Command = "M=3"
                    Case eMode.Aux
                        Command = "M=EH"
                    Case eMode.Cool
                        Command = "M=2"
                    Case eMode.Heat
                        Command = "M=1"
                    Case eMode.Off
                        Command = "M=0"
                End Select
        End Select
        If Command.Length > 0 Then
            Addr = "A=" & Me.Address.ToString & " "
            Command = Addr & "O=00 " & Command & vbCr
            SendCMD(Command)
        End If
    End Sub

    Private Function GetDevice(ByVal DeviceType As eDeviceTypes) As Scheduler.Classes.DeviceClass
        Select Case DeviceType
            Case eParent
                Return hs.GetDeviceByRef(RefID)
            Case Else
                Return hs.GetDeviceByRef(GetRefID(DeviceType))
        End Select
    End Function

    Private Function GetRefID(ByVal DeviceType As eDeviceTypes) As Integer
        Dim Rows() As DataRow
        Dim DeviceRefID As Integer = 0
        Rows = dtThermostats.Select("ParentRefID='" & RefID & "' AND DeviceType='" & DeviceType & "'")
        DeviceRefID = Rows(0).Item("RefID")
        Return DeviceRefID
    End Function

    Private Function GetDeviceType(ByVal DeviceRefID As Integer) As Integer
        Dim Rows() As DataRow
        Dim DeviceType As Integer
        Rows = dtThermostats.Select("ParentRefID='" & RefID & "' AND RefID='" & DeviceRefID & "'")
        DeviceType = Rows(0).Item("DeviceType")
        Return DeviceType
    End Function

    Private Sub UpdateChildDevices(ByVal DataType As eDeviceValues, ByVal value As String)
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim RefIDs() As Integer
        Dim iRefID As Integer
        dv = hs.GetDeviceByRef(RefID)
        If dv IsNot Nothing Then
            RefIDs = dv.AssociatedDevices(hs)
            For Each iRefID In RefIDs
                dv = hs.GetDeviceByRef(iRefID)
                Select Case DataType
                    Case eDeviceValues.Location
                        dv.Location(hs) = value
                    Case eDeviceValues.Location2
                        dv.Location2(hs) = value
                End Select
            Next
        End If
    End Sub

    Private Sub BindParentAndChild(ByVal dv As Scheduler.Classes.DeviceClass, ByVal ParentRefID As Integer, ByVal ChildRefID As Integer)
        'in a parent/child relationship in Homeseer both parties must be aware of each other
        'the parent must be linked to the child, and the child must be linked to the parent.

        If ParentRefID = ChildRefID Then Exit Sub 'the child IS the parent, don't do anything.
        If dv.Ref(hs) = ParentRefID Then 'we're binding the child to the parent
            dv.AssociatedDevice_Add(hs, ChildRefID)
            dv.Relationship(hs) = Enums.eRelationship.Parent_Root
        Else 'we're binding the parent to the child
            dv.AssociatedDevice_Add(hs, ParentRefID)
            dv.Relationship(hs) = Enums.eRelationship.Child
        End If
    End Sub

    Sub AddControl(ByVal ref As Integer, ByVal name As String, DeviceType As eDeviceTypes, ByVal value1 As Integer, Optional ByVal value2 As Integer = 0)
        Dim Pair As VSPair = Nothing
        Select Case DeviceType
            Case eDeviceTypes.Filter_Remind
                Pair = New VSPair(HomeSeerAPI.ePairStatusControl.Both)
                Pair.PairType = VSVGPairType.SingleValue
                Pair.Value = value1
                Pair.Status = name
                Pair.Render_Location.Row = 1
                Pair.Render_Location.Column = 1
                Pair.Render = Enums.CAPIControlType.Button
            Case eDeviceTypes.Fan, eDeviceTypes.Hold, eDeviceTypes.Mode
                Pair = New VSPair(HomeSeerAPI.ePairStatusControl.Both)
                Pair.PairType = VSVGPairType.SingleValue
                Pair.Value = value1
                Pair.Status = name
                Pair.Render_Location.Row = 1
                Pair.Render_Location.Column = 1
                Pair.Render = Enums.CAPIControlType.Button
            Case eDeviceTypes.Cool_SetPoint, eDeviceTypes.Heat_SetPoint
                Pair = New VSPair(ePairStatusControl.Both)
                Pair.PairType = VSVGPairType.Range
                Pair.RangeStart = value1
                Pair.RangeEnd = value2
                Pair.Render_Location.Row = 1
                Pair.Render_Location.Column = 1
                Pair.Render = Enums.CAPIControlType.ValuesRange
            Case eDeviceTypes.Temperature, eDeviceTypes.Outside_Temp
                Pair = New VSPair(ePairStatusControl.Status)
                Pair.PairType = VSVGPairType.Range
                Pair.RangeStart = value1
                Pair.RangeEnd = value2
                Pair.Render_Location.Row = 1
                Pair.Render_Location.Column = 1
                Pair.RangeStatusSuffix = "°"
                Pair.Render = Enums.CAPIControlType.Values
            Case eParent ' ToDo add support for message display
                Pair = New VSPair(ePairStatusControl.Status)
                Pair.PairType = VSVGPairType.Range
                Pair.RangeStart = value1
                Pair.RangeEnd = value2
                Pair.Render_Location.Row = 1
                Pair.Render_Location.Column = 1
                Pair.RangeStatusPrefix = "Address "
                Pair.Render = Enums.CAPIControlType.Values
        End Select
        hs.DeviceVSP_AddPair(ref, Pair)
    End Sub

    Sub SetUpStatusOnly(ByRef dv As Scheduler.Classes.DeviceClass)
        'swap this to a status only
        dv.MISC_Clear(hs, Enums.dvMISC.SHOW_VALUES)
        dv.MISC_Set(hs, Enums.dvMISC.STATUS_ONLY)

        'clear out defaults
        hs.DeviceVSP_ClearAll(dv.Ref(hs), True)
        hs.DeviceVGP_ClearAll(dv.Ref(hs), True)
    End Sub

    '    Public Enum eDeviceType_Thermostat
    '        Operating_State = 1
    '        Temperature = 2
    '        Mode_Set = 3
    '        Fan_Mode_Set = 4
    '        Fan_Status = 5
    '        Setpoint = 6
    '        RunTime = 7
    '        Hold_Mode = 8
    '        Operating_Mode = 9
    '        Additional_Temperature = 10
    '        Setback = 11
    '        Filter_Remind = 12
    '        Root = 99
    '   End Enum


    Private Function CreateDevice(ByVal DeviceName As String, ByVal Address As Integer, Optional ChildDeviceType As eDeviceTypes = -1, Optional iRefID As Integer = 0) As Integer
        Dim dv As Scheduler.Classes.DeviceClass = Nothing
        Dim ref As Integer
        Dim PED As clsPlugExtraData

        If iRefID > 0 Then
            dv = hs.GetDeviceByRef(iRefID)
            If dv IsNot Nothing Then Return iRefID 'no need to create the device
        End If

        DeviceName = DeviceName.Replace("_", " ")

        Try
            ref = hs.NewDeviceRef(DeviceName)
            dv = hs.GetDeviceByRef(ref)
            dv.Name(hs) = DeviceName

            dv.Can_Dim(hs) = False
            dv.Device_Type_String(hs) = DeviceName
            Dim DT As New DeviceTypeInfo
            DT.Device_API = DeviceTypeInfo.eDeviceAPI.Thermostat
            dv.DeviceType_Set(hs) = DT
            dv.Interface(hs) = IFACE_NAME
            dv.InterfaceInstance(hs) = ""
            dv.Last_Change(hs) = Now
            dv.Location(hs) = "Not Set"
            dv.Location2(hs) = "Not Set"
            dv.Can_Dim(hs) = False
            dv.Status_Support(hs) = False
            dv.UserNote(hs) = ""
            dv.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES)
            PED = dv.PlugExtraData_Get(hs)
            PEDAdd(PED, ref.ToString, ChildDeviceType)
            dv.PlugExtraData_Set(hs) = PED
            'put device specific stuff here
            Select Case ChildDeviceType
                Case eDeviceTypes.Heat_SetPoint
                    dv.Can_Dim(hs) = True
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    AddControl(ref, "Heat Point", ChildDeviceType, SetPoint_Low, SetPoint_High)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Setpoint
                    DT.Device_SubType = DeviceTypeInfo.eDeviceSubType_Setpoint.Heating_1
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Cool_SetPoint
                    dv.Can_Dim(hs) = True
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    AddControl(ref, "Cool Point", ChildDeviceType, SetPoint_Low, SetPoint_High)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Setpoint
                    DT.Device_SubType = DeviceTypeInfo.eDeviceSubType_Setpoint.Cooling_1
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Mode
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    AddControl(ref, "Auto", ChildDeviceType, eMode.Auto)
                    AddControl(ref, "Aux", ChildDeviceType, eMode.Aux)
                    AddControl(ref, "Cool", ChildDeviceType, eMode.Cool)
                    AddControl(ref, "Heat", ChildDeviceType, eMode.Heat)
                    AddControl(ref, "Off", ChildDeviceType, eMode.Off)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Operating_Mode
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Fan
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    AddControl(ref, "Auto", ChildDeviceType, eFan.Auto)
                    AddControl(ref, "On", ChildDeviceType, eFan.FanOn)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Fan_Status
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Hold
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    AddControl(ref, "Hold", ChildDeviceType, eHold.Hold)
                    AddControl(ref, "Run", ChildDeviceType, eHold.Run)
                    AddControl(ref, "Tmp", ChildDeviceType, eHold.Tmp)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Hold_Mode
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Temperature
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    SetUpStatusOnly(dv)
                    AddControl(ref, "Temp", ChildDeviceType, Temp_Low, Temp_High)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Temperature
                    DT.Device_SubType = DeviceTypeInfo.eDeviceSubType_Temperature.Temperature
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Outside_Temp
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    SetUpStatusOnly(dv)
                    AddControl(ref, "Outside Temp", ChildDeviceType, Temp_Low, Temp_High)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Temperature
                    DT.Device_SubType = DeviceTypeInfo.eDeviceSubType_Temperature.Other_Temperature
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.RunTime
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    SetUpStatusOnly(dv)
                    AddControl(ref, "Run Time", ChildDeviceType, 0, 2147483647)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.RunTime
                    dv.DeviceType_Set(hs) = DT
                Case eDeviceTypes.Filter_Remind
                    dv.ImageLarge(hs) = "images/EVC-TStat/VStat.png"
                    dv.Image(hs) = "images/EVC-TStat/VStat_small.png"
                    'SetUpStatusOnly(dv)
                    AddControl(ref, "Reset", ChildDeviceType, True)
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Filter_Remind
                    dv.DeviceType_Set(hs) = DT
                Case Else 'parent
                    dv.ImageLarge(hs) = "images/EVC-TStat/thermostat-large.jpg"
                    dv.Image(hs) = "images/EVC-TStat/thermostat-large_small.jpg"
                    SetUpStatusOnly(dv)
                    AddControl(ref, DeviceName, eParent, eAddress.Low, eAddress.High)
                    dv.Device_Type_String(hs) = "EVC Thermostats"
                    dv.Address(hs) = Address
                    DT.Device_Type = DeviceTypeInfo.eDeviceType_Thermostat.Root
                    dv.DeviceType_Set(hs) = DT
                    RefID = ref
            End Select

            BindParentAndChild(dv, RefID, ref)

            hs.SaveEventsDevices()

            Return ref
        Catch ex As Exception
            hs.WriteLog(IFACE_NAME, "Error initializing device: " & ex.Message)
            Return -1
        End Try
    End Function

    Private Sub SendCMD(ByVal Command As String)
        ComThread.SendCommand(Command)
    End Sub
#End Region

#Region "Public Subs/Functions"
    Public Sub Poll()
        Try
            Log("In Poll: Time: " & Now(), LogLevel.Debug)
            Try
                Dim cmd As String = "A=" & Me.Address.ToString & " O=00 R=1 R=2 SC=?" & vbCr
                SendCMD(cmd)
            Catch ex As Exception
                Log("Error in Poll: " & ex.Message, LogLevel.Normal)
            End Try

        Catch ex As Exception
            Log("Error polling: " & ex.Message, LogLevel.Debug)
        End Try
    End Sub

    Public Sub ProcessDataReceived(ByVal Data As String)
        Dim Prop As String
        Dim Value As String
        Dim addr As String = "1"
        Dim zone As String = "00"

        ' OK, We can get multiple items here.  So, split them all then parse each one.
        Dim commands() As String = Split(Trim(Data))
        Try
            For Each item As String In commands
                ' Get The Property 
                Prop = Mid(item, 1, InStr(item, "=") - 1)
                Value = Mid(item, InStr(item, "=") + 1)

                Log("In ProcessCMD, Property= " & Prop & ", Value= " & Value, LogLevel.Debug)

                Try
                    Select Case Prop
                        Case "A"
                            addr = Value
                        Case "Z"
                            zone = Value
                        Case "M"
                            Select Case Value
                                Case "O"
                                    Me.Mode = eMode.Off
                                Case "A"
                                    Me.Mode = eMode.Auto
                                Case "C"
                                    Me.Mode = eMode.Cool
                                Case "H"
                                    Me.Mode = eMode.Heat
                                Case "E", "EH"
                                    Me.Mode = eMode.Aux
                            End Select
                        Case "F", "FM"
                            Select Case Value
                                Case "0"
                                    Me.Fan = eFan.Auto
                                Case "1"
                                    Me.Fan = eFan.FanOn
                            End Select
                        Case "FR"
                            'Remaining Filter Time
                            Me.FilterRemind = Value
                        Case "FT"
                            'Total Filter Time
                            Me.TotalRunTime = Value
                        Case "SPH"
                            'HeatSet
                            Me.HeatSetPoint = Value
                        Case "SPC"
                            'Coolset
                            Me.CoolSetPoint = Value
                        Case "T"
                            Me.Temperature = Value
                        Case "TM"
                            Me.Message = Value
                        Case "OA"
                            Me.OutsideTemp = Value
                        Case "SC"
                            Select Case Value
                                Case "0"
                                    Me.Hold = eHold.Run
                                Case "1"
                                    Me.Hold = eHold.Hold
                                Case "2"
                                    Me.Hold = eHold.Tmp
                            End Select
                        Case Else
                            CheckTriggers(addr, zone, Prop, Value)
                    End Select
                Catch ex As Exception
                    Log("Error in ProcessCMD Select Block, Selecting Properties, " & ex.Message)
                End Try
            Next
        Catch ex As Exception
            Log("Error in ProcessCMD, Selecting Properties, " & ex.Message)
        End Try
    End Sub
#End Region
End Class
