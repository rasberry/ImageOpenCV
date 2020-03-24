using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageOpenCV
{
	public sealed class Params
	{
		public Params(string[] args)
		{
			Args = new List<string>(args);
		}

		List<string> Args;

		public string[] Remaining()
		{
			return Args.ToArray();
		}

		public bool Has(string @switch)
		{
			int i = Args.IndexOf(@switch);
			if (i != -1) {
				Args.RemoveAt(i);
			}
			return i != -1;
		}

		public bool Has(out string val, string def = null)
		{
			val = def;
			if (Args.Count <= 0) { return false; }
			val = Args[0];
			Args.RemoveAt(0);
			return true;
		}

		public bool Default<T>(string @switch,out T val,T def = default(T)) where T : IConvertible
		{
			val = def;
			int i = Args.IndexOf(@switch);
			if (i == -1) {
				return true;
			}
			if (i+1 >= Args.Count) {
				Tell.MissingArgument(@switch);
				return false;
			}
			if (!Aids.TryParse(Args[i+1],out val)) {
				Tell.CouldNotParse(@switch,Args[i+1]);
				return false;
			}
			Args.RemoveAt(i+1);
			Args.RemoveAt(i);
			return true;
		}

		public bool Default<T,U>(string @switch,out T tval, out U uval,
			T tdef = default(T), U udef = default(U))
			where T : IConvertible where U : IConvertible
		{
			tval = tdef;
			uval = udef;
			int i = Args.IndexOf(@switch);
			if (i == -1) {
				return true;
			}
			if (i+2 >= Args.Count) {
				Tell.MissingArgument(@switch);
				return false;
			}
			if (!Aids.TryParse(Args[i+1],out tval)) {
				Tell.CouldNotParse(@switch,Args[i+1]);
				return false;
			}
			if (!Aids.TryParse(Args[i+2],out uval)) {
				Tell.CouldNotParse(@switch,Args[i+2]);
				return false;
			}
			Args.RemoveAt(i+2);
			Args.RemoveAt(i+1);
			Args.RemoveAt(i);
			return true;
		}

		public bool Expect(out string val, string name)
		{
			if (!Has(out val) || String.IsNullOrWhiteSpace(val)) {
				Tell.MustProvideInput(name);
				return false;
			}
			return true;
		}
		
		public bool Expect(string @switch)
		{
			bool has = Has(@switch);
			if (!has) {
				Tell.MustProvideInput(@switch);
				return false;
			}
			return true;
		}

		public bool Expect<T>(string @switch, out T val) where T : IConvertible
		{
			bool has = Default(@switch,out val);
			if (!has) {
				Tell.MustProvideInput(@switch);
				return false;
			}
			return true;
		}

		public bool Expect<T,U>(string @switch, out T tval, out U uval)
			where T : IConvertible where U : IConvertible
		{
			bool has = Default(@switch,out tval,out uval);
			if (!has) {
				Tell.MustProvideInput(@switch);
				return false;
			}
			return true;
		}
	}
}