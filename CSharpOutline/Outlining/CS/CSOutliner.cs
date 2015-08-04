using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace JSOutlining11.Outlining.CS
{
	internal class CSOutliner: BaseOutliner
	{
		private static Regex RegionStartRegex = new Regex(@"^\/[\/\*]\s*#region\s(.+?)(\*\/)?$", RegexOptions.Compiled);
		private static Regex RegionEndRegex = new Regex(@"^\/[\/\*]\s*#endregion", RegexOptions.Compiled);

		private bool FunctionOccured;

		protected override TextRegion  ParseBuffer(SnapshotParser parser, TextRegion parent)
		{
			FunctionOccured = false;
			return base.ParseBuffer(parser, parent);
		}

		/// <summary>
		/// parses input buffer, searches for region start
		/// </summary>
		/// <param name="parser"></param>
		/// <returns>created region or null</returns>
		public override TextRegion TryCreateRegion(SnapshotParser parser)
		{
			ClassificationSpan span = parser.CurrentSpan;
            SnapshotPoint point = parser.CurrentPoint;
            if (span != null)
            {
                switch (span.ClassificationType.Classification)
                {
                    case "punctuation":
                        char c = point.GetChar();                        
                        switch (c)
                        {
                            case '{':
                                return new TextRegion(span.Span.Start, TextRegionType.Block);
                            case '[':
                                return new TextRegion(span.Span.Start, TextRegionType.Array);
                        }
                        break;
                    case "comment":
                        return ParseComment(parser, RegionStartRegex, RegionEndRegex);
                }
            }
			return null;
		}
        
		/// <summary>
		/// tries to close region
		/// </summary>
		/// <param name="parser">parser</param>
		/// <returns>whether region was closed</returns>
		protected override bool TryComplete(TextRegion r, SnapshotParser parser)
		{
			ClassificationSpan span = parser.CurrentSpan;
            SnapshotPoint point = parser.CurrentPoint;
            if (span != null)
            {
                string text = span.Span.GetText();
                if (span.ClassificationType.Classification == "punctuation")
                {
                    char c = point.GetChar();
                    //text can be "};", not just "}"
				    if (r.RegionType == TextRegionType.Block && c == '}'
					    || r.RegionType == TextRegionType.Array && c == ']')
				    {
					    r.EndPoint = span.Span.Start + 1;
				    }
                }            
				else if (span.ClassificationType.Classification == "comment" && r.RegionType == TextRegionType.Region)
				{					
					Match m = RegionEndRegex.Match(text);
					if (m.Success)
						r.EndPoint = span.Span.End;					
				}
			}
			return r.Complete;
		}

		protected override void ProcessCurrentToken(SnapshotParser p)
		{
			base.ProcessCurrentToken(p);
			ClassificationSpan span = p.CurrentSpan;
			//function keyword was found
			if (span != null && span.ClassificationType.Classification == "keyword" && span.Span.GetText() == "function")
			{
				FunctionOccured = true;
			}
		}

		protected override void OnRegionFound(TextRegion r)
		{
			base.OnRegionFound(r);
			if (r.RegionType == TextRegionType.Block && FunctionOccured)
			{
				r.RegionSubType = TextRegionSubType.Function;
			}
			FunctionOccured = false;
		}

		/// <summary>
		/// tries to merge sequential comments		
		/// </summary>
		/// <returns>true, if merged. In this case newRegion is not added to Children</returns>
		protected override bool TryMergeComments(TextRegion r, TextRegion newRegion)
		{
			if (r.Children.Count > 0)
			{
				TextRegion last = r.Children[r.Children.Count - 1];
				//merge conditions
				if (last.RegionType == TextRegionType.Comment
					&& newRegion.RegionType == TextRegionType.Comment
					&& newRegion.StartLine.LineNumber <= last.EndLine.LineNumber + 1
					&& string.IsNullOrWhiteSpace(new SnapshotSpan(last.EndPoint, newRegion.StartPoint).GetText()))
				{
					//instead of adding newRegion, we just move last child's end
					last.EndPoint = newRegion.EndPoint;
					return true;
				}
			}
			return false;
		}

	}
}