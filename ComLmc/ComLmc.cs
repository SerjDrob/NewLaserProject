using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace ComLmc;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
[Guid("20847F38-2C36-4AF2-A44B-FE4FD64B6FB2")]
public class ComLmc
{
    public ComLmc()
    {
    }
    public string GetErrorText(int nErr) => JczLmc.GetErrorText(nErr);
    public enum EzCad_Error_Code
    {
        LMC1_ERR_SUCCESS = 0, // Success
        LMC1_ERR_EZCADRUN, //1 // Find EZCAD running
        LMC1_ERR_NOFINDCFGFILE, //2 // Can not find EZCAD.CFG
        LMC1_ERR_FAILEDOPEN, //3 // Open LMC1 board failed
        LMC1_ERR_NODEVICE, //4 // Can not find valid lmc1 device
        LMC1_ERR_HARDVER, //5 // Lmc1’s version is error.
        LMC1_ERR_DEVCFG, //6 // Can not find configuration files
        LMC1_ERR_STOPSIGNAL, //7 // Alarm signal
        LMC1_ERR_USERSTOP, //8 // User stops
        LMC1_ERR_UNKNOW, //9 // Unknown error
        LMC1_ERR_OUTTIME, //10 // Overtime 
        LMC1_ERR_NOINITIAL, //11 // Un-initialized
        LMC1_ERR_READFILE, //12 // Read file error
        LMC1_ERR_OWENWNDNULL, //13 // Window handle is NULL
        LMC1_ERR_NOFINDFONT, //14 // Can not find designated font 
        LMC1_ERR_PENNO, //15 // Wrong pen number 
        LMC1_ERR_NOTTEXT, //16 // Object is not text 
        LMC1_ERR_SAVEFILE, //17 // Save file failed 
        LMC1_ERR_NOFINDENT, //18 // Can not find designated object
        LMC1_ERR_STATUE //19 // Can not run the operation in 
    }
    public string GetErrorText(int nErr, bool English) => JczLmc.GetErrorText(nErr, English);

    #region Йи±ё

    public int InitializeTotal(string PathName, bool bTestMode, IntPtr MailForm) => JczLmc.InitializeTotal(PathName, bTestMode, MailForm);
    public int Initialize(string PathName, bool bTestMode) => JczLmc.Initialize(PathName, bTestMode);
    public int Close() => JczLmc.Close();
    public int SetDevCfg() => JczLmc.SetDevCfg();
    public int SetDevCfg2(bool bAxisShow0, bool bAxisShow1) => JczLmc.SetDevCfg2(bAxisShow0, bAxisShow1);
    public int SetRotateMoveParam(double dMoveX, double dMoveY, double dCenterX, double dCenterY, double dRotateAng) => JczLmc.SetRotateMoveParam(dMoveX, dMoveY, dCenterX, dCenterY, dRotateAng);

    #endregion

    #region јУ№¤
    public int Mark(bool Fly) => JczLmc.Mark(Fly);
    public int MarkEntity(string EntName) => JczLmc.MarkEntity(EntName);
    public bool MES_Login(char[] EntName) => JczLmc.MES_Login(EntName);
    public bool MES_Init(char[] EntName) => JczLmc.MES_Init(EntName);
    public bool MES_LogReset() => JczLmc.MES_LogReset();
    public bool MES_Free(char[] EntName) => JczLmc.MES_Free(EntName);
    public bool MES_CheckSerialNum(string ent, char[] name) => JczLmc.MES_CheckSerialNum(ent, name);
    public int MarkFlyByStartSignal() => JczLmc.MarkFlyByStartSignal();
    public int MarkEntityFly(string EntName) => JczLmc.MarkEntityFly(EntName);
    public int MarkLine(double X1, double Y1, double X2, double Y2, int Pen) => JczLmc.MarkLine(X1, Y1, X2, Y2, Pen);
    public int MarkPoint(double X, double Y, double Delay, int Pen) => JczLmc.MarkPoint(X, Y, Delay, Pen);
    public int MarkPointBuf2(int nPointNum, [MarshalAs(UnmanagedType.LPArray)] double[,] ptbuf, double dJumpSpeed, double dLaserOnTimeMs)
                            => JczLmc.MarkPointBuf2(nPointNum, ptbuf, dJumpSpeed, dLaserOnTimeMs);
    public bool IsMarking() => JczLmc.IsMarking();
    public int StopMark() => JczLmc.StopMark();
    public int RedMark() => JczLmc.RedMark();
    public int RedMarkContour() => JczLmc.RedMarkContour();
    public int RedLightMarkByEnt(string EntName, bool bContour) => JczLmc.RedLightMarkByEnt(EntName, bContour);
    public int GetFlySpeed(ref double FlySpeed) => JczLmc.GetFlySpeed(ref FlySpeed);
    public int GotoPos(double x, double y) => JczLmc.GotoPos(x, y);
    public int GetCurCoor(ref double x, ref double y) => JczLmc.GetCurCoor(ref x, ref y);

    #endregion

    #region ОДјю

    public int LoadEzdFile(string FileName) => JczLmc.LoadEzdFile(FileName);
    public int SaveEntLibToFile(string strFileName) => JczLmc.SaveEntLibToFile(strFileName);
    internal bool DeleteObject(IntPtr hObject) => JczLmc.DeleteObject(hObject);
    internal IntPtr GetCurPrevBitmap(int bmpwidth, int bmpheight) => JczLmc.GetCurPrevBitmap(bmpwidth, bmpheight);
    public Image GetCurPreviewImage(int bmpwidth, int bmpheight) => JczLmc.GetCurPreviewImage(bmpwidth, bmpheight);
    internal IntPtr GetPrevBitmapByName(string EntName, int bmpwidth, int bmpheight) => JczLmc.GetPrevBitmapByName(EntName, bmpwidth, bmpheight);
    public Image GetCurPreviewImageByName(string EntName, int bmpwidth, int bmpheight) => JczLmc.GetCurPreviewImageByName(EntName, bmpwidth, bmpheight);

    #endregion

    #region ¶ФПу

    public int GetEntSize(string strEntName, ref double dMinx, ref double dMiny, ref double dMaxx, ref double dMaxy, ref double dz)
     => JczLmc.GetEntSize(strEntName, ref dMinx, ref dMiny, ref dMaxx, ref dMaxy, ref dz);
    public int MoveEnt(string strEntName, double dMovex, double dMovey) => JczLmc.MoveEnt(strEntName, dMovex, dMovey);
    public int ScaleEnt(string strEntName, double dCenx, double dCeny, double dScaleX, double dScaleY) => JczLmc.ScaleEnt(strEntName, dCenx, dCeny, dScaleX, dScaleY);
    public int MirrorEnt(string strEntName, double dCenx, double dCeny, bool bMirrorX, bool bMirrorY) => JczLmc.MirrorEnt(strEntName, dCenx, dCeny, bMirrorX, bMirrorY);
    public int RotateEnt(string strEntName, double dCenx, double dCeny, double dAngle) => JczLmc.RotateEnt(strEntName, dCenx, dCeny, dAngle);
    public int CopyEnt(string strEntName, string strNewEntName) => JczLmc.CopyEnt(strEntName,strNewEntName);
    internal int lmc1_GetEntityNameByIndex(int nEntityIndex, StringBuilder entname) => JczLmc.lmc1_GetEntityNameByIndex(nEntityIndex, entname);
    public string GetEntityNameByIndex(int nEntityIndex) => JczLmc.GetEntityNameByIndex(nEntityIndex);
    public int SetEntityNameByIndex(int nEntityIndex, string entname) => JczLmc.SetEntityNameByIndex(nEntityIndex, entname);
    public int ChangeEntName(string strEntName, string strNewEntName) => JczLmc.ChangeEntName(strEntName, strNewEntName);
    public int GroupEnt(string strEntName1, string strEntName2, string strGroupName, int nGroupPen) => JczLmc.GroupEnt(strEntName1, strEntName2, strGroupName, nGroupPen);
    public int UnGroupEnt(string strGroupName) => JczLmc.UnGroupEnt(strGroupName);
    public int lmc1_GroupEnt2(string[] strEntName, int nEntCount, string strGroupName, int nGroupPen) => JczLmc.lmc1_GroupEnt2(strEntName, nEntCount, strGroupName, nGroupPen);
    public int UnGroupEnt2(string GroupName, int nFlag) => JczLmc.UnGroupEnt2(GroupName, nFlag);
    public int GetBitmapEntParam2(string strEntName,
                                  StringBuilder strImageFileName,
                                                    ref int nBmpAttrib,
                                                    ref int nScanAttrib,
                                                    ref double dBrightness,
                                                    ref double dContrast,
                                                    ref double dPointTime,
                                                    ref int nImportDpi,
                                                    ref int bDisableMarkLowGrayPt,
                                                    ref int nMinLowGrayPt
                                                    ) => JczLmc.GetBitmapEntParam2(strEntName,
                                                        strImageFileName,
                                                        ref nBmpAttrib,
                                                    ref nScanAttrib,
                                                    ref dBrightness,
                                                    ref dContrast,
                                                    ref dPointTime,
                                                    ref nImportDpi,
                                                    ref bDisableMarkLowGrayPt,
                                                    ref nMinLowGrayPt);
    public int SetBitmapEntParam(string strEntName,
                                                     string strImageFileName,
                                                     int nBmpAttrib,
                                                     int nScanAttrib,
                                                     double dBrightness,
                                                     double dContrast,
                                                     double dPointTime,
                                                     int nImportDpi,
                                                     bool bDisableMarkLowGrayPt,
                                                     int nMinLowGrayPt
                                                      ) => JczLmc.SetBitmapEntParam(strEntName,
                                                     strImageFileName,
                                                     nBmpAttrib,
                                                     nScanAttrib,
                                                     dBrightness,
                                                     dContrast,
                                                     dPointTime,
                                                     nImportDpi,
                                                     bDisableMarkLowGrayPt,
                                                     nMinLowGrayPt);
    /*
    public int MoveEntityBefore(int nMoveEnt, int GoalEnt);
    public int SetBitmapEntParam3(string strEntName,
                                                    double dDpiX,
                                                    double dDpiY,
                                                    [MarshalAs(UnmanagedType.LPArray)] byte[] bGrayScaleBuf);
    public int GetBitmapEntParam3(string strEntName,
                                                      ref double dDpiX,
                                                      ref double dDpiY,
                                                      byte[] bGrayScaleBuf);

    public int MoveEntityAfter(int nMoveEnt, int GoalEnt);
    public int ReverseAllEntOrder();
    #endregion

    #region ¶ЛїЪ

    public int ReadPort(ref ushort data);
    public int WritePort(ushort data);
    public int GetOutPort(ref ushort data);
    public int LaserOn(bool bOpen);

    #endregion

    #region ОД±ѕ
    public int ChangeTextByName(string EntName, string NewText);
    public int GetTextByName(string strEntName, StringBuilder Text);
    public int TextResetSn(string TextName);

    #region ЧЦМе

    public const uint LMC1_FONT_JSF = 1; //JczSingleLineЧЦМе
    public const uint LMC1_FONT_TTF = 2; //TrueTypeЧЦМе
    public const uint LMC1_FONT_DMF = 4; //DotMatrixЧЦМе
    public const uint LMC1_FONT_BCF = 8; //BarcodeЧЦМе

    public struct FontRecord
    {
        public string fontname;//ЧЦМеГыіЖ
        public uint fontattrib;//ЧЦМеКфРФ
    }
    public int GetFontRecordCount(ref int fontCount);
    public int GetFontRecordByIndex(int fontIndex, StringBuilder fontName, ref uint fontAttrib);
    public static bool GetAllFontRecord(ref FontRecord[] fonts)
    {
        int fontnum = 0;
        if (GetFontRecordCount(ref fontnum) != 0)
        {
            return false;
        }
        if (fontnum == 0)
        {
            return true;
        }
        fonts = new FontRecord[fontnum];
        StringBuilder str = new StringBuilder("", 255);
        uint fontAttrib = 0;
        for (int i = 0; i < fontnum; i++)
        {
            GetFontRecordByIndex(i, str, ref fontAttrib);
            fonts[i].fontname = str.ToString(); ;
            fonts[i].fontattrib = fontAttrib;
        }
        return true;
    }
    public int GetFontParam3(string strFontName,
                                                 ref double CharHeight,
                                                 ref double CharWidthRatio,
                                                 ref double CharAngle,
                                                 ref double CharSpace,
                                                 ref double LineSpace,
                                                 ref bool EqualCharWidth,
                                                 ref int nTextAlign,
                                                 ref bool bBold,
                                                 ref bool bItalic);
    public int SetFontParam3(string fontname,
                                                double CharHeight,
                                                double CharWidthRatio,
                                                double CharAngle,
                                                double CharSpace,
                                                double LineSpace,
                                                double spaceWidthRatio,
                                                bool EqualCharWidth,
                                                int nTextAlign,
                                                bool bBold,
                                                bool bItalic);
    public int GetTextEntParam(string EntName,
                                                     StringBuilder FontName,
                                                  ref double CharHeight,
                                                  ref double CharWidthRatio,
                                                  ref double CharAngle,
                                                  ref double CharSpace,
                                                  ref double LineSpace,
                                                  ref bool EqualCharWidth);

    public int SetTextEntParam(string EntName,
                                                    double CharHeight,
                                                    double CharWidthRatio,
                                                    double CharAngle,
                                                    double CharSpace,
                                                    double LineSpace,
                                                    bool EqualCharWidth);

    public int GetTextEntParam2(string EntName,
                                                    StringBuilder FontName,
                                                 ref double CharHeight,
                                                 ref double CharWidthRatio,
                                                 ref double CharAngle,
                                                 ref double CharSpace,
                                                 ref double LineSpace,
                                                 ref double spaceWidthRatio,
                                                 ref bool EqualCharWidth);
    public int SetTextEntParam2(string EntName,
                                                     string fontname,
                                                    double CharHeight,
                                                    double CharWidthRatio,
                                                    double CharAngle,
                                                    double CharSpace,
                                                    double LineSpace,
                                                    double spaceWidthRatio,
                                                    bool EqualCharWidth);
    public int GetTextEntParam4(string EntName,
                                                   StringBuilder FontName,
                                                   ref int nTextSpaceMode,
                                                   ref double dTextSpace,
                                            ref double CharHeight,
                                            ref double CharWidthRatio,
                                            ref double CharAngle,
                                            ref double CharSpace,
                                            ref double LineSpace,
                                            ref double dNullCharWidthRatio,
                                            ref int nTextAlign,
                                            ref bool bBold,
                                            ref bool bItalic);
    public int SetTextEntParam4(string EntName,
                                               string fontname,
                                               int nTextSpaceMode,
                                              double dTextSpace,
                                              double CharHeight,
                                              double CharWidthRatio,
                                              double CharAngle,
                                              double CharSpace,
                                              double LineSpace,
                                              double spaceWidthRatio,
                                                int nTextAlign,
                                                bool bBold,
                                                bool bItalic);
    public int GetCircleTextParam(string pEntName,
                                              ref double dCenX,
                                              ref double dCenY,
                                              ref double dCenZ,
                                              ref double dCirDiameter,
                                              ref double dCirBaseAngle,
                                              ref bool bCirEnableAngleLimit,
                                              ref double dCirAngleLimit,
                                              ref int nCirTextFlag);
    public int SetCircleTextParam(string pEntName,
                                                double dCenX,
                                                double dCenY,
                                                double dCenZ,
                                                double dCirDiameter,
                                                double dCirBaseAngle,
                                                bool bCirEnableAngleLimit,
                                                double dCirAngleLimit,
                                                int nCirTextFlag);
    #endregion

    #region ±КєЕ
    public int GetPenParam(int nPenNo,
                 ref int nMarkLoop,
                 ref double dMarkSpeed,
                 ref double dPowerRatio,
                 ref double dCurrent,
                 ref int nFreq,
                 ref double dQPulseWidth,
                 ref int nStartTC,
                 ref int nLaserOffTC,
                 ref int nEndTC,
                 ref int nPolyTC,
                 ref double dJumpSpeed,
                 ref int nJumpPosTC,
                 ref int nJumpDistTC,
                 ref double dEndComp,
                 ref double dAccDist,
                 ref double dPointTime,
                 ref bool bPulsePointMode,
                 ref int nPulseNum,
                 ref double dFlySpeed);

    public int GetPenParam2(int nPenNo,
                 ref int nMarkLoop,
                 ref double dMarkSpeed,
                 ref double dPowerRatio,
                 ref double dCurrent,
                 ref int nFreq,
                 ref double dQPulseWidth,
                 ref int nStartTC,
                 ref int nLaserOffTC,
                 ref int nEndTC,
                 ref int nPolyTC,
                 ref double dJumpSpeed,
                 ref int nJumpPosTC,
                 ref int nJumpDistTC,
                 ref double dPointTime,
                 ref int nSpiWave,
                 ref bool bWobbleMode,
                 ref double bWobbleDiameter,
                 ref double bWobbleDist);


    [DllImport("MarkEzd", EntryPoint = "lmc1_GetPenParam4", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    public int GetPenParam4(int nPenNo,
                                                    StringBuilder pStrName,
                                                    ref int clr,
                                                    ref bool bDisableMark,
                                                    ref bool bUseDefParam,
                                                    ref int nMarkLoop,
                                                    ref double dMarkSpeed,
                                                    ref double dPowerRatio,
                                                    ref double dCurrent,
                                                    ref int nFreq,
                                                    ref double dQPulseWidth,
                                                    ref int nStartTC,
                                                    ref int nLaserOffTC,
                                                    ref int nEndTC,
                                                    ref int nPolyTC,
                                                    ref double dJumpSpeed,
                                                    ref int nMinJumpDelayTCUs,
                                                    ref int nMaxJumpDelayTCUs,
                                                    ref double dJumpLengthLimit,
                                                    ref double dPointTimeMs,
                                                    ref bool nSpiSpiContinueMode,
                                                    ref int nSpiWave,
                                                    ref int nYagMarkMode,
                                                    ref bool bPulsePointMode,
                                                    ref int nPulseNum,
                                                    ref bool bEnableACCMode,
                                                    ref double dEndComp,
                                                    ref double dAccDist,
                                                    ref double dBreakAngle,
                                                    ref bool bWobbleMode,
                                                    ref double bWobbleDiameter,
                                                    ref double bWobbleDist);
    public int SetPenParam(int nPenNo,
                         int nMarkLoop,
                         double dMarkSpeed,
                         double dPowerRatio,
                         double dCurrent,
                         int nFreq,
                         double dQPulseWidth,
                         int nStartTC,
                         int nLaserOffTC,
                         int nEndTC,
                         int nPolyTC,
                         double dJumpSpeed,
                         int nJumpPosTC,
                         int nJumpDistTC,
                         double dEndComp,
                         double dAccDist,
                         double dPointTime,
                         bool bPulsePointMode,
                         int nPulseNum,
                         double dFlySpeed);


    public static int SetPenParams(PenParams penParams) => SetPenParam(
                                         nPenNo: penParams.PenNo,
                                         nMarkLoop: penParams.MarkLoop,
                                         dMarkSpeed: penParams.MarkSpeed,
                                         dPowerRatio: penParams.PowerRatio,
                                         dCurrent: penParams.Current,
                                         nFreq: penParams.Freq,
                                         dQPulseWidth: penParams.QPulseWidth,
                                         nStartTC: penParams.StartTC,
                                         nLaserOffTC: penParams.LaserOffTC,
                                         nEndTC: penParams.EndTC,
                                         nPolyTC: penParams.PolyTC,
                                         dJumpSpeed: penParams.JumpSpeed,
                                         nJumpPosTC: penParams.JumpPosTC,
                                         nJumpDistTC: penParams.JumpDistTC,
                                         dEndComp: penParams.EndComp,
                                         dAccDist: penParams.AccDist,
                                         dPointTime: penParams.PointTime,
                                         bPulsePointMode: penParams.PulsePointMode,
                                         nPulseNum: penParams.PulseNum,
                                         dFlySpeed: penParams.FlySpeed);


    public int SetPenParam2(int nPenNo,
                                                    int nMarkLoop,
                                                    double dMarkSpeed,
                                                    double dPowerRatio,
                                                    double dCurrent,
                                                    int nFreq,
                                                    double dQPulseWidth,
                                                    int nStartTC,
                                                    int nLaserOffTC,
                                                    int nEndTC,
                                                    int nPolyTC,
                                                    double dJumpSpeed,
                                                    int nJumpPosTC,
                                                    int nJumpDistTC,
                                                    double dPointTime,
                                                    int nSpiWave,
                                                    bool bWobbleMode,
                                                    double bWobbleDiameter,
                                                    double bWobbleDist);


    public static int ColorToCOLORREF(Color color)
    {
        return ((color.R | (color.G << 8)) | (color.B << 0x10));
    }

    public static Color COLORREFToColor(int colorRef)
    {
        byte[] _IntByte = BitConverter.GetBytes(colorRef);
        return Color.FromArgb(_IntByte[0], _IntByte[1], _IntByte[2]);
    }
    public int SetPenParam4(int nPenNo,//±КєЕ
                                                        string pStrName,    // ГыіЖ
                                                        int clr,//СХЙ«
                                                        bool bDisableMark,//К№ДЬјУ№¤
                                                        bool bUseDefParam,//К№УГД¬ИПІОКэ
                                                        int nMarkLoop,//јУ№¤ґОКэ
                                                        double dMarkSpeed,//јУ№¤ЛЩ¶И
                                                        double dPowerRatio,//№¦ВК %
                                                        double dCurrent,//µзБч,A
                                                        int nFreq,//ЖµВК HZ
                                                        double dQPulseWidth,//ВцїнЈ¬yag us    ylpm ns
                                                        int nStartTC,//їЄ№вСУК±
                                                        int nLaserOffTC,//№Ш№вСУК±
                                                        int nEndTC,//ЅбКшСУК±
                                                        int nPolyTC,//№ХЅЗСУК±
                                                        double dJumpSpeed,//МшЧЄЛЩ¶И
                                                        int nMinJumpDelayTCUs,//ЧоРЎМшЧЄСУК±
                                                        int nMaxJumpDelayTCUs,//ЧоґуМшЧЄСУК±
                                                        double dJumpLengthLimit,//МшЧЄѕаАлгРЦµ
                                                        double dPointTimeMs,//ґтµгК±јд
                                                        bool nSpiSpiContinueMode,//SPIБ¬РшДЈКЅ
                                                        int nSpiWave,//SPIІЁРО±аєЕ
                                                        int nYagMarkMode,//YAGУЕ»ЇДЈКЅ
                                                        bool bPulsePointMode,//ВціеґтµгДЈКЅ
                                                        int nPulseNum,//ВціеґтµгВціеКэБї
                                                        bool bEnableACCMode,//ЖфУГјУјхЛЩУЕ»Ї
                                                        double dEndComp,//јУЛЩ
                                                        double dAccDist,//јхЛЩ
                                                        double dBreakAngle,//ЦР¶ПЅЗ¶И
                                                        bool bWobbleMode,//¶¶¶ЇДЈКЅ
                                                        double bWobbleDiameter,//¶¶¶ЇЦ±ѕ¶
                                                        double bWobbleDist);//¶¶¶ЇПЯјдѕа
    public int SetPenDisableState(int nPenNo, bool bDisableMark);
    public int GetPenDisableState(int nPenNo, ref bool bDisableMark);
    public int GetPenNumberFromName(string strEntName);
    public int GetPenNumberFromEnt(string strEntName);
    public void SetEntAllChildPen(string strEntName, int nPenNo);

    #endregion

    #region Моід
    public const int HATCHATTRIB_ALLCALC = 0x01;//И«Ії¶ФПуХыМејЖЛг
    public const int HATCHATTRIB_EDGE = 0x02;//ИЖ±ЯЧЯТ»ґО
    public const int HATCHATTRIB_MINUP = 0x04;//ЧоЙЩЖр±К
    public const int HATCHATTRIB_BIDIR = 0x08;//Л«ПтМоід
    public const int HATCHATTRIB_LOOP = 0x10;//»·РРМоід
    public const int HATCHATTRIB_OUT = 0x20;//»·РРУЙДЪПтНв
    public const int HATCHATTRIB_AUTOROT = 0x40;//ЧФ¶ЇЅЗ¶ИРэЧЄ
    public const int HATCHATTRIB_AVERAGELINE = 0x80;//ЧФ¶Ї·ЦІјМоідПЯ
    public const int HATCHATTRIB_CROSSLINE = 0x400;//Ѕ»ІжМоід
    public int SetHatchParam(bool bEnableContour,
                      int bEnableHatch1,
                      int nPenNo1,
                      int nHatchAttrib1,
                      double dHatchEdgeDist1,
                      double dHatchLineDist1,
                      double dHatchStartOffset1,
                      double dHatchEndOffset1,
                      double dHatchAngle1,
                      int bEnableHatch2,
                      int nPenNo2,
                      int nHatchAttrib2,
                      double dHatchEdgeDist2,
                      double dHatchLineDist2,
                      double dHatchStartOffset2,
                      double dHatchEndOffset2,
                      double dHatchAngle2);

    public static int sethatchparams(hatchparams hatchparams) => sethatchparam(
       benablecontour: hatchparams.enablecontour,
       benablehatch1: hatchparams.enablehatch ? 1 : 0,
       npenno1: hatchparams.penno,
       nhatchattrib1: hatchparams.hatchattribute,//hatchattrib_loop,//40
       dhatchedgedist1: hatchparams.hatchedgedist,
       dhatchlinedist1: hatchparams.hatchlinedist,
       dhatchstartoffset1: hatchparams.hatchstartoffset,
       dhatchendoffset1: hatchparams.hatchendoffset,
       dhatchangle1: hatchparams.hatchrotateangle,
       benablehatch2: 0,
       npenno2: 0,
       nhatchattrib2: 48,
       dhatchedgedist2: 0,
       dhatchlinedist2: 0,
       dhatchstartoffset2: 0,
       dhatchendoffset2: 0,
       dhatchangle2: 0);
    public int SetHatchParam2(bool bEnableContour,//К№ДЬВЦАЄ±ѕЙн
                                                          int nParamIndex,//МоідІОКэРтєЕЦµОЄ1,2,3
                                                          int bEnableHatch,//К№ДЬМоід
                                                          int nPenNo,//МоідІОКэ±КєЕ
                                                          int nHatchType,//МоідАаРН 0µҐПт 1Л«Пт 2»ШРО 3№­РО 4№­РОІ»·ґПт
                                                          bool bHatchAllCalc,//КЗ·сИ«Ії¶ФПуЧчОЄХыМеТ»ЖрјЖЛг
                                                          bool bHatchEdge,//ИЖ±ЯТ»ґО
                                                          bool bHatchAverageLine,//ЧФ¶ЇЖЅѕщ·ЦІјПЯ
                                                          double dHatchAngle,//МоідПЯЅЗ¶И 
                                                          double dHatchLineDist,//МоідПЯјдѕа
                                                          double dHatchEdgeDist,//МоідПЯ±Яѕа    
                                                          double dHatchStartOffset,//МоідПЯЖрКјЖ«ТЖѕаАл
                                                          double dHatchEndOffset,//МоідПЯЅбКшЖ«ТЖѕаАл
                                                          double dHatchLineReduction,//Ц±ПЯЛхЅш
                                                          double dHatchLoopDist,//»·јдѕа
                                                          int nEdgeLoop,//»·Кэ
                                                          bool nHatchLoopRev,//»·РО·ґЧЄ
                                                          bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                                                          double dHatchRotateAngle//ЧФ¶ЇРэЧЄЅЗ¶И   
                                                       );
    public int SetHatchParam3(bool bEnableContour,//К№ДЬВЦАЄ±ѕЙн
                                                      int nParamIndex,//МоідІОКэРтєЕЦµОЄ1,2,3
                                                      int bEnableHatch,//К№ДЬМоід
                                                      int nPenNo,//МоідІОКэ±КєЕ
                                                      int nHatchType,//МоідАаРН 0µҐПт 1Л«Пт 2»ШРО 3№­РО 4№­РОІ»·ґПт
                                                      bool bHatchAllCalc,//КЗ·сИ«Ії¶ФПуЧчОЄХыМеТ»ЖрјЖЛг
                                                      bool bHatchEdge,//ИЖ±ЯТ»ґО
                                                      bool bHatchAverageLine,//ЧФ¶ЇЖЅѕщ·ЦІјПЯ
                                                      double dHatchAngle,//МоідПЯЅЗ¶И 
                                                      double dHatchLineDist,//МоідПЯјдѕа
                                                      double dHatchEdgeDist,//МоідПЯ±Яѕа    
                                                      double dHatchStartOffset,//МоідПЯЖрКјЖ«ТЖѕаАл
                                                      double dHatchEndOffset,//МоідПЯЅбКшЖ«ТЖѕаАл
                                                      double dHatchLineReduction,//Ц±ПЯЛхЅш
                                                      double dHatchLoopDist,//»·јдѕа
                                                      int nEdgeLoop,//»·Кэ
                                                      bool nHatchLoopRev,//»·РО·ґЧЄ
                                                      bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                                                      double dHatchRotateAngle,//ЧФ¶ЇРэЧЄЅЗ¶И  
                                                      bool bHatchCross
                                                   );
    public int GetHatchParam3(ref bool bEnableContour,
                                                     int nParamIndex,
                                                     ref int bEnableHatch,
                                                     ref int nPenNo,
                                                     ref int nHatchType,
                                                     ref bool bHatchAllCalc,
                                                     ref bool bHatchEdge,
                                                     ref bool bHatchAverageLine,
                                                     ref double dHatchAngle,
                                                     ref double dHatchLineDist,
                                                     ref double dHatchEdgeDist,
                                                     ref double dHatchStartOffset,
                                                     ref double dHatchEndOffset,
                                                     ref double dHatchLineReduction,//Ц±ПЯЛхЅш
                                                     ref double dHatchLoopDist,//»·јдѕа
                                                     ref int nEdgeLoop,//»·Кэ
                                                     ref bool nHatchLoopRev,//»·РО·ґЧЄ
                                                     ref bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                                                     ref double dHatchRotateAngle,
                                                     ref bool nHatchCross);
    public int
        SetHatchEntParam(string HatchName,
                                                    bool bEnableContour,
                                                    int nParamIndex,
                                                    int bEnableHatch,
                                                    int nPenNo,
                                                    int nHatchType,
                                                    bool bHatchAllCalc,
                                                    bool bHatchEdge,
                                                    bool bHatchAverageLine,
                                                    double dHatchAngle,
                                                    double dHatchLineDist,
                                                    double dHatchEdgeDist,
                                                    double dHatchStartOffset,
                                                    double dHatchEndOffset,
                                                    double dHatchLineReduction,//Ц±ПЯЛхЅш
                                                    double dHatchLoopDist,//»·јдѕа
                                                    int nEdgeLoop,//»·Кэ
                                                    bool nHatchLoopRev,//»·РО·ґЧЄ
                                                    bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                                                    double dHatchRotateAngle);
    public int GetHatchEntParam(string HatchName,
                     ref bool bEnableContour,
                     int nParamIndex,
                     ref int bEnableHatch,
                     ref int nPenNo,
                     ref int nHatchType,
                     ref bool bHatchAllCalc,
                     ref bool bHatchEdge,
                     ref bool bHatchAverageLine,
                     ref double dHatchAngle,
                     ref double dHatchLineDist,
                     ref double dHatchEdgeDist,
                     ref double dHatchStartOffset,
                     ref double dHatchEndOffset,
                     ref double dHatchLineReduction,//Ц±ПЯЛхЅш
                     ref double dHatchLoopDist,//»·јдѕа
                     ref int nEdgeLoop,//»·Кэ
                     ref bool nHatchLoopRev,//»·РО·ґЧЄ
                     ref bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                     ref double dHatchRotateAngle);
    public int SetHatchEntParam2(string HatchName,
                        bool bEnableContour,
                        int nParamIndex,
                        int bEnableHatch,
                        bool bContourFirst,
                        int nPenNo,
                        int nHatchType,
                        bool bHatchAllCalc,
                        bool bHatchEdge,
                        bool bHatchAverageLine,
                        double dHatchAngle,
                        double dHatchLineDist,
                        double dHatchEdgeDist,
                        double dHatchStartOffset,
                        double dHatchEndOffset,
                        double dHatchLineReduction,//Ц±ПЯЛхЅш
                        double dHatchLoopDist,//»·јдѕа
                        int nEdgeLoop,//»·Кэ
                        bool nHatchLoopRev,//»·РО·ґЧЄ
                        bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                        double dHatchRotateAngle,
                        bool bHatchCrossMode,
                        int dCycCount);
    public int GetHatchEntParam2(string HatchName,
                     ref bool bEnableContour,
                     int nParamIndex,
                     ref int bEnableHatch,
                     ref bool bContourFirst,
                     ref int nPenNo,
                     ref int nHatchType,
                     ref bool bHatchAllCalc,
                     ref bool bHatchEdge,
                     ref bool bHatchAverageLine,
                     ref double dHatchAngle,
                     ref double dHatchLineDist,
                     ref double dHatchEdgeDist,
                     ref double dHatchStartOffset,
                     ref double dHatchEndOffset,
                     ref double dHatchLineReduction,//Ц±ПЯЛхЅш
                     ref double dHatchLoopDist,//»·јдѕа
                     ref int nEdgeLoop,//»·Кэ
                     ref bool nHatchLoopRev,//»·РО·ґЧЄ
                     ref bool bHatchAutoRotate,//КЗ·сЧФ¶ЇРэЧЄЅЗ¶И
                     ref double dHatchRotateAngle,
                     ref bool bHatchCrossMode,
                     ref int dCycCount);
    public int HatchEnt(string strEntName, string strHatchEntName);
    public int UnHatchEnt(string strHatchEntName);

    #endregion

    #region МнјУЙѕіэ¶ФПу
    public int ClearLibAllEntity();
    public int DeleteEnt(string strEntName);
    public int AddTextToLib(string text, string EntName, double dPosX, double dPosY, double dPosZ, int nAlign, double dTextRotateAngle, int nPenNo, int bHatchText);
    public int AddCircleTextToLib(string pStr,
                                                string pEntName,
                                                double dCenX,
                                                double dCenY,
                                                double dCenZ,
                                                int nPenNo,
                                                int bHatchText,
                                                double dCirDiameter,
                                                double dCirBaseAngle,
                                                bool bCirEnableAngleLimit,
                                                double dCirAngleLimit,
                                                int nCirTextFlag);
    public int AddCurveToLib([MarshalAs(UnmanagedType.LPArray)] double[,] PtBuf, int ptNum, string strEntName, int nPenNo);
    public int lmc1_AddCircleToLib(double ptCenX, double ptCenY, double dRadius, string pEntName, int nPenNo);
    public int AddPointToLib([MarshalAs(UnmanagedType.LPArray)] double[,] PtBuf, int ptNum, string strEntName, int nPenNo);
    public int AddDelayToLib(double dDelayMs);
    public int AddWritePortToLib(int nOutPutBit, bool bHigh, bool bPulse, double dPulseTimeMs);
    public int AddFileToLib(string strFileName, string strEntName, double dPosX, double dPosY, double dPosZ, int nAlign, double dRatio, int nPenNo, int bHatchFile);

    #region МхВл

    public enum BARCODETYPE
    {
        BARCODETYPE_39 = 0,
        BARCODETYPE_93 = 1,
        BARCODETYPE_128A = 2,
        BARCODETYPE_128B = 3,
        BARCODETYPE_128C = 4,
        BARCODETYPE_128OPT = 5,
        BARCODETYPE_EAN128A = 6,
        BARCODETYPE_EAN128B = 7,
        BARCODETYPE_EAN128C = 8,
        BARCODETYPE_EAN13 = 9,
        BARCODETYPE_EAN8 = 10,
        BARCODETYPE_UPCA = 11,
        BARCODETYPE_UPCE = 12,
        BARCODETYPE_25 = 13,
        BARCODETYPE_INTER25 = 14,
        BARCODETYPE_CODABAR = 15,
        BARCODETYPE_PDF417 = 16,
        BARCODETYPE_DATAMTX = 17,
        BARCODETYPE_USERDEF = 18,
        BARCODETYPE_QRCODE = 19,
        BARCODETYPE_MICROQRCODE = 20

    };

    public const ushort BARCODEATTRIB_CHECKNUM = 0x0004;//ЧФСйВл
    public const ushort BARCODEATTRIB_REVERSE = 0x0008;//·ґЧЄ
    public const ushort BARCODEATTRIB_SHORTMODE = 0x0040;//¶юО¬ВлЛх¶МДЈКЅ
    public const ushort BARCODEATTRIB_DATAMTX_DOTMODE = 0x0080;//¶юО¬ВлОЄµгДЈКЅ
    public const ushort BARCODEATTRIB_DATAMTX_CIRCLEMODE = 0x0100;//¶юО¬ВлОЄФІДЈКЅ
    public const ushort BARCODEATTRIB_DATAMTX_ENABLETILDE = 0x0200;//DataMatrixК№ДЬ~
    public const ushort BARCODEATTRIB_RECTMODE = 0x0400;//¶юО¬ВлОЄѕШРОДЈКЅ
    public const ushort BARCODEATTRIB_SHOWCHECKNUM = 0x0800;//ПФКѕРЈСйВлОДЧЦ
    public const ushort BARCODEATTRIB_HUMANREAD = 0x1000;//ПФКѕИЛК¶±рЧЦ·ы
    public const ushort BARCODEATTRIB_NOHATCHTEXT = 0x2000;//І»МоідЧЦ·ы
    public const ushort BARCODEATTRIB_BWREVERSE = 0x4000;//єЪ°Ч·ґЧЄ
    public const ushort BARCODEATTRIB_2DBIDIR = 0x8000;//2О¬ВлЛ«ПтЕЕБР

    public enum DATAMTX_SIZEMODE
    {
        DATAMTX_SIZEMODE_SMALLEST = 0,
        DATAMTX_SIZEMODE_10X10 = 1,
        DATAMTX_SIZEMODE_12X12 = 2,
        DATAMTX_SIZEMODE_14X14 = 3,
        DATAMTX_SIZEMODE_16X16 = 4,
        DATAMTX_SIZEMODE_18X18 = 5,
        DATAMTX_SIZEMODE_20X20 = 6,
        DATAMTX_SIZEMODE_22X22 = 7,
        DATAMTX_SIZEMODE_24X24 = 8,
        DATAMTX_SIZEMODE_26X26 = 9,
        DATAMTX_SIZEMODE_32X32 = 10,
        DATAMTX_SIZEMODE_36X36 = 11,
        DATAMTX_SIZEMODE_40X40 = 12,
        DATAMTX_SIZEMODE_44X44 = 13,
        DATAMTX_SIZEMODE_48X48 = 14,
        DATAMTX_SIZEMODE_52X52 = 15,
        DATAMTX_SIZEMODE_64X64 = 16,
        DATAMTX_SIZEMODE_72X72 = 17,
        DATAMTX_SIZEMODE_80X80 = 18,
        DATAMTX_SIZEMODE_88X88 = 19,
        DATAMTX_SIZEMODE_96X96 = 20,
        DATAMTX_SIZEMODE_104X104 = 21,
        DATAMTX_SIZEMODE_120X120 = 22,
        DATAMTX_SIZEMODE_132X132 = 23,
        DATAMTX_SIZEMODE_144X144 = 24,
        DATAMTX_SIZEMODE_8X18 = 25,
        DATAMTX_SIZEMODE_8X32 = 26,
        DATAMTX_SIZEMODE_12X26 = 27,
        DATAMTX_SIZEMODE_12X36 = 28,
        DATAMTX_SIZEMODE_16X36 = 29,
        DATAMTX_SIZEMODE_16X48 = 30,
    }
    public int AddBarCodeToLib(string strText,
        string strEntName,
        double dPosX,
        double dPosY,
        double dPosZ,
        int nAlign,
        int nPenNo,
        int bHatchText,
        BARCODETYPE nBarcodeType,
        ushort wBarCodeAttrib,
        double dHeight,
        double dNarrowWidth,
        [MarshalAs(UnmanagedType.LPArray)] double[] dBarWidthScale,
        [MarshalAs(UnmanagedType.LPArray)] double[] dSpaceWidthScale,
          double dMidCharSpaceScale,
        double dQuietLeftScale,
        double dQuietMidScale,
        double dQuietRightScale,
        double dQuietTopScale,
        double dQuietBottomScale,
        int nRow,
        int nCol,
        int nCheckLevel,
       DATAMTX_SIZEMODE nSizeMode,
        double dTextHeight,
        double dTextWidth,
        double dTextOffsetX,
        double dTextOffsetY,
        double dTextSpace,
        double dDiameter,
        string TextFontName);
    public int GetBarcodeParam(string pEntName,
                                                ref ushort wBarCodeAttrib,
                                                ref int nSizeMode,
                                                ref int nCheckLevel,
                                                ref int nLangPage,
                                                ref double dDiameter,
                                                ref int nPointTimesN,
                                                ref double dBiDirOffset);
    public int SetBarcodeParam(string pEntName,
                                                ushort wBarCodeAttrib,
                                                int nSizeMode,
                                                int nCheckLevel,
                                                int nLangPage,
                                                double dDiameter,
                                                int nPointTimesN,
                                                double dBiDirOffset);


    #endregion

    #endregion

    #region А©Х№Цб
    public int ResetAxis(bool bEnAxis1, bool bEnAxis2);
    public int AxisMoveTo(int axis, double GoalPos);
    public int AxisGoHome(int axis);
    public double GetAxisCoor(int axis);
    public int AxisMoveToPulse(int axis, int nGoalPos);
    public int GetAxisCoorPulse(int axis);

    #endregion

    #region УІјюЛшґж
    public int EnableLockInputPort(bool bLowToHigh);
    public int ClearLockInputPort();
    public int ReadLockPort(ref ushort data);
    #endregion
    */
#endregion
    
}

