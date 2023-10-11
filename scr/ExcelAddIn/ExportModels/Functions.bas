Attribute VB_Name = "Functions"
Function IsLegalExcel_(ePath) As Boolean
If InStr(ePath, "documents\1.”Œœ∑≈‰÷√") = 0 Then
    IsLegalExcel_ = False
Else
    IsLegalExcel_ = True
End If
End Function
Function IsFileExists_(ByVal strFileName As String) As Boolean
    Dim objFileSystem As Object
 
    Set objFileSystem = CreateObject("Scripting.FileSystemObject")
    If objFileSystem.FileExists(strFileName) = True Then
        IsFileExists_ = True
    Else
        IsFileExists_ = False
    End If
End Function
