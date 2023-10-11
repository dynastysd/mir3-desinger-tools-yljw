Attribute VB_Name = "��������"
'������ʵ��һЩͨ�õ�vba�������������߻�ʹ��

Function ConnectRangeWithSymbol(inputRange, inputSymbol)
    For Each item In inputRange
        If item.value <> "" Then
            ConnectRangeWithSymbol = ConnectRangeWithSymbol & inputSymbol & item.value
        End If
    Next
    ConnectRangeWithSymbol = right(ConnectRangeWithSymbol, Len(ConnectRangeWithSymbol) - Len(inputSymbol))
End Function
Function CountItemsNum(irange, itemID)
markV1 = "#"
markV2 = "|"

For Each ir In irange
    tItems = split(ir.value, markV2)
    For Each tItem In tItems
        kv = split(tItem, markV1)
        If kv(0) = CStr(itemID) Then
            CountItemsNum = CountItemsNum + kv(1)
        End If
    Next tItem
Next ir
End Function
Function ItemsMul(idata, itemID, mulNum)
markV1 = "#"
markV2 = "|"
itemPairs = split(idata.value, markV2)
For Each itemPair In itemPairs
    item = split(itemPair, markV1)
    If Int(item(0)) = itemID Then
        item(1) = item(1) * mulNum
    End If
    ItemsMul = ItemsMul & item(0) & markV1 & item(1) & markV2
Next itemPair

' del last markV2
ItemsMul = Left(ItemsMul, Len(ItemsMul) - 1)
End Function
Function CellGetValue(cDict)
markV1 = "#"
pairs = split(cDict, markV1)
CellGetValue = pairs(1)
End Function
Function DictSumRange(irange, Optional imarkV1 As String = "#", Optional imarkV2 As String = "|")
markV1 = imarkV1
markV2 = imarkV2
Set dict = CreateObject("Scripting.Dictionary")

For Each irng In irange
    'Debug.Print irng.value
    kvs = split(irng.value, markV2)
    For i = 0 To UBound(kvs)
        'Debug.Print kvs(i)
        kv = split(kvs(i), markV1)
        If dict.exists(kv(0)) Then
            dict.item(kv(0)) = Int(dict.item(kv(0))) + Int(kv(1))
        Else
            dict.Add kv(0), kv(1)
        End If
    Next i
Next irng

For Each key In dict
    'Debug.Print "key:" & key & ", value:" & dict.item(key)
    DictSumRange = DictSumRange & key & markV1 & dict.item(key) & markV2
Next
'del last markV2
DictSumRange = Left(DictSumRange, Len(DictSumRange) - Len(markV2))

End Function

Function DictGetValueByKey(dictRange, key, Optional imarkV1 As String = "#", Optional imarkV2 As String = "|")
markV1 = imarkV1
markV2 = imarkV2
pairs = split(dictRange, markV2)

For Each pair In pairs
    kv = split(pair, markV1)
    If kv(0) = key Then
        DictGetValueByKey = kv(1)
    End If
Next pair

End Function
Function DictSetValueByKey(dictRange, key, setVal, Optional imarkV1 As String = "#", Optional imarkV2 As String = "|")
markV1 = imarkV1
markV2 = imarkV2
pairs = split(dictRange.value, markV2)

For Each pair In pairs
    kv = split(pair, markV1)
    If kv(0) = key Then
         kv(1) = setVal
    End If
    DictSetValueByKey = DictSetValueByKey & kv(0) & markV1 & kv(1) & markV2
Next pair

DictSetValueByKey = Left(DictSetValueByKey, Len(DictSetValueByKey) - 1)

End Function
Function TransVlpData(itemcell, lookupRange, pos, isLookUp)
idata = split(itemcell.value, "#")
If isNumeric(idata(0)) Then
    itemID = Application.WorksheetFunction.VLookup(Int(idata(0)), lookupRange, pos, isLookUp)
Else
    itemID = Application.WorksheetFunction.VLookup(idata(0), lookupRange, pos, isLookUp)
End If
TransVlpData = itemID & "#" & idata(1)
End Function

Function TransXlpMutil(itemcell, lookupRange, findcol, retuncol)
idatas = split(itemcell.value, "|")
For Each idata In idatas
    TransXlpMutil = TransXlpMutil & "|" & TransXlpValue(idata, lookupRange, findcol, retuncol)
Next
    TransXlpMutil = right(TransXlpMutil, Len(TransXlpMutil) - 1)
End Function
Private Function TransXlpValue(ivalue, lookupRange, findcol, retuncol)
idata = split(ivalue, "#")
itemID = xlookup(idata(0), lookupRange, findcol, retuncol)
If (UBound(idata) = 0) Then
    TransXlpValue = itemID
Else
    TransXlpValue = itemID & "#" & idata(1)
End If
End Function
Function TransOpenDay(icell)
opdays = split(icell.value, "|")
dayBegin = split(opdays(0), ":")(0) + 1
dayEnd = split(opdays(1), ":")(0) + 1
If dayEnd = 10000 Then
    dayEnd = "���һ"
End If
TransOpenDay = "������" & dayBegin & "�쵽" & dayEnd & "��"

End Function
Function TransXlpData(itemcell, lookupRange, findcol, retuncol)
idata = split(itemcell.value, "#")
itemID = xlookup(idata(0), lookupRange, findcol, retuncol)
If (IsError(idata(1))) Then
    TransXlpData = itemID
Else
    TransXlpData = itemID & "#" & idata(1)
End If
End Function
Function IsStrEqual(val1, val2)
If isNumeric(val1) Then
    val1 = CStr(val1)
End If
If isNumeric(val2) Then
    val2 = CStr(val2)
End If
IsStrEqual = (val1 = val2)
End Function
'�߰汾��excel����xlookup������ʵ����һ�����װ汾�����Ͱ汾excelʹ��
Function xlookup(fval, irange, ifary, irary)
    aryoffset = irary - ifary
    Dim t As Range
    Set t = irange.Columns(ifary)
    For Each cel In irange.Columns(ifary).item(1).Cells()
        If IsStrEqual(cel.value, fval) Then
           xlookup = cel.offset(RowOffset:=0, ColumnOffset:=aryoffset).value
           Exit For
        End If
    Next
End Function
Function CallLambda(lambdas As String, ParamArray args() As Variant)
    Dim la As stdLambda
    Set la = stdLambda.Create(lambdas)
    CallLambda = la.RunEx(args)
End Function
'������ֵת��Ϊ����������
Function NumToChinese2(irange)
    'NumToChinese2 = WorksheetFunction.Text(irange.value, "[DBNum1][$-804]G/ͨ�ø�ʽ")
    NumToChinese2 = WorksheetFunction.text(irange.value, "[>20][DBNum1];[DBNum1]d")
End Function
Function NumToChinese(irange)
const10 = "ʮ"

nums = StringToCharArray(irange)
result = ""
numwei = UBound(nums) + 1
Select Case numwei
    Case 1
        'value between 1-9
        result = ToChineseSymbol(nums(0))
    Case 2
        If nums(0) = 1 Then
        'value between 10-19
            If nums(1) = 0 Then
                result = const10
            Else
                result = const10 & ToChineseSymbol(nums(1))
            End If
        Else
        'value between 20-99
            If nums(1) = 0 Then
                result = ToChineseSymbol(nums(0)) & const10
            Else
                result = ToChineseSymbol(nums(0)) & const10 & ToChineseSymbol(nums(1))
            End If
        End If
    Case Else
        result = "not support number"
End Select
NumToChinese = result
End Function
Function ToChineseSymbol(ByVal aNum As String) As String
Select Case aNum
    Case 1
        ToChineseSymbol = "һ"
    Case 2
        ToChineseSymbol = "��"
    Case 3
        ToChineseSymbol = "��"
    Case 4
        ToChineseSymbol = "��"
    Case 5
        ToChineseSymbol = "��"
    Case 6
        ToChineseSymbol = "��"
    Case 7
        ToChineseSymbol = "��"
    Case 8
        ToChineseSymbol = "��"
    Case 9
        ToChineseSymbol = "��"
    Case 0
        ToChineseSymbol = "��"
End Select
End Function
Function StringToCharArray(istr)
    strlen = Len(istr)
    
    Dim ary() As String
    For i = 0 To strlen - 1
        If i = 0 Then
            ReDim Preserve ary(0)
        Else
            ReDim Preserve ary(UBound(ary) + 1)
        End If
        ary(i) = Mid(istr, i + 1, 1)
    Next i
    StringToCharArray = ary
End Function
Function StringFormat(istr As String, ParamArray args() As Variant)
    For i = 0 To UBound(args)
        pos = InStr(istr, "%s")
        StringFormat = StringFormat & Left(istr, pos - 1) & args(i)
        istr = Mid(istr, pos + 2, Len(istr))
    Next
        StringFormat = StringFormat & istr
End Function
Function GetLeadingNumbersPos(irng)
    Dim result As String
    Dim i As Integer
    
    inputStr = irng.value
    ' ��ʼ������ַ���
    result = ""
    
    ' ���������ַ�����ÿ���ַ�
    For i = 1 To Len(inputStr)
        Dim currentChar As String
        currentChar = Mid(inputStr, i, 1)
        
        ' ��鵱ǰ�ַ��Ƿ�������
        If isNumeric(currentChar) Then
            ' ��������֣���ӵ�����ַ�����
            result = result & currentChar
        Else
            ' ����������֣�ֹͣ����
            Exit For
        End If
    Next i
    
    ' ���ؽ���ַ���
    GetLeadingNumbersPos = i
End Function
Function BatRen(rngSorc, rngTarg)
    BatRen = "ren " & rngSorc.value & " " & rngTarg.value
End Function
Function BatCopy(rngSorc, rngTarg)
    BatCopy = "copy " & rngSorc.value & " " & rngTarg.value
End Function
