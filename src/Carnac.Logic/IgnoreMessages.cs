using Carnac.Logic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Carnac.Logic
{
    class IgnoreMessages
    {
        HashSet<HashSet<string>> m_keys = new HashSet<HashSet<string>>();

        public void Add(string key)
        {
            m_keys.Add(new HashSet<string>() { key });
        }

        public void Add(IEnumerable<string> keys)
        {
            m_keys.Add(new HashSet<string>(keys));
        }

        public static IgnoreMessages FromYml(string text)
        {
            IgnoreMessages ignore = new IgnoreMessages();
            Deserializer deserializer = new Deserializer();
            string[] separator = new string[] { "+" };
            ignore.m_keys = new HashSet<HashSet<string>>(deserializer.Deserialize<List<string>>(text)
                .Select(s => new HashSet<string>(s.Split(separator, StringSplitOptions.RemoveEmptyEntries))));
            return ignore;
        }

        bool Match(string s)
        {
            return m_keys.Any(k => k.Count() == 1 && k.Contains(s));
        }

        public bool MatchAnyKey(Message m)
        {
            return m.Keys.Any(k => k.Input.Any(i => Match(i)));
        }

        public bool MatchAllKeys(Message m)
        {
            return m_keys.Any(keys => m.Keys.Any(k => k.Input.Count() == keys.Count() && k.Input.All(i => keys.Contains(i))));
        }
    }
}
