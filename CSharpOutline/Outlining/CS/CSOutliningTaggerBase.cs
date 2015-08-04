using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;


namespace JSOutlining11.Outlining.CS
{
	internal abstract class CSOutliningTaggerBase: BaseOutliningTagger
	{
		protected CSOutliningTaggerBase(ITextBuffer buffer, IClassifier classifier)
			: base(buffer, classifier)
		{            
		}

		protected override void Init()
		{
			Outliner = new CSOutliner();
			base.Init();
		}

		protected override OutliningOptions GetOptions()
		{
			return new OutliningOptions
			{
				TabSize = 4,
		        AutoCollapseRegions = true
			};			
		}        

        protected override SnapshotParser GetSnapshotParser(ITextSnapshot snapshot)
        {
            return new CSSnapshotParser(snapshot, Classifier);
        }
	}
}
