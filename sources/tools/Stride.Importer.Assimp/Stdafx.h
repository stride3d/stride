// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#pragma warning( disable : 4945 )

#include "assimp/Importer.hpp"
#include "assimp/Scene.h"
#include "assimp/IOSystem.hpp"
#pragma make_public(aiScene)

#include "assimp/PostProcess.h"

#include <msclr/marshal_cppstd.h>

#include "UtilityFunctions.h"

#include <vector>
#include <stack>