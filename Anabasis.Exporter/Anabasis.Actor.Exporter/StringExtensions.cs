using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Anabasis.Common
{
  public static class StringExtensions
  {
    static readonly List<string> _alreadyusedId = new List<string>();


    static StringExtensions()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string Clean(this string str)
    {
      var cleanString = str.Replace("&nbsp;", " ");

       cleanString = new string(HttpUtility.HtmlDecode(cleanString).Where(c =>
      {
        if (c == 173 || char.IsControl(c)) return false;

        return char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c);

      }).ToArray());

      return cleanString;
    }
    public static string Md5( params string[] str)
    {
      var crypt = new SHA256Managed();
      var hash = string.Empty;
      var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(string.Concat(str)));

      foreach (var b in crypto)
      {
        hash += b.ToString("x2");
      }

      return hash;

    }

    public static string Md5(this string str)
    {
      var crypt = new SHA256Managed();
      var hash = string.Empty;
      var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(str));

      foreach (var b in crypto)
      {
        hash += b.ToString("x2");
      }

      return hash;

    }

    public static string GetReadableId(this string title, bool throwIfDuplicate = false)
    {
      byte[] temp;

      temp = Encoding.GetEncoding("ISO-8859-8").GetBytes(title);

      var readbleId = Encoding.UTF8.GetString(temp)
        .ToLower()
        .Replace(" ", "-");

      var str = HttpUtility.UrlEncode(new string(readbleId
        .Where(@char => !char.IsControl(@char))
        .Select(@char =>
        {

          if (char.IsPunctuation(@char)) return '-';

          return @char;

        }).ToArray()));

      var iUsed = true;
      var index = 1;

      var id = str.Replace("---", "-").Replace("--", "-").Trim('-');

      while (iUsed)
      {
        if (_alreadyusedId.Contains(id))
        {
          if (throwIfDuplicate)
            throw new InvalidOperationException($"{id} is already used");

          id = str.Replace("---", "-").Replace("--", "-").Trim('-') + $"_{index}";
          index++;
        }
        else
        {
          iUsed = false;
        }

      }

      _alreadyusedId.Add(id);

      return id;
    }
  }
}

