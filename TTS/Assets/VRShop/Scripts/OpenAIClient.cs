using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VRShop
{
    /// <summary>
    /// Client for OpenAI API - handles text generation and text-to-speech
    /// </summary>
    public class OpenAIClient : MonoBehaviour
    {
        [Header("API Configuration")]
        [Tooltip("Your OpenAI API key (keep secret!)")]
        [SerializeField] private string apiKey = "";
        
        [Tooltip("Model to use for chat completions")]
        [SerializeField] private string chatModel = "gpt-4o-mini";
        
        [Tooltip("Model to use for text-to-speech")]
        [SerializeField] private string ttsModel = "tts-1";
        
        [Tooltip("Voice for text-to-speech")]
        [SerializeField] private string ttsVoice = "shimmer"; // shimmer = highest/squeakiest voice

        [Header("Generation Settings")]
        [SerializeField] private float temperature = 1.0f; // Higher = more creative/silly
        [SerializeField] private int maxTokens = 30; // Keep it short!

        private const string CHAT_ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string TTS_ENDPOINT = "https://api.openai.com/v1/audio/speech";

        /// <summary>
        /// Generate text using OpenAI chat completion
        /// </summary>
        public void GenerateText(string systemPrompt, string userPrompt, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(GenerateTextCoroutine(systemPrompt, userPrompt, onSuccess, onError));
        }

        /// <summary>
        /// Convert text to speech and return audio clip
        /// </summary>
        public void TextToSpeech(string text, Action<AudioClip> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(TextToSpeechCoroutine(text, onSuccess, onError));
        }

        /// <summary>
        /// Generate text and immediately convert to speech
        /// </summary>
        public void GenerateAndSpeak(string systemPrompt, string userPrompt, Action<string, AudioClip> onSuccess, Action<string> onError = null)
        {
            GenerateText(systemPrompt, userPrompt, 
                generatedText => {
                    TextToSpeech(generatedText, 
                        audioClip => onSuccess?.Invoke(generatedText, audioClip),
                        onError);
                },
                onError);
        }

        private IEnumerator GenerateTextCoroutine(string systemPrompt, string userPrompt, Action<string> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("OpenAI API key not set!");
                yield break;
            }

            var requestBody = new ChatCompletionRequest
            {
                model = chatModel,
                messages = new Message[]
                {
                    new Message { role = "system", content = systemPrompt },
                    new Message { role = "user", content = userPrompt }
                },
                temperature = temperature,
                max_tokens = maxTokens
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            Debug.Log($"[OpenAIClient] Request URL: {CHAT_ENDPOINT}");
            Debug.Log($"[OpenAIClient] Request body: {jsonBody}");
            Debug.Log($"[OpenAIClient] API Key (first 20 chars): {apiKey.Substring(0, Mathf.Min(20, apiKey.Length))}...");
            
            using (var request = new UnityWebRequest(CHAT_ENDPOINT, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[OpenAIClient] Chat error: {request.error}");
                    Debug.LogError($"[OpenAIClient] Response: {request.downloadHandler.text}");
                    onError?.Invoke(request.error);
                }
                else
                {
                    var response = JsonUtility.FromJson<ChatCompletionResponse>(request.downloadHandler.text);
                    if (response.choices != null && response.choices.Length > 0)
                    {
                        string text = response.choices[0].message.content;
                        Debug.Log($"[OpenAIClient] Generated: {text}");
                        onSuccess?.Invoke(text);
                    }
                    else
                    {
                        onError?.Invoke("No response from OpenAI");
                    }
                }
            }
        }

        private IEnumerator TextToSpeechCoroutine(string text, Action<AudioClip> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("OpenAI API key not set!");
                yield break;
            }

            var requestBody = new TTSRequest
            {
                model = ttsModel,
                input = text,
                voice = ttsVoice
            };

            string jsonBody = JsonUtility.ToJson(requestBody);

            using (var request = new UnityWebRequest(TTS_ENDPOINT, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[OpenAIClient] TTS error: {request.error}");
                    Debug.LogError($"[OpenAIClient] Response: {request.downloadHandler.text}");
                    onError?.Invoke(request.error);
                }
                else
                {
                    // OpenAI returns MP3 audio data
                    byte[] audioData = request.downloadHandler.data;
                    
                    // Convert MP3 to AudioClip (Unity doesn't natively support MP3)
                    // We'll save to temp file and load it
                    yield return StartCoroutine(LoadAudioFromMP3(audioData, onSuccess, onError));
                }
            }
        }

        private IEnumerator LoadAudioFromMP3(byte[] mp3Data, Action<AudioClip> onSuccess, Action<string> onError)
        {
            // Save MP3 to temp file
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"tts_{DateTime.Now.Ticks}.mp3");
            
            try
            {
                System.IO.File.WriteAllBytes(tempPath, mp3Data);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Failed to save audio: {e.Message}");
                yield break;
            }

            // Load the audio file
            using (var request = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Failed to load audio: {request.error}");
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    onSuccess?.Invoke(clip);
                }
            }

            // Clean up temp file
            try
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
            catch { }
        }

        // JSON serialization classes
        [Serializable]
        private class ChatCompletionRequest
        {
            public string model;
            public Message[] messages;
            public float temperature;
            public int max_tokens;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatCompletionResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class TTSRequest
        {
            public string model;
            public string input;
            public string voice;
        }
    }
}

