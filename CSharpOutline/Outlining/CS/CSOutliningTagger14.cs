using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace JSOutlining11.Outlining.CS
{
    /// <summary>
    /// outlining tagger for VS2015
    /// </summary>
    internal class CSOutliningTagger14: CSOutliningTaggerBase
    {
        public CSOutliningTagger14(ITextBuffer buffer, IClassifier classifier)
			: base(buffer, classifier)
		{            
		}

        protected override void Init()
        {
            base.Init();            

            Classifier.ClassificationChanged += (sender, args) => {
                if (FirstOutlining) {
                    Outline();
                } else {
                    //restart the timer
                    UpdateTimer.Stop();
                    UpdateTimer.Start();
                }
            };
        }

        protected override List<TextRegion> GetRegionList(TextRegion tree)
        {
            //VS 2015 outlines blocks and arrays on its own            
            return base.GetRegionList(tree).FindAll(r => r.RegionType != TextRegionType.Array);
        }
    }
}
