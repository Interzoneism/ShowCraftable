using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class VSWebClient : HttpClient
{
	public delegate void PostCompleteHandler(CompletedArgs args);

	public static readonly VSWebClient Inst = new VSWebClient
	{
		Timeout = TimeSpan.FromSeconds(ClientSettings.WebRequestTimeout)
	};

	public void PostAsync(Uri uri, FormUrlEncodedContent postData, PostCompleteHandler onFinished)
	{
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				HttpResponseMessage res = await PostAsync(uri, postData);
				string response = await res.Content.ReadAsStringAsync();
				CompletedArgs args = new CompletedArgs
				{
					State = ((!res.IsSuccessStatusCode) ? CompletionState.Error : CompletionState.Good),
					StatusCode = (int)res.StatusCode,
					Response = response,
					ErrorMessage = res.ReasonPhrase
				};
				onFinished(args);
			}
			catch (Exception ex)
			{
				CompletedArgs args2 = new CompletedArgs
				{
					State = CompletionState.Error,
					ErrorMessage = ex.ToString()
				};
				onFinished(args2);
			}
		});
	}

	public string Post(Uri uri, FormUrlEncodedContent postData)
	{
		try
		{
			return PostAsync(uri, postData).Result.Content.ReadAsStringAsync().Result;
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	public async Task DownloadAsync(string requestUri, Stream destination, IProgress<Tuple<int, long>> progress = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		using HttpResponseMessage response = await GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		long? contentLength = response.Content.Headers.ContentLength;
		await using Stream download = await response.Content.ReadAsStreamAsync(cancellationToken);
		if (progress == null || !contentLength.HasValue)
		{
			await download.CopyToAsync(destination, cancellationToken);
			return;
		}
		Progress<int> progress2 = new Progress<int>(delegate(int totalBytes)
		{
			progress.Report(new Tuple<int, long>(totalBytes, contentLength.Value));
		});
		await download.CopyToAsync(destination, 81920, progress2, cancellationToken);
		download.Close();
	}
}
