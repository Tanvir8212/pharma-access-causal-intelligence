using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PharmaAccess.Data.Research;

public sealed class ReadOnlyResearchCommandInterceptor : DbCommandInterceptor
{
    private static readonly string[] Forbidden=["INSERT","UPDATE","DELETE","MERGE","DROP","ALTER","TRUNCATE","CREATE"];
    public static void Guard(string sql){var value=(sql??"").TrimStart();if(Forbidden.Any(x=>value.StartsWith(x,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Research web database access is read-only.");}
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command,CommandEventData eventData,InterceptionResult<int> result){Guard(command.CommandText);return base.NonQueryExecuting(command,eventData,result);}
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<int> result,CancellationToken cancellationToken=default){Guard(command.CommandText);return base.NonQueryExecutingAsync(command,eventData,result,cancellationToken);}
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result){Guard(command.CommandText);return base.ReaderExecuting(command,eventData,result);}
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default){Guard(command.CommandText);return base.ReaderExecutingAsync(command,eventData,result,cancellationToken);}
}
