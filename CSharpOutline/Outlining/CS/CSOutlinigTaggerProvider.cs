using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;


namespace JSOutlining11.Outlining.CS
{
    [Export(typeof(ITaggerProvider))]
	[TagType(typeof(IOutliningRegionTag))]
	[ContentType("CSharp")]
    [ContentType("Razor.C#")]	

	internal sealed class CSOutliningTaggerProvider : ITaggerProvider
	{
		[Import]
		IClassifierAggregatorService classifierAggregator;
        
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			IClassifier classifier = classifierAggregator.GetClassifier(buffer);
			//var spans = c.GetClassificationSpans(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length));
			
            //create a single tagger for each buffer.
            int vsVersion = typeof(ITextBuffer).Assembly.GetName().Version.Major;

            var res = buffer.Properties.GetOrCreateSingletonProperty(
                () => vsVersion >= 14
                    ? new CSOutliningTagger14(buffer, classifier) as ITagger<T>
                    : new CSOutliningTagger11(buffer, classifier) as ITagger<T>                  
            );
            return res;
		} 
	}
}
