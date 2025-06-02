using OWOGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OWO_Valheim
{
    public class OWOSkin
    {

        private bool suitEnabled = false;
        private string modPath = "BepInEx\\Plugins";
        private Dictionary<String, Sensation> sensationsMap = new Dictionary<String, Sensation>();
        private Dictionary<String, Muscle[]> muscleMap = new Dictionary<String, Muscle[]>();

        public int stringBowIntensity = 40;

        public bool heartBeatIsActive = false;
        public bool teleportIsActive = false;
        public bool rainingIsActive = false;
        public bool stringBowIsActive = false;

        public Dictionary<string, Sensation> SensationsMap { get => sensationsMap; set => sensationsMap = value; }

        public OWOSkin()
        {
            RegisterAllSensationsFiles();
            DefineAllMuscleGroups();
            InitializeOWO();
        }

        public void LOG(String msg)
        {
            Plugin.Log.LogInfo(msg);
        }

        #region Skin Configuration

        private void RegisterAllSensationsFiles()
        {
            string configPath = $"{modPath}\\OWO";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.owo", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    Sensation test = Sensation.Parse(tactFileStr);
                    SensationsMap.Add(prefix, test);
                }
                catch (Exception e) { LOG(e.Message); }
            }
        }
        private void DefineAllMuscleGroups()
        {
            Muscle[] leftArm = { Muscle.Arm_L.WithIntensity(100), Muscle.Pectoral_L.WithIntensity(70), Muscle.Dorsal_L.WithIntensity(50) };
            muscleMap.Add("Left Arm", leftArm);

            Muscle[] rightArm = { Muscle.Arm_R.WithIntensity(100), Muscle.Pectoral_R.WithIntensity(70), Muscle.Dorsal_R.WithIntensity(50) };
            muscleMap.Add("Right Arm", rightArm);

            Muscle[] bothArms = leftArm.Concat(rightArm).ToArray();
            muscleMap.Add("Both Arms", bothArms);
        }
        private async void InitializeOWO()
        {
            LOG("Initializing OWO skin");

            var gameAuth = GameAuth.Create(AllBakedSensations()).WithId("0");

            OWO.Configure(gameAuth);
            string[] myIPs = GetIPsFromFile("OWO_Manual_IP.txt");
            if (myIPs.Length == 0) await OWO.AutoConnect();
            else
            {
                await OWO.Connect(myIPs);
            }

            if (OWO.ConnectionState == OWOGame.ConnectionState.Connected)
            {
                suitEnabled = true;
                LOG("OWO suit connected.");
                Feel("Heart Beat", 0);
            }
            if (!suitEnabled) LOG("OWO is not enabled?!?!");
        }

        public BakedSensation[] AllBakedSensations()
        {
            var result = new List<BakedSensation>();

            foreach (var sensation in SensationsMap.Values)
            {
                if (sensation is BakedSensation baked)
                {
                    LOG("Registered baked sensation: " + baked.name);
                    result.Add(baked);
                }
                else
                {
                    LOG("Sensation not baked? " + sensation);
                    continue;
                }
            }
            return result.ToArray();
        }

        public string[] GetIPsFromFile(string filename)
        {
            List<string> ips = new List<string>();
            string filePath = Directory.GetCurrentDirectory() + $"\\{modPath}\\OWO" + filename;
            if (File.Exists(filePath))
            {
                LOG("Manual IP file found: " + filePath);
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    if (IPAddress.TryParse(line, out _)) ips.Add(line);
                    else LOG("IP not valid? ---" + line + "---");
                }
            }
            return ips.ToArray();
        }

        ~OWOSkin()
        {
            LOG("Destructor called");
            DisconnectOWO();
        }

        public void DisconnectOWO()
        {
            LOG("Disconnecting OWO skin.");
            OWO.Disconnect();
        }
        #endregion

        public void Feel(String key, int Priority = 0, int intensity = 0)
        {
            if (SensationsMap.ContainsKey(key))
            {
                Sensation toSend = SensationsMap[key];

                if (intensity != 0)
                {
                    toSend = toSend.WithMuscles(Muscle.All.WithIntensity(intensity));
                }

                OWO.Send(toSend.WithPriority(Priority));
            }

            else LOG("Feedback not registered: " + key);
        }

        public void FeelWithMuscles(String key, String muscleKey = "Right Arm", int Priority = 0, int intensity = 0)
        {
            LOG($"FEEL WITH MUSCLES: {key} - {muscleKey}");

            if (!muscleMap.ContainsKey(muscleKey))
            {
                LOG("MuscleGroup not registered: " + muscleKey);
                return;
            }

            if (SensationsMap.ContainsKey(key))
            {
                if (intensity != 0)
                {
                    OWO.Send(SensationsMap[key].WithMuscles(muscleMap[muscleKey].WithIntensity(intensity)).WithPriority(Priority));
                }
                else
                {
                    OWO.Send(SensationsMap[key].WithMuscles(muscleMap[muscleKey]).WithPriority(Priority));
                }
            }

            else LOG("Feedback not registered: " + key);
        }


        public void StopAllHapticFeedback()
        {
            StopHeartBeat();
            StopRaining();
            StopStringBow();
            StopTeleporting();
            OWO.Stop();
        }

        public bool CanFeel()
        {
            return suitEnabled;
        }

        #region Loops

        #region HeartBeat

        public void StartHeartBeat()
        {
            if (heartBeatIsActive) return;

            heartBeatIsActive = true;
            HeartBeatFuncAsync();
        }

        public void StopHeartBeat()
        {
            heartBeatIsActive = false;
        }

        public async Task HeartBeatFuncAsync()
        {
            while (heartBeatIsActive)
            {
                Feel("Heart Beat", 0);
                await Task.Delay(1000);
            }
        }

        #endregion

        #region Teleporting

        public void StartTeleporting()
        {
            if (teleportIsActive) return;

            teleportIsActive = true;
            TeleportingFuncAsync();
        }

        public void StopTeleporting()
        {
            teleportIsActive = false;
        }

        public async Task TeleportingFuncAsync()
        {
            while (teleportIsActive)
            {
                Feel("Teleporting", 0);
                await Task.Delay(1000);
            }
        }

        #endregion       
        
        #region Raining

        public void StartRaining()
        {
            if (rainingIsActive) return;

            rainingIsActive = true;
            RainingFuncAsync();
        }

        public void StopRaining()
        {
            rainingIsActive = false;
        }

        public async Task RainingFuncAsync()
        {
            while (rainingIsActive)
            {
                Feel("Raining", 0);
                await Task.Delay(1000);
            }
        }

        #endregion

        #region StringBow
        public void StartStringBow()
        {
            if (stringBowIsActive) return;

            stringBowIsActive = true;
            StringBowFuncAsync();
        }

        public void StopStringBow()
        {
            stringBowIsActive = false;
        }

        public async Task StringBowFuncAsync()
        {
            while (stringBowIsActive)
            {
                Feel("Bow Pull", 0, stringBowIntensity);
                await Task.Delay(250);
            }
        }
        #endregion


        #endregion
    }
}
