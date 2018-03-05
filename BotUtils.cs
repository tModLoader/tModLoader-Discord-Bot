using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace tModloaderDiscordBot
{
	public static class BotUtils
	{
		// TODO refactor filewrits, mostly the same code

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

		public static byte[] UnicodeGetBytes(this string content) =>
			Encoding.Unicode.GetBytes(content);

		public static string UnicodeGetString(this byte[] buffer) =>
			Encoding.Unicode.GetString(buffer);

		public static string RemoveWhitespace(this string input) =>
			new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());

		private static readonly DateTime UnixEpoch =
			new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static long GetCurrentUnixTimestampMillis() =>
			(long)GetCurrentUnixTimespan().TotalMilliseconds;

		public static DateTime DateTimeFromUnixTimestampMillis(long millis) =>
			UnixEpoch.AddMilliseconds(millis);

		public static long GetCurrentUnixTimestampSeconds() =>
			(long)GetCurrentUnixTimespan().TotalSeconds;

		public static TimeSpan GetCurrentUnixTimespan() =>
			DateTime.UtcNow - UnixEpoch;

		public static DateTime DateTimeFromUnixTimestampSeconds(long seconds) =>
			UnixEpoch.AddSeconds(seconds);

		public static string ReplaceWhitespace(this string input, string replacement) =>
			input.Replace(" ", replacement);

		public static bool AreSorted<T>(IEnumerable<T> ids)
		{
			var enumerable = ids as T[] ?? ids.ToArray();
			return enumerable.SequenceEqual(enumerable.OrderBy(id => id));
		}

		public static bool AreUnique<T>(IEnumerable<T> ids)
		{
			var enumerable = ids as T[] ?? ids.ToArray();
			return enumerable.Distinct().Count() == enumerable.Count();
		}

		private static readonly Regex DiscordInviteRegex = new Regex(@"(?:discord(?:\.gg|.me|app\.com\/invite)\/(?<id>([\w]{16}|(?:[\w]+-?){3})))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public static bool IsDiscordInvite(this string str)
			=> DiscordInviteRegex.IsMatch(str);

		public static string FirstCharToUpper(this string input)
		{
			if (string.IsNullOrEmpty(input))
				throw new ArgumentNullException();
			return input.First().ToString().ToUpper() + input.Substring(1);
		}

		public static string PrettyPrint(this IEnumerable<string> list) =>
			string.Join(", ", list.Select(v => $"``{v}``"));

		public static string SurroundWith(this string text, string surrounder) =>
			$"{surrounder}{text}{surrounder}";

		public static string Cap(this string value, int length) =>
			value?.Substring(0, Math.Abs(Math.Min(value.Length, length)));

		public static bool Contains(this string source, string toCheck, StringComparison comp) =>
			source.IndexOf(toCheck, comp) >= 0;

		public static ConcurrentSet<T> ToConcurrentSet<T>(this IEnumerable<KeyValuePair<T, byte>> source)
			=> new ConcurrentSet<T>(source);

		public static ConcurrentSet<T> ToConcurrentSet<T>(this IEnumerable<T> source)
			=> new ConcurrentSet<T>(source);

		//public static ConcurrentBag<T> ToConcurrentBag<T>(this IEnumerable<T> source)
		//	=> new ConcurrentBag<T>(source);

		public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
			if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

			ConcurrentDictionary<TKey, TElement> d = new ConcurrentDictionary<TKey, TElement>(comparer);
			foreach (TSource element in source)
				d.TryAdd(keySelector(element), elementSelector(element));

			return d;
		}

		public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return ToConcurrentDictionary<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, null);
		}

		public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
		{
			return ToConcurrentDictionary<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
		}

		public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		{
			return ToConcurrentDictionary<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
		}

		internal class IdentityFunction<TElement>
		{
			public static Func<TElement, TElement> Instance
			{
				get { return x => x; }
			}
		}
		public static string FullName(this IUser user)
			=> $"{user.Username}#{user.Discriminator}";

		public static string PrettyPrintTimespan(TimeSpan span)
		{
			var strings = new List<string>();
			var numYears = 0;
			if (span.Days > 365)
			{
				numYears = span.Days / 365;
				strings.Add($"{numYears:D2} years");
			}

			if (span.Days > 0)
				strings.Add($"{(numYears > 0 ? span.Days - 365 * numYears : span.Days):D2} days");

			if (span.Hours > 0)
				strings.Add($"{span.Hours:D2} hours");

			if (span.Minutes > 0)
				strings.Add($"{span.Minutes:D2} minutes");

			if (span.Seconds > 0)
				strings.Add($"{span.Seconds:D2}{(span.Milliseconds > 0 ? $".{span.Milliseconds:D2}" : "")} seconds");

			return string.Join(", ", strings);
		}

		public static bool EqualsIgnoreCase(this string lhs, string rhs)
			=> lhs.Equals(rhs, StringComparison.InvariantCultureIgnoreCase);

		public static int LevenshteinDistance(this string s, string t)
		{
			var n = s.Length;
			var m = t.Length;
			var d = new int[n + 1, m + 1];

			// Step 1
			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			// Step 2
			for (var i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (var j = 0; j <= m; d[0, j] = j++)
			{
			}

			// Step 3
			for (var i = 1; i <= n; i++)
			{
				//Step 4
				for (var j = 1; j <= m; j++)
				{
					// Step 5
					var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

					// Step 6
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}

		/*
         *  The following recursive search had to be made for commands and modules,
         *  because none is present in the available search command
         *  (This could will recursively go through modules until the given command or module is found,
         *  or nothing left)
		 *
		 * TODO fix for multiple submodules
         */

		public static async Task<string> SearchCommand(CommandService commandService, ICommandContext commandContext, string command)
		{
			var sr = commandService.Search(commandContext, command);

			if (sr.IsSuccess)
				return sr.Text;

			var output = "";
			var found = FindDeepCommand(commandService.Modules, command, ref output);

			if (found)
				return output;

			await commandContext.Channel.SendMessageAsync($"`{command}` is not a known command or module");
			return null;
		}

		public static bool FindDeepCommand(IEnumerable<ModuleInfo> modules, string command, ref string output)
		{
			var module = command.StartsWith("module:");
			var text = module ? command.Split(":")[1].Trim() : command;

			foreach (var mod in modules)
			{
				var o = $"{output}{mod.Name} ";

				if ((module && (mod.Name.EqualsIgnoreCase(text) || mod.Aliases.Any(x => x.EqualsIgnoreCase(text))))
					|| (mod.Commands.Any(x => x.Name.EqualsIgnoreCase(text) || x.Aliases.Any(y => y.EqualsIgnoreCase(text)))))
				{
					output = module
						? $"module:{o}"
						: $"{o}{text}";
					output = output.TrimEnd();
					return true;
				}

				var b = false;
				if (mod.Submodules.Count > 0)
					b = FindDeepCommand(mod.Submodules, text, ref o);

				if (!b)
					continue;

				output = o;
				return true;
			}

			return false;
		}
	}
}
