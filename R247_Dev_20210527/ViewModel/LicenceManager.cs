using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;


namespace LicenceManager
{

    public class Licence
    {
        static Licence _licence;
        public static Licence Instance
        {
            get
            {
                if (_licence == null)
                {
                    _licence = new Licence();
                    return _licence;
                }
                else
                {
                    return _licence;
                }
            }
        }
        string licence_key = "";
        string camera_key = "";
        public string LicenceKey
        {
            get { return licence_key; }
        }

        private Licence()
        {
            RegistryKey SoftwareKey = Registry.LocalMachine.OpenSubKey("Software", true);

            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("NOVISIONDesigner");
            RegistryKey AppVersionKey = AppNameKey.CreateSubKey("version 1.0");

            if (AppVersionKey.GetValue("licence_key") == null)
            {
                AppVersionKey.SetValue("licence_key", "");
                licence_key = "";
            }
            else
            {
                licence_key = AppVersionKey.GetValue("licence_key") as string;
            }

            if (AppVersionKey.GetValue("camera_key") == null)
            {
                AppVersionKey.SetValue("camera_key", "");
                camera_key = "";
            }
            else
            {
                licence_key = AppVersionKey.GetValue("licence_key") as string;
                camera_key = AppVersionKey.GetValue("camera_key") as string;
            }

        }

        public void SetLicenceKey(string key)
        {
            RegistryKey SoftwareKey = Registry.LocalMachine.OpenSubKey("Software", true);

            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("NOVISIONDesigner");
            RegistryKey AppVersionKey = AppNameKey.CreateSubKey("version 1.0");

            if (AppVersionKey.GetValue("licence_key") != null)
            {
                AppVersionKey.SetValue("licence_key", key);
                licence_key = key;
            }
            else
            {
                AppVersionKey.SetValue("licence_key", key);
                //licence_key = AppVersionKey.GetValue("licence_key") as string;
                licence_key = key;
            }
        }

        public void SetCameraKey(string key)
        {
            RegistryKey SoftwareKey = Registry.LocalMachine.OpenSubKey("Software", true);

            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("NOVISIONDesigner");
            RegistryKey AppVersionKey = AppNameKey.CreateSubKey("version 1");

            if (AppVersionKey.GetValue("camera_key") != null)
            {
                AppVersionKey.SetValue("camera_key", key);
                camera_key = key;
            }
            else
            {
                AppVersionKey.SetValue("camera_key", key);
                //licence_key = AppVersionKey.GetValue("licence_key") as string;
                camera_key = key;
            }
        }
        public bool ValidateCamera(string cameraID)
        {
            if (CalculateMD5Hash(cameraID + "NOVisionDesigner@123") == camera_key.ToUpper())
            {
                return true;
            }
            return false;
        }
        public bool ValidateLicence()
        {
            foreach (string id in GetCPUID())
            {
                if (CalculateMD5Hash(id + "NOVisionDesigner@123") == licence_key.ToUpper())
                {
                    return true;
                }
            }
            return false;
        }
        public string CalculateMD5Hash(string input)

        { 

            // step 1, calculate MD5 hash from input

            MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)

            {

                sb.Append(hash[i].ToString("X2"));

            }

            return sb.ToString();

        }
        public List<string> GetCPUID()
        {
            List<string> result = new List<string>();
            System.Management.ManagementClass theClass = new System.Management.ManagementClass("Win32_Processor");
            System.Management.ManagementObjectCollection theCollectionOfResults = theClass.GetInstances();

            foreach (System.Management.ManagementObject currentResult in theCollectionOfResults)
            {
                result.Add(currentResult["ProcessorID"].ToString());
            }
            return result;
        }
    }
}
