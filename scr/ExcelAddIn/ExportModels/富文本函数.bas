Attribute VB_Name = "���ı�����"
Function RichText(text As String, Optional color As String = "ffffff", Optional isEndline As Boolean = False)
    RichText = "<color color="
    RichText = RichText & color & ">"
    RichText = RichText & text & "</color>"
    If isEndline Then
          RichText = RichText & "</br>"
    End If
End Function
Function RichTextSize(text As String, Optional color As String = "ffffff", Optional size As Integer = 18, Optional isEndline As Boolean = False)
    RichTextSize = "<color color=" & color
    RichTextSize = RichTextSize & " size=" & size
    RichTextSize = RichTextSize & ">"
    RichTextSize = RichTextSize & text & "</color>"
    If isEndline Then
          RichTextSize = RichTextSize & "</br>"
    End If
End Function
Function ���ı�(text As String, Optional color As String = "ffffff", Optional isEndline As Boolean = False)
    ���ı� = RichText(text, color, isEndline)
End Function
Function ��()
    �� = RichText("��", "5f73e1")
End Function
Function ��()
    �� = RichText("��", "3fbaa1")
End Function
Function ʥ()
    ʥ = RichText("ʥ", "3fbaa1")
End Function
Function ��()
    �� = RichText("��", "ca1214")
End Function
Function ��()
    �� = RichText("��", "c9ab42")
End Function
Function ��()
    �� = RichText("��", "c37344")
End Function
Function ��()
    �� = RichText("��", "af5ad2")
End Function
Function ����()
    ���� = "</br>"
End Function
Function �Ƽ�Ԫ��(irange)
eles = split(irange.value, "��")
For Each ele In eles
    If ele = "��" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ��() & "��"
    ElseIf ele = "��" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ��() & "��"
    ElseIf ele = "ʥ" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ʥ() & "��"
    ElseIf ele = "��" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ��() & "��"
    ElseIf ele = "��" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ��() & "��"
    ElseIf ele = "��" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ��() & "��"
    ElseIf ele = "��" Then
        �Ƽ�Ԫ�� = �Ƽ�Ԫ�� & ��() & "��"
    End If
Next
�Ƽ�Ԫ�� = Left(�Ƽ�Ԫ��, Len(�Ƽ�Ԫ��) - 1)
End Function

