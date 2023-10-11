#pragma once
#include "package.h"
#include "ImageFrame.h"
#include "TexFrame.h"
#include <map>

class Callback {
private:
	static Callback* s_cb;
public:
	virtual ~Callback() {}
	virtual void run(const char* msg) {}

	static void SetCallback(Callback* cb);
	static void Call(const char* msg);
};

struct RemoteFileData {
	void* data;
	unsigned int size;
	unsigned int compressedSize;
};

enum class Format
{
	//! JPEG
	JPG,
	//! PNG
	PNG,
	//! TIFF
	TIFF,
	//! WebP
	WEBP,
	//! PVR
	PVR,
	//! ETC
	ETC,
	//! ETC2
	ETC2,
	//! S3TC
	S3TC,
	//! ATITC
	ATITC,
	//! TGA
	TGA,
	//! Raw Data
	RAW_DATA,
	//! Unknown format
	UNKNOWN
};

class PackageUtil {
public:
	static bool GetKeys(Package& pkg, std::vector<int>& keys);

	static bool ParseImageInfos(const BufferType& buf, std::map<int, ImageFrameInfo>& infos);
	static bool BuildImageInfos(BufferType& buf, const std::map<int, ImageFrameInfo>& infos);
	static bool ParseRemoteInfos(const BufferType& buf, std::map<int, std::string>& infos);
	static bool BuildRemoteInfos(BufferType& buf, const std::map<int, std::string>& infos);
	static bool ParseTexInfo(const BufferType& in, BufferType& out, TexInfo& info);
	static bool BuildTexInfo(const BufferType& in, BufferType& out, const TexInfo& info);

	static bool ParseRemoteBlock(const BufferType& buf, std::map<int, RemoteFileData>& infos);
	static bool BuildRemoteBlock(BufferType& buf, std::map<int, RemoteFileData>& infos);

	static Format DetectFormat(void* data, size_t dataLen);
};