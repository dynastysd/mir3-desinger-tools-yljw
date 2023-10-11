Attribute VB_Name = "富文本函数"
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
Function 富文本(text As String, Optional color As String = "ffffff", Optional isEndline As Boolean = False)
    富文本 = RichText(text, color, isEndline)
End Function
Function 雷()
    雷 = RichText("雷", "5f73e1")
End Function
Function 冰()
    冰 = RichText("冰", "3fbaa1")
End Function
Function 圣()
    圣 = RichText("圣", "3fbaa1")
End Function
Function 火()
    火 = RichText("火", "ca1214")
End Function
Function 风()
    风 = RichText("风", "c9ab42")
End Function
Function 暗()
    暗 = RichText("暗", "c37344")
End Function
Function 幻()
    幻 = RichText("幻", "af5ad2")
End Function
Function 换行()
    换行 = "</br>"
End Function
Function 推荐元素(irange)
eles = split(irange.value, "、")
For Each ele In eles
    If ele = "雷" Then
        推荐元素 = 推荐元素 & 雷() & "、"
    ElseIf ele = "冰" Then
        推荐元素 = 推荐元素 & 冰() & "、"
    ElseIf ele = "圣" Then
        推荐元素 = 推荐元素 & 圣() & "、"
    ElseIf ele = "火" Then
        推荐元素 = 推荐元素 & 火() & "、"
    ElseIf ele = "风" Then
        推荐元素 = 推荐元素 & 风() & "、"
    ElseIf ele = "暗" Then
        推荐元素 = 推荐元素 & 暗() & "、"
    ElseIf ele = "幻" Then
        推荐元素 = 推荐元素 & 幻() & "、"
    End If
Next
推荐元素 = Left(推荐元素, Len(推荐元素) - 1)
End Function

