using Robust.Shared.Log;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Shared.JR
{
	public static class JRColorGetter
	{
        private const string JRBgBlack = "000000";
        private const string JRFgRed = "800000";
        private const string JRFgGreen = "008000";
        private const string JRFgYellow = "808000";
        private const string JRFgBlue = "000080";
        private const string JRFgMagenta = "800080";
        private const string JRFgCyan = "008080";
        private const string JRFgLightGray = "c0c0c0";
        private const string JRFgDarkGray = "808080";
        private const string JRFgBrightRed = "ff0000";
        private const string JRFgBrightGreen = "00ff00";
        private const string JRFgBrightYellow = "ffff00";
        private const string JRFgBrightBlue = "0000ff";
        private const string JRFgBrightMagenta = "ff00ff";
        private const string JRFgBrightCyan = "00ffff";
        private const string JRFgWhite = "ffffff";

        public static string GetLogLevel(LogLevel level)
		{
            return level switch
            {
                LogLevel.Verbose => JRFgGreen + LogMessage.LogNameVerbose,
                LogLevel.Debug => JRFgBlue + LogMessage.LogNameDebug,
                LogLevel.Info => JRFgBrightCyan + LogMessage.LogNameInfo,
                LogLevel.Warning => JRFgBrightYellow + LogMessage.LogNameWarning,
                LogLevel.Error => JRFgBrightRed + LogMessage.LogNameError,
                LogLevel.Fatal => JRFgBrightMagenta + LogMessage.LogNameFatal,
                _ => JRFgWhite + LogMessage.LogNameUnknown
            };
	}
}
}
