VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "cAddInManager"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False

'�˴���������VBA��������VB.net��ʹ��
Option Explicit

' Add-In ����
Private add_in_name As String

' Add-In �汾
Private add_in_version As String

' Excel Add-In �ļ�·��
Private excel_add_in_folder_path As String

' ��װ Add-In
Sub Install(add_in_name_ As String, version As String)
    On Error GoTo ErrorHandler
    
    ' Init variables
    add_in_name = add_in_name_
    add_in_version = version
    excel_add_in_folder_path = "C:\Users\" & Environ("username") & "\AppData\Roaming\Microsoft\AddIns"

    ' Exit if open from addin folder
    If ThisWorkbook.path = excel_add_in_folder_path Then Exit Sub
    
    ' If add_in ����, ��װ or ����װ
    If AddInExists Then
        If MsgBox("�˼��ع���(" & add_in_name & ") �Ѿ���װ, ��������ô?", vbYesNo) = vbYes Then
            
            ' ж��Add-In
            '�˾伴Ϊж�ع��ܣ���Ҫʵ��ж�ع��ܵ�֪sir����ֱ������Uninstallʵ��Ŷ��
            Application.AddIns(add_in_name).Installed = False
            
            ' ��װAdd-In
            Call InstallAddIn("update")
            
            ' ���Ѹ��³ɹ�
            MsgBox "��ϲ��! ���ع���(" & add_in_name & ") ���µ��汾 " & add_in_version, vbInformation
        End If
    
    ' If Add-In �в�������װ
    Else
        If MsgBox("��Ը�ⰲװ���ع���(" & add_in_name & ")��?", vbYesNo) = vbYes Then
            
            ' ��װAdd-In
            Call InstallAddIn
            
            ' ���Ѱ�װ�ɹ�
            MsgBox "��ϲ��! ���ع���(" & add_in_name & " " & add_in_version & ") ��ɰ�װ!", vbInformation
        End If
    End If
    
    ' �رձ��ĵ�
    ThisWorkbook.Close False
    Exit Sub
ErrorHandler:
    '����������ʾ������Ϣ
    MsgBox Err.Description, vbCritical
    ThisWorkbook.Close False
End Sub

' ��װ Add-In
Private Sub InstallAddIn(Optional handle As String = "install")
    
    ' ����  Add-In ����ַ
    Dim add_in_path As String: add_in_path = excel_add_in_folder_path & "\" & add_in_name & ".xlam"
    With CreateObject("Scripting.FileSystemObject")
        .CopyFile ThisWorkbook.FullName, add_in_path, True
    End With
    
    ' If there are no active sheets, there will be an error when installing Add-In
    If Not HasActiveWorkbook Then Workbooks.Add
    
    ' Install
    Application.AddIns.Add(add_in_path).Installed = True
End Sub

' ���Addin�Ѿ�����
Private Property Get AddInExists() As Boolean
    If add_in_name = "" Then AddInExists = False: Exit Property
    
    '��ÿһ��Application��Add_in ��ѯ
    Dim add_in As AddIn
    For Each add_in In Application.AddIns
        If add_in.Title = add_in_name Then
            AddInExists = True
            Exit For
        End If
    Next
End Property

' ����Ƿ�û�м�����ĵ�
Private Property Get HasActiveWorkbook() As Boolean
    On Error GoTo ErrorHandler
    
    Dim value As String: value = ActiveSheet.Range("A1").value
    HasActiveWorkbook = True
    
    Exit Property
ErrorHandler:
    HasActiveWorkbook = False
End Property