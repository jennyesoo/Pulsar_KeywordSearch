using HP.Pulsar.KeywordSearch.CommonDataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace HP.Pulsar.KeywordSearch.DataReader
{
    public class ConnectionProvider
    {
        public SqlDataReader Result;
        public SqlConnection SqlDataConnect;
        public ConnectionProvider(PulsarEnvironment env , string Command)
        {
            SqlConnection conn = new SqlConnection(GetSqlServerConnectionString(env));
            conn.Open();
            SqlCommand cmd = new SqlCommand(Command, conn);
            SqlDataReader result = cmd.ExecuteReader();
            Result = result;
            SqlDataConnect = conn;
        }

        public string GetSqlServerConnectionString(PulsarEnvironment env)
        {
            if ( env == PulsarEnvironment.Dev)
            {
                return "server=TdcPulsarItgDb.tpc.rd.hpicorp.net\\DEV;initial catalog=PRS;user id=xxxx;password=yyyy";
            }
            else if (env == PulsarEnvironment.Test)
            {
                return "Server = TdcPulsarItgDb.tpc.rd.hpicorp.net\\TEST; Initial Catalog = PRS;Integrated Security=true;";
            }
            else if (env == PulsarEnvironment.Sandbox)
            {
                return "Server = TdcPulsarItgDb.tpc.rd.hpicorp.net\\TEST; Initial Catalog = PRS;Integrated Security=true;";
            }
            else if (env == PulsarEnvironment.Production)
            {
                return "Server = TdcPulsarItgDb.tpc.rd.hpicorp.net\\TEST; Initial Catalog = PRS;Integrated Security=true;";
            }

            return "";
        }

        // 可有可無
        /*
        public string GetSqlServerStringCommand(string GetDataType) 
        {
            if (GetDataType == "all")
            {
                SqlServerStringCommand sql_command = new SqlServerStringCommand();
                return sql_command.productversion_command_all;
            }
            
            SqlServerStringCommand sql_command = new SqlServerStringCommand();
            return sql_command.productversion_command_one;
        }
        */
    }
}
