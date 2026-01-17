using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class TTSManager : MonoBehaviour
{
    [SerializeField] private string openAIKey = "YOUR_API_KEY_HERE";
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        Debug.Log("TTS Manager ready!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Generating speech...");
            StartCoroutine(GenerateSpeech("Hello! This is a test of text to speech."));
        }
    }

    IEnumerator GenerateSpeech(string text)
    {
        string url = "https://api.openai.com/v1/audio/speech";
        
        // Create JSON manually
        string jsonBody = "{\"model\":\"tts-1\",\"input\":\"" + text + "\",\"voice\":\"alloy\"}";
        
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + openAIKey);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("Playing speech!");
            }
            else
            {
                Debug.LogError("TTS Error: " + www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            }
        }
    }
}