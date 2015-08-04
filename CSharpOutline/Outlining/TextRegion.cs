using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace JSOutlining11.Outlining
{
	internal enum TextRegionType
	{ 
		None = 0,
		Block, // {}
		Array, // []
		Region, // #region #endregion
		Comment // multiline comment
	}

	internal enum TextRegionSubType
	{
		None = 0,
		Function
	}

	internal class TextRegion
	{
		
#region Props
		public SnapshotPoint StartPoint { get; set; }
		public SnapshotPoint EndPoint { get; set; }

		/// <summary>
		/// tagger which created a region
		/// </summary>
		public BaseOutliningTagger Tagger { get; set; }

		/// <summary>
		/// whether region has endpoint
		/// </summary>
		public bool Complete
		{
			get { return EndPoint.Snapshot != null; }
		}
		public ITextSnapshotLine StartLine { get { return StartPoint.GetContainingLine(); } }
		public ITextSnapshotLine EndLine { get { return EndPoint.GetContainingLine(); } }
		public TextRegionType RegionType { get; set; }
		public TextRegionSubType RegionSubType { get; set; }
		public string Name { get; set; }

		public TextRegion Parent { get; set; }
		public List<TextRegion> Children { get; set; }

		public string InnerText
		{
			get { return StartPoint.Snapshot.GetText(StartPoint.Position, EndPoint.Position - StartPoint.Position); }
		}

		/// <summary>
		/// text from first line start to region start
		/// </summary>
		public string TextBefore
		{
			get 
			{
				string text = StartLine.GetText();
				return text.Substring(0, Math.Min(StartPoint - StartLine.Start, text.Length)); 
			}
		}
#endregion

#region Constructors

		public TextRegion()
		{
			Children = new List<TextRegion>();
		}

		public TextRegion(SnapshotPoint startPoint, TextRegionType type)
			: this()
		{
			StartPoint = startPoint;
			RegionType = type;
		} 
#endregion

		public TagSpan<IOutliningRegionTag> AsOutliningRegionTag()
		{
			SnapshotSpan span = this.AsSnapshotSpan();
			OutliningOptions opt = this.Tagger.Options;

			//isImplementation means that block will collapse on "Collapse to definitions" command
			//my stupid parser collapses top-level blocks in document, regions and top-level blocks in each region
			//and also function bodies which it was able to detect
			bool isImplementation = 
				RegionSubType == TextRegionSubType.Function
				|| RegionType == TextRegionType.Region			
				|| Parent.RegionType == TextRegionType.None 
				|| Parent.RegionType == TextRegionType.Region;

			// collapsed when opening a file
			bool collapsed = false;
			if (Tagger.FirstOutlining)
			{
				collapsed = isImplementation && opt.AutoCollapseToDefinitions;
				if (!collapsed)
				{
					switch (RegionType)
					{
						case TextRegionType.Block:
						case TextRegionType.Array:
							collapsed = opt.AutoCollapseBraces;
							break;
						case TextRegionType.Comment:
							collapsed = opt.AutoCollapseComments;
							break;
						case TextRegionType.Region:
							collapsed = opt.AutoCollapseRegions;
							break;
					}
				}
			}

			return new TagSpan<IOutliningRegionTag>
						(
							span, 
							new OutliningRegionTag
							(
								collapsed, 
								isImplementation, 
								GetCollapsedText(), 
								new CollapsedHintFormatter(this).FormatHint()
							)
						);
		}

		public SnapshotSpan AsSnapshotSpan()
		{
			return new SnapshotSpan(this.StartPoint, this.EndPoint);
		}

		private string GetCollapsedText()
		{
			switch (RegionType)
			{ 
				case TextRegionType.Region: return Name;
				case TextRegionType.Comment: 
					return new SnapshotSpan(StartPoint, StartLine.EndIncludingLineBreak).GetText().TrimEnd() + " ...";
			}
			return "...";
		}
	}
}