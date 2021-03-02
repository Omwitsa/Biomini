using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Net;
using Suprema;

namespace BioMini_BioStar2
{
    class Program
    {

        static void Main(string[] args)
        {

            Program program = new Program();
            program.run();
        }

        public void run()
        {

            const int MAX_TEMPLATE_SIZE = 384;

            UFS_STATUS ufs_res;
            UFScannerManager ScannerManager;
            int nScannerNumber;

            ScannerManager = new UFScannerManager(null);

            ufs_res = ScannerManager.Init();
            nScannerNumber = ScannerManager.Scanners.Count;

            UFScanner Scanner = null;
            Scanner = ScannerManager.Scanners[0];


                //Scanner.Timeout = 7000;
                //Scanner.TemplateSize = MAX_TEMPLATE_SIZE;
                //Scanner.DetectCore = false;

            //byte[] Template = new byte[MAX_TEMPLATE_SIZE];
            byte[] Template0 = new byte[MAX_TEMPLATE_SIZE];
            byte[] Template1 = new byte[MAX_TEMPLATE_SIZE];

            int TemplateSize;
            int EnrollQuality;








            IntPtr versionPtr = API.BS2_Version();
            Console.WriteLine("SDK version : {0}", Marshal.PtrToStringAnsi(versionPtr));
            IntPtr sdkContext = IntPtr.Zero;

            sdkContext = API.BS2_AllocateContext();
            if (sdkContext == IntPtr.Zero)
            {
                Console.WriteLine("BS2_AllocateContext Failed.");
                return;
            }

            BS2ErrorCode result = (BS2ErrorCode)API.BS2_Initialize(sdkContext);
            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                Console.WriteLine("BS2_Initialize failed. Error : {0}", result);
                API.BS2_ReleaseContext(sdkContext);
                return;
            }

            // Connect to via Device IP address
            string ipAddress = "192.168.16.224";
            ushort port = 51211;
            uint deviceId = 0;

            //ADD------ BS_SDK_ERROR_CANNOT_CONNECT_SOCKET error test
            IntPtr ptrIPAddr = Marshal.StringToHGlobalAnsi(ipAddress);


            //result = (BS2ErrorCode)API.BS2_ConnectDeviceViaIP(context, ipAddress, port, out deviceId);
            result = (BS2ErrorCode)API.BS2_ConnectDeviceViaIP(sdkContext, ptrIPAddr, port, out deviceId);


            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                Console.WriteLine("Connecting to device failed : {0}", result);
                return;
            }

            Marshal.FreeHGlobal(ptrIPAddr);
            Console.WriteLine("Connected");

            BS2SimpleDeviceInfo deviceInfo;
            result = (BS2ErrorCode)API.BS2_GetDeviceInfo(sdkContext, deviceId, out deviceInfo);
            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                Console.WriteLine("BS2_GetDeviceInfo failed. Error : {0}", result);
                return;
            }









            BS2FingerprintTemplateFormatEnum templateFormat = BS2FingerprintTemplateFormatEnum.FORMAT_SUPREMA;
            SortedSet<BS2CardAuthModeEnum> privateCardAuthMode = new SortedSet<BS2CardAuthModeEnum>();
            SortedSet<BS2FingerAuthModeEnum> privateFingerAuthMode = new SortedSet<BS2FingerAuthModeEnum>();
            SortedSet<BS2IDAuthModeEnum> privateIDAuthMode = new SortedSet<BS2IDAuthModeEnum>();

            bool cardSupported = Convert.ToBoolean(deviceInfo.cardSupported);
            bool fingerSupported = Convert.ToBoolean(deviceInfo.fingerSupported);
            bool pinSupported = Convert.ToBoolean(deviceInfo.pinSupported);

            privateIDAuthMode.Add(BS2IDAuthModeEnum.PROHIBITED);

            if (cardSupported)
            {
                privateCardAuthMode.Add(BS2CardAuthModeEnum.PROHIBITED);
                privateCardAuthMode.Add(BS2CardAuthModeEnum.CARD_ONLY);

                if (pinSupported)
                {
                    privateCardAuthMode.Add(BS2CardAuthModeEnum.CARD_PIN);

                    privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_PIN);

                    if (fingerSupported)
                    {
                        privateCardAuthMode.Add(BS2CardAuthModeEnum.CARD_BIOMETRIC_OR_PIN);
                        privateCardAuthMode.Add(BS2CardAuthModeEnum.CARD_BIOMETRIC_PIN);

                        privateFingerAuthMode.Add(BS2FingerAuthModeEnum.BIOMETRIC_PIN);

                        privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_BIOMETRIC_OR_PIN);
                        privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_BIOMETRIC_PIN);
                    }
                }

                if (fingerSupported)
                {
                    privateCardAuthMode.Add(BS2CardAuthModeEnum.CARD_BIOMETRIC);

                    privateFingerAuthMode.Add(BS2FingerAuthModeEnum.BIOMETRIC_ONLY);

                    privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_BIOMETRIC);
                }
            }
            else if (fingerSupported)
            {
                if (pinSupported)
                {
                    privateFingerAuthMode.Add(BS2FingerAuthModeEnum.BIOMETRIC_PIN);

                    privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_BIOMETRIC_OR_PIN);
                    privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_BIOMETRIC_PIN);
                }

                privateFingerAuthMode.Add(BS2FingerAuthModeEnum.BIOMETRIC_ONLY);

                privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_BIOMETRIC);
            }
            else if (pinSupported)
            {
                privateIDAuthMode.Add(BS2IDAuthModeEnum.ID_PIN);
            }

            BS2UserBlob[] userBlob = Util.AllocateStructureArray<BS2UserBlob>(1);
            userBlob[0].user.version = 0;
            userBlob[0].user.formatVersion = 0;
            userBlob[0].user.faceChecksum = 0;
            userBlob[0].user.fingerChecksum = 0;
            userBlob[0].user.numCards = 0;
            userBlob[0].user.numFingers = 0;
            userBlob[0].user.numFaces = 0;

            userBlob[0].cardObjs = IntPtr.Zero;
            userBlob[0].fingerObjs = IntPtr.Zero;
            userBlob[0].faceObjs = IntPtr.Zero;

            Console.WriteLine("Enter the ID for the User which you want to enroll");
            Console.Write(">>>> ");
            string userID = Console.ReadLine();
            if (userID.Length == 0)
            {
                Console.WriteLine("The user id can not be empty.");
                return;
            }
            else if (userID.Length > BS2Environment.BS2_USER_ID_SIZE)
            {
                Console.WriteLine("The user id should less than {0} words.", BS2Environment.BS2_USER_ID_SIZE);
                return;
            }
            else
            {
                //TODO Alphabet user id is not implemented yet.
                UInt32 uid;
                if (!UInt32.TryParse(userID, out uid))
                {
                    Console.WriteLine("The user id should be a numeric.");
                    return;
                }

                byte[] userIDArray = Encoding.UTF8.GetBytes(userID);
                Array.Clear(userBlob[0].user.userID, 0, BS2Environment.BS2_USER_ID_SIZE);
                Array.Copy(userIDArray, userBlob[0].user.userID, userIDArray.Length);
            }

            Console.WriteLine("When is this user valid from? [default(Today), yyyy-MM-dd HH:mm:ss]");
            Console.Write(">>>> ");
            //if (!Util.GetTimestamp("yyyy-MM-dd HH:mm:ss", 0, out userBlob[0].setting.startTime))
            //{
            //    return;
            //}
            userBlob[0].setting.startTime = 1262307661; //2010-01-01 01:01:01

            Console.WriteLine("When is this user valid to? [default(Today), yyyy-MM-dd HH:mm:ss]");
            Console.Write(">>>> ");
            //if (!Util.GetTimestamp("yyyy-MM-dd HH:mm:ss", 0, out userBlob[0].setting.endTime))
            //{
            //    return;
            //}
            userBlob[0].setting.endTime = 1893459661; //2030-01-01 01:01:01

            if (fingerSupported)
            {
                Console.WriteLine("Enter the security level for this user: [{0}: {1}, {2}: {3}, {4}: {5}(default), {6}: {7}, {8}: {9}]",
                                (byte)BS2UserSecurityLevelEnum.LOWER,
                                BS2UserSecurityLevelEnum.LOWER,
                                (byte)BS2UserSecurityLevelEnum.LOW,
                                BS2UserSecurityLevelEnum.LOW,
                                (byte)BS2UserSecurityLevelEnum.NORMAL,
                                BS2UserSecurityLevelEnum.NORMAL,
                                (byte)BS2UserSecurityLevelEnum.HIGH,
                                BS2UserSecurityLevelEnum.HIGH,
                                (byte)BS2UserSecurityLevelEnum.HIGHER,
                                BS2UserSecurityLevelEnum.HIGHER);
                Console.Write(">>>> ");
                userBlob[0].setting.securityLevel = Util.GetInput((byte)BS2UserSecurityLevelEnum.NORMAL);

                userBlob[0].setting.fingerAuthMode = (byte)BS2FingerAuthModeEnum.NONE;



                Array.Clear(userBlob[0].name, 0, BS2Environment.BS2_USER_NAME_LEN);

                userBlob[0].photo.size = 0;
                Array.Clear(userBlob[0].photo.data, 0, BS2Environment.BS2_USER_PHOTO_SIZE);
                Array.Clear(userBlob[0].pin, 0, BS2Environment.BS2_PIN_HASH_SIZE);



                if (fingerSupported)
                {
                    Console.WriteLine("How many fingerprints do you want to register? [1(default) - {0}]", BS2Environment.BS2_MAX_NUM_OF_FINGER_PER_USER);
                    Console.Write(">>>> ");
                    userBlob[0].user.numFingers = Util.GetInput((byte)1);

                    if (userBlob[0].user.numFingers > 0)
                    {
                        BS2FingerprintConfig fingerprintConfig;
                        Console.WriteLine("Trying to get fingerprint config");
                        result = (BS2ErrorCode)API.BS2_GetFingerprintConfig(sdkContext, deviceId, out fingerprintConfig);
                        if (result != BS2ErrorCode.BS_SDK_SUCCESS)
                        {
                            Console.WriteLine("Got error({0}).", result);
                            return;
                        }
                        else
                        {
                            templateFormat = (BS2FingerprintTemplateFormatEnum)fingerprintConfig.templateFormat;
                        }

                        int structSize = Marshal.SizeOf(typeof(BS2Fingerprint));
                        BS2Fingerprint fingerprint = Util.AllocateStructure<BS2Fingerprint>();
                        userBlob[0].fingerObjs = Marshal.AllocHGlobal(structSize * userBlob[0].user.numFingers);
                        IntPtr curFingerObjs = userBlob[0].fingerObjs;

                        /*
                         Console.WriteLine("Place your finger");
                         ufs_res = Scanner.ClearCaptureImageBuffer();
                         ufs_res = Scanner.CaptureSingleImage();
                         ufs_res = Scanner.ExtractEx(MAX_TEMPLATE_SIZE, Template, out TemplateSize, out EnrollQuality);
                         byte[] template0 = Template;

                         Console.WriteLine("Place your finger once more");
                         ufs_res = Scanner.ClearCaptureImageBuffer();
                         ufs_res = Scanner.CaptureSingleImage();
                         ufs_res = Scanner.ExtractEx(MAX_TEMPLATE_SIZE, Template, out TemplateSize, out EnrollQuality);
                         byte[] template1 = Template;

                         System.Buffer.BlockCopy(template0, 0, fingerprint.data, 0, template0.Length);
                         System.Buffer.BlockCopy(template1, 0, fingerprint.data, template0.Length, template1.Length);
                         */


                        Console.WriteLine("Place your finger");
                        ufs_res = Scanner.ClearCaptureImageBuffer();
                        ufs_res = Scanner.CaptureSingleImage();

                        if (ufs_res == UFS_STATUS.OK)
                        {
                            Console.WriteLine("1st capturing succeeded");

                        }
                        else
                        {
                            Console.WriteLine("1st capturing failed");
                            return;
                        }

                        ufs_res = Scanner.ExtractEx(MAX_TEMPLATE_SIZE, Template0, out TemplateSize, out EnrollQuality);
                        
                        if (ufs_res == UFS_STATUS.OK)
                        {
                            Console.WriteLine("1st extraction succeeded");

                        }
                        else
                        {
                            Console.WriteLine("1st extraction failed");
                            return;
                        }
                        Console.WriteLine("Place your finger once more");



                        ufs_res = Scanner.ClearCaptureImageBuffer();
                        ufs_res = Scanner.CaptureSingleImage();
                        if (ufs_res == UFS_STATUS.OK)
                        {
                            Console.WriteLine("2nd capturing succeeded");

                        }
                        else
                        {
                            Console.WriteLine("2nd capturing failed");
                            return;
                        }


                        ufs_res = Scanner.ExtractEx(MAX_TEMPLATE_SIZE, Template1, out TemplateSize, out EnrollQuality);

                        if (ufs_res == UFS_STATUS.OK)
                        {
                            Console.WriteLine("2nd extraction succeeded");

                        }
                        else
                        {
                            Console.WriteLine("2nd extraction failed");
                            return;
                        }

                        System.Buffer.BlockCopy(Template0, 0, fingerprint.data, 0, Template0.Length);
                        System.Buffer.BlockCopy(Template1, 0, fingerprint.data, Template0.Length, Template1.Length);                        
                        fingerprint.flag = 0; //Not Duress Finger
                        fingerprint.index = 0; //1st Finger

                        

                        Array.Clear(userBlob[0].accessGroupId, 0, BS2Environment.BS2_MAX_ACCESS_GROUP_PER_USER);

                        Console.WriteLine("Which access groups does this user belongs to? [ex. ID_1 ID_2 ...]");
                        Console.Write(">>>> ");
                        int accessGroupIdIndex = 0;
                        char[] delimiterChars = { ' ', ',', '.', ':', '\t' };
                        string[] accessGroupIDs = Console.ReadLine().Split(delimiterChars);

                        foreach (string accessGroupID in accessGroupIDs)
                        {
                            if (accessGroupID.Length > 0)
                            {
                                UInt32 item;
                                if (UInt32.TryParse(accessGroupID, out item))
                                {
                                    userBlob[0].accessGroupId[accessGroupIdIndex++] = item;
                                }
                            }
                        }

                        Marshal.StructureToPtr(fingerprint, curFingerObjs, false);
                        curFingerObjs += structSize;

                        result = (BS2ErrorCode)API.BS2_EnrolUser(sdkContext, deviceId, userBlob, 1, 1);
                        if (result != BS2ErrorCode.BS_SDK_SUCCESS)
                        {
                            Console.WriteLine("Failed : {0}", result);
                            return;
                        }
                    }
                }
















            }
        }

    }
}
