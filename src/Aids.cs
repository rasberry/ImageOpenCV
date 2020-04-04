using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageOpenCV
{
	public static class Aids
	{
		public static string MethodName(PickMethod m)
		{
			return $"{(int)m}. {m}";
		}

		public static string GetOutputName(PickMethod m)
		{
			return $"{m}-{DateTime.Now.ToString("yyyyMMdd-hhmmss")}.png";
		}

		public static IEnumerable<T> EnumAll<T>(bool includeZero = false)
			where T : struct
		{
			foreach(T a in Enum.GetValues(typeof(T))) {
				int v = (int)((object)a);
				if (!includeZero && v == 0) { continue; }
				yield return a;
			};
		}

		public static void PrintEnum<T>(this StringBuilder sb, int level, bool nested = false, Func<T,string> descriptionMap = null,
			Func<T,string> nameMap = null) where T : struct
		{
			var allEnums = EnumAll<T>().ToList();
			int numLen = 1 + (int)Math.Floor(Math.Log10(allEnums.Count));
			foreach(T e in allEnums) {
				int inum = (int)((object)e);
				string pnum = inum.ToString();
				string npad = pnum.Length < numLen ? new string(' ',numLen - pnum.Length) : "";
				if (nested) { npad = " "+npad; }
				string pname = nameMap == null ? e.ToString() : nameMap(e);
				string ppad = new string(' ',(nested ? 24 : 26) - pname.Length);
				string pdsc = descriptionMap == null ? "" : descriptionMap(e);
				sb.WL(level,$"{npad}{pnum}. {pname}{ppad}{pdsc}");
			}
		}

		public static bool TryParse<V>(string sub, out V val) where V : IConvertible
		{
			val = default(V);
			TypeCode tc = val.GetTypeCode();
			Type t = typeof(V);

			if (t.IsEnum) {
				if (Enum.TryParse(t,sub,true,out object o)) {
					val = (V)o;
					return Enum.IsDefined(t,o);
				}
				return false;
			}

			switch(tc)
			{
			case TypeCode.Double: {
				if (double.TryParse(sub,out double b)) {
					val = (V)((object)b); return true;
				} break;
			}
			case TypeCode.Int32: {
				if (int.TryParse(sub,out int b)) {
					val = (V)((object)b); return true;
				} break;
			}
			//add others as needed
			}
			return false;
		}

		const int ColumnOffset = 30;
		public static StringBuilder WL(this StringBuilder sb, int level, string def, string desc)
		{
			int pad = level;
			return sb
				.Append(' ',pad)
				.Append(def)
				.Append(' ',ColumnOffset - def.Length - pad)
				.AppendWrap(ColumnOffset,desc);
		}

		public static StringBuilder WL(this StringBuilder sb, int level, string def)
		{
			int pad = level;
			return sb
				.Append(' ',pad)
				.AppendWrap(pad,def);
		}

		public static StringBuilder WL(this StringBuilder sb, string s = null)
		{
			return s == null ? sb.AppendLine() : sb.AppendLine(s);
		}

		public static StringBuilder AppendWrap(this StringBuilder self, int offset, string m)
		{
			int w = Console.IsOutputRedirected
				? int.MaxValue
				: Console.BufferWidth - 1 - offset
			;
			int c = 0;
			int l = m.Length;

			while(c < l) {
				//need spacing after first line
				string o = c > 0 ? new string(' ',offset) : "";
				//this is the last iteration
				if (c + w >= l) {
					string s = m.Substring(c);
					c += w;
					self.Append(o).AppendLine(s);
				}
				//we're in the middle
				else {
					string s = m.Substring(c,w);
					c += w;
					self.Append(o).AppendLine(s);
				}
			}
			//StringBuilder likes to chain
			return self;
		}

		//public static int IndexOf<T>(this T[] arr,T item)
		//{
		//	if (arr == null || arr.Length < 1) { return -1; }
		//
		//	int found = -1;
		//	for(int i=0; i<arr.Length; i++) {
		//		if (arr[i].Equals(item)) { found = i; }
		//	}
		//	return found;
		//}
	}
}