using System;
using System.Collections.Generic;
using System.Text;

namespace PackageLib {
    public class PackageFile {
        private Package pkg = null;
        private ZSTDCompress c = null;

        public PackageFile(string path, bool readOnly) {
            c = new ZSTDCompress();
            pkg = PackageWrap.PACKAGE_OPEN(path, c, readOnly);
            if (pkg == null) {
                throw new Exception(string.Format("cant open package {0}", path));
            }
        }

        private void ThrowIfClosed() {
            if (pkg == null) {
                throw new Exception("pkg closed");
            }
        }

        public void Close() {
            ThrowIfClosed();

            pkg.close();
            pkg = null;
        }

        public IList<int> GetKeys() {
            ThrowIfClosed();

            var keys = new VectorInt();
            PackageUtil.GetKeys(pkg, keys);
            return keys;
        }

        public IDictionary<int, ImageFrameInfo> GetImageInfos() {
            ThrowIfClosed();

            uint size;
            var buf = new BufferType();
            var infos = new MapInt2ImageFrameInfo();

            if (pkg.getBuffer(-1, buf, out size) == (int)PKG_ERR.SUCCESS) {
                PackageUtil.ParseImageInfos(buf, infos);
                return infos;
            }
            return null;
        }

        public void SetImageInfos(IDictionary<int, ImageFrameInfo> _infos) {
            ThrowIfClosed();

            var buf = new BufferType();
            var infos = new MapInt2ImageFrameInfo();
            foreach (var kv in _infos) {
                infos.Add(kv.Key, kv.Value);
            }
            PackageUtil.BuildImageInfos(buf, infos);
            pkg.addBuffer(-1, buf.data(), buf.size(), true);
        }

        public IDictionary<int, string> GetRemoteInfos() {
            ThrowIfClosed();

            uint size;
            var buf = new BufferType();
            var infos = new MapInt2String();

            if (pkg.getBuffer(-2, buf, out size) == (int)PKG_ERR.SUCCESS) {
                PackageUtil.ParseRemoteInfos(buf, infos);
                return infos;
            }
            return null;
        }

        public void SetRemoteInfos(IDictionary<int, string> _infos) {
            ThrowIfClosed();

            var buf = new BufferType();
            var infos = new MapInt2String();
            foreach (var kv in _infos) {
                infos.Add(kv.Key, kv.Value);
            }
            PackageUtil.BuildRemoteInfos(buf, infos);
            pkg.addBuffer(-2, buf.data(), buf.size(), true);
        }

        public bool isFileExist(int key) {
            ThrowIfClosed();

            return pkg.isFrameExist(key);
        }

        public byte[] GetFileData(int key, out bool isRemote, out Format format) {
            ThrowIfClosed();

            uint size;
            var buf = new BufferType();
            int ret = pkg.getBuffer(key, buf, out size);
            isRemote = ret == (int)PKG_ERR.IS_REMOTE_FILE;
            format = Format.UNKNOWN;

            if (ret != (int)PKG_ERR.SUCCESS) {
                return null;
            }

            format = PackageUtil.DetectFormat(buf.data(), buf.size());

            byte[] data = new byte[buf.size()];
            if (buf.size() > 0) {
                System.Runtime.InteropServices.Marshal.Copy(buf.data(), data, 0, (int)buf.size());
            }
            return data;
        }

        public byte[] GetImageData(int key, out bool isRemote, out Format format, out TexInfo info) {
            ThrowIfClosed();

            uint size;
            var buf = new BufferType();
            int ret = pkg.getBuffer(key, buf, out size);
            isRemote = ret == (int)PKG_ERR.IS_REMOTE_FILE;
            format = Format.UNKNOWN;
            info = null;

            if (ret != (int)PKG_ERR.SUCCESS) {
                return null;
            }

            return ParseImageData(buf, out format, out info);
        }

        public static byte[] ParseImageData(BufferType buf, out Format format, out TexInfo info) {
            var buf2 = new BufferType();
            info = new TexInfo();
            PackageUtil.ParseTexInfo(buf, buf2, info);
            format = PackageUtil.DetectFormat(buf2.data(), buf2.size());
            return fromBuf(buf2);
        }

        public static byte[] ParseImageData(byte[] data, out Format format, out TexInfo info) {
            var buf = new BufferType();
            toBuf(buf, data);
            return ParseImageData(buf, out format, out info);
        }

        public void SetImageData(int key, byte[] data, TexInfo info) {
            ThrowIfClosed();

            var buf = new BufferType();
            var buf2 = new BufferType();
            toBuf(buf, data);
            PackageUtil.BuildTexInfo(buf, buf2, info);
            pkg.addBuffer(key, buf2.data(), buf2.size(), true);
        }

        public void SetImageNull(int key) {
            ThrowIfClosed();

            pkg.addBuffer(key, IntPtr.Zero, 0, true);
        }

        public static Dictionary<int, byte[]> ParseRemoteData(byte[] data) {
            var dict = new Dictionary<int, byte[]>();
            var buf = new BufferType();
            toBuf(buf, data);
            var infos = new MapInt2RemoteFileData();
            var bufCC = new BufferType();
            var c = new ZSTDCompress();
            if (PackageUtil.ParseRemoteBlock(buf, infos)) {
                foreach (var kv in infos) {
                    bufCC.resize(kv.Value.size);
                    var ret = c.decompress(kv.Value.data, kv.Value.compressedSize, bufCC, kv.Value.size);
                    if (ZSTDCompress.isError_zstd(ret) != 0) {
                        //printf("decompress error :%s\n", ZSTDCompress::getErrorDesc_zstd(ret));
                        //return PKG_ERR::DECOMPRESS_FAIL;
                        throw new Exception(string.Format("decompress error {0}", ZSTDCompress.getErrorDesc_zstd(ret)));
                    }
                    dict.Add(kv.Key, fromBuf(bufCC));
                }
            }
            return dict;
        }
        
        public void AddFile(int key, byte[] data) {
            ThrowIfClosed();

            using (var buf = new BufferType()) {
                toBuf(buf, data);
                pkg.addBuffer(key, buf.data(), (uint)data.Length, true);
            }
        }

        public static byte[] fromBuf(BufferType buf) {
            byte[] data = new byte[buf.size()];
            System.Runtime.InteropServices.Marshal.Copy(buf.data(), data, 0, (int)buf.size());
            return data;
        }

        public static void toBuf(BufferType buf, byte[] data) {
            buf.resize((uint)Math.Max(data.Length, 1));
            System.Runtime.InteropServices.Marshal.Copy(data, 0, buf.data(), data.Length);
        }

        public static string GetFormatExt(Format f) {
            switch (f) {
                case Format.PNG: return "png";
                case Format.JPG: return "jpg";
                case Format.TIFF: return "tiff";
                case Format.WEBP: return "webp";
                case Format.ETC: return "pkm";
                case Format.ETC2: return "pkm";
            }
            return "unknown";
        }
    }
}
