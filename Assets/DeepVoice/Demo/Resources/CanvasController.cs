using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using System;
using System.Text.RegularExpressions;




namespace AiKodexDeepVoice
{
    public class CanvasController : MonoBehaviour
    {
        public InputField text, fileName, invoice;
        public Dropdown model, voice;
        public Slider variability, clarity;
        public Button generate, save, intro, verify;
        public Text charcterCounter;
        public RawImage spinner, waveForm, overlayWaveForm, mask;
        public Button play, pause, stop;
        public AudioClip introClip;
        AudioClip audioClip;
        bool action;
        List<string> options0 = new List<string>() { "Olivia", "Emma", "Amy", "Brian", "Arthur", "Kajal", "Aria", "Ayanda", "Salli", "Kimberly", "Kendra", "Joanna", "Ivy", "Ruth", "Kevin", "Matthew", "Justin", "Joey", "Stephen" };
        List<string> options1 = new List<string>() { "Jessie","Harry","Glinda","Clyde","Freya","Fin","Dave","Charlotte","Callum","Arthur","Hailey", "Batman", "Andrew", "Anime_Girl","Valentina","Wayne","Jan","Noah", "Lily","Lily_Narrator", "Ethan", "Sophia", "Olivia", "Ruby", "Lucas", "John" };
        List<string> options2 = new List<string>() { "Jessie","Harry","Glinda","Clyde","Freya","Fin","Dave","Charlotte","Callum","Arthur","Hailey", "Batman", "Andrew", "Anime_Girl","Valentina","Wayne","Jan","Noah", "Lily","Lily_Narrator", "Ethan", "Sophia", "Olivia", "Ruby", "Lucas", "John" };
        List<string> options3 = new List<string>() { "Lotte", "Maxim", "Salli", "Ola", "Geraint", "Miguel", "Giorgio", "Marlene", "Ines", "Zhiyu", "Zeina", "Karl", "Gwyneth", "Lucia", "Cristiano", "Astrid", "Vicki", "Mia", "Vitoria", "Bianca", "Chantal", "Raveena", "Russell", "Aditi", "Dora", "Enrique", "Hans", "Carmen", "Ewa", "Maja", "Nicole", "Filiz", "Camila", "Jacek", "Celine", "Ricardo", "Mads", "Mathieu", "Lea", "Tatyana", "Penelope", "Naja", "Ruben", "Takumi", "Mizuki", "Carla", "Conchita", "Jan", "Liv", "Lupe", "Seoyeon" };
        float scrubber;
        AudioSource audioSource;
        void Start()
        {
            Button gen = generate.GetComponent<Button>();
            gen.onClick.AddListener(Generate);
            Button saveClip = save.GetComponent<Button>();
            save.onClick.AddListener(Save);
            Button playClip = play.GetComponent<Button>();
            play.onClick.AddListener(Play);
            Button stopClip = stop.GetComponent<Button>();
            stop.onClick.AddListener(Stop);
            Button pauseClip = pause.GetComponent<Button>();
            pause.onClick.AddListener(Pause);
            Button verifyInvoice = verify.GetComponent<Button>();
            verify.onClick.AddListener(VerifyInvoiceButton);
            spinner.enabled = false;

            Button introClip = intro.GetComponent<Button>();
            intro.onClick.AddListener(Intro);

            text.onValueChanged.AddListener(UpdateCharacterCount);

            model.onValueChanged.AddListener(delegate
            {
                ModelChange(model);
            });

            // Set default value of Dropdown
            model.value = 1;
            voice.onValueChanged.AddListener(delegate
            {
                VoiceChange(voice);
            });

            // Set default value of Dropdown
            voice.value = 0;
            AudioClip ac = AudioClip.Create("Test", 44000, 1, 2000, false);
            float[] samples = new float[44000];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Sin(Mathf.PI * i * 0.02f) * 0.33f;
            }
            ac.SetData(samples, 5000);
            waveForm.texture = PaintWaveformSpectrum(ac, Screen.width / 5, 100, Color.gray, false, 0);
            overlayWaveForm.texture = waveForm.texture;
            mask.rectTransform.sizeDelta = new Vector2(0, mask.rectTransform.sizeDelta.y);
            audioSource = GetComponent<AudioSource>();

        }
        void Generate()
        {
            if(invoice.text=="")
                Debug.Log("Please enter your invoice number before proceeding");
                
            if (text.text != "" && invoice.text!="")
            {
                text.text = Regex.Replace(text.text, @"\\(?!n|"")", "");
                text.text = Regex.Replace(text.text, "(?<!n)\n", "\\n");
                text.text = Regex.Replace(text.text, "(?<!\\\\)\"", "\\\"");
                if (model.value == 0)
                    StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text.text}" + "\",\"model\":\"" + "DeepVoice_Neural" + "\",\"name\":\"" + $"{voice.options[voice.value].text}" + "\",\"variability\":\"" + "0.0" + "\",\"invoice\":\"" + invoice.text + "\",\"clarity\":\"" + "0.0" + "\"}"));
                else if (model.value == 1)
                    StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text.text}" + "\",\"model\":\"" + "DeepVoice_Mono" + "\",\"name\":\"" + $"{voice.options[voice.value].text}" + "\",\"invoice\":\"" + invoice.text + "\",\"variability\":\"" + $"{variability.value}" + "\",\"clarity\":\"" + $"{clarity.value}" + "\"}"));
                else if (model.value == 2)
                    StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text.text}" + "\",\"model\":\"" + "DeepVoice_Multi" + "\",\"name\":\"" + $"{voice.options[voice.value].text}" + "\",\"invoice\":\"" + invoice.text + "\",\"variability\":\"" + $"{variability.value}" + "\",\"clarity\":\"" + $"{clarity.value}" + "\"}"));
                else if (model.value == 3)
                    StartCoroutine(Post("http://50.19.203.25:5000/invoice", "{\"text\":\"" + $"{text.text}" + "\",\"model\":\"" + "DeepVoice_Standard" + "\",\"name\":\"" + $"{voice.options[voice.value].text}" + "\",\"invoice\":\"" + invoice.text + "\",\"variability\":\"" + "0.0" + "\",\"clarity\":\"" + "0.0" + "\"}"));
                StartCoroutine(Timer());
            }

        }
        void VerifyInvoiceButton()
        {
            StartCoroutine(Verify("http://50.19.203.25:5000/verify", "{\"invoice\":\"" + invoice.text + "\"}"));
        }
        void UpdateCharacterCount(string character)
        {
            int currentCharacterCount = 500 - text.text.Length;
            charcterCounter.text = currentCharacterCount.ToString();
            charcterCounter.color = currentCharacterCount >= 0 ? Color.white : Color.red;
        }
        void Save()
        {
            if (fileName.text != "")
            {
                WaveUtility.Save($"{fileName.text}", audioClip, "Assets/DeepVoice/Voices", false);
                Debug.Log($"<color=green>Saved Successfully: </color>File saved to directory Assets/DeepVoice/Voices/{fileName.text}");
            }
            else
            {
                Debug.Log($"<color=yellow>File Name Empty: </color>Please name your file before proceeding.");
            }
        }
        void Play()
        {
            audioSource.clip = audioClip;
            if (Mathf.Approximately(scrubber, 1000))
                scrubber = 0;
            audioSource.time = scrubber / 1000 * audioClip.length;
            audioSource.Play();
        }
        void Pause()
        {
            audioSource.Stop();
        }
        void Stop()
        {
            audioSource.Stop();
            scrubber = 0;
        }
        void ModelChange(Dropdown model)
        {
            voice.ClearOptions();
            if (model.value == 0)
                voice.AddOptions(options0);
            else if (model.value == 1)
                voice.AddOptions(options1);
            else if (model.value == 2)
                voice.AddOptions(options2);
            else if (model.value == 3)
                voice.AddOptions(options3);
        }
        void VoiceChange(Dropdown voice) { }
        void Intro()
        {
            audioClip = introClip;
            waveForm.texture = PaintWaveformSpectrum(introClip, Screen.width / 5, 100, Color.white, false, 0);
            overlayWaveForm.texture = waveForm.texture;
        }
        IEnumerator Post(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            while (!request.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
            if (request.responseCode.ToString() != "200")
            {
                Debug.Log("There was an error in generating the voice. Please check your invoice/order number and try again.");
                spinner.enabled = false;
                action = false;

            }
            else
            {
                byte[] soundBytes = System.Convert.FromBase64String(request.downloadHandler.text);
                //Here is your downloaded file. Please perform the necessary CRUD (Create, read, update, delete) commands.
                string _directoryPath = Application.persistentDataPath + "/downloadedFile.mp3";
                File.WriteAllBytes(_directoryPath, soundBytes);
                //This should be the path the file is saved to. Please access the file from here.
                Debug.Log("File downloaded and saved to: " + _directoryPath);
                //If the file paths are incorrect, the code below with throw an error. Please use StreamingAssets / Resources folder to resolve the error with paths.
                waveForm.texture = PaintWaveformSpectrum(audioClip, Screen.width / 5, 100, Color.white, false, 0);
                overlayWaveForm.texture = waveForm.texture;
                action = true;
            }
        }
        IEnumerator Verify(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                if (request.downloadHandler.text == "Not Verified")
                    Debug.Log("Invoice/Order number verification unsuccessful. Please check your invoice/order number and try again or contact the publisher on the email given in the documentation.");
                else
                    Debug.Log("Your invoice is verified. Thank you for choosing DeepVoice!");
            }
            request.Dispose();
        }
        IEnumerator Timer()
        {
            generate.interactable = false;
            bool temp = action;
            while (temp == action)
            {
                spinner.enabled = true;
                spinner.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, -Time.time * 360);
                yield return null;
            }
            spinner.enabled = false;
            generate.interactable = true;
            action = false;
        }
        void UpdatePlayHead()
        {
            if (audioClip != null && scrubber != 1000 && audioSource.isPlaying)
            {
                scrubber += Time.deltaTime * 1000 / audioClip.length;
                scrubber = Mathf.Clamp(scrubber, 0, 1000);
            }
            mask.rectTransform.sizeDelta = new Vector2(scrubber, mask.rectTransform.sizeDelta.y);
        }
        void Update()
        {
            UpdatePlayHead();
        }
        public Texture2D PaintWaveformSpectrum(AudioClip audio, int width, int height, Color col, bool slider, float sliderValue)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float[] samples = new float[audio.samples];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);
            int packSize = (audio.samples / width) + 1;
            int s = 0;
            for (int i = 0; i < audio.samples; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }


            for (int i = 1; i < waveform.Length; i++)
            {
                var start = (i - 2 > 0 ? i - 2 : 0);
                var end = (i + 2 < waveform.Length ? i + 2 : waveform.Length);

                float sum = 0;

                for (int j = start; j < end; j++)
                {
                    sum += waveform[j];
                }

                var avg = sum / (end - start);
                waveform[i] = avg;

            }


            //Transparent BG
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            if (!slider)
            {
                for (int x = 0; x < waveform.Length; x = x + 2)
                {
                    for (int y = 0; y <= waveform[x] * height; y++)
                    {
                        tex.SetPixel(x, (height / 2) + y, col);
                        tex.SetPixel(x, (height / 2) - y, col);
                    }
                }
            }
            else
            {
                for (int x = 0; x < waveform.Length; x = x + 2)
                {
                    for (int y = 0; y <= waveform[x] * height; y++)
                    {
                        if (x < waveform.Length * sliderValue)
                        {
                            tex.SetPixel(x, (height / 2) + y, col);
                            tex.SetPixel(x, (height / 2) - y, col);
                        }
                    }
                }
            }
            tex.Apply();

            return tex;
        }

    }

    public static class WaveUtility
    {
        private const uint HeaderSize = 44;
        private const float RescaleFactor = 32767; //to convert float to Int16

        public static void Save(string filename, AudioClip clip, string directory, bool trim = false)
        {
            if (!filename.ToLower().EndsWith(".wav"))
            {
                filename += ".wav";
            }

            var filepath = Path.Combine(directory, filename);

            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using (var fileStream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                var wav = GetWav(clip, out var length, trim);
                writer.Write(wav, 0, (int)length);
            }
        }

        public static byte[] GetWav(AudioClip clip, out uint length, bool trim = false)
        {
            var data = ConvertAndWrite(clip, out length, out var samples, trim);

            WriteHeader(data, clip, length, samples);

            return data;
        }

        private static byte[] ConvertAndWrite(AudioClip clip, out uint length, out uint samplesAfterTrimming, bool trim)
        {
            var samples = new float[clip.samples * clip.channels];

            clip.GetData(samples, 0);

            var sampleCount = samples.Length;

            var start = 0;
            var end = sampleCount - 1;

            if (trim)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    if ((short)(samples[i] * RescaleFactor) == 0)
                        continue;

                    start = i;
                    break;
                }

                for (var i = sampleCount - 1; i >= 0; i--)
                {
                    if ((short)(samples[i] * RescaleFactor) == 0)
                        continue;

                    end = i;
                    break;
                }
            }

            var buffer = new byte[(sampleCount * 2) + HeaderSize];

            var p = HeaderSize;
            for (var i = start; i <= end; i++)
            {
                var value = (short)(samples[i] * RescaleFactor);
                buffer[p++] = (byte)(value >> 0);
                buffer[p++] = (byte)(value >> 8);
            }

            length = p;
            samplesAfterTrimming = (uint)(end - start + 1);
            return buffer;
        }

        private static void AddDataToBuffer(byte[] buffer, ref uint offset, byte[] addBytes)
        {
            foreach (var b in addBytes)
            {
                buffer[offset++] = b;
            }
        }

        private static void WriteHeader(byte[] stream, AudioClip clip, uint length, uint samples)
        {
            var hz = (uint)clip.frequency;
            var channels = (ushort)clip.channels;

            var offset = 0u;

            var riff = Encoding.UTF8.GetBytes("RIFF");
            AddDataToBuffer(stream, ref offset, riff);

            var chunkSize = BitConverter.GetBytes(length - 8);
            AddDataToBuffer(stream, ref offset, chunkSize);

            var wave = Encoding.UTF8.GetBytes("WAVE");
            AddDataToBuffer(stream, ref offset, wave);

            var fmt = Encoding.UTF8.GetBytes("fmt ");
            AddDataToBuffer(stream, ref offset, fmt);

            var subChunk1 = BitConverter.GetBytes(16u);
            AddDataToBuffer(stream, ref offset, subChunk1);

            //const ushort two = 2;
            const ushort one = 1;

            var audioFormat = BitConverter.GetBytes(one);
            AddDataToBuffer(stream, ref offset, audioFormat);

            var numChannels = BitConverter.GetBytes(channels);
            AddDataToBuffer(stream, ref offset, numChannels);

            var sampleRate = BitConverter.GetBytes(hz);
            AddDataToBuffer(stream, ref offset, sampleRate);

            var byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            AddDataToBuffer(stream, ref offset, byteRate);

            var blockAlign = (ushort)(channels * 2);
            AddDataToBuffer(stream, ref offset, BitConverter.GetBytes(blockAlign));

            ushort bps = 16;
            var bitsPerSample = BitConverter.GetBytes(bps);
            AddDataToBuffer(stream, ref offset, bitsPerSample);

            var dataString = Encoding.UTF8.GetBytes("data");
            AddDataToBuffer(stream, ref offset, dataString);

            var subChunk2 = BitConverter.GetBytes(samples * 2);
            AddDataToBuffer(stream, ref offset, subChunk2);
        }
    }

}