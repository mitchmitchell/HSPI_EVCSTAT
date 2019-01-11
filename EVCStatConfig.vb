Imports System.Text
Imports System.Web
Imports System.Threading
Imports Scheduler
Imports System.Data
Imports HomeSeerAPI

Public Class EVCSTATConfig
    Inherits clsPageBuilder

    Dim i As Integer
    Dim pi As HSPI
    Dim s As String = ""
    Dim j As Integer
    Dim poll As Boolean = True

    Dim dd As New clsJQuery.jqDropList("dd0", PageName, False)
    Dim tb As New clsJQuery.jqTextBox("tb0", "", "", PageName, 0, True)
    Dim lb As New clsJQuery.jqListBox("lb0", PageName)
    Dim cb As New clsJQuery.jqCheckBox("cb0", "", PageName, True, True)
    Dim b As New clsJQuery.jqButton("b0", "", PageName, True)
    Dim colObjectValues As New hsCollection

    Public Sub New(ByVal PageName As String)
        MyBase.New(PageName)
    End Sub

    Public Overrides Function postBackProc(page As String, data As String, user As String, userRights As Integer) As String
        Dim parts As Collections.Specialized.NameValueCollection
        Dim sKey As String = ""
        parts = HttpUtility.ParseQueryString(data)
        Dim CurrentStat As Thermostat

        Select Case parts("id")
            Case "oButSave"
                ButSave_Click(data)
            Case "oButAddStat"
                ButAddStat_Click(data)
            Case "oButUpdateStat"
                ButUpdateStat_Click(data)
            Case "oButDeleteStat"
                ButDeleteStat_Click(data)
            Case "oListBoxStats"
                If parts("ListBoxStats") = "" Then
                    BuildOptions()
                Else
                    CurrentStat = arrThermostats(parts("ListBoxStats"))
                    BuildOptions(CurrentStat, True)
                End If
        End Select

        Return MyBase.postBackProc(page, data, user, userRights)
    End Function

    ' build and return the actual page
    Public Function GetPagePlugin(ByVal pageName As String, ByVal user As String, ByVal userRights As Integer, ByVal queryString As String) As String
        Dim stb As New StringBuilder

        pi = plugin

        Try
            Me.reset()

            ' handle any queries like mode=something
            Dim parts As Collections.Specialized.NameValueCollection = Nothing

            ' add the normal title
            Me.AddHeader(hs.GetPageHeader(pageName, "EVC MQTTT Configuration", "", "", False, False))

            stb.Append(clsPageBuilder.DivStart("pluginpage", ""))
            ' a message area for error messages from jquery ajax postback (optional, only needed if using AJAX calls to get data)
            stb.Append(clsPageBuilder.DivStart("errormessage", "class='errormessage'"))
            stb.Append(clsPageBuilder.DivEnd)

            'Me.RefreshIntervalMilliSeconds = 5000          ' # of seconds between callbacks 
            '' add a callback post string, this is what will be posted when the page timer expires 
            'stb.Append(Me.AddAjaxHandlerPost("id=timer", pageName))

            ' specific page code is here
            stb.Append(BuildWebPageBody())

            stb.Append(clsPageBuilder.DivEnd)

            ' add the body html to the page
            Me.AddBody(stb.ToString)

            Me.AddFooter(hs.GetPageFooter)
            Me.suppressDefaultFooter = True

            ' return the full page
            Return Me.BuildPage()
        Catch ex As Exception
            Log("Error - Building page: " & ex.Message, LogLevel.Normal)
            Return "error"
        End Try
    End Function


    Private Sub LoadItems()
        Try

            BuildDropList("ListBoxStats", True)
            BuildTextBox("TextBoxPoll", True)
            BuildCheckBox("CheckBoxDebug", True)
            BuildDropList("DropDownListEditStatID", True)

        Catch ex As Exception
            Log("Error in LoadItems: " & ex.Message)
        End Try
    End Sub

    Protected Sub ButSave_Click(ByVal data As String)
        Dim sKey As String
        Dim parts As Collections.Specialized.NameValueCollection
        Dim BaudRate As Integer
        parts = HttpUtility.ParseQueryString(data)
        Try
            'reset checkbox values first
            gDebug = False

            For Each sKey In parts.Keys
                If sKey Is Nothing Then Continue For
                If String.IsNullOrEmpty(sKey.Trim) Then Continue For
                Select Case True
                    Case InStr(sKey, "TextBoxPoll") > 0
                        ComThread.SetPolling(CInt(parts(sKey)))
                    Case InStr(sKey, "TextBoxSend") > 0
                        ComThread.SetSendTopic(CStr(parts(sKey)))
                    Case InStr(sKey, "TextBoxRecv") > 0
                        ComThread.SetRecvTopic(CStr(parts(sKey)))
                    Case InStr(sKey, "TextBoxHost") > 0
                        ComThread.SetMQTTHost(CStr(parts(sKey)))
                    Case InStr(sKey, "DropDownList") > 0
                        BaudRate = CInt(DDText("DropDownList", parts(sKey)))
                        If ComThread.BaudRate <> BaudRate Then
                            ComThread.BaudRate = BaudRate
                            ComThread.Restart()
                        End If

                    Case InStr(sKey, "CheckBoxDebug") > 0
                        gDebug = CheckBoxValue(parts(sKey))
                End Select
            Next
            SaveData(data)
        Catch ex As Exception
            Log("Error In Save: " & ex.Message)
        End Try
    End Sub


    Function BuildStatList() As Pair()
        Dim Thermostat As Thermostat
        Dim i As Integer = 0
        Dim DataPairs() As Pair = Nothing
        For Each Thermostat In arrThermostats.Values
            ReDim Preserve DataPairs(i)
            DataPairs(i).Name = Thermostat.Name
            DataPairs(i).Value = Thermostat.RefID
            i += 1
        Next
        Return DataPairs
    End Function

    Protected Sub ButUpdateStat_Click(ByVal data As String)
        Dim parts As Collections.Specialized.NameValueCollection
        'Dim Name As String
        Dim Address As String
        Dim RefID As Integer
        Dim oThermostat As Thermostat

        parts = HttpUtility.ParseQueryString(data)

        RefID = parts("RefID")
        'If parts("TextBoxName") = "" Then
        '    Name = parts("TextBoxNew")
        'Else
        '    Name = parts("TextBoxName")
        'End If
        If parts("DropDownListEditStatID") = "" Then
            Address = parts("DropDownListNewStatID")
        Else
            Address = parts("DropDownListEditStatID")
        End If
        Try
            'pi.UpdateStatObj(CurrentStat.RefID, TextBoxName & vbTab & parts("TextBoxLocation") & vbTab & DropDownListStatID & vbTab & DDText("DropDownListEditStatType", DropDownListStatType) & vbTab & CheckBoxValue(parts("CheckSingleSetpoint")).ToString, poll)
            oThermostat = arrThermostats(RefID.ToString)
            'oThermostat.Name = Name
            oThermostat.Address = Address
            'oThermostat.Location = parts("TextBoxLocation")
            'oThermostat.Location2 = parts("TextBoxLocation2")
            BuildOptions()
            BuildDropList("ListBoxStats", True)
            BuildDropList("DropDownListNewStatID", True)
        Catch ex As Exception
            Log("Error in UpdateStat: " & ex.Message & ex.TargetSite.ToString & ex.StackTrace)
        End Try
    End Sub

    Protected Sub ButAddStat_Click(ByVal data As String)
        Dim parts As Collections.Specialized.NameValueCollection
        Dim stat As Thermostat
        parts = HttpUtility.ParseQueryString(data)
        Try

            If parts("TextBoxNew") = "" Then Exit Sub

            stat = AddThermostat(parts("TextBoxNew"), parts("DropDownListNewStatID"))
            stat.Address = parts("DropDownListNewStatID")
            stat.Location = parts("TextBoxLocation")
            stat.Location2 = parts("TextBoxLocation2")

            ' Clear out the old stat
            BuildTextBox("TextBoxNew", True)
            BuildTextBox("TextBoxLocation", True)
            BuildTextBox("TextBoxLocation2", True)
            BuildDropList("DropDownListNewStatID", True)

            BuildDropList("ListBoxStats", True, stat.Address)

        Catch ex As Exception
            Log("Error in ButAddStat_Click: " & ex.Message)
        End Try

    End Sub

    Private Sub LoadDefaultStatVals()
        BuildTextBox("TextBoxLocation", True, "")
    End Sub

    Private Function BuildUsedStatAddressList() As String
        Dim Thermostat As Thermostat
        Dim StatList As String = "|"
        For Each Thermostat In arrThermostats.Values
            StatList &= Thermostat.Address & "|"
        Next
        Return StatList
    End Function

    Protected Sub ButDeleteStat_Click(ByVal data As String)
        Dim parts As Collections.Specialized.NameValueCollection
        parts = HttpUtility.ParseQueryString(data)

        Try

            RemoveThermostat(parts("RefID"))
            BuildButton("ButEditStat", True, False)
            LoadItems()
            BuildDropList("ListBoxStats", True)
            BuildDropList("DropDownListNewStatID", True)
            BuildOptions()

        Catch ex As Exception
            Log("Error in ButDeleteStat_Click: " & ex.Message)
        End Try

    End Sub

    Function BuildWebPageBody() As String
        Dim stb As New StringBuilder
        Try
            stb.Append("<table width='1000' cellpadding='0' cellspacing='0' border='0'>")
            stb.Append(" <tr>")
            stb.Append("  <td></td>")
            stb.Append("  <td align='right'>")
            stb.Append("   <a href='EVC%20Help%20File\EVCSerial-Help.htm'>Help Page</a>")
            stb.Append("  </td>")
            stb.Append(" </tr>")
            stb.Append(" <tr>")
            stb.Append("  <td colspan='2'>")
            stb.Append(clsPageBuilder.FormStart("frmStatsData", "StatsData", "Post"))
            stb.Append("   <table width='1000' cellpadding='0' cellspacing='0' style='border-right: black thin solid; border-top: black thin solid; border-left: black thin solid; border-bottom: black thin solid;'>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='3' style='background-color: #Cdcdcd; text-align: center'><br /></td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td style='width: 394px; background-color: #f5f5f5;'><strong>Poll Interval </strong></td>")
            stb.Append("     <td style='background-color: #f5f5f5;' width='100'>" & BuildTextBox("TextBoxPoll") & "</td>")
            stb.Append("     <td style='background-color: #f5f5f5;'> seconds</td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td style='width: 394px; background-color: #f5f5f5;'><strong>MQTTT Host Address </strong></td>")
            stb.Append("     <td style='background-color: #f5f5f5;' width='100'>" & BuildTextBox("TextBoxHost") & "</td>")
            stb.Append("     <td style='background-color: #f5f5f5;'></td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td style='width: 394px; background-color: #f5f5f5;'><strong>MQTTT Send Topic </strong></td>")
            stb.Append("     <td style='background-color: #f5f5f5;' width='100'>" & BuildTextBox("TextBoxSend") & "</td>")
            stb.Append("     <td style='background-color: #f5f5f5;'></td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td style='width: 394px; background-color: #f5f5f5;'><strong>MQTTT Receive Topic </strong></td>")
            stb.Append("     <td style='background-color: #f5f5f5;' width='100'>" & BuildTextBox("TextBoxRecv") & "</td>")
            stb.Append("     <td style='background-color: #f5f5f5;'></td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td style='width: 394px; background-color: #f5f5f5; height: 19px;'><strong>Enable Debug Messages</strong></td>")
            stb.Append("     <td style='background-color: #f5f5f5; height: 19px;' colspan='2'>" & BuildCheckBox("CheckBoxDebug") & "</td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='3' style='background-color: #f5f5f5; text-align: center'><br>" & BuildButton("ButSave") & "<br></td>")
            stb.Append("    </tr>")
            stb.Append("   </table>")
            stb.Append(clsPageBuilder.FormEnd)
            stb.Append("  </td>")
            stb.Append(" </tr>")
            '############################################################################################
            stb.Append(" <tr>")
            stb.Append("  <td colspan='2'>")
            stb.Append(clsPageBuilder.FormStart("frmStatAdd", "AddStat", "Post"))
            stb.Append("   <table width='1000' cellpadding='0' cellspacing='0' style='border-right: black thin solid; border-top: none; border-left: black thin solid; border-bottom: black thin solid;'>")
            stb.Append("    <tr>")
            stb.Append("     <td style='background-color: #Cdcdcd; text-align: center'><br /></td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td style='background-color: #f5f5f5; text-align: center'>")
            stb.Append("      <table width='100%'>")
            stb.Append("       <tr><td colspan='3'><strong>Add new thermostat</strong></td></tr>")
            stb.Append("       <tr>")
            stb.Append("        <td>Name:</td>")
            stb.Append("        <td>" & BuildTextBox("TextBoxNew") & "</td>")
            stb.Append("       </tr>")
            stb.Append("       <tr>")
            stb.Append("        <td>Location1:</td>")
            stb.Append("        <td>" & BuildTextBox("TextBoxLocation") & "</td>")
            stb.Append("       </tr>")
            stb.Append("       <tr>")
            stb.Append("        <td>Location2:</td>")
            stb.Append("        <td>" & BuildTextBox("TextBoxLocation2") & "</td>")
            stb.Append("       </tr>")
            stb.Append("       <tr>")
            stb.Append("        <td>Address:</td>")
            stb.Append("        <td>" & BuildDropList("DropDownListNewStatID") & "</td>")
            stb.Append("       </tr>")
            stb.Append("      </table>")
            stb.Append(BuildButton("ButAddStat") & "<br>")
            stb.Append("     </td>")
            stb.Append("    </tr>")
            stb.Append("   </table>")
            stb.Append(clsPageBuilder.FormEnd)
            stb.Append("  </td>")
            stb.Append(" </tr>")
            '#############################################################################################
            stb.Append(" <tr>")
            stb.Append("  <td colspan='2'>")
            stb.Append("   <table width='1000' cellpadding='0' cellspacing='0' style='border-right: black thin solid; border-top: none; border-left: black thin solid; border-bottom: black thin solid;'>")
            stb.Append("    <tr><td style='background-color: #Cdcdcd; text-align: center;'><br /></td></tr>")
            stb.Append("    <tr>")
            stb.Append("     <td align='center' style='background-color: #f5f5f5;'>")
            stb.Append("      <table width='100%'>")
            stb.Append("       <tr><td><strong>Select Thermostat to Edit</strong></td></tr>")
            stb.Append("       <tr>")
            stb.Append("        <td align='center'>" & BuildDropList("ListBoxStats") & "</td>")
            stb.Append("       </tr>")
            stb.Append("      </table>")
            stb.Append("     </td>")
            stb.Append("    </tr>")
            stb.Append("   </table>")
            stb.Append("  </td>")
            stb.Append(" </tr>")
            stb.Append(" <tr><td colspan='2'><div id='ViewOptions'></div></td></tr>")
            '#############################################################################################
            stb.Append("</table>")

            Return stb.ToString
        Catch ex As Exception
            Log("Error in BuildWebPageBody: " & ex.Message)
        End Try
        Return Nothing
    End Function

    Sub BuildOptions(Optional stat As Thermostat = Nothing, Optional ByVal Visible As Boolean = False)
        Dim stb As New StringBuilder
        If Visible Then
            stb.Append(clsPageBuilder.FormStart("frmStatEdit", "EditStat", "Post"))
            stb.Append("   <table width='1000' cellpadding='0' cellspacing='0' style='border-right: black thin solid; border-top: none; border-left: black thin solid; border-bottom: black thin solid;'>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='3' style='background-color: #Cdcdcd; text-align: center'><br /><input id='oRefID' name='RefID' type='hidden' value='" & stat.RefID & "'></td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='2'>Name:</td>")
            stb.Append("     <td colspan='2'>" & BuildLabel("LabelName", False, stat.Name) & "</td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='2'>Location1:</td>")
            stb.Append("     <td colspan='2'>" & BuildLabel("LabelLocation", False, stat.Location) & "</td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='2'>Location2:</td>")
            stb.Append("     <td colspan='2'>" & BuildLabel("LabelLocation2", False, stat.Location2) & "</td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='2'>Thermostat Address:</td>")
            stb.Append("     <td colspan='2' nowrap>" & BuildDropList("DropDownListEditStatID", False, , , stat.Address) & "</td>")
            stb.Append("    </tr>")
            stb.Append("    <tr>")
            stb.Append("     <td colspan='4' align='Center'>")
            stb.Append(BuildButton("ButUpdateStat"))
            stb.Append(BuildButton("ButDeleteStat"))
            stb.Append("   <br />")
            stb.Append("     </td>")
            stb.Append("    </tr>")
            stb.Append("   </table>")
            stb.Append(clsPageBuilder.FormEnd)
        End If
        Me.divToUpdate.Add("ViewOptions", stb.ToString)
    End Sub

    Function BuildButton(ByVal Name As String, Optional ByVal Rebuilding As Boolean = False, Optional ByVal Enabled As Boolean = True, Optional ByVal Visible As Boolean = True) As String
        Dim Content As String = ""
        Dim ButtonText As String = "Submit"

        Select Case Name
            Case "ButSave"
                ButtonText = "Save Settings"
            Case "ButAddStat"
                ButtonText = "Add Thermostat"
            Case "ButEditStat"
                ButtonText = "Edit Thermostat"
            Case "ButUpdateStat"
                ButtonText = "Update Thermostat"
            Case "ButDeleteStat"
                ButtonText = "Delete Thermostat"
        End Select
        If Visible Then
            If Rebuilding Then
                Me.divToUpdate.Add(Name & "_div", FormButton(b, Name, ButtonText, True, , , , Enabled))
            Else
                Content = "<div id='" & Name & "_div'>" & FormButton(b, Name, ButtonText, True, , , , Enabled) & "</div>"
            End If
        Else
            Me.divToUpdate.Add(Name & "_div", "")
        End If
        Return Content
    End Function

    Function BuildLabel(ByVal Name As String, Optional ByVal Rebuilding As Boolean = False, Optional ByVal Msg As String = "", Optional ByVal Visible As Boolean = True) As String
        Dim Content As String = ""


        If Msg <> "" Then
            If colObjectValues.ContainsKey(Name) Then
                colObjectValues(Name) = Msg
            Else
                colObjectValues.Add(CObj(Msg), Name)
            End If
        End If

        If Visible Then
            If colObjectValues.ContainsKey(Name) Then Msg = colObjectValues(Name)
            If Rebuilding Then
                Me.divToUpdate.Add(Name & "_div", FormLabel(Name, Msg))
            Else
                Content = "<div id='" & Name & "_div'>" & FormLabel(Name, Msg) & "</div>"
            End If
        Else
            Me.divToUpdate.Add(Name & "_div", "")
        End If
        Return Content
    End Function

    Function BuildTextBox(ByVal Name As String, Optional ByVal Rebuilding As Boolean = False, Optional ByVal Text As String = "", Optional ByVal Visible As Boolean = True) As String
        Dim Content As String = ""
        Select Case Name
            Case "TextBoxPoll"
                Text = ComThread.PollInterval.ToString
            Case "TextBoxSend"
                Text = ComThread.MQTT_SendTopic
            Case "TextBoxRecv"
                Text = ComThread.MQTT_RecvTopic
            Case "TextBoxHost"
                Text = ComThread.MQTT_HostAddr
        End Select
        If Text <> "" Then
            If colObjectValues.ContainsKey(Name) Then
                colObjectValues(Name) = Text
            Else
                colObjectValues.Add(CObj(Text), Name)
            End If
        End If
        If Visible Then
            If Rebuilding Then
                Me.divToUpdate.Add(Name & "_div", HTMLTextBox(Name, Text, 20))
            Else
                Content = "<div id='" & Name & "_div'>" & HTMLTextBox(Name, Text, 20) & "</div>"
            End If
        Else
            Me.divToUpdate.Add(Name & "_div", "")
        End If

        Return Content
    End Function

    Function BuildCheckBox(ByVal Name As String, Optional ByVal Rebuilding As Boolean = False, Optional ByVal Checked As Integer = -1, Optional ByVal Visible As Boolean = True) As String
        Dim Content As String = ""

        Select Case Name
            Case "CheckBoxDebug"
                Checked = Math.Abs(CInt(gDebug))
        End Select

        If Checked >= 0 Then
            If colObjectValues.ContainsKey(Name) Then
                colObjectValues(Name) = Checked
            Else
                colObjectValues.Add(Checked, Name)
            End If
        End If

        If Visible Then
            If colObjectValues.ContainsKey(Name) Then
                Checked = colObjectValues(Name)
            Else
                Checked = 0
            End If

            If Rebuilding Then
                Me.divToUpdate.Add(Name & "_div", FormCheckBox(cb, Name, Checked, True, True))
            Else
                Content = "<div id='" & Name & "_div'>" & FormCheckBox(cb, Name, Checked, True, True) & "</div>"
            End If
        Else
            Me.divToUpdate.Add(Name & "_div", "")
        End If

        Return Content
    End Function

    Function BuildListBox(ByVal Name As String, Optional ByVal Rebuilding As Boolean = False, Optional ByVal Selected As Integer = -1, Optional ByVal Visible As Boolean = True) As String
        Dim Content As String = ""
        Dim DataPairs() As Pair = Nothing

        Select Case Name
            Case "ListBoxStats"
                DataPairs = BuildStatList()

        End Select

        If Selected >= 0 Then
            If colObjectValues.ContainsKey(Name) Then
                colObjectValues(Name) = Selected
            Else
                colObjectValues.Add(Selected, Name)
            End If
        End If

        If Visible Then
            If colObjectValues.ContainsKey(Name) Then Selected = colObjectValues(Name)

            If Rebuilding Then
                Me.divToUpdate.Add(Name & "_div", FormListBox(lb, Name, DataPairs))
            Else
                Content = "<div id='" & Name & "_div'>" & FormListBox(lb, Name, DataPairs) & "</div>"
            End If
        Else
            Me.divToUpdate.Add(Name & "_div", "")
        End If

        Return Content
    End Function

    Function BuildDropList(ByVal Name As String, Optional ByVal Rebuilding As Boolean = False, Optional ByVal Selected As Integer = -1, Optional ByVal Visible As Boolean = True, Optional ByVal SelectedValue As String = "") As String
        Dim Content As String = ""
        Dim DataPairs() As Pair = Nothing
        Dim i As Integer
        Dim x As Integer = 0
        Dim EnumArray() As String
        Dim Msg As String = ""
        Dim AddBlank As Boolean = False
        Dim UsedAddressList As String = ""
        Dim AutoSubmit As Boolean = True
        Select Case Name
            Case "DropDownList"
                ReDim EnumArray(1)
                EnumArray(0) = "9600"
                EnumArray(1) = "19200"
                For i = 0 To 1
                    ReDim Preserve DataPairs(i)
                    DataPairs(i).Name = EnumArray(i)
                    DataPairs(i).Value = i.ToString
                Next
                Select Case ComThread.BaudRate
                    Case 9600
                        Selected = 0
                    Case 19200
                        Selected = 1
                End Select
                AddBlank = True
                Msg = "Please Select"
            Case "ListBoxStats"
                DataPairs = BuildStatList()
                AddBlank = True
                Msg = "Select Thermostat"
                AutoSubmit = False
            Case "DropDownListNewStatID", "DropDownListEditStatID"
                UsedAddressList = BuildUsedStatAddressList()
                For i = 1 To 15
                    If InStr(UsedAddressList, "|" & i.ToString & "|") = 0 Or SelectedValue <> "" Then
                        ReDim Preserve DataPairs(x)
                        DataPairs(x).Name = i.ToString
                        DataPairs(x).Value = i.ToString
                        x += 1
                    End If
                Next
        End Select
        If Selected >= 0 Then
            If colObjectValues.ContainsKey(Name) Then
                colObjectValues(Name) = Selected
            Else
                colObjectValues.Add(Selected, Name)
            End If
        End If
        If Visible Then
            If colObjectValues.ContainsKey(Name) Then Selected = colObjectValues(Name)
            If Rebuilding Then
                Me.divToUpdate.Add(Name & "_div", FormDropDown(dd, Name, DataPairs, Selected, , AutoSubmit, AddBlank, , , , Msg, SelectedValue))
            Else
                Content = "<div id='" & Name & "_div'>" & FormDropDown(dd, Name, DataPairs, Selected, , AutoSubmit, AddBlank, , , , Msg, SelectedValue) & "</div>"
            End If
        Else
            Me.divToUpdate.Add(Name & "_div", "")
        End If
        Return Content
    End Function
End Class