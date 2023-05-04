using System.Net;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;


namespace UnityLibrary
{
    public class StableTexture : EditorWindow
    {
        bool isSettingsOpen = false;
        static string url = "http://127.0.0.1:9090/";
        static string installationFolder = @"C:\Users\aravi\InvokeAI-release-1.14.1";
        static UnityWebRequest www;
        static string prompt = "empty";
        static int iterations = 1;
        static int steps = 50;
        static string seed = "-1";
        static bool seamless = false;
        static float cfg_scale = 7.5f;
        static string samplerName = "k_euler_a";
        static float variation_amount = 0;
        static string with_variations = "";
        static float strength = 0.75f;
        static float gfpgan_strength = 0.8f;
        static string upscale_level = "";
        static float upscale_strength = 0.75f;
        static Texture2D initimg = null;
        static string initimg_name = "";
        static bool fit = true;
        static string[] options_char = {"64","128","256","512","1024" };
        static int[] options_int = { 64,128,256,512,1024};
        
        
        static string lastImgPath = null;
        static int sizeIndex = 1;
        
        static string importFolder = "Assets/Textures";
        long lastSeed = -1;
        static Texture2D tex;
        static string prefix = "StableUI_";

        int progressValue = 0;
        int progressMax = 0;

        int panelWidth = 333;


        HttpWebResponse response;
        StreamReader reader;
        string fullResponse="";


        // Start is called before the first frame update
        [MenuItem("Addon/StableUI")]

        public static void Init()
        {
            var window = GetWindow(typeof(StableTexture));
            window.titleContent = new GUIContent("StableTexture");
            window.minSize = new Vector2(720, 512);
            
        }
        //......................................................
        void OnEnable()
        {
            LoadPrefs();
            EditorApplication.update -= EditorUpdate;
        }
        void OnDisable()
        {
            SavePrefs();
        }


        //GUI Contents...........................................................
        void OnGUI()
        {
            if (isSettingsOpen == true || installationFolder == "")
            {
                if (isSettingsOpen == false) isSettingsOpen = true;

                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

                url = EditorGUILayout.TextField("URL", url);
                installationFolder = EditorGUILayout.TextField("Installation folder", installationFolder);
                importFolder = EditorGUILayout.TextField("Import folder", importFolder);

                EditorGUILayout.Space(20);
                if (GUILayout.Button(new GUIContent("Save", "Save settings"), GUILayout.Width(250), GUILayout.Height(24)))
                {
                    isSettingsOpen = false;
                }
                return;
            }

            EditorGUILayout.BeginHorizontal("box", GUILayout.Width(EditorGUIUtility.currentViewWidth - 10), GUILayout.Height(position.height - 10));

            // settings panel
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(panelWidth));

            EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
            EditorStyles.textArea.wordWrap = true;

            prompt = EditorGUILayout.TextArea(prompt, EditorStyles.textArea, GUILayout.Height(80));

            if (GUILayout.Button("Generate", GUILayout.Height(44))) Generate();

            // progress bar
            Rect rect = EditorGUILayout.BeginVertical();
            GUILayout.Button("dummy", GUILayout.Height(1));
            EditorGUILayout.EndVertical();
            //This box will cover all controls between the former BeginVertical() & EndVertical()
            EditorGUI.DrawRect(rect, Color.black);
            rect.width = rect.width * (progressValue / (float)(progressMax + 1));
            EditorGUI.DrawRect(rect, Color.green);

            EditorGUILayout.Space(10);

            // --------------- SETTINGS ----------------
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            steps = EditorGUILayout.IntField("Steps", steps);
            //samplerIndex = EditorGUILayout.Popup("Sampler", samplerIndex, samplerOptions);
            sizeIndex = EditorGUILayout.Popup("sizeIndex", sizeIndex, options_char);
            

            EditorGUILayout.BeginHorizontal();
            seed = EditorGUILayout.TextField("Seed", seed);
            if (GUILayout.Button("x", GUILayout.Width(32))) seed = "-1";
            EditorGUILayout.EndHorizontal();

            seamless = EditorGUILayout.Toggle("Seamless", seamless);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Init image");
            initimg = (Texture2D)EditorGUILayout.ObjectField(initimg, typeof(Texture2D), true, GUILayout.Width(64), GUILayout.Height(64));
            // unity 2020.1+ can read camera icon with EditorGUIUtility.IconContent("Camera Gizmo") // https://github.com/halak/unity-editor-icons
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(new GUIContent("Clr", "Clear image"), GUILayout.Width(38))) initimg = null;
            if (GUILayout.Button(new GUIContent("Grab", "Screenshot from MainCamera"), GUILayout.Width(38))) TakeScreenshot();
            //if (GUILayout.Button(new GUIContent("<<<", "Use latest generated image"), GUILayout.Width(38))) TakeResult();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            //strength = EditorGUILayout.FloatField("Img2Img Strength", strength);
            // slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Img2Img Strength");
            strength = EditorGUILayout.Slider(strength, 0, 1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // --------------- TOOLS ------------------

            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Import", GUILayout.Height(24))) ImportTexture();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Last seed: " + lastSeed);
            if (GUILayout.Button("Copy last seed")) seed = lastSeed.ToString();
            EditorGUILayout.EndHorizontal();

           
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            // ------------------ RESULTS ------------------
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperLeft;
            var tpos = new Rect(panelWidth + 10, 0, EditorGUIUtility.currentViewWidth - panelWidth - 14, 500);
            if (tex != null) GUI.DrawTexture(tpos, tex, ScaleMode.ScaleToFit);

            EditorGUILayout.EndHorizontal();
        }
    
        //GUI Contents ends........................................................

        

        //GUI Functions...........................................................
        void Generate()
        {
            Debug.Log("Generate..");
            SavePrefs();

            lastImgPath = null;

            string fitStr = fit ? "on" : "off";
            string seamlessStr = seamless ? "'seamless': 'on'," : "";
            int width = options_int[sizeIndex];
            //string sampler_name = samplerOptions[samplerIndex];
            int height = width;
            string initImgObj = "";
            // convert texture2d into base64 jpeg
            if (initimg == null)
            {
                // send null
                Object temp = null;
                initImgObj = SimpleJsonConverter.Serialize(temp);
            }
            else
            {
                // if image is not readable, need to generate copy
                if (initimg.isReadable == false)
                {
                    initimg = DuplicateTexture(initimg);
                }

                byte[] bytes = initimg.EncodeToJPG();
                string base64 = System.Convert.ToBase64String(bytes);
                initImgObj = "\"data:image/jpeg;base64," + base64 + "\"";
            }


            //var initImgObj = SimpleJsonConverter.Serialize(initimg);
            string postdata = $"{{\"prompt\":\"{prompt}\",\"iterations\":\"{iterations}\",\"steps\":\"{steps}\",\"cfg_scale\":\"{cfg_scale}\",\"sampler_name\":\"{samplerName}\",\"width\":\"{width}\",\"height\":\"{height}\",\"seed\":\"{seed}\",\"variation_amount\":\"{variation_amount}\",\"with_variations\":\"{with_variations}\",\"initimg\":{initImgObj},\"strength\":\"{strength}\",\"fit\":\"on\",\"gfpgan_strength\":\"{gfpgan_strength}\",\"upscale_level\":\"{upscale_level}\",\"upscale_strength\":\"{upscale_strength}\",\"initimg_name\":\"{initimg_name}\"}}";
            //postdata.Replace("'", "\"");
            Debug.Log(postdata);


            var request = (HttpWebRequest)WebRequest.Create(url);
            var data = Encoding.ASCII.GetBytes(postdata);
            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            //request.ContentType = "application/json";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }


            EditorApplication.update += EditorUpdate;

            // reset old values
            fullResponse = "";
            progressValue = 0;
            progressMax = steps;


            Debug.Log("Output:"+reader);
            response = (HttpWebResponse)request.GetResponse();
            reader = new StreamReader(response.GetResponseStream());
        }

       
        void ImportTexture()
        {
            if (File.Exists(importFolder) == false)
            {
                Directory.CreateDirectory(importFolder);
            }
            var targetImagePath = Path.Combine(importFolder, Path.GetFileName(lastImgPath));
            File.Copy(lastImgPath, "Assets/Textures/" + Path.GetFileName(lastImgPath), true);
            AssetDatabase.Refresh();
            var lastImageAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(targetImagePath);
            EditorGUIUtility.PingObject(lastImageAsset);
        }
        //GUI Functions ends....................................................................


        void EditorUpdate()
        {
            var responseString = reader.ReadLine();
            if(string.IsNullOrEmpty(responseString)==false)
            {
                fullResponse += responseString+"\n";
                Debug.Log("Progress: " + ((progressValue++) + "/" + steps));
                Repaint();
            }

            if (reader.EndOfStream)
            {
                EditorApplication.update-= EditorUpdate;
                reader.Close();
                response.Close();

                //Deserialising image and disaplaying..............................
                var jsonreader = fullResponse.Split('\n');
                var lastrow = jsonreader[jsonreader.Length - 2];
                var deserialize = SimpleJsonConverter.Deserialize<Root>(lastrow);
                
                lastImgPath = Path.Combine(installationFolder, deserialize.url);

                tex = new Texture2D(2, 2);
                ImageConversion.LoadImage(tex, File.ReadAllBytes(lastImgPath));

            }
        }

        private void TakeResult()
        {
            if (lastImgPath != null)
            {
                initimg = new Texture2D(2, 2);
                initimg.LoadImage(File.ReadAllBytes(lastImgPath));
            }
        }

        void TakeScreenshot()
        {
            var camera = Camera.main;

            // clamp max size to 1024x1024, TODO test if any sizes work, or needs POT or div by 2?
            var w = camera.pixelWidth;
            var h = camera.pixelHeight;
            //w = Mathf.Min(w, 1024);
            //h = Mathf.Min(h, 1024);

            var cameraTexture = new RenderTexture(w, h, 24);
            camera.targetTexture = cameraTexture;
            camera.Render();
            RenderTexture.active = cameraTexture;
            var gameViewTexture = new Texture2D(cameraTexture.width, cameraTexture.height);
            gameViewTexture.ReadPixels(new Rect(0, 0, cameraTexture.width, cameraTexture.height), 0, 0);
            gameViewTexture.Apply();
            initimg = gameViewTexture;
            camera.targetTexture = null;
        }


        static void SavePrefs()
        {
            EditorPrefs.SetString(prefix + "url", url);
            //EditorPrefs.SetString(prefix + "installationFolder", installationFolder);
            //EditorPrefs.SetString(prefix + "importFolder", importFolder);
            
            EditorPrefs.SetInt(prefix + "iterations", iterations);
            EditorPrefs.SetInt(prefix + "steps", steps);
            EditorPrefs.SetFloat(prefix + "cfg_scale", cfg_scale);
            //EditorPrefs.SetString(prefix + "sampler_name", sampler_name);
            //EditorPrefs.SetInt(prefix + "size", size);
            EditorPrefs.SetInt(prefix + "sizeIndexWidth", sizeIndex);
            //EditorPrefs.SetInt(prefix + "sizeIndexHeight", sizeIndexHeight);
            //EditorPrefs.SetInt(prefix + "samplerIndex", samplerIndex);
            //EditorPrefs.SetInt(prefix + "width", width);
            //EditorPrefs.SetInt(prefix + "height", height);
            EditorPrefs.SetString(prefix + "seed", seed);
            EditorPrefs.SetBool(prefix + "seamless", seamless);
            EditorPrefs.SetFloat(prefix + "variation_amount", variation_amount);
            EditorPrefs.SetString(prefix + "with_variations", with_variations);
            EditorPrefs.SetString(prefix + "initimg_name", initimg_name);
            EditorPrefs.SetFloat(prefix + "strength", strength);
            EditorPrefs.SetBool(prefix + "fit", fit);
            EditorPrefs.SetFloat(prefix + "gfpgan_strength", gfpgan_strength);
            EditorPrefs.SetString(prefix + "upscale_level", upscale_level);
            EditorPrefs.SetFloat(prefix + "upscale_strength", upscale_strength);
        }

        static void LoadPrefs()
        {
            
            iterations = EditorPrefs.GetInt(prefix + "iterations", iterations);
            steps = EditorPrefs.GetInt(prefix + "steps", steps);
            seed = EditorPrefs.GetString(prefix + "seed", seed);
            seamless=EditorPrefs.GetBool(prefix+"seamless", seamless);
            sizeIndex=EditorPrefs.GetInt(prefix + "sizeIndexWidth", sizeIndex);
            //sizeIndexHeight=EditorPrefs.GetInt(prefix + "sizeIndexHeight", sizeIndexHeight);
            //height=EditorPrefs.GetInt(prefix+"height", options_int[sizeIndexWidth]);
            //width = EditorPrefs.GetInt(prefix + "width", options_int[sizeIndexHeight]);
            variation_amount = EditorPrefs.GetFloat(prefix + "variation_amount", variation_amount);
            with_variations=EditorPrefs.GetString(prefix + "with_variations", with_variations);
            initimg_name=EditorPrefs.GetString(prefix + "initimg_name", initimg_name);
            strength=EditorPrefs.GetFloat(prefix + "strength", strength);
            fit=EditorPrefs.GetBool(prefix + "fit", fit);
            gfpgan_strength=EditorPrefs.GetFloat(prefix + "gfpgan_strength", gfpgan_strength);
            upscale_level=EditorPrefs.GetString(prefix + "upscale_level", upscale_level);
            upscale_strength=EditorPrefs.GetFloat(prefix + "upscale_strength", upscale_strength);
            cfg_scale=EditorPrefs.GetFloat(prefix + "cfg_scale", cfg_scale);

        }

        Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
    }
    public class Config
    {
        public string prompt { get; set; }
        public string iterations { get; set; }
        public string steps { get; set; }
        public string cfg_scale { get; set; }
        public string sampler_name { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public long seed { get; set; }
        public string variation_amount { get; set; }
        public string with_variations { get; set; }
        public string initimg { get; set; }
        public string strength { get; set; }
        public string fit { get; set; }
        public string gfpgan_strength { get; set; }
        public string upscale_level { get; set; }
        public string upscale_strength { get; set; }
    }

    public class Root
    {
        public string @event { get; set; }
        public string url { get; set; }
        public long seed { get; set; }
        public Config config { get; set; }
    }

    
}
