using System.Xml.Linq;

namespace Cadenza.Net {

	static class WebDavNames {

		public static readonly XName Collection             = XName.Get ("collection", "DAV:");
		public static readonly XName CreationDate           = XName.Get ("creationdate", "DAV:");
		public static readonly XName Error                  = XName.Get ("error", "DAV:");
		public static readonly XName GetContentLength       = XName.Get ("getcontentlength", "DAV:");
		public static readonly XName Href                   = XName.Get ("href", "DAV:");
		public static readonly XName Location               = XName.Get ("location", "DAV:");
		public static readonly XName Multistatus            = XName.Get ("multistatus", "DAV:");
		public static readonly XName Prop                   = XName.Get ("prop", "DAV:");
		public static readonly XName Propfind               = XName.Get ("propfind", "DAV:");
		public static readonly XName Propname               = XName.Get ("propname", "DAV:");
		public static readonly XName Propstat               = XName.Get ("propstat", "DAV:");
		public static readonly XName ResourceType           = XName.Get ("resourcetype", "DAV:");
		public static readonly XName Response               = XName.Get ("response", "DAV:");
		public static readonly XName Status                 = XName.Get ("status", "DAV:");
		public static readonly XName ResponseDescription    = XName.Get ("responsedescription", "DAV:");
	}
}

