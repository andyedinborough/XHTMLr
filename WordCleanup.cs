using System;
using System.Text;
using System.Text.RegularExpressions;

namespace XHTMLr
{
	public class WordCleanup
	{
		#region Fields

		private static Regex rxFixes = new Regex(@"
            (<!--(\w|\W)+?-->)
                | (<title>(\w|\W)+?</title>)
                | (\s?class=\w+)
                | (\s+style='[^']+')
                | (<(meta|link|/?o:|/?style|/?div|/?st\d|/?head|/?html|body|/?body|/?span|!\[)[^>]*?>)
                | ((<[^>]+>)+&nbsp;(</\w+>)+)
                | (\s+v:\w+=""[^""]+"")
                | ((\n\r){2,})
                | (\s+[a-z]+\=[a-z]+)
            ", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

		#endregion

		#region Methods

		public static string CleanWordHtml(string html) => rxFixes.Replace(html, string.Empty).Trim();

		public static string FixEntities(byte[] html)
		{
			var ret = new StringBuilder();
			int k;
			for (int i = 0; i < html.Length; i++)
			{
				k = Convert.ToInt32(html[i]);
				if ((k >= 127 || k < 32) && k != 10 && k != 9 && k != 13)
				{
					ret.AppendFormat("&#{0};", k);
				}
				else
				{
					ret.Append(Convert.ToChar(k));
				}
			}
			return ret.ToString();
		}

		#endregion
	}
}