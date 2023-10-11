%module (directors="1")PackageWrap

%{
#include "common/bufferType.h"
#include "package.h"
#include "ZSTDCompress.h"
#include "ImageFrame.h"
#include "PackageUtil.h"
%}

%feature("director") Callback;

%include "std_string.i"
%include "std_vector.i"
%include "std_map.i"
%include <typemaps.i>

%apply void *VOID_INT_PTR { void * }
%apply unsigned int &OUTPUT { size_t &size }

struct ImageFrameInfo
{
	short offx;
	short offy;
	short wid;
	short hei;
	short posx;
	short posy;
	unsigned short image;
	bool rotated;
	bool reserved;
	short owid;
	short ohei;
};

struct TexInfo{
	short offx;
	short offy;
	short wid;
	short hei;
};

class BufferType {
public:
	void* data()const;
	size_t size() const;
	~BufferType();
	BufferType();

	void resize(size_t size);
};

namespace std {
   %template(VectorInt) vector<int>;
   %template(MapInt2String) map<int, string>;
   %template(MapInt2ImageFrameInfo) map<int, ImageFrameInfo>;
   %template(MapInt2RemoteFileData) map<int, RemoteFileData>;
};

class ZSTDCompress {
public:
	ZSTDCompress();
	~ZSTDCompress();

	size_t compress(const void *in, const size_t inbuffer_size, BufferType &out, int compress_level);
    size_t decompress(const void *in, const size_t inbuffer_size, BufferType &out, size_t outbuffer_size);

	static int isError_zstd(size_t code);
	static const char *getErrorDesc_zstd(size_t code);
};

enum PKG_ERR {
	SUCCESS = 0,
	CODE_CHECKHEAD_FAIL = 1,
	FILE_NOT_FOUND_IN_PACKAGE,
	READ_HEAD_FAIL,
	PACKAGE_NOT_FOUND,
	OPEN_PACKAGE_FAIL,
	READ_PKG_INFO_FAIL,
	WRITE_FILE_FAIL,
	DECOMPRESS_FAIL,
	COMPRESS_FAIL,
	IS_REMOTE_FILE,
	INVALID_TEXTURE_DATA
};

class Package {
public:
	virtual int addBuffer(const int bufName, void* buffer, size_t sz, bool compress, int compressedSize = -1) = 0;
	virtual int getBuffer(const int filename, BufferType &out, size_t &size) = 0;
	virtual bool isFrameExist(const int frameName) = 0;
	virtual bool isReadOnly() = 0;
	virtual int close() = 0;
	virtual int applyWritedData() = 0;
};

extern "C"{
	Package *PACKAGE_OPEN(const std::string &filename, ZSTDCompress *comprer, bool readOnly = false);
}

%include "PackageUtil.h"