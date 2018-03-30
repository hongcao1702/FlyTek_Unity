
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Second
{
    /// <summary>
    /// 语音识别类(iFLY Speech Recognizer)
    /// </summary>
    public class Qisr : IFlyInterface
    {
        private static string sessionISRBeginParams = "sub = asr, result_type = plain, result_encoding = utf8";
        private string path;

        public Qisr() {
#if UNITY_ANDROID
            path = Application.persistentDataPath + "/iflytek01.wav";
#elif UNITY_STANDALONE_WIN
            path = Application.streamingAssetsPath + "/iflytek01.wav";
#endif
        }

        public IEnumerator RunFunc() {
            IEnumerator e = null;
            if (Application.platform == RuntimePlatform.Android) //如果是Android平台
            {
                e = Utils.CopyFileAndroid("gm_continuous_digit.abnf");
                while (e.MoveNext()) {
                    yield return e.Current;
                }
                e = Utils.CopyFileAndroid("iflytek01.wav");
                while (e.MoveNext()) {
                    yield return e.Current;
                }
            }
            e = RunISR();
            while (e.MoveNext()) {
                yield return e.Current;
            }
            yield break;
        }

        /// <summary>
        /// 运行声音识别功能
        /// </summary>
        /// <returns></returns>
        public IEnumerator RunISR() {

            ///模拟录音，输入音频
            if (!File.Exists(path)) {
                Utils.CustomPrint("文件" + path + "不存在！");
                yield break;
            }

            const int BUFFER_NUM = 640 * 10;/// 每次写入200ms音频(16k，16bit)：1帧音频20ms，10帧=200ms。16k采样率的16位音频，一帧的大小为640Byte
            int ret = 0;
            string result = "";
            int len;
            int audStatus = (int)AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;  ///音频状态
            int epStatus = (int)EpStatus.MSP_EP_NULL;                    ///端点检测
            int recStatus = (int)RecogStatus.MSP_REC_NULL;               ///识别状态
            int rsltStatus = (int)RecogStatus.MSP_REC_NULL;
            bool first = true;

            int errcode = 0;

            Utils.CustomPrint("上传语法 ...+\r\n");
            string grammarID = GetGrammarID();
            if (!string.IsNullOrEmpty(grammarID)) {
                Utils.CustomPrint("上传语法成功\r\n");
            } else {
                Utils.CustomPrint("MSPUploadData error");
                yield break;
            }

            IntPtr ptrSessionID = DllImports.QISRSessionBegin(grammarID, sessionISRBeginParams, out errcode);
            string sessionID = Marshal.PtrToStringAnsi(ptrSessionID);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)errcode) {
                Utils.CustomPrint("QISRSessionBegin err,errCode=" + ((ErrorCode)errcode).ToString("G"));
                yield break;
            }

            FileStream fp = new FileStream(path, FileMode.Open);
            byte[] buff = new byte[BUFFER_NUM];
            IntPtr bp = Marshal.AllocHGlobal(BUFFER_NUM);

            while (fp.Position != fp.Length) {
                len = fp.Read(buff, 0, BUFFER_NUM);
                Marshal.Copy(buff, 0, bp, buff.Length);

                audStatus = (int)AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;
                if (first) {
                    audStatus = (int)AudioStatus.MSP_AUDIO_SAMPLE_FIRST;
                    first = false;
                }

                Utils.CustomPrint(">");
                ///开始向服务器发送音频数据
                ret = DllImports.QISRAudioWrite(sessionID, bp, (uint)len, audStatus, ref epStatus, ref recStatus);

                if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                    HandleErrorMsg("QISRAudioWrite", ret, sessionID, fp);
                    yield break;
                }

                if (EpStatus.MSP_EP_AFTER_SPEECH == (EpStatus)epStatus) {
                    break;
                }
                
                ///sleep一下很有必要，防止MSC端无缓冲的识别结果时浪费CPU资源
                yield return new WaitForSeconds(0.2f);
            }
            buff = null;
            fp.Close();
            fp = null;
            Marshal.FreeHGlobal(bp);

            ret = DllImports.QISRAudioWrite(sessionID, IntPtr.Zero, 0, (int)AudioStatus.MSP_AUDIO_SAMPLE_LAST, ref epStatus, ref recStatus);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                HandleErrorMsg("QISRAudioWrite", ret, sessionID, null);
                yield break;
            }

            ///最后一块数据发完之后，循环从服务器端获取结果
            ///考虑到网络环境不好的情况下，需要对循环次数作限定
            while (RecogStatus.MSP_REC_STATUS_COMPLETE != (RecogStatus)rsltStatus) {
                IntPtr rec_result = DllImports.QISRGetResult(sessionID, ref rsltStatus, 0, out ret);
                if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                    HandleErrorMsg("QISRGetResult", ret, sessionID, null);
                    yield break;
                }
                if (IntPtr.Zero != rec_result) {
                    string tmp = Marshal.PtrToStringAnsi(rec_result);
                    Utils.CustomPrint("---:" + tmp);
                    result += tmp;
                    Utils.CustomPrint("传完音频后返回结果！:" + result);
                }
                yield return new WaitForSeconds(0.2f);
            }

            ret = DllImports.QISRSessionEnd(sessionID, "Normal");
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("QISRSessionEnd failed, errCode=" + ((ErrorCode)ret).ToString("G"));
            }
            ptrSessionID = IntPtr.Zero;
            Utils.CustomPrint("识别完成\r\n");
            yield break;
        }


        /// <summary>
        /// 处理错误
        /// </summary>
        /// <param name="errorMsg">错误的函数</param>
        /// <param name="ret">错误的返回值</param>
        /// <param name="sessionID">sessionID</param>
        /// <param name="fs">打开的文件</param>
        private void HandleErrorMsg(string errorMsg, int ret, string sessionID, FileStream fs) {
            Utils.CustomPrint(errorMsg + " err,errCode=" + ((ErrorCode)ret).ToString("G"));
            DllImports.QISRSessionEnd(sessionID, errorMsg);
            if (fs != null) {
                fs.Close();
                fs = null;
            }
        }

        /// <summary>
        /// 获取语法的ID
        /// </summary>
        /// <returns></returns>
        private string GetGrammarID() {
            int ret = -1;
            string temp = Application.dataPath;
#if UNITY_ANDROID
            temp = Application.persistentDataPath + "/gm_continuous_digit.abnf";
#elif UNITY_STANDALONE_WIN
            temp = Application.streamingAssetsPath + "/gm_continuous_digit.abnf";
#endif

            byte[] grammarBytes = Utils.ReadFile(temp);

            int grammar_len = grammarBytes.Length;

            IntPtr ptrGrammarID = DllImports.MSPUploadData("usergram", grammarBytes, (uint)grammar_len, "dtt = abnf, sub = asr", out ret);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("MSPUploadData error, errCode=" + ((ErrorCode)ret).ToString("G"));
                return null;
            }
            string grammarID = Marshal.PtrToStringAnsi(ptrGrammarID);
            Utils.CustomPrint("grammarID:" + grammarID + "\r\n");
            return grammarID;
        }
    }
}