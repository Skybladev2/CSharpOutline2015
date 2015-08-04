using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JSOutlining11.Outlining
{
	/// <summary>
	/// Formats hint for collapsed code
	/// </summary>
	internal class CollapsedHintFormatter
	{
		private TextRegion Region;
		private const int MaxLines = 30;

		public CollapsedHintFormatter(TextRegion region)
		{
			Region = region;
		}

		/// <summary>
		/// Formats span text for better readability
		/// </summary>		
		/// <returns>Formatted text</returns>
		public string FormatHint()
		{
			string textBefore = Region.TextBefore;			
			string text = (string.IsNullOrWhiteSpace(textBefore) ? textBefore : "") + Region.InnerText;
			string[] lines = text.Split(new string[]{"\r\n", "\n"}, StringSplitOptions.None);
			//first, remove empty lines at start
			int empty = 0;
			while (empty < lines.Length && string.IsNullOrWhiteSpace(lines[empty]))
				empty++;

			//then put next lines (no more than MaxLines) into array
			//with tabs replaced by spaces
			string tabSpaces = new string(' ', Region.Tagger.Options.TabSize);
			string[] textLines = new string[Math.Min(lines.Length - empty, MaxLines)];			
			for (int i = 0; i < textLines.Length; i++)
				textLines[i] = lines[i + empty].Replace("\t", tabSpaces);

			//removing redundant indentation
			//calculating minimal indentation
			int minIndent = int.MaxValue;
			foreach (string s in textLines)
				minIndent = Math.Min(minIndent, GetIndentation(s));

			//unindenting all lines
			for (int i = 0; i < textLines.Length; i++)
				textLines[i] = textLines[i].Length > minIndent ? textLines[i].Substring(minIndent) : "";

			string res =  string.Join("\n", textLines);
			//if there are more lines then insert "..." at end
			if (lines.Length - empty > MaxLines)
				res += "\n...";
			return res;
		}

		private static int GetIndentation(string s)
		{				
			int i = 0;
			while (i < s.Length && char.IsWhiteSpace(s[i]))
				i++;
			//for lines entirely consisting of whitespace return int.MaxValue
			//so it won't affect indentation calculation
			return i == s.Length ? int.MaxValue : i;
		}	
	}
}
