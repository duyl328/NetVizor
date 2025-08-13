using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Net.HttpConn;
using Common.Net.Models;
using Utils.Firewall;

namespace Shell.Controllers;

public class FirewallController : BaseController
{
    public async Task GetRulesAsync(HttpContext context)
    {
        try
        {
            var firewallApi = new WindowsFirewallApi();

            var startIndex = int.Parse(GetQueryParam(context, "start", "0"));
            var limit = int.Parse(GetQueryParam(context, "limit", "50"));

            var filter = new RuleFilter();
            if (!string.IsNullOrEmpty(GetQueryParam(context, "name"))) 
                filter.NamePattern = GetQueryParam(context, "name");
            if (!string.IsNullOrEmpty(GetQueryParam(context, "direction")) &&
                Enum.TryParse<RuleDirection>(GetQueryParam(context, "direction"), true, out var direction))
                filter.Direction = direction;
            if (!string.IsNullOrEmpty(GetQueryParam(context, "enabled")) &&
                bool.TryParse(GetQueryParam(context, "enabled"), out var enabled))
                filter.Enabled = enabled;
            if (!string.IsNullOrEmpty(GetQueryParam(context, "protocol")) &&
                Enum.TryParse<ProtocolType>(GetQueryParam(context, "protocol"), true, out var protocol))
                filter.Protocol = protocol;
            if (!string.IsNullOrEmpty(GetQueryParam(context, "action")) &&
                Enum.TryParse<RuleAction>(GetQueryParam(context, "action"), true, out var action))
                filter.Action = action;
            if (!string.IsNullOrEmpty(GetQueryParam(context, "application")))
                filter.ApplicationName = GetQueryParam(context, "application");
            if (!string.IsNullOrEmpty(GetQueryParam(context, "port"))) 
                filter.Port = GetQueryParam(context, "port");
            if (!string.IsNullOrEmpty(GetQueryParam(context, "search"))) 
                filter.SearchKeyword = GetQueryParam(context, "search");

            var allRules = firewallApi.GetRulesByFilter(filter);
            var totalCount = allRules.Count;
            var pagedRules = allRules.Skip(startIndex).Take(limit).ToList();

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = new
                {
                    rules = pagedRules,
                    totalCount = totalCount,
                    startIndex = startIndex,
                    limit = limit,
                    hasMore = startIndex + limit < totalCount
                },
                Message = "查询成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"查询防火墙规则失败: {ex.Message}", 500);
        }
    }

    public async Task CreateRuleAsync(HttpContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(context.RequestBody))
            {
                await WriteErrorResponseAsync(context, "请求体不能为空");
                return;
            }

            var createRequest = ParseRequestBody<CreateRuleRequest>(context.RequestBody);
            if (createRequest == null)
            {
                await WriteErrorResponseAsync(context, "请求数据格式错误");
                return;
            }

            var firewallApi = new WindowsFirewallApi();
            var success = firewallApi.CreateRule(createRequest);

            if (success)
            {
                await WriteJsonResponseAsync(context, new ResponseModel<object>
                {
                    Success = true,
                    Message = "防火墙规则创建成功"
                });
            }
            else
            {
                await WriteErrorResponseAsync(context, "防火墙规则创建失败", 500);
            }
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"创建防火墙规则失败: {ex.Message}", 500);
        }
    }

    public async Task UpdateRuleAsync(HttpContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(context.RequestBody))
            {
                await WriteErrorResponseAsync(context, "请求体不能为空");
                return;
            }

            var updateRequest = ParseRequestBody<UpdateRuleRequest>(context.RequestBody);
            if (updateRequest == null || string.IsNullOrEmpty(updateRequest.CurrentName))
            {
                await WriteErrorResponseAsync(context, "请求数据格式错误或缺少规则名称");
                return;
            }

            var firewallApi = new WindowsFirewallApi();
            var success = firewallApi.UpdateRule(updateRequest);

            if (success)
            {
                await WriteJsonResponseAsync(context, new ResponseModel<object>
                {
                    Success = true,
                    Message = "防火墙规则更新成功"
                });
            }
            else
            {
                await WriteErrorResponseAsync(context, "防火墙规则更新失败，可能规则不存在", 500);
            }
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"更新防火墙规则失败: {ex.Message}", 500);
        }
    }

    public async Task DeleteRuleAsync(HttpContext context)
    {
        try
        {
            var ruleName = GetQueryParam(context, "name");
            if (string.IsNullOrEmpty(ruleName))
            {
                await WriteErrorResponseAsync(context, "缺少规则名称参数");
                return;
            }

            var firewallApi = new WindowsFirewallApi();
            var success = firewallApi.DeleteRule(ruleName);

            if (success)
            {
                await WriteJsonResponseAsync(context, new ResponseModel<object>
                {
                    Success = true,
                    Message = "防火墙规则删除成功"
                });
            }
            else
            {
                await WriteErrorResponseAsync(context, "防火墙规则删除失败，可能规则不存在", 500);
            }
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"删除防火墙规则失败: {ex.Message}", 500);
        }
    }

    public async Task GetStatusAsync(HttpContext context)
    {
        try
        {
            var firewallApi = new WindowsFirewallApi();
            var status = firewallApi.GetFirewallStatus();

            await WriteJsonResponseAsync(context, new ResponseModel<FirewallStatus>
            {
                Success = true,
                Data = status,
                Message = "获取防火墙状态成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取防火墙状态失败: {ex.Message}", 500);
        }
    }

    public async Task GetStatisticsAsync(HttpContext context)
    {
        try
        {
            var firewallApi = new WindowsFirewallApi();
            var statistics = firewallApi.GetStatistics();

            await WriteJsonResponseAsync(context, new ResponseModel<FirewallStatistics>
            {
                Success = true,
                Data = statistics,
                Message = "获取防火墙统计信息成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取防火墙统计信息失败: {ex.Message}", 500);
        }
    }

    public async Task SwitchFirewallAsync(HttpContext context)
    {
        try
        {
            var enabledParam = GetQueryParam(context, "enabled");
            var profileParam = GetQueryParam(context, "profile");

            if (string.IsNullOrEmpty(enabledParam))
            {
                await WriteErrorResponseAsync(context, "缺少必需参数 'enabled'，请使用 true 或 false");
                return;
            }

            var trim = enabledParam.Trim();
            bool enabled = trim switch
            {
                "true" or "TRUE" or "True" or "1" => true,
                "false" or "FALSE" or "False" or "0" => false,
                _ => throw new ArgumentException("enabled参数值无效，请使用 true/false 或 1/0")
            };

            FirewallProfile profile = string.IsNullOrEmpty(profileParam) ? FirewallProfile.All : profileParam switch
            {
                "domain" => FirewallProfile.Domain,
                "private" => FirewallProfile.Private,
                "public" => FirewallProfile.Public,
                "all" => FirewallProfile.All,
                _ => FirewallProfile.All
            };

            var api = new WindowsFirewallApi();
            bool result;
            string action;

            if (enabled)
            {
                result = api.EnableFirewall(profile);
                action = "启用";
            }
            else
            {
                result = api.DisableFirewall(profile);
                action = "禁用";
            }

            if (result)
            {
                await WriteJsonResponseAsync(context, new ResponseModel<object>
                {
                    Success = true,
                    Message = $"防火墙{action}成功 ({profile})",
                    Data = new
                    {
                        profile = profile.ToString(),
                        enabled = enabled,
                        action = action
                    }
                });
            }
            else
            {
                await WriteJsonResponseAsync(context, new ResponseModel<object>
                {
                    Success = false,
                    Message = $"防火墙{action}失败，可能需要管理员权限",
                    Data = null
                });
            }
        }
        catch (ArgumentException ex)
        {
            await WriteErrorResponseAsync(context, ex.Message);
        }
        catch (Exception ex)
        {
            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = false,
                Message = $"操作防火墙时发生错误: {ex.Message}",
                Data = null
            });
        }
    }
}