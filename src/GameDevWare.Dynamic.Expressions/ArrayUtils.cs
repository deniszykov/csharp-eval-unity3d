

using System;

namespace GameDevWare.Dynamic.Expressions
{
	internal static class ArrayUtils
	{
		public static ResultT[] ConvertAll<T, ResultT>(this T[] array,
#if NETSTANDARD
			Func<T, ResultT> converter
#else
			Converter<T, ResultT> converter
#endif
		)
		{
			if (array == null) throw new ArgumentNullException("array");
			if (converter == null) throw new ArgumentNullException("converter");

#if NETSTANDARD
			var result = new ResultT[array.Length];
			for (var i = 0; i < array.Length; i++)
				result[i] = converter(array[i]);
			return result;
#else
			
			return Array.ConvertAll(array, converter);
#endif
		}
	}
}
