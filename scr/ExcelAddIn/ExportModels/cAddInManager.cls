VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "cAddInManager"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False

'此代码适用于VBA，请勿在VB.net中使用
Option Explicit

' Add-In 名称
Private add_in_name As String

' Add-In 版本
Private add_in_version As String

' Excel Add-In 文件路径
Private excel_add_in_folder_path As String

' 安装 Add-In
Sub Install(add_in_name_ As String, version As String)
    On Error GoTo ErrorHandler
    
    ' Init variables
    add_in_name = add_in_name_
    add_in_version = version
    excel_add_in_folder_path = "C:\Users\" & Environ("username") & "\AppData\Roaming\Microsoft\AddIns"

    ' Exit if open from addin folder
    If ThisWorkbook.path = excel_add_in_folder_path Then Exit Sub
    
    ' If add_in 存在, 重装 or 不重装
    If AddInExists Then
        If MsgBox("此加载工具(" & add_in_name & ") 已经安装, 您想升级么?", vbYesNo) = vbYes Then
            
            ' 卸载Add-In
            '此句即为卸载功能，需要实现卸载功能的知sir可以直接增加Uninstall实现哦。
            Application.AddIns(add_in_name).Installed = False
            
            ' 安装Add-In
            Call InstallAddIn("update")
            
            ' 提醒更新成功
            MsgBox "恭喜您! 加载工具(" & add_in_name & ") 更新到版本 " & add_in_version, vbInformation
        End If
    
    ' If Add-In 尚不存在则安装
    Else
        If MsgBox("您愿意安装加载工具(" & add_in_name & ")吗?", vbYesNo) = vbYes Then
            
            ' 安装Add-In
            Call InstallAddIn
            
            ' 提醒安装成功
            MsgBox "恭喜您! 加载工具(" & add_in_name & " " & add_in_version & ") 完成安装!", vbInformation
        End If
    End If
    
    ' 关闭本文档
    ThisWorkbook.Close False
    Exit Sub
ErrorHandler:
    '错误的情况显示错误信息
    MsgBox Err.Description, vbCritical
    ThisWorkbook.Close False
End Sub

' 安装 Add-In
Private Sub InstallAddIn(Optional handle As String = "install")
    
    ' 复制  Add-In 到地址
    Dim add_in_path As String: add_in_path = excel_add_in_folder_path & "\" & add_in_name & ".xlam"
    With CreateObject("Scripting.FileSystemObject")
        .CopyFile ThisWorkbook.FullName, add_in_path, True
    End With
    
    ' If there are no active sheets, there will be an error when installing Add-In
    If Not HasActiveWorkbook Then Workbooks.Add
    
    ' Install
    Application.AddIns.Add(add_in_path).Installed = True
End Sub

' 如果Addin已经存在
Private Property Get AddInExists() As Boolean
    If add_in_name = "" Then AddInExists = False: Exit Property
    
    '对每一个Application的Add_in 轮询
    Dim add_in As AddIn
    For Each add_in In Application.AddIns
        If add_in.Title = add_in_name Then
            AddInExists = True
            Exit For
        End If
    Next
End Property

' 检查是否没有激活的文档
Private Property Get HasActiveWorkbook() As Boolean
    On Error GoTo ErrorHandler
    
    Dim value As String: value = ActiveSheet.Range("A1").value
    HasActiveWorkbook = True
    
    Exit Property
ErrorHandler:
    HasActiveWorkbook = False
End Property
