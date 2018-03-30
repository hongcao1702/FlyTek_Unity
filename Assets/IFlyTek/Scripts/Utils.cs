using System.Collections;
using System.IO;
using UnityEngine;

namespace Second {
    public class Utils {
        public static void CustomPrint(string printstring) {
            Debug.Log(printstring);
        }

        /// <summary>
        /// 读取fileName文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static byte[] ReadFile(string fileName) {
            FileStream pFileStream = null;
            byte[] pReadByte = new byte[0];
            try {
                pFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                BinaryReader r = new BinaryReader(pFileStream);
                r.BaseStream.Seek(0, SeekOrigin.Begin);    ///将文件指针设置到文件开
                pReadByte = r.ReadBytes((int)r.BaseStream.Length);
                return pReadByte;
            } catch {
                return pReadByte;
            } finally {
                if (pFileStream != null) {
                    pFileStream.Close();
                }    
            }
        }

        /// <summary>
        /// 写byte[]到fileName
        /// </summary>
        /// <param name="pReadByte"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool WriteFile(byte[] pReadByte, string fileName) {
            FileStream pFileStream = null;
            try {
                pFileStream = new FileStream(fileName, FileMode.OpenOrCreate);
                pFileStream.Write(pReadByte, 0, pReadByte.Length);
            } catch {
                return false;
            } finally {
                if (pFileStream != null) {
                    pFileStream.Close();
                }
            }
            return true;
        }


        /// <summary>
        /// 把assets下面的文件写入SD卡中
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static IEnumerator CopyFileAndroid(string fileName) {
            Utils.CustomPrint("Call CopyFileAndroid");
            WWW w = new WWW(Application.streamingAssetsPath + "/" + fileName);
            Utils.CustomPrint("streamingAssetsPath" + Application.streamingAssetsPath + "/" + fileName);
            Utils.CustomPrint("streamingAssetsPath" + Application.persistentDataPath + "/" + fileName);
            yield return w;
            if (w.error == null) {
                FileInfo fi = new FileInfo(Application.persistentDataPath + "/" + fileName);
                //判断文件是否存在
                if (!fi.Exists) {
                    FileStream fs = fi.OpenWrite();
                    fs.Write(w.bytes, 0, w.bytes.Length);
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                    Utils.CustomPrint("CopyTxt Success!" + "\n" + "Path: ======> " + Application.persistentDataPath + "/" + fileName);
                }
            } else {
                Utils.CustomPrint("Error : ======> " + w.error);
            }
        }

    }
}
