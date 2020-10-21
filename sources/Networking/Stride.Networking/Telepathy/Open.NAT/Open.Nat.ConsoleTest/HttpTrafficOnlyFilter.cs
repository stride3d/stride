using System.Diagnostics;

namespace Open.Nat.ConsoleTest
{
	public class HttpTrafficOnlyFilter : TraceFilter 
	{
		public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
		{
			if(source == "System.Net" && eventType == TraceEventType.Verbose )
			{
				if (formatOrMessage.Contains("<<") || formatOrMessage.Contains("//"))
				{
					return true;
				}
				return false;
			}
			if (source == "System.Net" && eventType == TraceEventType.Information)
			{
				if (formatOrMessage.Contains("Request:") || formatOrMessage.Contains("Sending headers") ||
					formatOrMessage.Contains("Received status line:") || formatOrMessage.Contains("Received headers"))
				{
					return true;
				}
				return false;
				
			}
			return true;
		}
	}
}
