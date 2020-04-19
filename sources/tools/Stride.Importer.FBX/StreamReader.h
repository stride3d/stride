// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
class StreamReader : public KFbxReader
{
public:
    StreamReader(KFbxSdkManager &pFbxSdkManager, int pID);

    //VERY important to put the file close in the destructor
    virtual ~StreamReader();

    virtual void GetVersion(int& pMajor, int& pMinor, int& pRevision) const;
    virtual bool FileOpen(char* pFileName);
    virtual bool FileClose();
    virtual bool IsFileOpen();

    virtual bool GetReadOptions(bool pParseFileAsNeeded = true);
    virtual bool Read(KFbxDocument* pDocument);

private:
    FILE *mFilePointer;
    KFbxSdkManager *mManager;
};

KFbxReader* CreateMyOwnReader(KFbxSdkManager& pManager, KFbxImporter& pImporter, int pSubID, int pPluginID);
void *GetMyOwnReaderInfo(KFbxReader::KInfoRequest pRequest, int pId);
void FillOwnReaderIOSettings(KFbxIOSettings& pIOS);

