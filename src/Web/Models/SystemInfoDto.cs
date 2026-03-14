namespace Web.Models;

public class SystemInfoDto
{
    public ServerInfoDto Server { get; set; } = new();
    public SqlServerInfoDto? SqlServer { get; set; }
    public List<TableCountDto> Tables { get; set; } = new();
}

public class ServerInfoDto
{
    public string MachineName { get; set; } = string.Empty;
    public string OsDescription { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string DotnetVersion { get; set; } = string.Empty;
    public string ServerUptime { get; set; } = string.Empty;
    public double WorkingMemoryMb { get; set; }
    public string Environment { get; set; } = string.Empty;
}

public class SqlServerInfoDto
{
    public string? Version { get; set; }
    public string? Edition { get; set; }
    public string? ProductLevel { get; set; }
    public string? ServerName { get; set; }
    public string? Collation { get; set; }
    public long DatabaseSizeMb { get; set; }
    public string? DatabaseName { get; set; }
    public int TableCount { get; set; }
    public DateTime? DatabaseCreated { get; set; }
    public int MaxConnections { get; set; }
    public string? Error { get; set; }
}

public class TableCountDto
{
    public string Name { get; set; } = string.Empty;
    public long Rows { get; set; }
}
