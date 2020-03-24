using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageOpenCV
{
	class Program
	{
		static void AddEnvironmentPaths(IEnumerable<string> paths)
		{
			var path = new[] { Environment.GetEnvironmentVariable("PATH") ?? string.Empty };
			string newPath = string.Join(Path.PathSeparator.ToString(), path.Concat(paths));
			Environment.SetEnvironmentVariable("PATH", newPath);
		}

		public static void Main(string[] args)
		{
			AddEnvironmentPaths(new string[] {
				AppContext.BaseDirectory,
				Path.Combine(AppContext.BaseDirectory,"x64")
			});

			if (args.Length < 1) {
				Options.Usage();
				return;
			}

			//parse initial options - determines which method to do
			if (!Options.Parse(args, out var pruned)) {
				return;
			}

			//map / parse method specific arguments
			IMain func = Registry.Map(Options.Method);
			// Log.Debug($"M = {Options.Method} F = {func != null} AL = {pruned.Length}");
			if (func == null || !func.ParseArgs(pruned)) {
				return;
			}

			//kick off method
			try {
				func.Main();
			}
			catch(Exception e) {
				#if DEBUG
				Log.Error(e.ToString());
				#else
				Log.Error(e.Message);
				#endif
			}
		}
	}
}
