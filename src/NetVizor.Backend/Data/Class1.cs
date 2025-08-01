namespace Data;

public class Class1
{
}

/*

软件信息的记录：
名字、路径、版本、公司、ICON等

归档与清理任务的调度
使用 BackgroundWorker / Timer / Task 定期执行：
按月归档
删除旧数据
合并压缩历史数据（比如求平均/最大/最小）

异常崩溃后的恢复
建议使用 WAL 模式 + 定期备份数据库文件。

常见的异常事件类型：
连接异常
连接失败（如 TCP 三次握手失败）
连接超时、断开异常
流量异常
突然流量暴增或骤降
单个 IP/端口流量异常（过高或过低）
安全告警
访问黑名单 IP 或域名
非法端口访问（比如非标准端口被访问）
端口扫描行为检测
异常登录/连接尝试
软件异常
某进程网络异常终止
网络接口断开或重连
系统网络错误（如 DNS 解析失败）



事件日志的记录结构建议
| 字段       | 说明                          |
| -------- | --------------------------- |
| 事件ID     | 唯一标识                        |
| 时间戳      | 事件发生时间                      |
| 事件类型     | 如“连接失败”、“异常流量”、“黑名单访问”等     |
| 相关进程/应用  | 关联的进程路径或名称（如果有）             |
| 本地 IP/端口 | 事件关联的本地地址                   |
| 远程 IP/端口 | 事件关联的远程地址                   |
| 事件描述     | 详细描述，如“连接 8.8.8.8:443 超过阈值” |
| 严重等级     | Info/Warning/Error/Critical |
| 额外数据     | 可 JSON 格式存储其他细节信息           |



CREATE TABLE event_log (
event_id INTEGER PRIMARY KEY AUTOINCREMENT,
timestamp DATETIME NOT NULL,
event_type TEXT NOT NULL,
process_path TEXT,
local_ip TEXT,
local_port INTEGER,
remote_ip TEXT,
remote_port INTEGER,
severity TEXT NOT NULL,
description TEXT,
extra_data TEXT -- JSON 格式可选
);



端口扫描行为如何检测？
检测思路
短时间内对多个端口发起连接尝试：

同一源 IP 在短时间内向同一目标 IP 扫描多个端口。

大量连接失败：

多个端口连接失败，说明可能是扫描。

连接速率异常：

短时间内连接速率远高于正常业务连接。

实现方法
滑动时间窗口统计：记录单位时间内访问某目标IP的端口次数。

阈值判断：超过阈值触发告警。

结合连接失败率：如果大部分端口连接失败，更可能是扫描。

黑名单过滤：过滤已知安全扫描器IP。




第一个版本只记录和分析数据，不进行异常信息监控和警告




 */