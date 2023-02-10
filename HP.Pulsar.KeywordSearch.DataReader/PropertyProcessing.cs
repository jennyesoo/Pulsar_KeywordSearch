using HP.Pulsar.KeywordSearch.CommonDataStructures;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP.Pulsar.KeywordSearch.DataReader
{
    public class PropertyProcessing
    {
        public string EndOfProduction(PulsarEnvironment env, string ProductVersionId)
        {
            List<string> EndOfProduction = new List<string>();
            string SQL_Command = "Exec usp_SelectEndOfProduction " + ProductVersionId;
            ConnectionStringProvider connect = new ConnectionStringProvider(env, SQL_Command);
            SqlDataReader EndOfProduction_SqlData = connect.Result;

            while (EndOfProduction_SqlData.Read())
            {
                EndOfProduction.Add(EndOfProduction_SqlData["EndOfProduction"].ToString().Split(" ")[0]);
            }
            connect.SqlDataConnect.Close();
            if (EndOfProduction.Count == 0)
            {
                return "";
            }
            else
            {
                return EndOfProduction[0];
            }
        }

        public string ProductGroups(PulsarEnvironment env, string ProductVersionId)
        {
            List<string> ProductGroups = new List<string>();
            string SQL_Command = "Exec usp_ListWHQLSubmissions " + ProductVersionId;
            ConnectionStringProvider connect = new ConnectionStringProvider(env, SQL_Command);
            SqlDataReader ProductGroups_SqlData = connect.Result;

            while (ProductGroups_SqlData.Read())
            {
                ProductGroups.Add(ProductGroups_SqlData["fullname"].ToString());
            }
            connect.SqlDataConnect.Close();
            if (ProductGroups.Count == 0)
            {
                return "";
            }
            else
            {
                return ProductGroups[0];
            }
        }

        public string WHQLstatus(PulsarEnvironment env, string ProductVersionId)
        {
            string WHQLstatus_result;
            List<string> WHQLstatus = new List<string>();
            string SQL_Command = "Exec usp_ListWHQLSubmissions " + ProductVersionId;
            ConnectionStringProvider connect = new ConnectionStringProvider(env, SQL_Command);
            SqlDataReader WHQLstatus_SqlData = connect.Result;

            while (WHQLstatus_SqlData.Read())
            {
                WHQLstatus.Add(WHQLstatus_SqlData["fullname"].ToString());
            }

            if (WHQLstatus.Count == 0)
            {
                WHQLstatus_result = "incomplete";
            }
            else
            {
                WHQLstatus_result = "Unknown";
            }
            connect.SqlDataConnect.Close();
            return WHQLstatus_result;
        }

        public string Leadproduct(PulsarEnvironment env, string BusinessSegmentID)
        {
            List<string> Leadproduct = new List<string>();
            string SQL_Command = "Declare @leadproductTable Table (ID int," +
                "Name varchar(100)," +
                "Active INT, " +
                "OnProduct int," +
                "BusinessSegmantID int," +
                "Cyc int," +
                "Yrs int ," +
                "LeadProductreleaseID int ," +
                "LeadProductreleaseDesc varchar(100)," +
                "LeadproductVersionId int ," +
                "LeadproductVersionReleaseId int ," +
                "RtmWave varchar(100))" +
                "Insert @leadproductTable exec usp_ProductVersion_release " + BusinessSegmentID +
                "Select CONCAT (LeadProductreleaseDesc, ',') as LeadProductreleaseDesc  from @leadproductTable where LeadProductreleaseDesc != '' ";

            ConnectionStringProvider connect = new ConnectionStringProvider(env, SQL_Command);
            SqlDataReader Leadproduct_SqlData = connect.Result;

            while (Leadproduct_SqlData.Read())
            {
                Leadproduct.Add(Leadproduct_SqlData["LeadProductreleaseDesc"].ToString());
            }
            connect.SqlDataConnect.Close();
            if (Leadproduct.Count == 0)
            {
                return "";
            }
            else
            {
                return Leadproduct[0];
            }

        }

        public string Chipsets(PulsarEnvironment env, string ProductVersionId)
        {
            List<string> Chipsets = new List<string>();
            string SQL_Command = "Exec usp_getproductchipsets " + ProductVersionId;
            ConnectionStringProvider connect = new ConnectionStringProvider(env, SQL_Command);
            SqlDataReader Chipsets_SqlData = connect.Result;

            string chipsets_value = "";
            while (Chipsets_SqlData.Read())
            {
                if (Chipsets_SqlData["Selected"].ToString() == "1")
                {
                    Console.Write("test value : " + Chipsets_SqlData["State"].ToString());
                    if (Chipsets_SqlData["State"].ToString() == "True")
                    {
                        if (chipsets_value == "")
                        {
                            chipsets_value += Chipsets_SqlData["CodeName"];
                        }
                        else
                        {
                            chipsets_value += " , " + Chipsets_SqlData["CodeName"];
                        }
                    }
                    else
                    {
                        if (chipsets_value == "")
                        {
                            chipsets_value += Chipsets_SqlData["CodeName"] + "Inactive";
                        }
                        else
                        {
                            chipsets_value += chipsets_value += " , " + Chipsets_SqlData["CodeName"];
                        }
                    }
                }
                Chipsets.Add(chipsets_value);
            }
            connect.SqlDataConnect.Close();
            return Chipsets[0];
        }

        public string CurrentBIOSVersions(PulsarEnvironment env, string ProductVersionId, string ProductStatus)
        {
            string CurrentBIOSVersions = "";
            string SQL_Command = "Select currentROM,currentWebROM From ProductVersion where ID = " + ProductVersionId;
            ConnectionStringProvider connect = new ConnectionStringProvider(env, SQL_Command);
            SqlDataReader CurrentROM_SqlData = connect.Result;

            while (CurrentROM_SqlData.Read())
            {
                string CurrentROM_value = CurrentROM_SqlData["currentROM"].ToString();
                string CurrentWebROM_value = CurrentROM_SqlData["currentWebROM"].ToString();
                string[] value_List = { "Development", "Definition" };

                if (CurrentROM_value == "" && (ProductStatus == "Development" || ProductStatus == "Definition"))
                {
                    string SQL_Command_two = "exec spListTargetedbiosversions " + ProductVersionId;
                    ConnectionStringProvider connect_two = new ConnectionStringProvider(env, SQL_Command_two);
                    SqlDataReader CurrentROM_SqlData_two = connect_two.Result;
                    while (CurrentROM_SqlData_two.Read())
                    {
                        CurrentROM_value = "Targeted: " + CurrentROM_SqlData_two["TargetedVersions"];
                    }
                }
                else if (!value_List.Contains(ProductStatus))
                {
                    if (CurrentROM_value == "")
                    {
                        CurrentROM_value = "Factory: ";
                    }
                    else
                    {
                        CurrentROM_value = "Factory: UnKnown";
                    }
                }
                if (CurrentROM_value != "" && CurrentWebROM_value != "")
                {
                    CurrentROM_value += "Web: " + CurrentWebROM_value;
                }
                else if (CurrentROM_value == "" && CurrentWebROM_value != "")
                {
                    CurrentROM_value = "Web: " + CurrentWebROM_value;
                }

                CurrentBIOSVersions = CurrentROM_value;
            }
            connect.SqlDataConnect.Close();
            return CurrentBIOSVersions;
        }
    }
}
