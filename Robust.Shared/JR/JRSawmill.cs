using Robust.Shared.Log;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Shared.JR
{
	public class JRLogHandler: ILogHandler
	{
        public JRLogHandler()
        {
            SlayerTK.JRCon.Host();
        }

		public void Log(string msg, LogEvent message)
        {
            SlayerTK.JRCon.Send(string.Format("[\a{0}\a-] {1}",
                JRColorGetter.GetLogLevel(message.Level.ToRobust()),
                message.RenderMessage()
                ));
        }
	}
}
