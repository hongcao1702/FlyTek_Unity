using UnityEngine;
using System.Collections;
using System.IO;
using Second;
using System.Text;

public class FlyTek : MonoBehaviour
{
#if UNITY_STANDALONE_WIN
    private string loginParams = "appid = 5aa645e7, work_dir = .";
#elif UNITY_ANDROID
    private string loginParams = "appid = 5aa646b6, work_dir = .";
#endif

    private void Start() {

        int ret = DllImports.MSPLogin(null, null, loginParams);
        if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
            Utils.CustomPrint("MSPLogin failed, errCode=" + ((ErrorCode)ret).ToString("G"));
        }
        Utils.CustomPrint("+++MSPLogin");
    }

    public IEnumerator StartFlyTek(FlyMode flyMode) {
        Utils.CustomPrint("StartFlyTek");
        IFlyInterface flyInterface = null;
        switch (flyMode) {
            case FlyMode.QISR:
                flyInterface = new Qisr();
                break;
            case FlyMode.QISE:
                flyInterface = new Qise();
                break;
            case FlyMode.QTTS:
                flyInterface = new Qtts();
                break;
        }
        if (flyInterface != null) {
            IEnumerator e = flyInterface.RunFunc();
            while (e.MoveNext()) {
                yield return e.Current;
            }
        }
        flyInterface = null;
    }

    private void OnDisable() {
        int ret = DllImports.MSPLogout();
        if (ErrorCode.MSP_SUCCESS != (ErrorCode)ret) {
            Utils.CustomPrint("MSPLogout failed, errCode=" + ((ErrorCode)ret).ToString("G"));
        }
        Utils.CustomPrint("+++MSPLogout");
    }

}
