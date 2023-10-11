using System;
using System.IO;
using System.Collections.Generic;
using Excel= ClosedXML.Excel;

namespace SheetToExcel
{
    class Program
    {
        public class SourceSheetConfig
        {
            public int ExportSheetNameCol;
            public Dictionary<int, bool> isExport = new Dictionary<int, bool>() ;

            public static SourceSheetConfig GetConfig(Excel.IXLWorksheet inputSheet)
            {
                SourceSheetConfig config = new SourceSheetConfig();

                foreach(var c in inputSheet.FirstRowUsed().Cells())
                {
                    config.isExport.Add(c.Address.ColumnNumber, c.GetString() == "导出");
                    if (c.GetString() == "SheetName")
                    {
                        config.ExportSheetNameCol = c.Address.ColumnNumber;
                    }
                }

                return config;
            }
        }

        public class SheetWriter
        {
            public int startRow;
            public int startCol;
            public Queue<Excel.IXLCell>[] head;
            public Queue<Queue<Excel.IXLCell>> data = new Queue<Queue<Excel.IXLCell>>();
            public Excel.IXLWorksheet sheet;

            public SheetWriter LoadSheetWriter(Action<SheetWriter> fuc)
            {
                SheetWriter sheetWriter = new SheetWriter();
                startRow = 1;
                startCol = 1;
                fuc(this);
                return sheetWriter;
            }

            void Write()
            {
                //write head data
                for (int i = 0; i < head.Length; i++)
                {
                    int recoder = startCol;
                    var tempQeCopy = head[i].ToArray();
                    for (int j = 0; j < tempQeCopy.Length; j++)
                    {
                        sheet.Cell(startRow, startCol).Value = tempQeCopy[j].Value;
                        startCol++;
                    }
                    startCol = recoder;
                    startRow++;
                }
                //write sheet data
                while (data.Count != 0)
                {
                    int recoder = startCol;
                    var tempQe = data.Dequeue();
                    while (tempQe.Count != 0)
                    {
                        sheet.Cell(startRow, startCol).Value = tempQe.Dequeue().Value;
                        startRow++;
                    }
                    startCol = recoder;
                    startRow++;
                }
            }
        }

        static Dictionary<string,string> GetArgs(string[] inputArgs,string splitMark = "#")
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach(string arg in inputArgs)
            {
                var temp = arg.Split(splitMark);
                data.Add(temp[0], temp[1]);
            }
            return data;
        }
        static void Main(string[] args)
        {
            Dictionary<string,string> param = GetArgs(args);
            Dictionary<string, Queue<Queue<Excel.IXLCell>>> sheets = new Dictionary<string, Queue<Queue<Excel.IXLCell>>>();
            Queue<Queue<Excel.IXLCell>> head = new Queue<Queue<Excel.IXLCell>>();

            if ((! param.ContainsKey("SourceExcelPath")) ||
                (! param.ContainsKey("SourceSheetName")) ||
                (! param.ContainsKey("ExportExcelPath")) )
            {
                foreach(var pair in param)
                {
                    Console.WriteLine($"key:{pair.Key}");
                    Console.WriteLine($"Value:{pair.Value}");
                }
                throw new Exception("参数输入不完整");
            }

            string sourceExcelPath = param["SourceExcelPath"];
            string sourceSheetName = param["SourceSheetName"];
            string exportExcelPath = param["ExportExcelPath"];
            var fsOut = new FileStream(exportExcelPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var fsInput = new FileStream(sourceExcelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sWorkbook = new Excel.XLWorkbook(fsInput);
            var sSheet = sWorkbook.Worksheet(sourceSheetName);
            var ssConfig = SourceSheetConfig.GetConfig(sSheet);

            //split sheet data to object "sheets" and object "head"
            foreach (var row in sSheet.RangeUsed().Rows())
            {
                Queue<Excel.IXLCell> rowsQe = new Queue<Excel.IXLCell>();
                foreach(var cell in row.Cells())
                {
                    if(ssConfig.isExport.ContainsKey(cell.Address.ColumnNumber) &&
                        ssConfig.isExport[cell.Address.ColumnNumber])
                    {
                        rowsQe.Enqueue(cell);
                    }
                }

                //push into head
                if(row.FirstCell().Address.RowNumber >1 
                    && row.FirstCell().Address.RowNumber <= 4)
                {
                    head.Enqueue(rowsQe);
                }
                //push into sheets
                else if (row.FirstCell().Address.RowNumber > 4)
                {
                    string sheetKey = row.Cell(ssConfig.ExportSheetNameCol).GetString();
                    if (sheets.ContainsKey(sheetKey))
                    {
                        sheets[sheetKey].Enqueue(rowsQe);
                    }
                    else
                    {
                        sheets.Add(sheetKey, new Queue<Queue<Excel.IXLCell>>());
                        sheets[sheetKey].Enqueue(rowsQe);
                    }
                }
            }
            if(sheets.Count == 0)
            {
                throw new Exception("没有输入任何需要创建的新表");
            }

            var expWorkbook = new Excel.XLWorkbook();

            foreach(var data in sheets)
            {
                Console.WriteLine($"Excel{exportExcelPath}创建表[{data.Key}]");
                var tempSheet = expWorkbook.AddWorksheet(data.Key);
                int colCount = 2;
                int rowCount = 2;
                tempSheet.Cell(1, 1).Value = "<>";

                //write sheet head
                var headCopy = head.ToArray();
                for(int i= 0;i<headCopy.Length;i++)
                {
                    var tempQeCopy = headCopy[i].ToArray();
                    for (int j = 0; j < tempQeCopy.Length; j++)
                    {
                        tempSheet.Cell(rowCount, colCount).Value = tempQeCopy[j].Value;
                        colCount++;
                    }
                    colCount = 2;
                    rowCount++;
                }
                //write sheet data
                while (data.Value.Count != 0)
                {
                    var tempQe = data.Value.Dequeue();
                    while (tempQe.Count != 0)
                    {
                        tempSheet.Cell(rowCount, colCount).Value = tempQe.Dequeue().Value;
                        colCount++;
                    }
                    colCount = 2;
                    rowCount++;
                }
            }

            expWorkbook.SaveAs(fsOut);

            Console.WriteLine("拆分子表完成");
        }
    }
}
