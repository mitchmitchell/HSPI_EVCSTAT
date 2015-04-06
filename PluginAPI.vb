Imports System
Imports Scheduler
Imports HomeSeerAPI
Imports DeviceAPI

Imports HSCF.Communication.Scs.Communication.EndPoints.Tcp
Imports HSCF.Communication.ScsServices.Client
Imports HSCF.Communication.ScsServices.Service
Imports System.Reflection


' Plugin API, this API is called from HomeSeer and must be implemented in the plugin
Public Class PluginAPI
    Inherits ScsService
    Implements IPlugInAPI               ' this API is required for ALL plugins
    'Implements IThermostatAPI           ' add this API if this plugin supports thermostats

    ' a custom call to call a specific procedure in the plugin
    Public Function PluginFunction(ByVal proc As String, ByVal parms() As Object) As Object Implements IPlugInAPI.PluginFunction
        Try
            Dim ty As Type = Me.GetType
            Dim mi As MethodInfo = ty.GetMethod(proc)
            If mi Is Nothing Then
                Log("Method " & proc & " does not exist in this plugin.", LogLevel.Err)
                Return Nothing
            End If
            Return (mi.Invoke(Me, parms))
        Catch ex As Exception
            Log("Error in PluginProc: " & ex.Message, LogLevel.Err)
        End Try

        Return Nothing
    End Function

    Public ReadOnly Property Name As String Implements HomeSeerAPI.IPlugInAPI.Name
        Get
            Return plugin.name
        End Get
    End Property

    Public Function PluginPropertyGet(ByVal proc As String, parms() As Object) As Object Implements IPlugInAPI.PluginPropertyGet
        Try
            Dim ty As Type = Me.GetType
            Dim mi As PropertyInfo = ty.GetProperty(proc)
            If mi Is Nothing Then
                Log("Method " & proc & " does not exist in this plugin.", LogLevel.Err)
                Return Nothing
            End If
            Return mi.GetValue(Me, Nothing)
        Catch ex As Exception
            Log("Error in PluginProc: " & ex.Message, LogLevel.Err)
        End Try

        Return Nothing
    End Function

    Public Sub PluginPropertySet(ByVal proc As String, value As Object) Implements IPlugInAPI.PluginPropertySet
        Try
            Dim ty As Type = Me.GetType
            Dim mi As PropertyInfo = ty.GetProperty(proc)
            If mi Is Nothing Then
                Log("Property " & proc & " does not exist in this plugin.", LogLevel.Err)
            End If
            mi.SetValue(Me, value, Nothing)
        Catch ex As Exception
            Log("Error in PluginPropertySet: " & ex.Message, LogLevel.Err)
        End Try
    End Sub


    ' HS will display a COM Port text box on the interfaces page for user to enter a COM port for use by the plugin
    Public ReadOnly Property HSCOMPort As Boolean Implements HomeSeerAPI.IPlugInAPI.HSCOMPort
        Get
            Return True
        End Get
    End Property

    ' we won't need this if we get rid of IR plugins (X10 already gone)
    Public Function Capabilities() As Integer Implements HomeSeerAPI.IPlugInAPI.Capabilities
        Return 4
    End Function


    Public Function AccessLevel() As Integer Implements HomeSeerAPI.IPlugInAPI.AccessLevel
        Return plugin.AccessLevel
    End Function

    Public Function InterfaceStatus() As HomeSeerAPI.IPlugInAPI.strInterfaceStatus Implements HomeSeerAPI.IPlugInAPI.InterfaceStatus
        Dim es As New IPlugInAPI.strInterfaceStatus
        es.intStatus = IPlugInAPI.enumInterfaceStatus.OK
        Return es
    End Function

    Public Function SupportsMultipleInstances() As Boolean Implements HomeSeerAPI.IPlugInAPI.SupportsMultipleInstances
        Return plugin.SupportsMultipleInstances
    End Function

    Public Function SupportsMultipleInstancesSingleEXE() As Boolean Implements HomeSeerAPI.IPlugInAPI.SupportsMultipleInstancesSingleEXE
        Return plugin.SupportsMultipleInstancesSingleEXE
    End Function

    Public Function InstanceFriendlyName() As String Implements HomeSeerAPI.IPlugInAPI.InstanceFriendlyName
        Return plugin.InstanceFriendlyName
    End Function

    Public Function InitIO(ByVal port As String) As String Implements HomeSeerAPI.IPlugInAPI.InitIO
        Return plugin.InitIO(port)
    End Function

    Public Function RaisesGenericCallbacks() As Boolean Implements HomeSeerAPI.IPlugInAPI.RaisesGenericCallbacks
        Return True
    End Function

    Public Function Search(ByVal SearchString As String, ByVal RegEx As Boolean) As HomeSeerAPI.SearchReturn() Implements IPlugInAPI.Search
        ' Not yet implemented in the Sample
        '
        ' Normally we would do a search on plug-in actions, triggers, devices, etc. for the string provided, using
        '   the string as a regular expression if RegEx is True.
        '
    End Function

    Public Sub SetIOMulti(colSend As System.Collections.Generic.List(Of HomeSeerAPI.CAPI.CAPIControl)) Implements HomeSeerAPI.IPlugInAPI.SetIOMulti
        plugin.SetIOMulti(colSend)
    End Sub

    Public Sub shutdownIO() Implements HomeSeerAPI.IPlugInAPI.ShutdownIO
        plugin.ShutdownIO()
    End Sub

    Public Sub HSEvent(ByVal EventType As Enums.HSEvent, ByVal parms() As Object) Implements HomeSeerAPI.IPlugInAPI.HSEvent
        plugin.HSEvent(EventType, parms)
    End Sub

    Public Function PollDevice(ByVal dvref As Integer) As IPlugInAPI.PollResultInfo Implements HomeSeerAPI.IPlugInAPI.PollDevice

    End Function

    ' HS2 compatible web page access
    Public Function GenPage(ByVal link As String) As String Implements HomeSeerAPI.IPlugInAPI.GenPage
        Return ""
    End Function

    Public Function PagePut(ByVal data As String) As String Implements HomeSeerAPI.IPlugInAPI.PagePut
        Return ""
    End Function

    ' HS3 compatible web page access
    Public Function GetPagePlugin(ByVal pageName As String, ByVal user As String, ByVal userRights As Integer, ByVal queryString As String) As String Implements HomeSeerAPI.IPlugInAPI.GetPagePlugin
        Return plugin.GetPagePlugin(pageName, user, userRights, queryString)
    End Function

    Public Function PostBackProc(ByVal pageName As String, ByVal data As String, ByVal user As String, ByVal userRights As Integer) As String Implements HomeSeerAPI.IPlugInAPI.PostBackProc
        Return plugin.postBackProc(pageName, data, user, userRights)
    End Function

    Public Property ActionAdvancedMode As Boolean Implements HomeSeerAPI.IPlugInAPI.ActionAdvancedMode
        Set(ByVal value As Boolean)

        End Set
        Get
            Return False
        End Get
    End Property

    Public Function ActionBuildUI(ByVal sUnique As String, ByVal ActInfo As IPlugInAPI.strTrigActInfo) As String Implements HomeSeerAPI.IPlugInAPI.ActionBuildUI
        Return plugin.ActionBuildUI(sUnique, ActInfo)
    End Function

    Public Function ActionConfigured(ByVal ActInfo As IPlugInAPI.strTrigActInfo) As Boolean Implements HomeSeerAPI.IPlugInAPI.ActionConfigured
        Return plugin.ActionConfigured(ActInfo)
    End Function

    Public Function ActionReferencesDevice(ByVal ActInfo As IPlugInAPI.strTrigActInfo, ByVal dvRef As Integer) As Boolean Implements HomeSeerAPI.IPlugInAPI.ActionReferencesDevice
        Console.WriteLine("ActionReferencesDevice Called")

    End Function

    Public Function ActionFormatUI(ByVal ActInfo As IPlugInAPI.strTrigActInfo) As String Implements HomeSeerAPI.IPlugInAPI.ActionFormatUI
        Return plugin.ActionFormatUI(ActInfo)
    End Function

    Public ReadOnly Property ActionName(ByVal ActionNumber As Integer) As String Implements HomeSeerAPI.IPlugInAPI.ActionName
        Get
            Return plugin.ActionName(ActionNumber)
        End Get
    End Property

    Public Function ActionProcessPostUI(ByVal PostData As Collections.Specialized.NameValueCollection, ByVal TrigInfoIN As IPlugInAPI.strTrigActInfo) As IPlugInAPI.strMultiReturn Implements HomeSeerAPI.IPlugInAPI.ActionProcessPostUI
        Return plugin.ActionProcessPostUI(PostData, TrigInfoIN)
    End Function

    Public Function ActionCount() As Integer Implements HomeSeerAPI.IPlugInAPI.ActionCount
        Return plugin.ActionCount
    End Function

    Public Property Condition(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As Boolean Implements HomeSeerAPI.IPlugInAPI.Condition
        Set(ByVal value As Boolean)

        End Set
        Get
            Return False
        End Get
    End Property

    Public Function HandleAction(ByVal ActInfo As IPlugInAPI.strTrigActInfo) As Boolean Implements HomeSeerAPI.IPlugInAPI.HandleAction
        Return plugin.HandleAction(ActInfo)
    End Function

    Public ReadOnly Property HasConditions(ByVal TriggerNumber As Integer) As Boolean Implements HomeSeerAPI.IPlugInAPI.HasConditions
        Get
            Return False
        End Get
    End Property



    Public Function TriggerTrue(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As Boolean Implements HomeSeerAPI.IPlugInAPI.TriggerTrue
        Return False
    End Function

    Public ReadOnly Property HasTriggers() As Boolean Implements HomeSeerAPI.IPlugInAPI.HasTriggers
        Get
            Return plugin.HasTriggers
        End Get
    End Property

    Public ReadOnly Property SubTriggerCount(ByVal TriggerNumber As Integer) As Integer Implements HomeSeerAPI.IPlugInAPI.SubTriggerCount
        Get
            Return plugin.SubTriggerCount(TriggerNumber)
        End Get
    End Property

    Public ReadOnly Property SubTriggerName(ByVal TriggerNumber As Integer, ByVal SubTriggerNumber As Integer) As String Implements HomeSeerAPI.IPlugInAPI.SubTriggerName
        Get
            Return plugin.SubTriggerName(TriggerNumber, SubTriggerNumber)
        End Get
    End Property

    Public Function TriggerBuildUI(ByVal sUnique As String, ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As String Implements HomeSeerAPI.IPlugInAPI.TriggerBuildUI
        Return plugin.TriggerBuildUI(sUnique, TrigInfo)
    End Function

    Public ReadOnly Property TriggerConfigured(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As Boolean Implements HomeSeerAPI.IPlugInAPI.TriggerConfigured
        Get
            Return plugin.TriggerConfigured(TrigInfo)
        End Get
    End Property

    Public Function TriggerReferencesDevice(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo, ByVal dvRef As Integer) As Boolean Implements HomeSeerAPI.IPlugInAPI.TriggerReferencesDevice

    End Function

    Public Function TriggerFormatUI(ByVal TrigInfo As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As String Implements HomeSeerAPI.IPlugInAPI.TriggerFormatUI
        Return plugin.TriggerFormatUI(TrigInfo)
    End Function

    Public ReadOnly Property TriggerName(ByVal TriggerNumber As Integer) As String Implements HomeSeerAPI.IPlugInAPI.TriggerName
        Get
            Return plugin.TriggerName(TriggerNumber)
        End Get
    End Property

    Public Function TriggerProcessPostUI(ByVal PostData As System.Collections.Specialized.NameValueCollection, _
                                         ByVal TrigInfoIn As HomeSeerAPI.IPlugInAPI.strTrigActInfo) As HomeSeerAPI.IPlugInAPI.strMultiReturn Implements HomeSeerAPI.IPlugInAPI.TriggerProcessPostUI
        Return plugin.TriggerProcessPostUI(PostData, TrigInfoIn)
    End Function

    Public ReadOnly Property TriggerCount As Integer Implements HomeSeerAPI.IPlugInAPI.TriggerCount
        Get
            Return plugin.TriggerCount
        End Get
    End Property

    Function SupportsConfigDevice() As Boolean Implements IPlugInAPI.SupportsConfigDevice
        Return plugin.SupportsConfigDevice
    End Function

    Function SupportsConfigDeviceAll() As Boolean Implements IPlugInAPI.SupportsConfigDeviceAll
        Return False
    End Function

    Public Function SupportsAddDevice() As Boolean Implements HomeSeerAPI.IPlugInAPI.SupportsAddDevice
        Return False
    End Function

    Function ConfigDevicePost(ByVal ref As Integer, ByVal data As String, ByVal user As String, ByVal userRights As Integer) As Enums.ConfigDevicePostReturn Implements IPlugInAPI.ConfigDevicePost
        Return plugin.ConfigDevicePost(ref, data, user, userRights)
    End Function

    Function ConfigDevice(ByVal ref As Integer, ByVal user As String, ByVal userRights As Integer, newDevice As Boolean) As String Implements IPlugInAPI.ConfigDevice
        Return plugin.ConfigDevice(ref, user, userRights, newDevice)
    End Function

    Public Sub SpeakIn(device As Integer, txt As String, w As Boolean, host As String) Implements HomeSeerAPI.IPlugInAPI.SpeakIn

    End Sub
End Class

