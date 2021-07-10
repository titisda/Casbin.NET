﻿using System;
using System.Collections.Generic;
using System.Linq;
using Casbin.Rbac;
using Casbin.Util;
using Casbin.Extensions;

namespace Casbin.Model
{
    /// <summary>
    /// Represents an expression in a section of the model.
    /// For example: r = sub, obj, act
    /// </summary>
    public class Assertion
    {
        public string Key { get; internal set; }

        public string Value { get; internal set;  }

        public IReadOnlyDictionary<string, int> Tokens { get; internal set; }

        public IRoleManager RoleManager { get; internal set; }

        public List<IReadOnlyList<string>> Policy { get; internal set; }

        internal HashSet<string> PolicyStringSet { get; }

        public Assertion()
        {
            Policy = new List<IReadOnlyList<string>>();
            PolicyStringSet = new HashSet<string>();
            RoleManager = new DefaultRoleManager(10);
        }

        public void RefreshPolicyStringSet()
        {
            PolicyStringSet.Clear();
            foreach (var rule in Policy)
            {
                PolicyStringSet.Add(Utility.RuleToString(rule));
            }
        }

        internal void BuildIncrementalRoleLink(PolicyOperation policyOperation, IEnumerable<string> rule)
        {
            int count = Value.Count(c => c is '_');
            if (count < 2)
            {
                throw new InvalidOperationException("the number of \"_\" in role definition should be at least 2.");
            }

            BuildRoleLink(count, policyOperation, rule);
        }

        internal void BuildIncrementalRoleLinks(PolicyOperation policyOperation, IEnumerable<IEnumerable<string>> rules)
        {
            int count = Value.Count(c => c is '_');
            if (count < 2)
            {
                throw new InvalidOperationException("the number of \"_\" in role definition should be at least 2.");
            }

            foreach (var rule in rules)
            {
                BuildRoleLink(count, policyOperation, rule);
            }
        }

        public void BuildRoleLinks()
        {
            int count = Value.Count(c => c is '_');
            if (count < 2)
            {
                throw new InvalidOperationException("the number of \"_\" in role definition should be at least 2.");
            }

            foreach (IEnumerable<string> rule in Policy)
            {
                BuildRoleLink(count, PolicyOperation.PolicyAdd, rule);
            }
        }

        private void BuildRoleLink(int groupPolicyCount,
            PolicyOperation policyOperation, IEnumerable<string> rule)
        {
            var roleManager = RoleManager;
            List<string> ruleEnum = rule as List<string> ?? rule.ToList();
            int ruleCount = ruleEnum.Count;

            if (ruleCount < groupPolicyCount)
            {
                throw new InvalidOperationException("Grouping policy elements do not meet role definition.");
            }

            if (ruleCount > groupPolicyCount)
            {
                ruleEnum = ruleEnum.GetRange(0, groupPolicyCount);
            }

            switch (policyOperation)
            {
                case PolicyOperation.PolicyAdd:
                    switch (groupPolicyCount)
                    {
                        case 2:
                            roleManager.AddLink(ruleEnum[0], ruleEnum[1]);
                            break;
                        case 3:
                            roleManager.AddLink(ruleEnum[0], ruleEnum[1], ruleEnum[2]);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(groupPolicyCount), groupPolicyCount, null);
                    }
                    break;
                case PolicyOperation.PolicyRemove:
                    switch (groupPolicyCount)
                    {
                        case 2:
                            roleManager.DeleteLink(ruleEnum[0], ruleEnum[1]);
                            break;
                        case 3:
                            roleManager.DeleteLink(ruleEnum[0], ruleEnum[1], ruleEnum[2]);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(groupPolicyCount), groupPolicyCount, null);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(policyOperation), policyOperation, null);
            }
        }

        internal bool Contains(IEnumerable<string> rule)
        {
            return PolicyStringSet.Contains(Utility.RuleToString(rule));
        }

        internal bool TryAddPolicy(IEnumerable<string> rule)
        {
            var ruleList = rule as IReadOnlyList<string> ?? rule.ToArray();
            if (Contains(ruleList))
            {
                return false;
            }

            if (TryGetPriorityIndex(out int index))
            {
                return TryAddPolicyByPriority(ruleList, index);
            }

            Policy.Add(ruleList);
            PolicyStringSet.Add(Utility.RuleToString(ruleList));
            return true;
        }

        internal bool TryRemovePolicy(IEnumerable<string> rule)
        {
            var ruleList = rule as IReadOnlyList<string> ?? rule.ToArray();
            if (Contains(ruleList) is false)
            {
                return false;
            }
            for (int i = 0; i < Policy.Count; i++)
            {
                var ruleInPolicy = Policy[i];
                if (ruleList.DeepEquals(ruleInPolicy) is false)
                {
                    continue;
                }
                Policy.RemoveAt(i);
                PolicyStringSet.Remove(Utility.RuleToString(ruleList));
                break;
            }
            return true;
        }

        internal void ClearPolicy()
        {
            Policy.Clear();
            PolicyStringSet.Clear();
        }

        private bool TryAddPolicyByPriority(IReadOnlyList<string> rule, int priorityIndex)
        {
            if (int.TryParse(rule[priorityIndex], out int priority) is false)
            {
                return false;
            }

            bool LastLessOrEqualPriority(IReadOnlyList<string> p)
            {
                return int.Parse(p[priorityIndex]) <= priority;
            }

            int lastIndex = Policy.FindLastIndex(LastLessOrEqualPriority);
            Policy.Insert(lastIndex + 1, rule);
            PolicyStringSet.Add(Utility.RuleToString(rule));
            return true;
        }

        private bool TryGetPriorityIndex(out int index)
        {
            if (Tokens is null)
            {
                index = -1;
                return false;
            }
            return Tokens.TryGetValue($"{Key}_priority", out index);
        }

        internal bool TrySortPoliciesByPriority()
        {
            if (TryGetPriorityIndex(out int priorityIndex) is false)
            {
                return false;
            }

            int PolicyComparison(IReadOnlyList<string> p1, IReadOnlyList<string> p2)
            {
                string priorityString1 = p1[priorityIndex];
                string priorityString2 = p2[priorityIndex];

                if (int.TryParse(priorityString1, out int priority1) is false
                    || int.TryParse(priorityString2, out int priority2) is false)
                {
                    return string.CompareOrdinal(priorityString1, priorityString2);
                }

                return priority1 - priority2;
            }

            Policy.Sort(PolicyComparison);
            return true;
        }
    }
}
