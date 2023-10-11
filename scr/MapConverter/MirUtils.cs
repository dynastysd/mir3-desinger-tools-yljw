using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mir2.Map;
using Mir3YouLong.Map;

namespace MirUtils
{
    public static class Utils
    {
        public static int FindType(string ipath)
        {
            if(!File.Exists(ipath))
                return 9999;

            byte[] input = File.ReadAllBytes(ipath);

            //c# custom map format
            if ((input[2] == 0x43) && (input[3] == 0x23))
            {
                Utils.Log("server reader:c# custom map format");
                return 100;
            }
            //wemade mir3 maps have no title they just start with blank bytes
            if (input[0] == 0)
            {
                Utils.Log("server reader:wemade mir3 maps have no title they just start with blank bytes");
                return 5;
            }

            //shanda mir3 maps start with title: (C) SNDA, MIR3.
            if ((input[0] == 0x0F) && (input[5] == 0x53) && (input[14] == 0x33))
            {
                Utils.Log("server reader:shanda mir3 maps start with title: (C) SNDA, MIR3.");
                return 6;
            }

            //wemades antihack map (laby maps) title start with: Mir2 AntiHack
            if ((input[0] == 0x15) && (input[4] == 0x32) && (input[6] == 0x41) && (input[19] == 0x31))
            {
                Utils.Log("server reader:wemades antihack map (laby maps) title start with: Mir2 AntiHack");
                return 4;
            }


            //wemades 2010 map format i guess title starts with: Map 2010 Ver 1.0
            if ((input[0] == 0x10) && (input[2] == 0x61) && (input[7] == 0x31) && (input[14] == 0x31))
            {
                Utils.Log("server reader:wemades 2010 map format i guess title starts with: Map 2010 Ver 1.0");
                return 1;
            }


            //shanda's 2012 format and one of shandas(wemades) older formats share same header info, only difference is the filesize
            if ((input[4] == 0x0F) && (input[18] == 0x0D) && (input[19] == 0x0A))
            {
                Utils.Log("server reader:shanda's 2012 format and one of shandas(wemades) older formats share same header info, only difference is the filesize");
                int W = input[0] + (input[1] << 8);
                int H = input[2] + (input[3] << 8);
                if (input.Length > (52 + (W * H * 14)))
                    return 3;
                else
                    return 2;
            }

            //3/4 heroes map format (myth/lifcos i guess)
            if ((input[0] == 0x0D) && (input[1] == 0x4C) && (input[7] == 0x20) && (input[11] == 0x6D))
            {
                Utils.Log("server reader:3/4 heroes map format (myth/lifcos i guess)");
                return 7;
            }

            Utils.Log("server reader:default");
            return 0;
        }

        static string logpath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Logs.log");
        public static void Log(string message)
        {
            File.AppendAllText(logpath, $"{DateTime.Now}, Log ,{message}\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        public static void LogErr(string message)
        {
            File.AppendAllText(logpath, $"{DateTime.Now}, LogError ,{message}\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        public static void LogWord(string message)
        {
            File.AppendAllText(logpath, message);
            Console.Write(message);
        }               
    }
    public struct SRenderHead
    {
        public int Width;
        public int Height;
    }
    public struct SRenderCell
    {
        public byte Tile_FileIdx;
        public ushort Tile_ImgIdx;

        public byte SmTil_FileIdx;
        public ushort SmTil_ImgIdx;

        public byte Obj_FileIdx;
        public ushort Obj_ImgIdx;
    }
    public struct  BitmapInfo
    {
        public int offsX;
        public int offsY;
        public Bitmap data;
    }
    public class MapRenderer
    {
        SRenderHead head;
        SRenderCell[,] cells;

        public string LibPath;
        double scale;

        //96*64=3:2
        const int TILED_WIDTH = 48;
        const int TILED_HEIGHT = 32;

        const string OFFSET_FOLDER_NAME = "Placements";

        //public MapRenderer(MapReader mapSource)
        //{
        //    Init(mapSource.Width, mapSource.Height);
        //    for(int ix = 0; ix < head.Width; ix++)
        //    {
        //        for(int iy = 0;iy < head.Height; iy++)
        //        {
        //            cells[ix, iy].Tile_FileIdx = mapSource.MapCells[ix, iy].BackIndex;
        //            cells[ix, iy].Tile_ImgIdx = mapSource.MapCells[ix, iy].BackImage;

        //            cells[ix, iy].SmTil_FileIdx = mapSource.MapCells[ix, iy].MiddleIndex;
        //            cells[ix, iy].SmTil_ImgIdx = mapSource.MapCells[ix, iy].MiddleImage;

        //            cells[ix, iy].Obj_FileIdx = mapSource.MapCells[ix, iy].FrontIndex;
        //            cells[ix, iy].Obj_ImgIdx = mapSource.MapCells[ix, iy].FrontImage;
        //        }
        //    }
        //}

        public MapRenderer(MapInfo mapSource)
        {
            Init(mapSource.Width,mapSource.Height);
            for (int ix = 0; ix < head.Width; ix++)
            {
                for (int iy = 0; iy < head.Height; iy++)
                {
                    //mir3youlong
                    cells[ix, iy].Tile_FileIdx = mapSource.cells[ix, iy].TileFileIdx;
                    cells[ix, iy].Tile_ImgIdx = mapSource.cells[ix, iy].TileImgIdx;

                    cells[ix, iy].SmTil_FileIdx = mapSource.cells[ix, iy].smTileFileIdx;
                    cells[ix, iy].SmTil_ImgIdx = mapSource.cells[ix, iy].smTileImgIdx;

                    cells[ix, iy].Obj_FileIdx = mapSource.cells[ix, iy].ObjFileIdx;
                    cells[ix, iy].Obj_ImgIdx = mapSource.cells[ix, iy].ObjImgIdx;

                    //cells[ix, iy].Tile_FileIdx = mapSource.cells2[ix, iy].TileIdx;
                    //cells[ix, iy].Tile_ImgIdx = mapSource.cells2[ix, iy].TileImg;

                    //cells[ix, iy].SmTil_FileIdx = mapSource.cells2[ix, iy].SmTileIdx;
                    //cells[ix, iy].SmTil_ImgIdx = mapSource.cells2[ix, iy].SmTilImg;

                    //cells[ix, iy].Obj_FileIdx = mapSource.cells2[ix, iy].ObjIndex;
                    //cells[ix, iy].Obj_ImgIdx = mapSource.cells2[ix, iy].ObjImg;
                }
            }
        }
        public void SetLibPath(string libPath)
        {
            Utils.Log($"设置资源路径:{libPath}");
            this.LibPath = libPath;
        }
        void Init(int iw,int ih)
        {
            head = new SRenderHead();
            head.Width = iw;
            head.Height = ih;
            cells = new SRenderCell[iw,ih];

            scale = 0.25;

            Utils.Log("初始化图片生成器");           
            Utils.Log($"地图宽:[{iw}]");
            Utils.Log($"地图高:[{ih}]");
            Utils.Log($"缩放比例:{scale * 100}%");
        }
        public Bitmap ToBitMap()
        {
            return ToBitMap(RenderLayer.RenderAll);
        }
        public Bitmap ToBitMap(RenderLayer renderLayer)
        {
           

            int iniX = (int)(head.Width * TILED_WIDTH * scale);
            int iniY = (int)(head.Height * TILED_HEIGHT*scale);
            Utils.Log($"地图转换成像素图:[{iniX}pix]*[{iniY}pix]");
            Bitmap mapBitmap = new Bitmap(iniX, iniY);

            int scale_TILED_WIDTH = (int)(TILED_WIDTH * scale);
            int scale_TILED_HEIGHT = (int)(TILED_HEIGHT * scale);
            using (Graphics graphics = Graphics.FromImage(mapBitmap))
            for (int ix= 0; ix < head.Width; ix++)
            {
                for(int iy= 0; iy < head.Height; iy++)
                {
                    int offSetX = ix * scale_TILED_WIDTH;
                    int offSetY = iy * scale_TILED_HEIGHT;

                    //render Tiled layer
                    if (((int)renderLayer & 0x1) == 0x1)
                    {
                            if(MapInfo.IsMir2VaildCell( cells[ix, iy].Tile_FileIdx , cells[ix, iy].Tile_ImgIdx) && cells[ix, iy].Tile_ImgIdx != ushort.MaxValue)
                            {
                                var info = GetBitmapByIndex(cells[ix, iy].Tile_FileIdx, cells[ix, iy].Tile_ImgIdx, LayerType.Tiled);
                                graphics.DrawImage(info.data, info.offsX+offSetX , info.offsY + offSetY);
                            }
                            
                    }
                    //render SmTiled layer
                    if (((int)renderLayer & 0x2) == 0x2)
                    {
                            if (MapInfo.IsMir2VaildCell(cells[ix, iy].SmTil_FileIdx, cells[ix, iy].SmTil_ImgIdx) && cells[ix, iy].SmTil_ImgIdx != ushort.MaxValue)
                            {
                                var info = GetBitmapByIndex(cells[ix, iy].SmTil_FileIdx, cells[ix, iy].SmTil_ImgIdx, LayerType.SmTiled);
                                graphics.DrawImage(info.data, info.offsX + offSetX, info.offsY + offSetY);
                            }
                    }
                    //render Object layer
                    if (((int)renderLayer & 0x4) == 0x4)
                    {
                            if (MapInfo.IsMir2VaildCell(cells[ix, iy].Obj_FileIdx, cells[ix, iy].Obj_ImgIdx) && cells[ix, iy].Obj_ImgIdx != ushort.MaxValue)
                            {
                                var info = GetBitmapByIndex(cells[ix, iy].Obj_FileIdx, cells[ix, iy].Obj_ImgIdx, LayerType.Object);
                                graphics.DrawImage(info.data, info.offsX + offSetX, info.offsY + offSetY);
                            }
                    }
                }
            }
            
            return mapBitmap;
        }
        BitmapInfo GetBitmapByIndex(int iFileIdx, int iImgId,LayerType layer)
        {
            string folderPath = "";
            switch (layer)
            {
                case LayerType.Tiled:
                    folderPath = "Tiles";
                    break;
                case LayerType.SmTiled:
                    folderPath = "SmTiles";
                    break;
                case LayerType.Object:
                    folderPath = "Objects";
                    break;
            }
            folderPath = Path.Join(LibPath, folderPath + iFileIdx.ToString());

            string imgPath = string.Format("{0:D6}", iImgId) + ".png";
            string offsetPath = string.Format("{0:D6}", iImgId) + ".txt";

            imgPath = Path.Join (folderPath, imgPath);
            offsetPath = Path.Join (folderPath, OFFSET_FOLDER_NAME, offsetPath);
            BitmapInfo bitmapInfo = new BitmapInfo();

            if (File.Exists(offsetPath) && layer == LayerType.Object)
            {
                var offsets = File.ReadAllLines(offsetPath);
                bitmapInfo.offsX = (int)(int.Parse(offsets[0]) * scale);
                bitmapInfo.offsY = (int)(int.Parse(offsets[1]) * scale);
            }
            else
            {
                //Utils.LogErr($"file do not exists [{offsetPath}]");
                bitmapInfo.offsX = 0;
                bitmapInfo.offsY = 0;
            }

            if (File.Exists(imgPath))
            {
                using(var source = new Bitmap(imgPath))
                {
                    int sx = (int)(source.Width * scale);
                    int sy = (int)(source.Height * scale);

                    bitmapInfo.data  = new Bitmap(sx, sy);
                    Graphics graphics = Graphics.FromImage(bitmapInfo.data);
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(source, 0, 0, sx, sy);

                    return bitmapInfo;
                }
            }
            else
            {
                var msg = $"file do not exists [{imgPath}]";
                Utils.LogErr(msg);
                int sx = (int)(TILED_WIDTH * scale);
                int sy = (int)(TILED_WIDTH * scale);
                bitmapInfo.data = new Bitmap(sx, sy);
                using (Graphics g = Graphics.FromImage(bitmapInfo.data))
                {
                    Utils.Log("Creating a transparent");
                    g.Clear(Color.Transparent); // 透明
                }
                return bitmapInfo;
            }
                  
        }

        public enum LayerType
        {
            Tiled,
            SmTiled,
            Object
        }
        public enum RenderLayer
        {           
            OnlyTiled=1,
            OnlySmTiled=2,
            OnlyObject=4,
            TiledPlusSmTiled=3,
            TiledPlusObject=5, 
            SmTiledPlusObject = 6,
            RenderAll = 7
        }
    }

}
