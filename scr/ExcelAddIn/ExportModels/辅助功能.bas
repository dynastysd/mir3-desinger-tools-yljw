Attribute VB_Name = "��������"
'Callback for g5b1 onAction
Sub OnSheetSplitToExcel(control As IRibbonControl)
ActiveWorkbook.Save

params = " SourceSheetName#" & ActiveSheet.name
params = params & " SourceExcelPath#" & ActiveWorkbook.path & "\" & ActiveWorkbook.name
params = params & " ExportExcelPath#" & ActiveWorkbook.path & "\" & ActiveSheet.name & ".xlsx"

toolpath = Left(ActiveWorkbook.path, InStr(ActiveWorkbook.path, "documents") + 9) & "2.���õ�������\"

Shell toolpath & "SheetToExcel.exe" & params, vbNormalFocus
End Sub

