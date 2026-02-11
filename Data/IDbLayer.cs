using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;

namespace HMSCore.Data
{
    public interface IDbLayer
    {
        // Execute stored procedure returning DataTable
        Task<DataTable> ExecuteSPAsync(string spName, SqlParameter[] parameters);

        // Execute scalar query (like SELECT MAX)
        Task<object> ExecuteScalarAsync(string query, SqlParameter[] parameters = null);
        Task<DataSet> ExecuteSPWithMultipleResultsAsync(string spName, SqlParameter[] parameters = null);

    }
}
