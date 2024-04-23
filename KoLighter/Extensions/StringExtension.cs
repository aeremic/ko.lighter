using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoLighter.Extensions
{
	internal static class StringExtension
	{
		/// <summary>
		/// Method for removing all whitespaces from given string.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		internal static string RemoveWhiteSpaces(this string source)
		{
			return new string(source.Where(c => !char.IsWhiteSpace(c)).ToArray());
		}
	}
}
