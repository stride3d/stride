/****************************************************************************************

   Copyright (C) 2011 Autodesk, Inc.
   All rights reserved.

   Use of this software is subject to the terms of the Autodesk license agreement
   provided at the time of installation or download, or which otherwise accompanies
   this software in either electronic or hard copy form.

****************************************************************************************/
#include "stdafx.h"
#include "StreamReader.h"

#include <fbxfilesdk/fbxfilesdk_nsuse.h>

StreamReader::StreamReader(KFbxSdkManager &pFbxSdkManager, int pID):
KFbxReader(pFbxSdkManager, pID),
mFilePointer(NULL),
mManager(&pFbxSdkManager)
{
}

StreamReader::~StreamReader()
{
    FileClose();
}

void StreamReader::GetVersion(int& pMajor, int& pMinor, int& pRevision) const

{
    pMajor = 1;
    pMinor = 0;
    pRevision=0;
}

bool StreamReader::FileOpen(char* pFileName)
{
    if(mFilePointer != NULL)
        FileClose();
    mFilePointer = fopen(pFileName, "r");
    if(mFilePointer == NULL)
        return false;
    return true;
}
bool StreamReader::FileClose()
{
    if(mFilePointer!=NULL)
        fclose(mFilePointer);
    return true;
    
}
bool StreamReader::IsFileOpen()
{
    if(mFilePointer != NULL)
        return true;
    return false;
}

bool StreamReader::GetReadOptions(bool pParseFileAsNeeded)
{
    return true;
}

//Read the custom file and reconstruct node hierarchy.
bool StreamReader::Read(KFbxDocument* pDocument)
{
    if (!pDocument)
    {
        GetError().SetLastErrorID(eINVALID_DOCUMENT_HANDLE);
        return false;
    }
    KFbxScene*      lScene = KFbxCast<KFbxScene>(pDocument);
    bool            lIsAScene = (lScene != NULL);
    bool            lResult = false;

    if(lIsAScene)
    {
        KFbxNode* lRootNode = lScene->GetRootNode();
        KFbxNodeAttribute * lRootNodeAttribute = KFbxNull::Create(lScene,"");
        lRootNode->SetNodeAttribute(lRootNodeAttribute);

        int lSize;
        char* lBuffer = NULL;    
        if(mFilePointer != NULL)
        {
            //To obtain file size
            fseek (mFilePointer , 0 , SEEK_END);
            lSize = ftell (mFilePointer);
            rewind (mFilePointer);

            //Read file content to a string.
            lBuffer = (char*) malloc (sizeof(char)*lSize + 1);
            size_t lRead = fread(lBuffer, 1, lSize, mFilePointer);
            lBuffer[lRead]='\0';
            KString lString(lBuffer);

            //Parse the string to get name and relation of Nodes. 
            KString lSubString, lChildName, lParentName;
            KFbxNode* lChildNode;
            KFbxNode* lParentNode;
            KFbxNodeAttribute* lChildAttribute;
            int lEndTokenCount = lString.GetTokenCount("\n");

            for (int i = 0; i < lEndTokenCount; i++)
            {
                lSubString = lString.GetToken(i, "\n");
                KString lNodeString;
                lChildName = lSubString.GetToken(0, "\"");
                lParentName = lSubString.GetToken(2, "\"");

                //Build node hierarchy.
                if(lParentName == "RootNode")
                {
                    lChildNode = KFbxNode::Create(lScene,lChildName.Buffer());
                    lChildAttribute = KFbxNull::Create(mManager,"");
                    lChildNode->SetNodeAttribute(lChildAttribute);

                    lRootNode->AddChild(lChildNode);
                }
                else
                {
                    lChildNode = KFbxNode::Create(lScene,lChildName.Buffer());
                    lChildAttribute = KFbxNull::Create(lScene,"");
                    lChildNode->SetNodeAttribute(lChildAttribute);

                    lParentNode = lRootNode->FindChild(lParentName.Buffer());
                    lParentNode->AddChild(lChildNode);
                }
            }
            free(lBuffer);
        }
        lResult = true;
    }    
    return lResult;
}
