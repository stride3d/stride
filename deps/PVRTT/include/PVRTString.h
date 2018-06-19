/*!****************************************************************************

 @file         PVRTString.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        A string class that can be used as drop-in replacement for
               std::string on platforms/compilers that don't provide a full C++
               standard library.

******************************************************************************/
#ifndef _PVRTSTRING_H_
#define _PVRTSTRING_H_

#include <stdio.h>
#define _USING_PVRTSTRING_

/*!***************************************************************************
 @class CPVRTString
 @brief A string class
*****************************************************************************/

#if defined(_WINDLL_EXPORT)
class __declspec(dllexport) CPVRTString
#elif defined(_WINDLL_IMPORT)
class __declspec(dllimport) CPVRTString
#else
class CPVRTString
#endif
{

private:

	// Checking printf and scanf format strings
#if defined(_CC_GNU_) || defined(__GNUG__) || defined(__GNUC__)
#define FX_PRINTF(fmt,arg) __attribute__((format(printf,fmt,arg)))
#define FX_SCANF(fmt,arg)  __attribute__((format(scanf,fmt,arg)))
#else
#define FX_PRINTF(fmt,arg)
#define FX_SCANF(fmt,arg)
#endif

public:
	typedef	size_t	size_type;
	typedef	char value_type;
	typedef	char& reference;
	typedef	const char& const_reference;

	static const size_type npos;


	

	/*!***********************************************************************
	@brief      		CPVRTString constructor
	@param[in]				_Ptr	A string
	@param[in]				_Count	Length of _Ptr
	************************************************************************/
	CPVRTString(const char* _Ptr, size_t _Count = npos);

	/*!***********************************************************************
	@brief      		CPVRTString constructor
	@param[in]				_Right	A string
	@param[in]				_Roff	Offset into _Right
	@param[in]				_Count	Number of chars from _Right to assign to the new string
	************************************************************************/
	CPVRTString(const CPVRTString& _Right, size_t _Roff = 0, size_t _Count = npos);

	/*!***********************************************************************
	@brief      		CPVRTString constructor 
	@param[in]				_Count	Length of new string
	@param[in]				_Ch		A char to fill it with
	*************************************************************************/
	CPVRTString(size_t _Count, const char _Ch);

	/*!***********************************************************************
	@brief      		Constructor
	@param[in]				_Ch	A char
	*************************************************************************/
	CPVRTString(const char _Ch);

	/*!***********************************************************************
	@brief      		Constructor
	************************************************************************/
	CPVRTString();

	/*!***********************************************************************
	@brief      		Destructor
	************************************************************************/
	virtual ~CPVRTString();

	/*!***********************************************************************
	@brief      		Appends a string
	@param[in]			_Ptr	A string
	@return 			Updated string
	*************************************************************************/
	CPVRTString& append(const char* _Ptr);

	/*!***********************************************************************
	@brief      		Appends a string of length _Count
	@param[in]			_Ptr	A string
	@param[in]			_Count	String length
	@return 			Updated string
	*************************************************************************/
	CPVRTString& append(const char* _Ptr, size_t _Count);

	/*!***********************************************************************
	@brief      		Appends a string
	@param[in]			_Str	A string
	@return 			Updated string
	*************************************************************************/
	CPVRTString& append(const CPVRTString& _Str);

	/*!***********************************************************************
	@brief      		Appends _Count letters of _Str from _Off in _Str
	@param[in]			_Str	A string
	@param[in]			_Off	A position in string
	@param[in]			_Count	Number of letters to append
	@return 			Updated string
	*************************************************************************/
	CPVRTString& append(const CPVRTString& _Str, size_t _Off, size_t _Count);

	/*!***********************************************************************
	@brief      		Appends _Ch _Count times
	@param[in]				_Ch		A char
	@param[in]				_Count	Number of times to append _Ch
	@return 			Updated string
	*************************************************************************/
	CPVRTString& append(size_t _Count, const char _Ch);

	//template<class InputIterator> CPVRTString& append(InputIterator _First, InputIterator _Last);

	/*!***********************************************************************
	@brief      		Assigns the string to the string _Ptr
	@param[in]			_Ptr A string
	@return 			Updated string
	*************************************************************************/
	CPVRTString& assign(const char* _Ptr);

	/*!***********************************************************************
	@brief      		Assigns the string to the string _Ptr
	@param[in]			_Ptr A string
	@param[in]			_Count Length of _Ptr
	@return 			Updated string
	*************************************************************************/
	CPVRTString& assign(const char* _Ptr, size_t _Count);

	/*!***********************************************************************
	@brief      		Assigns the string to the string _Str
	@param[in]			_Str A string
	@return 			Updated string
	*************************************************************************/
	CPVRTString& assign(const CPVRTString& _Str);

	/*!***********************************************************************
	@brief      		Assigns the string to _Count characters in string _Str starting at _Off
	@param[in]			_Str A string
	@param[in]			_Off First char to start assignment from
	@param[in]			_Count Length of _Str
	@return 			Updated string
	*************************************************************************/
	CPVRTString& assign(const CPVRTString& _Str, size_t _Off, size_t _Count=npos);

	/*!***********************************************************************
	@brief      		Assigns the string to _Count copies of _Ch
	@param[in]			_Ch A string
	@param[in]			_Count Number of times to repeat _Ch
	@return 			Updated string
	*************************************************************************/
	CPVRTString& assign(size_t _Count, char _Ch);

	//template<class InputIterator> CPVRTString& assign(InputIterator _First, InputIterator _Last);

	//const_reference at(size_t _Off) const;
	//reference at(size_t _Off);

	// const_iterator begin() const;
	// iterator begin();

	/*!***********************************************************************
	@brief      		Returns a const char* pointer of the string
	@return 			const char* pointer of the string
	*************************************************************************/
	const char* c_str() const;

	/*!***********************************************************************
	@brief      		Returns the size of the character array reserved
	@return 			The size of the character array reserved
	*************************************************************************/
	size_t capacity() const;

	/*!***********************************************************************
	@brief      		Clears the string
	*************************************************************************/
	void clear();

	/*!***********************************************************************
	@brief      		Compares the string with _Str
	@param[in]			_Str A string to compare with
	@return 			0 if the strings match
	*************************************************************************/
	int compare(const CPVRTString& _Str) const;

	/*!***********************************************************************
	@brief      		Compares the string with _Str
	@param[in]			_Pos1	Position to start comparing from
	@param[in]			_Num1	Number of chars to compare
	@param[in]			_Str 	A string to compare with
	@return 			0 if the strings match
	*************************************************************************/
	int compare(size_t _Pos1, size_t _Num1, const CPVRTString& _Str) const;

	/*!***********************************************************************
	@brief      		Compares the string with _Str
	@param[in]			_Pos1	Position to start comparing from
	@param[in]			_Num1	Number of chars to compare
	@param[in]			_Str 	A string to compare with
	@param[in]			_Off 	Position in _Str to compare from
	@param[in]			_Count	Number of chars in _Str to compare with
	@return 			0 if the strings match
	*************************************************************************/
	int compare(size_t _Pos1, size_t _Num1, const CPVRTString& _Str, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Compares the string with _Ptr
	@param[in]			_Ptr A string to compare with
	@return 			0 if the strings match
	*************************************************************************/
	int compare(const char* _Ptr) const;

	/*!***********************************************************************
	@brief      		Compares the string with _Ptr
	@param[in]			_Pos1	Position to start comparing from
	@param[in]			_Num1	Number of chars to compare
	@param[in]			_Ptr 	A string to compare with
	@return 			0 if the strings match
	*************************************************************************/
	int compare(size_t _Pos1, size_t _Num1, const char* _Ptr) const;

	/*!***********************************************************************
	@brief      		Compares the string with _Str
	@param[in]			_Pos1	Position to start comparing from
	@param[in]			_Num1	Number of chars to compare
	@param[in]			_Ptr 	A string to compare with
	@param[in]			_Count	Number of chars to compare
	@return 			0 if the strings match
	*************************************************************************/
	int compare(size_t _Pos1, size_t _Num1, const char* _Ptr, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Less than operator
	@param[in]			_Str A string to compare with
	@return 			True on success
	*************************************************************************/
	bool operator<(const CPVRTString & _Str) const;

	/*!***********************************************************************
	@brief      	== Operator
	@param[in]		_Str 	A string to compare with
	@return 		True if they match
	*************************************************************************/
	bool operator==(const CPVRTString& _Str) const;

	/*!***********************************************************************
	@brief      	== Operator
	@param[in]		_Ptr 	A string to compare with
	@return 		True if they match
	*************************************************************************/
	bool operator==(const char* const _Ptr) const;

	/*!***********************************************************************
	@brief      		!= Operator
	@param[in]				_Str 	A string to compare with
	@return 			True if they don't match
	*************************************************************************/
	bool operator!=(const CPVRTString& _Str) const;

	/*!***********************************************************************
	@brief      		!= Operator
	@param[in]			_Ptr 	A string to compare with
	@return 			True if they don't match
	*************************************************************************/
	bool operator!=(const char* const _Ptr) const;

	/*!***********************************************************************
	@fn       			copy
	@param[in,out]		_Ptr 	A string to copy to
	@param[in]			_Count	Size of _Ptr
	@param[in]			_Off	Position to start copying from
	@return 			Number of bytes copied
	@brief      		Copies the string to _Ptr
	*************************************************************************/
	size_t copy(char* _Ptr, size_t _Count, size_t _Off = 0) const;

	/*!***********************************************************************
	@fn       			data
	@return 			A const char* version of the string
	@brief      		Returns a const char* version of the string
	*************************************************************************/
	const char* data( ) const;

	/*!***********************************************************************
	@fn       			empty
	@return 			True if the string is empty
	@brief      		Returns true if the string is empty
	*************************************************************************/
	bool empty() const;

	// const_iterator end() const;
	// iterator end();

	//iterator erase(iterator _First, iterator _Last);
	//iterator erase(iterator _It);

	/*!***********************************************************************
	@brief      		Erases a portion of the string
	@param[in]			_Pos	The position to start erasing from
	@param[in]			_Count	Number of chars to erase
	@return 			An updated string
	*************************************************************************/
	CPVRTString& erase(size_t _Pos = 0, size_t _Count = npos);

	/*!***********************************************************************
	@brief      		Erases a portion of the string
	@param[in]			_src	Character to search
	@param[in]			_subDes	Character to substitute for
	@param[in]			_all	Substitute all
	@return 			An updated string
	*************************************************************************/
	CPVRTString& substitute(char _src,char _subDes, bool _all = true);

	/*!***********************************************************************
	@brief      		Erases a portion of the string
	@param[in]			_src	Character to search
	@param[in]			_subDes	Character to substitute for
	@param[in]			_all	Substitute all
	@return 			An updated string
	*************************************************************************/
	CPVRTString& substitute(const char* _src, const char* _subDes, bool _all = true);

	//size_t find(char _Ch, size_t _Off = 0) const;
	//size_t find(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Finds a substring within this string.
	@param[in]			_Ptr	String to search.
	@param[in]			_Off	Offset to search from.
	@param[in]			_Count	Number of characters in this string.
	@return 			Position of the first matched string.
	*************************************************************************/
	size_t find(const char* _Ptr, size_t _Off, size_t _Count) const;
	
	/*!***********************************************************************
	@brief      		Finds a substring within this string.
	@param[in]			_Str	String to search.
	@param[in]			_Off	Offset to search from.
	@return 			Position of the first matched string.
	*************************************************************************/
	size_t find(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that is not _Ch
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Position of the first char that is not _Ch
	*************************************************************************/
	size_t find_first_not_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that is not in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the first char that is not in _Ptr
	*************************************************************************/
	size_t find_first_not_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that is not in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Number of chars in _Ptr
	@return 			Position of the first char that is not in _Ptr
	*************************************************************************/
	size_t find_first_not_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that is not in _Str
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the first char that is not in _Str
	*************************************************************************/
	size_t find_first_not_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that is _Ch
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Position of the first char that is _Ch
	*************************************************************************/
	size_t find_first_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that matches a char in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the first char that matches a char in _Ptr
	*************************************************************************/
	size_t find_first_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that matches a char in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Size of _Ptr
	@return 			Position of the first char that matches a char in _Ptr
	*************************************************************************/
	size_t find_first_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the position of the first char that matches all chars in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Size of _Ptr
	@return 			Position of the first char that matches a char in _Ptr
	*************************************************************************/
	size_t find_first_ofn(const char* _Ptr, size_t _Off, size_t _Count) const;
	

	/*!***********************************************************************
	@brief      		Returns the position of the first char that matches a char in _Str
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the first char that matches a char in _Str
	*************************************************************************/
	size_t find_first_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is not _Ch
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Position of the last char that is not _Ch
	*************************************************************************/
	size_t find_last_not_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is not in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the last char that is not in _Ptr
	*************************************************************************/
	size_t find_last_not_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is not in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Length of _Ptr
	@return 			Position of the last char that is not in _Ptr
	*************************************************************************/
	size_t find_last_not_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is not in _Str
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the last char that is not in _Str
	*************************************************************************/
	size_t find_last_not_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is _Ch
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Position of the last char that is _Ch
	*************************************************************************/
	size_t find_last_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the last char that is in _Ptr
	*************************************************************************/
	size_t find_last_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is in _Ptr
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Length of _Ptr
	@return 			Position of the last char that is in _Ptr
	*************************************************************************/
	size_t find_last_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the position of the last char that is in _Str
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Position of the last char that is in _Str
	*************************************************************************/
	size_t find_last_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the number of occurances of _Ch in the parent string.
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Number of occurances of _Ch in the parent string.
	*************************************************************************/
	size_t find_number_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the number of occurances of _Ptr in the parent string.
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Number of occurances of _Ptr in the parent string.
	*************************************************************************/
	size_t find_number_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the number of occurances of _Ptr in the parent string.
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Size of _Ptr
	@return 			Number of occurances of _Ptr in the parent string.
	*************************************************************************/
	size_t find_number_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the number of occurances of _Str in the parent string.
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Number of occurances of _Str in the parent string.
	*************************************************************************/
	size_t find_number_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the next occurance of _Ch in the parent string
                        after or at _Off.	If not found, returns the length of the string.
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Next occurance of _Ch in the parent string.
	*************************************************************************/
	int find_next_occurance_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the next occurance of _Ptr in the parent string
                        after or at _Off.	If not found, returns the length of the string.
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Next occurance of _Ptr in the parent string.
	*************************************************************************/
	int find_next_occurance_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the next occurance of _Ptr in the parent string
                        after or at _Off.	If not found, returns the length of the string.
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Size of _Ptr
	@return 			Next occurance of _Ptr in the parent string.
	*************************************************************************/
	int find_next_occurance_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the next occurance of _Str in the parent string
                        after or at _Off.	If not found, returns the length of the string.
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Next occurance of _Str in the parent string.
	*************************************************************************/
	int find_next_occurance_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the previous occurance of _Ch in the parent string
                        before _Off.	If not found, returns -1.
	@param[in]			_Ch		A char
	@param[in]			_Off	Start position of the find
	@return 			Previous occurance of _Ch in the parent string.
	*************************************************************************/
	int find_previous_occurance_of(char _Ch, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the previous occurance of _Ptr in the parent string
                        before _Off.	If not found, returns -1.
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@return 			Previous occurance of _Ptr in the parent string.
	*************************************************************************/
	int find_previous_occurance_of(const char* _Ptr, size_t _Off = 0) const;

	/*!***********************************************************************
	@brief      		Returns the previous occurance of _Ptr in the parent string
                        before _Off.	If not found, returns -1.
	@param[in]			_Ptr	A string
	@param[in]			_Off	Start position of the find
	@param[in]			_Count	Size of _Ptr
	@return 			Previous occurance of _Ptr in the parent string.
	*************************************************************************/
	int find_previous_occurance_of(const char* _Ptr, size_t _Off, size_t _Count) const;

	/*!***********************************************************************
	@brief      		Returns the previous occurance of _Str in the parent string
                        before _Off.	If not found, returns -1.
	@param[in]			_Str	A string
	@param[in]			_Off	Start position of the find
	@return 			Previous occurance of _Str in the parent string.
	*************************************************************************/
	int find_previous_occurance_of(const CPVRTString& _Str, size_t _Off = 0) const;

	/*!***********************************************************************
	@fn       			left
	@param[in]			iSize	number of characters to return (excluding null character)
	@return 			The leftmost 'iSize' characters of the string.
	@brief      		Returns the leftmost characters of the string (excluding 
	the null character) in a new CPVRTString. If iSize is
	larger than the string, a copy of the original string is returned.
	*************************************************************************/
	CPVRTString left(size_t iSize) const;

	/*!***********************************************************************
	@fn       			right
	@param[in]			iSize	number of characters to return (excluding null character)
	@return 			The rightmost 'iSize' characters of the string.
	@brief      		Returns the rightmost characters of the string (excluding 
	the null character) in a new CPVRTString. If iSize is
	larger than the string, a copy of the original string is returned.
	*************************************************************************/
	CPVRTString right(size_t iSize) const;

	//allocator_type get_allocator( ) const;

	//CPVRTString& insert(size_t _P0, const char* _Ptr);
	//CPVRTString& insert(size_t _P0, const char* _Ptr, size_t _Count);
	//CPVRTString& insert(size_t _P0, const CPVRTString& _Str);
	//CPVRTString& insert(size_t _P0, const CPVRTString& _Str, size_t _Off, size_t _Count);
	//CPVRTString& insert(size_t _P0, size_t _Count, char _Ch);
	//iterator insert(iterator _It, char _Ch = char());
	//template<class InputIterator> void insert(iterator _It, InputIterator _First, InputIterator _Last);
	//void insert(iterator _It, size_t _Count, char _Ch);

	/*!***********************************************************************
	@fn       			length
	@return 			Length of the string
	@brief      		Returns the length of the string
	*************************************************************************/
	size_t length() const;

	/*!***********************************************************************
	@fn       			max_size
	@return 			The maximum number of chars that the string can contain
	@brief      		Returns the maximum number of chars that the string can contain
	*************************************************************************/
	size_t max_size() const;

	/*!***********************************************************************
	@fn       			push_back
	@param[in]			_Ch A char to append
	@brief      		Appends _Ch to the string
	*************************************************************************/
	void push_back(char _Ch);

	// const_reverse_iterator rbegin() const;
	// reverse_iterator rbegin();

	// const_reverse_iterator rend() const;
	// reverse_iterator rend();

	//CPVRTString& replace(size_t _Pos1, size_t _Num1, const char* _Ptr);
	//CPVRTString& replace(size_t _Pos1, size_t _Num1, const CPVRTString& _Str);
	//CPVRTString& replace(size_t _Pos1, size_t _Num1, const char* _Ptr, size_t _Num2);
	//CPVRTString& replace(size_t _Pos1, size_t _Num1, const CPVRTString& _Str, size_t _Pos2, size_t _Num2);
	//CPVRTString& replace(size_t _Pos1, size_t _Num1, size_t _Count, char _Ch);

	//CPVRTString& replace(iterator _First0, iterator _Last0, const char* _Ptr);
	//CPVRTString& replace(iterator _First0, iterator _Last0, const CPVRTString& _Str);
	//CPVRTString& replace(iterator _First0, iterator _Last0, const char* _Ptr, size_t _Num2);
	//CPVRTString& replace(iterator _First0, iterator _Last0, size_t _Num2, char _Ch);
	//template<class InputIterator> CPVRTString& replace(iterator _First0, iterator _Last0, InputIterator _First, InputIterator _Last);

	/*!***********************************************************************
	@fn       			reserve
	@param[in]			_Count Size of string to reserve
	@brief      		Reserves space for _Count number of chars
	*************************************************************************/
	void reserve(size_t _Count = 0);

	/*!***********************************************************************
	@fn       			resize
	@param[in]			_Count 	Size of string to resize to
	@param[in]			_Ch		Character to use to fill any additional space
	@brief      		Resizes the string to _Count in length
	*************************************************************************/
	void resize(size_t _Count, char _Ch = char());

	//size_t rfind(char _Ch, size_t _Off = npos) const;
	//size_t rfind(const char* _Ptr, size_t _Off = npos) const;
	//size_t rfind(const char* _Ptr, size_t _Off = npos, size_t _Count) const;
	//size_t rfind(const CPVRTString& _Str, size_t _Off = npos) const;

	/*!***********************************************************************
	@fn       			size
	@return 			Size of the string
	@brief      		Returns the size of the string
	*************************************************************************/
	size_t size() const;

	/*!***********************************************************************
	@fn       			substr
	@param[in]			_Off	Start of the substring
	@param[in]			_Count	Length of the substring
	@return 			A substring of the string
	@brief      		Returns the size of the string
	*************************************************************************/
	CPVRTString substr(size_t _Off = 0, size_t _Count = npos) const;

	/*!***********************************************************************
	@fn       			swap
	@param[in]			_Str	A string to swap with
	@brief      		Swaps the contents of the string with _Str
	*************************************************************************/
	void swap(CPVRTString& _Str);

	/*!***********************************************************************
	@fn       			toLower
	@return 			An updated string
	@brief      		Converts the string to lower case
	*************************************************************************/
	CPVRTString& toLower();
	
	/*!***********************************************************************
	@fn       			toUpper
	@return 			An updated string
	@brief      		Converts the string to upper case
	*************************************************************************/
	CPVRTString& toUpper();

	/*!***********************************************************************
	@fn       			format
	@param[in]			pFormat A string containing the formating
	@return 			A formatted string
	@brief      		return the formatted string
	************************************************************************/
	CPVRTString format(const char *pFormat, ...);
	
#ifndef UNDER_CE
	/*!***********************************************************************
	@fn       			formatPositional
	@param[in]			pFormat A string containing the formatting.
								Positional modifiers may be used.
	@return 			A formatted string
	@brief      		return the formatted string
	************************************************************************/
	CPVRTString formatPositional(const char *pFormat, ...);
#endif

	/*!***********************************************************************
	@brief      		+= Operator
	@param[in]			_Ch A char
	@return 			An updated string
	*************************************************************************/
	CPVRTString& operator+=(char _Ch);

	/*!***********************************************************************
	@brief      		+= Operator
	@param[in]			_Ptr A string
	@return 			An updated string
	*************************************************************************/
	CPVRTString& operator+=(const char* _Ptr);

	/*!***********************************************************************
	@brief      		+= Operator
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	CPVRTString& operator+=(const CPVRTString& _Right);

	/*!***********************************************************************
	@brief      		= Operator
	@param[in]			_Ch A char
	@return 			An updated string
	*************************************************************************/
	CPVRTString& operator=(char _Ch);

	/*!***********************************************************************
	@brief      		= Operator
	@param[in]			_Ptr A string
	@return 			An updated string
	*************************************************************************/
	CPVRTString& operator=(const char* _Ptr);

	/*!***********************************************************************
	@brief      		= Operator
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	CPVRTString& operator=(const CPVRTString& _Right);

	/*!***********************************************************************
	@brief      		[] Operator
	@param[in]			_Off An index into the string
	@return 			A character
	*************************************************************************/
	const_reference operator[](size_t _Off) const;

	/*!***********************************************************************
	@brief      		[] Operator
	@param[in]			_Off An index into the string
	@return 			A character
	*************************************************************************/
	reference operator[](size_t _Off);

	/*!***********************************************************************
	@brief      		+ Operator
	@param[in]			_Left A string
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	friend CPVRTString operator+ (const CPVRTString& _Left, const CPVRTString& _Right);

	/*!***********************************************************************
	@brief      		+ Operator
	@param[in]			_Left A string
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	friend CPVRTString operator+ (const CPVRTString& _Left, const char* _Right);

	/*!***********************************************************************
	@brief      		+ Operator
	@param[in]			_Left A string
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	friend CPVRTString operator+ (const CPVRTString& _Left, const char _Right);

	/*!***********************************************************************
	@brief      		+ Operator
	@param[in]			_Left A string
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	friend CPVRTString operator+ (const char* _Left, const CPVRTString& _Right);


	/*!***********************************************************************
	@brief      		+ Operator
	@param[in]			_Left A string
	@param[in]			_Right A string
	@return 			An updated string
	*************************************************************************/
	friend CPVRTString operator+ (const char _Left, const CPVRTString& _Right);

protected:
	char* m_pString;
	size_t m_Size;
	size_t m_Capacity;
};

/*************************************************************************
* MISCELLANEOUS UTILITY FUNCTIONS
*************************************************************************/
/*!***********************************************************************
 @fn       			PVRTStringGetFileExtension
 @param[in]			strFilePath A string
 @return 			Extension
 @brief      		Extracts the file extension from a file path.
                    Returns an empty CPVRTString if no extension is found.
************************************************************************/
CPVRTString PVRTStringGetFileExtension(const CPVRTString& strFilePath);

/*!***********************************************************************
 @fn       			PVRTStringGetContainingDirectoryPath
 @param[in]			strFilePath A string
 @return 			Directory
 @brief      		Extracts the directory portion from a file path.
************************************************************************/
CPVRTString PVRTStringGetContainingDirectoryPath(const CPVRTString& strFilePath);

/*!***********************************************************************
 @fn       			PVRTStringGetFileName
 @param[in]			strFilePath A string
 @return 			FileName
 @brief      		Extracts the name and extension portion from a file path.
************************************************************************/
CPVRTString PVRTStringGetFileName(const CPVRTString& strFilePath);

/*!***********************************************************************
 @fn       			PVRTStringStripWhiteSpaceFromStartOf
 @param[in]			strLine A string
 @return 			Result of the white space stripping
 @brief      		strips white space characters from the beginning of a CPVRTString.
************************************************************************/
CPVRTString PVRTStringStripWhiteSpaceFromStartOf(const CPVRTString& strLine);

/*!***********************************************************************
 @fn       			PVRTStringStripWhiteSpaceFromEndOf
 @param[in]			strLine A string
 @return 			Result of the white space stripping
 @brief      		strips white space characters from the end of a CPVRTString.
************************************************************************/
CPVRTString PVRTStringStripWhiteSpaceFromEndOf(const CPVRTString& strLine);

/*!***********************************************************************
 @fn       			PVRTStringFromFormattedStr
 @param[in]			pFormat A string containing the formating
 @return 			A formatted string
 @brief      		Creates a formatted string
************************************************************************/
CPVRTString PVRTStringFromFormattedStr(const char *pFormat, ...);

#ifndef UNDER_CE
/*!***********************************************************************
@Function			PVRTStringFromFormattedStrPositional
@Input				pFormat A string containing the formatting
							with optional positional qualifiers
@Returns			A formatted string
@Description		Creates a formatted string
************************************************************************/
CPVRTString PVRTStringFromFormattedStrPositional(const char *pFormat, ...);
#endif

#endif // _PVRTSTRING_H_

/*****************************************************************************
End of file (PVRTString.h)
*****************************************************************************/

