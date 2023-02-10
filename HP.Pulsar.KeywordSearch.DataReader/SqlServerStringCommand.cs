using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP.Pulsar.KeywordSearch.DataReader
{
    public class SqlServerStringCommand
    {
        public string productversion_command_all = @"SELECT p.id as ProductId,
                                                DOTSName as ProductName,
                                                partner.name as Partner,
                                                pdc.Name as DevCenter,
                                                Brands,
                                                p.SystemBoardId,
                                                ServiceLifeDate,
                                                ps.Name as ProductStatus,
                                                sg.Name as BusinessSegment,
                                                            p.CreatedBy as CreatorName,
                                                            p.Created as CreatedDate,
                                                            p.UpdatedBy as LastUpdaterName,
                                                            p.Updated as LatestUpdateDate,
                                                            user_SMID.FirstName + ' ' + user_SMID.LastName as SystemManager,
                                                            user_SMID.Email as SystemManagerEmail,
                                                            user_PDPM.FirstName + ' ' + user_PDPM.LastName as PlatformDevelopmentPM,
                                                            user_PDPM.Email as PlatformDevelopmentPMEmail,
                                                            user_SCID.FirstName + ' ' + user_SCID.LastName as SupplyChain,
                                                            user_SCID.Email as SupplyChainEmail,
                                                            user_ODMSEPM.FirstName + ' ' + user_ODMSEPM.LastName as ODMSystemEngineeringPM,
                                                            user_ODMSEPM.Email as ODMSystemEngineeringPMEmail,
                                                            user_CM.FirstName + ' ' + user_CM.LastName as ConfigurationManager,
                                                            user_CM.Email as ConfigurationManagerEmail,
                                                            user_CPM.FirstName + ' ' + user_CPM.LastName as CommodityPM,
                                                            user_CPM.Email as CommodityPMEmail,
                                                            user_Service.FirstName + ' ' + user_Service.LastName as Service,
                                                            user_Service.Email as ServiceEmail,
                                                            user_ODMHWPM.FirstName + ' ' + user_ODMHWPM.LastName as ODMHWPM,
                                                            user_ODMHWPM.Email as ODMHWPMEmail,
                                                            user_POPM.FirstName + ' ' + user_POPM.LastName as ProgramOfficeProgramManager,
                                                            user_POPM.Email as ProgramOfficeProgramManagerEmail,
                                                            user_Quality.FirstName + ' ' + user_Quality.LastName as Quality,
                                                            user_Quality.Email as QualityEmail,
                                                            user_PPM.FirstName + ' ' + user_PPM.LastName as PlanningPM,
                                                            user_PPM.Email as PlanningPMEmail,
                                                            user_BIOSPM.FirstName + ' ' + user_BIOSPM.LastName as BIOSPM,
                                                            user_BIOSPM.Email as BIOSPMEmail,
                                                            user_SEPM.FirstName + ' ' + user_SEPM.LastName as SystemsEngineeringPM,
                                                            user_SEPM.Email as SystemsEngineeringPMEmail,
                                                            user_MPM.FirstName + ' ' + user_MPM.LastName as MarketingProductMgmt,
                                                            user_MPM.Email as MarketingProductMgmtEmail,
                                                            user_ProPM.FirstName + ' ' + user_ProPM.LastName as ProcurementPM,
                                                            user_ProPM.Email as ProcurementPMEmail,
                                                            user_SWM.FirstName + ' ' + user_SWM.LastName as SWMarketing,
                                                            user_SWM.Email as SWMarketingEmail,
                                                            pf.Name as ProductFamily,
                                                            partner.name as ODM,
                                                            pis.Name as ReleaseTeam,
                                                            p.RegulatoryModel as RegulatoryModel,
                                                            STUFF((
                                                                    SELECT ',' + new_releases.Releases
                                                                    FROM  
                                                                    (SELECT ProductVersion.id as ProductId,
                                                                            pvr.Name as Releases

                                                                        FROM ProductVersion 
                                                                        FULL JOIN ProductVersion_Release pv_r ON pv_r.ProductVersionID = ProductVersion.ID
                                                                        FULL JOIN ProductVersionRelease pvr ON pvr.ID = pv_r.ReleaseID

                                                                        FULL JOIN ProductStatus ps ON ps.id = ProductVersion.ProductStatusID
                                                                        WHERE ps.Name <> 'Inactive' and p.FusionRequirements = 1 and ProductVersion.ID = p.id) as new_releases
                                                                    FOR XML PATH('') 
                                                                    ), 1, 1, '') as Releases,
                                                            p.Description,
                                                            pl.Name + '-' + pl.Description as ProductLine,
                                                            CASE WHEN p.PreinstallTeam = -1 THEN ''
                                                                WHEN p.PreinstallTeam = 1 THEN 'Houston'
                                                                WHEN p.PreinstallTeam = 2 THEN 'Taiwan'
                                                                WHEN p.PreinstallTeam = 3 THEN 'Singapore'
                                                                WHEN p.PreinstallTeam = 4 THEN 'Brazil'
                                                                WHEN p.PreinstallTeam = 5 THEN 'CDC'
                                                                WHEN p.PreinstallTeam = 6 THEN 'Houston – Thin Client'
                                                                WHEN p.PreinstallTeam = 7 THEN 'Mobility'
                                                                WHEN p.PreinstallTeam = 8 THEN ''
                                                            END AS PreinstallTeam,
                                                            p.MachinePNPID as MachinePNPID,
                                                            p.BusinessSegmentID
                                            FROM ProductVersion p
                                            FULL JOIN ProductFamily pf ON p.ProductFamilyId = pf.id
                                            FULL JOIN Partner partner ON partner.id = p.PartnerId
                                            FULL JOIN ProductDevCenter pdc ON pdc.ProductDevCenterId = DevCenter
                                            FULL JOIN ProductStatus ps ON ps.id = p.ProductStatusID
                                            FULL JOIN BusinessSegment sg ON sg.BusinessSegmentID = p.BusinessSegmentID
                                            FULL JOIN PreinstallTeam pis ON pis.ID = p.ReleaseTeam
                                            FULL JOIN UserInfo user_SMID On user_SMID.userid = p.SMID
                                            FULL JOIN UserInfo user_PDPM On user_PDPM.userid = p.PlatformDevelopmentID
                                            FULL JOIN UserInfo user_SCID On user_SCID.userid = p.SupplyChainID
                                            FULL JOIN UserInfo user_ODMSEPM On user_ODMSEPM.userid = p.ODMSEPMID
                                            FULL JOIN UserInfo user_CM On user_CM.userid = p.PMID 
                                            FULL JOIN UserInfo user_CPM On user_CPM.userid = p.PDEID 
                                            FULL JOIN UserInfo user_Service On user_Service.userid = p.ServiceID 
                                            FULL JOIN UserInfo user_ODMHWPM On user_ODMHWPM.userid = p.ODMHWPMID
                                            FULL JOIN UserInfo user_POPM On user_POPM.userid = p.TDCCMID
                                            FULL JOIN UserInfo user_Quality On user_Quality.userid = p.QualityID
                                            FULL JOIN UserInfo user_PPM On user_PPM.userid = p.PlanningPMID 
                                            FULL JOIN UserInfo user_BIOSPM On user_BIOSPM.userid = p.BIOSLeadID 
                                            FULL JOIN UserInfo user_SEPM On user_SEPM.userid = p.SEPMID 
                                            FULL JOIN UserInfo user_MPM On user_MPM.userid = p.ConsMarketingID 
                                            FULL JOIN UserInfo user_ProPM On user_ProPM.userid = p.ProcurementPMID 
                                            FULL JOIN UserInfo user_SWM On user_SWM.userid = p.SwMarketingId
                                            Full JOIN ProductLine pl ON pl.Id = p.ProductLineId
                                            WHERE ps.Name <> 'Inactive' and p.FusionRequirements = 1 ";

        public string productversion_command_one = @"SELECT p.id as ProductId,
                                                DOTSName as ProductName,
                                                partner.name as Partner,
                                                pdc.Name as DevCenter,
                                                Brands,
                                                p.SystemBoardId,
                                                ServiceLifeDate,
                                                ps.Name as ProductStatus,
                                                sg.Name as BusinessSegment,
                                                            p.CreatedBy as CreatorName,
                                                            p.Created as CreatedDate,
                                                            p.UpdatedBy as LastUpdaterName,
                                                            p.Updated as LatestUpdateDate,
                                                            user_SMID.FirstName + ' ' + user_SMID.LastName as SystemManager,
                                                            user_SMID.Email as SystemManagerEmail,
                                                            user_PDPM.FirstName + ' ' + user_PDPM.LastName as PlatformDevelopmentPM,
                                                            user_PDPM.Email as PlatformDevelopmentPMEmail,
                                                            user_SCID.FirstName + ' ' + user_SCID.LastName as SupplyChain,
                                                            user_SCID.Email as SupplyChainEmail,
                                                            user_ODMSEPM.FirstName + ' ' + user_ODMSEPM.LastName as ODMSystemEngineeringPM,
                                                            user_ODMSEPM.Email as ODMSystemEngineeringPMEmail,
                                                            user_CM.FirstName + ' ' + user_CM.LastName as ConfigurationManager,
                                                            user_CM.Email as ConfigurationManagerEmail,
                                                            user_CPM.FirstName + ' ' + user_CPM.LastName as CommodityPM,
                                                            user_CPM.Email as CommodityPMEmail,
                                                            user_Service.FirstName + ' ' + user_Service.LastName as Service,
                                                            user_Service.Email as ServiceEmail,
                                                            user_ODMHWPM.FirstName + ' ' + user_ODMHWPM.LastName as ODMHWPM,
                                                            user_ODMHWPM.Email as ODMHWPMEmail,
                                                            user_POPM.FirstName + ' ' + user_POPM.LastName as ProgramOfficeProgramManager,
                                                            user_POPM.Email as ProgramOfficeProgramManagerEmail,
                                                            user_Quality.FirstName + ' ' + user_Quality.LastName as Quality,
                                                            user_Quality.Email as QualityEmail,
                                                            user_PPM.FirstName + ' ' + user_PPM.LastName as PlanningPM,
                                                            user_PPM.Email as PlanningPMEmail,
                                                            user_BIOSPM.FirstName + ' ' + user_BIOSPM.LastName as BIOSPM,
                                                            user_BIOSPM.Email as BIOSPMEmail,
                                                            user_SEPM.FirstName + ' ' + user_SEPM.LastName as SystemsEngineeringPM,
                                                            user_SEPM.Email as SystemsEngineeringPMEmail,
                                                            user_MPM.FirstName + ' ' + user_MPM.LastName as MarketingProductMgmt,
                                                            user_MPM.Email as MarketingProductMgmtEmail,
                                                            user_ProPM.FirstName + ' ' + user_ProPM.LastName as ProcurementPM,
                                                            user_ProPM.Email as ProcurementPMEmail,
                                                            user_SWM.FirstName + ' ' + user_SWM.LastName as SWMarketing,
                                                            user_SWM.Email as SWMarketingEmail,
                                                            pf.Name as ProductFamily,
                                                            partner.name as ODM,
                                                            pis.Name as ReleaseTeam,
                                                            p.RegulatoryModel as RegulatoryModel,
                                                            STUFF((
                                                                    SELECT ',' + new_releases.Releases
                                                                    FROM  
                                                                    (SELECT ProductVersion.id as ProductId,
                                                                            pvr.Name as Releases

                                                                        FROM ProductVersion 
                                                                        FULL JOIN ProductVersion_Release pv_r ON pv_r.ProductVersionID = ProductVersion.ID
                                                                        FULL JOIN ProductVersionRelease pvr ON pvr.ID = pv_r.ReleaseID

                                                                        FULL JOIN ProductStatus ps ON ps.id = ProductVersion.ProductStatusID
                                                                        WHERE ps.Name <> 'Inactive' and p.FusionRequirements = 1 and ProductVersion.ID = p.id) as new_releases
                                                                    FOR XML PATH('') 
                                                                    ), 1, 1, '') as Releases,
                                                            p.Description,
                                                            pl.Name + '-' + pl.Description as ProductLine,
                                                            CASE WHEN p.PreinstallTeam = -1 THEN ''
                                                                WHEN p.PreinstallTeam = 1 THEN 'Houston'
                                                                WHEN p.PreinstallTeam = 2 THEN 'Taiwan'
                                                                WHEN p.PreinstallTeam = 3 THEN 'Singapore'
                                                                WHEN p.PreinstallTeam = 4 THEN 'Brazil'
                                                                WHEN p.PreinstallTeam = 5 THEN 'CDC'
                                                                WHEN p.PreinstallTeam = 6 THEN 'Houston – Thin Client'
                                                                WHEN p.PreinstallTeam = 7 THEN 'Mobility'
                                                                WHEN p.PreinstallTeam = 8 THEN ''
                                                            END AS PreinstallTeam,
                                                            p.MachinePNPID as MachinePNPID,
                                                            p.BusinessSegmentID
                                            FROM ProductVersion p
                                            FULL JOIN ProductFamily pf ON p.ProductFamilyId = pf.id
                                            FULL JOIN Partner partner ON partner.id = p.PartnerId
                                            FULL JOIN ProductDevCenter pdc ON pdc.ProductDevCenterId = DevCenter
                                            FULL JOIN ProductStatus ps ON ps.id = p.ProductStatusID
                                            FULL JOIN BusinessSegment sg ON sg.BusinessSegmentID = p.BusinessSegmentID
                                            FULL JOIN PreinstallTeam pis ON pis.ID = p.ReleaseTeam
                                            FULL JOIN UserInfo user_SMID On user_SMID.userid = p.SMID
                                            FULL JOIN UserInfo user_PDPM On user_PDPM.userid = p.PlatformDevelopmentID
                                            FULL JOIN UserInfo user_SCID On user_SCID.userid = p.SupplyChainID
                                            FULL JOIN UserInfo user_ODMSEPM On user_ODMSEPM.userid = p.ODMSEPMID
                                            FULL JOIN UserInfo user_CM On user_CM.userid = p.PMID 
                                            FULL JOIN UserInfo user_CPM On user_CPM.userid = p.PDEID 
                                            FULL JOIN UserInfo user_Service On user_Service.userid = p.ServiceID 
                                            FULL JOIN UserInfo user_ODMHWPM On user_ODMHWPM.userid = p.ODMHWPMID
                                            FULL JOIN UserInfo user_POPM On user_POPM.userid = p.TDCCMID
                                            FULL JOIN UserInfo user_Quality On user_Quality.userid = p.QualityID
                                            FULL JOIN UserInfo user_PPM On user_PPM.userid = p.PlanningPMID 
                                            FULL JOIN UserInfo user_BIOSPM On user_BIOSPM.userid = p.BIOSLeadID 
                                            FULL JOIN UserInfo user_SEPM On user_SEPM.userid = p.SEPMID 
                                            FULL JOIN UserInfo user_MPM On user_MPM.userid = p.ConsMarketingID 
                                            FULL JOIN UserInfo user_ProPM On user_ProPM.userid = p.ProcurementPMID 
                                            FULL JOIN UserInfo user_SWM On user_SWM.userid = p.SwMarketingId
                                            Full JOIN ProductLine pl ON pl.Id = p.ProductLineId
                                            WHERE ps.Name <> 'Inactive' and p.FusionRequirements = 1 and p.id =";

    }
}
