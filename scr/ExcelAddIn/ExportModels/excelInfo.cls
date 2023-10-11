VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "excelInfo"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False

'game's version,trunk or branch
Dim pGameVersion As String
'config's folder name
Dim pConfigFolderName As String

Dim pWorkBookName As String
Dim pWorkSheetName As String
Private Sub Class_Initialize()
    pGameVersion = "empty"
    pConfigFolderName = "empty"
    pWorkBookName = "empty"
    pWorkSheetName = "empty"
End Sub
'inlit object excelInfo by file's path
Sub InitByPath(ePath As String)
    tags = split(ePath, "\")
    counter = 0
    For Each Tag In tags
        
        Select Case counter
            Case 1
                counter = counter + 1
                pGameVersion = Tag
            Case 2
                counter = counter + 1
                pConfigFolderName = Tag
            Case 3
                counter = counter + 1
                pWorkBookName = Tag
        End Select
        
        If Tag = "1.”Œœ∑≈‰÷√" Then
            counter = 1
        End If
    Next
End Sub
Property Get version()
    version = pGameVersion
End Property
Property Get folder()
    folder = pConfigFolderName
End Property
Property Get bookName()
    bookName = pWorkBookName
End Property
