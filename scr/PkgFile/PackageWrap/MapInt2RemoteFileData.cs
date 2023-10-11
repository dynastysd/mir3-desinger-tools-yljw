//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.1.0
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------


public class MapInt2RemoteFileData : global::System.IDisposable 
    , global::System.Collections.Generic.IDictionary<int, RemoteFileData>
 {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal MapInt2RemoteFileData(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(MapInt2RemoteFileData obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  internal static global::System.Runtime.InteropServices.HandleRef swigRelease(MapInt2RemoteFileData obj) {
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

  ~MapInt2RemoteFileData() {
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
          PackageWrapPINVOKE.delete_MapInt2RemoteFileData(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }


  public RemoteFileData this[int key] {
    get {
      return getitem(key);
    }

    set {
      setitem(key, value);
    }
  }

  public bool TryGetValue(int key, out RemoteFileData value) {
    if (this.ContainsKey(key)) {
      value = this[key];
      return true;
    }
    value = default(RemoteFileData);
    return false;
  }

  public int Count {
    get {
      return (int)size();
    }
  }

  public bool IsReadOnly {
    get {
      return false;
    }
  }

  public global::System.Collections.Generic.ICollection<int> Keys {
    get {
      global::System.Collections.Generic.ICollection<int> keys = new global::System.Collections.Generic.List<int>();
      int size = this.Count;
      if (size > 0) {
        global::System.IntPtr iter = create_iterator_begin();
        for (int i = 0; i < size; i++) {
          keys.Add(get_next_key(iter));
        }
        destroy_iterator(iter);
      }
      return keys;
    }
  }

  public global::System.Collections.Generic.ICollection<RemoteFileData> Values {
    get {
      global::System.Collections.Generic.ICollection<RemoteFileData> vals = new global::System.Collections.Generic.List<RemoteFileData>();
      foreach (global::System.Collections.Generic.KeyValuePair<int, RemoteFileData> pair in this) {
        vals.Add(pair.Value);
      }
      return vals;
    }
  }

  public void Add(global::System.Collections.Generic.KeyValuePair<int, RemoteFileData> item) {
    Add(item.Key, item.Value);
  }

  public bool Remove(global::System.Collections.Generic.KeyValuePair<int, RemoteFileData> item) {
    if (Contains(item)) {
      return Remove(item.Key);
    } else {
      return false;
    }
  }

  public bool Contains(global::System.Collections.Generic.KeyValuePair<int, RemoteFileData> item) {
    if (this[item.Key] == item.Value) {
      return true;
    } else {
      return false;
    }
  }

  public void CopyTo(global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>[] array) {
    CopyTo(array, 0);
  }

  public void CopyTo(global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>[] array, int arrayIndex) {
    if (array == null)
      throw new global::System.ArgumentNullException("array");
    if (arrayIndex < 0)
      throw new global::System.ArgumentOutOfRangeException("arrayIndex", "Value is less than zero");
    if (array.Rank > 1)
      throw new global::System.ArgumentException("Multi dimensional array.", "array");
    if (arrayIndex+this.Count > array.Length)
      throw new global::System.ArgumentException("Number of elements to copy is too large.");

    global::System.Collections.Generic.IList<int> keyList = new global::System.Collections.Generic.List<int>(this.Keys);
    for (int i = 0; i < keyList.Count; i++) {
      int currentKey = keyList[i];
      array.SetValue(new global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>(currentKey, this[currentKey]), arrayIndex+i);
    }
  }

  global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>> global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>>.GetEnumerator() {
    return new MapInt2RemoteFileDataEnumerator(this);
  }

  global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() {
    return new MapInt2RemoteFileDataEnumerator(this);
  }

  public MapInt2RemoteFileDataEnumerator GetEnumerator() {
    return new MapInt2RemoteFileDataEnumerator(this);
  }

  // Type-safe enumerator
  /// Note that the IEnumerator documentation requires an InvalidOperationException to be thrown
  /// whenever the collection is modified. This has been done for changes in the size of the
  /// collection but not when one of the elements of the collection is modified as it is a bit
  /// tricky to detect unmanaged code that modifies the collection under our feet.
  public sealed class MapInt2RemoteFileDataEnumerator : global::System.Collections.IEnumerator,
      global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>>
  {
    private MapInt2RemoteFileData collectionRef;
    private global::System.Collections.Generic.IList<int> keyCollection;
    private int currentIndex;
    private object currentObject;
    private int currentSize;

    public MapInt2RemoteFileDataEnumerator(MapInt2RemoteFileData collection) {
      collectionRef = collection;
      keyCollection = new global::System.Collections.Generic.List<int>(collection.Keys);
      currentIndex = -1;
      currentObject = null;
      currentSize = collectionRef.Count;
    }

    // Type-safe iterator Current
    public global::System.Collections.Generic.KeyValuePair<int, RemoteFileData> Current {
      get {
        if (currentIndex == -1)
          throw new global::System.InvalidOperationException("Enumeration not started.");
        if (currentIndex > currentSize - 1)
          throw new global::System.InvalidOperationException("Enumeration finished.");
        if (currentObject == null)
          throw new global::System.InvalidOperationException("Collection modified.");
        return (global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>)currentObject;
      }
    }

    // Type-unsafe IEnumerator.Current
    object global::System.Collections.IEnumerator.Current {
      get {
        return Current;
      }
    }

    public bool MoveNext() {
      int size = collectionRef.Count;
      bool moveOkay = (currentIndex+1 < size) && (size == currentSize);
      if (moveOkay) {
        currentIndex++;
        int currentKey = keyCollection[currentIndex];
        currentObject = new global::System.Collections.Generic.KeyValuePair<int, RemoteFileData>(currentKey, collectionRef[currentKey]);
      } else {
        currentObject = null;
      }
      return moveOkay;
    }

    public void Reset() {
      currentIndex = -1;
      currentObject = null;
      if (collectionRef.Count != currentSize) {
        throw new global::System.InvalidOperationException("Collection modified.");
      }
    }

    public void Dispose() {
      currentIndex = -1;
      currentObject = null;
    }
  }


  public MapInt2RemoteFileData() : this(PackageWrapPINVOKE.new_MapInt2RemoteFileData__SWIG_0(), true) {
  }

  public MapInt2RemoteFileData(MapInt2RemoteFileData other) : this(PackageWrapPINVOKE.new_MapInt2RemoteFileData__SWIG_1(MapInt2RemoteFileData.getCPtr(other)), true) {
    if (PackageWrapPINVOKE.SWIGPendingException.Pending) throw PackageWrapPINVOKE.SWIGPendingException.Retrieve();
  }

  private uint size() {
    uint ret = PackageWrapPINVOKE.MapInt2RemoteFileData_size(swigCPtr);
    return ret;
  }

  public bool empty() {
    bool ret = PackageWrapPINVOKE.MapInt2RemoteFileData_empty(swigCPtr);
    return ret;
  }

  public void Clear() {
    PackageWrapPINVOKE.MapInt2RemoteFileData_Clear(swigCPtr);
  }

  private RemoteFileData getitem(int key) {
    RemoteFileData ret = new RemoteFileData(PackageWrapPINVOKE.MapInt2RemoteFileData_getitem(swigCPtr, key), false);
    if (PackageWrapPINVOKE.SWIGPendingException.Pending) throw PackageWrapPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private void setitem(int key, RemoteFileData x) {
    PackageWrapPINVOKE.MapInt2RemoteFileData_setitem(swigCPtr, key, RemoteFileData.getCPtr(x));
    if (PackageWrapPINVOKE.SWIGPendingException.Pending) throw PackageWrapPINVOKE.SWIGPendingException.Retrieve();
  }

  public bool ContainsKey(int key) {
    bool ret = PackageWrapPINVOKE.MapInt2RemoteFileData_ContainsKey(swigCPtr, key);
    return ret;
  }

  public void Add(int key, RemoteFileData value) {
    PackageWrapPINVOKE.MapInt2RemoteFileData_Add(swigCPtr, key, RemoteFileData.getCPtr(value));
    if (PackageWrapPINVOKE.SWIGPendingException.Pending) throw PackageWrapPINVOKE.SWIGPendingException.Retrieve();
  }

  public bool Remove(int key) {
    bool ret = PackageWrapPINVOKE.MapInt2RemoteFileData_Remove(swigCPtr, key);
    return ret;
  }

  private global::System.IntPtr create_iterator_begin() {
    global::System.IntPtr ret = PackageWrapPINVOKE.MapInt2RemoteFileData_create_iterator_begin(swigCPtr);
    return ret;
  }

  private int get_next_key(global::System.IntPtr swigiterator) {
    int ret = PackageWrapPINVOKE.MapInt2RemoteFileData_get_next_key(swigCPtr, swigiterator);
    return ret;
  }

  private void destroy_iterator(global::System.IntPtr swigiterator) {
    PackageWrapPINVOKE.MapInt2RemoteFileData_destroy_iterator(swigCPtr, swigiterator);
  }

}
