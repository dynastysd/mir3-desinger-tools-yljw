//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.1.0
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------


public class ZSTDCompress : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal ZSTDCompress(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(ZSTDCompress obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  internal static global::System.Runtime.InteropServices.HandleRef swigRelease(ZSTDCompress obj) {
    if (obj != null) {
      if (!obj.swigCMemOwn)
        throw new global::System.ApplicationException("Cannot release ownership as memory is not owned");
      global::System.Runtime.InteropServices.HandleRef ptr = obj.swigCPtr;
      obj.swigCMemOwn = false;
      obj.Dispose();
      return ptr;
    } else {
      return new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
    }
  }

  ~ZSTDCompress() {
    Dispose(false);
  }

  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          PackageWrapPINVOKE.delete_ZSTDCompress(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public ZSTDCompress() : this(PackageWrapPINVOKE.new_ZSTDCompress(), true) {
  }

  public uint compress(global::System.IntPtr in_, uint inbuffer_size, BufferType out_, int compress_level) {
    uint ret = PackageWrapPINVOKE.ZSTDCompress_compress(swigCPtr, in_, inbuffer_size, BufferType.getCPtr(out_), compress_level);
    if (PackageWrapPINVOKE.SWIGPendingException.Pending) throw PackageWrapPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public uint decompress(global::System.IntPtr in_, uint inbuffer_size, BufferType out_, uint outbuffer_size) {
    uint ret = PackageWrapPINVOKE.ZSTDCompress_decompress(swigCPtr, in_, inbuffer_size, BufferType.getCPtr(out_), outbuffer_size);
    if (PackageWrapPINVOKE.SWIGPendingException.Pending) throw PackageWrapPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public static int isError_zstd(uint code) {
    int ret = PackageWrapPINVOKE.ZSTDCompress_isError_zstd(code);
    return ret;
  }

  public static string getErrorDesc_zstd(uint code) {
    string ret = PackageWrapPINVOKE.ZSTDCompress_getErrorDesc_zstd(code);
    return ret;
  }

}
