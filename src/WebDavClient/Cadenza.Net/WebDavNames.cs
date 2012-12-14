using System.Xml.Linq;

namespace Cadenza.Net {

	static class WebDavNames {

		public static readonly XName Multistatus    = XName.Get ("multistatus", "DAV:");
		public static readonly XName Response       = XName.Get ("response", "DAV:");
		public static readonly XName Href           = XName.Get ("href", "DAV:");
		public static readonly XName Propfind       = XName.Get ("propfind", "DAV:");
		public static readonly XName Propname       = XName.Get ("propname", "DAV:");
	}
}

