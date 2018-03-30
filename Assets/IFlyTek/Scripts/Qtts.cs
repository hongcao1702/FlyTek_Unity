using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Second {
    /// <summary>
    /// 语音合成类(iFLY Speech Synthesizer)
    /// </summary>
    public class Qtts : IFlyInterface
    {
        private static string sessionTTSBeginParams = "voice_name = john, text_encoding = utf8, sample_rate = 16000, speed = 50, volume = 50, pitch = 50, rdn = 2";
        private byte[] text;
        private string textPath;

        public Qtts() {
            //string temp = "亲爱的用户，您好，这是一个语音合成示例，感谢您对科大讯飞语音技术的支持！科大讯飞是亚太地区最大的语音上市公司，股票代码：002230,test"; ///合成文本
            string temp = "What's your name,How old are you"; ///合成文本

            text = Encoding.GetEncoding("utf-8").GetBytes(ConvertEncoding(temp));
#if UNITY_ANDROID
            textPath = Application.persistentDataPath + "/tts_sample.wav";
#elif UNITY_STANDALONE_WIN
            textPath = Application.streamingAssetsPath + "/tts_sample.wav";
#endif
        }

        public IEnumerator RunFunc() {
            IEnumerator e = RunTTS();
            while (e.MoveNext()) {
                yield return e.Current;
            }
        }

        /// <summary>
        /// 运行语音合成功能
        /// </summary>
        /// <returns></returns>
        public IEnumerator RunTTS() {

            if(text == null) {
                yield break;
            }

            WaveFormat waveFormat = new WaveFormat();
            int nLength = 0;
            int nFormatChunkLength = 0x10;  // Format chunk length.
            short shPad = 1;                // File padding  

            int ret = 0;
            uint audioLen = 0;
            int sampleCount = 0;

            int synthStatus = (int)TTSStatus.MSP_TTS_FLAG_STILL_HAVE_DATA;

            IntPtr ptrSessionID = DllImports.QTTSSessionBegin(sessionTTSBeginParams, out ret);
            string sessionID = Marshal.PtrToStringAnsi(ptrSessionID);

            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("TTSSessionBegin failed, errCode=" + ((ErrorCode)ret).ToString("G"));
                yield break;
            }

            FileStream waveFile = new FileStream(textPath, FileMode.OpenOrCreate);
            BinaryWriter writer = new BinaryWriter(waveFile);

            char[] chunkRiff = { 'R', 'I', 'F', 'F' };
            char[] chunkType = { 'W', 'A', 'V', 'E' };
            char[] chunkFmt = { 'f', 'm', 't', ' ' };
            char[] chunkData = { 'd', 'a', 't', 'a' };

            //RIFF块
            writer.Write(chunkRiff);
            writer.Write(nLength);
            writer.Write(chunkType);

            //WAVE块
            writer.Write(chunkFmt);
            writer.Write(nFormatChunkLength);
            writer.Write(shPad);
            writer.Write(waveFormat.mChannels);
            writer.Write(waveFormat.mSamplesPerSecond);
            writer.Write(waveFormat.mAverageBytesPerSecond);
            writer.Write(waveFormat.mBlockAlign);
            writer.Write(waveFormat.mBitsPerSample);

            //数据块    
            writer.Write(chunkData);
            writer.Write((int)0);   // The sample length will be written in later.  

            ret = DllImports.QTTSTextPut(sessionID, text, (uint)text.Length, null);
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                HandleErrorMsg("QTTSTextPut", ret, sessionID, writer, waveFile);
                yield break;
            }

            Utils.CustomPrint("正在合成 ...\n");

            while (true) {
                ///获取合成音频
                IntPtr data = DllImports.QTTSAudioGet(sessionID, ref audioLen, ref synthStatus, out ret);
                if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                    break;
                }
                if (IntPtr.Zero != data) {
                    byte[] buff = new byte[audioLen];
                    Marshal.Copy(data, buff, 0, buff.Length);
                    writer.Write(buff, 0, buff.Length);
                    sampleCount += (int)audioLen;///计算data_size大小
                    buff = null;
                }
                if(TTSStatus.MSP_TTS_FLAG_DATA_END == (TTSStatus)synthStatus) {
                    break;
                }
                Utils.CustomPrint(">");
                yield return new WaitForSeconds(0.2f);
            }

            if(ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("QTTSAudioGet failed, errCode=" + ((ErrorCode)ret).ToString("G"));
                DllImports.QTTSSessionEnd(sessionID, "AudioGetError");
                writer.Close();
                waveFile.Close();
                yield break;
            }

            //写WAV文件尾    
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((int)(sampleCount + 36));
            writer.Seek(40, SeekOrigin.Begin);
            writer.Write(sampleCount);

            writer.Close();
            waveFile.Close();
            writer = null;
            waveFile = null;

            ///合成完毕
            ret = DllImports.QTTSSessionEnd(sessionID, "Normal");
            if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
                Utils.CustomPrint("QTTSSessionEnd failed, errCode=" + ((ErrorCode)ret).ToString("G"));
            }
            ptrSessionID = IntPtr.Zero;
            Utils.CustomPrint("合成完毕");
            yield break;
        }

        /// <summary>
        /// 处理错误
        /// </summary>
        /// <param name="errorMsg">错误的函数</param>
        /// <param name="ret">错误的返回值</param>
        /// <param name="sessionID">sessionID</param>
        /// <param name="writer">打开的文件</param>
        /// <param name="fs">打开的文件</param>
        private static void HandleErrorMsg(string errorMsg, int ret, string sessionID, BinaryWriter writer,FileStream fs) {
            Utils.CustomPrint(errorMsg + " err,errCode=" + ((ErrorCode)ret).ToString("G"));
            DllImports.QTTSSessionEnd(sessionID, errorMsg);
            if(writer != null) {
                writer.Close();
                writer = null;
            }
            if (fs != null) {
                fs.Close();
                fs = null;
            }
        }

        /// <summary>
        /// 把各平台字符串转换为UTF-8格式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string ConvertEncoding(string source) {
            string encodingName = Encoding.Default.EncodingName.ToLower();
            if (encodingName.Equals("utf-8")) {
                return source;
            }

            Encoding utf8 = Encoding.GetEncoding("utf-8");
            if (encodingName.Contains("gb2312")) {///PC平台一般是gb2312编码
                Encoding gb2312 = Encoding.GetEncoding("gb2312");
                return utf8.GetString(Encoding.Convert(gb2312, utf8, gb2312.GetBytes(source)));
            } else if (encodingName.Contains("unicode")) {///Android平台一般是unicode编码
                return utf8.GetString(Encoding.Convert(Encoding.Unicode, utf8, Encoding.Unicode.GetBytes(source)));
            }
            return source;
        }

    }
}
