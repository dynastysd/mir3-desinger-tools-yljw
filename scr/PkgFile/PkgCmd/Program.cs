using Sharprompt;
using System;
using System.IO;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using PListNet.Nodes;
using System.Linq;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PkgCmd {
    class CSharpCallback : Callback {
        public override void run(string msg) {
            Console.WriteLine(msg);
        }
    }

    public class Urls
    {
        public string remoteurl;
        public string localurl;
        //JsonConvert.DeserializeObject<Urls>(File.ReadAllText(Path.Join(System.AppDomain.CurrentDomain.BaseDirectory, "urls.json")));
        public Urls() { }
    }

    public class Mir2TransMir3Info
    {
        public string Mir2ImgName;
        public string Mir3ImgName;

        public Mir2TransMir3Info() { }
    }

    public class Mir2OffsetInfo
    {
        public short x;
        public short y;

        public Mir2OffsetInfo(string FileName)
        {
            if (!File.Exists(FileName))
                throw new FileNotFoundException($"[{FileName}]文件不存在");
            var offsets = File.ReadAllLines(FileName);
            x = short.Parse(offsets[0]);
            y = short.Parse(offsets[1]);
        }
    }

    class Program {

        const string PackMir3Mark = "PackMir3";
        static void LogInfo(string format, params object[] args) {
            var clrSave = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(format, args);
            Console.ForegroundColor = clrSave;
        }

        static void LogWarn(string format, params object[] args) {
            var clrSave = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(format, args);
            Console.ForegroundColor = clrSave;
        }

        static void LogErr(string format, params object[] args) {
            var clrSave = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ForegroundColor = clrSave;
        }
        static void PackMir2ResQuick(string isourcePath,string ipkgPath, string iplacementPath) 
        {
            Pack(isourcePath, ipkgPath, "plist", iplacementPath ,false,false);
        }
        static void PackMir2ResOutQuick(string isourcePath, string ipkgPath, string iplacementPath)
        {
            Pack(isourcePath, ipkgPath, "plist", iplacementPath ,true,false);
        }
        static void PackMir2ResOutMonQuick(string isourcePath, string ipkgPath, string iplacementPath)
        {
            Pack(isourcePath, ipkgPath, "plist", iplacementPath,true ,true);
        }

        static void PackCmdMir2Res()
        {
            Func<object, ValidationResult> vali = obj => {
                var str = obj as string;
                if (Directory.Exists(str) && Directory.GetParent(str) != null)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult("输入路径无效");
            };
            bool isOutsideRes = Prompt.Confirm("是否是Mir2外观资源？", true);
            bool isMonsterRes = false;
            if(isOutsideRes)
                isMonsterRes = Prompt.Confirm("是否是Mir2怪物外观？", false);

            string dirPath = Prompt.Input<string>("请输入Mir2资源文件夹路径", null, null, new[] { vali });
            string placementsPath = Prompt.Input<string>("请输入Placements文件夹路径", null, null, new[] { vali });
            vali = obj => {
                return IsPathValid(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string pkgPath = Path.ChangeExtension(dirPath.TrimEnd(new char[] { '\\', '/' }), "pkg");
            pkgPath = Prompt.Input<string>("请输入打包PKG文件路径", pkgPath, null, new[] { vali });
            string atlasFormat = "plist";

            Pack(dirPath, pkgPath, atlasFormat,placementsPath, isOutsideRes ,isMonsterRes);
        }
        static void PackQuick(string sourcePath, string pkgExPath,string iformat)
        {
            //打包文件夹路径
            string dirPath = sourcePath;
            //string pkgPath = Path.ChangeExtension(dirPath.TrimEnd(new char[] { '\\', '/' }), "pkg");
            //打包PKG文件路径
            string pkgPath = pkgExPath;
            string atlasFormat = iformat;

            Pack(dirPath, pkgPath, atlasFormat);
           
        }
        static void PackCmd()
        {
            Func<object, ValidationResult> vali = obj => {
                var str = obj as string;
                if (Directory.Exists(str) && Directory.GetParent(str) != null)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult("输入路径无效");
            };
            string dirPath = Prompt.Input<string>("请输入打包文件夹路径", null, null, new[] { vali });
            vali = obj => {
                return IsPathValid(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string pkgPath = Path.ChangeExtension(dirPath.TrimEnd(new char[] { '\\', '/' }), "pkg");
            pkgPath = Prompt.Input<string>("请输入打包PKG文件路径", pkgPath, null, new[] { vali });
            string atlasFormat = Prompt.Select("请选择图集格式", new string[] { "plist", "json" }, null, "plist");

            Pack(dirPath, pkgPath, atlasFormat);
        }
        static void Pack(string dirPath,string pkgPath,string atlasFormat,string mir2PlacementPath = PackMir3Mark,bool isOutsideRes = true ,bool isMonsterRes  = false ) {           
                         
            LogInfo($"打包文件夹: {dirPath}");
            LogInfo($"输出PKG包:  {pkgPath}");

            if (File.Exists(pkgPath)) {
                if (Prompt.Confirm("输出PKG包已存在。是否删除已存在的PKG文件？", true)) {
                    LogInfo($"删除文件 {pkgPath}");
                    File.Delete(pkgPath);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(pkgPath));

            var texInfoDict = new Dictionary<int, TexInfo>();
            {
                string path = Path.Join(dirPath, "texinfo.json");
                if (File.Exists(path)) {
                    var root = JObject.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root) {
                        texInfoDict.Add(int.Parse(kv.Key), kv.Value.ToObject<TexInfo>());
                    }
                    LogInfo($"读取图片配置信息 数量={texInfoDict.Count}");
                }
            }

            var noPackSet = new HashSet<uint>();
            {
                string path = Path.Join(dirPath, "nopack.json");
                if (File.Exists(path)) {
                    var root = JArray.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root) {
                        noPackSet.Add((uint)kv);
                    }
                }
            }

            Dictionary<int, ImageFrameInfo> frameInfos = new Dictionary<int, ImageFrameInfo>();
            var pkg = new PackageLib.PackageFile(pkgPath, false);
            foreach (var f in Directory.GetFiles(dirPath)) {
                if (Path.GetExtension(f) == ".plist" ||
                    Path.GetExtension(f) == ".json") {
                    continue;
                }
                string name = Path.GetFileNameWithoutExtension(f);
                try {
                    var key = ushort.Parse(name);
                    if (noPackSet.Contains(key)) {
                        LogInfo($"设置不打包的远程文件 序号={key}");
                        pkg.SetImageNull(key);
                    }
                    else {
                        var data = File.ReadAllBytes(f);

                        TexInfo info = null;
                        if (!texInfoDict.TryGetValue(key, out info)) {
                            info = new TexInfo();
                            try {
                                using (Image image = Image.Load(data)) {
                                    info.offx = 0;
                                    info.offy = 0;
                                    info.wid = (short)image.Width;
                                    info.hei = (short)image.Height;
                                }
                            }
                            catch (UnknownImageFormatException) { }
                        }

                        string infoStr = info != null ? $"offx={info.offx} offy={info.offy} wid={info.wid} hei={info.hei}" : "";
                        LogInfo($"添加文件 序号={key} 大小={data.Length} {infoStr}");
                        pkg.SetImageData(key, data, info);
                    }

                    if (atlasFormat == "plist") {
                        var plistPath = Path.Join(Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f) + ".plist");
                        if (File.Exists(plistPath)) {
                            using (var fs = new FileStream(plistPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                                fromPList(fs, key, frameInfos,mir2PlacementPath, isOutsideRes, isMonsterRes );
                            }
                        }
                    }
                }
                catch (FormatException) {
                    LogWarn($"跳过无效序号 {name}");
                }
                catch (OverflowException) {
                    LogWarn($"跳过无效序号 {name}");
                }
            }

            if (atlasFormat == "json") {
                string path = Path.Join(dirPath, "frame.json");
                if (File.Exists(path)) {
                   var root = JObject.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                   foreach (var kv in root) {
                       frameInfos.Add(int.Parse(kv.Key), kv.Value.ToObject<ImageFrameInfo>());
                   }
                   LogInfo($"添加图集信息 数量={frameInfos.Count}");
                }
            }

            if (frameInfos.Count> 0) {
                pkg.SetImageInfos(frameInfos);
            }

            {
                string path = Path.Join(dirPath, "remote.json");
                if (File.Exists(path)) {
                    Dictionary<int, string> infos = new Dictionary<int, string>();
                    var root = JObject.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root) {
                        infos.Add(int.Parse(kv.Key), kv.Value.ToString());
                    }
                    LogInfo($"添加远程包信息 数量={infos.Count}");
                    pkg.SetRemoteInfos(infos);
                }
            }

            pkg.Close();
        }

        static BufferType PackImageData(byte[] data, TexInfo info) {
            var buf = new BufferType();
            var buf2 = new BufferType();
            PackageLib.PackageFile.toBuf(buf, data);
            PackageUtil.BuildTexInfo(buf, buf2, info);
            return buf2;
        }

        static string CalcMD5(string input) {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        static string CalcMD5(byte[] data) {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] hashBytes = md5.ComputeHash(data);
                return Convert.ToHexString(hashBytes);
            }
        }
        static void cmdPackRemote(string imgPath,string md5file)
        {

            string dirPath = imgPath;
            string outDir = md5file;

            LogInfo($"打包文件夹: {dirPath}");
            LogInfo($"输出文件夹: {outDir}");

            if (Directory.Exists(outDir))
            {
                if (Directory.GetDirectories(outDir).Length > 0 || Directory.GetFiles(outDir).Length > 0)
                {

                        LogInfo($"输出文件夹已存在并且非空,删除文件夹 {outDir}");
                        Directory.Delete(outDir, true);
                        Directory.CreateDirectory(outDir);
                }
            }
            else
            {
                Directory.CreateDirectory(outDir);
            }

            var texInfoDict = new Dictionary<int, TexInfo>();
            {
                string path = Path.Join(dirPath, "texinfo.json");
                if (File.Exists(path))
                {
                    var root = JObject.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root)
                    {
                        texInfoDict.Add(int.Parse(kv.Key), kv.Value.ToObject<TexInfo>());
                    }
                    LogInfo($"读取图片配置信息 数量={texInfoDict.Count}");
                }
            }

            var noPackSet = new HashSet<uint>();
            {
                string path = Path.Join(dirPath, "nopack.json");
                if (File.Exists(path))
                {
                    var root = JArray.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root)
                    {
                        noPackSet.Add((uint)kv);
                    }
                }
            }

            var remoteInfos = new Dictionary<int, string>();
            foreach (var f in Directory.GetFiles(dirPath))
            {
                if (Path.GetExtension(f) == ".plist" ||
                    Path.GetExtension(f) == ".json")
                {
                    continue;
                }
                string name = Path.GetFileNameWithoutExtension(f);
                try
                {
                    var key = ushort.Parse(name);
                    if (!noPackSet.Contains(key))
                    {
                        continue;
                    }

                    var data = File.ReadAllBytes(f);

                    TexInfo info = null;
                    if (!texInfoDict.TryGetValue(key, out info))
                    {
                        info = new TexInfo();
                        try
                        {
                            using (Image image = Image.Load(data))
                            {
                                info.offx = 0;
                                info.offy = 0;
                                info.wid = (short)image.Width;
                                info.hei = (short)image.Height;
                            }
                        }
                        catch (UnknownImageFormatException) { }
                    }

                    var packData = BuildRemoteData(new()
                    {
                        [key] = PackImageData(data, info)
                    });
                    var md5 = CalcMD5(packData).ToLower();
                    LogInfo($"远程文件 序号={key} 大小={packData.Length} md5={md5}");
                    File.WriteAllBytes(Path.Join(outDir, md5), packData);
                    remoteInfos.Add(key, md5);
                }
                catch (FormatException)
                {
                    LogWarn($"跳过无效序号 {name}");
                }
                catch (OverflowException)
                {
                    LogWarn($"跳过无效序号 {name}");
                }
            }

            {
                LogInfo($"释放远程包信息: remote.json");
                JObject root = new JObject();
                foreach (var kv in remoteInfos)
                {
                    root.Add(kv.Key.ToString(), kv.Value);
                }
                File.WriteAllText(Path.Join(outDir, "remote.json"), root.ToString());
            }
        }
        static void PackRemote() {
            Func<object, ValidationResult> vali = obj => {
                var str = obj as string;
                if (Directory.Exists(str) && Directory.GetParent(str) != null) {
                    return ValidationResult.Success;
                }
                return new ValidationResult("输入路径无效");
            };
            string dirPath = Prompt.Input<string>("请输入打包文件夹路径", null, null, new [] { vali });
            vali = obj => {
                return IsPathValid(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string outDir = Prompt.Input<string>("请输入输出文件夹路径", null, null, new[] { vali });
            
            LogInfo($"打包文件夹: {dirPath}");
            LogInfo($"输出文件夹: {outDir}");

            if (Directory.Exists(outDir)) {
                if (Directory.GetDirectories(outDir).Length > 0 || Directory.GetFiles(outDir).Length > 0) {
                    if (Prompt.Confirm("输出文件夹已存在并且非空。是否删除输出文件夹？", true)) {
                        LogInfo($"删除文件夹 {outDir}");
                        Directory.Delete(outDir, true);
                        Directory.CreateDirectory(outDir);
                    }
                }
            } else {
                Directory.CreateDirectory(outDir);
            }

            var texInfoDict = new Dictionary<int, TexInfo>();
            {
                string path = Path.Join(dirPath, "texinfo.json");
                if (File.Exists(path)) {
                    var root = JObject.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root) {
                        texInfoDict.Add(int.Parse(kv.Key), kv.Value.ToObject<TexInfo>());
                    }
                    LogInfo($"读取图片配置信息 数量={texInfoDict.Count}");
                }
            }

            var noPackSet = new HashSet<uint>();
            {
                string path = Path.Join(dirPath, "nopack.json");
                if (File.Exists(path)) {
                    var root = JArray.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
                    foreach (var kv in root) {
                        noPackSet.Add((uint)kv);
                    }
                }
            }

            var remoteInfos = new Dictionary<int, string>();
            foreach (var f in Directory.GetFiles(dirPath)) {
                if (Path.GetExtension(f) == ".plist" ||
                    Path.GetExtension(f) == ".json") {
                    continue;
                }
                string name = Path.GetFileNameWithoutExtension(f);
                try {
                    var key = ushort.Parse(name);
                    if (!noPackSet.Contains(key)) {
                        continue;
                    }
                    
                    var data = File.ReadAllBytes(f);

                    TexInfo info = null;
                    if (!texInfoDict.TryGetValue(key, out info)) {
                        info = new TexInfo();
                        try {
                            using (Image image = Image.Load(data)) {
                                info.offx = 0;
                                info.offy = 0;
                                info.wid = (short)image.Width;
                                info.hei = (short)image.Height;
                            }
                        }
                        catch (UnknownImageFormatException) { }
                    }

                    var packData = BuildRemoteData(new () {
                        [key] = PackImageData(data, info)
                    });
                    var md5 = CalcMD5(packData).ToLower();
                    LogInfo($"远程文件 序号={key} 大小={packData.Length} md5={md5}");
                    File.WriteAllBytes(Path.Join(outDir, md5), packData);
                    remoteInfos.Add(key, md5);
                }
                catch (FormatException) {
                    LogWarn($"跳过无效序号 {name}");
                }
                catch (OverflowException) {
                    LogWarn($"跳过无效序号 {name}");
                }
            }

            {
                LogInfo($"释放远程包信息: remote.json");
                JObject root = new JObject();
                foreach (var kv in remoteInfos) {
                    root.Add(kv.Key.ToString(), kv.Value);
                }
                File.WriteAllText(Path.Join(outDir, "remote.json"), root.ToString());
            }
        }

        // static void UnPackRemote() {
        //     Func<object, ValidationResult> vali = obj => {
        //         var str = obj as string;
        //         if (File.Exists(str)) {
        //             return ValidationResult.Success;
        //         }
        //         return new ValidationResult("输入路径无效");
        //     };
        //     string remotePath = Prompt.Input<string>("请输入解包文件路径", null, null, new [] { vali });
        //     vali = obj => {
        //         var str = obj as string;
        //         if (Directory.Exists(str)) {
        //             return ValidationResult.Success;
        //         }
        //         return new ValidationResult("输入路径无效");
        //     };
        //     string outDir = Prompt.Input<string>("请输入输出目录", null, null, new [] { vali });

        //     LogInfo($"解包文件: {remotePath}");
        //     LogInfo($"输出文件夹: {outDir}");

        //     byte[] rawData = File.ReadAllBytes(remotePath);
        //     Dictionary<int, byte[]> remoteData = PackageLib.PackageFile.ParseRemoteData(rawData);
        //     foreach (var kv in remoteData) {
        //         Format format;
        //         TexInfo info;
        //         var data = PackageLib.PackageFile.ParseImageData(kv.Value, out format, out info);

        //         var key = kv.Key;
        //         if (data == null) {
        //             LogWarn($"文件读取失败 序号={key}");
        //         } else {
        //             string infoStr = info != null ? $"offx={info.offx} offy={info.offy} wid={info.wid} hei={info.hei}" : "";
        //             LogInfo($"释放文件 序号={key} 大小={data.Length} {infoStr}");
        //             string filename = $"{key}.{PackageLib.PackageFile.GetFormatExt(format)}";
        //             string path = Path.Join(outDir, filename);
        //             File.WriteAllBytes(path, data);
        //         }
        //     }
        // }
        
        

        static byte[] Download(string md5) {
            string url = $"{downloadUrl}/{md5}";
            LogInfo($"开始下载文件 {url}");
            var req = WebRequest.CreateHttp(url);
            req.Method = "GET";
            var ms = new MemoryStream();
            req.GetResponse().GetResponseStream().CopyTo(ms);
            LogInfo("下载完成");
            return ms.ToArray();
        }
        static byte[] GetLocalMd5(string md5)
        {
            string fPath = Path.Join(localpath, md5);
            FileStream fs = new FileStream(fPath, FileMode.Open, FileAccess.Read);
            try
            {
                byte[] buffur = new byte[fs.Length];
                fs.Read(buffur, 0, (int)fs.Length);
                LogInfo($"获取本地MD5文件:{md5}");
                return buffur;
            }
            catch (Exception ex)
            {
                //MessageBoxHelper.ShowPrompt(ex.Message);
                return null;
            }
            finally
            {
                if (fs != null)
                {
                    //关闭资源
                    fs.Close();
                }
            }
        }
        static void UnpackCmd() 
        {
            Func<object, ValidationResult> vali = obj => {
                return File.Exists(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string pkgPath = Prompt.Input<string>("请输入解包PKG文件路径", null, null, new[] { vali });

            vali = obj => {
                return IsPathValid(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string dirPath = Path.Join(Path.GetDirectoryName(pkgPath), Path.GetFileNameWithoutExtension(pkgPath));
            dirPath = Prompt.Input<string>("请输入解包输出路径", dirPath, null, new[] { vali });

            bool dlRemote = Prompt.Confirm("是否下载远程包数据", true);

            string atlasFormat = Prompt.Select("请选择图集格式", new string[] { "plist", "json" }, null, "plist");

            Unpack(pkgPath, dirPath, dlRemote, atlasFormat);
        }
        static void UnpackQuick(string ipkgpath,string exportPath,string iformat)
        {
            string pkgPath = ipkgpath;
            string dirPath = exportPath;
            bool dlRemote = true;
            string atlasFormat = iformat;
            Unpack(pkgPath, dirPath, dlRemote, atlasFormat);

        }
        static void Unpack(string pkgPath,string dirPath,bool dlRemote,string atlasFormat) {
        

            if (Directory.Exists(dirPath)) {
                if (Directory.GetDirectories(dirPath).Length > 0 || Directory.GetFiles(dirPath).Length > 0) {
                    if (Prompt.Confirm("输出文件夹已存在并且非空。是否删除输出文件夹？", true)) {
                        LogInfo($"删除文件夹 {dirPath}");
                        Directory.Delete(dirPath, true);
                        Directory.CreateDirectory(dirPath);
                    }
                }
            } else {
                Directory.CreateDirectory(dirPath);
            }

            LogInfo($"解包PKG文件:  {pkgPath}");
            LogInfo($"输出目录:     {dirPath}");
            
            var pkg = new PackageLib.PackageFile(pkgPath, true);

            var remoteInfos = pkg.GetRemoteInfos();
            if (remoteInfos != null) {
                LogInfo($"释放远程包信息: remote.json");
                JObject root = new JObject();
                foreach (var kv in remoteInfos) {
                    root.Add(kv.Key.ToString(), kv.Value);
                }
                File.WriteAllText(Path.Join(dirPath, "remote.json"), root.ToString());
            }

            var texInfoDict = new Dictionary<int, TexInfo>();
            var exportFileName = new Dictionary<int, string>();
            var remoteDataCache = new Dictionary<string, Dictionary<int, byte[]>>();
            var noPackList = new List<int>();

            List<string> lackMd5 = new List<string>();
            foreach (var key in pkg.GetKeys()) {
                if (key < 0) {
                    continue;
                }
                bool isRemote;
                Format format;
                TexInfo info;
                var data = pkg.GetImageData(key, out isRemote, out format, out info);
                if (isRemote) {
                    noPackList.Add(key);
                    if (!dlRemote) {
                        LogWarn($"跳过远程文件 序号={key}");
                        continue;
                    }
                    LogInfo($"远程文件 序号={key}");
                    if (remoteInfos != null && remoteInfos.ContainsKey(key)) {
                        string md5 = remoteInfos[key];
                        Dictionary<int, byte[]> remoteData = null;
                        if (remoteDataCache.ContainsKey(md5)) {
                            remoteData = remoteDataCache[md5];
                        } else {
                            var rawData = new byte[2048];
                            if (Path.Exists(Path.Join(localpath, md5)))
                            {
                                rawData = GetLocalMd5(md5);
                            }
                            else
                            {
                                rawData = Download(md5);
                                lackMd5.Add(md5);
                            }

                            remoteData = PackageLib.PackageFile.ParseRemoteData(rawData);
                            remoteDataCache[md5] = remoteData;
                        }
                        
                        if (remoteData.ContainsKey(key)) {
                            data = remoteData[key];
                            data = PackageLib.PackageFile.ParseImageData(data, out format, out info);
                        } else {
                            LogWarn($"远程文件包中找不到文件 序号={key} 包名={md5}");
                        }
                    } else {
                        LogWarn($"远程文件包信息不存在 序号={key}");
                    }
                }

                if (data == null) {
                    LogWarn($"文件读取失败 序号={key}");
                } else {
                    string infoStr = info != null ? $"offx={info.offx} offy={info.offy} wid={info.wid} hei={info.hei}" : "";
                    LogInfo($"释放文件 序号={key} 大小={data.Length} {infoStr}");
                    if (info is not null) {
                        texInfoDict.Add(key, info);
                    }
                    string filename = $"{key}.{PackageLib.PackageFile.GetFormatExt(format)}";
                    exportFileName.Add(key, filename);
                    string path = Path.Join(dirPath, filename);
                    File.WriteAllBytes(path, data);
                }
            }
            if (lackMd5.Count > 0)
            {
                Console.WriteLine("有缺少的MD5文件");
                using (var fs = new FileStream("md5Lacks.txt", FileMode.OpenOrCreate, FileAccess.Write))
                {
                    var sw = new StreamWriter(fs);
                    foreach (var s in lackMd5)
                    {
                        sw.WriteLine(s);
                    }
                    sw.Close();
                }
            }
            if (texInfoDict.Count > 0) {
                LogInfo($"释放图片配置信息: texinfo.json");
                JObject root = new JObject();
                foreach (var kv in texInfoDict) {
                    root.Add(kv.Key.ToString(), JObject.FromObject(kv.Value));
                }
                File.WriteAllText(Path.Join(dirPath, "texinfo.json"), root.ToString());
            }

            var infos = pkg.GetImageInfos();
            if (infos != null) {
                if (atlasFormat == "json") {
                    LogInfo($"释放图集信息: frame.json");
                    JObject root = new JObject();
                    foreach (var kv in infos) {
                        root.Add(kv.Key.ToString(), JObject.FromObject(kv.Value));
                    }
                    File.WriteAllText(Path.Join(dirPath, "frame.json"), root.ToString());
                }

                if (atlasFormat == "plist") {
                    foreach (var g in infos.GroupBy(n => n.Value.image)) {
                        if (exportFileName.TryGetValue(g.Key, out string filename)) {
                            string path = Path.Join(dirPath, $"{g.Key}.plist");
                            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                                toPList(fs, g);
                            }
                        }
                        else {
                            LogErr($"图集信息引用的图片不存在 序号={g.Key}");
                        }
                    }
                }
            }

            if (noPackList.Count > 0) {
                JArray root = new JArray();
                foreach (var kv in noPackList) {
                    root.Add(kv);
                }
                File.WriteAllText(Path.Join(dirPath, "nopack.json"), root.ToString());
            }

            pkg.Close();
        }

        static void toPList(Stream s, IEnumerable<KeyValuePair<int, ImageFrameInfo>> infos) {
            SaveFramesPList(s, infos.Select(n => {
                var frame = new AtlasFrame();
                frame.spriteOffsetX = n.Value.offx;
                frame.spriteOffsetY = n.Value.offy;
                frame.spriteSizeWidth = n.Value.wid;
                frame.spriteSizeHeight = n.Value.hei;
                frame.textureRectPosX = n.Value.posx;
                frame.textureRectPosY = n.Value.posy;
                frame.textureRectWidth = frame.spriteSizeWidth;
                frame.textureRectHeight = frame.spriteSizeHeight;
                frame.spriteSourceSizeWidth = n.Value.owid;
                frame.spriteSourceSizeHeight = n.Value.ohei;
                frame.textureRotated = n.Value.rotated;

                // 偏移坐标修正
                var centerX = 24 - n.Value.offx;
                var centerY = 16 - n.Value.offy;

                var w = n.Value.wid;
                var h = n.Value.hei;
                if (w % 2 == 1) w += 1;
                if (h % 2 == 1) h += 1;
                var outW = w + Math.Abs(w / 2 - centerX) * 2;
                var outH = h + Math.Abs(h / 2 - centerY) * 2;

                frame.spriteSourceSizeWidth = outW;
                frame.spriteSourceSizeHeight = outH;

                frame.spriteOffsetX = frame.textureRectWidth / 2 - centerX;
                frame.spriteOffsetY = centerY - frame.textureRectHeight / 2;

                return Tuple.Create(n.Key.ToString(), frame);
            }));
        }

        static void fromPList(Stream s, ushort image, Dictionary<int, ImageFrameInfo> infos, string Mir2OffsetPath, bool isOutsideRes, bool isMonsterRes) {

            //存放mir2图片的偏移数据
            var offsInfos = new Dictionary<string, Mir2OffsetInfo>();
            //存放映射数据，mir3与mir2图片的映射关系，mir2一般是mir3 的子集
            List<Mir2TransMir3Info> transInfos = new List<Mir2TransMir3Info> ();
            if (isMonsterRes)
            {
                transInfos = JsonConvert.DeserializeObject<List<Mir2TransMir3Info>>(File.ReadAllText("Mir2TransMir3MonInfos.json"));
            }
            else
            {
                transInfos = JsonConvert.DeserializeObject<List<Mir2TransMir3Info>>(File.ReadAllText("Mir2TransMir3Infos.json"));
            }
            
            var transTable = new Dictionary<string, string>();

            //打包mir2资源，初始化数据
            if (Mir2OffsetPath != PackMir3Mark) {
                foreach (var file in Directory.GetFiles(Mir2OffsetPath))
                {
                    offsInfos.Add(Path.GetFileNameWithoutExtension(file), new Mir2OffsetInfo(file));
                }
                foreach (var info in transInfos)
                {   
                    transTable.Add(info.Mir3ImgName, info.Mir2ImgName);
                }
            }
            
            var frames = LoadFramesPList(s);
            foreach (var frame in frames) {
                var info = new ImageFrameInfo();
                info.image = image;

                info.posx = (short)frame.Item2.textureRectPosX;
                info.posy = (short)frame.Item2.textureRectPosY;
                info.wid = (short)frame.Item2.textureRectWidth;
                info.hei = (short)frame.Item2.textureRectHeight;
                info.offx = (short)frame.Item2.spriteOffsetX;
                info.offy = (short)frame.Item2.spriteOffsetY;
                info.owid = (short)frame.Item2.spriteSourceSizeWidth;
                info.ohei = (short)frame.Item2.spriteSourceSizeHeight;
                info.rotated = frame.Item2.textureRotated;

                // 偏移坐标修正
                info.offx = (short)(24 - info.wid / 2 + info.offx);
				info.offy = (short)(16 - info.hei / 2 - info.offy);

                var key = int.Parse(Path.GetFileNameWithoutExtension(frame.Item1));

                //mi2资源不使用textpurepacker(图集不裁切)的偏移，直接使用placements文件夹里的偏移量
                if (Mir2OffsetPath != PackMir3Mark) 
                {
                    if (isOutsideRes)
                    {
                        info.offx = (short)(offsInfos[transTable[key.ToString()]].x);
                        info.offy = (short)(offsInfos[transTable[key.ToString()]].y);                       
                    }
                    else
                    {
                        var tdata = new Mir2OffsetInfo(Path.Join(Mir2OffsetPath, frame.Item1.Replace(".png", ".txt")));
                        info.offx = (short)(tdata.x);
                        info.offy = (short)(tdata.y);
                    }
                    
                }

                infos.Add(key, info);
            }
        }

        static void Etc2Png() {
            Func<object, ValidationResult> vali = obj => {
                return Directory.Exists(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string dir = Prompt.Input<string>("请输入转换的搜索目录", null, null, new[] { vali });
            bool dlRemote = Prompt.Confirm("转换后是否删除原文件", true);
        }

        static bool IsPathValid(string path) {
            System.IO.FileInfo fi = null;
            try {
                fi = new System.IO.FileInfo(path);
            }
            catch (ArgumentException) { }
            catch (System.IO.PathTooLongException) { }
            catch (NotSupportedException) { }
            return fi != null;
        }

        static string GetAtlasImagePath(string plistPath) {
            foreach (var ext in new[] { "png", "jpg", "tiff", "webp", "pkm" }) {
                string path = Path.ChangeExtension(plistPath.TrimEnd(new char[] { '\\', '/' }), ext);
                if (File.Exists(path)) {
                    return path;
                }
            }
            return null;
        }


        static IEnumerable<Tuple<string, AtlasFrame>> LoadFramesPList(string path) {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                return LoadFramesPList(fs);
            }
        }

        static void SaveFramesPList(string path, IEnumerable<Tuple<string, AtlasFrame>> infos) {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
                SaveFramesPList(fs, infos);
            }
        }

        static IEnumerable<Tuple<string, AtlasFrame>> LoadFramesPList(Stream s) {
            var ret = new Dictionary<string, AtlasFrame>();
            var root = PListNet.PList.Load(s) as DictionaryNode;
            long format = ((root["metadata"] as DictionaryNode)["format"] as IntegerNode).Value;
            if (format < 0 || format > 3) {
                throw new Exception($"不支持的plist版本 - {format}");
            }
            return (root["frames"] as DictionaryNode).Select(n => Tuple.Create(n.Key, AtlasFrame.Parse(n.Value as DictionaryNode, format)));
        }

        static void SaveFramesPList(Stream s, IEnumerable<Tuple<string, AtlasFrame>> infos) {
            var root = new DictionaryNode();
            var meta = new DictionaryNode();
            var frames = new DictionaryNode();
            root["frames"] = frames;
            root["metadata"] = meta;
            meta["format"] = new IntegerNode(3);

            foreach (var tup in infos) {
                var info = tup.Item2;
                var node = new DictionaryNode();
                node["aliases"] = new ArrayNode();
                node["spriteOffset"] = new StringNode($"{{{info.spriteOffsetX},{info.spriteOffsetY}}}");
                node["spriteSize"] = new StringNode($"{{{info.spriteSizeWidth},{info.spriteSizeHeight}}}");
                node["spriteSourceSize"] = new StringNode($"{{{info.spriteSourceSizeWidth},{info.spriteSourceSizeHeight}}}");
                node["textureRect"] = new StringNode($"{{{{{info.textureRectPosX},{info.textureRectPosY}}},{{{info.textureRectWidth},{info.textureRectHeight}}}}}");
                node["textureRotated"] = new BooleanNode(info.textureRotated);
                frames[tup.Item1.ToString()] = node;
            }

            PListNet.PList.Save(root, s, PListNet.PListFormat.Xml);
        }

        public class AtlasFrame {
            public int spriteOffsetX;
            public int spriteOffsetY;
            public int spriteSizeWidth;
            public int spriteSizeHeight;
            public int spriteSourceSizeWidth;
            public int spriteSourceSizeHeight;
            public int textureRectPosX;
            public int textureRectPosY;
            public int textureRectWidth;
            public int textureRectHeight;
            public bool textureRotated;

            public static AtlasFrame Parse(DictionaryNode node, long format) {
                var ret = new AtlasFrame();
                if (format == 0) {
                    ret.spriteOffsetX = short.Parse((node["offsetX"] as StringNode).Value);
                    ret.spriteOffsetY = short.Parse((node["offsetY"] as StringNode).Value);
                    ret.spriteSizeWidth = short.Parse((node["width"] as StringNode).Value);
                    ret.spriteSizeHeight = short.Parse((node["height"] as StringNode).Value);
                    ret.textureRectPosX = short.Parse((node["x"] as StringNode).Value);
                    ret.textureRectPosY = short.Parse((node["y"] as StringNode).Value);
                    ret.textureRectWidth = ret.spriteSizeWidth;
                    ret.textureRectHeight = ret.spriteSizeHeight;
                    ret.spriteSourceSizeWidth = short.Parse((node["originalWidth"] as StringNode).Value);
                    ret.spriteSourceSizeHeight = short.Parse((node["originalHeight"] as StringNode).Value);
                    ret.textureRotated = false;
                }
                else if (format == 1 || format == 2) {
                    var sep = new char[] { '{', '}', ',' };
                    {
                        var tokens = (node["offset"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.spriteOffsetX = short.Parse(tokens[0]);
                        ret.spriteOffsetY = short.Parse(tokens[1]);
                    }
                    {
                        var tokens = (node["frame"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.textureRectPosX = short.Parse(tokens[0]);
                        ret.textureRectPosY = short.Parse(tokens[1]);
                        ret.textureRectWidth = short.Parse(tokens[2]);
                        ret.textureRectHeight = short.Parse(tokens[3]);

                        ret.spriteSizeWidth = ret.textureRectWidth;
                        ret.spriteSizeHeight = ret.textureRectHeight;
                    }
                    {
                        var tokens = (node["sourceSize"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.spriteSourceSizeWidth = short.Parse(tokens[0]);
                        ret.spriteSourceSizeHeight = short.Parse(tokens[1]);
                    }
                    if (format == 2) {
                        ret.textureRotated = (node["rotated"] as BooleanNode).Value;
                    }
                    else {
                        ret.textureRotated = false;
                    }
                }
                else if (format == 3) {
                    var sep = new char[] { '{', '}', ',' };
                    {
                        var tokens = (node["spriteOffset"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.spriteOffsetX = short.Parse(tokens[0]);
                        ret.spriteOffsetY = short.Parse(tokens[1]);
                    }
                    {
                        var tokens = (node["spriteSize"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.spriteSizeWidth = short.Parse(tokens[0]);
                        ret.spriteSizeHeight = short.Parse(tokens[1]);
                    }
                    {
                        var tokens = (node["spriteSourceSize"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.spriteSourceSizeWidth = short.Parse(tokens[0]);
                        ret.spriteSourceSizeHeight = short.Parse(tokens[1]);
                    }
                    {
                        var tokens = (node["textureRect"] as StringNode).Value.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        ret.textureRectPosX = short.Parse(tokens[0]);
                        ret.textureRectPosY = short.Parse(tokens[1]);
                        ret.textureRectWidth = short.Parse(tokens[2]);
                        ret.textureRectHeight = short.Parse(tokens[3]);
                    }
                    ret.textureRotated = (node["textureRotated"] as BooleanNode).Value;
                }
                
                return ret;
            }
        }

        static void DoUnpackAtlas(string plistPath, string dirPath, bool offsetEx) {
            Directory.CreateDirectory(dirPath);

            var imgPath = GetAtlasImagePath(plistPath);
            if (imgPath == null) {
                LogErr("找不到plist对应的图集图片");
                return;
            }

            using (Image image = Image.Load(imgPath)) {
                foreach (var frame in LoadFramesPList(plistPath)) {
                    LogInfo($"输出图片 - {frame.Item1}");
                    var path = Path.Join(dirPath, frame.Item1);
                    var ext = Path.GetExtension(path);
                    if (string.IsNullOrWhiteSpace(ext)) {
                        path += ".png";
                    }

                    var rotated = frame.Item2.textureRotated;
                    var oriW = frame.Item2.spriteSourceSizeWidth;
                    var oriH = frame.Item2.spriteSourceSizeHeight;
                    if (oriW <= 0 || oriH <= 0) {
                        LogWarn($"跳过尺寸不正常的图片({oriW},{oriH})");
                        continue;
                    }

                    var texRect = new Rectangle(
                            frame.Item2.textureRectPosX, 
                            frame.Item2.textureRectPosY, 
                            rotated ? frame.Item2.textureRectHeight : frame.Item2.textureRectWidth,
                            rotated ? frame.Item2.textureRectWidth : frame.Item2.textureRectHeight);

                    var outW = frame.Item2.spriteSourceSizeWidth;
                    var outH = frame.Item2.spriteSourceSizeHeight;
                    var drawLoc = new Point(
                        outW / 2 - frame.Item2.textureRectWidth / 2 + frame.Item2.spriteOffsetX, 
                        outH / 2 - frame.Item2.textureRectHeight / 2 - frame.Item2.spriteOffsetY
                    );

                    if (offsetEx) {
                        var centerX = 24 - frame.Item2.spriteOffsetX;
                        var centerY = 16 - frame.Item2.spriteOffsetY;

                        var w = frame.Item2.textureRectWidth;
                        var h = frame.Item2.textureRectHeight;
                        if (w % 2 == 1) w += 1;
                        if (h % 2 == 1) h += 1;

                        outW = w + Math.Abs(w / 2 - centerX) * 2;
                        outH = h + Math.Abs(h / 2 - centerY) * 2;

                        drawLoc = new Point(
                            outW / 2 - centerX,
                            outH / 2 - centerY
                        );
                    }

                    try {
                        using (var src = image.Clone(x => x.Crop(texRect).Rotate(rotated ? RotateMode.Rotate270 : RotateMode.None)))
                        using (var img = new Image<Rgba32>(outW, outH))
                        {
                            img.Mutate(n => n.DrawImage(src, drawLoc, 1f));
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            img.Save(path);
                        }
                    } catch (Exception ex) {
                        LogErr(ex.Message);
                    }
                }
            }
        }

        static void UnpackAtlas() {
            Func<object, ValidationResult> vali = obj => {
                return File.Exists(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string plistPath = Prompt.Input<string>("请输入需要分解的图集plist文件", null, null, new[] { vali });

            vali = obj => {
                return IsPathValid(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string dirPath = Path.Join(Path.GetDirectoryName(plistPath), Path.GetFileNameWithoutExtension(plistPath));
            dirPath = Prompt.Input<string>("请输入图片输出的目录", dirPath, null, new[] { vali });

            if (Directory.Exists(dirPath)) {
                if (Directory.GetDirectories(dirPath).Length > 0 || Directory.GetFiles(dirPath).Length > 0) {
                    if (Prompt.Confirm("输出文件夹已存在并且非空。是否删除输出文件夹？", true)) {
                        LogInfo($"删除文件夹 {dirPath}");
                        Directory.Delete(dirPath, true);
                    }
                }
            }

            DoUnpackAtlas(plistPath, dirPath, false);
        }

        static void UnpackAtlasDir() {
            Func<object, ValidationResult> vali = obj => {
                return Directory.Exists(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string plistDir = Prompt.Input<string>("请输入需要分解的图集目录", null, null, new[] { vali });

            vali = obj => {
                return IsPathValid(obj as string) ? ValidationResult.Success : new ValidationResult("输入路径无效");
            };
            string dirPath = Prompt.Input<string>("请输入图片输出的目录", null, null, new[] { vali });

            if (Directory.Exists(dirPath)) {
                if (Directory.GetDirectories(dirPath).Length > 0 || Directory.GetFiles(dirPath).Length > 0) {
                    if (Prompt.Confirm("输出文件夹已存在并且非空。是否删除输出文件夹？", true)) {
                        LogInfo($"删除文件夹 {dirPath}");
                        Directory.Delete(dirPath, true);
                    }
                }
            }

            var files = Directory.GetFiles(plistDir, "*.plist");
            foreach (var file in files) {
                LogInfo(file);
                DoUnpackAtlas(file, dirPath, false);
            }
        }

        static byte[] BuildRemoteData(Dictionary<int, BufferType> dict) {
            var infos = new MapInt2RemoteFileData();
            var c = new ZSTDCompress();
            List<BufferType> bufList = new List<BufferType>();
            
            foreach (var kv in dict) {
                var rfd = new RemoteFileData();
                var buf = new BufferType();
                bufList.Add(buf);
                var ret = c.compress(kv.Value.data(), kv.Value.size(), buf, 11);
                if (ZSTDCompress.isError_zstd(ret) != 0) {
                    throw new Exception(string.Format("compress error {0}", ZSTDCompress.getErrorDesc_zstd(ret)));
                }
                rfd.data = buf.data();
                rfd.compressedSize = ret;
                rfd.size = kv.Value.size();
                infos.Add(kv.Key, rfd);
            }

            var bufOut = new BufferType();
            PackageUtil.BuildRemoteBlock(bufOut, infos);
            return PackageLib.PackageFile.fromBuf(bufOut);
        }

        static string downloadUrl = "";
        static string localpath = "";
        static string cmd = "-prompt";
        static string format = "json";
        static string inputPath = "";
        static string outputPath = "";

        //打包传2资源
        static string placementPath = "";

        static void VerifyArgs()
        {
            string[] strs = { "--help","h" };
            if (strs.Contains<string>(cmd))
            {
                cmd = "-h";
            }
            strs = new string[] { "j", "--json", "-j"};
            if (strs.Contains<string>(format))
            {
                format = "json";
            }
            strs = new string[] { "p", "--plist", "-p" };
            if (strs.Contains<string>(format))
            {
                format = "plist";
            }
        }

        static void Init(string[] iargs)
        {
            Urls urls = new Urls(); 
            urls = JsonConvert.DeserializeObject<Urls>(File.ReadAllText(Path.Join(System.AppDomain.CurrentDomain.BaseDirectory, "urls.json")));

            downloadUrl = urls.remoteurl;
            localpath = urls.localurl;

            if(iargs.Length > 0)
            {
                string[] strs = new string[] { "-up", "-updir", "-p","-pdir" };
                bool haveOpea = false;

                if (strs.Contains( iargs[0]) ){
                    cmd = iargs[0];
                    format = iargs[1];
                    inputPath = iargs[2];
                    outputPath = iargs[3];
                    haveOpea = true;
                }

                strs = new string[] { "-pr","-prdir" };
                if (strs.Contains(iargs[0]))
                {
                    cmd = iargs[0];
                    inputPath = iargs[1];
                    outputPath = iargs[2];
                    haveOpea = true;
                }

                strs = new string[] { "-pm2","-pm2-out","-pm2-out-mon" };
                if (strs.Contains(iargs[0]))
                {
                    cmd = iargs[0];
                    inputPath = iargs[1];
                    outputPath = iargs[2];
                    placementPath = iargs[3];
                    haveOpea = true;
                }

                if (!haveOpea)
                {
                    cmd = iargs[0];
                }
                              
            }

            VerifyArgs();
            //if (iargs.Length == 4)
            //{
            //    cmd = iargs[0];
            //    inputPath = iargs[1];
            //    outputPath = iargs[2];
            //    placementPath = iargs[3];
            //}
            //if (iargs.Length == 3)
            //{
            //    cmd = iargs[0]; 
            //    inputPath = iargs[1];
            //    outputPath = iargs[2];
            //}
            //if (iargs.Length == 1) 
            //{
            //    cmd = iargs[0];
            //}
        }


        static void Main(string[] args) {
            Callback.SetCallback(new CSharpCallback());

            Init(args);

            if (cmd == "-up")
            {

                Console.WriteLine($"unpack single file:[{inputPath}]");
                UnpackQuick(inputPath, outputPath, format);

            }
            else if (cmd == "-updir")
            {
                Console.WriteLine($"unpack files in directory:[{inputPath}]");
                foreach (var file in Directory.GetFiles(inputPath))
                {
                    UnpackQuick(file, Path.Join(outputPath, Path.GetFileNameWithoutExtension(file)), format);
                }
            }
            else if (cmd == "-p")
            {
                Console.WriteLine($"pack single file:[{inputPath}]");
                PackQuick(inputPath, outputPath, format);
            }
            else if (cmd == "-pdir")
            {
                Console.WriteLine($"pack files in directory:[{inputPath}]");
                foreach (var path in Directory.GetDirectories(inputPath))
                {
                    PackQuick(path, Path.Join(outputPath,Path.GetFileNameWithoutExtension(path)+".pkg"),format);
                }
            }
            else if (cmd == "-pr")
            {
                Console.WriteLine($"pack single remote file :[{inputPath}]");
                cmdPackRemote(inputPath, outputPath);
            }
            else if(cmd == "-prdir")
            {
                Console.WriteLine($"pack remote files in directory:[{inputPath}]");
                foreach (var path in Directory.GetDirectories(inputPath))
                {
                    string pkgName = Path.GetFileNameWithoutExtension(path);
                    cmdPackRemote(path, Path.Join(outputPath, pkgName));
                }
            }
            else if (cmd == "-pm2")
            {
                PackMir2ResQuick(inputPath, outputPath,placementPath );             
            }
            else if (cmd == "-pm2-out")
            {
                PackMir2ResOutQuick(inputPath, outputPath, placementPath);
            }
            else if (cmd == "-pm2-out-mon" )
            {
                PackMir2ResOutMonQuick(inputPath, outputPath, placementPath);
            }
            else if (cmd == "-h")
            {
                Console.WriteLine("解包(下载远端)：             -up [json/plist] [sourcePath] [exportPath]");
                Console.WriteLine("解包(处理目录)(下载远端)：   -updir [json/plist] [sourcePath] [exportPath]");
                Console.WriteLine("打包：                       -p [json/plist] [sourcePath] [exportPath(*.pkg)]");
                Console.WriteLine("打包(处理目录)：             -pdir [json/plist] [sourcePath] [exportPath]");
                Console.WriteLine("远端打包：                   -pr [sourcePath] [exportPath]");
                Console.WriteLine("远端打包(处理目录)：         -prdir [sourcePath] [exportPath]");
                Console.WriteLine("打包Mir2资源：               -pm2 [sourcePath] [exportPath(.pkg)] [PlacementsPath] ");
                Console.WriteLine("打包Mir2资源(外观)：         -pm2-out [sourcePath] [exportPath(.pkg)] [PlacementsPath] ");
                Console.WriteLine("打包Mir2资源(怪物外观)：     -pm2-out-mon [sourcePath] [exportPath(.pkg)] [PlacementsPath] ");
                Console.WriteLine("控制面板：              默认值");
            }
            else if(cmd == "-prompt")
            {
                while (true)
                {
                    string actPack = "打包";
                    string actUnpack = "解包";
                    string actUnpackAtlas = "分解图集";
                    string actUnpackAtlasDir = "分解图集批量处理目录";
                    string actPackRemote = "打包远程文件";
                    string actPackMir2Res = "打包传奇2资源";
                    // string actUnPackRemote = "解包远程文件";
                    string actExit = "退出";
                    var action = Prompt.Select(new SelectOptions<string>
                    {
                        Message = "选择要进行的操作",
                        Items = new[] { actPack, actUnpack, actUnpackAtlas, actUnpackAtlasDir, actPackRemote, actPackMir2Res,actExit },
                        DefaultValue = "打包"
                    });

                    if (action == actPack)
                    {
                        PackCmd();
                    }
                    else if (action == actUnpack)
                    {
                        UnpackCmd();
                    }
                    else if (action == actUnpackAtlas)
                    {
                        UnpackAtlas();
                    }
                    else if (action == actUnpackAtlasDir)
                    {
                        UnpackAtlasDir();
                    }
                    else if (action == actPackRemote)
                    {
                        PackRemote();
                    }
                    else if (action == actPackMir2Res)
                    {
                        PackCmdMir2Res();
                    }
                    // else if (action == actUnPackRemote) {
                    //     UnPackRemote();
                    // }
                    else if (action == actExit)
                    {
                        break;
                    }

                    LogInfo("");
                    LogInfo("操作完成");
                    LogInfo("");
                }
            }
            else
            {
                LogErr("error cmd");
                LogErr("input -h to get help");
            }
          
        }
    }
}
