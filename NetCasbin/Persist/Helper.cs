﻿using System.Linq;
using Casbin.Model;

namespace Casbin.Persist
{
    public class Helper
    {
        public delegate void LoadPolicyLineHandler<T, TU>(T t, TU u);

        public static void LoadPolicyLine(string line, IModel model)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            if (line[0] == '#')
            {
                return;
            }

            var tokens = line.Split(',').Select(x => x.Trim()).ToArray();

            string key = tokens[0];
            string sec = key.Substring(0, 1);

            if (model.Sections.ContainsKey(sec))
            {
                var item = model.Sections[sec];
                var policy = item[key];
                if (policy == null)
                {
                    return;
                }

                var content = tokens.Skip(1).ToList();
                if (!model.HasPolicy(sec, key, content))
                {
                    policy.TryAddPolicy(content);
                }
            }
        }


    }
}
