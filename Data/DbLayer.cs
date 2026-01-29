using Microsoft.Data.SqlClient;
using System.Data; 


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

        public async Task<int> ExecuteNonQueryAsync(string spName, SqlParameter[] parameters)
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            using SqlCommand cmd = new SqlCommand(spName, con);

            cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            await con.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
    }

}
