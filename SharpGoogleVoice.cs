/*

Copyright (C) 2012 Alex Yumashev
Jitbit Sofwtare
http://www.jitbit.com/
https://bitbucket.org/jitbit/sharpgooglevoice/

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Jitbit.Utils
{
	class SharpGoogleVoice
	{
		private CookieWebClient _webClient;

		private string rnrse;

		private const string USERAGENT = "Mozilla/5.0 (iPhone; U; CPU iPhone OS 2_2_1 like Mac OS X; en-us) AppleWebKit/525.18.1 (KHTML, like Gecko) Version/3.1.1 Mobile/5H11 Safari/525.20";

		public void Login(string username, string password)
		{
			_webClient = new CookieWebClient();
			_webClient.Headers.Add("User-agent", USERAGENT); //mobile user agent to save bandwidth (google will serve mobile version of the page)

			//get "GALX" value from google login page
			string response = Encoding.ASCII.GetString(_webClient.DownloadData("https://accounts.google.com/ServiceLogin?service=grandcentral"));
			Regex galxRegex = new Regex(@"name=""GALX"".*?value=""(.*?)""", RegexOptions.Singleline);
			string galx = galxRegex.Match(response).Groups[1].Value;

			//sending login info
			_webClient.Headers.Add("Content-type", "application/x-www-form-urlencoded;charset=utf-8");
			_webClient.Headers.Add("User-agent", USERAGENT); //mobile user agent to save bandwidth (google will serve mobile version of the page)
			byte[] responseArray = _webClient.UploadData(
				"https://accounts.google.com/ServiceLogin?service=grandcentral",
				"POST",
				PostParameters(new Dictionary<string, string>
				               	{
				               		{"Email", username},
				               		{"Passwd", password},
				               		{"GALX", galx}
				               	}));
			response = Encoding.ASCII.GetString(responseArray);
		}

		private static byte[] PostParameters(IDictionary<string, string> parameters)
		{
			string paramStr = "";

			foreach (string key in parameters.Keys)
			{
				paramStr += key + "=" + HttpUtility.UrlEncode(parameters[key]) + "&";
			}

			return Encoding.ASCII.GetBytes(paramStr);
		}

		/// <summary>
		/// Gets google's "session id" field value
		/// </summary>
		private void GetRNRSE()
		{
			//get goovle voice "homepage" (mobile version - to save bandwidth)
			string response = Encoding.ASCII.GetString(_webClient.DownloadData("https://www.google.com/voice/m"));

			//find the hidden field
			Regex rnrRegex = new Regex(@"<input.*?name=""_rnr_se"".*?value=""(.*?)""");
			if (rnrRegex.IsMatch(response))
			{
				rnrse = rnrRegex.Match(response).Groups[1].Value;
			}
			else
				throw new Exception("'RNRSE' FIELD NOT FOUND ON THE PAGE! Something is wrong.");
		}

		public void SendSMS(string number, string text)
		{
			GetRNRSE();

			byte[] parameters = PostParameters(new Dictionary<string, string>
			                                   	{
			                                   		{"phoneNumber", number},
			                                   		{"text", text},
			                                   		{"_rnr_se", rnrse}
			                                   	});

			_webClient.Headers.Add("Content-type", "application/x-www-form-urlencoded;charset=utf-8");
			byte[] responseArray = _webClient.UploadData("https://www.google.com/voice/sms/send", "POST", parameters);
			string response = Encoding.ASCII.GetString(responseArray);
		}
	}

	internal class CookieWebClient : System.Net.WebClient
	{
		private CookieContainer _cookieContainer = new CookieContainer();
		public CookieContainer CookieContainer
		{
			get { return _cookieContainer; }
			set { _cookieContainer = value; }
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest request = base.GetWebRequest(address);
			if (request is HttpWebRequest)
			{
				(request as HttpWebRequest).CookieContainer = _cookieContainer;
			}
			return request;
		}
	}
}
