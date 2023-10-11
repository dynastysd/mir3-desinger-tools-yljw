using Mir2.Map;
using Mir2Svr.Map;
using Mir3YouLong.Map;
using MirUtils;
using System.Runtime.InteropServices;

namespace MapConverter
{
    public enum MapDefine
    {
        Mir2_OldSchool,    //head is 52 byte,per info is 12 byte
        Mir2_ShengDa2012,  //head is 52 byte,per info is 14 byte
        Mir3_Youlong_org,
        Mir3_Youlong_fromMir2
    }
    public enum MapLayerDefine
    {
        Empty = 0,
        Tile = 1,
        SmTile = 2,
        Object = 4
    }
    internal class Program
    {     
        struct SParam
        {
            //necessary 
            public string mapPath;
            public CmdType cmdType;

            //optional
            public string exportPath;
            public MapLayerDefine changerLayer;
            public int sourceId;
            public int targetId;
            public string libPath;
            
            //not input by args
            public bool vaild; 
        }
        enum CmdType
        {
            TransToM3,
            TransFileId,
            ShowInfo,
            RenderMiniMap
        }
        static SParam param;
        public static void InitParams(string[] args)
        {
            param = new SParam();
            param.vaild = false;
            param.changerLayer = MapLayerDefine.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-p") //path
                {
                    if (!string.IsNullOrEmpty(args[i]))
                        param.mapPath = args[i+1];
                }
                if (args[i].ToLower() == "-o") //out path
                {
                    if (!string.IsNullOrEmpty(args[i]))
                        param.exportPath = args[i + 1];
                }
                if (args[i].ToLower() == "-t") //type of cmd
                {
                    if (!string.IsNullOrEmpty(args[i + 1]))
                    {
                        var str = args[i + 1].ToLower();

                        if (str == "showinfo")
                        {
                            param.cmdType = CmdType.ShowInfo;
                            param.vaild = true;
                        }
                        if (str == "tomir3")
                        {
                            param.cmdType = CmdType.TransToM3;
                            param.vaild = true;
                        }
                        if (str == "chgfileid")
                        {
                            param.cmdType = CmdType.TransFileId;
                            string lay = "";
                            int steps = 1;

                            if( !string.IsNullOrEmpty(args[i + 2]))
                            {
                                lay = args[i + 2].ToLower();
                                if (lay == "tile")
                                {
                                    param.changerLayer = MapLayerDefine.Tile;
                                    steps++;
                                }                                    
                                if (lay == "smtile")
                                {
                                    param.changerLayer = MapLayerDefine.SmTile;
                                    steps++;
                                }
                                if (lay == "object")
                                {
                                    param.changerLayer = MapLayerDefine.Object;
                                    steps++;
                                }
                            }
                                
                            if (!string.IsNullOrEmpty(args[i + 3]))
                            {
                                param.sourceId = int.Parse(args[i + 2].ToLower());
                                steps++;
                            }
                            if (!string.IsNullOrEmpty(args[i + 4]))
                            {
                                param.targetId = int.Parse(args[i + 3].ToLower());
                                steps++;
                            }

                            if(steps == 4)
                                param.vaild = true;
                        }
                        if (str == "minimap")
                        {
                            param.cmdType = CmdType.RenderMiniMap;

                            if (!string.IsNullOrEmpty(args[i + 2]))
                            {
                                param.libPath = args[i + 2];
                            }
                            param.vaild = true;
                        }
                    }
                        
                }
            }
        }
        public static bool VerifyMap(string iMapPath) {
            if (File.Exists (iMapPath) && iMapPath.ToLower().EndsWith(".map"))
            {
                return true;
            }
            else
            { 
                return false; 
            }
        }
        public static void ShowInfo() 
        {
            string mapPath = param.mapPath; 
            if (VerifyMap(mapPath)) 
            {
                Utils.Log($"----[{Path.GetFileNameWithoutExtension(mapPath)}]--Info----");
                int maptype = Utils.FindType(mapPath);
                Utils.Log($"map type id:[{maptype}]");
                var map = new MapInfo();
                if (maptype == 2)
                {                    
                    map.Load_M2(mapPath, MapDefine.Mir2_ShengDa2012);
                    
                }
                if(maptype == 0)
                {
                    map.Load_M2(mapPath, MapDefine.Mir2_OldSchool);
                }
                map.ShowMeasureInfo();
            }
            else
            {
                Utils.LogErr("map file Invalid ");
            }
        }
        public static void TransToMir3()
        {
            string mapPath =param.mapPath;
            string extName = "_mir3yl";
            string ext = ".map";
            string savePath = string.IsNullOrEmpty(param.exportPath)
                              ? Path.Join(Path.GetDirectoryName(mapPath), Path.GetFileNameWithoutExtension(mapPath) + extName + ext)
                               : param.exportPath;

            if (VerifyMap(mapPath))
            {
                int maptype = Utils.FindType(mapPath);
                var map = new MapInfo();
                Utils.Log("读取源[mir2]地图");
                if (maptype == 2)
                {
                    map.Load_M2(mapPath, MapDefine.Mir2_ShengDa2012);

                }
                if (maptype == 0)
                {
                    map.Load_M2(mapPath, MapDefine.Mir2_OldSchool);
                }
                map.TransToMir2Format();
                map.Save(savePath);
            }
            else
            {
                Utils.LogErr("map file Invalid ");
            }

        }
        public static void ChangeLayerId()
        {
            string mapPath = param.mapPath;
            var map = new MapInfo();
            string savePath = param.exportPath;
            if (VerifyMap(mapPath))
            {
                Utils.Log("读取源[mir3youlong]地图");
                map.Load(mapPath);
                map.ChingeFIleId(param.changerLayer, param.sourceId, param.targetId);
                map.Save(savePath);
            }else
            {
                Utils.LogErr("map file Invalid ");
            }
        }
        public static void RenderMiniMao()
        {
            string mapPath = param.mapPath;
            string resLib = param.libPath;
            var map = new MapInfo();
            if (VerifyMap(mapPath))
            {
                Utils.Log($"create minimap from[{Path.GetFileNameWithoutExtension(mapPath)}]");
                map.Load(mapPath);
                MapRenderer renderer = new MapRenderer(map);
                renderer.SetLibPath(resLib);
                var bp = renderer.ToBitMap();
                Utils.Log($"minimap save to[{param.exportPath}]");
                bp.Save(param.exportPath);
            }
        }
        static void Main(string[] args)
        {
#if !DEBUG
            InitParams(args);

            if(param.vaild)
            {
                switch (param.cmdType)
                {
                    case CmdType.ShowInfo:
                        ShowInfo();
                        break;
                    case CmdType.TransToM3:
                        TransToMir3();
                        break;
                    case CmdType.TransFileId:
                        ChangeLayerId();
                        break;
                    case CmdType.RenderMiniMap:
                        RenderMiniMao();
                        break;
                }
                
                Utils.Log("end run-----------------------------------------------------");
            }
            else
            {
                Utils.LogErr("input params Invalid");
            }

#else

            string mapPath = @"E:\工作文档\1.美术资源管理\maps\M_FM2_29\M_FM2_29_src_mir3yl.map";
            string libPath = @"E:\工作文档\1.美术资源管理\maps\M_FM2_23";
            string extName = "_fix";
            string ext = ".map";
            string savePath = Path.Join(Path.GetDirectoryName(mapPath),
                                        Path.GetFileNameWithoutExtension(mapPath) + extName + ext);

            //var maptype = Utils.FindType(mapPath);

            var mapC = new MapInfo();
            mapC.Load(mapPath);
            //mapC.Load_M2(mapPath,MapDefine.Mir2_OldSchool);
            //mapC.TransToMir2Format();
            //MapRenderer renderer = new MapRenderer(mapC);
            //renderer.SetLibPath(libPath);

            //var bp = renderer.ToBitMap();
            // bp.Save(savePath);
            //mapC.SubTest();


            mapC.ChingeFIleId(MapLayerDefine.Tile, 10, 17);
            //mapC.ChingeFIleId(MapLayerDefine.SmTile, 150, 5);
            mapC.ChingeFIleId(MapLayerDefine.Object, 150, 17);
            //mapC.MeasureAll();
            //mapC.ShowMeasureInfo();
            mapC.Save(savePath);

            Utils.Log("end");
                  
#endif
        }
    }
  
}