using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EasyNetQ.Hosepipe
{
    public class ArgParser
    {
        private readonly Regex argRegex = new Regex(@"([a-z]{1,2})\:(.*)");

        public Arguments Parse(string[] args)
        {
            var arguments = new Arguments();

            foreach (var arg in args)
            {
                var argument = ParseArgument(arg);
                arguments.Add(argument);
            }
            return arguments;
        }

        private Argument ParseArgument(string arg)
        {
            var match = argRegex.Match(arg);
            return match.Success
                       ? new Argument(match.Groups[2].Value, match.Groups[1].Value)
                       : new Argument(arg);
        }
    }

    public class Arguments
    {
        private readonly IList<Argument> arguments = new List<Argument>();
        private readonly IDictionary<string, Argument> keys = new Dictionary<string, Argument>();

        public void Add(Argument argument)
        {
            arguments.Add(argument);
            if (argument.HasKey)
            {
                keys.Add(argument.Key, argument);
            }
        }

        public TryResult At(int position, Action<Argument> argumentAction)
        {
            if (position < 0 || position >= arguments.Count) return TryResult.Fail();
            var argument = arguments[position];
            argumentAction(argument);
            return TryResult.Pass();
        }

        public TryResult WithKey(string key, Action<Argument> argumentAction)
        {
            if (!keys.ContainsKey(key)) return TryResult.Fail();
            var argument = keys[key];
            argumentAction(argument);
            return TryResult.Pass();
        }

        public TryResult WithTypedKeyOptional<T>(string key, Action<Argument> argumentAction) where T : IConvertible
        {
            if (!keys.ContainsKey(key)) return TryResult.Pass();

            try
            {
                Convert.ChangeType(keys[key].Value, typeof(T));
            }
            catch (InvalidCastException)
            {
                return TryResult.Fail();
            }

            var argument = keys[key];
            argumentAction(argument);
            return TryResult.Pass();
        }

        public TryResult At(int position, string command, Action argumentAction)
        {
            if (position < 0 || position >= arguments.Count) return TryResult.Fail();
            var argument = arguments[position];
            if (argument.Value != command) return TryResult.Fail();
            argumentAction();
            return TryResult.Pass();
        }
    }

    public class Argument
    {
        public Argument(string value, string key)
        {
            Value = value;
            Key = key;
            HasKey = true;
        }

        public Argument(string value)
        {
            Value = value;
            HasKey = false;
        }

        public string Value { get; private set; }
        public string Key { get; private set; }
        public bool HasKey { get; private set; }
    }

    public class TryResult
    {
        private bool pass;

        public static TryResult Pass()
        {
            return new TryResult { pass = true };
        }

        public static TryResult Fail()
        {
            return new TryResult { pass = false };
        }

        public void FailWith(Action action)
        {
            if (!pass) action();
        }
    }
}
