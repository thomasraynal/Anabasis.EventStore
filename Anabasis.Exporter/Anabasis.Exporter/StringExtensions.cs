using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Anabasis.Exporter
{
  public static class StringExtensions
  {
    public static string GetReadableId(this string title)
    {
      byte[] temp;

      temp = Encoding.GetEncoding("ISO-8859-8").GetBytes(title);

      var readbleId = Encoding.UTF8.GetString(temp)
        .ToLower()
        .Replace(" ", "-");

      var str = HttpUtility.UrlEncode(new string(readbleId
        .Where(@char => !char.IsControl(@char))
        .Select(@char => {

          if (char.IsPunctuation(@char)) return '-';

          return @char;

        }).ToArray()));

      return str.Replace("---", "-").Replace("--", "-").Trim('-');
    }
  }
}
