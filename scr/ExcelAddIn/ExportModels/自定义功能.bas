Attribute VB_Name = "自定义功能"
'Callback for g4b1 getLabel
Sub onGetCustBtnName1(control As IRibbonControl, ByRef returnedVal)
returnedVal = VbaGlobal.setCon.CustBtnName1
End Sub

'Callback for g4b1 onAction
Sub CustBtnCmd1(control As IRibbonControl)
ExecuteCmd VbaGlobal.setCon.CustBtnCmd1
End Sub

'Callback for g4b2 getLabel
Sub onGetCustBtnName2(control As IRibbonControl, ByRef returnedVal)
returnedVal = VbaGlobal.setCon.CustBtnName2
End Sub

'Callback for g4b2 onAction
Sub CustBtnCmd2(control As IRibbonControl)
ExecuteCmd VbaGlobal.setCon.CustBtnCmd2
End Sub

'Callback for g4b3 getLabel
Sub onGetCustBtnName3(control As IRibbonControl, ByRef returnedVal)
returnedVal = VbaGlobal.setCon.CustBtnName3
End Sub

'Callback for g4b3 onAction
Sub CustBtnCmd3(control As IRibbonControl)
ExecuteCmd VbaGlobal.setCon.CustBtnCmd3
End Sub
Sub ExecuteCmd(iCmd As String)

Dim ofs As Object
Set ofs = CreateObject("Scripting.FileSystemObject")

If ofs.FolderExists(iCmd) Then
    Shell "explorer " & iCmd
ElseIf ofs.FileExists(iCmd) Then
    ext = ofs.GetExtensionName(iCmd)

    If ext = "exe" Or ext = "bat" Then
        Shell iCmd
    ElseIf ext = "xlsx" Or ext = "xls" Then
        Shell "excel " & iCmd
    Else
        MsgBox "无法识别拓展名"
    End If
Else
MsgBox "路径不正确"
End If
End Sub

