#define mir3
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ExcelExport {
    public static class MyExtensions {
        private static DataFormatter df = new DataFormatter(CultureInfo.CurrentCulture);

        public static string GetCellStrVal(ICell cell, CellType t) {
            switch (t) {
                case CellType.Numeric: return cell.NumericCellValue.ToString();
                case CellType.String: return cell.StringCellValue;
                case CellType.Boolean: return cell.BooleanCellValue.ToString();
                case CellType.Error: return cell.ErrorCellValue.ToString();
            }
            return "";
        }

        public static string StrVal(this ICell cell) {
            return GetCellStrVal(cell, cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType);
        }

        public static string CellStrVal(this IRow row, int columnIndex) {
            var cell = row.GetCell(columnIndex);
            if (cell != null) {
                return cell.StrVal();
            }
            return "";
        }

        public static string CellStrVal(this ISheet sheet, int rowIndex, int columnIndex) {
            var row = sheet.GetRow(rowIndex);
            if (row != null) {
                return row.CellStrVal(columnIndex);
            }
            return "";
        }
    }


    class ExportConfig {
        public List<ExportFile> Files  = new List<ExportFile>();
        public string ClientDir;
        public string ServerDir;
        public string ExcelDir;

        public static ExportConfig Load(Dictionary<string,string> inputParams) {
            ExportConfig cfg = new ExportConfig();
            string path = inputParams.ContainsKey("configExcelname") ? inputParams["configExcelname"] : "导表配置.xlsx";

            var wb = Program.OpenWorkbook(path);
            var ds = wb.GetSheet("data");
            for (int i = ds.FirstRowNum; i<= ds.LastRowNum; i++) {
                if (ds.CellStrVal(i, 0) == "客户端路径") {
                    cfg.ClientDir = inputParams.ContainsKey("clientPath") ? inputParams["clientPath"] : ds.CellStrVal(i, 1);
                }
                if (ds.CellStrVal(i, 0) == "服务器配置") {
                    cfg.ServerDir = inputParams.ContainsKey("serverPath") ? inputParams["serverPath"] : ds.CellStrVal(i, 1);
                }
                if (ds.CellStrVal(i, 0) == "配置表目录") {
                    cfg.ExcelDir = inputParams.ContainsKey("excelPath") ? inputParams["excelPath"] : ds.CellStrVal(i, 1);
                }
            }

            ds = wb.GetSheet("ExportConfig");
            var fieldAction = new Dictionary<int, Action<ExportFile, string>>();
            foreach (var c in ds.GetRow(ds.FirstRowNum).Cells) {
                if (c.StrVal() == "Id") {
                    fieldAction.Add(c.ColumnIndex, (n, val) => n.Id = val);
                }
                if (c.StrVal() == "Tips") {
                    fieldAction.Add(c.ColumnIndex, (n, val) => n.Tips = val);
                }
                if (c.StrVal() == "PathOfExcel") {
                    fieldAction.Add(c.ColumnIndex, (n, val) => n.PathOfExcel = val);
                }
                if (c.StrVal() == "NameOfWorksheet") {
                    fieldAction.Add(c.ColumnIndex, (n, val) => n.NameOfWorksheet = val);
                }
                if (c.StrVal() == "BeginMakr") {
                    fieldAction.Add(c.ColumnIndex, (n, val) => n.BeginMakr = val);
                }
                if (c.StrVal() == "IsExport") {
                    fieldAction.Add(c.ColumnIndex, (n, val) => n.IsExport = int.Parse(val) != 0);
                }
                if (c.StrVal().StartsWith("ExportPath")) {
                    int subIndex = int.Parse(c.StrVal().Substring("ExportPath".Length));
                    fieldAction.Add(c.ColumnIndex, (n, val) => {
                        if (string.IsNullOrWhiteSpace(val)) return;
                        if (!n.Export.ContainsKey(subIndex)) { n.Export.Add(subIndex, new ExportEntry()); }
                        n.Export[subIndex].Path = val;
                    });
                }
                if (c.StrVal().StartsWith("ExportType")) {
                    int subIndex = int.Parse(c.StrVal().Substring("ExportType".Length));
                    fieldAction.Add(c.ColumnIndex, (n, val) => {
                        if (string.IsNullOrWhiteSpace(val)) return;
                        if (!n.Export.ContainsKey(subIndex)) { n.Export.Add(subIndex, new ExportEntry()); }
                        n.Export[subIndex].Type = val;
                    });
                }
                if (c.StrVal().StartsWith("ExportParams")) {
                    int subIndex = int.Parse(c.StrVal().Substring("ExportParams".Length));
                    fieldAction.Add(c.ColumnIndex, (n, val) => {
                        if (string.IsNullOrWhiteSpace(val)) return;
                        if (!n.Export.ContainsKey(subIndex)) { n.Export.Add(subIndex, new ExportEntry()); }
                        n.Export[subIndex].Params = val;
                    });
                }
                if (c.StrVal().StartsWith("ExportEncoding")) {
                    int subIndex = int.Parse(c.StrVal().Substring("ExportEncoding".Length));
                    fieldAction.Add(c.ColumnIndex, (n, val) => {
                        if (string.IsNullOrWhiteSpace(val)) return;
                        if (!n.Export.ContainsKey(subIndex)) { n.Export.Add(subIndex, new ExportEntry()); }
                        n.Export[subIndex].Encoding = val;
                    });
                }
            }

            for (int i = ds.FirstRowNum + 1; i <= ds.LastRowNum; i++) {
                if (ds.GetRow(i) == null) continue;

                ExportFile file = null;
                foreach (var cell in ds.GetRow(i).Cells) {
                    if (fieldAction.ContainsKey(cell.ColumnIndex)) {
                        if (file == null) file = new ExportFile();
                        fieldAction[cell.ColumnIndex].Invoke(file, cell.StrVal());
                    }
                }

                if (file != null) {
                    cfg.Files.Add(file);
                }
            }

            return cfg;
        }
    }

    public class ExportFile {
        public string Id;
        public string Tips;
        public string PathOfExcel;
        public string NameOfWorksheet;
        public string BeginMakr;
        public bool IsExport;
        public Dictionary<int, ExportEntry> Export = new Dictionary<int, ExportEntry>();
    }

    public class ExportEntry {
        public string Path;
        public string Type;
        public string Params;
        public string Encoding;
    }

    class TableInfo {
        public string Name;
        public string Database;
        public List<ColumnInfo> Columns;
    }

    class ColumnInfo {
        public string Name;
        public string Type;
        public long StrLenMax;
        public bool IsPriKey;
        public bool IsUniKey;
        public bool IsNullable;

        [JsonIgnore]
        private FieldType _fieldType = FieldType.Unknown;
        [JsonIgnore]
        public FieldType FieldType {
            get {
                if (_fieldType != FieldType.Unknown) {
                    return _fieldType;
                }
                _fieldType = ParseFieldType(Type);
                return _fieldType;
            }
        }

        public static FieldType ParseFieldType(string val)
        {
            switch (val)
            {
                case "int8": return FieldType.Int8;
                case "int16": return FieldType.Int16;
                case "int32": return FieldType.Int32;
                case "int48": return FieldType.Int48;
                case "int64": return FieldType.Int64;
                case "uint8": return FieldType.UInt8;
                case "uint16": return FieldType.UInt16;
                case "uint32": return FieldType.UInt32;
                case "uint48": return FieldType.UInt48;
                case "uint64": return FieldType.UInt64;
                case "string": return FieldType.String;
                case "date": return FieldType.Date;
                case "datetime": return FieldType.Datetime;
            }
            throw new Exception($"invalid FieldType {val}");
        }
    }

    public enum FieldType
    {
        Unknown,
        Int8,
        Int16,
        Int32,
        Int48,
        Int64,
        UInt8,
        UInt16,
        UInt32,
        UInt48,
        UInt64,
        String,
        Date,
        Datetime,
    }

    

    public class Program {
        private static Dictionary<string, TableInfo> dictTableInfo;
        static TableInfo GetTableInfo(string name) {
            return dictTableInfo.ContainsKey(name) ? dictTableInfo[name] : null;
        }

        static void LoadTableInfo() {
            dictTableInfo = JsonConvert.DeserializeObject<TableInfo[]>(File.ReadAllText("DBSchema.json")).ToDictionary(n => n.Name);

            foreach (var tab in dictTableInfo.Values) {
                foreach (var col in tab.Columns) {
                    var ft = col.FieldType;
                }
            }
        }

        public static IWorkbook OpenWorkbook(string path) {
            string ext = Path.GetExtension(path).ToLower();
            if (ext == ".xlsx") {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    return new XSSFWorkbook(fs);
                }
            }
            else if (ext == ".xls") {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    return new HSSFWorkbook(fs);
                }
            }
            else {
                throw new Exception($"不支持的文件类型 {path}");
            }
        }

        static ISheet OpenExcelSheet(string path, string sheet, bool throwIfNotFound = true) {
            var wb = OpenWorkbook(path);
            var ds = wb.GetSheet(sheet);
            if (throwIfNotFound && ds == null) {
                throw new Exception($"Excel文件{path}中找不到工作表{sheet}");
            }
            return ds;
        }

        static bool CheckDataForMark(ISheet ds, string mark, out int rowStart, out int colStart, out int colEnd) {
            if (string.IsNullOrWhiteSpace(mark)) {
                throw new Exception("无效的起始标记符");
            }
            rowStart = -1;
            colStart = -1;
            colEnd = -1;
            for (int i = ds.FirstRowNum; i <= ds.LastRowNum; i++) {
                var row = ds.GetRow(i);
                if (row == null) continue;

                if (rowStart < 0 && colStart < 0) {
                    for (int j = row.FirstCellNum; j < row.LastCellNum; j++) {
                        var cell = row.GetCell(j);
                        if (cell == null) continue;
                        if (cell.CellType == CellType.String && cell.StringCellValue == mark) {
                            rowStart = i + 1;
                            colStart = j + 1;
                            break;
                        }
                    }
                }
                else {
                    colEnd = Math.Max(colEnd, row.LastCellNum);
                }
            }
            return rowStart >= 0;
        }

        static IEnumerable<int> RangeToInc(int from, int to) {
            return Enumerable.Range(from, to - from + 1);
        }

        static IEnumerable<int> RangeToExc(int from, int to) {
            return Enumerable.Range(from, to - from);
        }

        static IEnumerable<IEnumerable<string>> OpenFileFormat1(string path, string sheet, string mark) {
            var ds = OpenExcelSheet(path, sheet);
            if (CheckDataForMark(ds, mark, out int rowStart, out int colStart, out int colEnd)) {
                return RangeToInc(rowStart, ds.LastRowNum)
                    .Select(i => ds.GetRow(i))
                    .Where(n => n != null)
                    .Select(n => RangeToExc(colStart, colEnd).Select(nn => n.CellStrVal(nn)));
            }
            else {
                throw new Exception($"{path}[{sheet}] 没找到指定的起始符 {mark}");
            }
        }

        static IEnumerable<Tuple<Dictionary<string, string>, int>> OpenFileFormat2(string path, string sheet, string mark) {
            var ds = OpenExcelSheet(path, sheet);
            if (CheckDataForMark(ds, mark, out int rowStart, out int colStart, out int colEnd)) {
                if (rowStart > ds.LastRowNum) {
                    throw new Exception();
                }

                var headerRow = ds.GetRow(rowStart);
                var dictColName = RangeToExc(colStart, colEnd).ToDictionary(n => n, n => headerRow.CellStrVal(n));

                return RangeToInc(rowStart, ds.LastRowNum)
                    .Select(i => ds.GetRow(i))
                    .Where(n => n != null)
                    .Select(n => Tuple.Create(
                        RangeToExc(colStart, colEnd).ToDictionary(nn => dictColName[nn], nn => n.CellStrVal(nn)), 
                        n.RowNum)
                    );
            }
            else {
                throw new Exception($"{path}[{sheet}] 没找到指定的起始符 {mark}");
            }
        }

        static void OpenFileWrite(string path, string encoding, Action<StreamWriter> func) {
            var enc = System.Text.Encoding.GetEncoding(encoding);
            if (enc.Equals(System.Text.Encoding.UTF8)) {
                enc = new System.Text.UTF8Encoding(false);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs, enc)) {
                func(sw);
            }
        }
        
        static void ExportType1(string from, string to, string sheet, string mark, string encodingName, char sep) {
            var data = OpenFileFormat1(from, sheet, mark);
            OpenFileWrite(to, encodingName, sw => {
                var writeLineCount = 0;
                foreach (var tokens in data) {
#if mir3
                    //遇到第一列为空时，停止导出
                    if (tokens.First() == "")
                        break;
#endif
                    var line = string.Join(sep, tokens);
                    if (!string.IsNullOrWhiteSpace(line)) {
                        if (writeLineCount > 0) {
                            sw.WriteLine();
                        }
                        sw.Write(line);
                        writeLineCount += 1;
                    }
                }
            });
        }

        static Regex dateRegex = new Regex(@"^(\d{4})-(\d{1,2})-(\d{1,2})$");
        static Regex datetimeRegex = new Regex(@"^(\d{4})-(\d{1,2})-(\d{1,2}) (\d{1,2}):(\d{1,2}):(\d{1,2})$");

        static object ParseFieldValue(FieldType type, long StrLenMax, bool isNullable, string value) {
            if (string.IsNullOrEmpty(value)) {
                if (isNullable) {
                    return null;
                }
            }
            switch (type) {
                case FieldType.Int8: return SByte.Parse(value);
                case FieldType.Int16: return Int16.Parse(value);
                case FieldType.Int32: return Int32.Parse(value);
                case FieldType.Int64: return Int64.Parse(value);
                case FieldType.UInt8: return Byte.Parse(value);
                case FieldType.UInt16: return UInt16.Parse(value);
                case FieldType.UInt32: return UInt32.Parse(value);
                case FieldType.UInt64: return UInt64.Parse(value);
                case FieldType.String: {
                    if (value == null) throw new NullReferenceException();
                    if (value.Length > StrLenMax) {
                        throw new OverflowException($"字符串{value}超出最大允许长度{StrLenMax}");
                    }
                    return value;
                }
                case FieldType.Int48: {
                    const long min = -8388608;
                    const long max =  8388607;
                    var val = Int64.Parse(value);
                    if (val < min || val > max) {
                        throw new OverflowException($"数值{val}超出int48范围 {min}-{max}");
                    }
                    return val;
                }
                case FieldType.UInt48: {
                    const ulong min = 0;
                    const ulong max = 16777215;
                    var val = UInt64.Parse(value);
                    if (val < min || val > max) {
                        throw new OverflowException($"数值{val}超出uint48范围 {min}-{max}");
                    }
                    return val;
                }
                case FieldType.Date: {
                    if (!dateRegex.IsMatch(value)) {
                        throw new Exception($"{value}不是正确的日期格式");
                    }
                    return value;
                }
                case FieldType.Datetime: {
                    if (!datetimeRegex.IsMatch(value)) {
                        throw new Exception($"{value}不是正确的时间格式");
                    }
                    return value;
                }
                default: throw new ArgumentException("不支持的字段类型", "type");
            }
        }

        static void ExportType2(string from, string to, string sheet, string mark, string encodingName, string tab) {
            var tabInfo = GetTableInfo(tab);
            if (tabInfo == null) {
                throw new Exception($"找不到MySQL表配置{tab}");
            }

            var tabData = OpenFileFormat2(from, sheet, mark);
            var firstRow = tabData.FirstOrDefault();
            if (firstRow != null) {
                tabData = tabData.Skip(1);
                var dictColumn = tabInfo.Columns.ToDictionary(n => n.Name);
                foreach (var name in dictColumn.Keys) {
                    if (!firstRow.Item1.ContainsKey(name)) {
                        throw new Exception($"{from}[{sheet}]中缺少字段{name}");
                    }
                }
                foreach (var name in firstRow.Item1.Keys) {
                    if (!dictColumn.ContainsKey(name)) {
                        throw new Exception($"{from}[{sheet}]中有没有配置的字段{name}");
                    }
                }
            }
            
            OpenFileWrite(to, encodingName, sw => {
                sw.WriteLine("SET NAMES utf8mb4;");
                sw.WriteLine($"USE {tabInfo.Database};");
                sw.WriteLine($"DELETE FROM {tabInfo.Name};");
                var values = new List<string>();
                var uniqueSets = new Dictionary<string, HashSet<object>>();
                foreach (var row in tabData) {
                    values.Clear();
                    foreach (var colInfo in tabInfo.Columns) {
                        var strVal = row.Item1[colInfo.Name];
                        try {
                            object val = ParseFieldValue(colInfo.FieldType, colInfo.StrLenMax, colInfo.IsNullable, strVal);
                            if (colInfo.IsPriKey || colInfo.IsUniKey) {
                                if (!uniqueSets.ContainsKey(colInfo.Name)) {
                                    uniqueSets.Add(colInfo.Name, new HashSet<object>());
                                }
                                if (uniqueSets[colInfo.Name].Contains(val)) {
                                    throw new Exception("字段值重复");
                                }
                                uniqueSets[colInfo.Name].Add(val);
                            }

                            if (val == null) {
                                values.Add("NULL");
                            }
                            else if (colInfo.FieldType == FieldType.String || colInfo.FieldType == FieldType.Date || colInfo.FieldType == FieldType.Datetime) {
                                values.Add($"'{val}'");
                            }
                            else {
                                values.Add(val.ToString());
                            }
                        }
                        catch (Exception ex) {
                            throw new Exception($"{from}[{sheet}]({row.Item2 + 1}:{colInfo.Name})数据表字段解析错误 - {strVal} - {ex.Message}");
                        }
                    }
                    var strValues = string.Join(", ", values);
                    sw.WriteLine($"REPLACE INTO `{tabInfo.Name}` VALUES ({strValues});");
                }
            });
        }

        static void ExportType3(string pyName,string excelName , string sheetName, string mark, string exportPath)
        {
            try
            {
                using (Process pyProcess = new Process())
                {
                    var ds = OpenExcelSheet(excelName, sheetName);
                    if (CheckDataForMark(ds, mark, out int rowStart, out int colStart, out int colEnd))
                    {
                        pyProcess.StartInfo.UseShellExecute = false;
                        pyProcess.StartInfo.FileName = pyName;
                        pyProcess.StartInfo.Arguments = excelName + " " + sheetName +" "+ rowStart + " "+ colStart + " " + exportPath;
                        pyProcess.StartInfo.CreateNoWindow = false;
                        pyProcess.Start();
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        static void ExportType4(string sourceExcelPath, ExportFile exportFile, ExportEntry exportEntry,string exportPath)
        {
            string form = Path.Join(Path.GetDirectoryName(sourceExcelPath), exportFile.NameOfWorksheet+".xlsx");
            var wb = OpenWorkbook(form);

            Dictionary<string, string> typeParams = new Dictionary<string, string>();
            foreach(string param in exportEntry.Params.Split("|"))
            {
                var temp = param.Split("#");
                typeParams.Add(temp[0], temp[1]);
            }
            foreach (var sheet in wb)
            {
                switch (typeParams["type"])
                {
                    case "1":
                        string to = Path.Join(exportPath, sheet.SheetName + typeParams["ext"]);
                        Console.WriteLine($"导出[{sheet.SheetName}]");
                        ExportType1(form,to, sheet.SheetName, exportFile.BeginMakr, exportEntry.Encoding, (char)int.Parse(typeParams["ascii"]));
                        break;
                    case "3":
                        Console.WriteLine($"导出[{sheet.SheetName}]");
                        ExportType3(typeParams["pyname"], form, sheet.SheetName, exportFile.BeginMakr, exportPath);
                        break;
                    default:
                        Console.WriteLine("不支持的导出类型");
                        break;
                }
            }
        }
        static void Main(string[] args) {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            LoadTableInfo();

            //onlySheets:导出指定sheet的名字
            //configExcelname:导出配置的名字
            //serverPath:导出服务器的路径
            //clientPath:导出客户端的路段
            //excelPath:配置表目录
            Dictionary<string,string> iParams = new Dictionary<string, string>();
            foreach(string arg in args)
            {
                var temp = arg.Split(":");
                iParams.Add(temp[0], temp[1]);
            }

            var cfg = ExportConfig.Load(iParams);

            HashSet<string> onlySheetNames = null;
            if (iParams.ContainsKey("onlySheets")) {
                onlySheetNames = new HashSet<string>(iParams["onlySheets"].Split("|"));
                Console.WriteLine($"导出指定Sheet {string.Join(' ', args)}");
            }
            else {
                Console.WriteLine($"导出全部");
            }


            foreach (var one in cfg.Files) {
                if (!one.IsExport) {
                    continue;
                }
                if (onlySheetNames != null && !onlySheetNames.Contains(one.NameOfWorksheet)) {
                    continue;
                }

                //导出类型1，用分隔符连接cell值，导出为文本文件
                //导出类型2，导出为写入数据库的脚本，新增配置需要更新DBSchem.json文件
                //导出类型3，导出为客户端用的lua数据，需要调用前端写的python脚本
                //导出类型4，嵌套，批量导出excel里的sheet，可以指定用类型1或类型3
                string pathExcel = Path.Join(cfg.ExcelDir, one.PathOfExcel);
                foreach (var kv in one.Export) {
                    if (kv.Value.Type == "1") {
                        string pathOut = Path.Join(kv.Key == 1 ? cfg.ServerDir : cfg.ClientDir, kv.Value.Path);
                        Console.WriteLine($"{Path.GetFullPath (pathExcel)}[{one.NameOfWorksheet}] => {Path.GetFullPath(pathOut)}");
                        ExportType1(pathExcel, pathOut, one.NameOfWorksheet, one.BeginMakr, kv.Value.Encoding, (char)int.Parse(kv.Value.Params));
                    }
                    else if (kv.Value.Type == "2") {
                        string pathOut = Path.Join(kv.Key == 1 ? cfg.ServerDir : cfg.ClientDir, kv.Value.Path);
                        Console.WriteLine($"{Path.GetFullPath(pathExcel)}[{one.NameOfWorksheet}] => {Path.GetFullPath(pathOut)}");
                        ExportType2(pathExcel, pathOut, one.NameOfWorksheet, one.BeginMakr, kv.Value.Encoding, kv.Value.Params);
                    }
                    else if (kv.Value.Type == "3")
                    {
                        string pathOut = Path.Join(kv.Key == 1 ? cfg.ServerDir : cfg.ClientDir, kv.Value.Path);
                        //lua文件名仅做展示，不会影响导出文件名
                        string luaFileName = "DB_"+one.NameOfWorksheet.Split("#")[1]+".lua";
                        Console.WriteLine($"{Path.GetFullPath(pathExcel)}[{one.NameOfWorksheet}] => {Path.Join( Path.GetFullPath(pathOut),luaFileName)}");
                        ExportType3(kv.Value.Params, pathExcel, one.NameOfWorksheet,one.BeginMakr, pathOut);
                    }
                    else if (kv.Value.Type == "4")
                    {
                        string pathOut = Path.Join(kv.Key == 1 ? cfg.ServerDir : cfg.ClientDir, kv.Value.Path);
                        Console.WriteLine($"{Path.GetFullPath(pathExcel)}[{one.NameOfWorksheet}]全部导出-------");
                        ExportType4(pathExcel, one, kv.Value, pathOut);
                        
                    }
                }
            }
        }
    }
}
