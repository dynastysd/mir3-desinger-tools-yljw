VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "SettingConfig"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Dim pAddinVersion As String
Dim pAllowedInstall As Boolean
Dim pAddinName As String

Dim pIsSilentExport As Boolean

Dim pCustBtnName1 As String
Dim pCustBtnCmd1 As String
Dim pCustBtnName2 As String
Dim pCustBtnCmd2 As String
Dim pCustBtnName3 As String
Dim pCustBtnCmd3 As String

Dim pExportKey As String
'############################################################
Property Let ExportKey(setV As String)
    pExportKey = setV
End Property
Property Get ExportKey() As String
    ExportKey = pExportKey
End Property
'custom button name and cmds
Property Let CustBtnName1(setV As String)
    pCustBtnName1 = setV
End Property
Property Get CustBtnName1() As String
    CustBtnName1 = pCustBtnName1
End Property
Property Let CustBtnCmd1(setV As String)
    pCustBtnCmd1 = setV
End Property
Property Get CustBtnCmd1() As String
    CustBtnCmd1 = pCustBtnCmd1
End Property
Property Let CustBtnName2(setV As String)
    pCustBtnName2 = setV
End Property
Property Get CustBtnName2() As String
    CustBtnName2 = pCustBtnName2
End Property
Property Let CustBtnCmd2(setV As String)
    pCustBtnCmd2 = setV
End Property
Property Get CustBtnCmd2() As String
    CustBtnCmd2 = pCustBtnCmd2
End Property
Property Let CustBtnName3(setV As String)
    pCustBtnName3 = setV
End Property
Property Get CustBtnName3() As String
    CustBtnName3 = pCustBtnName3
End Property
Property Let CustBtnCmd3(setV As String)
    pCustBtnCmd3 = setV
End Property
Property Get CustBtnCmd3() As String
    CustBtnCmd3 = pCustBtnCmd3
End Property
'############################################################
Property Let isSilentExport(setV As Boolean)
    pIsSilentExport = setV
End Property
Property Get isSilentExport() As Boolean
    isSilentExport = pIsSilentExport
End Property
'############################################################
Private Sub Class_Initialize()

Dim dict
Set dict = CreateObject("Scripting.Dictionary")
ReadLocalConfig dict

End Sub

Private Sub ReadLocalConfig(ByRef dict) 'dict is object Scripting.Dictionary
    configPath = ThisWorkbook.path & "\"
    If IsFileExists_(configPath & "AddInConfig.txt") = False Then
        CreatDefaultConfig
    End If
    
    'get local value
    Set fs = CreateObject("Scripting.FileSystemObject")
    Set configTxt = fs.OpenTextFile(configPath & "AddInConfig.txt")
    Do While configTxt.AtEndOfLine <> True
        configPairs = split(configTxt.ReadLine(), "#")
        dict.Add configPairs(0), configPairs(1)
    Loop
    
    'set local value
    pAddinVersion = ThisVersion
    pAllowedInstall = AllowedInstall
    pAddinName = GetToolName
    pIsSilentExport = dict.item("IsSilentExport")
    
    pCustBtnName1 = dict.item("CustBtnName1")
    pCustBtnCmd1 = dict.item("CustBtnCmd1")
    pCustBtnName2 = dict.item("CustBtnName2")
    pCustBtnCmd2 = dict.item("CustBtnCmd2")
    pCustBtnName3 = dict.item("CustBtnName3")
    pCustBtnCmd3 = dict.item("CustBtnCmd3")
    
    pExportKey = dict.item("ExportKey")
    
End Sub
Private Sub CreatDefaultConfig()
    configPath = ThisWorkbook.path & "\"
    
    Set fs = CreateObject("Scripting.FileSystemObject")
    Set txtConfig = fs.CreateTextFile(configPath & "AddInConfig.txt", True)
    
    txtConfig.WriteLine "Version#" & ThisVersion()
    txtConfig.WriteLine "AllowedInstall#" & AllowedInstall()
    txtConfig.WriteLine "AddinName#" & GetToolName()
    
    txtConfig.WriteLine "IsSilentExport#False"
    
    txtConfig.WriteLine "CustBtnName1#自定义按钮1"
    txtConfig.WriteLine "CustBtnCmd1#"
    txtConfig.WriteLine "CustBtnName2#自定义按钮2"
    txtConfig.WriteLine "CustBtnCmd2#"
    txtConfig.WriteLine "CustBtnName3#自定义按钮3"
    txtConfig.WriteLine "CustBtnCmd3#"
    
    txtConfig.WriteLine "ExportKey#e"
    
    txtConfig.Close
End Sub
Sub WriteLocalConfig()
    
    configPath = ThisWorkbook.path & "\"
    
    Set fs = CreateObject("Scripting.FileSystemObject")
    Set txtConfig = fs.CreateTextFile(configPath & "AddInConfig.txt", True)
    
    txtConfig.WriteLine "Version#" & ThisVersion()
    txtConfig.WriteLine "AllowedInstall#" & AllowedInstall()
    txtConfig.WriteLine "AddinName#" & GetToolName()
    
    txtConfig.WriteLine "IsSilentExport#" & pIsSilentExport
    
    txtConfig.WriteLine "CustBtnName1#" & pCustBtnName1
    txtConfig.WriteLine "CustBtnCmd1#" & pCustBtnCmd1
    txtConfig.WriteLine "CustBtnName2#" & pCustBtnName2
    txtConfig.WriteLine "CustBtnCmd2#" & pCustBtnCmd2
    txtConfig.WriteLine "CustBtnName3#" & pCustBtnName3
    txtConfig.WriteLine "CustBtnCmd3#" & pCustBtnCmd3
    
    txtConfig.WriteLine "ExportKey#" & pExportKey
    
    txtConfig.Close

End Sub
Private Sub Class_Terminate()
    WriteLocalConfig
End Sub
