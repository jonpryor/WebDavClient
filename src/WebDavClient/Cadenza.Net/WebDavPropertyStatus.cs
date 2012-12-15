using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace Cadenza.Net
{
	
	public class WebDavPropertyStatus {
		
		public WebDavPropertyStatus (XElement element)
		{
			if (element == null)
				throw new ArgumentNullException ("element");
			
			Element     = element;
			
			Properties  = element.Element (WebDavNames.Prop).Elements ();

			Status  = element.GetElementPathValue (WebDavNames.Status);
			Error   = element.GetElementPath (WebDavNames.Error);
			if (Error != null)
				Error = Error.Elements ().FirstOrDefault ();
			ResponseDescription = element.GetElementPathValue (WebDavNames.ResponseDescription);
		}
		
		public XElement                 Element             {get; private set;}
		public IEnumerable<XElement>    Properties          {get; private set;}
		public string                   Status              {get; private set;}
		public HttpStatusCode?          StatusCode          {get {return WebDavResponse.GetStatusCode (Status);}}
		public XElement                 Error               {get; private set;}
		public string                   ResponseDescription {get; private set;}
	}
}

