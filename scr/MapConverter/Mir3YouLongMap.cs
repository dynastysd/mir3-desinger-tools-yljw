using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Mir2.Map;
using Mir2Svr.Map;
using MirUtils;
using System.Text;
using System.Linq;
using MapConverter;

namespace Mir3YouLong.Map
{
    using vFilesId = System.Int32;
    using vImgId = System.Int32;
    using vUesdTimes = System.Int32;

    public class MapInfo
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
        struct TMir2MapHead
        {
            public UInt16 Width;
            public UInt16 Height;
            public UInt128 Title;
            public UInt64 UpdateDate;
            public Byte Version;
            public UInt128 Recv1;
            public UInt32 Recv2;
            public Byte Recv3;
            public Byte Recv4;
            public Byte Recv5;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct TMir2MapFileInfo                  //用于Load Map 文件
        {
            public UInt16 TileImg;                      // +0  背景图片 TiTle.wil      $8000 Can't move and fly
            public UInt16 SmTilImg;                     // +2  中间地板 smTiles.wil
            public UInt16 ObjImg;                       // +4  物件图片 objectx.wil    $8000 Can't move but can fly
            public Byte DoorIndex;                      // +6  $80 尾数为 index
            public Byte DoorOffset;                     // +7  在objectX.wil中的位移
            public Byte AniFrame;                       // +8  动画结构 ????
            public Byte AniTick;                        // +9  动画点 ????
            public Byte ObjIndex;                       // +10  Object文件索引，有的版本字段叫 area
            public Byte light;                          // +11  灯 0..1..4
            public Byte TileIdx;                        // +12  Tiled 文件索引
            public Byte SmTileIdx;                      // +13  SmTiled 文件索引
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct TMir2MapFileInfo_12Byte                  //用于Load Map 文件
        {
            public UInt16 TileImg;                      // +0  背景图片 TiTle.wil      $8000 Can't move and fly
            public UInt16 SmTilImg;                     // +2  中间地板 smTiles.wil
            public UInt16 ObjImg;                       // +4  物件图片 objectx.wil    $8000 Can't move but can fly
            public Byte DoorIndex;                      // +6  $80 尾数为 index
            public Byte DoorOffset;                     // +7  在objectX.wil中的位移
            public Byte AniFrame;                       // +8  动画结构 ????
            public Byte AniTick;                        // +9  动画点 ????
            public Byte ObjIndex;                       // +10  Object文件索引，有的版本字段叫 area
            public Byte light;                          // +11  灯 0..1..4
            //public Byte TileIdx;                      // +12  Tiled 文件索引
            //public Byte SmTileIdx;                    // +13  SmTiled 文件索引
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct TMir3MapHeader
        {
            //{--------适用与新版传奇3地图（2020-12月）版本------------}
            //{--------总共40个字节，$28                               }
            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            // Byte[] Title;
            public UInt128 Title;
            public UInt16 Width;
            public UInt16 Height;
            public UInt16 Attr;
            public Byte Version;
            public Byte Rev;
            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            // Byte[] Resv;
            public UInt128 Resv;

        }
        const string HEADTITLE = "@Copyright-SX";

        //{      2020/12/2    SDNewMir3定义                              }
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct TMir3MapFileInfo
        {
            public Byte Flag;
            public Byte TileFileIdx;
            public Byte smTileFileIdx;      //----CellFileIdx
            public Byte ObjFileIdx;
            public UInt16 TileImgIdx;       //imglarge  : Word;
            public UInt16 smTileImgIdx;     //imgsmall  : Word;
            public UInt16 ObjImgIdx;        //imgobject : Word;
            public Byte ObjAni_1;           //doorindex : Byte;
            public Byte ObjAni_2;           //dooroffset: Byte;
            public UInt16 Light;            //aniframe  : Byte;
            public Byte FlagEx;             //anitick   : Byte;
            public Byte Reserve;            //area      : Byte;
            public Int32 ReserveEx;         //light     : Byte;
        }


        TMir3MapHeader dHead;
        TMir3MapFileInfo[,] dCells;
        public bool IsMir3DataInied;
        public TMir3MapFileInfo[,] cells { get { return dCells; } }

        TMir2MapHead dHeadM2;
        TMir2MapFileInfo[,] dCellsM2;
        public bool IsMir2DataInied;
        public TMir2MapFileInfo[,] cells2 { get { return dCellsM2; } }


        public Dictionary<vFilesId, Dictionary<vImgId, vUesdTimes>> CountTiles;
        public Dictionary<vFilesId, Dictionary<vImgId, vUesdTimes>> CountSmTiles;
        public Dictionary<vFilesId, Dictionary<vImgId, vUesdTimes>> CountObjects;
        public void ReSetCount()
        {
            Utils.Log("clear count----");
            //foreach(var kvp in CountObjects)
            //{
            //    var a= kvp.Value.Where(kv => kv.Value < short.MaxValue);
            //    Utils.Log($"file [ {kvp.Key}],files samller than 32767 [{a.ToList<KeyValuePair<vImgId, vUesdTimes>>().Count}]");
            //}
            
            CountTiles.Clear();
            CountSmTiles.Clear();
            CountObjects.Clear();
        }
        public void MeasureTile(TMir2MapFileInfo icell )
        {

            if (icell.TileIdx >= 0)
            {
                if (!CountTiles.ContainsKey(icell.TileIdx))
                {
                    CountTiles.Add(icell.TileIdx, new Dictionary<int, int>());
                    CountTiles[icell.TileIdx].Add(icell.TileImg, 1);
                }
                else
                {
                    if(CountTiles[icell.TileIdx].ContainsKey(icell.TileImg))
                        CountTiles[icell.TileIdx][icell.TileImg] += 1;
                    else
                        CountTiles[icell.TileIdx].Add(icell.TileImg, 1);
                }                   
            }
            if (icell.SmTileIdx >= 0)
            {
                if (!CountSmTiles.ContainsKey(icell.SmTileIdx))
                {
                    CountSmTiles.Add(icell.SmTileIdx, new Dictionary<int, int>());
                    CountSmTiles[icell.SmTileIdx].Add(icell.SmTilImg, 1);
                }
                else
                {
                    if (CountSmTiles[icell.SmTileIdx].ContainsKey(icell.SmTilImg))
                        CountSmTiles[icell.SmTileIdx][icell.SmTilImg] += 1;
                    else
                        CountSmTiles[icell.SmTileIdx].Add(icell.SmTilImg, 1);
                }
            }
            if (icell.ObjIndex >= 0)
            {
                if (!CountObjects.ContainsKey(icell.ObjIndex))
                {
                    CountObjects.Add(icell.ObjIndex, new Dictionary<int, int>());
                    CountObjects[icell.ObjIndex].Add(icell.ObjImg, 1);
                }
                else
                {
                    if (CountObjects[icell.ObjIndex].ContainsKey(icell.ObjImg))
                        CountObjects[icell.ObjIndex][icell.ObjImg] += 1;
                    else
                        CountObjects[icell.ObjIndex].Add(icell.ObjImg, 1);
                }
            }
        }
        public void MeasureTile(TMir3MapFileInfo icell)
        {
            var jump = false;
            if (jump)
                return;

            if (icell.TileFileIdx >= 0)
            {
                if (!CountTiles.ContainsKey(icell.TileFileIdx))
                {
                    CountTiles.Add(icell.TileFileIdx, new Dictionary<int, int>());
                    CountTiles[icell.TileFileIdx].Add(icell.TileImgIdx, 1);
                }
                else
                {
                    if (CountTiles[icell.TileFileIdx].ContainsKey(icell.TileImgIdx))
                        CountTiles[icell.TileFileIdx][icell.TileImgIdx] += 1;
                    else
                        CountTiles[icell.TileFileIdx].Add(icell.TileImgIdx, 1);
                }
            }
            if (icell.smTileFileIdx >= 0)
            {
                if (!CountSmTiles.ContainsKey(icell.smTileFileIdx))
                {
                    CountSmTiles.Add(icell.smTileFileIdx, new Dictionary<int, int>());
                    CountSmTiles[icell.smTileFileIdx].Add(icell.smTileImgIdx, 1);
                }
                else
                {
                    if (CountSmTiles[icell.smTileFileIdx].ContainsKey(icell.smTileImgIdx))
                        CountSmTiles[icell.smTileFileIdx][icell.smTileImgIdx] += 1;
                    else
                        CountSmTiles[icell.smTileFileIdx].Add(icell.smTileImgIdx, 1);
                }
            }
            if (icell.ObjFileIdx >= 0)
            {
                if (!CountObjects.ContainsKey(icell.ObjFileIdx))
                {
                    CountObjects.Add(icell.ObjFileIdx, new Dictionary<int, int>());
                    CountObjects[icell.ObjFileIdx].Add(icell.ObjImgIdx, 1);
                }
                else
                {
                    if (CountObjects[icell.ObjFileIdx].ContainsKey(icell.ObjImgIdx))
                        CountObjects[icell.ObjFileIdx][icell.ObjImgIdx] += 1;
                    else
                        CountObjects[icell.ObjFileIdx].Add(icell.ObjImgIdx, 1);
                }
            }
        }
        public void MeasureAll()
        {
            if (IsMir2DataInied)
            {
                Utils.Log("mir2 MeasureAll");
            }else if (IsMir3DataInied)
            {
                Utils.Log("mir3 MeasureAll");
            }
            for (int ix = 0; ix < this.width; ix++) 
            {
                for (int iy = 0; iy < this.height; iy++)
                {
                    if (IsMir2DataInied)
                    {
                        MeasureTile(dCellsM2[ix, iy]);
                        continue;
                    }
                    if (IsMir3DataInied)
                    {
                        MeasureTile(dCells[ix, iy]);
                    }
                }
            }
                
        }
        public void ShowMeasureInfo()
        {
            Utils.Log("----------MapInfo----------");
            Utils.Log("--Map Size:");
            Utils.Log($"----Map Size Width:{this.width}");
            Utils.Log($"----Map Size Height:{this.height}");
            Utils.Log($"--Tiles Files count:{CountTiles.Count}");
            foreach(var info in CountTiles)
            {
                Utils.Log($"----Tiles File [{info.Key}] ,iamge used num:[{info.Value.Count}]");
            }
            Utils.Log($"--SmTiles Files count:{CountSmTiles.Count}");
            foreach (var info in CountSmTiles)
            {
                Utils.Log($"----SmTiles File [{info.Key}] ,iamge used num:[{info.Value.Count}]");
            }
            Utils.Log($"--Objects Files count:{CountObjects.Count}");
            foreach (var info in CountObjects)
            {
                Utils.Log($"----Object File [{info.Key}] ,iamge used num:[{info.Value.Count}]");
            }
            Utils.Log("---------MapInfoEnd----------");
        }
        public static MapInfo CreateEmpty(int width, int height)
        {
            var pMap = new MapInfo();
            pMap.width = width;
            pMap.height = height;
            pMap.dHead = new TMir3MapHeader();

            pMap.dHead.Width = (ushort)width;
            pMap.dHead.Height = (ushort)height;
            pMap.dHead.Attr = 0;
            pMap.dHead.Version = 0;

            pMap.dCells = new TMir3MapFileInfo[width, height];
            return pMap;
        }
        public MapInfo()
        {
            CountTiles = new Dictionary<int, Dictionary<int,int>>();
            CountSmTiles = new Dictionary<int, Dictionary<int, int>>();
            CountObjects = new Dictionary<int, Dictionary<int, int>>();
        }
        public void CopyCells(MapReader clientReader, SvrMapReader serverReader)
        {
            //verify source
            if (clientReader.Height != serverReader.Height || clientReader.Width != serverReader.Width)
            {
                Utils.LogErr("clientReader and serverReader do not match!!!");
                return;
            }

            for (int x = 0; x < clientReader.Width; x++)
                for (int y = 0; y < clientReader.Height; y++)
                {
                    dCells[x, y] = new TMir3MapFileInfo();
                    var ims = clientReader.MapCells[x, y];

                    //only "Flag" is read from server,other from client
                    dCells[x, y].Flag = (byte)(serverReader.Cells[x, y].Valid ? 0x03 : 0x02);

                    dCells[x, y].TileFileIdx = (byte)ims.BackIndex;
                    dCells[x, y].TileImgIdx = (UInt16)ims.BackImage;

                    dCells[x, y].smTileFileIdx = (byte)ims.MiddleIndex;
                    dCells[x, y].smTileImgIdx = (UInt16)ims.MiddleImage;

                    dCells[x, y].ObjFileIdx = (byte)ims.FrontIndex;
                    dCells[x, y].ObjImgIdx = (UInt16)ims.FrontImage;

                    dCells[x, y].ObjAni_1 = ims.DoorIndex;
                    dCells[x, y].ObjAni_2 = ims.DoorOffset;

                    dCells[x, y].Light = ims.FrontAnimationFrame;
                    dCells[x, y].FlagEx = 0;
                    dCells[x, y].Reserve = 0;
                    dCells[x, y].ReserveEx = ims.Light;
                }
            Utils.Log("copy finish");
        }
        public void TransToMir2Format()
        {
            Utils.Log("trans map format from Mir2 to Mir3.Youlong");
            dHead = new TMir3MapHeader();
            dHead.Title = BitConverter.ToUInt64(ASCIIEncoding.ASCII.GetBytes(HEADTITLE));
            dHead.Width = dHeadM2.Width;
            dHead.Height = dHeadM2.Height;
            dHead.Attr = 0;
            //version == 1 的时候，客户端读取pkg的逻辑会变化，根据fieid去对应目录里找
            dHead.Version = 1;
            dCells = new TMir3MapFileInfo[dHead.Width, dHead.Height];
            IsMir3DataInied = true;

            for (int ix = 0; ix < dHead.Width; ix++)
            {
                for (int iy = 0; iy < dHead.Height; iy++)
                {
                    //img = 0xffff (ushort.MaxValue) 时才不显示，不然找一张图片在展示
                    //set tile layer
                    if (IsMir2VaildCell( dCellsM2[ix, iy].TileIdx, dCellsM2[ix, iy].TileImg )) 
                    {
                        dCells[ix, iy].TileFileIdx = (byte)(dCellsM2[ix, iy].TileIdx + 1) ;
                        if(dCellsM2[ix, iy].TileImg > short.MaxValue)
                            dCells[ix, iy].TileImgIdx = (ushort)(dCellsM2[ix, iy].TileImg - short.MaxValue - 2);
                        else
                            dCells[ix, iy].TileImgIdx = (ushort)(dCellsM2[ix, iy].TileImg - 1);
                    }
                    else
                    {
                        SetNullTile(ref dCells[ix, iy].TileFileIdx, ref dCells[ix, iy].TileImgIdx);
                    }

                    //set smtile layer
                    if (IsMir2VaildCell(dCellsM2[ix, iy].SmTileIdx, dCellsM2[ix, iy].SmTilImg))
                    {
                        dCells[ix, iy].smTileFileIdx = (byte)(dCellsM2[ix, iy].SmTileIdx + 1);
                        if (dCellsM2[ix, iy].SmTilImg > short.MaxValue)
                            dCells[ix, iy].smTileImgIdx = (ushort)(dCellsM2[ix, iy].SmTilImg - short.MaxValue - 2);
                        else
                            dCells[ix, iy].smTileImgIdx = (ushort)(dCellsM2[ix, iy].SmTilImg - 1);
                    }
                    else
                    {
                        SetNullTile(ref dCells[ix, iy].smTileFileIdx, ref dCells[ix, iy].smTileImgIdx);
                    }

                    if (IsMir2VaildCell(dCellsM2[ix, iy].ObjIndex, dCellsM2[ix, iy].ObjImg))
                    {
                        dCells[ix, iy].ObjFileIdx = (byte)(dCellsM2[ix, iy].ObjIndex + 1);
                        if (dCellsM2[ix, iy].ObjImg > short.MaxValue)
                            dCells[ix, iy].ObjImgIdx = (ushort)(dCellsM2[ix, iy].ObjImg - short.MaxValue - 2);
                        else
                            dCells[ix, iy].ObjImgIdx = (ushort)(dCellsM2[ix, iy].ObjImg - 1);
                    }
                    else
                    {
                        SetNullTile(ref dCells[ix, iy].ObjFileIdx, ref dCells[ix, iy].ObjImgIdx);
                    }

                    dCells[ix, iy].ObjAni_1 = 255;
                    dCells[ix, iy].ObjAni_2 = 255;
                    dCells[ix, iy].Flag = (byte)(IsBlock(ix, iy) ? 0 : 3);

                    
                }

            }
            Utils.Log("trans finish");

        }
        public void ChingeFIleId(MapLayerDefine mapLayer, int srcId,int tarId)
        { 
            Utils.Log($"change filed id in lay[{mapLayer.ToString()}]");
            Utils.Log($"id from [{srcId}] to [{tarId}]");
            int counter = 0;

            for (int ix = 0; ix < this.width; ix++)
            {
                for (int iy = 0; iy < this.height; iy++)
                {
                    if (mapLayer == MapLayerDefine.Tile)
                        if (dCells[ix, iy].TileFileIdx == srcId)
                        {
                            counter++;
                            dCells[ix, iy].TileFileIdx = (byte)tarId;
                        }
                            
                    if (mapLayer == MapLayerDefine.SmTile)
                        if (dCells[ix, iy].smTileFileIdx == srcId)
                        {
                            counter++;
                            dCells[ix, iy].smTileFileIdx = (byte)tarId;
                        }
                            

                    if (mapLayer == MapLayerDefine.Object)
                        if (dCells[ix, iy].ObjFileIdx == srcId)
                        {
                            counter++;
                            dCells[ix, iy].ObjFileIdx = (byte)tarId;
                        }
                            
                }
            }

            Utils.Log($"[{counter}] imgs has change id");
            Utils.Log($"chinge [{mapLayer.ToString()}] id finish.");
        }
        public void SetNullTile(ref byte fileid ,ref ushort imgid)
        {
            fileid = 0;
            imgid = ushort.MaxValue;
        }
        public static bool IsMir2VaildCell( byte fileid,  ushort imgid) 
        {
            if(fileid == 0 |
                imgid == 0 ) 
                return false;
            else
                return true;
        }
        public static bool IsMir3VaildCell(byte fileid, ushort imgid)
        {
            if (fileid == 0 ||
                imgid == 0 ||
                imgid == 65535)
                return false;
            else
                return true;
        }
        enum CellFlag
        {
            Free,
            DenyFly,
            DenyMove,
        }
        const int MAX_MAP_SIZE_MIR2 = 1024;
        public void WriteTest(TMir3MapFileInfo sample)
        {
            for (int ix = 0; ix < this.width; ix++)
            {
                for (int iy = 0; iy < this.height; iy++)
                {
                    this.dCells[ix, iy].Flag = sample.Flag;
                    this.dCells[ix, iy].TileFileIdx = sample.TileFileIdx;
                    this.dCells[ix, iy].smTileFileIdx = sample.smTileFileIdx;
                    this.dCells[ix, iy].ObjFileIdx = sample.ObjFileIdx;
                    this.dCells[ix, iy].TileImgIdx = sample.TileImgIdx;
                    this.dCells[ix, iy].smTileImgIdx = sample.smTileImgIdx;
                    this.dCells[ix, iy].ObjImgIdx = sample.ObjImgIdx;
                    this.dCells[ix, iy].ObjAni_1 = sample.ObjAni_1;
                    this.dCells[ix, iy].ObjAni_2 = sample.ObjAni_2;
                    this.dCells[ix, iy].Light = sample.Light;
                    this.dCells[ix, iy].FlagEx = sample.FlagEx;
                    this.dCells[ix, iy].Reserve = sample.Reserve;
                    this.dCells[ix, iy].ReserveEx = sample.ReserveEx;
                }
            }
        }
        public bool Load_M2(string path,MapDefine mapDefine)
        {
            int bufLen = Math.Max(Marshal.SizeOf<TMir2MapHead>(), Marshal.SizeOf<TMir2MapFileInfo>());
            Span<byte> buf = stackalloc byte[bufLen];
            using (var fs = File.OpenRead(path))
            {
                fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir2MapHead>()));
                dHeadM2 = MemoryMarshal.Read<TMir2MapHead>(buf);
                //ref var header = ref MemoryMarshal.AsRef<TMir2MapHead>(buf);

                if (dHeadM2.Width > MAX_MAP_SIZE_MIR2 || dHeadM2.Height > MAX_MAP_SIZE_MIR2)
                {
                    throw new Exception($"ivnalid map size width={dHeadM2.Width} height={dHeadM2.Height}");
                }

                int padding = 0;
                int fix = 0;
                if (mapDefine == MapDefine.Mir2_OldSchool)
                    fix = 2;

                //这个是12字节的版本的兼容方法，改为14字节了
                switch (dHeadM2.Version)
                {
                    case 2: padding = 0+fix; break;
                    case 6: padding = 22+fix; break;
                    case 0: padding = 0; break;
                    case 13:
                    case 36: padding = 0; break;
                    case 196: padding = 0; break;
                    default:
                        throw new Exception($"invalid map version={dHeadM2.Version} path={path}");
                }
                ////这个是12字节的版本,鹏哥服务器的兼容方法
                //switch (dHeadM2.Version)
                //{
                //    case 2: padding = 2 ; break;
                //    case 6: padding = 24 ; break;
                //    case 0: 
                //    case 13:
                //    case 36: padding = 0; break;
                //    default:
                //        throw new Exception($"invalid map version={dHeadM2.Version} path={path}");
                //}

                width = dHeadM2.Width;
                height = dHeadM2.Height;
                blocks = new BitArray(width * height);
                dCellsM2 = new TMir2MapFileInfo[width, height];
                IsMir2DataInied = true;
                randWalkList.Clear();

                List<Pos> freePos = new List<Pos>();
                for (int ix = 0; ix < width; ix++)
                {
                    freePos.Clear();
                    for (int iy = 0; iy < height; iy++)
                    {
                        if(mapDefine == MapDefine.Mir2_OldSchool)
                        {
                            fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir2MapFileInfo_12Byte>()));
                            var temp = MemoryMarshal.Read<TMir2MapFileInfo_12Byte>(buf);
                            dCellsM2[ix, iy] = new TMir2MapFileInfo();
                            dCellsM2[ix, iy].TileImg = temp.TileImg;
                            dCellsM2[ix, iy].SmTilImg = temp.SmTilImg;
                            dCellsM2[ix, iy].ObjImg = temp.ObjImg;
                            dCellsM2[ix, iy].DoorIndex = temp.DoorIndex;
                            dCellsM2[ix, iy].DoorOffset = temp.DoorOffset;
                            dCellsM2[ix, iy].AniFrame = temp.AniFrame;
                            dCellsM2[ix, iy].AniTick = temp.AniTick;
                            dCellsM2[ix, iy].ObjIndex = temp.ObjIndex;
                            dCellsM2[ix, iy].light = temp.light;
                            dCellsM2[ix, iy].TileIdx = 0;
                            dCellsM2[ix, iy].SmTileIdx = 0;
                        }
                        else
                        {
                            fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir2MapFileInfo>()));
                            dCellsM2[ix, iy] = MemoryMarshal.Read<TMir2MapFileInfo>(buf);
                        }
                        
                        //ref var cell = ref MemoryMarshal.AsRef<TMir2MapFileInfo>(buf);

                        bool block = false;
                        if ((dCellsM2[ix, iy].TileImg & 0x8000) != 0)
                        { // DenyFly
                            block = true;
                        }
                        else if ((dCellsM2[ix, iy].ObjImg & 0x8000) != 0)
                        { // DenyMove
                            block = true;
                        }
                        else
                        {
                            block = false;
                            freePos.Add(new Pos(ix, iy));
                        }
                        SetBlock(ix, iy, block);

                        if (padding > 0)
                        {
                            fs.Seek(padding, SeekOrigin.Current);
                        }
                    }
                    randWalkList.AddRange(ShuffleIterator(freePos, Random.Shared).Take(2));
                }
            }
            return true;
        }

        const int MAX_MAP_SIZE = 800;
        public bool Load(string path)
        {
            Utils.Log($"load file[{Path.GetFileNameWithoutExtension(path)}]");
            int bufLen = Math.Max(Marshal.SizeOf<TMir3MapHeader>(), Marshal.SizeOf<TMir3MapFileInfo>());
            Span<byte> buf = stackalloc byte[bufLen];
            using (var fs = File.OpenRead(path))
            {
                fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir3MapHeader>()));
                dHead = new TMir3MapHeader();
                dHead =  MemoryMarshal.AsRef<TMir3MapHeader>(buf);

                if (dHead.Width > MAX_MAP_SIZE || dHead.Height > MAX_MAP_SIZE)
                {
                    throw new Exception($"ivnalid map size width={dHead.Width} height={dHead.Height}");
                }

                width = dHead.Width;
                height = dHead.Height;
                blocks = new BitArray(width * height);
                randWalkList.Clear();
                dCells = new TMir3MapFileInfo[width, height];
                IsMir3DataInied = true;

                List<Pos> freePos = new List<Pos>();
                for (int ix = 0; ix < width; ix++)
                {
                    freePos.Clear();
                    for (int iy = 0; iy < height; iy++)
                    {
                        fs.ReadExactly(buf.Slice(0, Marshal.SizeOf<TMir3MapFileInfo>()));
                        dCells[ix, iy] = MemoryMarshal.AsRef<TMir3MapFileInfo>(buf);

                        bool block = false;
                        if ((dCells[ix, iy].Flag & 0x2) != 0x2)
                        { // DenyFly
                            block = true;
                        }
                        else if ((dCells[ix, iy].Flag & 0x1) != 0x1)
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

            // RenderBlockInConsole();
            return true;
        }
        public bool Save(string path)
        {
            int bufLen = Math.Max(Marshal.SizeOf<TMir3MapHeader>(), Marshal.SizeOf<TMir3MapFileInfo>());
            //Span<byte> buf = stackalloc byte[bufLen];
            using (var fs = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                //write head
                byte[] headerBytes = new byte[Marshal.SizeOf<TMir3MapHeader>()];
                IntPtr headerPtr = Marshal.AllocHGlobal(bufLen);
                Marshal.StructureToPtr(dHead, headerPtr, false);
                Marshal.Copy(headerPtr, headerBytes, 0, bufLen);
                Marshal.FreeHGlobal(headerPtr);
                writer.Write(headerBytes);

                for (int ix = 0; ix < width; ix++)
                {
                    for (int iy = 0; iy < height; iy++)
                    {
                        Span<byte> buff = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref dCells[ix, iy], 1));
                        writer.Write(buff);
                    }
                }
            }
            Utils.Log($"file save to [{path}]");
            return true;
        }
        public void RenderBlockInConsole()
        {
            for (int ix = 0; ix < width; ix++)
            {
                Utils.LogWord((ix + 1).ToString());
                for (int iy = 0; iy < height; iy++)
                {
                    if (IsBlock(ix, iy))
                    {
                        Utils.LogWord("-");
                    }
                    else
                    {
                        Utils.LogWord("#");
                    }
                }
                Utils.LogWord("\n");
            }
        }
        public void RenderVaildInConsole()
        {
            //for (int rol = 0; rol < width; rol++)
            //   // Utils.LogWord((rol + 1).ToString());

            for (int ix = 0; ix < width; ix++)
            {
                Utils.LogWord((ix + 1).ToString());
                for (int iy = 0; iy < height; iy++)
                {
                    if ((dCells[ix, iy].Flag & 0x2) != 0x2 || (dCells[ix, iy].Flag & 0x1) != 0x1)
                    {
                        Utils.LogWord("-");
                    }
                    else
                    {
                        Utils.LogWord("#");
                    }
                }
                Utils.LogWord("\n");
            }
        }
        public void SubTest()
        {
            for(int ix = 0;ix < width; ix++)
            {
                for(int iy = 0; iy < height;iy++)
                {
                    if (dCellsM2[ix,iy].ObjImg > 0)
                    {
                        var c = dCellsM2[ix, iy];
                        if( dCellsM2[ix, iy].ObjImg > short.MaxValue)
                            Utils.Log($"{ix},{iy},{c.ObjImg - short.MaxValue}");
                        else
                            Utils.Log($"{ix},{iy},{c.ObjImg }");
                    }
                    
                }
            }
            Utils.Log("sub end------------");
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

    //class MapInfoMgr {
    //    protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    //    private static Dictionary<int, MapInfo> mapInfoDict = new ();

    //    public static void Init() {
    //        Dictionary<string, MapInfo> nameInfoDict = new ();

    //        foreach (var cfg in Cfg.Map.All()) {
    //            string mapFile = cfg.name.TrimStart('~') + ".map";
    //            if (!nameInfoDict.TryGetValue(mapFile, out var info)) {
    //                info = new MapInfo();
    //                var mapFilePath = Path.Join(Logic.Res.DataDir, "map", mapFile);
    //                logger.Info("load map {}", mapFilePath);
    //                if (!info.Load(mapFilePath)) {
    //                    logger.Error("load map failed {}", mapFilePath);
    //                    continue;
    //                }   
    //                nameInfoDict.Add(mapFile, info);
    //            }

    //            mapInfoDict.Add(cfg.id, info);
    //        }
    //    }

    //    public static bool TryGetMapInfo(int mapId, [MaybeNullWhen(false)] out MapInfo info) {
    //        return mapInfoDict.TryGetValue(mapId, out info);
    //    }
    //}

} // namespace