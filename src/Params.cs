using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageOpenCV
{
	public enum Result {
		Missing = 0,
		Invalid = 1,
		Good = 2
	}

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

		// check for existance of a single parameter
		public Result Has(string @switch)
		{
			int i = Args.IndexOf(@switch);
			if (i != -1) {
				Args.RemoveAt(i);
			}
			return i != -1 ? Result.Good : Result.Missing;
		}

		// check for a non-qualified (leftover) parameter
		public Result Has(out string val, string def = null)
		{
			val = def;
			if (Args.Count <= 0) { return Result.Missing; }
			val = Args[0];
			Args.RemoveAt(0);
			return Result.Good;
		}

		//find or default a parameter with one argument
		public Result Default<T>(string @switch,out T val,T def = default(T)) where T : IConvertible
		{
			val = def;
			int i = Args.IndexOf(@switch);
			if (i == -1) {
				return Result.Missing;
			}
			if (i+1 >= Args.Count) {
				Tell.MissingArgument(@switch);
				return Result.Invalid;
			}
			if (!Aids.TryParse(Args[i+1],out val)) {
				Tell.CouldNotParse(@switch,Args[i+1]);
				return Result.Invalid;
			}
			Args.RemoveAt(i+1);
			Args.RemoveAt(i);
			return Result.Good;
		}

		//find or default a parameter with two arguments
		public Result Default<T,U>(string @switch,out T tval, out U uval,
			T tdef = default(T), U udef = default(U))
			where T : IConvertible where U : IConvertible
		{
			tval = tdef;
			uval = udef;
			int i = Args.IndexOf(@switch);
			if (i == -1) {
				return Result.Missing;
			}
			if (i+2 >= Args.Count) {
				Tell.MissingArgument(@switch);
				return Result.Invalid;
			}
			if (!Aids.TryParse(Args[i+1],out tval)) {
				Tell.CouldNotParse(@switch,Args[i+1]);
				return Result.Invalid;
			}
			if (!Aids.TryParse(Args[i+2],out uval)) {
				Tell.CouldNotParse(@switch,Args[i+2]);
				return Result.Invalid;
			}
			Args.RemoveAt(i+2);
			Args.RemoveAt(i+1);
			Args.RemoveAt(i);
			return Result.Good;
		}

		public Result Expect(out string val, string name)
		{
			if (Result.Good != Has(out val) || String.IsNullOrWhiteSpace(val)) {
				Tell.MustProvideInput(name);
				return Result.Invalid;
			}
			return Result.Good;
		}

		public Result Expect(string @switch)
		{
			var has = Has(@switch);
			if (Result.Good != has) {
				if (has == Result.Missing) { Tell.MustProvideInput(@switch); }
				return Result.Invalid;
			}
			return Result.Good;
		}

		public Result Expect<T>(string @switch, out T val) where T : IConvertible
		{
			var has = Default(@switch,out val);
			if (Result.Good != has) {
				if (has == Result.Missing) { Tell.MustProvideInput(@switch); }
				return Result.Invalid;
			}
			return Result.Good;
		}

		public Result Expect<T,U>(string @switch, out T tval, out U uval)
			where T : IConvertible where U : IConvertible
		{
			var has = Default(@switch,out tval,out uval);
			if (Result.Good != has) {
				if (has == Result.Missing) { Tell.MustProvideInput(@switch); }
				return Result.Invalid;
			}
			return Result.Good;
		}
	}

	public static class ParamsExtensions
	{
		public static bool IsGood(this Result r)
		{
			return r == Result.Good;
		}

		public static bool IsBad(this Result r)
		{
			return r != Result.Good;
		}

		public static bool IsInvalid(this Result r)
		{
			return r == Result.Invalid;
		}
	}
}