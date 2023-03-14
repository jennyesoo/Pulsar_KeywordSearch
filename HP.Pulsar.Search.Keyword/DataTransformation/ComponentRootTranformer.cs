using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    internal class ComponentRootTranformer : IDataTranformer
    {
        public static List<string> DataPropertyList = new List<string> { "created" , "muiawareDate" , "updated" };

        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentRoots)
        {
            foreach (CommonDataModel root in componentRoots)
            {
                foreach (string key in root.GetAllKeys())
                {
                    root.Add(key, DataProcessingInitializationCombination(root.GetValue(key), key));
                }
            }

            return componentRoots;
        }

        private string DataProcessingInitializationCombination(string propertyValue, string propertyName)
        {
            if (DataPropertyList.Contains(propertyName.ToLower()))
            {
                propertyValue = ChangeDateFormat(propertyValue);
            }

            propertyValue = AddPropertyName(propertyName, propertyValue);

            return propertyValue;
        }

        private string ChangeDateFormat(string propertyValue)
        {
            return propertyValue.Split(" ")[0];
        }

        private string AddPropertyName(string propertyName, string propertyValue)
        {
            if (string.Equals(propertyName, "ComponentRootId"))
            {
                return "Component Root Id : " + propertyValue;
            }
            else if (propertyName == "ComponentRootName")
            {
                return "Component Root Name : " + propertyValue;
            }
            else if (propertyName == "description")
            {
                return "description : " + propertyValue;
            }
            else if (propertyName == "VendorName")
            {
                return "Vendor Name : " + propertyValue;
            }
            else if (propertyName == "SIFunctionTestGroupCategory")
            {
                return "SI Function Test Group Category : " + propertyValue;
            }
            else if (propertyName == "ComponentPM")
            {
                return "Component PM : " + propertyValue;
            }
            else if (propertyName == "Developer")
            {
                return "Developer Name : " + propertyValue;
            }
            else if (propertyName == "TestLead")
            {
                return "Tester Lead : " + propertyValue;
            }
            else if (propertyName == "SICoreTeam")
            {
                return "SI Core Team : " + propertyValue;
            }
            else if (propertyName == "ComponentType")
            {
                return "Component Type : " + propertyValue;
            }
            else if (propertyName == "BasePartNumber")
            {
                return "Base Part Number : " + propertyValue;
            }
            else if (propertyName == "OtherDependencies")
            {
                return "Other Dependencies : " + propertyValue;
            }
            else if (propertyName == "Preinstall")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "DropInBox")
            {
                return "Drop In Box : " + propertyValue;
            }
            else if (propertyName == "Visibility")
            {
                return "Visibility : " + propertyValue;
            }
            else if (propertyName == "RootFilename")
            {
                return "ROM Family : " + propertyValue;
            }
            else if (propertyName == "TargetPartition")
            {
                return "Target Partition : " + propertyValue;
            }
            else if (propertyName == "Install")
            {
                return "Install : " + propertyValue;
            }
            else if (propertyName == "SilentInstall")
            {
                return "Silent Install : " + propertyValue;
            }
            else if (propertyName == "ARCDInstall")
            {
                return "ARCD Installe : " + propertyValue;
            }
            else if (propertyName == "Reboot")
            {
                return "Reboot : " + propertyValue;
            }
            else if (propertyName == "Replicater")
            {
                return "CDs Replicated By : " + propertyValue;
            }
            else if (propertyName == "Binary")
            {
                return "ROM Components : " + propertyValue;
            }
            else if (propertyName == "CAB")
            {
                return "ROM Components : " + propertyValue;
            }
            else if (propertyName == "FloppyDisk")
            {
                return "Packaging  : " + propertyValue;
            }
            else if (propertyName == "PreinstallROM")
            {
                return "ROM Components : " + propertyValue;
            }
            else if (propertyName == "SysReboot")
            {
                return "SysReboot : " + propertyValue;
            }
            else if (propertyName == "Admin")
            {
                return "Admin : " + propertyValue;
            }
            else if (propertyName == "WHQLCertificationRequire")
            {
                return "WHQL Certification Require : " + propertyValue;
            }
            else if (propertyName == "PNPDevices")
            {
                return "PNPDevices : " + propertyValue;
            }
            else if (propertyName == "ScriptPaq")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "Softpaq")
            {
                return "ROM Components  : " + propertyValue;
            }
            else if (propertyName == "MultiLanguage")
            {
                return "Multi Language : " + propertyValue;
            }
            else if (propertyName == "SoftpaqCategory")
            {
                return "Softpaq Category : " + propertyValue;
            }
            else if (propertyName == "Created")
            {
                return "Created : " + propertyValue;
            }
            else if (propertyName == "IconDesktop")
            {
                return "Touch Points  : " + propertyValue;
            }
            else if (propertyName == "IconMenu")
            {
                return "Touch Points : " + propertyValue;
            }
            else if (propertyName == "IconTray")
            {
                return "Touch Points : " + propertyValue;
            }
            else if (propertyName == "IconPanel")
            {
                return "Touch Points : " + propertyValue;
            }
            else if (propertyName == "PackageForWeb")
            {
                return "Package For Webe : " + propertyValue;
            }
            else if (propertyName == "PropertyTabs")
            {
                return "Property Tabs : " + propertyValue;
            }
            else if (propertyName == "PostRTMStatus")
            {
                return "Post RTM Status : " + propertyValue;
            }
            else if (propertyName == "SubAssembly")
            {
                return "SubAssemblye : " + propertyValue;
            }
            else if (propertyName == "DeveloperNotification")
            {
                return "Developer Notification : " + propertyValue;
            }
            else if (propertyName == "RoyaltyBearing")
            {
                return "Special Notes : " + propertyValue;
            }
            else if (propertyName == "RecoveryOption")
            {
                return "Recovery Option : " + propertyValue;
            }
            else if (propertyName == "KitNumber")
            {
                return "Kit Number : " + propertyValue;
            }
            else if (propertyName == "KitDescription")
            {
                return "Kit Description : " + propertyValue;
            }
            else if (propertyName == "FunctionalSpec")
            {
                return "Functional Spec : " + propertyValue;
            }
            else if (propertyName == "MUIAware")
            {
                return "MUIAware : " + propertyValue;
            }
            else if (propertyName == "MUIAwareDate")
            {
                return "MUIAware Date : " + propertyValue;
            }
            else if (propertyName == "IconInfoCenter")
            {
                return "Touch Points : " + propertyValue;
            }
            else if (propertyName == "Notifications")
            {
                return "Notifications : " + propertyValue;
            }
            else if (propertyName == "OEMReadyRequired")
            {
                return "OEM Ready Required : " + propertyValue;
            }
            else if (propertyName == "OEMReadyException")
            {
                return "OEM Ready Exceptionl : " + propertyValue;
            }
            else if (propertyName == "HPPI")
            {
                return "HPPI : " + propertyValue;
            }
            else if (propertyName == "OTSFVTOrganizationID")
            {
                return "OTSFVT Organization : " + propertyValue;
            }
            else if (propertyName == "TransferServer")
            {
                return "Transfer Server : " + propertyValue;
            }
            else if (propertyName == "SoftpaqInstructions")
            {
                return "Softpaq Instructionss : " + propertyValue;
            }
            else if (propertyName == "LockReleases")
            {
                return "Lock Releases : " + propertyValue;
            }
            else if (propertyName == "ReturnCodes")
            {
                return "Return Codes  : " + propertyValue;
            }
            else if (propertyName == "OsCode")
            {
                return "Os Code : " + propertyValue;
            }
            else if (propertyName == "ShowOnStatus")
            {
                return "Show On Status : " + propertyValue;
            }
            else if (propertyName == "InitialOfferingStatus")
            {
                return "Initial Offering Status : " + propertyValue;
            }
            else if (propertyName == "InitialOfferingChangeStatus")
            {
                return "Initial Offering Change Status : " + propertyValue;
            }
            else if (propertyName == "InitialOfferingChangeStatusArchive")
            {
                return "Initial Offering Change Status Archive : " + propertyValue;
            }
            else if (propertyName == "LTFAVNo")
            {
                return "LTFAVNo : " + propertyValue;
            }
            else if (propertyName == "LTFSubassemblyNo")
            {
                return "LTFSubassemblyNo  : " + propertyValue;
            }
            else if (propertyName == "Patch")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "SystemBoardID")
            {
                return "System Board ID : " + propertyValue;
            }
            else if (propertyName == "IRSTransfers")
            {
                return "IRS Transfers : " + propertyValue;
            }
            else if (propertyName == "DevicesInfPath")
            {
                return "Devices Infomation Path  : " + propertyValue;
            }
            else if (propertyName == "TestNotes")
            {
                return "Test Note : " + propertyValue;
            }
            else if (propertyName == "CreatedBy")
            {
                return "Created By : " + propertyValue;
            }
            else if (propertyName == "Updated")
            {
                return "Updated : " + propertyValue;
            }
            else if (propertyName == "UpdatedBy")
            {
                return "Updated By : " + propertyValue;
            }
            else if (propertyName == "Deleted")
            {
                return "Deleted : " + propertyValue;
            }
            else if (propertyName == "DeletedBy")
            {
                return "Deleted By : " + propertyValue;
            }
            else if (propertyName == "SupplierID")
            {
                return "Supplier : " + propertyValue;
            }
            else if (propertyName == "SIOApprover")
            {
                return "SIO Approver : " + propertyValue;
            }
            else if (propertyName == "KoreanCertificationRequired")
            {
                return "Special Notes : " + propertyValue;
            }
            else if (propertyName == "BaseFilePath")
            {
                return "Base File Path : " + propertyValue;
            }
            else if (propertyName == "SubmissionPath")
            {
                return "Submission Path : " + propertyValue;
            }
            else if (propertyName == "IRSPartNumberLastSpin")
            {
                return "IRS Part Number Last Spin : " + propertyValue;
            }
            else if (propertyName == "IRSBasePartNumber")
            {
                return "IRS Base Part Number : " + propertyValue;
            }
            else if (propertyName == "IrsComponentCloneId")
            {
                return "Irs Component Clone : " + propertyValue;
            }
            else if (propertyName == "PrismSWType")
            {
                return "Prism SW Type : " + propertyValue;
            }
            else if (propertyName == "LimitFuncTestGroupVisability")
            {
                return "Limit Function Test Group Visability : " + propertyValue;
            }
            else if (propertyName == "NamingStandard")
            {
                return "Naming Standard : " + propertyValue;
            }
            else if (propertyName == "ML")
            {
                return "ML : " + propertyValue;
            }
            else if (propertyName == "AgencyLead")
            {
                return "Agency Lead : " + propertyValue;
            }
            else if (propertyName == "SortOrder")
            {
                return "Sort Order : " + propertyValue;
            }
            else if (propertyName == "IconTile")
            {
                return "Touch Points  : " + propertyValue;
            }
            else if (propertyName == "IconTaskBarIcon")
            {
                return "Touch Points : " + propertyValue;
            }
            else if (propertyName == "SettingFWML")
            {
                return "Other Setting : " + propertyValue;
            }
            else if (propertyName == "SettingUWPCompliant")
            {
                return "Other Setting : " + propertyValue;
            }
            else if (propertyName == "FtpSitePath")
            {
                return "Ftp Site Path : " + propertyValue;
            }
            else if (propertyName == "DrDvd")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "MsStore")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "ErdComments")
            {
                return "Erd Comments : " + propertyValue;
            }
            else if (propertyName == "IsAutoTagingEnabled")
            {
                return "Is Auto Taging Enabled : " + propertyValue;
            }
            else if (propertyName == "IsSoftPaqInPreinstall")
            {
                return "Is SoftPaq In Preinstall : " + propertyValue;
            }
            else if (propertyName == "Partner2Id")
            {
                return "Partner2Id : " + propertyValue;
            }
            else if (propertyName == "ProductList")
            {
                return "Product : " + propertyValue;
            }
            else if (propertyName == "target")
            {
                return propertyValue;
            }
            else if (propertyName == "Id")
            {
                return propertyValue;
            }
            return propertyValue;
        }
    }
}
