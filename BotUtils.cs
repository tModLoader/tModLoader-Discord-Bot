using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace dtMLBot
{
	public static class BotUtils
	{
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
