<%--<%@ Page Language="VB" Debug="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<script runat="server">
    
    Dim i As Integer
    Dim hs As Object
    Dim pi As HSPI_RCSSERIAL.HSPI
    Dim s As String = ""
    Dim j As Integer
    Dim poll As Boolean = True
    
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            hs = Context.Items("Content")
            pi = hs.Plugin("RCS Serial Thermostats")

            If Not IsPostBack Then
                LoadItems()
            End If
            Me.MultiViewEditStat.ActiveViewIndex = 1
           
            
        Catch ex As Exception
            Response.Write("Error In Load: " & ex.Message)
        End Try
        
    End Sub
    
    Private Sub LoadItems()
        Try
            

            Do While ListBoxStats.Items.Count > 0
                ListBoxStats.Items.RemoveAt(0)
            Loop
            For i = 1 To pi.StatCount
                ListBoxStats.Items.Add(pi.StatName(i))
            Next
            Select Case pi.ConfigTemperatureScale
                Case HSPI_RCSSERIAL.HSPI.TS.C
                    Me.DropDownList1.SelectedIndex = 1
                Case HSPI_RCSSERIAL.HSPI.TS.F
                    Me.DropDownList1.SelectedIndex = 0
            End Select
            Select Case pi.BaudRate
                Case 9600
                    Me.DropDownList2.SelectedIndex = 0
                Case 19200
                    Me.DropDownList2.SelectedIndex = 1
            End Select
            TextBoxPoll.Text = CStr(pi.ConfigPollInterval)
            CheckBoxDebug.Checked = CBool(pi.gDebug)
            CheckBoxRevDampers.Checked = CBool(pi.ConfigReverseDampers)
           
            Me.DropDownListEditStatID.Items.Clear()
            Me.DropDownListNewStatID.Items.Clear()
            For i = 1 To 255
                Me.DropDownListEditStatID.Items.Add(i)
                Me.DropDownListNewStatID.Items.Add(i)
            Next
                       
        Catch ex As Exception
            Response.Write("Error in LoadItems: " & ex.Message)
        End Try
    End Sub
    
    Private Function GetHeadContent() As String
        Try
            Return hs.GetPageHeader("RCS Serial Configuration", "", "", False, False, True, False, False)
        Catch ex As Exception
        End Try
        Return ""
    End Function
    
    Private Function GetBodyContent() As String
        Try
            Return hs.GetPageHeader("RCS Serial Configuration", "", "", False, False, False, True, False)
        Catch ex As Exception
        End Try
        Return ""
    End Function
    
    Protected Sub ButSave_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            pi.ConfigPollInterval = CInt(TextBoxPoll.Text)
            Select Case Me.DropDownList1.SelectedItem.Value.ToString
                Case "Farenheight"
                    pi.ConfigTemperatureScale = 0
                Case "Celcius"
                    pi.ConfigTemperatureScale = 1
            End Select
            pi.gDebug = Me.CheckBoxDebug.Checked
            pi.BaudRate = Me.DropDownList2.SelectedValue
            pi.ConfigReverseDampers = Me.CheckBoxRevDampers.Checked
            pi.SaveAllValues()
        Catch ex As Exception
            Response.Write("Error In Save: " & ex.Message)
        End Try
    End Sub
    

    Protected Sub ButEditStat_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            If ListBoxStats.SelectedIndex = -1 Then Exit Sub
            For i = 1 To ListBoxStats.Items.Count
                If ListBoxStats.Items(i - 1).Selected Then
                    s = ListBoxStats.Items(i - 1).Text
                    Exit For
                End If
            Next
            Me.ButEditStat.Visible = False
        Catch ex As Exception
            Response.Write("Error In Edit-1, value of i= : " & i.ToString & "LB" & ListBoxStats.Items.Count.ToString & ex.Message)
        End Try
        
        Try
            For j = 1 To pi.StatCount
                If pi.StatName(j) = s Then
                    ShowInfoForThisStat(pi.StatObj(j))
                End If
            Next
        Catch ex As Exception
            Response.Write("Error In Edit-2: " & ex.Message)
        End Try
    End Sub
        
    Protected Sub ShowInfoForThisStat(ByVal Stat As HSPI_RCSSERIAL.Thermostat)
        Try
    
        ' Set up the right view
        MultiViewEditStat.ActiveViewIndex = 0
        
        ' Load info from stat into the forms
        TextBoxName.Text = Stat.Name
        TextBoxLocation.Text = Stat.Location
        Me.DropDownListEditStatID.SelectedValue = Stat.Address
        Me.DropDownListEditStatType.SelectedValue = Replace(Stat.Type.ToString, "_", " ")
            Me.LabelStatID.Text = Stat.Number
            If Stat.SingleSetPoint Then
                Me.CheckSingleSetpoint.Checked = True
            Else
                Me.CheckSingleSetpoint.Checked = False
            End If
        
            ' This is the "Secret" property on the page
            LabelThermID.Text = Stat.Number
                
            If Stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZC6R_Zone_Controller Or _
            Stat.Type = HSPI_RCSSERIAL.Thermostat.StatTypes.ZCV_Zone_Controller Then
        
                ' Set up the view for Zone Controllers
        
                ' First, clear and add zones to the list
                Me.ListBoxZones.Items.Clear()
                Me.ListBoxZones.Items.Add("Add Zone...")
                Dim Zones As Generic.List(Of HSPI_RCSSERIAL.Thermostat) = Stat.Zones
                For i = 0 To Zones.Count - 1
                    Me.ListBoxZones.Items.Add(Zones(i).Name)
                Next
            
                ' Show Edit Zone and the list
                Me.ButtonEditZone.Visible = True
                Me.ListBoxZones.Visible = True
            Else
                ' hide everything, standalone
                Me.ButtonEditZone.Visible = False
                Me.ListBoxZones.Visible = False
            End If
            ' Hide all the zone editing info
            Me.LabelZName.Visible = False
            Me.LabelZones.Visible = False
            Me.LabelZID.Visible = False
            Me.DropDownListZoneID.Visible = False
            Me.TextBoxZoneName.Visible = False
            Me.ButtonDeletZone.Visible = False
            Me.ButtonUpdateZone.Visible = False
        
        Catch Ex As Exception
            Response.Write("Error in ShowInfoForThisStat: " & Ex.Message)
        End Try
    End Sub
        
    Protected Sub ButUpdateStat_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            pi.UpdateStatObj(CInt(LabelThermID.Text), TextBoxName.Text & vbTab & TextBoxLocation.Text & vbTab & Me.DropDownListEditStatID.SelectedValue & vbTab & Me.DropDownListEditStatType.Text & vbTab & Me.CheckSingleSetpoint.Checked.ToString, poll)
            Me.ButEditStat.Visible = True
            LoadItems()
        Catch ex As Exception
            Response.Write("Error in UpdateStat: " & ex.Message & ex.TargetSite.ToString & ex.StackTrace)
        End Try
    End Sub
    
    Protected Sub ButAddStat_Click(ByVal sender As Object, ByVal e As System.EventArgs)
       
        Try
                    
            ' Pretty much this whole sub is a hack, so make sure we have a stat to add
            If Me.TextBoxNew.Text = "" Then Exit Sub
                
            'Create the thermostat back in the plugin (index & name)
            pi.CreateThermostat_Old(pi.StatCount + 1, TextBoxNew.Text & vbTab & vbTab & CStr(pi.StatCount + 1) & vbTab & vbTab & vbTab & vbTab & CStr(Me.DropDownListNewStatID.SelectedValue) & vbTab & Me.DropDownListNewStatType.Text, True)
	    
            'Make sure our local index is accurate
            LabelThermID.Text = pi.StatCount + 1
        
            'Increment the counter in the plugin
            pi.StatCount += 1
        
            'Grab the data for the new stat
            TextBoxName.Text = TextBoxNew.Text
            DropDownListEditStatType.SelectedValue = DropDownListNewStatType.SelectedValue
            DropDownListEditStatID.SelectedValue = DropDownListNewStatID.SelectedValue
        
            ' Clear out the old stat
            TextBoxNew.Text = ""
   
            ' Load Default info into Text Fields
            LoadDefaultStatVals()
        
            ' Fake an "Update Stat" click so that the latest info is saved in the plugin
            ' i dont think this is necessary either, but w/e
            poll = False
            Me.ButUpdateStat_Click(Nothing, Nothing)
            poll = True
        
            ' Hack so that our new stat is now selected
            ListBoxStats.SelectedIndex = ListBoxStats.Items.Count - 1
        
            'Fake an "Edit Stat" click to show the info.
            Me.ButEditStat_Click(Nothing, Nothing)

        Catch ex As Exception
            Response.Write("Error in ButAddStat_Click: " & ex.Message)
        End Try
        
    End Sub
    
    Private Sub LoadDefaultStatVals()
        Me.TextBoxLocation.Text = "" ' Do we even need this?
    End Sub

    Protected Sub ButDeleteStat_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        
        Try
            
            pi.RemoveThermostat(ListBoxStats.SelectedIndex + 1)
            Me.ButEditStat.Visible = True
            LoadItems()
            
        Catch ex As Exception
            Response.Write("Error in ButDeleteStat_Click: " & ex.Message)
        End Try
        
    End Sub

    Protected Sub ButtonEditZone_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        
        Try
            
            'Make sure we are on the right view
            MultiViewEditStat.ActiveViewIndex = 0
            
            ' Get the Zones collection for the thermostat object
            Dim Zones As Generic.List(Of HSPI_RCSSERIAL.Thermostat)
            Zones = pi.StatObj(CInt(Me.LabelThermID.Text)).Zones
            
            
            ' Handle adding a new zone, "7" is the max value here.
            If Me.ListBoxZones.SelectedItem.Text = "Add Zone..." Then
                ' New Zone!
                Me.LabelZoneID.Text = 7
                ShowInfoForThisZone(Nothing, 7)
            else
                ' See if we can find the zone that exists
                For i As Integer = 0 To Zones.Count - 1
                    If Zones(i).Name = Me.ListBoxZones.SelectedItem.Text Then
                        ' This is our Zone
                        ShowInfoForThisZone(Zones(i), Zones(i).Number)
                    End If
                Next
            end if
            
            Me.ButtonEditZone.Visible = False
            
        Catch ex As Exception
            Response.Write("Error in ButtonEditZone_Click: " & ex.Message)
        End Try
        
    End Sub
    
    Protected Sub ShowInfoForThisZone(ByVal Stat As HSPI_RCSSERIAL.Thermostat, ByVal Zone As Integer)
        
        Try
            ' Set the correct view
            MultiViewEditStat.ActiveViewIndex = 0
            
            'Check for new zone, it passes nothing for stat
            If Zone <> 7 Then
                If Stat IsNot Nothing Then
                    Me.TextBoxZoneName.Text = Stat.Name
                    For i As Integer = 0 To Me.DropDownListZoneID.Items.Count - 1
                        If Me.DropDownListZoneID.Items(i).Text = Stat.Name Then
                            Me.DropDownListZoneID.SelectedIndex = i
                            Exit For
                        End If
                    Next
                    'Try
                    '    Me.DropDownListZoneID.SelectedIndex = Stat.Address - 1
                    'Catch Ex As Exception
                    '    Me.DropDownListZoneID.SelectedIndex = 0
                    'End Try
                    Me.LabelZoneID.Text = Zone
                End If
            End If
        
            ' Show Zone editing area
            Me.DropDownListZoneID.Visible = True
            Me.LabelZName.Visible = True
            Me.LabelZones.Visible = True
            Me.LabelZID.Visible = True
            Me.TextBoxZoneName.Visible = True
            Me.ButtonDeletZone.Visible = True
            Me.ButtonUpdateZone.Visible = True
            
        Catch ex As Exception
            Response.Write("Error in ShowInfoForThisZone: " & ex.Message)
        End Try
        
    End Sub

    Protected Sub ButtonDeletZone_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        
        Try
            'Set our view
            MultiViewEditStat.ActiveViewIndex = 0
        
            ' Get Zone and Stat ID's
            Dim ZoneID As Integer = CInt(Me.LabelZoneID.Text)
            Dim ThermID As Integer = CInt(Me.LabelThermID.Text)
        
            ' Remove the zone
            pi.RemoveZone(ThermID, ZoneID)
        
            ' Reset Zone listing
            ShowInfoForThisStat(pi.StatObj(ThermID))
            Me.ButtonEditZone.Visible = True
            
        Catch ex As Exception
            Response.Write("Error in ButtonDeletZone_Click: " & ex.Message)
        End Try
        
    End Sub

    Protected Sub ButtonUpdateZone_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        
        Try
            ' Set right view
            MultiViewEditStat.ActiveViewIndex = 0
                
            ' Get Zone and Therm ID's
            Dim ZoneID As Integer = CInt(Me.LabelZoneID.Text)
            Dim ThermID As Integer = CInt(Me.LabelThermID.Text)
        
            ' Get the zone type based on the zone controller.
            Dim tType As HSPI_RCSSERIAL.Thermostat.StatTypes
            Select Case pi.StatObj(ThermID).Type
                Case HSPI_RCSSERIAL.Thermostat.StatTypes.ZC6R_Zone_Controller
                    tType = HSPI_RCSSERIAL.Thermostat.StatTypes.ZC6R_Zone
                Case HSPI_RCSSERIAL.Thermostat.StatTypes.ZCV_Zone_Controller
                    tType = HSPI_RCSSERIAL.Thermostat.StatTypes.ZCV_Zone
            End Select
            
            'New Zone
            If ZoneID = 7 Then
                Me.LabelZoneID.Text = Me.DropDownListZoneID.SelectedValue
                ZoneID = Me.DropDownListZoneID.SelectedValue
                pi.CreateZone(ThermID, ZoneID, Me.TextBoxZoneName.Text, tType)
            End If
            
            ' Update the Zone with the new data
            pi.UpdateZoneObj(ThermID, ZoneID, Me.TextBoxZoneName.Text, tType)
        
            ' Set the view
            Me.DropDownListZoneID.Visible = False
            Me.LabelZName.Visible = False
            Me.LabelZones.Visible = False
            Me.LabelZID.Visible = False
            Me.TextBoxZoneName.Visible = False
            Me.ButtonDeletZone.Visible = False
            Me.ButtonUpdateZone.Visible = False
        
            ' Return to stat editing
            ShowInfoForThisStat(pi.StatObj(ThermID))
            Me.ButtonEditZone.Visible = True
            
        Catch ex As Exception
            Response.Write("Error in ButtonUpdateZone_Click: " & ex.Message)
        End Try
        
    End Sub
</script>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>RCS Serial Configuration</title>
    <%Response.Write(GetHeadContent())%>
</head>
<body>
    <% Response.Write(GetBodyContent())%>
    <form id="form1" runat="server">
        <div>
            <br />
            <table style="border-right: black thin solid; border-top: black thin solid; border-left: black thin solid;
                border-bottom: black thin solid;" cellpadding="0" cellspacing="0">
                <tr>
                    <td style="width: 394px; height: 24px;" colspan="2" align="left">
                    <a href="rcsserial-Status.aspx">Status Page</a>
                    </td>
                    <td style="height: 24px; " colspan="2" align="right">
                    <a href="RCS Help File\RCSSerial-Help.htm">Help Page</a>
                    </td>
                </tr>
                
                <tr>
                    <td style="width: 394px; background-color: #Cdcdcd; height: 24px;">
                        <strong>Temperature Scale</strong></td>
                    <td style="background-color: #Cdcdcd; height: 24px;">
                        <asp:DropDownList ID="DropDownList1" runat="server">
                            <asp:ListItem Selected="True">Farenheight</asp:ListItem>
                            <asp:ListItem>Celcius</asp:ListItem>
                        </asp:DropDownList></td>
                </tr>
                <tr>
                    <td style="width: 394px; background-color: #f5f5f5;">
                        <strong>Poll Interval </strong>
                    </td>
                    <td style="background-color: #f5f5f5;">
                        <asp:TextBox ID="TextBoxPoll" runat="server" Width="50px"></asp:TextBox>
                        seconds</td>
                </tr>
                <tr>
                    <td style="width: 394px; background-color: #Cdcdcd; height: 19px;">
                        <strong>Enable Debug Messages</strong></td>
                    <td style="background-color: #Cdcdcd; height: 19px;">
                        <asp:CheckBox ID="CheckBoxDebug" runat="server" /></td>
                </tr>
                <tr>
                    <td style="background-color: #f5f5f5; text-align: left">
                        <strong>Baud Rate</strong></td>
                    <td style="background-color: #f5f5f5; text-align: left">
                        <asp:DropDownList ID="DropDownList2" runat="server" Enabled="False">
                            <asp:ListItem Selected="True">9600</asp:ListItem>
                            <asp:ListItem>19200</asp:ListItem>
                        </asp:DropDownList></td>
                </tr>
                <tr>
                    <td style="background-color: #Cdcdcd; text-align: left">
                        <strong>Reverse Dampers (Zone Controllers)</strong></td>
                    <td style="background-color: #Cdcdcd; text-align: left">
                        <asp:CheckBox ID="CheckBoxRevDampers" runat="server" /></td>
                </tr>
                <tr>
                    <td colspan="2" style="background-color: #f5f5f5; text-align: center">
                        <asp:Button ID="ButSave" runat="server" OnClick="ButSave_Click" Text="Save Settings"
                            CssClass="formbutton" /></td>
                </tr>
                <tr>
                    <td colspan="2" style="background-color: #Cdcdcd; text-align: center">
                        <br />
                    </td>
                </tr>
                <tr>
                    <td colspan="4" style="background-color: #f5f5f5; text-align: center">
                        &nbsp;<table width="100%">
                            <tr>
                                <td colspan="2">
                                    <strong>Add new thermostat</strong></td>
                            </tr>
                            <tr>
                                <td>
                                    Name:</td>
                                <td>
                                    <asp:TextBox ID="TextBoxNew" runat="server" Width="256px"></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>
                                    Address:<br />
                                    (Zone 1 Address for ZCVs)</td>
                                <td>
                                    <asp:DropDownList ID="DropDownListNewStatID" runat="server">
                                    </asp:DropDownList></td>
                            </tr>
                            <tr>
                                <td>
                                    Type:</td>
                                <td>
                                    <asp:DropDownList ID="DropDownListNewStatType" runat="server">
                                        <asp:ListItem>Stand Alone</asp:ListItem>
                                        <asp:ListItem>ZCV Zone Controller</asp:ListItem>
                                        <asp:ListItem>ZC6R Zone Controller</asp:ListItem>
                                        <asp:ListItem>Sensor</asp:ListItem>
                                    </asp:DropDownList></td>
                            </tr>
                        </table>
                        <asp:Button ID="ButAddStat" runat="server" Text="Add Thermostat" OnClick="ButAddStat_Click"
                            CssClass="formbutton" />
                    </td>
                </tr>
                <tr>
                    <td colspan="2" style="background-color: #Cdcdcd; text-align: center;">
                        <br />
                    </td>
                </tr>
                <tr>
                    <td style="background-color: #f5f5f5;" align="center" colspan="4">
                        Select Thermostat to Edit<br />
                        <asp:ListBox ID="ListBoxStats" runat="server" Width="320px" Height="120px"></asp:ListBox>
                        <br />
                        <asp:Button ID="ButEditStat" runat="server" Text="Edit Thermostat" OnClick="ButEditStat_Click"
                            CssClass="formbutton" /><br />
                        <asp:MultiView ID="MultiViewEditStat" runat="server">
                            <asp:View ID="ViewOptions" runat="Server">
                                <asp:Label ID="LabelThermID" runat="server" Text="Label" Visible="False"></asp:Label>
                                <asp:Label ID="LabelZoneID" runat="server" Text="Label" Visible="False"></asp:Label><br />
                                Thermostat Options:
                                <table width="100%">
                                    <tr>
                                        <td colspan="2">
                                            Name:</td>
                                        <td colspan="2">
                                            <asp:TextBox ID="TextBoxName" runat="server" Width="200px"></asp:TextBox></td>
                                    </tr>
                                    <tr>
                                        <td colspan="2">
                                            Location:</td>
                                        <td colspan="2">
                                            <asp:TextBox ID="TextBoxLocation" runat="server" Width="200px"></asp:TextBox></td>
                                    </tr>
                                    
                                    <tr>
                                        <td colspan="">
                                            Address:
                                        </td>
                                        <td>
                                            <asp:DropDownList ID="DropDownListEditStatID" runat="server">
                                            </asp:DropDownList>
                                        </td>
                                        
                                        <td colspan="2">
                                            ID (For Scripting):&nbsp;&nbsp;
                                            <asp:Label ID="LabelStatID" runat="server" Text="Label"></asp:Label>
                                        </td>
                                        
                                    </tr>
                                    
                                    <tr>
                                        <td colspan="2">
                                            Type:
                                        </td>
                                        <td>
                                            &nbsp;<asp:DropDownList ID="DropDownListEditStatType" runat="server">
                                                <asp:ListItem>Stand Alone</asp:ListItem>
                                                <asp:ListItem>ZCV Zone Controller</asp:ListItem>
                                                <asp:ListItem>ZC6R Zone Controller</asp:ListItem>
                                                <asp:ListItem>Sensor</asp:ListItem>
                                            </asp:DropDownList>
                                        </td>
                                    </tr>
                                    
                                    
                                    <tr>
                                        <td colspan="2" style="height: 21px">
                                            Single SetPoint Mode:</td>
                                        <td style="height: 21px" colspan="2"><asp:CheckBox ID="CheckSingleSetpoint" runat="server" /></td>
                                    </tr>
                                    <tr>
                                        <td colspan="4">
                                            <asp:Label ID="LabelZones" runat="server" Text="Zones:"></asp:Label></td>
                                    </tr>
                                    
                                    
                                    <tr>
                                        <td colspan="1" style="height: 114px" align="center">
                                            <asp:Button ID="ButtonEditZone" runat="server" Text="Edit Zone" OnClick="ButtonEditZone_Click" CssClass="formbutton" />
                                        </td>
                                        <td colspan="3" style="height: 114px" align="center">
                                            <asp:ListBox ID="ListBoxZones" runat="server" Width="320px" Height="100px"></asp:ListBox>
                                        </td>
                                    </tr>
                                    
                                    
                                    
                                    <tr>
                                        <td colspan="4">
                                        </td>
                                    </tr>
                                    <tr style="background-color: #cdcdcd">
                                        <td colspan="2">
                                            <asp:Label ID="LabelZName" runat="server" Text="Zone Name:"></asp:Label></td>
                                        <td colspan="2">
                                            <asp:TextBox ID="TextBoxZoneName" runat="server" Width="200px"></asp:TextBox></td>
                                    </tr>
                                    <tr style="background-color: #cdcdcd">
                                        <td colspan="2">
                                            <asp:Label ID="LabelZID" runat="server" Text="Zone ID:"></asp:Label></td>
                                        <td colspan="2">
                                            <asp:DropDownList ID="DropDownListZoneID" runat="server">
                                                <asp:ListItem>1</asp:ListItem>
                                                <asp:ListItem>2</asp:ListItem>
                                                <asp:ListItem>3</asp:ListItem>
                                                <asp:ListItem>4</asp:ListItem>
                                                <asp:ListItem>5</asp:ListItem>
                                                <asp:ListItem>6</asp:ListItem>
                                            </asp:DropDownList></td>
                                    </tr>
                                    <tr style="background-color: #cdcdcd">
                                        <td colspan="4">
                                            <asp:Button ID="ButtonUpdateZone" runat="server" Text="Update Zone" OnClick="ButtonUpdateZone_Click"
                                                CssClass="formbutton" />
                                            <asp:Button ID="ButtonDeletZone" runat="server" Text="Delete Zone" OnClick="ButtonDeletZone_Click"
                                                CssClass="formbutton" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan="4" rowspan="2">
                                        </td>
                                    </tr>
                                    <tr>
                                    </tr>
                                </table>
                                <asp:Button ID="ButUpdateStat" runat="server" Text="Update Thermostat" OnClick="ButUpdateStat_Click"
                                    CssClass="formbutton" />
                                <asp:Button ID="ButDeleteStat" runat="server" Text="Delete Thermostat" OnClick="ButDeleteStat_Click"
                                    CssClass="formbutton" /><br />
                            </asp:View>
                            <asp:View ID="ViewNothing" runat="server">
                            </asp:View>
                        </asp:MultiView>
                    </td>
                </tr>
            </table>
        </div>
    </form>
</body>
</html>
--%>