using UnityEngine;
using Second;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

public class MainDemo : MonoBehaviour {

    private FlyTek flyTek;

    private FlyMode flyMode = FlyMode.QTTS;

    private bool calling = false;

    public AudioSource audioSource;
    private string textPath;

    // Use this for initialization
    void Start() {
#if UNITY_ANDROID
        textPath = "file:///" + Application.persistentDataPath + "/tts_sample.wav";
#elif UNITY_STANDALONE_WIN
        textPath = Application.streamingAssetsPath + "/tts_sample.wav";
#endif
        flyTek = GetComponent<FlyTek>();
        List<string> btnsName = new List<string>();
        btnsName.Add("TTS");
        btnsName.Add("ISR");
        btnsName.Add("ISE");
        foreach (string btnName in btnsName) {
            GameObject btnObj = GameObject.Find(btnName);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(delegate () {
                this.OnClick(btnObj);
            });
        }
    }

    public void OnClick(GameObject sender) {
        switch (sender.name) {
            case "TTS":
                flyMode = FlyMode.QTTS;
                if (!calling) {
                    StartCoroutine(Test());
                }
                break;
            case "ISR":
                flyMode = FlyMode.QISR;
                if (!calling) {
                    StartCoroutine(Test());
                }
                break;
            case "ISE":
                flyMode = FlyMode.QISE;
                if (!calling) {
                    StartCoroutine(Test());
                }
                break;
            default:
                Debug.Log("none");
                break;
        }
    }

    private IEnumerator Test() {
        calling = true;
        if (flyTek != null) {
            IEnumerator e = flyTek.StartFlyTek(flyMode);
            while (e.MoveNext()) {
                yield return e.Current;
            }
            if(flyMode == FlyMode.QTTS) {
                e = LoadMusic(textPath);
                while (e.MoveNext()) {
                    yield return e.Current;
                }
            }
        }
        calling = false;
    }

    private IEnumerator LoadMusic(string filepath) {
        Utils.CustomPrint("+++++1:" + filepath);
        WWW www = new WWW(filepath);
        while (!www.isDone) {
        }
        yield return www;
        if (www.isDone) {
            Utils.CustomPrint("+++++2");
            audioSource.clip = www.GetAudioClip();
            audioSource.Play();
        } else {
            Utils.CustomPrint("+++++3");
        }
    }
}
