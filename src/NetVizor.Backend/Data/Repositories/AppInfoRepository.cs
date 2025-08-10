using Data.Models;
using Dapper;

namespace Data.Repositories;

public interface IAppInfoRepository
{
    Task<IEnumerable<AppInfo>> GetAllAppsAsync();
    Task<AppInfo?> GetAppByIdAsync(int id);
    Task<AppInfo?> GetAppByAppIdAsync(string appId);
    Task<int> SaveAppAsync(AppInfo app);
    Task<int> UpdateAppAsync(AppInfo app);
    Task<int> DeleteAppAsync(int id);
}

public class AppInfoRepository : IAppInfoRepository
{
    private readonly NetVizorDbContext _context;

    public AppInfoRepository(NetVizorDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppInfo>> GetAllAppsAsync()
    {
        const string sql = "SELECT * FROM AppInfo WHERE DeleteTime <= 0 ORDER BY InsertTime DESC";
        return await _context.Connection.QueryAsync<AppInfo>(sql);
    }

    public async Task<AppInfo?> GetAppByIdAsync(int id)
    {
        const string sql = "SELECT * FROM AppInfo WHERE Id = @Id AND DeleteTime <= 0";
        return await _context.Connection.QueryFirstOrDefaultAsync<AppInfo>(sql, new { Id = id });
    }

    public async Task<AppInfo?> GetAppByAppIdAsync(string appId)
    {
        const string sql = "SELECT * FROM AppInfo WHERE AppId = @AppId AND DeleteTime <= 0";
        return await _context.Connection.QueryFirstOrDefaultAsync<AppInfo>(sql, new { AppId = appId });
    }

    public async Task<int> SaveAppAsync(AppInfo app)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        app.InsertTime = now;
        app.UpdateTime = now;
        app.DeleteTime = 0;

        const string sql = @"
            INSERT INTO AppInfo (AppId, OriginalAppId, Name, Path, Version, Company, Base64Icon, Hash, InsertTime, UpdateTime, DeleteTime)
            VALUES (@AppId, @OriginalAppId, @Name, @Path, @Version, @Company, @Base64Icon, @Hash, @InsertTime, @UpdateTime, @DeleteTime)";

        var result = await _context.Connection.ExecuteAsync(sql, app);

        // 获取插入的ID
        const string getIdSql = "SELECT last_insert_rowid()";
        var id = await _context.Connection.QuerySingleAsync<int>(getIdSql);
        app.Id = id;

        return result;
    }

    public async Task<int> UpdateAppAsync(AppInfo app)
    {
        app.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        const string sql = @"
            UPDATE AppInfo 
            SET AppId = @AppId, OriginalAppId = @OriginalAppId, Name = @Name, Path = @Path, Version = @Version, Company = @Company, 
                Base64Icon = @Base64Icon, Hash = @Hash, UpdateTime = @UpdateTime
            WHERE Id = @Id";

        return await _context.Connection.ExecuteAsync(sql, app);
    }

    public async Task<int> DeleteAppAsync(int id)
    {
        const string sql = "UPDATE AppInfo SET DeleteTime = @DeleteTime WHERE Id = @Id";
        return await _context.Connection.ExecuteAsync(sql,
            new { Id = id, DeleteTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
    }
}