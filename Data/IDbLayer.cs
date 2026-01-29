using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Data
{ 
    public interface IDbLayer
    {
        Task<DataTable> ExecuteSPAsync(string spName, SqlParameter[] parameters);
        Task<int> ExecuteNonQueryAsync(string spName, SqlParameter[] parameters);
    }

}
