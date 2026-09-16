using Agriloco.Api.Models;
using Agriloco.Api.Security;
using Microsoft.Data.Sqlite;
using System.Text;

// Offline only: never start the website or contact SQL Server/email services.
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: PrepareMemberMigration <source.sqlite> <new-sanitized.sqlite>");
    return 2;
}
try
{
    var sourcePath = Path.GetFullPath(args[0]);
    var destinationPath = Path.GetFullPath(args[1]);
    if (sourcePath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase) || File.Exists(destinationPath))
        throw new InvalidOperationException("Destination must be a new file distinct from the source.");
    using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sourcePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
    source.Open();
    using var snapshot = source.BeginTransaction(deferred: true);
    var schema = new List<(string Type, string Name, string Sql)>();
    using (var command = source.CreateCommand())
    {
        command.Transaction = snapshot;
        command.CommandText = "SELECT type,name,sql FROM sqlite_master WHERE sql IS NOT NULL AND name NOT LIKE 'sqlite_%' ORDER BY rowid";
        using var reader = command.ExecuteReader();
        while (reader.Read()) schema.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
    }
    // Validate and hash every known legacy credential before creating any output.
    var hashes = new Dictionary<long, byte[]>();
    int converted = 0;
    using (var command = source.CreateCommand())
    {
        command.Transaction = snapshot;
        command.CommandText = "SELECT Id,PasswordHash,PasswordSalt FROM Members";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var stored = (byte[])reader.GetValue(1);
            var salt = (byte[])reader.GetValue(2);
            var text = new UTF8Encoding(false, true).GetString(stored);
            if (text.StartsWith("ASPNET:", StringComparison.Ordinal))
            {
                // Validate the hasher's versioned payload without exposing it or needing the password.
                var payload = Convert.FromBase64String(text[7..]);
                if (salt.Length != 0 || payload.Length < 49 || payload[0] != 1)
                    throw new InvalidOperationException("Unsupported credential format. No migration copy was produced.");
                hashes[id] = stored;
            }
            else
            {
                if (salt.Length != 0 || stored.Length == 0)
                    throw new InvalidOperationException("Unsupported legacy credential. Arrange a controlled reset; no migration copy was produced.");
                var member = new Member { Id = checked((int)id) };
                var hash = MemberPasswords.Hash(member, text);
                member.PasswordHash = hash;
                if (!MemberPasswords.Verify(member, text, out _)) throw new InvalidOperationException("Credential verification failed.");
                hashes[id] = hash;
                converted++;
            }
        }
    }
    // Fresh logical copy: original plaintext bytes never enter the destination file or its journal.
    using (new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
    using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destinationPath, Mode = SqliteOpenMode.ReadWrite }.ToString());
    destination.Open();
    using var transaction = destination.BeginTransaction();
    void Execute(string sql)
    {
        using var command = destination.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
    string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
    foreach (var table in schema.Where(x => x.Type == "table")) Execute(table.Sql);
    foreach (var table in schema.Where(x => x.Type == "table"))
    {
        using var read = source.CreateCommand();
        read.Transaction = snapshot;
        read.CommandText = "SELECT * FROM " + Quote(table.Name);
        using var rows = read.ExecuteReader();
        var columns = Enumerable.Range(0, rows.FieldCount).Select(rows.GetName).ToArray();
        while (rows.Read())
        {
            using var insert = destination.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = $"INSERT INTO {Quote(table.Name)} ({string.Join(',', columns.Select(Quote))}) VALUES ({string.Join(',', columns.Select((_, i) => "$p" + i))})";
            for (int i = 0; i < columns.Length; i++)
            {
                object value = rows.GetValue(i);
                if (table.Name.Equals("Members", StringComparison.OrdinalIgnoreCase))
                {
                    if (columns[i] == "PasswordHash") value = hashes[rows.GetInt64(rows.GetOrdinal("Id"))];
                    if (columns[i] == "PasswordSalt") value = Array.Empty<byte>();
                }
                insert.Parameters.AddWithValue("$p" + i, value);
            }
            insert.ExecuteNonQuery();
        }
    }
    foreach (var item in schema.Where(x => x.Type != "table")) Execute(item.Sql);
    transaction.Commit();
    Console.WriteLine($"Sanitized migration copy created. Members: {hashes.Count}; legacy credentials converted: {converted}. Source unchanged. No plaintext credentials written to output.");
    return 0;
}
catch
{
    // Database exceptions can include data. Never print exception details from credential migration.
    Console.Error.WriteLine("Migration preparation failed. Do not import the destination. Verify source formats, destination permissions and schema locally; no credential values were logged.");
    return 1;
}
