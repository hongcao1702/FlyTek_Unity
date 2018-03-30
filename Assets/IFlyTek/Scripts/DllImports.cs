using System;
using System.Runtime.InteropServices;

namespace Second
{
    public class DllImports
    {
#if UNITY_STANDALONE_WIN
        [DllImport("msc_x64")]
        public static extern int MSPLogin(string usr, string pwd, string parameters);

        [DllImport("msc_x64")]
        public static extern int MSPLogout();

        [DllImport("msc_x64")]
        public static extern IntPtr MSPGetResult(out UIntPtr rsltLen, out IntPtr rsltStatus, out int errorCode);

        [DllImport("msc_x64")]
        public static extern int MSPSetParam(string paramName, string parameters);

        [DllImport("msc_x64")]
        public static extern int MSPGetParam(string paramName, out string parameters, ref UIntPtr valueLen);

        [DllImport("msc_x64")]
        public static extern IntPtr MSPUploadData(string dataName, byte[] data, uint dataLen, string parameters, out int errorCode);

        [DllImport("msc_x64")]
        public static extern byte[] MSPDownloadData(string paramsval, out UIntPtr dataLen, out int errorCode);

        [DllImport("msc_x64")]
        public static extern IntPtr MSPSearch(string parameters, string text, out UIntPtr dataLen, out int errorCode);

        [DllImport("msc_x64")]
        public static extern IntPtr MSPNlpSearch(string parameters, string text, uint textLen, IntPtr errorCode, IntPtr callback, out byte[] userData);

        [DllImport("msc_x64")]
        public static extern int MSPNlpSchCancel(string sessionID, string hints);

        [DllImport("msc_x64")]
        public static extern int MSPRegisterNotify(IntPtr statusCb, byte[] userData);

        [DllImport("msc_x64")]
        public static extern int QISRAudioWrite(string sessionID, IntPtr waveData, uint waveLen, int audioStatus, ref int epStatus, ref int recogStatus);

        [DllImport("msc_x64")]
        public static extern IntPtr QISRSessionBegin(string grammarList, string parameters, out int errorCode);

        [DllImport("msc_x64")]
        public static extern IntPtr QISRGetResult(string sessionID, ref int rsltStatus, int waitTime, out int errorCode);

        [DllImport("msc_x64")]
        public static extern int QISRSessionEnd(string sessionID, string hints);

        [DllImport("msc_x64")]
        public static extern IntPtr QTTSSessionBegin(string paramsval, out int errorCode);

        [DllImport("msc_x64")]
        public static extern int QTTSTextPut(string sessionID, byte[] text, uint textLen, string parameters);

        [DllImport("msc_x64")]
        public static extern int QTTSSessionEnd(string sessionID, string hints);

        [DllImport("msc_x64")]
        public static extern IntPtr QTTSAudioGet(string sessionID, ref uint audioLen, ref int synthStatus, out int errorCode);

        [DllImport("msc_x64")]
        public static extern IntPtr QISESessionBegin(string parameters, string userModelID, out int errorCode);

        [DllImport("msc_x64")]
        public static extern int QISETextPut(string sessionID, string textString, uint textLen, string parameters);

        [DllImport("msc_x64")]
        public static extern int QISEAudioWrite(string sessionID, IntPtr waveData, uint waveLen, int audioStatus, ref int epStatus, ref int recogStatus);

        [DllImport("msc_x64")]
        public static extern IntPtr QISEResultInfo(string sessionID, out int errorCode);

        [DllImport("msc_x64")]
        public static extern int QISESessionEnd(string sessionID, string hints);

        [DllImport("msc_x64")]
        public static extern int QISEGetParam(string sessionID, string paramName, string parameters, ref uint valueLen);

        [DllImport("msc_x64")]
        public static extern IntPtr QISEGetResult(string sessionID, ref uint rsltLen, ref int rsltStatus, ref int errorCode);
#endif

#if UNITY_ANDROID
    [DllImport("msc")]
    public static extern int MSPLogin(string usr, string pwd, string parameters);

    [DllImport("msc")]
    public static extern int MSPLogout();

    [DllImport("msc")]
    public static extern IntPtr MSPGetResult(out UIntPtr rsltLen, out IntPtr rsltStatus, out int errorCode);

    [DllImport("msc")]
    public static extern int MSPSetParam(string paramName, string parameters);

    [DllImport("msc")]
    public static extern int MSPGetParam(string paramName, out string parameters, ref UIntPtr valueLen);

    [DllImport("msc")]
    public static extern IntPtr MSPUploadData(string dataName, byte[] data, uint dataLen, string paramsval, out int errorCode);

    [DllImport("msc")]
    public static extern byte[] MSPDownloadData(string paramsval, out UIntPtr dataLen, out int errorCode);

    [DllImport("msc")]
    public static extern IntPtr MSPSearch(string parameters, string text, out UIntPtr dataLen, out int errorCode);

    [DllImport("msc")]
    public static extern IntPtr MSPNlpSearch(string parameters, string text, uint textLen, IntPtr errorCode, IntPtr callback, out byte[] userData);

    [DllImport("msc")]
    public static extern int MSPNlpSchCancel(string sessionID, string hints);

    [DllImport("msc")]
    public static extern int MSPRegisterNotify(IntPtr statusCb, byte[] userData);

    [DllImport("msc")]
    public static extern int QISRAudioWrite(string sessionID, IntPtr waveData, uint waveLen, int audioStatus, ref int epStatus, ref int recogStatus);

    [DllImport("msc")]
    public static extern IntPtr QISRSessionBegin(string grammarList, string parameters, out int errorCode);

    [DllImport("msc")]
    public static extern IntPtr QISRGetResult(string sessionID, ref int rsltStatus, int waitTime, out int errorCode);

    [DllImport("msc")]
    public static extern int QISRSessionEnd(string sessionID, string hints);

    [DllImport("msc")]
    public static extern IntPtr QTTSSessionBegin(string parameters, out int errorCode);

    [DllImport("msc")]
    public static extern int QTTSTextPut(string sessionID, byte[] text, uint textLen, string parameters);

    [DllImport("msc")]
    public static extern int QTTSSessionEnd(string sessionID, string hints);

    [DllImport("msc")]
    public static extern IntPtr QTTSAudioGet(string sessionID, ref uint audioLen, ref int synthStatus, out int errorCode);

    [DllImport("msc")]
    public static extern IntPtr QISESessionBegin(string parameters, string userModelID, out int errorCode);

    [DllImport("msc")]
    public static extern int QISETextPut(string sessionID, string textString, uint textLen, string parameters);

    [DllImport("msc")]
    public static extern int QISEAudioWrite(string sessionID, IntPtr waveData, uint waveLen, int audioStatus, ref int epStatus, ref int recogStatus);

    [DllImport("msc")]
    public static extern string QISEResultInfo(string sessionID, out int errorCode);

    [DllImport("msc")]
    public static extern int QISESessionEnd(string sessionID, string hints);

    [DllImport("msc")]
    public static extern int QISEGetParam(string sessionID, string paramName, string parameters, ref uint valueLen);

    [DllImport("msc")]
    public static extern IntPtr QISEGetResult(string sessionID, ref uint rsltLen, ref int rsltStatus, ref int errorCode);
#endif
    }
}