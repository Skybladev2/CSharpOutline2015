using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace JSOutlining11.Outlining
{
	internal abstract class BaseOutliner
	{
		/// <summary>
		/// parses input buffer, searches for region start
		/// </summary>
		/// <param name="parser"></param>
		/// <returns>created region or null</returns>
		public abstract TextRegion TryCreateRegion(SnapshotParser parser);
		
		protected TextRegion ParseComment(SnapshotParser parser, Regex regionStartRegex, Regex regionEndRegex)
		{
			SnapshotPoint point = parser.CurrentPoint;
			ClassificationSpan span = parser.CurrentSpan;
			Match m = regionStartRegex.Match(span.Span.GetText());
			if (m.Success)
			{
				return new TextRegion(point, TextRegionType.Region)
				{
					Name = m.Groups[1].Value
				};
			}
			if (!regionEndRegex.IsMatch(span.Span.GetText()))
			{
				return new TextRegion(point, TextRegionType.Comment)
					    {
					       	EndPoint = span.Span.End
					    };
			}
			return null;
		}

		/// <summary>
		/// tries to close region
		/// </summary>
		/// <param name="parser">parser</param>
		/// <returns>whether region was closed</returns>
		protected abstract bool TryComplete(TextRegion r, SnapshotParser parser);
		
		/// <summary>
		/// parser the text buffer
		/// </summary>
		/// <param name="parser">buffer parser</param>
		/// <returns>text region tree</returns>
		public TextRegion ParseBuffer(SnapshotParser parser)
		{
			TextRegion regionTree = new TextRegion();		
			while (ParseBuffer(parser, regionTree) != null);
			return regionTree;
		}

		/// <summary>
		/// parses buffer
		/// </summary>
		/// <param name="parser"></param>
		/// <param name="parent">parent region or null</param>
		/// <returns>a region with its children or null</returns>
		protected virtual TextRegion ParseBuffer(SnapshotParser parser, TextRegion parent)
		{
			for (; !parser.AtEnd(); parser.MoveNext())
			{
				ProcessCurrentToken(parser);
				TextRegion r = TryCreateRegion(parser);

				if (r != null)
				{
					//found the start of the region
					OnRegionFound(r);
					r.Parent = parent;
					parser.MoveNext();
					if (!r.Complete)
					{
						//searching for child regions						
						while (ParseBuffer(parser, r) != null) ;
					}
					//adding to children or merging with last child
					if (!TryMergeComments(parent, r))
					{
						parent.Children.Add(r);
						ExtendStartPoint(r);
						
					}
					return r;
				}
				//found parent's end - terminating parsing
				if (TryComplete(parent, parser))
				{
					parser.MoveNext();
					return null;
				}
			}
			return null;
		}

		/// <summary>
		/// A function that looks for special tokens in source code
		/// </summary>
		protected virtual void ProcessCurrentToken(SnapshotParser p)
		{

		}

		protected virtual void OnRegionFound(TextRegion r)
		{

		}

		/// <summary>
		/// Tries to move region start point up to get C#-like outlining
		/// 
		/// for (var k in obj)
		/// { -- from here
		/// 
		/// for (var k in obj) -- to here
		/// {
		/// </summary>
		private void ExtendStartPoint(TextRegion r)
		{
			//some are not extended
			if (r.RegionType == TextRegionType.Region
				|| r.RegionType == TextRegionType.Comment
				|| !r.Complete
				|| r.StartLine.LineNumber == r.EndLine.LineNumber
				|| !string.IsNullOrWhiteSpace(r.TextBefore)) return;

			//how much can we move region start
			int upperLimit = 0;
			if (r.Parent != null)
			{
				int childPosition = r.Parent.Children.IndexOf(r);

				if (childPosition == -1)
					childPosition = r.Parent.Children.Count;
				if (childPosition == 0)
				{
					//this region is first child of its parent
					//we can go until the parent's start
					upperLimit = r.Parent.RegionType != TextRegionType.None ? r.Parent.StartLine.LineNumber + 1 : 0;
				}
				else
				{
					//there is previous child
					//we can go until its end
					TextRegion prevRegion = r.Parent.Children[childPosition - 1];
					upperLimit = prevRegion.EndLine.LineNumber + (prevRegion.EndLine.LineNumber == prevRegion.StartLine.LineNumber ? 0 : 1);
				}
			}

			//now looking up to calculated upper limit for non-empty line
			for (int i = r.StartLine.LineNumber - 1; i >= upperLimit; i--)
			{
				ITextSnapshotLine line = r.StartPoint.Snapshot.GetLineFromLineNumber(i);
				if (!string.IsNullOrWhiteSpace(line.GetText()))
				{
					//found such line, placing region start at its end
					r.StartPoint = line.End;
					return;
				}
			}
		}

		/// <summary>
		/// tries to merge sequential comments		
		/// </summary>
		/// <returns>true, if merged. In this case newRegion is not added to Children</returns>
		protected abstract bool TryMergeComments(TextRegion r, TextRegion newRegion);

	}
}