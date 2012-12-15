using System;
using System.Xml.Linq;

namespace Cadenza.Net
{
	static class XElementCoda
	{
		public static XElement GetElementPath (this XElement self, params XName[] path)
		{
			foreach (var p in path) {
				self = self.Element (p);
				if (self == null)
					return null;
			}
			return self;
		}

		public static string GetElementPathValue (this XElement self, params XName[] path)
		{
			var e = GetElementPath (self, path);
			return e == null ? null : e.Value;
		}
	}
}

