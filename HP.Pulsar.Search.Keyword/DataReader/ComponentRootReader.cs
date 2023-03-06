using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader
{
    internal class ComponentRootReader : IKeywordSearchDataReader
    {
        public ComponentRootReader(KeywordSearchInfo info)
        {
            _csProvider = new(info.Environment);
        }

        private ConnectionStringProvider _csProvider;

        public Task<CommonDataModel> GetDataAsync(int componentRootId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
        {
            Console.WriteLine("Read Data");
            IEnumerable<CommonDataModel> componentRoot = await _GetComponentRootAsync();
            componentRoot = await _GetComponentRootListAsync(componentRoot);
            return componentRoot;
        }

        private string _GetAllComponentRootSqlCommandText()
        {
            return @"
                    SELECT root.id AS ComponentRootId,
                        root.name AS ComponentRootName,
                        description,
                        vendor.Name AS VendorName,
                        cate.name AS Category,
                        user1.FirstName + ' ' + user1.LastName AS PM,
                        user2.FirstName + ' ' + user2.LastName AS DeveloperName,
                        user3.FirstName + ' ' + user3.LastName AS TesterName,
                        coreteam.Name AS CoreTeam,
                        ComponentType.Name AS ComponentType
                    FROM DeliverableRoot root
                    JOIN ComponentType ON ComponentType.ComponentTypeId = root.TypeID
                    JOIN Vendor ON root.vendorid = vendor.id
                    JOIN componentCategory cate ON cate.CategoryId = root.categoryid
                    JOIN UserInfo user1 ON user1.userid = root.devmanagerid
                    JOIN UserInfo user2 ON user2.userid = root.DeveloperID
                    JOIN UserInfo user3 ON user3.userid = root.TesterID
                    JOIN ComponentCoreTeam coreteam ON coreteam.ComponentCoreTeamId = root.CoreTeamID
                    WHERE typeid = 2
                        AND root.active = 1
                        AND (
                            @ComponentRootId = - 1
                            OR root.id = @ComponentRootId
                            );
                    ";
        }

        private string _GetTSQLProductListCommandText()
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

            //SELECT p.dotsname, r.Name
            //FROM DeliverableRoot root
            //join Product_DelRoot pr on root.Id = pr.DeliverableRootId
            //join ProductVersion p on pr.ProductVersionID = p.id
            //join ProductVersion_Release pr2 on pr2.ProductVersionID = p.Id
            //join ProductVersionRelease r on r.Id = pr2.ReleaseId
            //where root.id = 36886
            //order by p.dotsname
        }

        // TODO - performance improvement needed
        private async Task<IEnumerable<CommonDataModel>> _GetComponentRootListAsync(IEnumerable<CommonDataModel> componentRoots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();

            foreach (CommonDataModel root in componentRoots)
            {
                SqlCommand command = new(_GetTSQLProductListCommandText(), connection);
                SqlParameter parameter = new SqlParameter("ComponentRootId", root.GetValue("ComponentRootId"));
                command.Parameters.Add(parameter);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    root.Add("ProductList", reader["Product"].ToString());
                }
            }
            return componentRoots;
        }

        private async Task<IEnumerable<CommonDataModel>> _GetComponentRootAsync()
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            SqlCommand command = new(_GetAllComponentRootSqlCommandText(), connection);

            SqlParameter parameter = new("ComponentRootId", -1);
            command.Parameters.Add(parameter);

            await connection.OpenAsync();

            using SqlDataReader reader = command.ExecuteReader();

            List<CommonDataModel> output = new();

            while (reader.Read())
            {
                //string businessSegmentId;
                CommonDataModel root = new();
                int fieldCount = reader.FieldCount;

                for (int i = 0; i < fieldCount; i++)
                {
                    if (await reader.IsDBNullAsync(i))
                    {
                        continue;
                    }

                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString();
                    root.Add(columnName, value);
                }

                root.Add("target", "Component Root");

                output.Add(root);
            }

            return output;
        }
    }
}
