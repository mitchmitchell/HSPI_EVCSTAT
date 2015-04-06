<%@ Page Language="VB" Debug="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<script runat="server">
    Dim i As Integer
    Dim hs As Scheduler.hsapplication
    Dim pi As HomeSeerAPI.PluginAccess
    Dim s As String = ""
    Dim j As Integer
    Dim a As System.Reflection.Assembly
    
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            hs = Context.Items("Content")
            pi = New HomeSeerAPI.PluginAccess(hs, "RCS Serial Thermostats", "")
            
            If Not IsPostBack Then
                GetData()
            Else
            End If
                    
        Catch ex As Exception
            Response.Write("Error In Load: " & ex.Message)
        End Try
    End Sub
    
    ' get the info for all the thermostats and set as the data source for the datalist control
    Private Sub GetData()
        Dim values As New ArrayList()
        Dim stat As Object
        
        Try
            If pi.StatCount = 0 Then
                LabelNoStats.Text = "No thermostats are configured"
                Return
            End If
            
            For i = 1 To pi.StatCount
                stat = pi.StatObj(i)
                Dim sd As New StatData(stat, pi)
                If sd.LastError <> "" Then
                    hs.WriteLog("RCS Serial Thermostats", "Error getting thermostat data: " & sd.LastError)
                End If
                values.Add(sd)
            Next
            DataList1.DataSource = values
            DataList1.DataBind()
            
        Catch ex As Exception
            Response.Write("<br>Error accessing thermostats: " & ex.Message)
        End Try
    End Sub
    
    Public Class StatData
                
        Public _Name As String
        Public _HeatSetPoint As String
        Public _CoolSetPoint As String
        Public _CoolSetPointVisible As Boolean
        Public _Temp As String
        Public _Fan As String
        Public _Mode As String
        Public _Hold As String
        Public _HoldVisible As Boolean
        Public _HoldOverrideVisible As Boolean
        Public _Number As String
        Public _SysVisible As Boolean
        Public _Zones As New ArrayList()
        Public LastError As String
                             
        Public _OAVisible As Boolean
        
        Public Sub New(ByVal stat As HSPI_RCSSERIAL.Thermostat, ByVal cb As HSPI_RCSSERIAL.HSPI)
            Try
                                
                LastError = ""
                _Number = stat.Number
                _Name = stat.Location & " " & stat.Name
                _Temp = CStr(stat.Temperature) & "°" & cb.ConfigTemperatureScale.ToString
                _HeatSetPoint = CStr(stat.HeatSet)
                _CoolSetPoint = CStr(stat.CoolSet)
                _CoolSetPointVisible = stat.SupportsCoolSet(_Number, 1)
                _Fan = stat.FanMode.ToString
                If stat.ModeOperating.FA Then
                    _Fan &= " (On)"
                Else
                    _Fan &= " (Off)"
                End If
                _Mode = stat.Mode.ToString
                Dim alloff As Boolean = True
                If stat.ModeOperating.H1A Then
                    _Mode &= " (First Stage Heat)"
                    alloff = False
                End If
                If stat.ModeOperating.H2A Then
                    _Mode &= " (Second Stage Heat)"
                    alloff = False
                End If
                If stat.ModeOperating.H3A Then
                    _Mode &= " (Third Stage Heat)"
                    alloff = False
                End If
                If stat.ModeOperating.C1A Then
                    _Mode &= " (First Stage Cool)"
                    alloff = False
                End If
                If stat.ModeOperating.C2A Then
                    _Mode &= " (Second Stage Cool)"
                    alloff = False
                End If
                If alloff Then
                    _Mode &= " (Idle)"
                End If
                _Hold = stat.Hold.ToString
                _HoldVisible = stat.SupportsHold(_Number, 1)
                _HoldOverrideVisible = stat.SupportsHoldOverride(_Number, 1)
                If stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZC6R_Zone_Controller Or stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZCV_Zone_Controller Then
                    _SysVisible = True
                Else
                    _SysVisible = False
                End If
                If stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZC6R_Zone_Controller Or stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZCV_Zone_Controller Then
                    For i As Integer = 0 To stat.Zones.Count - 1
                        _Zones.Add(New ZoneData(stat.Zones(i), cb, _Number))
                    Next
                Else
                    _Zones.Add(New ZoneData(stat, cb, _Number))
                End If
                If stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.Sensor Then
                    _OAVisible = True
                Else
                    _OAVisible = False
                End If
            Catch ex As Exception
                LastError = "Error getting thermostat data: " & ex.Message
            End Try
            
        End Sub
        
        Public ReadOnly Property Name()
            Get
                Return _Name
            End Get
        End Property
        
        Public ReadOnly Property Temp()
            Get
                Return _Temp
            End Get
        End Property
        
        Public ReadOnly Property HeatSetPoint()
            Get
                Return _HeatSetPoint
            End Get
        End Property
        
        Public ReadOnly Property CoolSetPoint()
            Get
                Return _CoolSetPoint
            End Get
        End Property
        
        Public ReadOnly Property CoolSetPointVisible()
            Get
                Return _CoolSetPointVisible
            End Get
        End Property
        
        Public ReadOnly Property Fan()
            Get
                Return _Fan
            End Get
        End Property
        
        Public ReadOnly Property Mode()
            Get
                Return _Mode
            End Get
        End Property
        
        Public ReadOnly Property Hold()
            Get
                Return _Hold
            End Get
        End Property
        
        Public ReadOnly Property HoldVisible()
            Get
                Return _HoldVisible
            End Get
        End Property
        
        Public ReadOnly Property HoldOverrideVisible()
            Get
                Return _HoldOverrideVisible
            End Get
        End Property
        
        Public ReadOnly Property Number()
            Get
                Return _Number
            End Get
        End Property
        
        Public ReadOnly Property SysVisible()
            Get
                Return _SysVisible
            End Get
        End Property
               
        Public ReadOnly Property Zones()
            Get
                Return _Zones
            End Get
        End Property
        
        Public ReadOnly Property OAVisible()
            Get
                Return _OAVisible
            End Get
        End Property
        
    End Class
    
    Public Class ZoneData
        Public _RS As String
        Public _Name As String
        Public _HeatSetPoint As String
        Public _CoolSetPoint As String
        Public _CoolSetPointVisible As Boolean
        Public _Temp As String
        Public _Number As String
        Public _ThermID As String
        Public _Mode As String
        Public LastError As String
        Public _Fan As String
        Public _SysVisible As Boolean
        Public _Hold As String
        Public _HoldVisible As Boolean
        Public _HoldOverrideVisible As Boolean
        
        Public _OAVisible As Boolean
                             
        Public Sub New(ByVal stat As HSPI_RCSSERIAL.Thermostat, ByVal cb As HSPI_RCSSERIAL.HSPI, ByVal ThermID As Integer)
            Try
                                
                LastError = ""
                _Number = stat.Number
                _ThermID = ThermID
                _Name = stat.Location & " " & stat.Name
                _Temp = CStr(stat.Temperature) & "°" & cb.ConfigTemperatureScale.ToString
                ' -100 is OUTSIDE_UNINITIALIZED
                _RS = IIf(stat.OutSideAir <> -100, "Outside Temperature: " & CStr(stat.OutSideAir) & "°" & cb.ConfigTemperatureScale.ToString, " No Outside Temp ")
                _HeatSetPoint = CStr(stat.HeatSet)
                _CoolSetPoint = CStr(stat.CoolSet)
                _CoolSetPointVisible = stat.SupportsCoolSet(_Number, 1)
                _Fan = stat.FanMode.ToString
                _Mode = stat.Mode.ToString
                If stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.Stand_Alone Then
                    If stat.ModeOperating.FA Then
                        _Fan &= " (On)"
                    Else
                        _Fan &= " (Off)"
                    End If
                    Dim alloff As Boolean = True
                    If stat.ModeOperating.H1A Then
                        _Mode &= " (First Stage Heat)"
                        alloff = False
                    End If
                    If stat.ModeOperating.H2A Then
                        _Mode &= " (Second Stage Heat)"
                        alloff = False
                    End If
                    If stat.ModeOperating.H3A Then
                        _Mode &= " (Third Stage Heat)"
                        alloff = False
                    End If
                    If stat.ModeOperating.C1A Then
                        _Mode &= " (First Stage Cool)"
                        alloff = False
                    End If
                    If stat.ModeOperating.C2A Then
                        _Mode &= " (Second Stage Cool)"
                        alloff = False
                    End If
                    If alloff Then
                        _Mode &= " (Idle)"
                    End If
                Else ' Zones
                    Select Case stat.Damper
                        Case False
                            _Mode &= " (Damper Closed)"
                        Case True
                            _Mode &= " (Damper Open)"
                    End Select
                End If
                _Hold = stat.Hold.ToString
                _HoldVisible = stat.SupportsHold(_Number, 1)
                If stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZC6R_Zone Or stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZCV_Zone Then
                    _SysVisible = True
                Else
                    _SysVisible = False
                End If
                If stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.Sensor Then
                    _OAVisible = True
                Else
                    _OAVisible = False
                End If
            Catch ex As Exception
                LastError = "Error getting thermostat data: " & ex.Message
            End Try
            
        End Sub

        Public ReadOnly Property RS()
            Get
                Return _RS
            End Get
        End Property
        
        Public ReadOnly Property SysVisible()
            Get
                Return _SysVisible
            End Get
        End Property
        
        Public ReadOnly Property Fan()
            Get
                Return _Fan
            End Get
        End Property
        
        Public ReadOnly Property Mode()
            Get
                Return _Mode
            End Get
        End Property
        
        Public ReadOnly Property Name()
            Get
                Return _Name
            End Get
        End Property
        
        Public ReadOnly Property Temp()
            Get
                Return _Temp
            End Get
        End Property
        
        Public ReadOnly Property HeatSetPoint()
            Get
                Return _HeatSetPoint
            End Get
        End Property
        
        Public ReadOnly Property CoolSetPoint()
            Get
                Return _CoolSetPoint
            End Get
        End Property
        
        Public ReadOnly Property CoolSetPointVisible()
            Get
                Return _CoolSetPointVisible
            End Get
        End Property
                
        Public ReadOnly Property Number()
            Get
                Return _Number
            End Get
        End Property
        
        
        
        Public ReadOnly Property ThermID()
            Get
                Return _ThermID
            End Get
        End Property
        
        Public ReadOnly Property Hold()
            Get
                Return _Hold
            End Get
        End Property
        
        Public ReadOnly Property HoldVisible()
            Get
                Return _HoldVisible
            End Get
        End Property
        
        Public ReadOnly Property OAVisible()
            Get
                Return _OAVisible
            End Get
        End Property
    End Class
             
    Private Function GetHeadContent() As String
        Try
            Return hs.GetPageHeader("RCS Serial Status", "", "", False, False, True, False, False)
        Catch ex As Exception
        End Try
        Return ""
    End Function
    
    Private Function GetBodyContent() As String
        Try
            Return hs.GetPageHeader("RCS Serial Status", "", "", False, False, False, True, False)
        Catch ex As Exception
        End Try
        Return ""
    End Function
    
    Private Function GetFooterContent() As String
        Try
            Return hs.GetPageFooter(False)
        Catch ex As Exception
        End Try
        Return ""
    End Function
    
    ' when a button is clicked on one of the thermostat forms, this function is called
    ' make sure the CommandName property of the control has been set
    Protected Sub DataList1_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataListCommandEventArgs)
        Dim s As String
        Dim ThermNum As Integer = CInt(CType(e.Item.FindControl("LabelNumber"), Label).Text)
        Dim cmd As String = e.CommandName
        Select Case cmd
            Case "SetHeatSetpoint"
                s = CType(e.Item.FindControl("TextBoxHeatSet"), TextBox).Text
                pi.CmdSetHeat(ThermNum, CDbl(s))
            Case "SetCoolSetpoint"
                s = CType(e.Item.FindControl("TextBoxCoolSet"), TextBox).Text
                pi.CmdSetCool(ThermNum, CDbl(s))
                ' Fan
                'Modes: 0=Auto, 1=On 
            Case "FanOn"
                pi.CmdSetFan(ThermNum, 1)
            Case "FanAuto"
                pi.CmdSetFan(ThermNum, 0)
                ' Mode
                'Modes: 0=Off, 1=Heat, 2=Cool, 3=Auto, 4=Aux (Aux only if supported)
            Case "Auto"
                pi.CmdSetMode(ThermNum, 3)
            Case "Heat"
                pi.CmdSetMode(ThermNum, 1)
            Case "Cool"
                pi.CmdSetMode(ThermNum, 2)
            Case "Off"
                pi.CmdSetMode(ThermNum, 0)
            Case "Aux"
                pi.CmdSetMode(ThermNum, 4)
                ' Hold
                'Modes: 0=Off Hold, 1=Hold (If hold is supported.)
            Case "Hold"
                pi.CmdSetHold(ThermNum, 1)
            Case "Run"
                pi.CmdSetHold(ThermNum, 0)
            Case "HoldOver"
                pi.CmdSetHold(ThermNum, 2)
        End Select
        GetData()
    End Sub
    
    Protected Sub DataList2_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataListCommandEventArgs)
        Dim s As String
        Dim ThermNum As Integer = CInt(CType(e.Item.FindControl("LabelNumber"), Label).Text)
        Dim ZoneNum As Integer = CInt(CType(e.Item.FindControl("LabelNumberZone"), Label).Text)
        Dim cmd As String = e.CommandName
        Select Case cmd
            Case "SetHeatSetpoint"
                s = CType(e.Item.FindControl("TextBoxHeatSet"), TextBox).Text
                pi.CmdSetHeat(ThermNum, CDbl(s), ZoneNum)
            Case "SetCoolSetpoint"
                s = CType(e.Item.FindControl("TextBoxCoolSet"), TextBox).Text
                pi.CmdSetCool(ThermNum, CDbl(s), ZoneNum)
                ' Fan
                'Modes: 0=Auto, 1=On 
            Case "FanOn"
                pi.CmdSetFan(ThermNum, 1, ZoneNum)
            Case "FanAuto"
                pi.CmdSetFan(ThermNum, 0, ZoneNum)
                ' Mode
                'Modes: 0=Off, 1=Heat, 2=Cool, 3=Auto, 4=Aux (Aux only if supported)
            Case "Auto"
                pi.CmdSetMode(ThermNum, 3, ZoneNum)
            Case "Heat"
                pi.CmdSetMode(ThermNum, 1, ZoneNum)
            Case "Cool"
                pi.CmdSetMode(ThermNum, 2, ZoneNum)
            Case "Off"
                pi.CmdSetMode(ThermNum, 0, ZoneNum)
            Case "Aux"
                pi.CmdSetMode(ThermNum, 4, ZoneNum)
                ' Hold
                'Modes: 0=Off Hold, 1=Hold (If hold is supported.)
            Case "Hold"
                pi.CmdSetHold(ThermNum, 1, ZoneNum)
            Case "Run"
                pi.CmdSetHold(ThermNum, 0, ZoneNum)
            Case "HoldOver"
                pi.CmdSetHold(ThermNum, 2, ZoneNum)
        End Select
        GetData()
        
    End Sub

    Protected Sub DataList1_SelectedIndexChanged(sender As Object, e As System.EventArgs)

    End Sub
</script>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>RCS Serial Status</title>
    <%Response.Write(GetHeadContent())%>
</head>
<body>
    <% Response.Write(GetBodyContent())%>
    <form id="form1" runat="server">
        <div>
                <table border="0" style="width: 50%">
                    <tr>
                        <td style="width: 100px" align="left">
                            <a href="RCSSerial-Config.aspx">Configure&nbsp;Thermostats</a>
            <asp:DataList ID="DataList1" runat="server" OnItemCommand="DataList1_ItemCommand" 
                                onselectedindexchanged="DataList1_SelectedIndexChanged" 
                                style="margin-right: 327px; margin-top: 0px">
                <ItemTemplate>
                    <table style="border: solid thin gray; background-color: #f5f5f5; border-right: thin solid black;
                        border-top: thin solid black; border-left: black thin solid; border-bottom: black thin solid;"
                        cellpadding="0" cellspacing="0" id="Table1">
                        <tr>
                            <td rowspan="1" style="border-top-width: thin; border-left-width: thin; vertical-align: middle;
                                border-top-color: black; border-bottom: black thin solid; background-color: lightskyblue;
                                text-align: center; border-right-width: thin; border-right-color: black;">
                                <asp:Label ID="LabelStatName" runat="server" Font-Bold="True" Text='<%# DataBinder.Eval(Container.DataItem, "Name") %>'
                                    Font-Size="20pt"></asp:Label>&nbsp;&nbsp;</td>
                        </tr>
                        <tr>
                            <td rowspan="1" style="border-top-width: thin; border-left-width: thin;
                                vertical-align: middle; border-top-color: black; text-align: center; border-right-width: thin;
                                border-right-color: black;">
                                &nbsp;
                                <asp:Panel ID="PanelSysSettings" runat="server" Style="font-size: 100%" 
                                    Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' 
                                    Height="90px" Width="637px">
                                    <table cellpadding="0" cellspacing="0" style="border-right: black thin solid; border-top: black thin solid;
                                        border-left: black thin solid; border-bottom: black thin solid; width: 90%; text-align: left;
                                        margin-top: 10px; margin-bottom: 10px;" id="TABLE2">
                                        <tr>
                                            <td colspan="3" style="background-color: lightskyblue; text-align: center; border-bottom: black thin solid;">
                                                <asp:Label ID="Label1" runat="server" Font-Bold="True" Font-Size="14pt" Text="System Settings"
                                                    Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'></asp:Label></td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Label ID="LabelSystemMode" runat="server" Text="System Mode:" Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'
                                                    Font-Bold="True"></asp:Label></td>
                                            <td>
                                                <asp:Label ID="LabelMode" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Mode") %>'
                                                    Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'></asp:Label></td>
                                            <td>
                                                <asp:Button ID="ButtonModeAuto" runat="server" Text="Auto" CssClass="formbutton"
                                                    CommandName="Auto" Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' /><asp:Button
                                                        ID="ButtonModeHeat" runat="server" Text="Heat" CssClass="formbutton" CommandName="Heat"
                                                        Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' /><asp:Button
                                                            ID="ButtonCool" runat="server" Text="Cool" CssClass="formbutton" CommandName="Cool"
                                                            Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' /><asp:Button
                                                                ID="ButtonModeAux" runat="server" Text="Aux" Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'
                                                                CommandName="Aux" CssClass="formbutton" /><asp:Button ID="ButtonModeOff" runat="server"
                                                                    Text="Off" CssClass="formbutton" CommandName="Off" Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' /></td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Label ID="LabelSystemFan" runat="server" Text="System Fan Mode:" Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'
                                                    Font-Bold="True"></asp:Label></td>
                                            <td>
                                                <asp:Label ID="LabelFan" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Fan") %>'
                                                    Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'></asp:Label></td>
                                            <td>
                                                <asp:Button ID="ButtonFanOn" runat="server" Text="Fan On" CssClass="formbutton" CommandName="FanOn"
                                                    Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' /><asp:Button
                                                        ID="ButtonFanAuto" runat="server" Text="Fan Auto" CssClass="formbutton" CommandName="FanAuto"
                                                        Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>' /></td>
                                        </tr>
                                    </table>
                                </asp:Panel>
                                <asp:Panel ID="PanelSensorTempOnly" runat="server" Visible='<%# DataBinder.Eval(Container.DataItem, "OAVisible") %>'>
                                    <table cellpadding="0" cellspacing="0" style="border: solid thin gray; border-right: black thin solid;
                                        border-top: black thin solid; border-left: black thin solid; width: 90%; border-bottom: black thin solid;
                                        text-align: left; margin-top: 10px; margin-bottom: 10px;">
                                        <tr style="font-weight: bold; font-size: 12pt">
                                            <td style="border-right: black thin solid; width: 99px; background-color: silver;
                                                text-align: center">
                                                <asp:Label ID="LabelMainTemp" runat="server" Font-Bold="True" Font-Italic="False"
                                                    Font-Size="26pt" Text='<%# DataBinder.Eval(Container.DataItem, "Temp") %>'></asp:Label>
                                            </td>
                                        </tr>
                                    </table>
                                </asp:Panel>
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align: center">
                                <asp:DataList ID="DataList2" runat="server" DataSource='<%# DataBinder.Eval(Container.DataItem, "Zones") %>'
                                    OnItemCommand="DataList2_ItemCommand" 
                                    Visible='<%# Not DataBinder.Eval(Container.DataItem, "OAVisible") %>' 
                                    style="margin-top: 45px">
                                    <ItemTemplate>
                                        <table cellpadding="0" cellspacing="0" style="border: solid thin gray; border-right: black thin solid;
                                            border-top: black thin solid; border-left: black thin solid; width: 90%; border-bottom: black thin solid;
                                            text-align: left; margin-top: 10px; margin-bottom: 10px;">
                                            <tr>
                                                <td colspan="4" rowspan="1" style="background-color: lightskyblue; text-align: center;
                                                    border-bottom: black thin solid;">
                                                    <asp:Label ID="LabelStatName" runat="server" Font-Bold="True" Font-Size="14pt" Text='<%# DataBinder.Eval(Container.DataItem, "Name") %>'
                                                        Visible='<%# DataBinder.Eval(Container.DataItem, "SysVisible") %>'></asp:Label>
                                                    <asp:Label ID="LabelNumberZone" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Number") %>'
                                                        Visible="False"></asp:Label>
                                                    <asp:Label ID="LabelNumber" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "ThermID") %>'
                                                        Visible="False"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td rowspan="3" style="border-right: black thin solid; width: 150px; background-color: silver;
                                                    text-align: center">
                                                    <asp:Label Text='<%# DataBinder.Eval(Container.DataItem, "Temp") %>' ID="LabelMainTemp"
                                                        runat="server" Font-Bold="True" Font-Size="26pt" Font-Italic="False"></asp:Label></td>
                                                <td>
                                                    <strong>Heat</strong> <strong>Setpoint:</strong></td>
                                                <td>
                                                    <asp:TextBox Text='<%# DataBinder.Eval(Container.DataItem, "HeatSetPoint") %>' ID="TextBoxHeatSet"
                                                        runat="server" Width="64px"></asp:TextBox></td>
                                                <td>
                                                    <asp:Button ID="ButtonSetHeat" runat="server" Text="Set Heat Setpoint" CssClass="formbutton"
                                                        CommandName="SetHeatSetpoint" /></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="LabelCoolSetText" runat="server" Text="Cool Setpoint:" Visible='<%# DataBinder.Eval(Container.DataItem, "CoolSetPointVisible") %>'
                                                        Font-Bold="True" />
                                                </td>
                                                <td>
                                                    <asp:TextBox Text='<%# DataBinder.Eval(Container.DataItem, "CoolSetPoint") %>' ID="TextBoxCoolSet"
                                                        runat="server" Width="64px" Visible='<%# DataBinder.Eval(Container.DataItem, "CoolSetPointVisible") %>' />
                                                </td>
                                                <td>
                                                    <asp:Button ID="ButtonSetCool" runat="server" Text="Set Cool Setpoint" CssClass="formbutton"
                                                        EnableTheming="False" CommandName="SetCoolSetpoint" Visible='<%# DataBinder.Eval(Container.DataItem, "CoolSetPointVisible") %>' /></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <strong>Mode:</strong></td>
                                                <td>
                                                    <asp:Label ID="LabelMode" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Mode") %>' />
                                                </td>
                                                <td>
                                                    <asp:Button ID="ButtonModeAuto" runat="server" Text="Auto" CssClass="formbutton"
                                                        CommandName="Auto" />
                                                    <asp:Button ID="ButtonModeHeat" runat="server" Text="Heat" CssClass="formbutton"
                                                        CommandName="Heat" />
                                                    <asp:Button ID="ButtonCool" runat="server" Text="Cool" CssClass="formbutton" CommandName="Cool" />
                                                    <asp:Button ID="ButtonModeAux" runat="server" Text="Aux" CommandName="Aux" CssClass="formbutton" />
                                                    <asp:Button ID="ButtonModeOff" runat="server" Text="Off" CssClass="formbutton" CommandName="Off" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td rowspan="2" style="border-right: black thin solid; border-top: black thin solid;
                                                    background-color: gainsboro">
                                                    <asp:Label ID="LabelRS" runat="server" Font-Bold="True" Font-Italic="False" Font-Size="12pt"
                                                        Text='<%# DataBinder.Eval(Container.DataItem, "RS") %>' /></td>
                                                <td>
                                                    <strong>Fan Mode:</strong>
                                                </td>
                                                <td>
                                                    <asp:Label ID="LabelFan" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Fan") %>' />
                                                </td>
                                                <td>
                                                    <asp:Button ID="ButtonFanOn" runat="server" Text="Fan On" CssClass="formbutton" CommandName="FanOn" />
                                                    <asp:Button ID="ButtonFanAuto" runat="server" Text="Fan Auto" CssClass="formbutton"
                                                        CommandName="FanAuto" /></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <asp:Label ID="LabelHoldText" runat="server" Font-Bold="True" Text="Hold:" Visible='<%# DataBinder.Eval(Container.DataItem, "HoldVisible") %>' />
                                                </td>
                                                <td>
                                                    <asp:Label ID="LabelHold" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Hold") %>'
                                                        Visible='<%# DataBinder.Eval(Container.DataItem, "HoldVisible") %>' /></td>
                                                <td>
                                                    <asp:Button ID="ButHoldHold" runat="server" CssClass="formbutton" Text="Hold" CommandName="Hold"
                                                        Visible='<%# DataBinder.Eval(Container.DataItem, "HoldVisible") %>' />
                                                    <asp:Button ID="ButHoldNormal" runat="server" CssClass="formbutton" Text="Run"
                                                        CommandName="Run" Visible='<%# DataBinder.Eval(Container.DataItem, "HoldVisible") %>' />
                                                </td>
                                            </tr>
                                        </table>
                                    </ItemTemplate>
                                </asp:DataList>
                            </td>
                        </tr>
                        <tr>
                            <td style="background-color: gainsboro; text-align: center; border-top: thin solid black;">
                                <asp:Label ID="LabelStatus" runat="server" Style="color: green" Text=''></asp:Label>
                                <asp:Label ID="LabelNumber" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Number") %>'
                                    Visible="False"></asp:Label></td>
                        </tr>
                    </table>
                </ItemTemplate>
            </asp:DataList>
                        </td>
                        <td align="right">
                            <a href="RCS Help File\RCSSerial-Help.htm">Help&nbsp;Page</a>
                        </td>
                    </tr>
                </table>
            <asp:Label ID="LabelNoStats" runat="server" Font-Bold="True"></asp:Label>
            </div>
    </form>
    <% Response.Write(Me.GetFooterContent())%>
</body>
</html>
