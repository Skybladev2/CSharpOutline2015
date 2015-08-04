using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace JSOutlining11.Outlining.CS
{
    /// <summary>
    /// sequential parser for ITextSnapshot
    /// </summary>
    internal class CSSnapshotParser: SnapshotParser
	{
		public CSSnapshotParser(ITextSnapshot snapshot, IClassifier classifier): base(snapshot, classifier)
		{
			
		}

		/// <summary>
		/// Moves forward by one char or one classification span
		/// </summary>
		/// <returns>true, if moved</returns>
		public override  bool MoveNext()
		{
			if (!AtEnd())
			{
                //operators are processed char by char, because the classifier can merge several operators into one span (like "]]", "[]")
				CurrentPoint = CurrentSpan != null && CurrentSpan.ClassificationType.Classification != "punctuation" ? CurrentSpan.Span.End : CurrentPoint + 1;

                if (SpanIndex.ContainsKey(CurrentPoint.Position))
                {
                    CurrentSpan = SpanIndex[CurrentPoint.Position];
                }
                else
                {
                    if (CurrentSpan != null && CurrentPoint.Position >= CurrentSpan.Span.End.Position) //we're out of current span
                        CurrentSpan = null;
                }
				return true;
			}
			return false;
		}        	
	}
}
