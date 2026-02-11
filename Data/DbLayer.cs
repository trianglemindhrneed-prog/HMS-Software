using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace HMSCore.Data
{
    public class DbLayer : IDbLayer
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DbLayer(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<DataTable> ExecuteSPAsync(string spName, SqlParameter[] parameters)
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(spName, con);

            cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            await con.OpenAsync();

            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            return dt;
        }

        public async Task<object> ExecuteScalarAsync(string query, SqlParameter[] parameters = null)
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(query, con);

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            await con.OpenAsync();
            return await cmd.ExecuteScalarAsync();
        }

        public async Task<DataSet> ExecuteSPWithMultipleResultsAsync(string spName, SqlParameter[] parameters = null)
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(spName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            await con.OpenAsync();

            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);

            return ds;
        }

    }
}
