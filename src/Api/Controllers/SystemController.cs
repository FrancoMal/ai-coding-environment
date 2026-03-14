using System.Diagnostics;
using System.Runtime.InteropServices;
using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly AppDbContext _db;

    public SystemController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetSystemInfo()
    {
        var process = Process.GetCurrentProcess();

        // Server info
        var serverInfo = new
        {
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            DotnetVersion = RuntimeInformation.FrameworkDescription,
            ServerUptime = FormatUptime(DateTime.UtcNow - process.StartTime.ToUniversalTime()),
            WorkingMemoryMb = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        // SQL Server info
        object? sqlInfo = null;
        try
        {
            var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    SERVERPROPERTY('ProductVersion') AS Version,
                    SERVERPROPERTY('Edition') AS Edition,
                    SERVERPROPERTY('ProductLevel') AS ProductLevel,
                    SERVERPROPERTY('ServerName') AS ServerName,
                    SERVERPROPERTY('Collation') AS Collation,
                    (SELECT SUM(CAST(size AS BIGINT)) * 8 / 1024 FROM sys.master_files WHERE database_id = DB_ID()) AS DatabaseSizeMb,
                    DB_NAME() AS DatabaseName,
                    (SELECT COUNT(*) FROM sys.tables) AS TableCount,
                    (SELECT create_date FROM sys.databases WHERE name = DB_NAME()) AS DatabaseCreated,
                    @@MAX_CONNECTIONS AS MaxConnections";

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                sqlInfo = new
                {
                    Version = reader["Version"]?.ToString(),
                    Edition = reader["Edition"]?.ToString(),
                    ProductLevel = reader["ProductLevel"]?.ToString(),
                    ServerName = reader["ServerName"]?.ToString(),
                    Collation = reader["Collation"]?.ToString(),
                    DatabaseSizeMb = reader["DatabaseSizeMb"] != DBNull.Value ? Convert.ToInt64(reader["DatabaseSizeMb"]) : 0,
                    DatabaseName = reader["DatabaseName"]?.ToString(),
                    TableCount = reader["TableCount"] != DBNull.Value ? Convert.ToInt32(reader["TableCount"]) : 0,
                    DatabaseCreated = reader["DatabaseCreated"] != DBNull.Value ? Convert.ToDateTime(reader["DatabaseCreated"]) : (DateTime?)null,
                    MaxConnections = reader["MaxConnections"] != DBNull.Value ? Convert.ToInt32(reader["MaxConnections"]) : 0
                };
            }
            await conn.CloseAsync();
        }
        catch
        {
            sqlInfo = new { Error = "No se pudo conectar a SQL Server" };
        }

        // Table row counts
        List<object>? tableCounts = null;
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT t.name AS TableName,
                       SUM(p.rows) AS RowCount
                FROM sys.tables t
                INNER JOIN sys.partitions p ON t.object_id = p.object_id
                WHERE p.index_id IN (0, 1)
                GROUP BY t.name
                ORDER BY t.name";

            using var reader = await cmd.ExecuteReaderAsync();
            tableCounts = new List<object>();
            while (await reader.ReadAsync())
            {
                tableCounts.Add(new
                {
                    Name = reader["TableName"]?.ToString(),
                    Rows = Convert.ToInt64(reader["RowCount"])
                });
            }
            await conn.CloseAsync();
        }
        catch { }

        return Ok(new { Server = serverInfo, SqlServer = sqlInfo, Tables = tableCounts });
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        return $"{(int)uptime.TotalMinutes}m";
    }
}
