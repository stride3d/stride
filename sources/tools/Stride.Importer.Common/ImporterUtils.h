// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#ifndef __IMPORTER_UTILS_H__
#define __IMPORTER_UTILS_H__

void ReplaceCharacter(std::string& name, char c, char replacement)
{
	size_t nextCharacterPos = name.find(c);
	while (nextCharacterPos != std::string::npos)
	{
		name.replace(nextCharacterPos, 1, 1, replacement);
		nextCharacterPos = name.find(c, nextCharacterPos);
	}
}

void RemoveCharacter(std::string& name, char c)
{
	size_t nextCharacterPos = name.find(c);
	while (nextCharacterPos != std::string::npos)
	{
		name.erase(nextCharacterPos, 1);
		nextCharacterPos = name.find(c, nextCharacterPos);
	}
}

#endif // __IMPORTER_UTILS_H__
