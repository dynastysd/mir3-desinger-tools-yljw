Attribute VB_Name = "信息"
'Callback for g1lc2 getLabel
Sub onGetVersionTog1lc2(control As IRibbonControl, ByRef returnedVal)
    Dim info  As excelInfo
    Set info = New excelInfo
    fPath = ActiveWorkbook.path & "\" & ActiveWorkbook.name
    If IsLegalExcel_(fPath) Then
        info.InitByPath (fPath)
        returnedVal = info.version
    Else
        returnedVal = "非配置目录"
    End If
End Sub
