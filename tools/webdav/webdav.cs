using System;
using System.Net;
using System.Collections.Generic;

using Mono.Options;
using Mono.Terminal;

namespace Cadenza.Tools.WebDav
{
	class App
	{
		public static void Main (string[] args)
		{
			var c = new WebDavConnection ();

			bool show_help = false;
			bool show_version = false;
			var o = new OptionSet {
				"Usage: webdav [OPTIONS]",
				"",
				"Simple WebDav command-line client to excercise WebDavClient.",
				"",
				"Options:",
				{ "server=",
				  "Set name of WebDAV {SERVER} to connect to.",
				  v => c.Builder.Server = new Uri (v) },
				{ "user=",
				  "Set {USERNAME} on WebDAV server to connect to.",
				  v => (c.Builder.NetworkCredential ?? (c.Builder.NetworkCredential = new NetworkCredential ())).UserName = v },
				{ "pass=",
				  "Set {PASSWORD} on WebDAV server to connect to.",
				  v => (c.Builder.NetworkCredential ?? (c.Builder.NetworkCredential = new NetworkCredential ())).Password = v },
				{ "v",
				  "Show verbose communication information.",
				  v => c.Builder.Log = Console.Out },
				{ "version",
				  "Show version information and exit.",
				  v => show_version = v != null },
				{ "help|h|?",
				  "Show this message and exit.",
				  v => show_help = v != null },
			};

			try {
				o.Parse (args);
			} catch (Exception ex) {
				Console.Error.WriteLine ("webdav: {0}", ex.Message);
			}

			if (show_version) {
				Console.WriteLine ("webdav 0.1");
				return;
			}
			if (show_help) {
				o.WriteOptionDescriptions (Console.Out);
				return;
			}

			LineEditor e = new LineEditor ("webdav");
			string s;
			
			while ((s = e.Edit ("webdav> ", "")) != null) {
				if ((s = s.Trim ()).Length == 0)
					continue;

				var p = s.IndexOf (' ');
				var m = p < 0 ? s : s.Substring (0, p);
				var a = WebDavConnection.GetCommand (m);
				if (a == null) {
					Console.Error.WriteLine ("webdav: Invalid command: {0}", s);
					continue;
				}
				while (p > 0 && p < s.Length && char.IsWhiteSpace (s, p))
					++p;
				s = p >= 0 && p < s.Length ? s.Substring (p) : "";
				a (c, s);
			}
		}
	}
}
