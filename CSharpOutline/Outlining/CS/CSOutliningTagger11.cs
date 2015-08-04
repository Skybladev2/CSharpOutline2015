using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace JSOutlining11.Outlining.CS
{
    /// <summary>
    /// outlining tagger for VS2012 and 2013
    /// </summary>
    internal class CSOutliningTagger11: CSOutliningTaggerBase
    {
        public CSOutliningTagger11(ITextBuffer buffer, IClassifier classifier)
			: base(buffer, classifier)
		{            
		}

        protected override void Init()
        {
            base.Init();
            Outline();

            Classifier.ClassificationChanged += (sender, args) => {                
                //restart the timer
                UpdateTimer.Stop();
                UpdateTimer.Start();                
            };
        }

        protected override List<TextRegion> GetRegionList(TextRegion tree)
        {
            //Visual Studio outlines functions itself, let's not conflict with it
            return base.GetRegionList(tree).FindAll(r => r.RegionSubType != TextRegionSubType.Function);
        }
    }
}
