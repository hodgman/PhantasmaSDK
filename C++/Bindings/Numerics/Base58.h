﻿#pragma once
#include "BigInteger.h"

namespace phantasma {

template<class T>
void ArrayReverse(PHANTASMA_VECTOR<T>& vec)
{
	int last = vec.size()-1;
	for( int i=0, end=(int)vec.size()/2; i!=end; ++i )
		PHANTASMA_SWAP( vec[i], vec[last-i] );
}

namespace Base58
{
	constexpr Char Alphabet[] = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
	inline int AlphabetIndexOf( Char in )
	{
		for( const Char* c = Alphabet; *c; ++c )
			if( *c == in )
				return (int)(c - Alphabet);
		return -1;
	}

	inline PHANTASMA_VECTOR<Byte> Decode(const String& input)
	{
		PHANTASMA_VECTOR<Byte> tmp;
		if(input.empty())
		{
			return tmp;
		}

		BigInteger bi = BigInteger::Zero();
		for (int i = input.length() - 1; i >= 0; i--)
		{
			int index = AlphabetIndexOf(input[i]);
			if(index < 0)
			{
				PHANTASMA_EXCEPTION("invalid character");
				return tmp;
			}

			bi += BigInteger(index) * BigInteger::Pow(58, input.length() - 1 - i);
		}

		PHANTASMA_VECTOR<Byte> bytes = bi.ToByteArray();
		ArrayReverse(bytes);

		int leadingZeros = 0;
		for (int i = 0; i < input.length() && input[i] == Alphabet[0]; i++)
		{
			leadingZeros++;
		}

		tmp.resize(bytes.size() + leadingZeros);
		PHANTASMA_COPY(bytes.begin(), bytes.end(), tmp.begin() + leadingZeros);
		return tmp;
	}

	inline String Encode(const Byte* input, int length)
	{
		if( length == 0 )
			return String();

		PHANTASMA_VECTOR<Byte> temp;
		temp.resize(length + 1);
		for (int i=0; i<length; i++)
		{
			temp[i] = input[(length - 1) - i];
		}
		temp[length] = 0;

		BigInteger value(temp);
		PHANTASMA_WIPEMEM(temp.begin(), temp.size());
		PHANTASMA_VECTOR<Char> sb;
		while (value >= 58)
		{
			BigInteger mod = value % 58;
			sb.push_back(Alphabet[(int)mod]);
			value /= 58;
		}
		sb.push_back(Alphabet[(int)value]);

		for(int i=0; i<length; ++i)
		{
			if( input[i] == 0 )
				sb.push_back(Alphabet[0]);
			else
				break;
		}
		ArrayReverse(sb);

		String result(&sb.front(), sb.size());
		PHANTASMA_WIPEMEM(&sb.front(), sb.size());
		return result;
	}
}

}
