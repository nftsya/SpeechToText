using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.SpeechToText.v1;
using Newtonsoft.Json.Linq;
using Google.Cloud.Speech.V1;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SpeechToText
{
    public partial class Form1 : Form
    {
        string filepath = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.Filter = "Файлы mp3|*.mp3";
            if (OPF.ShowDialog() == DialogResult.OK) {filepath = OPF.FileName;}
        }

        //IBM Watson
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            string key = "weUJxiXv-R9A96AuovjTWhqKuRl25jpIb-SV4ebvh24t";
            string url = "https://api.eu-de.speech-to-text.watson.cloud.ibm.com/instances/d06d44af-8045-4752-852f-832c674d4de9";

            IamAuthenticator authenticator = new IamAuthenticator(apikey: key);

            SpeechToTextService speechToText = new SpeechToTextService(authenticator);
            speechToText.SetServiceUrl(url);

            var result = speechToText.Recognize(
                audio: new MemoryStream(File.ReadAllBytes(filepath)),
                contentType: "audio/mp3",
                endOfPhraseSilenceTime: 10
                );
            JObject json = JObject.Parse(result.Response);
            textBox1.AppendText(json["results"][0]["alternatives"][0]["transcript"].ToString());
        }

        //Google Cloud Platform
        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = "";
            var speech = new SpeechClientBuilder
            {
                CredentialsPath = "client_secret2.json"
            }.Build();

            var response = speech.Recognize(new RecognitionConfig()
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.EncodingUnspecified,
                SampleRateHertz = 48000,
                LanguageCode = "en",
            }, RecognitionAudio.FromFile(filepath));

            textBox2.Text = "";

            foreach (var result in response.Results)
            {
                foreach (var alternative in result.Alternatives)
                {
                    textBox2.Text = textBox2.Text + " " + alternative.Transcript;
                }
            }
        }

        //Microsoft Azure
        string result123;
        public async Task ContinuousRecognitionWithFileAsync()
        {
            var config = SpeechConfig.FromSubscription("289c88a602f64297a01812078354facf", "eastus");
            var stopRecognition = new TaskCompletionSource<int>();
            //MP3 to WAV
            string tempFileName = Path.GetTempFileName();
            Mp3FileReader mp3FileReader = new Mp3FileReader(filepath);
            WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3FileReader);
            WaveFileWriter.CreateWaveFile(tempFileName, pcm);

            using (var audioInput = AudioConfig.FromWavFileInput(tempFileName))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    // конечный результат
                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            result123 += e.Result.Text;
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            result123 = "Speech could not be recognized.";
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        stopRecognition.TrySetResult(0);
                    };

                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    Task.WaitAny(new[] { stopRecognition.Task });

                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
        }

        async private void button4_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
            result123 = "";
            await ContinuousRecognitionWithFileAsync();
            textBox3.Text = textBox3.Text + " " + result123;
        }

        //SpeechText.AI
        string resp;
        public async Task SpeechApi()
        {
            string url = "https://api.speechtext.ai/recognize?key=ec3275eab16f41c8aa6b5e4b238ef64e&language=en-US&punctuation=true&format=mp3";

            HttpClient client = new HttpClient();

            var content = new MultipartFormDataContent();
            var audioContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filepath));
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/3gpp");
            content.Add(audioContent, "audio", "testaudio.3gpp");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            var httpResponseMessage = await client.PostAsync(url, content);

            var response = await httpResponseMessage.Content.ReadAsStringAsync();

            JObject json = JObject.Parse(response);
            var task = json["id"].ToString();

            string newurl = "https://api.speechtext.ai/results?key=ec3275eab16f41c8aa6b5e4b238ef64e" + "&task=" + task + "&summary=true&summary_size=15&highlights=true&max_keywords=15";
            while (true)
            {
                var httpResponseMessage2 = await client.GetAsync(newurl);
                var response2 = await httpResponseMessage2.Content.ReadAsStringAsync();
                JObject json2 = JObject.Parse(response2);

                if (json2["status"].ToString() == "failed")
                {
                    resp = "Failed to transcribe!";
                    break;
                }
                if (json2["status"].ToString() == "finished")
                {
                    resp = json2["results"]["transcript"].ToString().Replace("<kw>", "").Replace("</kw>", "");
                    break;
                }
                // sleep for 15 seconds if the task has the status - 'processing'
                System.Threading.Thread.Sleep(15);
            }

        }
        async private void button5_Click(object sender, EventArgs e)
        {
            textBox4.Text = "";
            await SpeechApi();
            textBox4.Text = textBox4.Text + " " + resp;
        }
    }
}

    
    

