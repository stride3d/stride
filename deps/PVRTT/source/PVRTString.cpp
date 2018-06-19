#ifdef DEBUG

/******************************************************************************

 @File         PVRTString.cpp

 @Title        PVRTString

 @Version      

 @Copyright    Copyright (C)  Imagination Technologies Limited.

 @Platform     ANSI compatible

 @Description  A string class that can be used as drop-in replacement for
               std::string on platforms/compilers that don't provide a full C++
               standard library.

******************************************************************************/
#include "PVRTString.h"

#ifdef _USING_PVRTSTRING_

#include <stdlib.h>
#include <string.h>
#include <stdarg.h>

#include "PVRTGlobal.h"

const size_t CPVRTString::npos = (size_t) -1;

#if defined(_WIN32) && !defined(__BADA__)
#define vsnprintf _vsnprintf
#endif

/*!***********************************************************************
@Function			CPVRTString
@Input				_Ptr	A string
@Input				_Count	Length of _Ptr
@Description		Constructor
************************************************************************/
CPVRTString::CPVRTString(const char* _Ptr, size_t _Count) :
m_pString(0), m_Capacity(0)
{
	if (_Count == npos)
		assign(_Ptr);
	else
		assign(_Ptr, _Count);
}

/*!***********************************************************************
@Function			CPVRTString
@Input				_Right	A string
@Input				_Roff	Offset into _Right
@Input				_Count	Number of chars from _Right to assign to the new string
@Description		Constructor
************************************************************************/
CPVRTString::CPVRTString(const CPVRTString& _Right, size_t _Roff, size_t _Count) :
m_pString(0), m_Capacity(0)
{
	assign(_Right, _Roff, _Count);
}

/*!***********************************************************************
@Function			CPVRTString
@Input				_Count	Length of new string
@Input				_Ch		A char to fill it with
@Description		Constructor
*************************************************************************/
CPVRTString::CPVRTString(size_t _Count, char _Ch) :
m_pString(0), m_Capacity(0)
{
	assign(_Count,_Ch);
}

/*!***********************************************************************
@Function			CPVRTString
@Input				_Ch	A char
@Description		Constructor
*************************************************************************/
CPVRTString::CPVRTString(const char _Ch) :
m_pString(0), m_Capacity(0)
{
	assign( 1, _Ch);
}

/*!***********************************************************************
@Function			CPVRTString
@Description		Constructor
*************************************************************************/
CPVRTString::CPVRTString() :
m_Size(0), m_Capacity(1)
{
	m_pString = (char*)calloc(1, 1);
}

/*!***********************************************************************
@Function			~CPVRTString
@Description		Destructor
*************************************************************************/
CPVRTString::~CPVRTString()
{
	free(m_pString);
}

/*!***********************************************************************
@Function			append
@Input				_Ptr	A string
@Returns			Updated string
@Description		Appends a string
*************************************************************************/
CPVRTString& CPVRTString::append(const char* _Ptr)
{
	return append(_Ptr,strlen(_Ptr));
}

/*!***********************************************************************
@Function			append
@Input				_Ptr	A string
@Input				_Count	String length
@Returns			Updated string
@Description		Appends a string of length _Count
*************************************************************************/
CPVRTString& CPVRTString::append(const char* _Ptr, size_t _Count)
{
	char* pString = m_pString;
	size_t newCapacity = _Count + m_Size + 1;	// +1 for null termination

	// extend CPVRTString if necessary
	if (m_Capacity < newCapacity)
	{
		pString = (char*)malloc(newCapacity);
		m_Capacity = newCapacity;
		memmove(pString, m_pString, m_Size);
		pString[m_Capacity-1]='\0';
	}

	// append chars from _Ptr
	memmove(pString + m_Size, _Ptr, _Count);
	m_Size += _Count;
	pString[m_Size] = 0;

	// remove old CPVRTString if necessary
	if (pString != m_pString)
	{
		free(m_pString);
		m_pString = pString;
	}
	return *this;
}

/*!***********************************************************************
@Function			append
@Input				_Str	A string
@Returns			Updated string
@Description		Appends a string
*************************************************************************/
CPVRTString& CPVRTString::append(const CPVRTString& _Str)
{
	return append(_Str.m_pString,_Str.m_Size);
}

/*!***********************************************************************
@Function			append
@Input				_Str	A string
@Input				_Off	A position in string
@Input				_Count	Number of letters to append
@Returns			Updated string
@Description		Appends _Count letters of _Str from _Off in _Str
*************************************************************************/
CPVRTString& CPVRTString::append(const CPVRTString& _Str, size_t _Off, size_t _Count)
{
	return append(_Str.m_pString+_Off,_Count);
}

/*!***********************************************************************
@Function			append
@Input				_Ch		A char
@Input				_Count	Number of times to append _Ch
@Returns			Updated string
@Description		Appends _Ch _Count times
*************************************************************************/
CPVRTString& CPVRTString::append(size_t _Count, char _Ch)
{
	char* pString = m_pString;
	size_t newCapacity = _Count + m_Size + 1;	// +1 for null termination
	// extend CPVRTString if necessary
	if (m_Capacity < newCapacity)
	{
		pString = (char*)malloc(newCapacity);
		m_Capacity = newCapacity;
		memmove(pString, m_pString, m_Size+1);
	}

	char* newChar = &pString[m_Size];
	// fill new space with _Ch
	for(size_t i=0;i<_Count;++i)
	{
		*newChar++ = _Ch;
	}
	*newChar = '\0';		// set null terminato
	m_Size+=_Count;			// adjust length of string for new characters

	// remove old CPVRTString if necessary
	if (pString != m_pString)
	{
		free(m_pString);
		m_pString = pString;
	}
	return *this;
}

/*!***********************************************************************
@Function			assign
@Input				_Ptr A string
@Returns			Updated string
@Description		Assigns the string to the string _Ptr
*************************************************************************/
CPVRTString& CPVRTString::assign(const char* _Ptr)
{
	return assign(_Ptr, strlen(_Ptr));
}

/*!***********************************************************************
@Function			assign
@Input				_Ptr A string
@Input				_Count Length of _Ptr
@Returns			Updated string
@Description		Assigns the string to the string _Ptr
*************************************************************************/
CPVRTString& CPVRTString::assign(const char* _Ptr, size_t _Count)
{
	char* pString = m_pString;
	if (m_Capacity <= _Count)
	{
		pString = (char*)malloc(_Count + 1);
		m_Capacity = _Count+1;
	}
	m_Size = _Count;

	memmove(pString, _Ptr, m_Size);
	pString[m_Size] = 0;

	if (pString != m_pString)
	{
		free(m_pString);
		m_pString = pString;
	}
	return *this;
}

/*!***********************************************************************
@Function			assign
@Input				_Str A string
@Returns			Updated string
@Description		Assigns the string to the string _Str
*************************************************************************/
CPVRTString& CPVRTString::assign(const CPVRTString& _Str)
{
	return assign(_Str.m_pString, _Str.m_Size);
}

/*!***********************************************************************
@Function			assign
@Input				_Str A string
@Input				_Off First char to start assignment from
@Input				_Count Length of _Str
@Returns			Updated string
@Description		Assigns the string to _Count characters in string _Str starting at _Off
*************************************************************************/
CPVRTString& CPVRTString::assign(const CPVRTString& _Str, size_t _Off, size_t _Count)
{
	if(_Count==npos)
	{
		_Count = _Str.m_Size - _Off;
	}
	return assign(&_Str.m_pString[_Off], _Count);
}

/*!***********************************************************************
@Function			assign
@Input				_Ch A string
@Input				_Count Number of times to repeat _Ch
@Returns			Updated string
@Description		Assigns the string to _Count copies of _Ch
*************************************************************************/
CPVRTString& CPVRTString::assign(size_t _Count,char _Ch)
{
	if (m_Capacity <= _Count)
	{
		free(m_pString);
		m_pString = (char*)malloc(_Count + 1);
		m_Capacity = _Count+1;
	}
	m_Size = _Count;
	memset(m_pString, _Ch, _Count);
	m_pString[m_Size] = 0;

	return *this;
}

//const_reference at(size_t _Off) const;
//reference at(size_t _Off);

/*!***********************************************************************
@Function			c_str
@Returns			const char* pointer of the string
@Description		Returns a const char* pointer of the string
*************************************************************************/
const char* CPVRTString::c_str() const
{
	return m_pString;
}

/*!***********************************************************************
@Function			capacity
@Returns			The size of the character array reserved
@Description		Returns the size of the character array reserved
*************************************************************************/
size_t CPVRTString::capacity() const
{
	return m_Capacity;
}

/*!***********************************************************************
@Function			clear
@Description		Clears the string
*************************************************************************/
void CPVRTString::clear()
{
	free(m_pString);
	m_pString = (char*)calloc(1, 1);
	m_Size = 0;
	m_Capacity = 1;
}

/*!***********************************************************************
@Function			compare
@Input				_Str A string to compare with
@Returns			0 if the strings match
@Description		Compares the string with _Str
*************************************************************************/
int CPVRTString::compare(const CPVRTString& _Str) const
{
	return strcmp(m_pString,_Str.m_pString);
}

/*!***********************************************************************
@Function			<
@Input				_Str A string to compare with
@Returns			True on success
@Description		Less than operator
*************************************************************************/
bool CPVRTString::operator<(const CPVRTString & _Str) const
{
	return (strcmp(m_pString, _Str.m_pString) < 0);
}

/*!***********************************************************************
@Function			compare
@Input				_Pos1	Position to start comparing from
@Input				_Num1	Number of chars to compare
@Input				_Str 	A string to compare with
@Returns			0 if the strings match
@Description		Compares the string with _Str
*************************************************************************/
int CPVRTString::compare(size_t _Pos1, size_t _Num1, const CPVRTString& _Str) const
{
	_ASSERT(_Pos1<=m_Size);	// check comparison starts within lhs CPVRTString

	int i32Ret;	// value to return if no difference in actual comparisons between chars
	size_t stLhsLength = m_Size-_Pos1;
	size_t stSearchLength = PVRT_MIN(stLhsLength,PVRT_MIN(_Str.m_Size,_Num1));	// number of comparisons to do
	if(PVRT_MIN(stLhsLength,_Num1)<PVRT_MIN(_Str.m_Size,_Num1))
	{
		i32Ret = -1;
	}
	else if(PVRT_MIN(stLhsLength,_Num1)>PVRT_MIN(_Str.m_Size,_Num1))
	{
		i32Ret = 1;
	}
	else
	{
		i32Ret = 0;
	}

	// do actual comparison
	const char* lhptr = &m_pString[_Pos1];
	const char* rhptr = _Str.m_pString;
	for(size_t i=0;i<stSearchLength;++i)
	{
		if(*lhptr<*rhptr)
			return -1;
		else if (*lhptr>*rhptr)
			return 1;
		lhptr++;rhptr++;
	}
	// no difference found in compared characters
	return i32Ret;
}

/*!***********************************************************************
@Function			compare
@Input				_Pos1	Position to start comparing from
@Input				_Num1	Number of chars to compare
@Input				_Str 	A string to compare with
@Input				_Off 	Position in _Str to compare from
@Input				_Count	Number of chars in _Str to compare with
@Returns			0 if the strings match
@Description		Compares the string with _Str
*************************************************************************/
int CPVRTString::compare(size_t _Pos1, size_t _Num1, const CPVRTString& _Str, size_t /*_Off*/, size_t _Count) const
{
	_ASSERT(_Pos1<=m_Size);	// check comparison starts within lhs CPVRTString

	int i32Ret;	// value to return if no difference in actual comparisons between chars
	size_t stLhsLength = m_Size-_Pos1;
	size_t stSearchLength = PVRT_MIN(stLhsLength,PVRT_MIN(_Str.m_Size,PVRT_MIN(_Num1,_Count)));	// number of comparisons to do
	if(PVRT_MIN(stLhsLength,_Num1)<PVRT_MIN(_Str.m_Size,_Count))
	{
		i32Ret = -1;
	}
	else if(PVRT_MIN(stLhsLength,_Num1)>PVRT_MIN(_Str.m_Size,_Count))
	{
		i32Ret = 1;
	}
	else
	{
		i32Ret = 0;
	}


	// do actual comparison
	char* lhptr = &m_pString[_Pos1];
	char* rhptr = _Str.m_pString;
	for(size_t i=0;i<stSearchLength;++i)
	{
		if(*lhptr<*rhptr)
			return -1;
		else if (*lhptr>*rhptr)
			return 1;
		lhptr++;rhptr++;
	}
	// no difference found in compared characters
	return i32Ret;
}

/*!***********************************************************************
@Function			compare
@Input				_Ptr A string to compare with
@Returns			0 if the strings match
@Description		Compares the string with _Ptr
*************************************************************************/
int CPVRTString::compare(const char* _Ptr) const
{
	return strcmp(m_pString,_Ptr);
}

/*!***********************************************************************
@Function			compare
@Input				_Pos1	Position to start comparing from
@Input				_Num1	Number of chars to compare
@Input				_Ptr 	A string to compare with
@Returns			0 if the strings match
@Description		Compares the string with _Ptr
*************************************************************************/
int CPVRTString::compare(size_t _Pos1, size_t _Num1, const char* _Ptr) const
{
	_ASSERT(_Pos1<=m_Size);	// check comparison starts within lhs CPVRTString

	int i32Ret;	// value to return if no difference in actual comparisons between chars
	size_t stLhsLength = m_Size-_Pos1;
	size_t stRhsLength = strlen(_Ptr);
	size_t stSearchLength = PVRT_MIN(stLhsLength,PVRT_MIN(stRhsLength,_Num1));	// number of comparisons to do
	if(PVRT_MIN(stLhsLength,_Num1)<PVRT_MIN(stRhsLength,_Num1))
	{
		i32Ret = -1;
	}
	else if(PVRT_MIN(stLhsLength,_Num1)>PVRT_MIN(stRhsLength,_Num1))
	{
		i32Ret = 1;
	}
	else
	{
		i32Ret = 0;
	}

	// do actual comparison
	const char* lhptr = &m_pString[_Pos1];
	const char* rhptr = _Ptr;
	for(size_t i=0;i<stSearchLength;++i)
	{
		if(*lhptr<*rhptr)
			return -1;
		else if (*lhptr>*rhptr)
			return 1;
		lhptr++;rhptr++;
	}
	// no difference found in compared characters
	return i32Ret;
}

/*!***********************************************************************
@Function			compare
@Input				_Pos1	Position to start comparing from
@Input				_Num1	Number of chars to compare
@Input				_Ptr 	A string to compare with
@Input				_Count	Number of char to compare
@Returns			0 if the strings match
@Description		Compares the string with _Str
*************************************************************************/
int CPVRTString::compare(size_t _Pos1, size_t _Num1, const char* _Ptr, size_t _Count) const
{
	_ASSERT(_Pos1<=m_Size);	// check comparison starts within lhs CPVRTString

	int i32Ret;	// value to return if no difference in actual comparisons between chars
	size_t stLhsLength = m_Size-_Pos1;
	size_t stRhsLength = strlen(_Ptr);
	size_t stSearchLength = PVRT_MIN(stLhsLength,PVRT_MIN(stRhsLength,PVRT_MIN(_Num1,_Count)));	// number of comparisons to do
	if(PVRT_MIN(stLhsLength,_Num1)<PVRT_MIN(stRhsLength,_Count))
	{
		i32Ret = -1;
	}
	else if(PVRT_MIN(stLhsLength,_Num1)>PVRT_MIN(stRhsLength,_Count))
	{
		i32Ret = 1;
	}
	else
	{
		i32Ret = 0;
	}


	// do actual comparison
	char* lhptr = &m_pString[_Pos1];
	const char* rhptr = _Ptr;
	for(size_t i=0;i<stSearchLength;++i)
	{
		if(*lhptr<*rhptr)
			return -1;
		else if (*lhptr>*rhptr)
			return 1;
		lhptr++;rhptr++;
	}
	// no difference found in compared characters
	return i32Ret;
}

/*!***********************************************************************
@Function			==
@Input				_Str 	A string to compare with
@Returns			True if they match
@Description		== Operator
*************************************************************************/
bool CPVRTString::operator==(const CPVRTString& _Str) const
{
	return strcmp(m_pString, _Str.m_pString)==0;
}

/*!***********************************************************************
@Function			==
@Input				_Ptr 	A string to compare with
@Returns			True if they match
@Description		== Operator
*************************************************************************/
bool CPVRTString::operator==(const char* const _Ptr) const
{
	return strcmp(m_pString, _Ptr)==0;
}

/*!***********************************************************************
@Function			!=
@Input				_Str 	A string to compare with
@Returns			True if they don't match
@Description		!= Operator
*************************************************************************/
bool CPVRTString::operator!=(const CPVRTString& _Str) const
{
	return strcmp(m_pString, _Str.m_pString)!=0;
}

/*!***********************************************************************
@Function			!=
@Input				_Ptr 	A string to compare with
@Returns			True if they don't match
@Description		!= Operator
*************************************************************************/
bool CPVRTString::operator!=(const char* const _Ptr) const
{
	return strcmp(m_pString, _Ptr)!=0;
}

/*!***********************************************************************
@Function			copy
@Modified			_Ptr 	A string to copy to
@Input				_Count	Size of _Ptr
@Input				_Off	Position to start copying from
@Returns			Number of bytes copied
@Description		Copies the string to _Ptr
*************************************************************************/
size_t CPVRTString::copy(char* _Ptr, size_t _Count, size_t _Off) const
{
	if(memcpy(_Ptr, &m_pString[_Off], PVRT_MIN(_Count, m_Size - _Off)))
		return _Count;

	return 0;
}

/*!***********************************************************************
@Function			data
@Returns			A const char* version of the string
@Description		Returns a const char* version of the string
*************************************************************************/
const char* CPVRTString::data() const
{
	return m_pString;
}

/*!***********************************************************************
@Function			empty
@Returns			True if the string is empty
@Description		Returns true if the string is empty
*************************************************************************/
bool CPVRTString::empty() const
{
	return (m_Size == 0);
}

/*!***********************************************************************
@Function			erase
@Input				_Pos	The position to start erasing from
@Input				_Count	Number of chars to erase
@Returns			An updated string
@Description		Erases a portion of the string
*************************************************************************/
CPVRTString& CPVRTString::erase(size_t _Pos, size_t _Count)
{
	if (_Count == npos || _Pos + _Count >= m_Size)
	{
		resize(_Pos, 0);
	}
	else
	{
		memmove(&m_pString[_Pos], &m_pString[_Pos + _Count], m_Size + 1 - (_Pos + _Count));
	}
	return *this;
}

/*!***********************************************************************
@Function			find_first_not_of
@Input				_Ch		A char
@Input				_Off	Start position of the find
@Returns			Position of the first char that is not _Ch
@Description		Returns the position of the first char that is not _Ch
*************************************************************************/
size_t CPVRTString::find_first_not_of(char _Ch, size_t _Off) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		if(m_pString[i]!=_Ch)
			return i;
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_not_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Returns			Position of the first char that is not in _Ptr
@Description		Returns the position of the first char that is not in _Ptr
*************************************************************************/
size_t CPVRTString::find_first_not_of(const char* _Ptr, size_t _Off) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		bool bFound = false;
		// compare against each char from _Ptr
		for(size_t j=0;_Ptr[j]!=0;++j)
		{
			bFound = bFound || (m_pString[i]==_Ptr[j]);
		}
		if(!bFound)
		{	// return if no match
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_not_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Input				_Count	Number of chars in _Ptr
@Returns			Position of the first char that is not in _Ptr
@Description		Returns the position of the first char that is not in _Ptr
*************************************************************************/
size_t CPVRTString::find_first_not_of(const char* _Ptr, size_t _Off, size_t _Count) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		bool bFound = false;
		// compare against each char from _Ptr
		for(size_t j=0;j<_Count;++j)
		{
			bFound = bFound || (m_pString[i]==_Ptr[j]);
		}
		if(!bFound)
		{	// return if no match
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_not_of
@Input				_Str	A string
@Input				_Off	Start position of the find
@Returns			Position of the first char that is not in _Str
@Description		Returns the position of the first char that is not in _Str
*************************************************************************/
size_t CPVRTString::find_first_not_of(const CPVRTString& _Str, size_t _Off) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		bool bFound = false;
		// compare against each char from _Str
		for(size_t j=0;j<_Str.m_Size;++j)
		{
			bFound = bFound || (m_pString[i]==_Str[j]);
		}
		if(!bFound)
		{	// return if no match
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_of
@Input				_Ch		A char
@Input				_Off	Start position of the find
@Returns			Position of the first char that is _Ch
@Description		Returns the position of the first char that is _Ch
*************************************************************************/
size_t CPVRTString::find_first_of(char _Ch, size_t _Off) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		if(m_pString[i]==_Ch)
			return i;
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Returns			Position of the first char that matches a char in _Ptr
@Description		Returns the position of the first char that matches a char in _Ptr
*************************************************************************/
size_t CPVRTString::find_first_of(const char* _Ptr, size_t _Off) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		// compare against each char from _Ptr
		for(size_t j=0;_Ptr[j]!=0;++j)
		{
			if(m_pString[i]==_Ptr[j])
				return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Input				_Count	Size of _Ptr
@Returns			Position of the first char that matches a char in _Ptr
@Description		Returns the position of the first char that matches a char in _Ptr
*************************************************************************/
size_t CPVRTString::find_first_of(const char* _Ptr, size_t _Off, size_t _Count) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		// compare against each char from _Ptr
		for(size_t j=0;j<_Count;++j)
		{
			if(m_pString[i]==_Ptr[j])
				return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_first_of
@Input				_Str	A string
@Input				_Off	Start position of the find
@Returns			Position of the first char that matches a char in _Str
@Description		Returns the position of the first char that matches a char in _Str
*************************************************************************/
size_t CPVRTString::find_first_of(const CPVRTString& _Str, size_t _Off) const
{
	for(size_t i=_Off;i<m_Size;++i)
	{
		// compare against each char from _Ptr
		for(size_t j=0;j<_Str.m_Size;++j)
		{
			if(m_pString[i]==_Str[j])
				return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_not_of
@Input				_Ch		A char
@Input				_Off	Start position of the find
@Returns			Position of the last char that is not _Ch
@Description		Returns the position of the last char that is not _Ch
*************************************************************************/
size_t CPVRTString::find_last_not_of(char _Ch, size_t _Off) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		if(m_pString[i]!=_Ch)
		{
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_not_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Returns			Position of the last char that is not in _Ptr
@Description		Returns the position of the last char that is not in _Ptr
*************************************************************************/
size_t CPVRTString::find_last_not_of(const char* _Ptr, size_t _Off) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		bool bFound = true;
		// compare against each char from _Ptr
		for(size_t j=0;_Ptr[j]!=0;++j)
		{
			bFound = bFound && (m_pString[i]!=_Ptr[j]);
		}
		if(bFound)
		{	// return if considered character differed from all characters from _Ptr
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_not_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Input				_Count	Length of _Ptr
@Returns			Position of the last char that is not in _Ptr
@Description		Returns the position of the last char that is not in _Ptr
*************************************************************************/
size_t CPVRTString::find_last_not_of(const char* _Ptr, size_t _Off, size_t _Count) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		bool bFound = true;
		// compare against each char from _Ptr
		for(size_t j=0;j<_Count;++j)
		{
			bFound = bFound && (m_pString[i]!=_Ptr[j]);
		}
		if(bFound)
		{
		    // return if considered character differed from all characters from _Ptr
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_not_of
@Input				_Str	A string
@Input				_Off	Start position of the find
@Returns			Position of the last char that is not in _Str
@Description		Returns the position of the last char that is not in _Str
*************************************************************************/
size_t CPVRTString::find_last_not_of(const CPVRTString& _Str, size_t _Off) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		bool bFound = true;
		// compare against each char from _Ptr
		for(size_t j=0;j<_Str.m_Size;++j)
		{
			bFound = bFound && (m_pString[i]!=_Str[j]);
		}
		if(bFound)
		{
            // return if considered character differed from all characters from _Ptr
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_of
@Input				_Ch		A char
@Input				_Off	Start position of the find
@Returns			Position of the last char that is _Ch
@Description		Returns the position of the last char that is _Ch
*************************************************************************/
size_t CPVRTString::find_last_of(char _Ch, size_t _Off) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		if(m_pString[i]==_Ch)
		{
			return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Returns			Position of the last char that is in _Ptr
@Description		Returns the position of the last char that is in _Ptr
*************************************************************************/
size_t CPVRTString::find_last_of(const char* _Ptr, size_t _Off) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		// compare against each char from _Ptr
		for(size_t j=0;_Ptr[j]!=0;++j)
		{
			if(m_pString[i]==_Ptr[j])
				return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_of
@Input				_Ptr	A string
@Input				_Off	Start position of the find
@Input				_Count	Length of _Ptr
@Returns			Position of the last char that is in _Ptr
@Description		Returns the position of the last char that is in _Ptr
*************************************************************************/
size_t CPVRTString::find_last_of(const char* _Ptr, size_t _Off, size_t _Count) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		// compare against each char from _Ptr
		for(size_t j=0;j<_Count;++j)
		{
			if(m_pString[i]!=_Ptr[j])
				return i;
		}
	}
	return npos;
}

/*!***********************************************************************
@Function			find_last_of
@Input				_Str	A string
@Input				_Off	Start position of the find
@Returns			Position of the last char that is in _Str
@Description		Returns the position of the last char that is in _Str
*************************************************************************/
size_t CPVRTString::find_last_of(const CPVRTString& _Str, size_t _Off) const
{
	for(size_t i=m_Size-_Off-1;i<m_Size;--i)
	{
		// compare against each char from _Str
		for(size_t j=0;j<_Str.m_Size;++j)
		{
			if(m_pString[i]!=_Str[j])
				return i;
		}
	}
	return npos;
}

//CPVRTString& CPVRTString::insert(size_t _P0, const char* _Ptr)
//{
//	return replace(_P0, 0, _Ptr);
//}

//CPVRTString& CPVRTString::insert(size_t _P0, const char* _Ptr, size_t _Count)
//{
//	return replace(_P0, 0, _Ptr, _Count);
//}

//CPVRTString& CPVRTString::insert(size_t _P0, const CPVRTString& _Str)
//{
//	return replace(_P0, 0, _Str);
//}

//CPVRTString& CPVRTString::insert(size_t _P0, const CPVRTString& _Str, size_t _Off, size_t _Count)
//{
//	return replace(_P0, 0, _Str, _Off, _Count);
//}

//CPVRTString& CPVRTString::insert(size_t _P0, size_t _Count, char _Ch)
//{
//	return replace(_P0, 0, _Count, _Ch);
//}

/*!***********************************************************************
@Function			length
@Returns			Length of the string
@Description		Returns the length of the string
*************************************************************************/
size_t CPVRTString::length() const
{
	return m_Size;
}

/*!***********************************************************************
@Function			max_size
@Returns			The maximum number of chars that the string can contain
@Description		Returns the maximum number of chars that the string can contain
*************************************************************************/
size_t CPVRTString::max_size() const
{
	return 0x7FFFFFFF;
}

/*!***********************************************************************
@Function			push_back
@Input				_Ch A char to append
@Description		Appends _Ch to the string
*************************************************************************/
void CPVRTString::push_back(char _Ch)
{
	append(_Ch, 1);
}

//CPVRTString& replace(size_t _Pos1, size_t _Num1, const char* _Ptr)
//CPVRTString& replace(size_t _Pos1, size_t _Num1, const CPVRTString& _Str)
//CPVRTString& replace(size_t _Pos1, size_t _Num1, const char* _Ptr, size_t _Num2)
//CPVRTString& replace(size_t _Pos1, size_t _Num1, const CPVRTString& _Str, size_t _Pos2, size_t _Num2)
//CPVRTString& replace(size_t _Pos1, size_t _Num1, size_t _Count, char _Ch)

/*!***********************************************************************
@Function			reserve
@Input				_Count Size of string to reserve
@Description		Reserves space for _Count number of chars
*************************************************************************/
void CPVRTString::reserve(size_t _Count)
{
	if (_Count >= m_Capacity)
	{
		m_pString = (char*)realloc(m_pString, _Count + 1);
		m_Capacity = _Count + 1;
	}
}

/*!***********************************************************************
@Function			resize
@Input				_Count 	Size of string to resize to
@Input				_Ch		Character to use to fill any additional space
@Description		Resizes the string to _Count in length
*************************************************************************/
void CPVRTString::resize(size_t _Count, char _Ch)
{
	if (_Count <= m_Size)
	{
		m_Size = _Count;
		m_pString[m_Size] = 0;
	}
	else
	{
		append(_Count - m_Size,_Ch);
	}
}

//size_t rfind(char _Ch, size_t _Off = npos) const;
//size_t rfind(const char* _Ptr, size_t _Off = npos) const;
//size_t rfind(const char* _Ptr, size_t _Off = npos, size_t _Count) const;
//size_t rfind(const CPVRTString& _Str, size_t _Off = npos) const;

/*!***********************************************************************
@Function			size
@Returns			Size of the string
@Description		Returns the size of the string
*************************************************************************/
size_t CPVRTString::size() const
{
	return m_Size;
}

/*!***********************************************************************
@Function			substr
@Input				_Off	Start of the substring
@Input				_Count	Length of the substring
@Returns			A substring of the string
@Description		Returns the size of the string
*************************************************************************/
CPVRTString CPVRTString::substr(size_t _Off, size_t _Count) const
{
	return CPVRTString(*this, _Off, _Count);
}

/*!***********************************************************************
@Function			swap
@Input				_Str	A string to swap with
@Description		Swaps the contents of the string with _Str
*************************************************************************/
void CPVRTString::swap(CPVRTString& _Str)
{
	size_t Size = _Str.m_Size;
	size_t Capacity = _Str.m_Capacity;
	char* pString = _Str.m_pString;
	_Str.m_Size = m_Size;
	_Str.m_Capacity = m_Capacity;
	_Str.m_pString = m_pString;
	m_Size = Size;
	m_Capacity = Capacity;
	m_pString = pString;
}

/*!***********************************************************************
@Function			toLower
@Returns			An updated string
@Description		Converts the string to lower case
*************************************************************************/
CPVRTString&  CPVRTString::toLower()
{
	int i = 0;
	while ( (m_pString[i] = (m_pString[i]>='A'&&m_pString[i]<='Z') ? ('a'+m_pString[i])-'A': m_pString[i]) != 0) i++;
	return *this;
}

/*!***********************************************************************
@Function			+=
@Input				_Ch A char
@Returns			An updated string
@Description		+= Operator
*************************************************************************/
CPVRTString& CPVRTString::operator+=(char _Ch)
{
	return append(_Ch, 1);
}

/*!***********************************************************************
@Function			+=
@Input				_Ptr A string
@Returns			An updated string
@Description		+= Operator
*************************************************************************/
CPVRTString& CPVRTString::operator+=(const char* _Ptr)
{
	return append(_Ptr);
}

/*!***********************************************************************
@Function			+=
@Input				_Right A string
@Returns			An updated string
@Description		+= Operator
*************************************************************************/
CPVRTString& CPVRTString::operator+=(const CPVRTString& _Right)
{
	return append(_Right);
}

/*!***********************************************************************
@Function			=
@Input				_Ch A char
@Returns			An updated string
@Description		= Operator
*************************************************************************/
CPVRTString& CPVRTString::operator=(char _Ch)
{
	return assign(_Ch, 1);
}

/*!***********************************************************************
@Function			=
@Input				_Ptr A string
@Returns			An updated string
@Description		= Operator
*************************************************************************/
CPVRTString& CPVRTString::operator=(const char* _Ptr)
{
	return assign(_Ptr);
}

/*!***********************************************************************
@Function			=
@Input				_Right A string
@Returns			An updated string
@Description		= Operator
*************************************************************************/
CPVRTString& CPVRTString::operator=(const CPVRTString& _Right)
{
	return assign(_Right);
}

/*!***********************************************************************
@Function			[]
@Input				_Off An index into the string
@Returns			A character
@Description		[] Operator
*************************************************************************/
CPVRTString::const_reference CPVRTString::operator[](size_t _Off) const
{
	return m_pString[_Off];
}

/*!***********************************************************************
@Function			[]
@Input				_Off An index into the string
@Returns			A character
@Description		[] Operator
*************************************************************************/
CPVRTString::reference CPVRTString::operator[](size_t _Off)
{
	return m_pString[_Off];
}

/*!***********************************************************************
@Function			+
@Input				_Left A string
@Input				_Right A string
@Returns			An updated string
@Description		+ Operator
*************************************************************************/
CPVRTString operator+ (const CPVRTString& _Left, const CPVRTString& _Right)
{
	return CPVRTString(_Left).append(_Right);
}

/*!***********************************************************************
@Function			+
@Input				_Left A string
@Input				_Right A string
@Returns			An updated string
@Description		+ Operator
*************************************************************************/
CPVRTString operator+ (const CPVRTString& _Left, const char* _Right)
{
	return CPVRTString(_Left).append(_Right);
}

/*!***********************************************************************
@Function			+
@Input				_Left A string
@Input				_Right A string
@Returns			An updated string
@Description		+ Operator
*************************************************************************/
CPVRTString operator+ (const CPVRTString& _Left, const char _Right)
{
	return CPVRTString(_Left).append(_Right);
}

/*!***********************************************************************
@Function			+
@Input				_Left A string
@Input				_Right A string
@Returns			An updated string
@Description		+ Operator
*************************************************************************/
CPVRTString operator+ (const char* _Left, const CPVRTString& _Right)
{
	return CPVRTString(_Left).append(_Right);
}

/*!***********************************************************************
@Function			+
@Input				_Left A string
@Input				_Right A string
@Returns			An updated string
@Description		+ Operator
*************************************************************************/
CPVRTString operator+ (const char _Left, const CPVRTString& _Right)
{
	return CPVRTString(_Left).append(_Right);
}

/*************************************************************************
* MISCELLANEOUS UTILITY FUNCTIONS
*************************************************************************/
/*!***********************************************************************
@Function			PVRTStringGetFileExtension
@Input				strFilePath A string
@Returns			Extension
@Description		Extracts the file extension from a file path.
					Returns an empty CPVRTString if no extension is found.
************************************************************************/
CPVRTString PVRTStringGetFileExtension(const CPVRTString& strFilePath)
{
	CPVRTString::size_type idx = strFilePath.find_last_of ( '.' );

    if (idx == CPVRTString::npos)
    	return CPVRTString("");
    else
    	return strFilePath.substr(idx);
}

/*!***********************************************************************
@Function			PVRTStringGetContainingDirectoryPath
@Input				strFilePath A string
@Returns			Directory
@Description		Extracts the directory portion from a file path.
************************************************************************/
CPVRTString PVRTStringGetContainingDirectoryPath(const CPVRTString& strFilePath)
{
	size_t i32sep = strFilePath.find_last_of('/');
	if(i32sep == strFilePath.npos)
	{
		i32sep = strFilePath.find_last_of('\\');
		if(i32sep == strFilePath.npos)
		{	// can't find an actual \ or /, so return an empty string
			return CPVRTString("");
		}
	}
	return strFilePath.substr(0,i32sep);
}

/*!***********************************************************************
@Function			PVRTStringGetFileName
@Input				strFilePath A string
@Returns			FileName
@Description		Extracts the name and extension portion from a file path.
************************************************************************/
CPVRTString PVRTStringGetFileName(const CPVRTString& strFilePath)
{
	size_t i32sep = strFilePath.find_last_of('/');
	if(i32sep == strFilePath.npos)
	{
		i32sep = strFilePath.find_last_of('\\');
		if(i32sep == strFilePath.npos)
		{	// can't find an actual \ or / so leave it be
			return strFilePath;
		}
	}
	return strFilePath.substr(i32sep+1,strFilePath.length());
}

/*!***********************************************************************
@Function			PVRTStringStripWhiteSpaceFromStartOf
@Input				strLine A string
@Returns			Result of the white space stripping
@Description		strips white space characters from the beginning of a CPVRTString.
************************************************************************/
CPVRTString PVRTStringStripWhiteSpaceFromStartOf(const CPVRTString& strLine)
{
	size_t start = strLine.find_first_not_of(" \t	\n\r");
	if(start!=strLine.npos)
		return strLine.substr(start,strLine.length()-(start));
	return strLine;
}


/*!***********************************************************************
@Function			PVRTStringStripWhiteSpaceFromEndOf
@Input				strLine A string
@Returns			Result of the white space stripping
@Description		strips white space characters from the end of a CPVRTString.
************************************************************************/
CPVRTString PVRTStringStripWhiteSpaceFromEndOf(const CPVRTString& strLine)
{
	size_t end = strLine.find_last_not_of(" \t	\n\r");
	if(end!=strLine.npos)
		return strLine.substr(0,end+1);
	return strLine;
}

/*!***********************************************************************
@Function			PVRTStringFromFormattedStr
@Input				pFormat A string containing the formating
@Returns			A formatted string
@Description		Creates a formatted string
************************************************************************/
CPVRTString PVRTStringFromFormattedStr(const char *pFormat, ...)
{
	va_list arg;
	char	buf[1024];

	va_start(arg, pFormat);
#if defined(__SYMBIAN32__) || defined(UITRON) || defined(_UITRON_)
	vsprintf(buf, pFormat, arg);
#else
	vsnprintf(buf, 1024, pFormat, arg);
#endif
	va_end(arg);

	return buf;
}

///*!***************************************************************************

#endif // _USING_PVRTSTRING_

/*****************************************************************************
 End of file (PVRTString.cpp)
*****************************************************************************/

#endif // DEBUG