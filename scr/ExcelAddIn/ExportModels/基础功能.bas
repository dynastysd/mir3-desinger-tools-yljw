Attribute VB_Name = "基础功能"
Sub ExportOneWithArgs(Optional iExcelCongName As String = "", Optional iSeverPath As String = "", Optional iClientPath As String = "")

params = " onlySheets:" & ActiveSheet.name
If (iExcelCongName <> "") Then
    params = params & " configExcelname:" & iExcelCongName
End If
If (iSeverPath <> "") Then
    params = params & " serverPath:" & iSeverPath
End If
If (iClientPath <> "") Then
    params = params & " clientPath:" & clientPath
End If

ActiveWorkbook.Save


toolpath = Left(ActiveWorkbook.path, InStr(ActiveWorkbook.path, "documents") + 9) & "2.配置导出工具\"

If IsLegalExcel_(ActiveWorkbook.path) = False Then
    MsgBox ("当前excel文件不在配置目录")
    Exit Sub
End If

If VbaGlobal.setCon.isSilentExport = True Then
    ChDrive Left(toolpath, 1)
    ChDir toolpath
    Shell "ExcelExport.exe " & params
Else
Set fs = CreateObject("Scripting.FileSystemObject")
Set batR = fs.CreateTextFile(toolpath & "~tempRun.bat", True)
batR.WriteLine "cd /d " & toolpath
batR.WriteLine "ExcelExport.exe " & params
batR.WriteLine "@echo --------------导出结束--------------"
batR.WriteLine "@echo off"
batR.WriteLine "pause"
batR.Close

WaitFinish = Shell(toolpath & "~tempRun.bat", vbNormalFocus)

Application.Wait (Now + TimeValue("0:00:05"))
Kill toolpath & "~tempRun.bat"
End If

End Sub
Sub ExportOneCore()
    Dim info  As excelInfo
    Set info = New excelInfo
    
    fPath = ActiveWorkbook.path & "\" & ActiveWorkbook.name
    If IsLegalExcel_(fPath) Then
        info.InitByPath (fPath)
    End If
    
    If info.version = "trunk" Then
        ExportOneWithArgs "trunk.导表配置.xlsx"
    Else
        ExportOneWithArgs ".\branchConfigs\" & "branch导表配置." & info.version & ".xlsx"
    End If
End Sub
Sub ExportOneByKey()
    ExportOneCore
End Sub
'Callback for g3b1 onAction
Sub exportOne(control As IRibbonControl)
    ExportOneCore
End Sub
'Callback for g3b2 onAction
Sub exportAll(control As IRibbonControl)
ActiveWorkbook.Save
If IsLegalExcel_(ActiveWorkbook.path) = False Then
    MsgBox ("当前excel文件不在配置目录")
    Exit Sub
End If

toolpath = Left(ActiveWorkbook.path, InStr(ActiveWorkbook.path, "documents") + 9) & "2.配置导出工具\"
ChDrive Left(toolpath, 1)
ChDir toolpath
Shell toolpath & "导出全部并上传[trunk].bat", vbNormalFocus
End Sub
'Callback for g3b3 onAction
Sub exportOneToS2(control As IRibbonControl)
    ExportOneWithArgs "trunk.导表配置.xlsx", iSeverPath:="..\1.游戏配置\trunk\服务器上传配置内网2服\"
End Sub

'Callback for g3b4 onAction
Sub exportOneToS3(control As IRibbonControl)
    ExportOneWithArgs "trunk.导表配置.xlsx", iSeverPath:="..\1.游戏配置\trunk\服务器上传配置内网3服\"
End Sub
'Callback for b1 getKeytip
Sub onGetSingelExpKey(control As IRibbonControl, ByRef returnedVal)
    returnedVal = "v"
End Sub
