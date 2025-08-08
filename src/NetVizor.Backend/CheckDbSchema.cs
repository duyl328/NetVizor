using System;
using System.IO;
using System.Threading.Tasks;
using Data;
using Common.Logger;
using Dapper;

class CheckDbSchema
{
    static async Task Main(string[] args)
    {
        Log.Initialize();
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(appDataPath, "NetVizor", "netvizor.db");
        
        Console.WriteLine($"检查数据库: {dbPath}");
        
        if (!File.Exists(dbPath))
        {
            Console.WriteLine("❌ 数据库文件不存在");
            return;
        }
        
        try
        {
            using var context = new NetVizorDbContext(dbPath);
            var connection = context.Connection;
            
            Console.WriteLine("✓ 数据库连接成功");
            
            // 检查所有表
            var tables = await connection.QueryAsync<string>(
                "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
            
            Console.WriteLine($"\n📋 现有表 ({tables.Count()}个):");
            foreach (var table in tables)
            {
                Console.WriteLine($"  - {table}");
            }
            
            // 检查聚合表是否存在
            var aggregationTables = new[]
            {
                "AppNetworkHourly", "AppNetworkDaily", "AppNetworkWeekly", "AppNetworkMonthly",
                "GlobalNetworkHourly", "GlobalNetworkDaily", "GlobalNetworkWeekly", "GlobalNetworkMonthly"
            };
            
            Console.WriteLine("\n🔍 聚合表检查:");
            var missingTables = new List<string>();
            
            foreach (var tableName in aggregationTables)
            {
                if (tables.Contains(tableName))
                {
                    // 检查表结构
                    var columns = await connection.QueryAsync<dynamic>(
                        $"PRAGMA table_info({tableName})");
                    
                    var columnNames = columns.Select(c => c.name.ToString()).ToArray();
                    Console.WriteLine($"  ✓ {tableName} (列: {string.Join(", ", columnNames)})");
                }
                else
                {
                    Console.WriteLine($"  ❌ {tableName} - 缺失");
                    missingTables.Add(tableName);
                }
            }
            
            if (missingTables.Count > 0)
            {
                Console.WriteLine($"\n⚠ 发现 {missingTables.Count} 个缺失的聚合表，需要运行数据库初始化");
                
                // 运行初始化以创建缺失的表
                Console.WriteLine("\n🔄 正在创建缺失的聚合表...");
                await context.InitializeDatabaseAsync();
                Console.WriteLine("✓ 数据库表结构更新完成");
                
                // 再次检查
                var updatedTables = await connection.QueryAsync<string>(
                    "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
                Console.WriteLine($"\n📋 更新后的表 ({updatedTables.Count()}个):");
                foreach (var table in updatedTables)
                {
                    Console.WriteLine($"  - {table}");
                }
            }
            else
            {
                Console.WriteLine("\n✅ 所有聚合表都已存在");
            }
            
            // 检查数据数量
            Console.WriteLine("\n📊 数据统计:");
            var appNetworkCount = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM AppNetwork");
            var globalNetworkCount = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM GlobalNetwork");
            var appInfoCount = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM AppInfo");
            
            Console.WriteLine($"  - AppNetwork: {appNetworkCount:N0} 条记录");
            Console.WriteLine($"  - GlobalNetwork: {globalNetworkCount:N0} 条记录");
            Console.WriteLine($"  - AppInfo: {appInfoCount:N0} 条记录");
            
            // 检查聚合表数据
            foreach (var tableName in aggregationTables)
            {
                if (tables.Contains(tableName) || missingTables.Count == 0)
                {
                    try
                    {
                        var count = await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {tableName}");
                        Console.WriteLine($"  - {tableName}: {count:N0} 条记录");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  - {tableName}: 查询失败 ({ex.Message})");
                    }
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            Console.WriteLine($"详细: {ex}");
        }
    }
}