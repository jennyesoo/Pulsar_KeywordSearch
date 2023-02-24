using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP.Pulsar.Search.Keyword.DataReader
{
    internal class ComponentRootReader : IKeywordSearchDataReader
    {
        public ComponentRootReader(KeywordSearchInfo info)
        {
            _csProvider = new(info.Environment);
        }

        private ConnectionStringProvider _csProvider;

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync(int meiliseatchcount)
        {
            Console.WriteLine("Read Data");
            IEnumerable<CommonDataModel> ComponentRoot = await GetComponentRootAsync(meiliseatchcount);
            ComponentRoot = await GetComponentRootListAsync(ComponentRoot);
            return ComponentRoot;
        }

        private string GetAllComponentRootSqlCommandText()
        {
            return @"
                    SELECT root.id AS ComponentRootId,
                        root.name AS ComponentRootName,
                        description,
                        vendor.Name AS VendorName,
                        cate.name As Category,
                        user1.FirstName + ' ' + user1.LastName AS PM,
                        user2.FirstName + ' ' + user2.LastName AS DeveloperName,
                        user3.FirstName + ' ' + user3.LastName AS TesterName,
                        coreteam.Name AS CoreTeam,
                        ComponentType.Name As ComponentType
                    FROM DeliverableRoot root
                    Join ComponentType on ComponentType.ComponentTypeId = root.TypeID
                    JOIN vendor ON root.vendorid = vendor.id
                    JOIN componentCategory cate ON cate.CategoryId = root.categoryid
                    JOIN UserInfo user1 ON user1.userid = root.devmanagerid
                    JOIN userinfo user2 ON user2.userid = root.DeveloperID
                    JOIN userinfo user3 ON user3.userid = root.TesterID
                    JOIN componentcoreteam coreteam ON coreteam.ComponentCoreTeamId = root.CoreTeamID 
                    WHERE typeid = 2 AND root.active = 1 AND (@ComponentRootId = -1 OR root.id = @ComponentRootId);
                ";
        }

        private string GetTSQLProductListCommandText()
        {
            return @"select  DR.Id as ComponentRoot,
                            stuff((SELECT ' , ' + (CONVERT(Varchar, p.Id) + ' ' +  p.DOTSName)
                                    FROM ProductVersion p
                                    JOIN ProductStatus ps ON ps.id = p.ProductStatusID
                                    JOIN Product_DelRoot pr on pr.ProductVersionId = p.id
                                    JOIN DeliverableRoot root ON root.Id = pr.DeliverableRootId
                                    WHERE root.Id = DR.Id  and ps.Name <> 'Inactive' and p.FusionRequirements = 1 order by root.Id
                                    for xml path('')),1,3,'') As Product
                    FROM DeliverableRoot DR
                    where DR.Id = @ComponentRootId
                    group by DR.Id";
        }

        private async Task<IEnumerable<CommonDataModel>> GetComponentRootListAsync(IEnumerable<CommonDataModel> ComponentRoots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();

            foreach (CommonDataModel ComponentRoot in ComponentRoots)
            {
                SqlCommand command = new(GetTSQLProductListCommandText(), connection);
                SqlParameter parameter = new SqlParameter("ComponentRootId", ComponentRoot.GetValue("ComponentRootId"));
                command.Parameters.Add(parameter);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    ComponentRoot.Add("ProductList", reader["Product"].ToString());
                }
            }
            return ComponentRoots;
        }
        private async Task<IEnumerable<CommonDataModel>> GetComponentRootAsync(int meiliseatchcount)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            SqlCommand command = new(GetAllComponentRootSqlCommandText(), connection);

            SqlParameter parameter = new("ComponentRootId", -1);
            command.Parameters.Add(parameter);

            await connection.OpenAsync();

            using SqlDataReader reader = command.ExecuteReader();

            List<CommonDataModel> output = new();

            while (reader.Read())
            {
                //string businessSegmentId;
                CommonDataModel ComponentRoot = new();
                int fieldCount = reader.FieldCount;

                for (int i = 0; i < fieldCount; i++)
                {
                    if (await reader.IsDBNullAsync(i))
                    {
                        continue;
                    }

                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString();
                    ComponentRoot.Add(columnName, value);
       
                }

                meiliseatchcount++;
                ComponentRoot.Add("target", "Component Root");
                ComponentRoot.Add("Id", meiliseatchcount.ToString());
                output.Add(ComponentRoot);
            }
            return output;
        }
    }
}
