using System;
using System.IO;

namespace Cadenza.Net {

	public class WebDavDownloadMethod : WebDavMethod {

		public Stream   DownloadedContents  {get; private set;}

		internal WebDavDownloadMethod (Stream downloadedContents)
		{
			if (downloadedContents == null)
				downloadedContents = new MemoryStream ();
			this.DownloadedContents = downloadedContents;
		}

		protected override void OnResponse (Stream response)
		{
			response.CopyTo (DownloadedContents);
		}
	}
}

