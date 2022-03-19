using Advantech.Motion;
using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace TestSwLimit
{
    public static class Extensions
    {
        public static void CheckResult(this uint result)
        {
            if (result != 0)
            {
                var sb = new StringBuilder(50);
                Motion.mAcm_GetErrorMessage(result, sb, 50);
                throw new Exception($"{sb} Error Code: [0x{result:X}]");
            }
        }
    }
    internal class Program
    {
        private static uint deviceCount;
        private static IntPtr m_DeviceHandle;
        private static IntPtr[] m_Axishand;

        public static DEV_LIST[] CurAvailableDevs { get; private set; }

        public static uint AxesPerDev;

        private static Boolean GetDevCfgDllDrvVer()
        {
            string fileName = "";
            FileVersionInfo myFileVersionInfo;
            string FileVersion = "";
            fileName = Environment.SystemDirectory + "\\ADVMOT.dll";//SystemDirectory指System32 
            myFileVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
            FileVersion = myFileVersionInfo.FileVersion;
            string DetailMessage;
            string[] strSplit = FileVersion.Split(',');
            if (Convert.ToUInt16(strSplit[0]) < 2)
            {

                DetailMessage = "The Driver Version  Is Too Low" + "\r\nYou can update the driver through the driver installation package ";
                DetailMessage = DetailMessage + "\r\nThe Current Driver Version Number is " + FileVersion;
                DetailMessage = DetailMessage + "\r\nYou need to update the driver to 2.0.0.0 version and above";
                MessageBox.Show(DetailMessage, "DIO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
        private static void ShowMessages(string DetailMessage, uint errorCode)
        {
            StringBuilder ErrorMsg = new StringBuilder("", 100);
            //Get the error message according to error code returned from API
            Boolean res = Motion.mAcm_GetErrorMessage(errorCode, ErrorMsg, 100);
            string ErrorMessage = "";
            if (res)
                ErrorMessage = ErrorMsg.ToString();
            MessageBox.Show(DetailMessage + "\r\nError Message:" + ErrorMessage, "DIO", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void Main(string[] args)
        {
            var VersionIsOk = GetDevCfgDllDrvVer(); //Get Driver Version Number, this step is not necessary

            uint Result;
            string strTemp;
            if (VersionIsOk == false)
            {
                return;
            }
            // Get the list of available device numbers and names of devices, of which driver has been loaded successfully 
            //If you have two/more board,the device list(m_avaDevs) may be changed when the slot of the boards changed,for example:m_avaDevs[0].szDeviceName to PCI-1245
            //m_avaDevs[1].szDeviceName to PCI-1245L,changing the slot，Perhaps the opposite 
            CurAvailableDevs = new DEV_LIST[Motion.MAX_DEVICES];

            Result = (uint)Motion.mAcm_GetAvailableDevs(CurAvailableDevs, Motion.MAX_DEVICES, ref deviceCount);
            if (Result != (int)ErrorCode.SUCCESS)
            {
                strTemp = "Get Device Numbers Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, (uint)Result);
                return;
            }
            //If you want to get the device number of fixed equipment，you also can achieve it By adding the API:GetDevNum(UInt32 DevType, UInt32 BoardID, UInt32 MasterRingNo, UInt32 SlaveBoardID),
            //The API is defined and illustrates the way of using in this example,but it is not called,you can copy it to your program and
            //don't need to call Motion.mAcm_GetAvailableDevs(CurAvailableDevs, Motion.MAX_DEVICES, ref deviceCount)
            //GetDevNum(UInt32 DevType, UInt32 BoardID, UInt32 MasterRingNo, UInt32 SlaveBoardID) API Variables are stated below:
            //UInt32 DevType : Set Device Type ID of your motion card plug in PC. (Definition is in ..\Public\AdvMotDev.h)
            //UInt32 BoardID : Set Hardware Board-ID of your motion card plug in PC,you can get it from Utility
            //UInt32 MasterRingNo: PCI-Motion card, Always set to 0
            //UInt32 SlaveBoardID : PCI-Motion card,Always set to 0

            var DeviceNum = CurAvailableDevs[0].DeviceNum;
            Result = Motion.mAcm_DevOpen(DeviceNum, ref m_DeviceHandle);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Open Device Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return;
            }
            Result = Motion.mAcm_GetU32Property(m_DeviceHandle, (uint)PropertyID.FT_DevAxesCount, ref AxesPerDev);
            if (Result != (uint)ErrorCode.SUCCESS)
            {
                strTemp = "Get Axis Number Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                ShowMessages(strTemp, Result);
                return;
            }
            var m_ulAxisCount = AxesPerDev;
            m_Axishand = new IntPtr[m_ulAxisCount];
            for (int i = 0; i < m_ulAxisCount; i++)
            {
                //Open every Axis and get the each Axis Handle
                //And Initial property for each Axis 		
                //Open Axis 
                Result = Motion.mAcm_AxOpen(m_DeviceHandle, (UInt16)i, ref m_Axishand[i]);
                if (Result != (uint)ErrorCode.SUCCESS)
                {
                    strTemp = "Open Axis Failed With Error Code: [0x" + Convert.ToString(Result, 16) + "]";
                    ShowMessages(strTemp, Result);
                    return;
                }
            }
            ushort status = 0;
            Motion.mAcm_AxResetError(m_Axishand[0]);
            Motion.mAcm_AxGetState(m_Axishand[0], ref status);
            //AxisState.
            Motion.mAcm_AxSetCmdPosition(m_Axishand[0], 3).CheckResult();
            var axisNum = 0;
            var buf = (uint)SwLmtEnable.SLMT_DIS;
            Motion.mAcm_SetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelEnable, ref buf, 4).CheckResult();
            Motion.mAcm_SetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwMelEnable, ref buf, 4).CheckResult();
            Motion.mAcm_AxResetError(m_Axishand[axisNum]).CheckResult();
            buf = (uint)SwLmtReact.SLMT_IMMED_STOP;
            Motion.mAcm_SetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelReact, ref buf, 4).CheckResult();
            Motion.mAcm_SetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwMelReact, ref buf, 4).CheckResult();
            var pos = 5;
            Motion.mAcm_SetF64Property(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelValue, 5).CheckResult();//.mAcm_SetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelValue, ref pos, 4).CheckResult();
            int getPos = 0;
            uint bufL = 8;
            double gP = 0;
            Motion.mAcm_GetF64Property(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelValue, ref gP).CheckResult();
            Motion.mAcm_GetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelValue, ref getPos, ref bufL).CheckResult();
            buf = (uint)SwLmtEnable.SLMT_EN;
            Motion.mAcm_SetProperty(m_Axishand[axisNum], (uint)PropertyID.CFG_AxSwPelEnable, ref buf, 4).CheckResult();
        }
    }
}
