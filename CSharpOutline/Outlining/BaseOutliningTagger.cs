using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace JSOutlining11.Outlining
{
	internal abstract class BaseOutliningTagger:ITagger<IOutliningRegionTag>, IDisposable
	{		
		private ITextBuffer Buffer;
		private ITextSnapshot Snapshot;
		private List<TextRegion> Regions = new List<TextRegion>();		
		protected IClassifier Classifier;
		protected DispatcherTimer UpdateTimer;
		public bool FirstOutlining { get; protected set; }
		public OutliningOptions Options { get; private set; }
		protected BaseOutliner Outliner;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public BaseOutliningTagger(ITextBuffer buffer, IClassifier classifier)
		{			
			Buffer = buffer;
			Snapshot = buffer.CurrentSnapshot;
			Classifier = classifier;
			Init();
		}

		protected virtual void Init()
		{
			Options = GetOptions();

			//timer that will trigger outlining update after some period of no buffer changes
			UpdateTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			UpdateTimer.Interval = TimeSpan.FromMilliseconds(2500);
			FirstOutlining = true;
			UpdateTimer.Tick += (sender, args) => {				
				UpdateTimer.Stop();
				Outline();
			};
            //bind to events and start outlining           
		}
        
		protected abstract OutliningOptions GetOptions();
		
		/// <summary>
		/// Gets nested outlining regions for buffer
		/// </summary>
		protected void Outline()
		{
			ITextSnapshot snapshot = Buffer.CurrentSnapshot;
            SnapshotParser parser = GetSnapshotParser(snapshot);
			//parsing snapshot
			TextRegion regionTree = Outliner.ParseBuffer(parser); 
						
			List<TextRegion> newRegions = GetRegionList(regionTree);

			List<Span> oldSpans = Regions.ConvertAll(r => r.AsSnapshotSpan().TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive).Span);
			List<Span> newSpans = newRegions.ConvertAll(r => r.AsSnapshotSpan().Span);

			NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
			NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

			//the changed regions are regions that appear in one set or the other, but not both.
			NormalizedSpanCollection removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

			int changeStart = int.MaxValue;
			int changeEnd = -1;

			if (removed.Count > 0)
			{
				changeStart = removed[0].Start;
				changeEnd = removed[removed.Count - 1].End;
			}

			if (newSpans.Count > 0)
			{
				changeStart = Math.Min(changeStart, newSpans[0].Start);
				changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
			}

			this.Snapshot = snapshot;
			this.Regions = newRegions;

			if (changeStart <= changeEnd && this.TagsChanged != null)
			{					
				this.TagsChanged(this, new SnapshotSpanEventArgs(
						new SnapshotSpan(this.Snapshot, Span.FromBounds(changeStart, changeEnd))));
			}
            FirstOutlining = false;
		}

        protected virtual SnapshotParser GetSnapshotParser(ITextSnapshot snapshot)
        {
            return new SnapshotParser(snapshot, Classifier);
        }

		/// <summary>
		/// converts region tree into flat list
		/// </summary>		
		protected virtual List<TextRegion> GetRegionList(TextRegion tree)
		{			
			List<TextRegion> res = new List<TextRegion>(tree.Children.Count);
			foreach (TextRegion r in tree.Children)
			{
				if (r.Complete && r.StartLine.LineNumber != r.EndLine.LineNumber)
					res.Add(r);
				if (r.Children.Count != 0)
					res.AddRange(GetRegionList(r));
			}
			//assigning tagger
			foreach (TextRegion r in res)
				r.Tagger = this;

			return res;
		}

		//Implement the GetTags method, which instantiates the tag spans. 
		//This example assumes that the spans in the NormalizedSpanCollection passed in to the method are contiguous, although this may not always be the case. 
		//This method instantiates a new tag span for each of the outlining regions.
		public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0)
				yield break;
			List<TextRegion> currentRegions = this.Regions;
			ITextSnapshot currentSnapshot = this.Snapshot;
			SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
			int startLineNumber = entire.Start.GetContainingLine().LineNumber;
			int endLineNumber = entire.End.GetContainingLine().LineNumber;
			foreach (TextRegion region in currentRegions)
			{
				if (region.StartLine.LineNumber <= endLineNumber && region.EndLine.LineNumber >= startLineNumber)
				{					
					yield return region.AsOutliningRegionTag();					
				}
			}
		}

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			// Need to stop the timer here to ensure an Outline isn't attempted after dispose as that crashes VS
			if (UpdateTimer.IsEnabled)
			{
				UpdateTimer.Stop(); 
			}
			//Buffer.Changed -= BufferChanged;
		}

		#endregion
	}
}
