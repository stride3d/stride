// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "../StrideNative.h"

#if defined(WINDOWS_DESKTOP) || !defined(__clang__)

//Borrowed from: https://msdn.microsoft.com/en-us/library/windows/desktop/ee417014(v=vs.85).aspx#XInput_and_DirectInput_Side_by_Side

#include "../../../deps/NativePath/NativePath.h"

typedef struct _GUID {
	unsigned long  Data1;
	unsigned short Data2;
	unsigned short Data3;
	unsigned char  Data4[8];
} GUID;

typedef GUID IID;

typedef unsigned long       DWORD;
typedef int                 BOOL;
typedef unsigned char       BYTE;
typedef unsigned short      WORD;
typedef float               FLOAT;
typedef FLOAT               *PFLOAT;
typedef int                 INT;
typedef unsigned int        UINT;
typedef unsigned int        *PUINT;
typedef wchar_t				WCHAR;
typedef WCHAR OLECHAR;
typedef OLECHAR* BSTR;
typedef BSTR* LPBSTR;

#define REFIID const IID &
#define HRESULT long
#define UINT32 unsigned int 
#define ULONG unsigned long
#define THIS_
#define THIS void
#define PURE = 0;
#define _Outptr_
#define _In_
#define _In_opt_
#define _Out_
#define X2DEFAULT(x) =x
#define _Reserved_
#define _In_reads_bytes_(_xxx_)
#define _Out_writes_bytes_(_xxx_)
#define _Out_writes_(_xxx_)
#define _In_reads_(_xxx_)
#define STDMETHOD(method) virtual HRESULT __stdcall method
#define STDMETHOD_(type,method) virtual type __stdcall method
#define XAUDIO2_COMMIT_NOW              0             // Used as an OperationSet argument
#define XAUDIO2_COMMIT_ALL              0             // Used in IXAudio2::CommitChanges
#define XAUDIO2_INVALID_OPSET           (UINT32)(-1)  // Not allowed for OperationSet arguments
#define XAUDIO2_NO_LOOP_REGION          0             // Used in XAUDIO2_BUFFER.LoopCount
#define XAUDIO2_LOOP_INFINITE           255           // Used in XAUDIO2_BUFFER.LoopCount
#define XAUDIO2_DEFAULT_CHANNELS        0             // Used in CreateMasteringVoice
#define XAUDIO2_DEFAULT_SAMPLERATE      0             // Used in CreateMasteringVoice
#define BYTE char
#define UINT64 unsigned __int64 
#define _In_opt_z_
#define FAILED(hr) (((HRESULT)(hr)) < 0)
#define _Inout_

typedef unsigned short VARTYPE;
typedef int64_t LONGLONG;
typedef uint64_t ULONGLONG;
typedef char CHAR;
typedef short SHORT;
typedef long LONG;
typedef double DOUBLE;
typedef short VARIANT_BOOL;
typedef LONG SCODE;
typedef SCODE *PSCODE;

typedef union tagCY {
	struct {
		ULONG Lo;
		LONG      Hi;
	} DUMMYSTRUCTNAME;
	LONGLONG int64;
} CY;

typedef double DATE;

typedef void IDispatch;
typedef void IRecordInfo;

typedef unsigned short USHORT;
typedef void* PVOID;

typedef struct tagSAFEARRAYBOUND
{
	ULONG cElements;
	LONG lLbound;
} 	SAFEARRAYBOUND;

typedef struct tagSAFEARRAY
{
	USHORT cDims;
	USHORT fFeatures;
	ULONG cbElements;
	ULONG cLocks;
	PVOID pvData;
	SAFEARRAYBOUND rgsabound[1];
} 	SAFEARRAY;

typedef struct tagVARIANT VARIANT;

typedef struct tagDEC {
	USHORT wReserved;
	union {
		struct {
			BYTE scale;
			BYTE sign;
		} DUMMYSTRUCTNAME;
		USHORT signscale;
	} DUMMYUNIONNAME;
	ULONG Hi32;
	union {
		struct {
			ULONG Lo32;
			ULONG Mid32;
		} DUMMYSTRUCTNAME2;
		ULONGLONG Lo64;
	} DUMMYUNIONNAME2;
} DECIMAL;

struct IUnknown;

typedef struct tagVARIANT {
	union {
		struct __tagVARIANT {
			VARTYPE vt;
			WORD    wReserved1;
			WORD    wReserved2;
			WORD    wReserved3;
			union {
				LONGLONG            llVal;
				LONG                lVal;
				BYTE                bVal;
				SHORT               iVal;
				FLOAT               fltVal;
				DOUBLE              dblVal;
				VARIANT_BOOL        boolVal;
				SCODE               scode;
				CY                  cyVal;
				DATE                date;
				BSTR                bstrVal;
				IUnknown            *punkVal;
				IDispatch           *pdispVal;
				SAFEARRAY           *parray;
				BYTE                *pbVal;
				SHORT               *piVal;
				LONG                *plVal;
				LONGLONG            *pllVal;
				FLOAT               *pfltVal;
				DOUBLE              *pdblVal;
				VARIANT_BOOL        *pboolVal;
				SCODE               *pscode;
				CY                  *pcyVal;
				DATE                *pdate;
				BSTR                *pbstrVal;
				IUnknown            **ppunkVal;
				IDispatch           **ppdispVal;
				SAFEARRAY           **pparray;
				VARIANT             *pvarVal;
				PVOID               byref;
				CHAR                cVal;
				USHORT              uiVal;
				ULONG               ulVal;
				ULONGLONG           ullVal;
				INT                 intVal;
				UINT                uintVal;
				DECIMAL             *pdecVal;
				CHAR                *pcVal;
				USHORT              *puiVal;
				ULONG               *pulVal;
				ULONGLONG           *pullVal;
				INT                 *pintVal;
				UINT                *puintVal;
				struct __tagBRECORD {
					PVOID       pvRecord;
					IRecordInfo *pRecInfo;
				} __VARIANT_NAME_4;
			} __VARIANT_NAME_3;
		} __VARIANT_NAME_2;
		DECIMAL             decVal;
	} __VARIANT_NAME_1;
} VARIANT, *LPVARIANT, VARIANTARG, *LPVARIANTARG;

#ifndef SAFE_RELEASE
#define SAFE_RELEASE(x) \
   if(x != NULL)        \
   {                    \
      x->Release();     \
      x = NULL;         \
   }
#endif

#define SUCCEEDED(__xx__) !FAILED(__xx__)

#define RPC_C_AUTHN_WINNT        10
#define RPC_C_AUTHZ_NONE    0
#define RPC_C_AUTHN_LEVEL_CALL          3
#define RPC_C_IMP_LEVEL_IMPERSONATE  3

typedef
enum tagEOLE_AUTHENTICATION_CAPABILITIES
{
	EOAC_NONE = 0,
	EOAC_MUTUAL_AUTH = 0x1,
	EOAC_STATIC_CLOAKING = 0x20,
	EOAC_DYNAMIC_CLOAKING = 0x40,
	EOAC_ANY_AUTHORITY = 0x80,
	EOAC_MAKE_FULLSIC = 0x100,
	EOAC_DEFAULT = 0x800,
	EOAC_SECURE_REFS = 0x2,
	EOAC_ACCESS_CONTROL = 0x4,
	EOAC_APPID = 0x8,
	EOAC_DYNAMIC = 0x10,
	EOAC_REQUIRE_FULLSIC = 0x200,
	EOAC_AUTO_IMPERSONATE = 0x400,
	EOAC_NO_CUSTOM_MARSHAL = 0x2000,
	EOAC_DISABLE_AAA = 0x1000
} 	EOLE_AUTHENTICATION_CAPABILITIES;

enum VARENUM
{
	VT_EMPTY = 0,
	VT_NULL = 1,
	VT_I2 = 2,
	VT_I4 = 3,
	VT_R4 = 4,
	VT_R8 = 5,
	VT_CY = 6,
	VT_DATE = 7,
	VT_BSTR = 8,
	VT_DISPATCH = 9,
	VT_ERROR = 10,
	VT_BOOL = 11,
	VT_VARIANT = 12,
	VT_UNKNOWN = 13,
	VT_DECIMAL = 14,
	VT_I1 = 16,
	VT_UI1 = 17,
	VT_UI2 = 18,
	VT_UI4 = 19,
	VT_I8 = 20,
	VT_UI8 = 21,
	VT_INT = 22,
	VT_UINT = 23,
	VT_VOID = 24,
	VT_HRESULT = 25,
	VT_PTR = 26,
	VT_SAFEARRAY = 27,
	VT_CARRAY = 28,
	VT_USERDEFINED = 29,
	VT_LPSTR = 30,
	VT_LPWSTR = 31,
	VT_RECORD = 36,
	VT_INT_PTR = 37,
	VT_UINT_PTR = 38,
	VT_FILETIME = 64,
	VT_BLOB = 65,
	VT_STREAM = 66,
	VT_STORAGE = 67,
	VT_STREAMED_OBJECT = 68,
	VT_STORED_OBJECT = 69,
	VT_BLOB_OBJECT = 70,
	VT_CF = 71,
	VT_CLSID = 72,
	VT_VERSIONED_STREAM = 73,
	VT_BSTR_BLOB = 0xfff,
	VT_VECTOR = 0x1000,
	VT_ARRAY = 0x2000,
	VT_BYREF = 0x4000,
	VT_RESERVED = 0x8000,
	VT_ILLEGAL = 0xffff,
	VT_ILLEGALMASKED = 0xfff,
	VT_TYPEMASK = 0xfff
};
typedef ULONG PROPID;

typedef
enum tagCLSCTX
{
	CLSCTX_INPROC_SERVER = 0x1,
	CLSCTX_INPROC_HANDLER = 0x2,
	CLSCTX_LOCAL_SERVER = 0x4,
	CLSCTX_INPROC_SERVER16 = 0x8,
	CLSCTX_REMOTE_SERVER = 0x10,
	CLSCTX_INPROC_HANDLER16 = 0x20,
	CLSCTX_RESERVED1 = 0x40,
	CLSCTX_RESERVED2 = 0x80,
	CLSCTX_RESERVED3 = 0x100,
	CLSCTX_RESERVED4 = 0x200,
	CLSCTX_NO_CODE_DOWNLOAD = 0x400,
	CLSCTX_RESERVED5 = 0x800,
	CLSCTX_NO_CUSTOM_MARSHAL = 0x1000,
	CLSCTX_ENABLE_CODE_DOWNLOAD = 0x2000,
	CLSCTX_NO_FAILURE_LOG = 0x4000,
	CLSCTX_DISABLE_AAA = 0x8000,
	CLSCTX_ENABLE_AAA = 0x10000,
	CLSCTX_FROM_DEFAULT_CONTEXT = 0x20000,
	CLSCTX_ACTIVATE_32_BIT_SERVER = 0x40000,
	CLSCTX_ACTIVATE_64_BIT_SERVER = 0x80000,
	CLSCTX_ENABLE_CLOAKING = 0x100000,
	CLSCTX_APPCONTAINER = 0x400000,
	CLSCTX_ACTIVATE_AAA_AS_IU = 0x800000,
	CLSCTX_PS_DLL = (int)0x80000000
} 	CLSCTX;

typedef void *LPVOID;

typedef void IWbemContext;

struct IUnknown
{
	// NAME: IXAudio2::QueryInterface
	// DESCRIPTION: Queries for a given COM interface on the XAudio2 object.
	//              Only IID_IUnknown and IID_IXAudio2 are supported.
	//
	// ARGUMENTS:
	//  riid - IID of the interface to be obtained.
	//  ppvInterface - Returns a pointer to the requested interface.
	//
	STDMETHOD(QueryInterface) (THIS_ REFIID riid, _Outptr_ void** ppvInterface) PURE;

	// NAME: IXAudio2::AddRef
	// DESCRIPTION: Adds a reference to the XAudio2 object.
	//
	STDMETHOD_(ULONG, AddRef) (THIS) PURE;

	// NAME: IXAudio2::Release
	// DESCRIPTION: Releases a reference to the XAudio2 object.
	//
	STDMETHOD_(ULONG, Release) (THIS) PURE;
};

#define STDMETHODCALLTYPE __stdcall

#define __RPC__in
#define __RPC__in_opt
#define __RPC__deref_opt_inout_opt
#define __RPC__deref_out_opt
#define __RPC__out_ecount_part
#define __RPC__out

typedef const WCHAR *LPCWSTR, *PCWSTR;

typedef void IWbemQualifierSet;

typedef long CIMTYPE;

//MIDL_INTERFACE("dc12a681-737f-11cf-884d-00aa004b2e24")
struct IWbemClassObject : IUnknown
{
	virtual HRESULT STDMETHODCALLTYPE GetQualifierSet(
		/* [out] */ IWbemQualifierSet **ppQualSet) = 0;

	virtual HRESULT STDMETHODCALLTYPE Get(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [unique][in][out] */ VARIANT *pVal,
		/* [unique][in][out] */ CIMTYPE *pType,
		/* [unique][in][out] */ long *plFlavor) = 0;

	virtual HRESULT STDMETHODCALLTYPE Put(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [in] */ VARIANT *pVal,
		/* [in] */ CIMTYPE Type) = 0;

	virtual HRESULT STDMETHODCALLTYPE Delete(
		/* [string][in] */ LPCWSTR wszName) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetNames(
		/* [string][in] */ LPCWSTR wszQualifierName,
		/* [in] */ long lFlags,
		/* [in] */ VARIANT *pQualifierVal,
		/* [out] */ SAFEARRAY * *pNames) = 0;

	virtual HRESULT STDMETHODCALLTYPE BeginEnumeration(
		/* [in] */ long lEnumFlags) = 0;

	virtual HRESULT STDMETHODCALLTYPE Next(
		/* [in] */ long lFlags,
		/* [unique][in][out] */ BSTR *strName,
		/* [unique][in][out] */ VARIANT *pVal,
		/* [unique][in][out] */ CIMTYPE *pType,
		/* [unique][in][out] */ long *plFlavor) = 0;

	virtual HRESULT STDMETHODCALLTYPE EndEnumeration(void) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetPropertyQualifierSet(
		/* [string][in] */ LPCWSTR wszProperty,
		/* [out] */ IWbemQualifierSet **ppQualSet) = 0;

	virtual HRESULT STDMETHODCALLTYPE Clone(
		/* [out] */ IWbemClassObject **ppCopy) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetObjectText(
		/* [in] */ long lFlags,
		/* [out] */ BSTR *pstrObjectText) = 0;

	virtual HRESULT STDMETHODCALLTYPE SpawnDerivedClass(
		/* [in] */ long lFlags,
		/* [out] */ IWbemClassObject **ppNewClass) = 0;

	virtual HRESULT STDMETHODCALLTYPE SpawnInstance(
		/* [in] */ long lFlags,
		/* [out] */ IWbemClassObject **ppNewInstance) = 0;

	virtual HRESULT STDMETHODCALLTYPE CompareTo(
		/* [in] */ long lFlags,
		/* [in] */ IWbemClassObject *pCompareTo) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetPropertyOrigin(
		/* [string][in] */ LPCWSTR wszName,
		/* [out] */ BSTR *pstrClassName) = 0;

	virtual HRESULT STDMETHODCALLTYPE InheritsFrom(
		/* [in] */ LPCWSTR strAncestor) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetMethod(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [out] */ IWbemClassObject **ppInSignature,
		/* [out] */ IWbemClassObject **ppOutSignature) = 0;

	virtual HRESULT STDMETHODCALLTYPE PutMethod(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [in] */ IWbemClassObject *pInSignature,
		/* [in] */ IWbemClassObject *pOutSignature) = 0;

	virtual HRESULT STDMETHODCALLTYPE DeleteMethod(
		/* [string][in] */ LPCWSTR wszName) = 0;

	virtual HRESULT STDMETHODCALLTYPE BeginMethodEnumeration(
		/* [in] */ long lEnumFlags) = 0;

	virtual HRESULT STDMETHODCALLTYPE NextMethod(
		/* [in] */ long lFlags,
		/* [unique][in][out] */ BSTR *pstrName,
		/* [unique][in][out] */ IWbemClassObject **ppInSignature,
		/* [unique][in][out] */ IWbemClassObject **ppOutSignature) = 0;

	virtual HRESULT STDMETHODCALLTYPE EndMethodEnumeration(void) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetMethodQualifierSet(
		/* [string][in] */ LPCWSTR wszMethod,
		/* [out] */ IWbemQualifierSet **ppQualSet) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetMethodOrigin(
		/* [string][in] */ LPCWSTR wszMethodName,
		/* [out] */ BSTR *pstrClassName) = 0;

};

typedef void IWbemObjectSink;
typedef void IWbemCallResult;

//MIDL_INTERFACE("027947e1-d731-11ce-a357-000000000001")
struct IEnumWbemClassObject : IUnknown
{
	virtual HRESULT STDMETHODCALLTYPE Reset(void) = 0;

	virtual HRESULT STDMETHODCALLTYPE Next(
		/* [in] */ long lTimeout,
		/* [in] */ ULONG uCount,
		IWbemClassObject **apObjects,
		/* [out] */ __RPC__out ULONG *puReturned) = 0;

	virtual HRESULT STDMETHODCALLTYPE NextAsync(
		/* [in] */ ULONG uCount,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pSink) = 0;

	virtual HRESULT STDMETHODCALLTYPE Clone(
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = 0;

	virtual HRESULT STDMETHODCALLTYPE Skip(
		/* [in] */ long lTimeout,
		/* [in] */ ULONG nCount) = 0;

};

//MIDL_INTERFACE("9556dc99-828c-11cf-a37e-00aa003240c7")
struct IWbemServices : IUnknown
{
	virtual HRESULT STDMETHODCALLTYPE OpenNamespace(
		/* [in] */ __RPC__in const BSTR strNamespace,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemServices **ppWorkingNamespace,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE CancelAsyncCall(
		/* [in] */ __RPC__in_opt IWbemObjectSink *pSink) = 0;

	virtual HRESULT STDMETHODCALLTYPE QueryObjectSink(
		/* [in] */ long lFlags,
		/* [out] */ __RPC__deref_out_opt IWbemObjectSink **ppResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetObject(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemClassObject **ppObject,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE GetObjectAsync(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE PutClass(
		/* [in] */ __RPC__in_opt IWbemClassObject *pObject,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE PutClassAsync(
		/* [in] */ __RPC__in_opt IWbemClassObject *pObject,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE DeleteClass(
		/* [in] */ __RPC__in const BSTR strClass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE DeleteClassAsync(
		/* [in] */ __RPC__in const BSTR strClass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE CreateClassEnum(
		/* [in] */ __RPC__in const BSTR strSuperclass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = 0;

	virtual HRESULT STDMETHODCALLTYPE CreateClassEnumAsync(
		/* [in] */ __RPC__in const BSTR strSuperclass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE PutInstance(
		/* [in] */ __RPC__in_opt IWbemClassObject *pInst,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE PutInstanceAsync(
		/* [in] */ __RPC__in_opt IWbemClassObject *pInst,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE DeleteInstance(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE DeleteInstanceAsync(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE CreateInstanceEnum(
		/* [in] */ __RPC__in const BSTR strFilter,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = 0;

	virtual HRESULT STDMETHODCALLTYPE CreateInstanceEnumAsync(
		/* [in] */ __RPC__in const BSTR strFilter,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE ExecQuery(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = 0;

	virtual HRESULT STDMETHODCALLTYPE ExecQueryAsync(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE ExecNotificationQuery(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = 0;

	virtual HRESULT STDMETHODCALLTYPE ExecNotificationQueryAsync(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

	virtual HRESULT STDMETHODCALLTYPE ExecMethod(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ __RPC__in const BSTR strMethodName,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemClassObject *pInParams,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemClassObject **ppOutParams,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult) = 0;

	virtual HRESULT STDMETHODCALLTYPE ExecMethodAsync(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ __RPC__in const BSTR strMethodName,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemClassObject *pInParams,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler) = 0;

};

//MIDL_INTERFACE("dc12a687-737f-11cf-884d-00aa004b2e24")
struct IWbemLocator : IUnknown
{
	virtual HRESULT __stdcall ConnectServer(
		/* [in] */ const BSTR strNetworkResource,
		/* [in] */ const BSTR strUser,
		/* [in] */ const BSTR strPassword,
		/* [in] */ const BSTR strLocale,
		/* [in] */ long lSecurityFlags,
		/* [in] */ const BSTR strAuthority,
		/* [in] */ IWbemContext *pCtx,
		/* [out] */ IWbemServices **ppNamespace) = 0;

};

typedef intptr_t ULONG_PTR, *PULONG_PTR;

typedef ULONG_PTR DWORD_PTR, *PDWORD_PTR;

#define MAKELONG(a, b) ((LONG)(((WORD)(((DWORD_PTR)(a)) & 0xffff)) | ((DWORD)((WORD)(((DWORD_PTR)(b)) & 0xffff))) << 16))

extern "C"
{
	extern wchar_t* __cdecl wcsstr(wchar_t const* _Str, wchar_t const* _SubStr);

	extern BSTR __stdcall SysAllocString(const OLECHAR * psz);

	extern HRESULT __stdcall CoInitialize(LPVOID pvReserved);

	typedef GUID IID;
#define REFCLSID const IID &
	typedef /* [unique] */ IUnknown *LPUNKNOWN;

	typedef void* RPC_AUTH_IDENTITY_HANDLE;

	extern HRESULT __stdcall CoCreateInstance(_In_ REFCLSID rclsid, _In_opt_ LPUNKNOWN pUnkOuter, _In_ DWORD dwClsContext, _In_ REFIID riid, LPVOID* ppv);

	extern HRESULT __stdcall CoSetProxyBlanket(
		_In_ IUnknown * pProxy,
		_In_ DWORD dwAuthnSvc,
		_In_ DWORD dwAuthzSvc,
		_In_opt_ OLECHAR * pServerPrincName,
		_In_ DWORD dwAuthnLevel,
		_In_ DWORD dwImpLevel,
		_In_opt_ RPC_AUTH_IDENTITY_HANDLE pAuthInfo,
		_In_ DWORD dwCapabilities
		);

	extern void __stdcall CoUninitialize(void);

	extern void __stdcall SysFreeString(_In_opt_ BSTR bstrString);

	typedef /* [string] */  const OLECHAR *LPCOLESTR;
	typedef GUID CLSID;
	typedef CLSID *LPCLSID;

	HRESULT __stdcall CLSIDFromString(_In_ LPCOLESTR lpsz, _Out_ LPCLSID pclsid);

	typedef IID *LPIID;

	extern HRESULT __stdcall IIDFromString(_In_ LPCOLESTR lpsz, _Out_ LPIID lpiid);

	extern long __cdecl wcstol(wchar_t const* _String, wchar_t** _EndPtr, int _Radix);

	long Extract(WCHAR* origin, DWORD* out)
	{
		auto ret = origin + 4;
		auto v = wcstol(origin, &ret, 16);
		if (ret != origin + 4)
		{
			return -1;
		}
		*out = v;
		return 0;
	}
}

//-----------------------------------------------------------------------------
// Enum each PNP device using WMI and check each device ID to see if it contains 
// "IG_" (ex. "VID_045E&PID_028E&IG_00").  If it does, then it's an XInput device
// Unfortunately this information can not be found by just using DirectInput 
//-----------------------------------------------------------------------------
DLL_EXPORT_API extern "C" BOOL IsXInputDevice(const GUID* pGuidProductFromDirectInput)
{
	IWbemLocator*           pIWbemLocator = NULL;
	IEnumWbemClassObject*   pEnumDevices = NULL;
	IWbemClassObject*       pDevices[20] = { 0 };
	IWbemServices*          pIWbemServices = NULL;
	BSTR                    bstrNamespace = NULL;
	BSTR                    bstrDeviceID = NULL;
	BSTR                    bstrClassName = NULL;
	DWORD                   uReturned = 0;
	bool                    bIsXinputDevice = false;
	UINT                    iDevice = 0;
	VARIANT                 var;
	HRESULT                 hr;

	// CoInit if needed
	hr = CoInitialize(NULL);
	bool bCleanupCOM = SUCCEEDED(hr);

	IID wbemLocatorId;
	IIDFromString(L"{4590f811-1d3a-11d0-891f-00aa004b2e24}", &wbemLocatorId);

	IID iwbemLocatorId;
	IIDFromString(L"{dc12a687-737f-11cf-884d-00aa004b2e24}", &iwbemLocatorId);

	// Create WMI
	hr = CoCreateInstance(wbemLocatorId,
		NULL,
		CLSCTX_INPROC_SERVER,
		iwbemLocatorId,
		(LPVOID*)&pIWbemLocator);
	if (FAILED(hr) || pIWbemLocator == NULL)
		goto LCleanup;

	bstrNamespace = SysAllocString(L"\\\\.\\root\\cimv2"); if (bstrNamespace == NULL) goto LCleanup;
	bstrClassName = SysAllocString(L"Win32_PNPEntity");   if (bstrClassName == NULL) goto LCleanup;
	bstrDeviceID = SysAllocString(L"DeviceID");          if (bstrDeviceID == NULL)  goto LCleanup;

	// Connect to WMI 
	hr = pIWbemLocator->ConnectServer(bstrNamespace, NULL, NULL, 0L,
		0L, NULL, NULL, &pIWbemServices);
	if (FAILED(hr) || pIWbemServices == NULL)
		goto LCleanup;

	// Switch security level to IMPERSONATE. 
	CoSetProxyBlanket(pIWbemServices, RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, NULL,
		RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, NULL, EOAC_NONE);

	hr = pIWbemServices->CreateInstanceEnum(bstrClassName, 0, NULL, &pEnumDevices);
	if (FAILED(hr) || pEnumDevices == NULL)
		goto LCleanup;

	// Loop over all devices
	for (;; )
	{
		// Get 20 at a time
		hr = pEnumDevices->Next(10000, 20, pDevices, &uReturned);
		if (FAILED(hr))
			goto LCleanup;
		if (uReturned == 0)
			break;

		for (iDevice = 0; iDevice<uReturned; iDevice++)
		{
			// For each device, get its device ID
			hr = pDevices[iDevice]->Get(bstrDeviceID, 0L, &var, NULL, NULL);
			if (!FAILED(hr) && var.__VARIANT_NAME_1.__VARIANT_NAME_2.vt == VT_BSTR && var.__VARIANT_NAME_1.__VARIANT_NAME_2.__VARIANT_NAME_3.bstrVal != NULL)
			{
				// Check if the device ID contains "IG_".  If it does, then it's an XInput device
				// This information can not be found from DirectInput 
				if (wcsstr(var.__VARIANT_NAME_1.__VARIANT_NAME_2.__VARIANT_NAME_3.bstrVal, L"IG_"))
				{
					// If it does, then get the VID/PID from var.bstrVal
					DWORD dwPid = 0, dwVid = 0;
					WCHAR* strVid = wcsstr(var.__VARIANT_NAME_1.__VARIANT_NAME_2.__VARIANT_NAME_3.bstrVal, L"VID_");
					if (strVid && Extract(strVid + 4, &dwVid) == -1)
						dwVid = 0;
					WCHAR* strPid = wcsstr(var.__VARIANT_NAME_1.__VARIANT_NAME_2.__VARIANT_NAME_3.bstrVal, L"PID_");
					if (strPid && Extract(strPid + 4, &dwPid) == -1)
						dwPid = 0;

					// Compare the VID/PID to the DInput device
					DWORD dwVidPid = MAKELONG(dwVid, dwPid);
					if (dwVidPid == pGuidProductFromDirectInput->Data1)
					{
						bIsXinputDevice = true;
						goto LCleanup;
					}
				}
			}
			SAFE_RELEASE(pDevices[iDevice]);
		}
	}

LCleanup:
	if (bstrNamespace)
		SysFreeString(bstrNamespace);
	if (bstrDeviceID)
		SysFreeString(bstrDeviceID);
	if (bstrClassName)
		SysFreeString(bstrClassName);
	for (iDevice = 0; iDevice<20; iDevice++)
		SAFE_RELEASE(pDevices[iDevice]);

	SAFE_RELEASE(pEnumDevices);
	SAFE_RELEASE(pIWbemLocator);
	SAFE_RELEASE(pIWbemServices);

	if (bCleanupCOM)
		CoUninitialize();

	return bIsXinputDevice;
}

#else

DLL_EXPORT_API extern "C" int IsXInputDevice(void* pGuidProductFromDirectInput)
{
	return 0;
}

#endif
