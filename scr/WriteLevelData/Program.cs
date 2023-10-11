using System;
using System.IO;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json.Linq;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Collections;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace WriteLevelData
{
    //鹏哥封装的类，用于读取.map文件
    class MapInfo
    {
        private int width = 0;
        private int height = 0;
        public int Width => width;
        public int Height => height;

        private BitArray? blocks;

        struct Pos
        {
            public int x;
            public int y;
            public Pos(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        private List<Pos> randWalkList = new List<Pos>();

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct TMir3MapHeader
        {
            //{--------适用与新版传奇3地图（2020-12月）版本------------}
            //{--------总共40个字节，$28                               }
            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            // Byte[] Title;
            UInt128 Title;
            public UInt16 Width;
            public UInt16 Height;
            UInt16 Attr;
            Byte Version;
            Byte Rev;
            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            // Byte[] Resv;
            UInt128 Resv;
        }

        //{      2020/12/2    SDNewMir3定义                              }
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct TMir3MapFileInfo
        {
            public Byte Flag;
            Byte TileFileIdx;
            Byte smTileFileIdx;  //----CellFileIdx
            Byte ObjFileIdx;
            UInt16 TileImgIdx;     //imglarge  : Word;
            UInt16 smTileImgIdx;   //imgsmall  : Word;
            UInt16 ObjImgIdx;      //imgobject : Word;
            Byte ObjAni_1;       //doorindex : Byte;
            Byte ObjAni_2;       //dooroffset: Byte;
            UInt16 Light;          //aniframe  : Byte;
            Byte FlagEx;         //anitick   : Byte;
            Byte Reserve;        //area      : Byte;
            Int32 ReserveEx;      //light     : Byte;
        }

        enum CellFlag
        {
            Free,
            DenyFly,
            DenyMove,
        }

        const int MAX_MAP_SIZE = 800;
        public bool Load(string path)
        {
            int bufLen = Math.Max(Marshal.SizeOf<TMir3MapHeader>(), Marshal.SizeOf<TMir3MapFileInfo>());
            Span<byte> buf = stackalloc byte[bufLen];
            using (var fs = File.OpenRead(path))
            {
                fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir3MapHeader>()));
                ref var header = ref MemoryMarshal.AsRef<TMir3MapHeader>(buf);

                if (header.Width > MAX_MAP_SIZE || header.Height > MAX_MAP_SIZE)
                {
                    throw new Exception($"ivnalid map size width={header.Width} height={header.Height}");
                }

                width = header.Width;
                height = header.Height;
                blocks = new BitArray(width * height);
                randWalkList.Clear();

                List<Pos> freePos = new List<Pos>();
                for (int ix = 0; ix < width; ix++)
                {
                    freePos.Clear();
                    for (int iy = 0; iy < height; iy++)
                    {
                        fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir3MapFileInfo>()));
                        ref var cell = ref MemoryMarshal.AsRef<TMir3MapFileInfo>(buf);

                        bool block = false;
                        if ((cell.Flag & 0x2) != 0x2)
                        { // DenyFly
                            block = true;
                        }
                        else if ((cell.Flag & 0x1) != 0x1)
                        { // DenyMove
                            block = true;
                        }
                        else
                        {
                            block = false;
                            freePos.Add(new Pos(ix, iy));
                        }
                        SetBlock(ix, iy, block);
                    }
                    randWalkList.AddRange(ShuffleIterator(freePos, Random.Shared).Take(2));
                }
            }
            return true;
        }

        private static IEnumerable<T> ShuffleIterator<T>(List<T> source, Random rng)
        {
            var buffer = source;
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        public bool IsBlock(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return true;
            }
            return blocks?.Get(x * height + y) ?? true;
        }

        private void SetBlock(int x, int y, bool block)
        {
            blocks?.Set(x * height + y, block);
        }

        public bool GetRandomStandPos(out int x, out int y)
        {
            x = 0; y = 0;
            if (randWalkList.Count <= 0)
            {
                return false;
            }

            int index = Random.Shared.Next(randWalkList.Count);
            x = randWalkList[index].x;
            y = randWalkList[index].y;
            return true;
        }
    }
    public static class ExtenMethod
    {
        public static void SetBgImage(this MapJson mapJson, string ImgPath)
        {
            Console.WriteLine("设置背景图层");
            foreach (var lay in mapJson.layers)
            {
                if (lay.name == "background" && lay.type == "imagelayer")
                {
                    ((ImageLayer)lay).image = ImgPath;
                }
            }
        }
        public static void SetBlockData(this MapJson mapJson, string MapFilePath)
        {
            var mapinfo = new MapInfo();
            mapinfo.Load(MapFilePath);
            mapJson.ReSize(mapinfo.Width, mapinfo.Height);

            Console.WriteLine("添加[可移动图层]数据");
            var blockLay = new TileLayer();
            foreach (var lay in mapJson.layers)
            {
                if (lay.name == "block" && lay.type == "tilelayer")
                {
                    blockLay = (TileLayer)lay;
                }

                ////如果 .map 目录下有对应的jpg文件，则设置为背景
                //if (lay.name == "background" && lay.type == "imagelayer")
                //{                            
                //    if (File.Exists(Path.ChangeExtension(MapFilePath, ".jpg"))) {
                //        var mapname = Path.GetFileNameWithoutExtension(MapFilePath);
                //        Console.WriteLine($"[{mapname}.json]设置背景图层");
                //        ((ImageLayer)lay).image = mapname+".jpg";
                //    }                    
                //}
            }

            for (int i = 0; i < blockLay.data.Length; i++)
            {
                int x = i % blockLay.width;
                int y = i / blockLay.width;

                blockLay.data[i] = mapinfo.IsBlock(x, y) ? 1+int.Parse(mapJson.nextlayerid) : 0+ int.Parse(mapJson.nextlayerid);
            }
        }
        public static void AddBlockLayer(this MapJson mapJson,string MapFilePath)
        {
            if (File.Exists(MapFilePath) | Path.GetExtension(MapFilePath) != ".map")
                throw new Exception(".map文件输入不正确-------");

            foreach(var lay in mapJson.layers)
            {
                if (lay.name == "block")
                {
                    Console.WriteLine("该json文件已经含有block图层------");
                    return;
                }
            }

            var mapinfo = new MapInfo();
            mapinfo.Load(MapFilePath);
            
            var blockLay = new TileLayer();
            blockLay.name = "block";
            blockLay.id = mapJson.layers.Count + 1;
            blockLay.opacity = "1";
            blockLay.type = "tilelayer";
            blockLay.visible = "true";
            blockLay.x = 0;
            blockLay.y = 0;
            blockLay.width = mapJson.width;
            blockLay.height = mapJson.height;
            blockLay.data = new int[mapJson.height * mapJson.width];

            if (mapJson.width != mapinfo.Width | mapJson.width != mapinfo.Height)
                throw new Exception($".json文件和.map文件的尺寸不一样: .json文件{mapJson.width}*{mapJson.width},.map{mapinfo.Width}*{mapinfo.Height}");

            for(int i = 0; i < blockLay.data.Length; i++)
            {
                int x = i % blockLay.width;
                int y = i / blockLay.height;

                blockLay.data[i] = mapinfo.IsBlock(x, y) ? 1 : 0;
            }

            mapJson.layers.Add(blockLay);
        }
        public static void ReSize(this MapJson mapJson, int nWidth, int nHeight)
        {
            mapJson.width = nWidth; 
            mapJson.height = nHeight;

            foreach(var lay in mapJson.layers.Where(n=>n.type == "tilelayer")) {
                 ((TileLayer)lay).ReSize(nWidth, nHeight);
            }
        }
        public static void ReSize(this TileLayer tileLayer, int nWidth, int nHeight)
        {
            tileLayer.width = nWidth;
            tileLayer.height = nHeight;
            tileLayer.data = null;
            tileLayer.data = new int[nWidth * nHeight];
        }
    }
    public class MapJson
    {
        public string compressionlevel;
        public int height;
        public string infinite;
        public List<Layer> layers;
        public string nextlayerid;
        public string nextobjectid;
        public string orientation;
        public string renderorder;
        public string tiledversion;
        public int tileheight;
        public List<Tileset> tilesets;
        public int tilewidth;
        public string type;
        public string version;
        public int width;
    }
    public class Layer
    {
        public int id;
        public string name;
        public string opacity;
        public string type;
        public string visible;
        public int x;
        public int y;
    }
    public class ImageLayer:Layer
    {
        public string image;
    }
    public class TileLayer : Layer
    {
        public int[] data;
        public int height;
        public int width;
    }
    public class LayerJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Layer).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string itype = (string)jo["type"];
            Layer tlayer;
            if (itype == "imagelayer")
            {
                tlayer=  new ImageLayer();
            }
            else if (itype == "tilelayer")
            {
                tlayer= new TileLayer();
            }
            else { tlayer= new Layer(); }

            serializer.Populate(jo.CreateReader(), tlayer);

            return tlayer;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    public class Tileset
    {
        public string firstgid;
        public string source;
    }
    public class Pos
    {
        public int x;
        public int y;

        public Pos(int ix,int iy)
        {
            x = ix;
            y = iy;
        }

        public static Pos GetPos(int count,int width)
        {
            return new Pos(count % width, count / width);
        }
    }
    public class PointSet
    {
        public string difLv;
        public string genMonNum;
        public string genMonTime;

        public PointSet(string iParam)
        {
            var iparams = iParam.Split("#");
            if(iparams.Length >= 3)
            {
                difLv = iparams[0];
                genMonNum = iparams[1];
                genMonTime = iparams[2];
            }
            else
            {
                throw new Exception("配置的参数参数不完整！！，每个点需要怪物[怪物种类]、[数量]、[生成时间]");
            }
           
        }
    }
    public class MapSetting
    {
        public string mapName;
        public string ranger;
        public Dictionary<string, MonNameGener> monNames = new Dictionary<string, MonNameGener>();
        public Dictionary<string, List<PointSet>> pointSetLv = new Dictionary<string, List<PointSet>>();
        public Dictionary<string,string> tags = new Dictionary<string,string>();

        public static Dictionary<string, MapSetting> LoadSettings(IXLWorksheet sheet)
        {
            Dictionary<string, MapSetting> settings = new Dictionary<string, MapSetting>();

            Dictionary<int, Action<MapSetting, string>> headAction = new Dictionary<int, Action<MapSetting, string>>();
            foreach (var cell in sheet.FirstRow().Cells())
            {
                if (cell.Value.ToString() == "MapName")
                {
                    headAction.Add(cell.Address.ColumnNumber, (ms, val) => ms.mapName = val);
                }
                if (cell.Value.ToString() == "Ranger")
                {
                    headAction.Add(cell.Address.ColumnNumber, (ms, val) => ms.ranger = val);
                }
                if (cell.Value.ToString().StartsWith("MonName"))
                {
                    string subIndex = cell.Value.ToString().Substring("MonName".Length);
                    headAction.Add(cell.Address.ColumnNumber, (ms, val) =>
                    {
                        ms.monNames.Add(subIndex, new MonNameGener(val));
                    });
                }
                if (cell.Value.ToString().StartsWith("PointSetLv"))
                {
                    string subIndex = cell.Value.ToString().Substring("PointSetLv".Length);
                    headAction.Add(cell.Address.ColumnNumber, (ms, val) =>
                    {
                        var iparams = val.Split("|");
                        var pointSets = new List<PointSet>();
                        foreach(var ip in iparams)
                        {
                            pointSets.Add(new PointSet(ip));
                        }
                        ms.pointSetLv.Add(subIndex, pointSets);
                    });
                }
                if (cell.Value.ToString().StartsWith("Tag"))
                {
                    //string subIndex = cell.Value.ToString().Substring("Tag".Length);
                    headAction.Add(cell.Address.ColumnNumber, (ms, val) => ms.tags.Add(cell.Value.ToString(), val));
                }
            }

            for (int i = sheet.FirstRowUsed().RowNumber() + 1; i <= sheet.LastRowUsed().RowNumber(); i++)
            {
                var row = sheet.Row(i);
                MapSetting mset = new MapSetting();

                foreach (var cell in row.Cells())
                {
                    if (headAction.ContainsKey(cell.Address.ColumnNumber))
                    {
                        headAction[cell.Address.ColumnNumber].Invoke(mset, cell.Value.ToString());
                    }
                }
                Console.WriteLine($"加载地图配置——[{mset.mapName}]");
                settings.Add(mset.mapName, mset);
            }

            return settings;
        }
    }
    public class MonNameGener
    {
        string[] names;
        private int genCounter;
        public  MonNameGener(string mons)
        {
            names = mons.Split("|");
            genCounter = 0;
        }

        public string GetARandomName()
        {
            //随机获取名字
            //Random random = new Random();
            //return names[random.Next(names.Length)];

            //按填写顺序逐个取，循环
            string result  = names[genCounter];

            if (genCounter == names.Length - 1)
                genCounter = 0;
            else
                genCounter++;

            return result;
        }
    }
    public class PointData
    {
        public Pos pos;
        public int lv;
    }
    public class MapData
    {
        public string mapName;
        public int maph;
        public int mapw;
        public int nextLayerID;
        public List<PointData> genMonPointDatas = new List<PointData>();
        public List<PointData> genMonPointDatas2 = new List<PointData>();
        public List<PointData> playerEnterPointDatas = new List<PointData>();
        public List<PointData> flagPointDatas = new List<PointData>();

        public MapData(string jsonPath)
        {
            string a = Path.GetExtension(jsonPath).ToLower();
            if (Path.GetExtension(jsonPath).ToLower() != ".json")
                throw new Exception($"[{jsonPath}]不是json文件!!!!!");

            Console.WriteLine($"读取tiled源文件数据{Path.GetFileName(jsonPath)}");

            mapName = Path.GetFileNameWithoutExtension(jsonPath);
            var data = JsonConvert.DeserializeObject<MapJson>(File.ReadAllText(jsonPath),new LayerJsonConverter());
            maph = data.height; 
            mapw = data.width;
            nextLayerID = int.Parse(data.nextlayerid);

            TileLayer genMonLayer = null;
            TileLayer genMonLayer2 = null;
            TileLayer playEnterLayer = null;
            TileLayer blockLayer = null;
            foreach (var d in data.layers)
            {
                if(d.name == "种怪" && d.type == "tilelayer")
                {
                    genMonLayer = (TileLayer)d;
                }
                if (d.name == "出生点" && d.type == "tilelayer")
                {
                    playEnterLayer = (TileLayer)d;
                }
                if (d.name == "种怪2" && d.type == "tilelayer")
                {
                    genMonLayer2 = (TileLayer)d;
                }
                if (d.name == "block" && d.type == "tilelayer")
                {
                    blockLayer = (TileLayer)d;
                }
            }

            if (playEnterLayer == null)
            {
                Console.WriteLine($"未配置地图[{mapName}]的传送点坐标--");
            }
            else
            {
                for (int i = 0; i < playEnterLayer.data.Length; i++)
                {
                    if (playEnterLayer.data[i] > 0)
                    {
                        PointData gpd = new PointData();
                        gpd.lv = playEnterLayer.data[i];
                        gpd.pos = Pos.GetPos(i, playEnterLayer.width);

                        playerEnterPointDatas.Add(gpd);
                    }
                }
            }
                
            if (genMonLayer == null)
                throw new Exception($"json{jsonPath}文件不包含tilelayer图层，或tilelayer图层的名字不叫map！！！！");

            for(int i = 0; i < genMonLayer.data.Length; i++)
            {
                if (genMonLayer.data[i] > 0)
                {
                    PointData gpd = new PointData();
                    gpd.lv = genMonLayer.data[i];
                    gpd.pos = Pos.GetPos(i, genMonLayer.width);

                    genMonPointDatas.Add(gpd);
                }
            } 
            
            if(genMonLayer2 != null)
            {
                for (int i = 0; i < genMonLayer2.data.Length; i++)
                {
                    if (genMonLayer2.data[i] > 0)
                    {
                        PointData gpd = new PointData();
                        gpd.lv = genMonLayer2.data[i];
                        gpd.pos = Pos.GetPos(i, genMonLayer2.width);

                        genMonPointDatas2.Add(gpd);
                    }
                }
            }
            if (blockLayer != null)
            {
                for (int i = 0; i < blockLayer.data.Length; i++)
                {
                        PointData gpd = new PointData();
                        gpd.lv = blockLayer.data[i];
                        gpd.pos = Pos.GetPos(i, blockLayer.width);

                        flagPointDatas.Add(gpd);
                }
            }
        }
    }
    public class DataRecode
    {
        public string mapname;
        public int mapw;
        public int maph;
        public Dictionary<string, int> monNumCounter;
        public Dictionary<string, int> pointNumConter;
        public Dictionary<string, string> tags;
        public int vaildBlockNum;
        public DataRecode(string imapname,int imapw,int imaph)
        {
            mapname = imapname;
            mapw = imapw;
            maph = imaph;
            monNumCounter = new Dictionary<string, int>();
            pointNumConter = new Dictionary<string, int>();
            tags = new Dictionary<string, string>();
        }
    }
    public class DataRecoder
    {
        //List可以按序号取，用来计数的
        private List<string> names;
        private Dictionary<string, DataRecode> datas;
        //初始化excel的委托
        private Action<DataRecode> initRecodeAction;
        //private Action<IXLWorksheet> initSheetAction;
        //存放数据的EXCEl
        private XLWorkbook dataWb;
        private IXLWorksheet dataWs;

        private Dictionary<int, Action<DataRecode, IXLCell>> headAction;
        public int count { private set; get; }


        public void Add(MapData map,MapSetting ms)
        {
            if (datas.ContainsKey(map.mapName))
            {
                throw new Exception("地图重复加入");
            }
            else
            {
                var temp = new DataRecode(map.mapName,map.mapw, map.maph);
                initRecodeAction(temp);
                temp.tags = ms.tags;
                names.Add(map.mapName);
                datas.Add(map.mapName, temp);
                count++;
            }
        }
        public void Recode(string imapname,Action<DataRecode> action)
        {
            if (!datas.ContainsKey(imapname))
            {
                throw new Exception("没有这个地图");
            }
            else {
                action(datas[imapname]);
            }
            
        }
        public void Write(string imap)
        {
            int writeRow = 1;
            for(int i= 1; i <= names.Count; i++)
            {
                if (names[i-1]==imap)
                    writeRow = writeRow + i;
            }
            for(int i= 1; i <= dataWs.FirstRowUsed().CellCount(); i++)
            {
                if (i == 1)
                {
                    dataWs.Cell(writeRow, 1).Value = writeRow - 1;
                    continue;
                }
                if(headAction.ContainsKey(i))
                    headAction[i].Invoke(datas[imap], dataWs.Cell(writeRow, i));
            }
        }
        public void Save()
        {
            if (File.Exists("地图统计数据.xlsx"))
            {
                File.Delete("地图统计数据.xlsx");
            }          
            dataWb.SaveAs("地图统计数据.xlsx");
            Console.WriteLine($"统计数据保存至[{Path.GetFullPath("地图统计数据.xlsx")}]");
        }
        public DataRecoder(IXLWorksheet iws)
        {

            dataWb = new XLWorkbook();
            dataWs = dataWb.AddWorksheet("datas");
            count = 0;
            datas = new Dictionary<string, DataRecode>();
            names = new List<string>();
            headAction = new Dictionary<int, Action<DataRecode, IXLCell>>();

            //初始化Excel
            dataWs.Cell(1, 1).Value = "序号";
            dataWs.Cell(1, 2).Value = "地图名";
            headAction.Add(2, (idr, Icell) => { Icell.Value = idr.mapname; });
            dataWs.Cell(1, 3).Value = "宽度";
            headAction.Add(3, (idr, Icell) => { Icell.Value = idr.mapw; });
            dataWs.Cell(1, 4).Value = "高度";
            headAction.Add(4, (idr, Icell) => { Icell.Value = idr.maph; });
            dataWs.Cell(1, 5).Value = "可到达地块";
            headAction.Add(5, (idr, Icell) => { Icell.Value = idr.vaildBlockNum; });
            int colCounter = 6;
            foreach (var c in iws.FirstRow().Cells())
            {
                if (c.Value.ToString().StartsWith("MonName"))
                {
                    string subIndex = c.Value.ToString().Substring("MonName".Length);
                    dataWs.Cell(1, colCounter).Value = subIndex + "数量";
                    headAction.Add(colCounter, (idr, Icell) => { Icell.Value = idr.monNumCounter[subIndex]; });
                    colCounter++;
                }
            }
            foreach (var c in iws.FirstRow().Cells())
            {
                if (c.Value.ToString().StartsWith("PointSetLv"))
                {
                    string subIndex = c.Value.ToString().Substring("PointSetLv".Length);
                    dataWs.Cell(1, colCounter).Value = "难度" + subIndex + "数量";
                    headAction.Add(colCounter, (idr, Icell) => { Icell.Value = idr.pointNumConter[subIndex]; });
                    colCounter++;
                }
            }
            foreach (var c in iws.FirstRow().Cells())
            {
                if (c.Value.ToString().StartsWith("Tag"))
                {
                    dataWs.Cell(1, colCounter).Value = c.Value.ToString();
                    headAction.Add(colCounter, (idr, Icell) => { Icell.Value = idr.tags[c.Value.ToString()]; });
                    colCounter++;
                }
            }

            //初始化recode
            initRecodeAction = (dataReco) =>
            {
                foreach (var c in iws.FirstRow().Cells())
                {
                    if (c.Value.ToString().StartsWith("MonName"))
                    {
                        string subIndex = c.Value.ToString().Substring("MonName".Length);
                        dataReco.monNumCounter.Add(subIndex, 0);
                    }
                    if (c.Value.ToString().StartsWith("PointSetLv"))
                    {
                        string subIndex = c.Value.ToString().Substring("PointSetLv".Length);
                        dataReco.pointNumConter.Add(subIndex, 0);
                    }
                }
            };

        }
    }
    class Program
    {
        static void GenTiledProj(Dictionary<string, string> iparams)
        {        
            string sourcePath = iparams["data"];
            string outPath = iparams["out"];
                       
            if (!File.Exists("sample.json"))
            {
                throw new Exception("未找到示例文件[sample.json]");
            }
            Console.WriteLine($"[{Path.GetFileName(sourcePath)}]生成Tiled工程数据");

            var mapjson = JsonConvert.DeserializeObject<MapJson>(File.ReadAllText("sample.json"), new LayerJsonConverter());

            mapjson.SetBlockData(sourcePath);

            if (iparams.ContainsKey("bgimg"))
            {
                mapjson.SetBgImage(iparams["bgimg"]);
            }

            File.WriteAllText(outPath, JsonConvert.SerializeObject(mapjson));
            Console.WriteLine($"[{Path.GetFileName(sourcePath)}]Tiled工程数据生成完毕----------------------\n");
        }
        static void GenMonData(Dictionary<string,string> iparams)
        {
            if (!File.Exists("地图种怪配置.xlsx"))
                throw new Exception("地图种怪配置.xlsx不存在！！！！！");

            //初始化配置
            Console.WriteLine("开始加载[地图种怪配置.xlsx]");
            FileStream fswb = new FileStream("地图种怪配置.xlsx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var wb = new XLWorkbook(fswb);
            var mapSets = MapSetting.LoadSettings(wb.Worksheet("MapSetting"));

            var paths = iparams["path"].Split("#");

            if (paths.Length <= 0){
                throw new Exception("未输入json路径！！！！");
            }              

            var mapDatas = new List<MapData>();
            for (int i = 0; i < paths.Length; i++) {
                mapDatas.Add(new MapData(paths[i]));
            }
                

            if (File.Exists("种怪数据.xlsx")){
                Console.WriteLine("[种怪数据.xlsx]文件已存在，删除---------------");
                File.Delete("种怪数据.xlsx");
            }

            Console.WriteLine("创建[种怪数据.xlsx]");
            var wbData = new XLWorkbook();

            var ws = wbData.AddWorksheet("种怪点数据");
            var ws2 = wbData.AddWorksheet("出生点数据");
            var ws3 = wbData.AddWorksheet("种怪点数据2");

            ws.Cell(1, 1).Value = ";MAP";
            ws.Cell(1, 2).Value = "START_X";
            ws.Cell(1, 3).Value = "START_Y";
            ws.Cell(1, 4).Value = "MonName";
            ws.Cell(1, 5).Value = "Ranger";
            ws.Cell(1, 6).Value = "AMOUNT";
            ws.Cell(1, 7).Value = "GENTIME";
            ws.Cell(1, 8).Value = "GenType";
            ws.Cell(1, 9).Value = "TimeOffSet";

            ws2.Cell(1, 1).Value = "地图名";
            ws2.Cell(1, 2).Value = "传动点坐标";

            ws3.Cell(1, 1).Value = ";MAP";
            ws3.Cell(1, 2).Value = "START_X";
            ws3.Cell(1, 3).Value = "START_Y";
            ws3.Cell(1, 4).Value = "MonName";
            ws3.Cell(1, 5).Value = "Ranger";
            ws3.Cell(1, 6).Value = "AMOUNT";
            ws3.Cell(1, 7).Value = "GENTIME";
            ws3.Cell(1, 8).Value = "GenType";
            ws3.Cell(1, 9).Value = "TimeOffSet";

            var dataRecoder = new DataRecoder( wb.Worksheet("MapSetting"));

            int rowCounter = 2;
            int rowCounterWs3 = 2;
            int mapCounter = 2;
            foreach (var mapData in mapDatas)
            {
                //数据统计器新增数据
                dataRecoder.Add(mapData, mapSets[mapData.mapName]);

                //开始写入数据
                //写入地图传送点的数据
                if (mapData.playerEnterPointDatas.Count > 0)
                {
                    Console.WriteLine($"生成地图[{mapData.mapName}]的传送点数据到Excel表格,共{mapData.playerEnterPointDatas.Count}个传送点！！！");
                    ws2.Cell(mapCounter, 1).Value = mapData.mapName;
                    for (int i = 0; i < mapData.playerEnterPointDatas.Count; i++)
                    {
                        if (i == 0)
                        {
                            ws2.Cell(mapCounter, 2).Value = (mapData.playerEnterPointDatas[i].pos.x ).ToString() + "#"
                                                          + (mapData.playerEnterPointDatas[i].pos.y ).ToString();
                        }
                        else
                        {
                            ws2.Cell(mapCounter, 2).Value = ws2.Cell(mapCounter, 2).Value.ToString() + "|"
                                                            + (mapData.playerEnterPointDatas[i].pos.x ).ToString() + "#"
                                                            + (mapData.playerEnterPointDatas[i].pos.y ).ToString();
                        }
                    }

                }

                //写入怪物生成点的数据
                Console.WriteLine($"生成地图[{mapData.mapName}]的种怪数据到Excel表格,共{mapData.genMonPointDatas.Count}个种怪点！！！");

                //种怪图层1
                foreach (var point in mapData.genMonPointDatas)
                {
                    var mapSet = mapSets[mapData.mapName];
                    //var levelSet = levelSetting[point.lv];

                    //统计坐标点-难度的数据
                    dataRecoder.Recode(mapData.mapName, (dr) => { dr.pointNumConter[point.lv.ToString()] = dr.pointNumConter[point.lv.ToString()] + 1; });

                    foreach (var monGen in mapSet.pointSetLv[point.lv.ToString()])
                    {
                        //确认写入数据
                        ws.Cell(rowCounter, 1).Value = mapSet.mapName;
                        ws.Cell(rowCounter, 2).Value = point.pos.x ;
                        ws.Cell(rowCounter, 3).Value = point.pos.y ;
                        ws.Cell(rowCounter, 4).Value = mapSet.monNames[monGen.difLv].GetARandomName();
                        ws.Cell(rowCounter, 5).Value = mapSet.ranger;
                        ws.Cell(rowCounter, 6).Value = monGen.genMonNum;
                        ws.Cell(rowCounter, 7).Value = monGen.genMonTime;
                        //默认数据，生成类型和刷新起始点
                        ws.Cell(rowCounter, 8).Value = 2;
                        ws.Cell(rowCounter, 9).Value = "00:00:00";
                        rowCounter++;

                        //统计可以到达的地块数----------------------------
                        int startX = point.pos.x- int.Parse(mapSet.ranger);
                        int startY = point.pos.y- int.Parse(mapSet.ranger);
                        int length = int.Parse(mapSet.ranger) * 2;
                        int tempCounter = 0;
                        //Console.WriteLine($"计算点:{point.pos.x},{point.pos.y}");
                        for (int tx = 0; tx < length; tx++)
                        {
                            for (int ty = 0; ty < length; ty++)
                            {
                                //Console.WriteLine($"x:{startX + tx}.y:{startY + ty},val:{mapData.flagPointDatas[(startY + ty) * mapData.mapw + startX + tx].lv}");
                                if (mapData.flagPointDatas[(startY + ty) * mapData.mapw + startX + tx].lv
                                    == (mapData.nextLayerID + 0))
                                {
                                    tempCounter++;
                                }
                            }
                        }
                        //Console.WriteLine($"可到达点数：{tempCounter}");
                        dataRecoder.Recode(mapData.mapName, (dr) => { dr.vaildBlockNum = dr.vaildBlockNum+tempCounter; });

                        //统计怪物数量的数据
                        dataRecoder.Recode(mapData.mapName, (dr) => { dr.monNumCounter[monGen.difLv] = dr.monNumCounter[monGen.difLv] + int.Parse(monGen.genMonNum); });
                        
                    }
                }

                //种怪图层2,可空
                if (mapData.genMonPointDatas2 != null)
                {
                    foreach (var point in mapData.genMonPointDatas2)
                    {
                        var mapSet = mapSets[mapData.mapName];
                        //var levelSet = levelSetting[point.lv];

                        //统计坐标点-难度的数据
                        dataRecoder.Recode(mapData.mapName, (dr) => { dr.pointNumConter[point.lv.ToString()] = dr.pointNumConter[point.lv.ToString()] + 1; });

                        foreach (var monGen in mapSet.pointSetLv[point.lv.ToString()])
                        {
                            //确认写入数据
                            ws3.Cell(rowCounterWs3, 1).Value = mapSet.mapName;
                            ws3.Cell(rowCounterWs3, 2).Value = point.pos.x ;
                            ws3.Cell(rowCounterWs3, 3).Value = point.pos.y ;
                            ws3.Cell(rowCounterWs3, 4).Value = mapSet.monNames[monGen.difLv].GetARandomName();
                            ws3.Cell(rowCounterWs3, 5).Value = mapSet.ranger;
                            ws3.Cell(rowCounterWs3, 6).Value = monGen.genMonNum;
                            ws3.Cell(rowCounterWs3, 7).Value = monGen.genMonTime;
                            //默认数据，生成类型和刷新起始点
                            ws3.Cell(rowCounterWs3, 8).Value = 2;
                            ws3.Cell(rowCounterWs3, 9).Value = "00:00:00";
                            rowCounterWs3++;

                            //统计怪物数量的数据
                            dataRecoder.Recode(mapData.mapName, (dr) => { dr.monNumCounter[monGen.difLv] = dr.monNumCounter[monGen.difLv] + int.Parse(monGen.genMonNum); });

                        }
                    }
                }
                dataRecoder.Write(mapData.mapName);
                mapCounter++;
            }

            wbData.SaveAs("种怪数据.xlsx");
            dataRecoder.Save();
            Console.WriteLine("打开[种怪数据.xlsx]查看生成的配置表数据");
        }
        static void PrintHelp()
        {
            Console.WriteLine("每个参数用 # 分割 key 和 value");
            Console.WriteLine("1.生成种怪配置:");
            Console.WriteLine("     cmd#genmon");
            Console.WriteLine("     path#Dir1#dDir2#......");
            Console.WriteLine("2.生成Tiled工程数据:");
            Console.WriteLine("     cmd#tiled");
            Console.WriteLine("     data#filepath.map");
            Console.WriteLine("     out#outpath.json");
            Console.WriteLine("     bgimg#imgpath.jpg --(bgimg arg is optional)");
        }
        static void Main(string[] args)
        {
            ////test
            //args = new string[] { "", "" };
            //args[0] = "cmd#genmon";
            //args[1] = "path#D:\\SVNProject\\Mir3\\documents\\7.地图种怪工具\\1.TileMap工程\\maps\\42\\42.json";
            ////test

            //args = new string[] { "cmd#tiled",
            //                      "bgimg#72.jpg",
            //                      "out#D:\\SVNProject\\Mir3\\documents\\7.地图种怪工具\\1.TileMap工程\\maps\\72\\72.json",
            //                      "data#E:\\工作文档\\20230609.种怪.V.2\\map文件\\72.map"};

            Dictionary<string, string> inputParam = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                var s = arg.Split("#");

                if (s[0] == "path")
                {
                    inputParam.Add(s[0], s[1]);
                    for (int i = 2; i < s.Length; i++)
                        inputParam[s[0]] = inputParam[s[0]] + "#" + s[i];
                }
                else { inputParam.Add(s[0], s[1]); }
            }

            if (!inputParam.ContainsKey("cmd"))
            {
                Console.WriteLine("输入参数不完整！！！！");
                PrintHelp();               
            }
            else if (inputParam["cmd"] == "genmon")
            {
                Console.WriteLine("执行命令：生成种怪配置--------");
                GenMonData(inputParam);
            }
            else if (inputParam["cmd"] == "tiled")
            {
                Console.WriteLine("执行命令：生成[Tiled]工程数据--------");
                GenTiledProj(inputParam);
            }
            else {
                PrintHelp();
            }
        }

        static void testMain(string[] args) {
        //    string samplePath = "D:\\SVNProject\\Mir3\\documents\\7.地图种怪工具\\1.TileMap工程\\maps\\75\\sample.json";
            string mapFile = "D:\\SVNProject\\Mir3\\documents\\7.地图种怪工具\\1.TileMap工程\\maps\\75\\75.map";

            //    var map  = JsonConvert.DeserializeObject<MapJson>(File.ReadAllText(samplePath), new LayerJsonConverter());
            //    map.SetBlockData(mapFile);

            //    File.WriteAllText("D:\\SVNProject\\Mir3\\documents\\7.地图种怪工具\\1.TileMap工程\\maps\\75\\new.json", JsonConvert.SerializeObject(map));

            Console.WriteLine(Path.ChangeExtension(mapFile, ".jpg"));
        }

    }           
}
