using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOutline
{
    class Log
    {
        [Import]
        private static SVsServiceProvider ServiceProvider = null;

        public static void Write(string message)
        {

            IVsActivityLog log = ServiceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            if (log == null) return;
            int hr = log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,  "C# outline 2015", message);
        }
    }
}
