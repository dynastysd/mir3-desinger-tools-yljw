Attribute VB_Name = "��Ϣ"
'Callback for g1lc2 getLabel
Sub onGetVersionTog1lc2(control As IRibbonControl, ByRef returnedVal)
    Dim info  As excelInfo
    Set info = New excelInfo
    fPath = ActiveWorkbook.path & "\" & ActiveWorkbook.name
    If IsLegalExcel_(fPath) Then
        info.InitByPath (fPath)
        returnedVal = info.version
    Else
        returnedVal = "������Ŀ¼"
    End If
End Sub
