using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Second
{

    /// <summary>
    /// 语音评测类(iFLY Speech Evaluation)
    /// </summary>
    public class Qise : IFlyInterface
    {
        enum Category {
            READ_SYLLABLE_CN = 0,
            READ_WORD_CN     = 1,
            READ_SENTENCE_CN = 2,
            READ_WORD_EN     = 3,
            READ_SENTENCE_EN = 4,
        }

        private Category category = Category.READ_WORD_CN;

        public Qise() {
        }

        public IEnumerator RunFunc() {
            IEnumerator e = null;
            if (Application.platform == RuntimePlatform.Android) //如果是Android平台
            {
                string parameters, textPath, wavePath;
                GetValuesByCategory(category, out parameters, out textPath, out wavePath);
                e = Utils.CopyFileAndroid(textPath);
                while (e.MoveNext()) {
                    yield return e.Current;
                }
                e = Utils.CopyFileAndroid(wavePath);
                while (e.MoveNext()) {
                    yield return e.Current;
                }
            }
            e = RunISE();
            while (e.MoveNext()) {
                yield return e.Current;
            }
            yield break;
        }

        /// <summary>
        /// 运行语音评测功能
        /// </summary>
        /// <returns></returns>
        public IEnumerator RunISE() {
            int len;
            int audStatus = (int)AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;  ///音频状态
            int epStatus = (int)EpStatus.MSP_EP_NULL;                    ///端点检测
            int recStatus = (int)RecogStatus.MSP_REC_NULL;               ///识别状态
            bool first = true;
            uint rlstLen = 0;
            IntPtr rsltPrt = IntPtr.Zero;

            int ret = 0;
            const int BUFFER_NUM = 640 * 10;/// 每次写入200ms音频(16k，16bit)：1帧音频20ms，10帧=200ms。16k采样率的16位音频，一帧的大小为640Byte

            FileStream textFS = null;
            FileStream waveFS = null;

            string parameters, textPath, wavePath;
            GetValuesByCategory(category, out parameters, out textPath, out wavePath);

#if UNITY_ANDROID
            textPath = Application.persistentDataPath + "/" + textPath;
            wavePath = Application.persistentDataPath + "/" + wavePath;
#elif UNITY_STANDALONE_WIN
            textPath = Application.streamingAssetsPath + "/" + textPath;
            wavePath = Application.streamingAssetsPath + "/" + wavePath;
#endif

            IntPtr ptrSessionID = DllImports.QISESessionBegin(parameters, "", out ret);
            string sessionID = Marshal.PtrToStringAnsi(ptrSessionID);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("QISESessionBegin err,errCode=" + ((ErrorCode)ret).ToString("G"));
                yield break;
            }

            if (!File.Exists(textPath) || !File.Exists(wavePath)) {
                Utils.CustomPrint("文件" + textPath + " 或者 " + wavePath + "不存在！");
                DllImports.QISESessionEnd(sessionID,"File not exist");
                yield break;
            }                                                                                                         
            textFS = new FileStream(textPath, FileMode.Open);
            int textLength = (int)textFS.Length;
            byte[] buff = new byte[textLength];
            textFS.Read(buff, 0, textLength);
            textFS.Close();
            textFS = null;

            ret = DllImports.QISETextPut(sessionID, Encoding.Default.GetString(buff), (uint)textLength, "");
            Utils.CustomPrint("Encoding:" + Encoding.Default.EncodingName.ToLower());
            Utils.CustomPrint(Regex.Unescape(Encoding.Default.GetString(buff)) + "|length:" + textLength);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                HandleErrorMsg("QISETextPut", ret, sessionID, null);
                yield break;
            }

            waveFS = new FileStream(wavePath, FileMode.Open);
            buff = new byte[BUFFER_NUM];
            IntPtr bp = Marshal.AllocHGlobal(BUFFER_NUM);

            while (waveFS.Position != waveFS.Length) {
                len = waveFS.Read(buff, 0, BUFFER_NUM);
                Marshal.Copy(buff, 0, bp, buff.Length);

                audStatus = (int)AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;
                if (first) {
                    audStatus = (int)AudioStatus.MSP_AUDIO_SAMPLE_FIRST;
                    first = false;
                }

                Utils.CustomPrint(">" + len);
                ///开始向服务器发送音频数据
                ret = DllImports.QISEAudioWrite(sessionID, bp, (uint)len, audStatus, ref epStatus, ref recStatus);
                if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                    HandleErrorMsg("QISEAudioWrite", ret, sessionID, waveFS);
                    yield break;
                }

                if (RecogStatus.MSP_REC_STATUS_SUCCESS == (RecogStatus)recStatus) {
                    rsltPrt = DllImports.QISEGetResult(sessionID, ref rlstLen, ref recStatus, ref ret);
                    if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                        HandleErrorMsg("QISEGetResult", ret, sessionID, waveFS);
                        yield break;
                    }
                    if(IntPtr.Zero != rsltPrt) {
                        Utils.CustomPrint("rlstLen:" + rlstLen);
                        byte[] data = new byte[rlstLen];
                        Marshal.Copy(rsltPrt, data, 0, (int)rlstLen);
                        Utils.CustomPrint("+++传完音频后返回结果！:" + GBToUnicode(data, data.Length));
                    }
                }

                if (EpStatus.MSP_EP_AFTER_SPEECH == (EpStatus)epStatus) {
                    break;
                }
                ///sleep一下很有必要，防止MSC端无缓冲的识别结果时浪费CPU资源
                yield return new WaitForSeconds(0.2f);
            }
            buff = null;
            waveFS.Close();
            waveFS = null;
            Marshal.FreeHGlobal(bp);

            ret = DllImports.QISEAudioWrite(sessionID, IntPtr.Zero, 0, (int)AudioStatus.MSP_AUDIO_SAMPLE_LAST, ref epStatus, ref recStatus);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                HandleErrorMsg("QISEAudioWrite", ret, sessionID, null);
                yield break;
            }

            while (RecogStatus.MSP_REC_STATUS_COMPLETE != (RecogStatus)recStatus) {
                rsltPrt = DllImports.QISEGetResult(sessionID, ref rlstLen, ref recStatus, ref ret);
                if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                    HandleErrorMsg("QISEGetResult", ret, sessionID, null);
                    yield break;
                }
                if (IntPtr.Zero != rsltPrt) {
                    Utils.CustomPrint("rlstLen:" + rlstLen);
                    byte[] data = new byte[rlstLen];
                    Marshal.Copy(rsltPrt, data, 0, (int)rlstLen);
                    Utils.CustomPrint("---传完音频后返回结果！:" + GBToUnicode(data, data.Length));
                }
                yield return new WaitForSeconds(0.2f);
            }
            rsltPrt = IntPtr.Zero;
            ret = DllImports.QISESessionEnd(sessionID, "Normal");
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("QISESessionEnd failed, errCode=" + ((ErrorCode)ret).ToString("G"));
            }
            ptrSessionID = IntPtr.Zero;
            Utils.CustomPrint("评测完成\r\n");
            yield break;
        }


        /// <summary>
        /// 处理错误
        /// </summary>
        /// <param name="errorMsg">错误的函数</param>
        /// <param name="ret">错误的返回值</param>
        /// <param name="sessionID">sessionID</param>
        /// <param name="fs">打开的文件</param>
        private static void HandleErrorMsg(string errorMsg,int ret, string sessionID,FileStream fs) {
            Utils.CustomPrint(errorMsg + " err,errCode=" + ((ErrorCode)ret).ToString("G"));
            DllImports.QISESessionEnd(sessionID, errorMsg);
            if(fs != null) {
                fs.Close();
                fs = null;
            }
        }

        /// <summary>
        /// 把各平台字符串转换为UTF-8格式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string ConvertEncoding(byte[] source) {
            StringBuilder sb = new StringBuilder();
            for(int i=0; i<source.Length; i+=2) {
                if(i<source.Length-1) {
                    sb.AppendFormat("\\u{0:x2}{1:x2}", source[i + 1], source[i]);
                } else {
                    sb.AppendFormat("\\u{0:x2}",source[i]);
                }
            }
            return sb.ToString();
        }

        public static string GBToUnicode(byte[] buffer, int length) {
            //使用list存储从GB2312转成Unicode的字节码
            List<byte> data = new List<byte>();

            int i = 0;
            while (i < length) {
                //若字节码小于0xa1，说明表示ascii码，直接在高位补上0x00，即可转换成Unicode码
                if (buffer[i] < 0xa1) {
                    data.Add(buffer[i]);
                    data.Add(0x00);
                    i++;
                } else {
                    int value = buffer[i];

                    //GB2312将前一个字节与后一个字节组成一个汉字编码
                    value = ((value << 8) & 0xff00) | (buffer[i + 1] & 0xff);
                    //查找对应的Unicode编码
                    int index = Unicode.DichotomySearch(value, Unicode.GetLength(), 0);
                    if (index == -1)
                        return "";
                    value = Unicode.GB2312ToUnicode[index + 1];

                    //将找到Unicode编码分成两个字节，分别存储在byte集合中
                    int temp = (value >> 8) & 0xff;
                    value = value & 0x00ff;
                    data.Add((byte)value);
                    data.Add((byte)temp);
                    i += 2;
                }
            }
            byte[] dataBuffer = new byte[data.Count];
            //将Byte集合中的字节码存进byte[].
            for (int j = 0; j < data.Count; j++) {
                dataBuffer[j] = data[j];
            }
            //return data.ToString();
            //输出字符编码所对应的字符串
            return Encoding.Unicode.GetString(dataBuffer, 0, dataBuffer.Length);
        }

        private string StringToUnicode(byte[] bytes) {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length-1; i += 2) {
                // 取两个字符，每个字符都是右对齐。
                stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 获取对应的参数
        /// </summary>
        /// <param name="category"></param>
        /// <param name="parameters"></param>
        /// <param name="textPath"></param>
        /// <param name="wavePath"></param>
        private void GetValuesByCategory(Category category,out string parameters,out string textPath,out string wavePath) {
            switch(category) {
                case Category.READ_SENTENCE_CN:
                    textPath = "cn_sentence.txt";
                    wavePath = "cn_sentence.wav";
                    parameters = "sub=ise,category=read_sentence,language=zh_cn,aue=speex-wb;7,auf=audio/L16;rate=16000";
                    break;
                case Category.READ_SENTENCE_EN:
                    textPath = "en_sentence.txt";
                    wavePath = "en_sentence.wav";
                    parameters = "sub=ise,category=read_sentence,language=en_us,aue=speex-wb;7,auf=audio/L16;rate=16000";
                    break;
                case Category.READ_SYLLABLE_CN:
                    textPath = "cn_syll.txt";
                    wavePath = "cn_syll.wav";
                    parameters = "sub=ise,category=read_syllable,language=zh_cn,aue=speex-wb;7,auf=audio/L16;rate=16000";
                    break;
                case Category.READ_WORD_CN:
                    textPath = "cn_word.txt";
                    wavePath = "cn_word.wav";
                    parameters = "sub=ise,category=read_word,language=zh_cn,aue=speex-wb;7,auf=audio/L16;rate=16000";
                    break;
                case Category.READ_WORD_EN:
                default:
                    textPath = "en_word.txt";
                    wavePath = "en_word.wav";
                    parameters = "sub=ise,category=read_word,language=en_us,aue=speex-wb;7,auf=audio/L16;rate=16000";
                    break;
            }
        }
    }
}