using MirUtils;
using System.Drawing;

// Github: https://github.com/Suprcode/mir2
//old code form mir2 client
//namespace Client.MirObjects
//file local: mir2-master\Client\MirObjects\MapCode.cs
namespace Mir2.Map
{
    public class CellInfo
    {
        public short BackIndex;
        public int BackImage;
        public short MiddleIndex;
        public int MiddleImage;
        public short FrontIndex;
        public int FrontImage;

        public byte DoorIndex;
        public byte DoorOffset;

        public byte FrontAnimationFrame;
        public byte FrontAnimationTick;

        public byte MiddleAnimationFrame;
        public byte MiddleAnimationTick;

        public short TileAnimationImage;
        public short TileAnimationOffset;
        public byte  TileAnimationFrames;

        public byte Light;
        public byte Unknown;
        //public List<MapObject> CellObjects;

        public bool FishingCell;

        //public void AddObject(MapObject ob)
        //{
        //    if (CellObjects == null) CellObjects = new List<MapObject>();

        //    CellObjects.Insert(0, ob);
        //    Sort();
        //}
        //public void RemoveObject(MapObject ob)
        //{
        //    if (CellObjects == null) return;

        //    CellObjects.Remove(ob);

        //    if (CellObjects.Count == 0) CellObjects = null;
        //    else Sort();
        //}
        //public MapObject FindObject(uint ObjectID)
        //{
        //    return CellObjects.Find(
        //        delegate(MapObject mo)
        //    {
        //        return mo.ObjectID == ObjectID;
        //    });
        //}
        //public void DrawObjects()
        //{
        //    if (CellObjects == null) return;

        //    for (int i = 0; i < CellObjects.Count; i++)
        //    {
        //        if (!CellObjects[i].Dead)
        //        {
        //            CellObjects[i].Draw();
        //            continue;
        //        }

        //        if(CellObjects[i].Race == ObjectType.Monster)
        //        {
        //            switch(((MonsterObject)CellObjects[i]).BaseImage)
        //            {
        //                case Monster.PalaceWallLeft:
        //                case Monster.PalaceWall1:
        //                case Monster.PalaceWall2:
        //                case Monster.SSabukWall1:
        //                case Monster.SSabukWall2:
        //                case Monster.SSabukWall3:
        //                case Monster.HellLord:
        //                    CellObjects[i].Draw();
        //                    break;
        //                default:
        //                    continue;
        //            }
        //        }
        //    }
        //}

        //public void DrawDeadObjects()
        //{
        //    if (CellObjects == null) return;
        //    for (int i = 0; i < CellObjects.Count; i++)
        //    {
        //        if (!CellObjects[i].Dead) continue;

        //        if (CellObjects[i].Race == ObjectType.Monster)
        //        {
        //            switch (((MonsterObject)CellObjects[i]).BaseImage)
        //            {
        //                case Monster.PalaceWallLeft:
        //                case Monster.PalaceWall1:
        //                case Monster.PalaceWall2:
        //                case Monster.SSabukWall1:
        //                case Monster.SSabukWall2:
        //                case Monster.SSabukWall3:
        //                case Monster.HellLord:
        //                    continue;
        //            }
        //        }

        //        CellObjects[i].Draw();
        //    }
        //}

        //public void Sort()
        //{
        //    CellObjects.Sort(delegate(MapObject ob1, MapObject ob2)
        //    {
        //        if (ob1.Race == ObjectType.Item && ob2.Race != ObjectType.Item)
        //            return -1;
        //        if (ob2.Race == ObjectType.Item && ob1.Race != ObjectType.Item)
        //            return 1;
        //        if (ob1.Race == ObjectType.Spell && ob2.Race != ObjectType.Spell)
        //            return -1;
        //        if (ob2.Race == ObjectType.Spell && ob1.Race != ObjectType.Spell)
        //            return 1;

        //        int i = ob2.Dead.CompareTo(ob1.Dead);
        //        return i == 0 ? ob1.ObjectID.CompareTo(ob2.ObjectID) : i;
        //    });
        //}
    }

    public class MapReader
    {
        public int Width, Height;
        public CellInfo[,] MapCells;
        private string FileName;
        private byte[] Bytes;
        
        public MapReader(string FileName)
        {
            this.FileName = FileName;

            initiate();
        }

        private void initiate()
        {
            if (File.Exists(FileName))
            {
                Bytes = File.ReadAllBytes(FileName);
            }
            else
            {
                Width = 1000;
                Height = 1000;
                MapCells = new CellInfo[Width, Height];

                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        MapCells[x, y] = new CellInfo();
                    }
                return;
            }


            //c# custom map format
            if ((Bytes[2] == 0x43) && (Bytes[3] == 0x23))
            {
                Utils.Log("client reader:custom map format");
                LoadMapType100();
                return;
            }

            //wemade mir3 maps have no title they just start with blank bytes
            if (Bytes[0] == 0)
            {
                Utils.Log("client reader:wemade mir3 maps have no title they just start with blank bytes");
                LoadMapType5();
                return;
            }
            //shanda mir3 maps start with title: (C) SNDA, MIR3.
            if ((Bytes[0] == 0x0F) && (Bytes[5] == 0x53) && (Bytes[14] == 0x33))
            {
                Utils.Log("client reader:shanda mir3 maps start with title: (C) SNDA, MIR3.");
                LoadMapType6();
                return;
            }
            //wemades antihack map (laby maps) title start with: Mir2 AntiHack
            if ((Bytes[0] == 0x15) && (Bytes[4] == 0x32) && (Bytes[6] == 0x41) && (Bytes[19] == 0x31))
            {
                Utils.Log("client reader:wemades antihack map (laby maps) title start with: Mir2 AntiHack");
                LoadMapType4();
                return;
            }
            //wemades 2010 map format i guess title starts with: Map 2010 Ver 1.0
            if ((Bytes[0] == 0x10) && (Bytes[2] == 0x61) && (Bytes[7] == 0x31) && (Bytes[14] == 0x31))
            {
                Utils.Log("client reader:wemades 2010 map format i guess title starts with: Map 2010 Ver 1.0");
                LoadMapType1();
                return;
            }
            //shanda's 2012 format and one of shandas(wemades) older formats share same header info, only difference is the filesize
            if ((Bytes[4] == 0x0F) || (Bytes[4] == 0x03) && (Bytes[18] == 0x0D) && (Bytes[19] == 0x0A))
            {
                Utils.Log("shanda's 2012 format and one of shandas(wemades) older formats share same header info, only difference is the filesize");
                int W = Bytes[0] + (Bytes[1] << 8);
                int H = Bytes[2] + (Bytes[3] << 8);
                if (Bytes.Length > (52 + (W*H*14)))
                {                   
                    LoadMapType3();
                    return;
                }
                else
                {

                    LoadMapType2();
                    return;
                }
            }

            //3/4 heroes map format (myth/lifcos i guess)
            if ((Bytes[0] == 0x0D) && (Bytes[1] == 0x4C) && (Bytes[7] == 0x20) && (Bytes[11] == 0x6D))
            {
                Utils.Log("client reader:3/4 heroes map format (myth/lifcos i guess)");
                LoadMapType7();
                return;
            }

            //if it's none of the above load the default old school format
            Utils.Log("client reader:default old school format");
            LoadMapType0();
        }

        private void LoadMapType0()
        {
            try
            {
                int offset = 0;
                Width = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                Height = BitConverter.ToInt16(Bytes, offset);
                MapCells = new CellInfo[Width, Height];
                offset = 52;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {//12
                        MapCells[x, y] = new CellInfo();
                        MapCells[x, y].BackIndex = 0;
                        MapCells[x, y].MiddleIndex = 1;
                        MapCells[x, y].BackImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].MiddleImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].FrontImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].DoorIndex = (byte)(Bytes[offset++] & 0x7F);
                        MapCells[x, y].DoorOffset = Bytes[offset++];
                        MapCells[x, y].FrontAnimationFrame = Bytes[offset++];
                        MapCells[x, y].FrontAnimationTick = Bytes[offset++];
                        MapCells[x, y].FrontIndex = (short)(Bytes[offset++]+ 2);
                        MapCells[x, y].Light = Bytes[offset++];
                        if ((MapCells[x, y].BackImage & 0x8000) != 0)
                            MapCells[x, y].BackImage = (MapCells[x, y].BackImage & 0x7FFF) | 0x20000000;

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;


                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
                //if (Settings.LogErrors) CMain.SaveError(ex.ToString());
            }

        }

        private void LoadMapType1()
        {
            try
            {
                int offSet = 21;
                   
                int w = BitConverter.ToInt16(Bytes, offSet);
                offSet += 2;
                int xor = BitConverter.ToInt16(Bytes, offSet);
                offSet += 2;
                int h = BitConverter.ToInt16(Bytes, offSet);
                Width = w ^ xor;
                Height = h ^ xor;
                MapCells = new CellInfo[Width, Height];

                offSet = 54;

                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        MapCells[x, y] = new CellInfo
                            {
                                BackIndex = 0,
                                BackImage = (int)(BitConverter.ToInt32(Bytes, offSet) ^ 0xAA38AA38),
                                MiddleIndex = 1,
                                MiddleImage = (short)(BitConverter.ToInt16(Bytes, offSet += 4) ^ xor),
                                FrontImage = (short)(BitConverter.ToInt16(Bytes, offSet += 2) ^ xor),
                                DoorIndex = (byte)(Bytes[offSet += 2] & 0x7F),
                                DoorOffset = Bytes[++offSet],
                                FrontAnimationFrame = Bytes[++offSet],
                                FrontAnimationTick = Bytes[++offSet],
                                FrontIndex = (short)(Bytes[++offSet] + 2),
                                Light = Bytes[++offSet],
                                Unknown = Bytes[++offSet],
                            };
                        offSet++;

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
                //if (Settings.LogErrors) CMain.SaveError(ex.ToString());
            }
        }

        private void LoadMapType2()
        {
            try
            {
                int offset = 0;
                Width = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                Height = BitConverter.ToInt16(Bytes, offset);
                MapCells = new CellInfo[Width, Height];
                offset = 52;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {//14
                        MapCells[x, y] = new CellInfo();
                        MapCells[x, y].BackImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].MiddleImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].FrontImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].DoorIndex = (byte)(Bytes[offset++] & 0x7F);
                        MapCells[x, y].DoorOffset = Bytes[offset++];
                        MapCells[x, y].FrontAnimationFrame = Bytes[offset++];
                        MapCells[x, y].FrontAnimationTick = Bytes[offset++];
                        MapCells[x, y].FrontIndex = (short)(Bytes[offset++] + 120);
                        MapCells[x, y].Light = Bytes[offset++];
                        MapCells[x, y].BackIndex = (short)(Bytes[offset++] + 100);
                        MapCells[x, y].MiddleIndex = (short)(Bytes[offset++] + 110);
                        if ((MapCells[x, y].BackImage & 0x8000) != 0)
                            MapCells[x, y].BackImage = (MapCells[x, y].BackImage & 0x7FFF) | 0x20000000;

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }

        }

        private void LoadMapType3()
        {
            try
            {
                int offset = 0;
                Width = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                Height = BitConverter.ToInt16(Bytes, offset);
                MapCells = new CellInfo[Width, Height];
                offset = 52;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {//36
                        MapCells[x, y] = new CellInfo();
                        MapCells[x, y].BackImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].MiddleImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].FrontImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].DoorIndex = (byte)(Bytes[offset++] & 0x7F);
                        MapCells[x, y].DoorOffset = Bytes[offset++];
                        MapCells[x, y].FrontAnimationFrame = Bytes[offset++];
                        MapCells[x, y].FrontAnimationTick = Bytes[offset++];
                        MapCells[x, y].FrontIndex = (short)(Bytes[offset++] + 120);
                        MapCells[x, y].Light = Bytes[offset++];
                        MapCells[x, y].BackIndex = (short)(Bytes[offset++] + 100);
                        MapCells[x, y].MiddleIndex = (short)(Bytes[offset++] + 110);
                        MapCells[x, y].TileAnimationImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 7;//2bytes from tileanimframe, 2 bytes always blank?, 2bytes potentialy 'backtiles index', 1byte fileindex for the backtiles?
                        MapCells[x, y].TileAnimationFrames = Bytes[offset++];
                        MapCells[x, y].TileAnimationOffset = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 14; //tons of light, blending, .. related options i hope
                        if ((MapCells[x, y].BackImage & 0x8000) != 0)
                            MapCells[x, y].BackImage = (MapCells[x, y].BackImage & 0x7FFF) | 0x20000000;

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                    }

            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }
        }

        private void LoadMapType4()
        {
            try
            {
                int offset = 31;
                int w = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                int xor = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                int h = BitConverter.ToInt16(Bytes, offset);
                Width = w ^ xor;
                Height = h ^ xor;
                MapCells = new CellInfo[Width, Height];
                offset = 64;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {//12
                        MapCells[x, y] = new CellInfo();
                        MapCells[x, y].BackIndex = 0;
                        MapCells[x, y].MiddleIndex = 1;
                        MapCells[x, y].BackImage = (short)(BitConverter.ToInt16(Bytes, offset) ^ xor);
                        offset += 2;
                        MapCells[x, y].MiddleImage = (short)(BitConverter.ToInt16(Bytes, offset) ^ xor);
                        offset += 2;
                        MapCells[x, y].FrontImage = (short)(BitConverter.ToInt16(Bytes, offset) ^xor);
                        offset += 2;
                        MapCells[x, y].DoorIndex = (byte)(Bytes[offset++] & 0x7F);
                        MapCells[x, y].DoorOffset = Bytes[offset++];
                        MapCells[x, y].FrontAnimationFrame = Bytes[offset++];
                        MapCells[x, y].FrontAnimationTick = Bytes[offset++];
                        MapCells[x, y].FrontIndex = (short)(Bytes[offset++] + 2);
                        MapCells[x, y].Light = Bytes[offset++];
                        if ((MapCells[x, y].BackImage & 0x8000) != 0)
                            MapCells[x, y].BackImage = (MapCells[x, y].BackImage & 0x7FFF) | 0x20000000;

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }
        }

        private void LoadMapType5()
        {
            try
            {
                byte flag = 0;
                int offset = 20;
                short Attribute = (short)(BitConverter.ToInt16(Bytes,offset));
                Width = (int)(BitConverter.ToInt16(Bytes,offset+=2));
                Height = (int)(BitConverter.ToInt16(Bytes, offset += 2));
                //ignoring eventfile and fogcolor for now (seems unused in maps i checked)
                offset = 28;
                //initiate all cells
                MapCells = new CellInfo[Width, Height];
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        MapCells[x, y] = new CellInfo();
                //read all back tiles
                for (int x = 0; x < (Width/2); x++)
                    for (int y = 0; y < (Height/2); y++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            MapCells[(x * 2) + (i % 2), (y * 2) + (i / 2)].BackIndex = (short)(Bytes[offset] != 255? Bytes[offset]+200 : -1);
                            MapCells[(x*2) + (i % 2), (y*2) + (i / 2)].BackImage = (int)(BitConverter.ToUInt16(Bytes, offset + 1)+1);
                        }
                        offset += 3;
                    }
                //read rest of data
                offset = 28 + (3 * ((Width /2) + (Width %2)) * (Height / 2));
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        flag = Bytes[offset++];
                        MapCells[x, y].MiddleAnimationFrame = Bytes[offset++];

                        MapCells[x, y].FrontAnimationFrame = Bytes[offset] == 255? (byte)0 : Bytes[offset];
                        MapCells[x, y].FrontAnimationFrame &= 0x8F;
                        offset++;
                        MapCells[x, y].MiddleAnimationTick = 0;
                        MapCells[x, y].FrontAnimationTick = 0;
                        MapCells[x,y].FrontIndex = (short)(Bytes[offset] != 255 ? Bytes[offset] + 200 : -1);
                        offset++;
                        MapCells[x,y].MiddleIndex = (short)(Bytes[offset] != 255 ? Bytes[offset] + 200 : -1);
                        offset++;
                        MapCells[x,y].MiddleImage = (ushort)(BitConverter.ToUInt16(Bytes,offset)+1);
                        offset += 2;
                        MapCells[x, y].FrontImage = (ushort)(BitConverter.ToUInt16(Bytes, offset)+1);
                        if ((MapCells[x, y].FrontImage == 1) && (MapCells[x, y].FrontIndex == 200))
                            MapCells[x, y].FrontIndex = -1;
                        offset += 2;
                        offset += 3;//mir3 maps dont have doors so dont bother reading the info
                        MapCells[x, y].Light = (byte)(Bytes[offset] & 0x0F);
                        offset += 2;
                        if ((flag & 0x01) != 1) MapCells[x, y].BackImage |= 0x20000000;
                        if ((flag & 0x02) != 2) MapCells[x, y].FrontImage = (ushort)((UInt16)MapCells[x, y].FrontImage | 0x8000);

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                        else
                            MapCells[x, y].Light *= 2;//expand general mir3 lighting as default range is small. Might break new colour lights.
                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }
        }

        private void LoadMapType6()
        {
            try
            {
                byte flag = 0;
                int offset = 16;
                Width = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                Height = BitConverter.ToInt16(Bytes, offset);
                MapCells = new CellInfo[Width, Height];
                offset = 40;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        MapCells[x, y] = new CellInfo();
                        flag = Bytes[offset++];
                        MapCells[x,y].BackIndex = (short)(Bytes[offset] != 255 ? Bytes[offset]+ 300 : -1);
                        offset++;
                        MapCells[x, y].MiddleIndex = (short)(Bytes[offset] != 255 ? Bytes[offset] + 300 : -1);
                        offset++;
                        MapCells[x, y].FrontIndex = (short)(Bytes[offset] != 255 ? Bytes[offset] + 300 : -1);
                        offset++;
                        MapCells[x, y].BackImage = (short)(BitConverter.ToInt16(Bytes, offset) + 1);
                        offset += 2;
                        MapCells[x, y].MiddleImage = (short)(BitConverter.ToInt16(Bytes, offset) + 1);
                        offset += 2;
                        MapCells[x, y].FrontImage = (short)(BitConverter.ToInt16(Bytes, offset) + 1);
                        offset += 2;
                        if ((MapCells[x, y].FrontImage == 1) && (MapCells[x, y].FrontIndex == 200))
                            MapCells[x, y].FrontIndex = -1;
                        MapCells[x, y].MiddleAnimationFrame = Bytes[offset++];
                        MapCells[x, y].FrontAnimationFrame = Bytes[offset] == 255 ? (byte)0 : Bytes[offset];
                        if (MapCells[x, y].FrontAnimationFrame > 0x0F)//assuming shanda used same value not sure
                            MapCells[x, y].FrontAnimationFrame = (byte)(/*0x80 ^*/ (MapCells[x, y].FrontAnimationFrame & 0x0F));
                        offset++;
                        MapCells[x, y].MiddleAnimationTick = 1;
                        MapCells[x, y].FrontAnimationTick = 1;
                        MapCells[x, y].Light = (byte)(Bytes[offset] & 0x0F);
                        MapCells[x, y].Light *= 4;//far wants all light on mir3 maps to be maxed :p
                        offset += 8;
                        if ((flag & 0x01) != 1) MapCells[x, y].BackImage |= 0x20000000;
                        if ((flag & 0x02) != 2) MapCells[x, y].FrontImage = (short)((UInt16)MapCells[x, y].FrontImage | 0x8000);

                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }

        }

        private void LoadMapType7()
        {
            try
            {
                int offset = 21;
                Width = BitConverter.ToInt16(Bytes, offset);
                offset += 4;
                Height = BitConverter.ToInt16(Bytes, offset);
                MapCells = new CellInfo[Width, Height];

                offset = 54;

                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {//total 15
                        MapCells[x, y] = new CellInfo
                        {
                            BackIndex = 0,
                            BackImage = (int)BitConverter.ToInt32(Bytes, offset),
                            MiddleIndex = 1,
                            MiddleImage = (short)BitConverter.ToInt16(Bytes, offset += 4),
                            FrontImage = (short)BitConverter.ToInt16(Bytes, offset += 2),
                            DoorIndex = (byte)(Bytes[offset += 2] & 0x7F),
                            DoorOffset = Bytes[++offset],
                            FrontAnimationFrame = Bytes[++offset],
                            FrontAnimationTick = Bytes[++offset],
                            FrontIndex = (short)(Bytes[++offset] + 2),
                            Light = Bytes[++offset],
                            Unknown = Bytes[++offset],
                        };
                        if ((MapCells[x, y].BackImage & 0x8000) != 0)
                            MapCells[x, y].BackImage = (MapCells[x, y].BackImage & 0x7FFF) | 0x20000000;
                        offset++;

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }
        }

        private void LoadMapType100()
        {
            try 
            { 
                int offset = 4;
                if ((Bytes[0]!= 1) || (Bytes[1] != 0)) return;//only support version 1 atm
                Width = BitConverter.ToInt16(Bytes, offset);
                offset += 2;
                Height = BitConverter.ToInt16(Bytes, offset);
                MapCells = new CellInfo[Width, Height];
                offset = 8;
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        MapCells[x, y] = new CellInfo();
                        MapCells[x, y].BackIndex = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].BackImage = (int)BitConverter.ToInt32(Bytes, offset);
                        offset += 4;
                        MapCells[x, y].MiddleIndex = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].MiddleImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].FrontIndex = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].FrontImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].DoorIndex = (byte)(Bytes[offset++] & 0x7F);
                        MapCells[x, y].DoorOffset = Bytes[offset++];
                        MapCells[x, y].FrontAnimationFrame = Bytes[offset++];
                        MapCells[x, y].FrontAnimationTick = Bytes[offset++];
                        MapCells[x, y].MiddleAnimationFrame = Bytes[offset++];
                        MapCells[x, y].MiddleAnimationTick = Bytes[offset++];
                        MapCells[x, y].TileAnimationImage = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].TileAnimationOffset = (short)BitConverter.ToInt16(Bytes, offset);
                        offset += 2;
                        MapCells[x, y].TileAnimationFrames = Bytes[offset++];
                        MapCells[x, y].Light = Bytes[offset++];

                        if (MapCells[x, y].Light >= 100 && MapCells[x, y].Light <= 119)
                            MapCells[x, y].FishingCell = true;
                    }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }
        }

    }

}

// Github: https://github.com/Suprcode/mir2
//old code form mir2 Server.Library
//namespace Server.MirEnvir
//file local: mir2-master\Server\MirEnvir\Map.cs
namespace Mir2Svr.Map
{
    public class SvrMapReader 
    {
        public int Width, Height;
        public Cell[,] Cells;

        //data no used
        public Door[,] DoorIndex;
        public List<Door> Doors = new List<Door>();
        public List<Point> WalkableCells;

        public SvrMapReader(string iMapPath)
        {
            Load(iMapPath);
        }
        private byte FindType(byte[] input)
        {
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

        private void LoadMapCellsv0(byte[] fileBytes)
        {
            int offSet = 0;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 52;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 12
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //Can't Fire Over.

                    offSet += 2;

                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //No Floor Tile.

                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };

                    offSet += 4;

                    if (fileBytes[offSet] > 0)
                        DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));

                    offSet += 3;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);
                }
        }

        private void LoadMapCellsv1(byte[] fileBytes)
        {
            int offSet = 21;

            int w = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int xor = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int h = BitConverter.ToInt16(fileBytes, offSet);
            Width = w ^ xor;
            Height = h ^ xor;
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 54;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (((BitConverter.ToInt32(fileBytes, offSet) ^ 0xAA38AA38) & 0x20000000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.

                    offSet += 6;
                    if (((BitConverter.ToInt16(fileBytes, offSet) ^ xor) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //No Floor Tile.

                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                        DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                    offSet += 5;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);

                    offSet += 1;
                }
        }

        private void LoadMapCellsv2(byte[] fileBytes)
        {
            int offSet = 0;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 52;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 14
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //Can't Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //No Floor Tile.

                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };

                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                        DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                    offSet += 5;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);

                    offSet += 2;
                }
        }

        private void LoadMapCellsv3(byte[] fileBytes)
        {
            int offSet = 0;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 52;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 36
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //Can't Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //No Floor Tile.

                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                        DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                    offSet += 12;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);

                    offSet += 17;
                }
        }

        private void LoadMapCellsv4(byte[] fileBytes)
        {
            int offSet = 31;
            int w = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int xor = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int h = BitConverter.ToInt16(fileBytes, offSet);
            Width = w ^ xor;
            Height = h ^ xor;
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 64;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 12
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //Can't Fire Over.

                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 4;
                    if (fileBytes[offSet] > 0)
                        DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                    offSet += 6;
                }
        }

        private void LoadMapCellsv5(byte[] fileBytes)
        {
            int offSet = 22;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 28 + (3 * ((Width / 2) + (Width % 2)) * (Height / 2));
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 14
                    if ((fileBytes[offSet] & 0x01) != 1)
                        Cells[x, y] = Cell.HighWall;
                    else if ((fileBytes[offSet] & 0x02) != 2)
                        Cells[x, y] = Cell.LowWall;
                    else
                        Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 13;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);
                }
        }

        private void LoadMapCellsv6(byte[] fileBytes)
        {
            int offSet = 16;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 40;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 20
                    if ((fileBytes[offSet] & 0x01) != 1)
                        Cells[x, y] = Cell.HighWall;
                    else if ((fileBytes[offSet] & 0x02) != 2)
                        Cells[x, y] = Cell.LowWall;
                    else
                        Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 20;
                }
        }

        private void LoadMapCellsv7(byte[] fileBytes)
        {
            int offSet = 21;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 4;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offSet = 54;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 15
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.
                    offSet += 6;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //Can't Fire Over.
                    //offSet += 2;
                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                        DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                    offSet += 4;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);

                    offSet += 2;
                }
        }

        private void LoadMapCellsV100(byte[] Bytes)
        {
            int offset = 4;
            if ((Bytes[0] != 1) || (Bytes[1] != 0)) return;//only support version 1 atm
            Width = BitConverter.ToInt16(Bytes, offset);
            offset += 2;
            Height = BitConverter.ToInt16(Bytes, offset);
            Cells = new Cell[Width, Height];
            DoorIndex = new Door[Width, Height];

            offset = 8;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    offset += 2;
                    if ((BitConverter.ToInt32(Bytes, offset) & 0x20000000) != 0)
                        Cells[x, y] = Cell.HighWall; //Can Fire Over.
                    offset += 10;
                    if ((BitConverter.ToInt16(Bytes, offset) & 0x8000) != 0)
                        Cells[x, y] = Cell.LowWall; //Can't Fire Over.

                    if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offset += 2;
                    if (Bytes[offset] > 0)
                        DoorIndex[x, y] = AddDoor(Bytes[offset], new Point(x, y));
                    offset += 11;

                    byte light = Bytes[offset++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y].FishingAttribute = (sbyte)(light - 100);
                }

        }

        public bool Load(string mapPath)
        {
            try
            {
                string fileName = mapPath;
                if (File.Exists(fileName))
                {
                    byte[] fileBytes = File.ReadAllBytes(fileName);
                    switch (FindType(fileBytes))
                    {
                        case 0:
                            LoadMapCellsv0(fileBytes);
                            break;
                        case 1:
                            LoadMapCellsv1(fileBytes);
                            break;
                        case 2:
                            LoadMapCellsv2(fileBytes);
                            break;
                        case 3:
                            LoadMapCellsv3(fileBytes);
                            break;
                        case 4:
                            LoadMapCellsv4(fileBytes);
                            break;
                        case 5:
                            LoadMapCellsv5(fileBytes);
                            break;
                        case 6:
                            LoadMapCellsv6(fileBytes);
                            break;
                        case 7:
                            LoadMapCellsv7(fileBytes);
                            break;
                        case 100:
                            LoadMapCellsV100(fileBytes);
                            break;
                    }

                    //GetWalkableCells();
                    //for (int i = 0; i < Info.Respawns.Count; i++)
                    //{
                    //    MapRespawn info = new MapRespawn(Info.Respawns[i]);
                    //    if (info.Monster == null) continue;
                    //    info.Map = this;
                    //    info.WalkableCells = WalkableCells.Where(x =>
                    //    x.X <= info.Info.Location.X + info.Info.Spread &&
                    //    x.X >= info.Info.Location.X - info.Info.Spread &&
                    //    x.Y <= info.Info.Location.Y + info.Info.Spread &&
                    //    x.Y >= info.Info.Location.Y - info.Info.Spread).ToList();
                    //
                    //    Respawns.Add(info);
                    //    if ((info.Info.SaveRespawnTime) && (info.Info.RespawnTicks != 0))
                    //        Envir.SavedSpawns.Add(info);
                    //}
                    //for (int i = 0; i < Info.NPCs.Count; i++)
                    //{
                    //    NPCInfo info = Info.NPCs[i];
                    //    if (!ValidPoint(info.Location)) continue;
                    //    AddObject(new NPCObject(info) { CurrentMap = this });
                    //}
                    //for (int i = 0; i < Info.SafeZones.Count; i++)
                    //    CreateSafeZone(Info.SafeZones[i]);
                    //CreateMine();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Utils.LogErr(ex.ToString());
            }

            Utils.LogErr("Failed to Load Map: " + mapPath);
            return false;
        }
        public Door AddDoor(byte DoorIndex, Point location)
        {
            DoorIndex = (byte)(DoorIndex & 0x7F);
            for (int i = 0; i < Doors.Count; i++)
                if (Doors[i].index == DoorIndex)
                    return Doors[i];
            Door DoorInfo = new Door() { index = DoorIndex, Location = location };
            Doors.Add(DoorInfo);
            return DoorInfo;
        }
        public void GetWalkableCells()
        {
            if (WalkableCells == null)
            {
                WalkableCells = new List<Point>();

                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        if (Cells[x, y].Attribute == CellAttribute.Walk)
                            WalkableCells.Add(new Point(x, y));
            }
        }
    }

    public class Cell
    {
        public static Cell LowWall { get { return new Cell { Attribute = CellAttribute.LowWall }; } }
        public static Cell HighWall { get { return new Cell { Attribute = CellAttribute.HighWall }; } }

        public bool Valid
        {
            get { return Attribute == CellAttribute.Walk; }
        }

        public CellAttribute Attribute;
        public sbyte FishingAttribute = -1;
    }
    public class Door
    {
        public byte index;
        public DoorState DoorState;
        public byte ImageIndex;
        public long LastTick;
        public Point Location;
    }
    public enum CellAttribute : byte
    {
        Walk = 0,
        HighWall = 1,
        LowWall = 2,
    }
    public enum DoorState : byte
    {
        Closed = 0,
        Opening = 1,
        Open = 2,
        Closing = 3
    }

}
