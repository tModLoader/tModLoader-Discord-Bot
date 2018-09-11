using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace tModloaderDiscordBot.Utils
{
    public class FileUtils
    {
	    public static async Task<string> FileReadToEndAsync(SemaphoreSlim semaphore, string filePath)
	    {
		    string buffer;

		    await semaphore.WaitAsync();

		    try
		    {
			    using (var stream = File.Open(filePath, FileMode.Open))
			    using (var reader = new StreamReader(stream))
				    buffer = await reader.ReadToEndAsync();
		    }
		    finally
		    {
			    semaphore.Release();
		    }

		    return buffer;
	    }

	    public static async Task FileWriteAsync(SemaphoreSlim semaphore, string path, string content)
	    {
		    await semaphore.WaitAsync();

		    try
		    {
			    using (var stream = File.Open(path, FileMode.Create))
			    using (var writer = new StreamWriter(stream))
				    await writer.WriteAsync(content);
			}
		    finally
		    {
			    semaphore.Release();
		    }
	    }

	    public static async Task FileWriteLineAsync(SemaphoreSlim semaphore, string path, string content)
	    {
		    await semaphore.WaitAsync();

		    try
		    {
			    using (var stream = File.Open(path, FileMode.Create))
			    using (var writer = new StreamWriter(stream))
				    await writer.WriteLineAsync(content);
		    }
		    finally
		    {
			    semaphore.Release();
		    }
	    }

	    public static async Task FileAppendAsync(SemaphoreSlim semaphore, string path, string content)
	    {
		    await semaphore.WaitAsync();

		    try
		    {
			    using (var stream = File.Open(path, FileMode.Append))
			    using (var writer = new StreamWriter(stream))
				    await writer.WriteAsync(content);
		    }
		    finally
		    {
			    semaphore.Release();
		    }
	    }

	    public static async Task FileAppendLineAsync(SemaphoreSlim semaphore, string path, string content)
	    {
		    await semaphore.WaitAsync();

		    try
		    {
			    using (var stream = File.Open(path, FileMode.Append))
			    using (var writer = new StreamWriter(stream))
				    await writer.WriteLineAsync(content);
		    }
		    finally
		    {
			    semaphore.Release();
		    }
	    }
	}
}
