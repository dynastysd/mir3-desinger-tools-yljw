#include "PackageUtil.h"
#include "packageEx.h"
#include "RemoteFilesMD5Frame.h"

Callback* Callback::s_cb = nullptr;

void Callback::SetCallback(Callback* cb)
{
	s_cb = cb;
}

void Callback::Call(const char* msg)
{
	if (s_cb) {
		s_cb->run(msg);
	}
}

class PackageEx2 :public Package {
public:
	PackageEx2();
	~PackageEx2();

	std::string name;
	FILE *fHandle;
	FILE *fHandleAsync;
	std::mutex async_mutex;

	head_package *pkgHead;

	fpos_t theEndPos;
	U32 maxFileSize;

	bool readOnly;

	std::map<S32, U32>bufferMap;

	struct slot {
		U32 size;
		fpos_t offset;
	};

	typedef std::vector<slot> slotVec;
	slotVec slots;

	//quick: ignore current slots
	int init(const std::string &filename, ZSTDCompress *compr, bool isReadOnly = false);

	int readFileInfos();

	int close();
	int applyWritedData();

	BufferType compressBuf;//op

	ZSTDCompress *comprer;

	int getBufferWithHandler(FILE *handler, const int filename, BufferType & out, size_t &size);
	int addBufferWithHandler(FILE* handler, const int bufName, void* buffer, size_t sz, bool compress, int compressedSize = -1);
public:
	void setCompress(ZSTDCompress *c) {
		comprer = c;
	}
	ZSTDCompress *getCompress() {
		return comprer;
	}

	static PackageEx *open(const std::string &filename, ZSTDCompress *compr, bool isReadOnly = false);

	bool isReadOnly();

	slotVec::iterator getSlot(U32 sz);



	int addBuffer(const int bufName, void* buffer, size_t sz, bool compress, int compressedSize = -1);
	int addBufferAsync(const int bufName, void *buffer, size_t sz, bool compress, int compressedSize = -1);

	int removeFile(const int filename);

	int getBuffer(const int filename, BufferType &out, size_t &size);

	int getBufferForAsync(const int framename, BufferType &out, size_t &size);

	bool isFrameExist(const int framename) {
		return bufferMap.find(framename) != bufferMap.end();
	}
};

bool PackageUtil::GetKeys(Package& pkg, std::vector<int>& keys)
{
	PackageEx2* pack2 = (PackageEx2*)&pkg;
	for (const auto& one : pack2->bufferMap) {
		keys.push_back(one.first);
	}
	return true;
}

bool PackageUtil::ParseImageInfos(const BufferType& in, std::map<int, ImageFrameInfo>& info)
{
	int bsize = in.size();
	if (bsize < 12)return false;

	size_t size = 0;//info.size();
	char* p = (char*)in.data();
	memcpy(&size, p, sizeof(uint32_t));
	p += sizeof(uint32_t);
	info.clear();
	//info.reserve(size);
	for (int i = 0; i < size; ++i) {
		int key;
		ImageFrameInfo io;
		memcpy(&key, p, sizeof(key));
		p += sizeof(key);
		memcpy(&io, p, sizeof(io));
		p += sizeof(io);
		info[key] = io;
	}
	auto i = info[16];
	//printf("%d %d %d %d", i.posx, i.posy, i.owid, i.ohei);

	return true;
}

bool PackageUtil::BuildImageInfos(BufferType& out, const std::map<int, ImageFrameInfo>& info)
{
	size_t sz = 4 + info.size() * (sizeof(int) + sizeof(ImageFrameInfo));
	out.resize(sz);
	char* p = (char*)out.data();

	uint32_t size = info.size();
	memcpy(p, &size, sizeof(uint32_t));
	p += 4;
	for (auto i : info) {
		memcpy(p, &i.first, sizeof(i.first));
		p += sizeof(i.first);
		memcpy(p, &i.second, sizeof(i.second));
		p += sizeof(i.second);
	}

	return true;
}

static int RemoteFilesMD5Frame_fromBuffer(const BufferType &in, std::vector<RemoteFilesMD5Frame::hash>& _hashs, std::map<int, int>&_map2files)
{
	int32_t* imap = (int32_t*)in.data();
	const int32_t msz = *imap;
	for (int i = 0; i < msz; ++i) {
		int32_t k = *(++imap);
		int32_t v = *(++imap);
		_map2files[k] = v;
	}
	const int32_t hsz = *(++imap);
	_hashs.reserve(hsz);
	uint8_t* ihash = ((uint8_t*)++imap);
	for (int i = 0; i < hsz; ++i) {
		_hashs.emplace_back();
		auto iter = _hashs.rbegin();
		memcpy(iter->hex, ihash, sizeof(iter->hex));
		ihash += sizeof(iter->hex);
	}

	return 0;
}

static int RemoteFilesMD5Frame_toBuffer(BufferType &out, std::vector<RemoteFilesMD5Frame::hash>& _hashs, std::map<int, int>&_map2files)
{
	const int32_t msz = _map2files.size() * sizeof(int32_t) * 2;
	const int32_t hsz = _hashs.size() * sizeof(RemoteFilesMD5Frame::hash);
	out.resize(msz + hsz + 8);
	int32_t* imap = (int32_t*)out.data();
	*imap = _map2files.size();
	for (auto pr : _map2files) {
		*(++imap) = pr.first;
		*(++imap) = pr.second;
	}

	*(++imap) = _hashs.size();
	uint8_t* ihash = ((uint8_t*)++imap);
	for (auto hs : _hashs) {
		memcpy(ihash, hs.hex, sizeof(hs.hex));
		ihash += sizeof(hs.hex);
	}
	return 0;
}

bool PackageUtil::ParseRemoteInfos(const BufferType& buf, std::map<int, std::string>& infos)
{
	std::vector<RemoteFilesMD5Frame::hash> _hashs;
	std::map<int, int> _map2files;
	RemoteFilesMD5Frame_fromBuffer(buf, _hashs, _map2files);

	for (auto &one : _map2files) {
		if (one.second < 0 || one.second >= _hashs.size()) {
			continue;
		}
		infos[one.first] = _hashs[one.second].hex;
	}
	return true;
}

bool PackageUtil::BuildRemoteInfos(BufferType& buf, const std::map<int, std::string>& infos)
{
	std::vector<RemoteFilesMD5Frame::hash> _hashs;
	std::map<int, int> _map2files;

	for (auto &one : infos) {
		int index = -1;
		for (int i = 0; i < _hashs.size(); i++) {
			if (strcmp(_hashs[i].hex, one.second.c_str()) == 0) {
				index = i;
				break;
			}
		}
		if (index < 0) {
			index = _hashs.size();
			RemoteFilesMD5Frame::hash h;
			strcpy_s(h.hex, one.second.c_str());
			_hashs.push_back(h);
		}
		_map2files[one.first] = index;
	}

	RemoteFilesMD5Frame_toBuffer(buf, _hashs, _map2files);
	return true;
}

bool PackageUtil::ParseTexInfo(const BufferType& in, BufferType& out, TexInfo& info)
{
	if (in.size() < 8)
		return false;

	out.resize(in.size() - 8);
	memcpy(out.data(), in.data(), out.size());

	char *p = (char *)in.data();
	S16 *infoArr = (S16*)(p + in.size() - 8);
	info.offx = infoArr[0];
	info.offy = infoArr[1];
	info.wid = infoArr[2];
	info.hei = infoArr[3];
	return true;
}

bool PackageUtil::BuildTexInfo(const BufferType& in, BufferType& out, const TexInfo& info)
{
	out.resize(in.size() + 8);
	memcpy(out.data(), in.data(), in.size());

	char *p = (char *)out.data();
	S16 *infoArr = (S16*)(p + in.size());
	infoArr[0] = info.offx;
	infoArr[1] = info.offy;
	infoArr[2] = info.wid;
	infoArr[3] = info.hei;
	return true;
}

static uint16_t ints[] = {
	3297 * 0x10,1400 * 0x10,3483 * 0x10,506 * 0x10,
	2616 * 0x10,383 * 0x10,133 * 0x10,2522 * 0x10,
	976 * 0x10,366 * 0x10,3138 * 0x10,1980 * 0x10,
	1173 * 0x10,1296 * 0x10,338 * 0x10,873 * 0x10
};

void getBuffer(const uint8_t* inBuffer, int bufferSize) {
	uint8_t* buffer = (uint8_t*)inBuffer;
	int32_t sz = bufferSize / 2;
	for (int i = 0; i < sz; ++i) {
		uint8_t *v = (uint8_t*)&ints[buffer[i * 2] & 0x0f];
		for (int j = 0; j < 2; ++j) {
			buffer[i * 2 + j] ^= v[j];
		}
	}
}

static bool ParseRemoteBlock2(const uint8_t* inBuffer, int bufferSize, std::map<int, RemoteFileData>& infos)
{
	getBuffer(inBuffer, bufferSize);
	uint8_t* buffer = (uint8_t*)inBuffer;
	uint8_t* end = buffer + bufferSize;
	while (true) {
		int key = 0;
		memcpy(&key, buffer, sizeof(int));
		buffer += sizeof(int32_t);
		int compressedSize = 0;
		memcpy(&compressedSize, buffer, sizeof(int));
		buffer += sizeof(int32_t);
		int size = 0;
		memcpy(&size, buffer, sizeof(int));
		buffer += sizeof(int32_t);

		RemoteFileData rfd;
		rfd.data = buffer;
		rfd.size = size;
		rfd.compressedSize = compressedSize;
		infos[key] = rfd;

		buffer += compressedSize;
		if (buffer + 16 >= end) {//????15??md5?????????????????0??
			break;
		}
	}
	return true;
}

bool PackageUtil::ParseRemoteBlock(const BufferType& buf, std::map<int, RemoteFileData>& infos)
{
	return ParseRemoteBlock2((const uint8_t*)buf.data(), buf.size(), infos);
}

bool PackageUtil::BuildRemoteBlock(BufferType& buf, std::map<int, RemoteFileData>& infos)
{
	int len = 0;
	for (const auto& kv: infos) {
		len += sizeof(int32_t) * 3;
		len += kv.second.compressedSize;
	}

	buf.resize(len);
	uint8_t* buffer = (uint8_t*)buf.data();
	for (const auto& kv: infos) {
		int32_t key = kv.first;
		memcpy(buffer, &key, sizeof(int32_t));
		buffer += sizeof(int32_t);

		int32_t compressedSize = kv.second.compressedSize;
		memcpy(buffer, &compressedSize, sizeof(int32_t));
		buffer += sizeof(int32_t);

		int32_t size = kv.second.size;
		memcpy(buffer, &size, sizeof(int32_t));
		buffer += sizeof(int32_t);

		memcpy(buffer, kv.second.data, compressedSize);
		buffer += compressedSize;
	}

	if (buf.size() % 2 == 1) {
		buf.resize(buf.size() + 1);
		*(((uint8_t*)buf.data()) + buf.size() - 1) = 0xff;
	}

	getBuffer((const uint8_t*)buf.data(), buf.size());
	return true;
}

typedef size_t ssize_t;

bool isPng(const unsigned char * data, ssize_t dataLen)
{
	if (dataLen <= 8)
	{
		return false;
	}

	static const unsigned char PNG_SIGNATURE[] = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

	return memcmp(PNG_SIGNATURE, data, sizeof(PNG_SIGNATURE)) == 0;
}

typedef unsigned char etc1_byte;
typedef int etc1_bool;
typedef unsigned int etc1_uint32;

static const char etc1_kMagic[] = { 'P', 'K', 'M', ' ', '1', '0' };

static const etc1_uint32 ETC1_PKM_FORMAT_OFFSET = 6;
static const etc1_uint32 ETC1_PKM_ENCODED_WIDTH_OFFSET = 8;
static const etc1_uint32 ETC1_PKM_ENCODED_HEIGHT_OFFSET = 10;
static const etc1_uint32 ETC1_PKM_WIDTH_OFFSET = 12;
static const etc1_uint32 ETC1_PKM_HEIGHT_OFFSET = 14;

static const etc1_uint32 ETC1_RGB_NO_MIPMAPS = 0;

static etc1_uint32 readBEUint16(const etc1_byte* pIn) {
	return (pIn[0] << 8) | pIn[1];
}

etc1_bool etc1_pkm_is_valid(const etc1_byte* pHeader) {
	if (memcmp(pHeader, etc1_kMagic, sizeof(etc1_kMagic))) {
		return false;
	}
	etc1_uint32 format = readBEUint16(pHeader + ETC1_PKM_FORMAT_OFFSET);
	etc1_uint32 encodedWidth = readBEUint16(pHeader + ETC1_PKM_ENCODED_WIDTH_OFFSET);
	etc1_uint32 encodedHeight = readBEUint16(pHeader + ETC1_PKM_ENCODED_HEIGHT_OFFSET);
	etc1_uint32 width = readBEUint16(pHeader + ETC1_PKM_WIDTH_OFFSET);
	etc1_uint32 height = readBEUint16(pHeader + ETC1_PKM_HEIGHT_OFFSET);
	return format == ETC1_RGB_NO_MIPMAPS &&
		encodedWidth >= width && encodedWidth - width < 4 &&
		encodedHeight >= height && encodedHeight - height < 4;
}


bool isEtc(const unsigned char * data, ssize_t /*dataLen*/)
{
	return etc1_pkm_is_valid((etc1_byte*)data) ? true : false;
}

typedef unsigned char etc2_byte;
typedef int etc2_bool;
typedef unsigned int etc2_uint32;

static const char ect2_kMagic[] = { 'P', 'K', 'M', ' ', '2', '0' };

static const etc2_uint32 ETC2_PKM_FORMAT_OFFSET = 6;
static const etc2_uint32 ETC2_PKM_ENCODED_WIDTH_OFFSET = 8;
static const etc2_uint32 ETC2_PKM_ENCODED_HEIGHT_OFFSET = 10;
static const etc2_uint32 ETC2_PKM_WIDTH_OFFSET = 12;
static const etc2_uint32 ETC2_PKM_HEIGHT_OFFSET = 14;

#define ETC2_RGB_NO_MIPMAPS 1
#define ETC2_RGBA_NO_MIPMAPS 3
#define ETC2_RGBA1_NO_MIPMAPS 4

etc2_bool etc2_pkm_is_valid(const etc2_byte* pHeader) {
	if (memcmp(pHeader, ect2_kMagic, sizeof(ect2_kMagic))) {
		return false;
	}
	etc2_uint32 format = readBEUint16(pHeader + ETC2_PKM_FORMAT_OFFSET);
	etc2_uint32 encodedWidth = readBEUint16(pHeader + ETC2_PKM_ENCODED_WIDTH_OFFSET);
	etc2_uint32 encodedHeight = readBEUint16(pHeader + ETC2_PKM_ENCODED_HEIGHT_OFFSET);
	etc2_uint32 width = readBEUint16(pHeader + ETC2_PKM_WIDTH_OFFSET);
	etc2_uint32 height = readBEUint16(pHeader + ETC2_PKM_HEIGHT_OFFSET);
	return (format == ETC2_RGB_NO_MIPMAPS || format == ETC2_RGBA_NO_MIPMAPS || format == ETC2_RGBA1_NO_MIPMAPS) &&
		encodedWidth >= width && encodedWidth - width < 4 &&
		encodedHeight >= height && encodedHeight - height < 4;
}

bool isEtc2(const uint8_t* data, ssize_t dataLen)
{
	return !!etc2_pkm_is_valid((etc2_byte*)data);
}

bool isJpg(const unsigned char * data, ssize_t dataLen)
{
	if (dataLen <= 4)
	{
		return false;
	}

	static const unsigned char JPG_SOI[] = { 0xFF, 0xD8 };

	return memcmp(data, JPG_SOI, 2) == 0;
}

bool isTiff(const unsigned char * data, ssize_t dataLen)
{
	if (dataLen <= 4)
	{
		return false;
	}

	static const char* TIFF_II = "II";
	static const char* TIFF_MM = "MM";

	return (memcmp(data, TIFF_II, 2) == 0 && *(static_cast<const unsigned char*>(data) + 2) == 42 && *(static_cast<const unsigned char*>(data) + 3) == 0) ||
		(memcmp(data, TIFF_MM, 2) == 0 && *(static_cast<const unsigned char*>(data) + 2) == 0 && *(static_cast<const unsigned char*>(data) + 3) == 42);
}

bool isWebp(const unsigned char * data, ssize_t dataLen)
{
	if (dataLen <= 12)
	{
		return false;
	}

	static const char* WEBP_RIFF = "RIFF";
	static const char* WEBP_WEBP = "WEBP";

	return memcmp(data, WEBP_RIFF, 4) == 0
		&& memcmp(static_cast<const unsigned char*>(data) + 8, WEBP_WEBP, 4) == 0;
}

Format PackageUtil::DetectFormat(void* data_, size_t dataLen)
{
	const unsigned char * data = (const unsigned char *)data_;
	if (isPng(data, dataLen))
	{
		return Format::PNG;
	}
	else if (isJpg(data, dataLen))
	{
		return Format::JPG;
	}
	else if (isTiff(data, dataLen))
	{
		return Format::TIFF;
	}
	else if (isWebp(data, dataLen))
	{
		return Format::WEBP;
	}
	//else if (isPvr(data, dataLen))
	//{
	//	return Format::PVR;
	//}
	else if (isEtc(data, dataLen))
	{
		return Format::ETC;
	}
	else if (isEtc2(data, dataLen))
	{
		return Format::ETC2;
	}
	//else if (isS3TC(data, dataLen))
	//{
	//	return Format::S3TC;
	//}
	//else if (isATITC(data, dataLen))
	//{
	//	return Format::ATITC;
	//}
	/*else if (isASTC(data, dataLen))
	{
		return Format::ASTC;
	}*/
	else
	{
		return Format::UNKNOWN;
	}
}

