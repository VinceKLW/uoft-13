using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;

public class TTSManager : MonoBehaviour
{
    [SerializeField] private string openAIKey = "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";
    [SerializeField] private TextMeshProUGUI speechText;
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private float bubbleDisplayTime = 5f;
    
    private AudioSource audioSource;
    
    // Random Shopify mascot quips
    private string[] shopifyQuips = new string[]
    {
        "Ready to bag some sales!",
        "Let's turn browsers into buyers!",
        "Time to check out these awesome products!",
        "Your success is in the bag!",
        "Shopping cart? More like shopping art!",
        "Every sale starts with a click!",
        "I'm not just a bag, I'm a brand experience!",
        "Let's make commerce better for everyone!",
        "Checkout is my favorite word!",
        "From cart to conversion, I've got you covered!",
        "Who needs a physical store when you've got digital style?",
        "Shipping happiness, one order at a time!",
        "I may be a bag, but I carry big dreams!",
        "Let's optimize that conversion rate!",
        "Add to cart? More like add to awesome!",
        "I'm handle-ing your e-commerce needs!",
        "Retail therapy is now in session!",
        "Your store, your rules, my enthusiasm!",
        "Let's bag this sale and celebrate!",
        "I'm bullish on your business!"
    };

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Hide bubble at start
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
        
        Debug.Log("TTS Manager ready! Press SPACE for random quip!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Pick a random quip
            string randomQuip = shopifyQuips[Random.Range(0, shopifyQuips.Length)];
            Debug.Log("Shopify says: " + randomQuip);
            
            // Show the text bubble
            ShowSpeechBubble(randomQuip);
            
            // Generate speech
            StartCoroutine(GenerateSpeech(randomQuip));
        }
    }

    void ShowSpeechBubble(string text)
    {
        if (speechText != null)
        {
            speechText.text = text;
        }
        
        if (speechBubble != null)
        {
            speechBubble.SetActive(true);
            // Hide after a delay
            StartCoroutine(HideSpeechBubbleAfterDelay());
        }
    }

    IEnumerator HideSpeechBubbleAfterDelay()
    {
        yield return new WaitForSeconds(bubbleDisplayTime);
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
    }

    IEnumerator GenerateSpeech(string text)
    {
        string url = "https://api.openai.com/v1/audio/speech";
        
        // Escape quotes in the text
        string escapedText = text.Replace("\"", "\\\"");
        string jsonBody = "{\"model\":\"tts-1\",\"input\":\"" + escapedText + "\",\"voice\":\"nova\"}";
        
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