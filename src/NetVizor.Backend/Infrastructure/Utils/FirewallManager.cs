// using System;
// using System.Collections.Generic;
// using System.Linq;
// using NetFwTypeLib;
// using NetVizor.Infrastructure.Models;
//
// namespace NetVizor.Infrastructure.Utils
// {
//     public class FirewallManager
//     {
//         private readonly INetFwPolicy2 _firewallPolicy;
//
//         public FirewallManager()
//         {
//             Type policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2")!;
//             _firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(policyType)!;
//         }
//
//         /// <summary>
//         /// 获取所有防火墙规则
//         /// </summary>
//         public IEnumerable<FirewallRule> GetRules()
//         {
//             return _firewallPolicy.Rules
//                 .Cast<INetFwRule>()
//                 .Select(MapRuleToModel)
//                 .ToList();
//         }
//
//         /// <summary>
//         /// 添加一条新的防火墙规则
//         /// </summary>
//         public void AddRule(FirewallRule rule)
//         {
//             INetFwRule newRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule")!)!;
//             MapModelToRule(rule, newRule);
//             _firewallPolicy.Rules.Add(newRule);
//         }
//
//         /// <summary>
//         /// 删除一条防火墙规则
//         /// </summary>
//         public void DeleteRule(string ruleName)
//         {
//             _firewallPolicy.Rules.Remove(ruleName);
//         }
//
//         /// <summary>
//         /// 更新一条防火墙规则 (通过先删除后添加实现)
//         /// </summary>
//         public void UpdateRule(FirewallRule rule)
//         {
//             // COM API 不直接支持更新, 标准做法是删除旧的, 添加新的
//             DeleteRule(rule.Name);
//             AddRule(rule);
//         }
//         
//         // --- 私有辅助方法 ---
//
//         private FirewallRule MapRuleToModel(INetFwRule comRule)
//         {
//             return new FirewallRule
//             {
//                 Name = comRule.Name,
//                 Description = comRule.Description,
//                 GroupName = comRule.Grouping,
//                 ApplicationName = comRule.ApplicationName ?? "*",
//                 Protocol = comRule.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY ? "ANY" : comRule.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP ? "TCP" : "UDP",
//                 LocalPorts = comRule.LocalPorts,
//                 RemotePorts = comRule.RemotePorts,
//                 Direction = comRule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIRECTION_IN ? FirewallDirection.In : FirewallDirection.Out,
//                 Action = comRule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW ? FirewallAction.Allow : FirewallAction.Block,
//                 Enabled = comRule.Enabled
//             };
//         }
//
//         private void MapModelToRule(FirewallRule model, INetFwRule comRule)
//         {
//             comRule.Name = model.Name;
//             comRule.Description = model.Description;
//             comRule.Grouping = model.GroupName;
//             comRule.ApplicationName = model.ApplicationName == "*" ? null : model.ApplicationName;
//             comRule.Protocol = model.Protocol.ToUpper() switch
//             {
//                 "TCP" => (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP,
//                 "UDP" => (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP,
//                 _ => (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY
//             };
//             comRule.LocalPorts = model.LocalPorts;
//             comRule.RemotePorts = model.RemotePorts;
//             comRule.Direction = model.Direction == FirewallDirection.In ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIRECTION_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIRECTION_OUT;
//             comRule.Action = model.Action == FirewallAction.Allow ? NET_FW_ACTION_.NET_FW_ACTION_ALLOW : NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
//             comRule.Enabled = model.Enabled;
//         }
//     }
// }
