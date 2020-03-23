using System;
using System.Collections.Generic;
using System.Text;

namespace ImageOpenCV
{
	public static class Options
	{
		public static void Usage(PickMethod method = PickMethod.None)
		{
			StringBuilder sb = new StringBuilder();
			string name = nameof(ImageOpenCV);
			sb
				.WL()
				.WL(0,$"Usage {name} (method) [options]")
				.WL(0,"Options:")
				.WL(1,"-h / --help","Show full help")
				.WL(1,"(method) -h","Method specific help")
				.WL(1,"--methods"  ,"List possible methods")
			;
			
			if (ShowFullHelp)
			{
				foreach(PickMethod a in Aids.EnumAll<PickMethod>()) {
					IMain func = Registry.Map(a);
					func?.Usage(sb);
				}
			}
			else if (method != PickMethod.None)
			{
				IMain func = Registry.Map(method);
				func?.Usage(sb);
			}
			else
			{
				if (ShowHelpMethods) {
					sb
						.WL()
						.WL(0,"Methods:")
						.PrintEnum<PickMethod>(1)
					;
				}
			}

			Log.Message(sb.ToString());
		}

		public static bool Parse(string[] args, out string[] prunedArgs)
		{
			prunedArgs = null;
			var pArgs = new List<string>();

			int len = args.Length;
			for(int a=0; a<len; a++) {
				string curr = args[a];
				if (curr == "-h" || curr == "--help") {
					if (Method == PickMethod.None) {
						ShowFullHelp = true;
					}
					else {
						ShowHelpMethods = true;
					}
				}
				else if (curr == "--methods") {
					ShowHelpMethods = true;
				}
				else if (Method == PickMethod.None) {
					PickMethod which;
					if (!Aids.TryParse<PickMethod>(curr,out which)) {
						Tell.UnknownMethod(curr);
						return false;
					}
					Method = which;
				}
				else {
					pArgs.Add(curr);
				}
			}

			if (ShowFullHelp || ShowHelpMethods) {
				Usage(Method);
				return false;
			}

			if (Method == PickMethod.None) {
				Tell.MethodNotSpecified();
				return false;
			}

			prunedArgs = pArgs.ToArray();
			return true;
		}

		public static bool ShowFullHelp = false;
		public static bool ShowHelpMethods = false;
		public static PickMethod Method = PickMethod.None;

	}
}