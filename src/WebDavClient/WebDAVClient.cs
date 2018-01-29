﻿/*
* (C) 2010 Kees van den Broek: kvdb@kvdb.net
*          D-centralize: d-centralize.nl
*          
* Latest van den Broek version and examples on: http://kvdb.net/projects/webdav
* 
* Feel free to use this code however you like.
* http://creativecommons.org/license/zero/
* 
* Copyright (C) 2012 Xamarin Inc. (http://xamarin.com)
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Cadenza.Net
{
    public enum WebDavEntryType {
	    Default,
	    Directory,
	    File,
    }

    public class WebDavEntry {

	    public string Directory {get; internal set;}
	    public string Name {get; internal set;}
	    public string Path {get; internal set;}
        public string AbsoluteUri { get; internal set; }
        public WebDavEntryType Type {get; internal set;}
        public long? ContentLength { get; internal set; }

	    internal WebDavEntry ()
	    {
	    }

	    public override string ToString ()
	    {
		    return Path;
	    }
    }

    public class WebDavClient : IDisposable
    {

        //XXX: submit along with state object.
        HttpWebRequest httpWebRequest;

        #region WebDAV connection parameters
        /// <summary>
        /// Request builder to handle authentication, proxy and so on
        /// </summary>
        public HttpRequestBuilder requestBuilder { get; set; }

        private String server;
        /// <summary>
        /// Specify the WebDAV hostname (required).
        /// </summary>
        public String Server
        {
            get { return server; }
            set
            {
                value = value.TrimEnd('/');
                server = value;
            }
        }
        private String basePath = "/";
        /// <summary>
        /// Specify the path of a WebDAV directory to use as 'root' (default: /)
        /// </summary>
        public String BasePath
        {
            get { return basePath; }
            set
            {
                value = value.Trim('/');
                basePath = "/" + value + "/";
            }
        }
        private int? port = null;
        /// <summary>
        /// Specify an port (default: null = auto-detect)
        /// </summary>
        public int? Port
        {
            get { return port; }
            set { port = value; }
        }

        Uri getServerUrl(String path, Boolean appendTrailingSlash)
        {
            String completePath = basePath;
            if (path != null)
            {
                completePath += path.Trim('/');
            }

            if (appendTrailingSlash && completePath.EndsWith("/") == false) { completePath += '/'; }

            if(port.HasValue) {
			    return new Uri(server + ":" + port + completePath);
            }
            else {
                return new Uri(server + completePath);
            }
            
        }
        #endregion

        #region WebDAV operations
        /// <summary>
        /// List files in the root directory
        /// </summary>
        public Task<IEnumerable<WebDavEntry>> List()
        {
            // Set default depth to 1. This would prevent recursion (default is infinity).
            return List("/", 1);
        }

        /// <summary>
        /// List files in the given directory
        /// </summary>
        /// <param name="path"></param>
	    public Task<IEnumerable<WebDavEntry>> List(String path)
        {
            // Set default depth to 1. This would prevent recursion.
            return List(path, 1);
        }

        /// <summary>
        /// List all files present on the server.
        /// </summary>
        /// <param name="remoteFilePath">List only files in this path</param>
        /// <param name="depth">Recursion depth</param>
        /// <returns>A list of files (entries without a trailing slash) and directories (entries with a trailing slash)</returns>
	    public Task<IEnumerable<WebDavEntry>> List(String remoteFilePath, int? depth)
        {
            // Uri should end with a trailing slash
            Uri listUri = getServerUrl(remoteFilePath, true);

            // http://webdav.org/specs/rfc4918.html#METHOD_PROPFIND
            StringBuilder propfind = new StringBuilder();
            propfind.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            propfind.Append("<propfind xmlns=\"DAV:\">");
            propfind.Append("  <propname/>");
            propfind.Append("</propfind>");

            // Depth header: http://webdav.org/specs/rfc4918.html#rfc.section.9.1.4
            IDictionary<string, string> headers = new Dictionary<string, string>();
            if (depth != null)
            {
                headers.Add("Depth", depth.ToString());
            }

		    return WebDavOperation(listUri, "PROPFIND", headers, Encoding.UTF8.GetBytes(propfind.ToString()), null, FinishList, remoteFilePath);
        }

        private String NormalizePath(String path)
        {
            return path.Trim('/');
        }

        IEnumerable<WebDavEntry> FinishList(IAsyncResult result)
        {
            string remoteFilePath = NormalizePath((string)result.AsyncState);

            using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
            {
                using (Stream stream = response.GetResponseStream())
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(stream);
                    XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(xml.NameTable);
                    xmlNsManager.AddNamespace("d", "DAV:");

                    foreach (XmlNode node in xml.DocumentElement.ChildNodes)
                    {
                        XmlNode xmlNode = node.SelectSingleNode("d:href", xmlNsManager);
                        string filepath = Uri.UnescapeDataString(xmlNode.InnerXml);
                        string uri = filepath;
					    if (filepath.StartsWith (basePath))
						    filepath = filepath.Substring (basePath.Length);

                        // skip the "query" node
					    if (filepath.Length == 0 || NormalizePath(filepath) == remoteFilePath)
						    continue;
					    var type = filepath.EndsWith ("/") ? WebDavEntryType.Directory : WebDavEntryType.File;
					    int endDir = filepath.LastIndexOf ('/');
					    if (type == WebDavEntryType.Directory)
						    endDir = filepath.LastIndexOf ("/", endDir - 1);
					    endDir++;

                        long? contentLength = null;

                        XmlNode propStatNode = node.SelectSingleNode("d:propstat", xmlNsManager);
                        if(propStatNode != null)
                        {
                            XmlNode propNode = propStatNode.SelectSingleNode("d:prop", xmlNsManager);
                            if (propNode != null)
                            {
                                // get content length
                                XmlNode prop = propNode.SelectSingleNode("d:getcontentlength", xmlNsManager);
                                if (prop != null)
                                {
                                    contentLength = long.Parse(prop.InnerText);
                                }

                                // get content type
                                prop = propNode.SelectSingleNode("d:resourcetype", xmlNsManager);
                                if (prop != null)
                                {
                                    if (prop.SelectSingleNode("d:collection", xmlNsManager) != null)
                                        type = WebDavEntryType.Directory;
                                    else
                                        type = WebDavEntryType.File;
                                }
                            }
                        }
                        // get name and get rid of trailing /
                        var name = filepath.Substring(endDir).Trim('/');

                        yield return new WebDavEntry {
                            Directory = filepath.Substring(0, endDir),
                            Name = name,
                            Path = filepath,
                            Type = type,
                            ContentLength = contentLength,
                            AbsoluteUri = uri
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Upload a file to the server
        /// </summary>
        /// <param name="localFilePath">Local path and filename of the file to upload</param>
        /// <param name="remoteFilePath">Destination path and filename of the file on the server</param>
	    public Task<HttpStatusCode> Upload(String localFilePath, String remoteFilePath)
        {
            return Upload(localFilePath, remoteFilePath, null);
        }

        /// <summary>
        /// Upload a file to the server
        /// </summary>
        /// <param name="localFilePath">Local path and filename of the file to upload</param>
        /// <param name="remoteFilePath">Destination path and filename of the file on the server</param>
        /// <param name="state">Object to pass along with the callback</param>
        public Task<HttpStatusCode> Upload(String localFilePath, String remoteFilePath, object state)
        {
            FileInfo fileInfo = new FileInfo(localFilePath);
            long fileSize = fileInfo.Length;

            Uri uploadUri = getServerUrl(remoteFilePath, false);
            string method = WebRequestMethods.Http.Put.ToString();

		    return WebDavOperation(uploadUri, method, null, null, localFilePath, FinishUpload, state);
        }


        HttpStatusCode FinishUpload(IAsyncResult result)
        {
            using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
            {
                return response.StatusCode;
            }
        }


        /// <summary>
        /// Download a file from the server
        /// </summary>
        /// <param name="remoteFilePath">Source path and filename of the file on the server</param>
        /// <param name="localFilePath">Destination path and filename of the file to download on the local filesystem</param>
        public Task<HttpStatusCode> Download(String remoteFilePath, String localFilePath)
        {
            // Should not have a trailing slash.
            Uri downloadUri = getServerUrl(remoteFilePath, false);
            string method = WebRequestMethods.Http.Get.ToString();

		    return WebDavOperation(downloadUri, method, null, null, null, FinishDownload, localFilePath);
        }


        HttpStatusCode FinishDownload(IAsyncResult result)
        {
            string localFilePath = (string)result.AsyncState;

            using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
            {
                int contentLength = int.Parse(response.GetResponseHeader("Content-Length"));
			    int read = 0;
                using (Stream s = response.GetResponseStream())
                {
                    using (FileStream fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] content = new byte[4096];
                        int bytesRead = 0;
                        do
                        {
                            bytesRead = s.Read(content, 0, content.Length);
                            fs.Write(content, 0, bytesRead);
						    read += bytesRead;
                        } while (bytesRead > 0);
                    }
                }
			    if (contentLength != read)
				    Console.WriteLine ("Length read doesn't match header! Content-Length={0}; Read={1}", contentLength, read);
			    return response.StatusCode;
            }
        }


        /// <summary>
        /// Create a directory on the server
        /// </summary>
        /// <param name="remotePath">Destination path of the directory on the server</param>
        public Task<HttpStatusCode> CreateDir(string remotePath)
        {
            // Should not have a trailing slash.
            Uri dirUri = getServerUrl(remotePath, false);

            string method = WebRequestMethods.Http.MkCol.ToString();

            return WebDavOperation (dirUri, method, null, null, null, FinishCreateDir, null);
        }

        public Task<Boolean> Exists(string remotePath)
        {
            string method = WebRequestMethods.Http.Head.ToString();
            Uri dirUri = getServerUrl(remotePath, false);
            return WebDavOperation(dirUri, method, null, null, null, (result) =>
            {
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
                    {
                        return response.StatusCode == HttpStatusCode.OK;
                    }
                }
                catch (WebException wex)
                {
                    var resp = (HttpWebResponse)wex.Response;
                    if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                        return false;
                    throw wex;
                }
            }, null);
        }

        HttpStatusCode FinishCreateDir(IAsyncResult result)
        {
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
                {
                    return response.StatusCode;
                }
            } catch (WebException wex)
            {
                var webresponse = ((WebException)wex).Response as HttpWebResponse;
                return webresponse.StatusCode;
            }
        }


        /// <summary>
        /// Delete a file on the server
        /// </summary>
        /// <param name="remoteFilePath"></param>
        public Task<HttpStatusCode> Delete(string remoteFilePath)
        {
            Uri delUri = getServerUrl(remoteFilePath, remoteFilePath.EndsWith("/"));

		    return WebDavOperation(delUri, "DELETE", null, null, null, FinishDelete, null);
        }


        HttpStatusCode FinishDelete(IAsyncResult result)
        {
            using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.EndGetResponse(result))
            {
                return response.StatusCode;
            }
        }
        #endregion

        #region Server communication

        /// <summary>
        /// This class stores the request state of the request.
        /// </summary>
        class RequestState
        {
            public WebRequest request;
            // The request either contains actual content...
            public byte[] content;
            // ...or a reference to the file to be added as content.
            public string uploadFilePath;
        }

        Task<TResult> WebDavOperation<TResult>(Uri uri, string requestMethod, IDictionary<string, string> headers, byte[] content, string uploadFilePath, Func<IAsyncResult, TResult> callback, object state)
        {
            return WebDavOperation(uri, requestMethod, headers, content, null, uploadFilePath, callback, state);
        }


        /// <summary>
        /// Perform the WebDAV call and fire the callback when finished.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="requestMethod"></param>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <param name="uploadFilePath"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        Task<TResult> WebDavOperation<TResult>(Uri uri, string requestMethod, IDictionary<string, string> headers, byte[] content, String contentType, string uploadFilePath, Func<IAsyncResult, TResult> callback, object state)
        {
            httpWebRequest = requestBuilder.Build(uri);
			
            /*
            * The following line fixes an authentication problem explained here:
            * http://www.devnewsgroups.net/dotnetframework/t9525-http-protocol-violation-long.aspx
            */
            System.Net.ServicePointManager.Expect100Continue = false;

            httpWebRequest.Method = requestMethod;

            // Need to send along headers?
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    httpWebRequest.Headers.Set(key, headers[key]);
                }
            }

            // Need to send along content?
            if (content != null || uploadFilePath != null)
            {
                RequestState asyncState = new RequestState();
                asyncState.request = httpWebRequest;

                if (content != null)
                {
                    // The request either contains actual content...
                    httpWebRequest.ContentLength = content.Length;
                    asyncState.content = content;
                    if(contentType != null)
                        httpWebRequest.ContentType = "text/xml";
                }
                else
                {
                    // ...or a reference to the file to be added as content.
                    httpWebRequest.ContentLength = new FileInfo(uploadFilePath).Length;
                    asyncState.uploadFilePath = uploadFilePath;
                }

                // Perform asynchronous request.
			    return Task.Factory.FromAsync (asyncState.request.BeginGetRequestStream, ReadCallback, asyncState)
				    .ContinueWith (t => {
					    if (t.IsFaulted)
						    throw t.Exception;
					    return Task<TResult>.Factory.FromAsync (httpWebRequest.BeginGetResponse, callback, state).Result;
				    });
            }
            else
            {
                // Begin async communications
			    return Task<TResult>.Factory.FromAsync (httpWebRequest.BeginGetResponse, callback, state);
            }
        }

        /// <summary>
        /// Submit data asynchronously
        /// </summary>
        /// <param name="result"></param>
        private void ReadCallback(IAsyncResult result)
        {
            RequestState state = (RequestState)result.AsyncState;
            WebRequest request = state.request;

            // End the Asynchronus request.
            using (Stream streamResponse = request.EndGetRequestStream(result))
            {
                // Submit content
                if (state.content != null)
                {
                    streamResponse.Write(state.content, 0, state.content.Length);
                }
                else
                {
                    using (FileStream fs = new FileStream(state.uploadFilePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] content = new byte[4096];
                        int bytesRead = 0;
                        do
                        {
                            bytesRead = fs.Read(content, 0, content.Length);
                            streamResponse.Write(content, 0, bytesRead);
                        } while (bytesRead > 0);

                        //XXX: perform upload status callback
                    }
                }
            }
        }

        public void Dispose()
        {
            if (httpWebRequest != null)
            {
                httpWebRequest.Abort();
            }
        }
        #endregion
    }
}
