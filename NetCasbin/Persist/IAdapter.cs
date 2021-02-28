﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Casbin.Persist
{
    public interface IAdapter
    {
        void LoadPolicy(IModel model);

        Task LoadPolicyAsync(IModel model);

        void SavePolicy(IModel model);

        Task SavePolicyAsync(IModel model);

        void AddPolicy(string section, string policyType, IList<string> rule);

        Task AddPolicyAsync(string section, string policyType, IList<string> rule);

        void AddPolicies(string section, string policyType, IEnumerable<IList<string>> rules);

        Task AddPoliciesAsync(string section, string policyType, IEnumerable<IList<string>> rules);

        void RemovePolicy(string section, string policyType, IList<string> rule);

        Task RemovePolicyAsync(string section, string policyType, IList<string> rule);

        void RemovePolicies(string section, string policyType, IEnumerable<IList<string>> rules);

        Task RemovePoliciesAsync(string section, string policyType, IEnumerable<IList<string>> rules);

        void RemoveFilteredPolicy(string section, string policyType, int fieldIndex, params string[] fieldValues);

        Task RemoveFilteredPolicyAsync(string section, string policyType, int fieldIndex, params string[] fieldValues);
    }
}
