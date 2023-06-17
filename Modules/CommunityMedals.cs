using HarmonyLib;
using Steamworks;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeonWhiteQoL.Modules
{
    public class CommunityMedals : MonoBehaviour
    {
        public static Sprite emeraldMedal, purpleMedal, emeraldCrystal, purpleCrystal, mikeyEmerald, mikeyAmethyst, mikeyOriginal, topazMedal, topazCrystal, mikeyTopaz;
        public static string displaytime = "";
        private static Dictionary<string, long[]> CommunityMedalTimes;

        // to do:
        // FIX SIDEQUEST STAMPS, THEY ARE LITERALLY COPY PASTED . I WAS WORKING ON THEM AT 2 AM .
        // fix bug where if you select sidequest stamps first --> go to main levels, the stamps will not work properly if selecting a level with a default red ace medal

        public static void Initialize()
        {
            if (!SteamManager.Initialized)
            {
                return;
            }

            string path = Application.persistentDataPath + "\\" + SteamUser.GetSteamID().m_SteamID.ToString() + "\\communitymedals.json ";
            Stream stream = File.Open(path, FileMode.Create);


            DataContractJsonSerializerSettings Settings = new() { UseSimpleDictionaryFormat = true };
            var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  ");

            DataContractJsonSerializer serializer = new(typeof(Dictionary<string, long[]>), Settings);
            serializer.WriteObject(writer, CommunityMedalTimes);
            writer.Flush();

            stream.Close();

            emeraldMedal = LoadSprite(Properties.Resources.uiMedal_Emerald);
            purpleMedal = LoadSprite(Properties.Resources.uiMedal_Purple);
            topazMedal = LoadSprite(Properties.Resources.uiMedal_Topaz);
            emeraldCrystal = LoadSprite(Properties.Resources.uiCrystal_Emerald);
            purpleCrystal = LoadSprite(Properties.Resources.uiCrystal_Amethyst);
            topazCrystal = LoadSprite(Properties.Resources.uiCrystal_Topaz);
            mikeyEmerald = LoadSprite(Properties.Resources.mikeysealEmerald);
            mikeyAmethyst = LoadSprite(Properties.Resources.mikeysealAmethyst);
            mikeyTopaz = LoadSprite(Properties.Resources.mikeysealTopaz);
            mikeyOriginal = LoadSprite(Properties.Resources.uiMedal_MikeyStamp);

            MethodInfo method = typeof(LevelInfo).GetMethod("SetLevel");
            HarmonyMethod harmonyMethod = new (typeof(CommunityMedals).GetMethod("PostSetLevel"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MenuButtonLevel).GetMethod("SetLevelData");
            harmonyMethod = new (typeof(CommunityMedals).GetMethod("PostSetLevelData"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);
        }

        private static Sprite LoadSprite(byte[] image)
        {
            Texture2D SpriteTexture = new(2, 2);
            SpriteTexture.LoadImage(image);

            return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100f);
        }

        public static void PostSetLevel(LevelInfo __instance, ref LevelData level, ref bool fromStore, ref bool isNewScore, ref bool skipNewScoreInitalDelay)
        {
            if (SceneManager.GetActiveScene().name == "CustomLevel") return;

            if (!NeonLite.CommunityMedals_enable.Value)
            {
                __instance.devTime.color = new Color(0.420f, 0.015f, 0.043f);
                return;
            }

            // Get default mikey stamp
            //if (mikeyOriginal == null)
            //{
            //    GameObject mikeyStamp = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Inventory Inspector/Inventory Inspector Holder/Panels/Leaderboards And LevelInfo/Level Panel/Info Holder/Stats/Normal Level Stats/Layout Right/Medal Info/Holder/MikeyStamp/");
            //    if (mikeyStamp.activeSelf)
            //        mikeyOriginal = mikeyStamp.transform.Find("MikeyStampGraphic").gameObject.GetComponent<Image>().sprite;
            //}

            GameData gameData = Singleton<Game>.Instance.GetGameData();
            LevelStats levelStats = gameData.GetLevelStats(level.levelID);

            if (!levelStats.GetCompleted()) return;

            var communityTimes = CommunityMedalTimes[level.levelID];

            string TopazMedalTime = Game.GetTimerFormattedMillisecond(communityTimes[2]);
            string AmethystMedalTime = Game.GetTimerFormattedMillisecond(communityTimes[1]);
            string EmeraldMedalTime = Game.GetTimerFormattedMillisecond(communityTimes[0]);

            if (levelStats._timeBestMicroseconds < communityTimes[2])
            {
                __instance._levelMedal.sprite = topazMedal;
                __instance.devTime.SetText(TopazMedalTime);
                __instance.devTime.color = new Color(0.976f, 0.341f, 0f);

                if (level.isSidequest)
                {
                    __instance._medalInfoHolder.SetActive(true);
                    __instance.devStamp.SetActive(true);

                    __instance._crystalHolderFilledImage.sprite = topazCrystal;

                    Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = mikeyTopaz;
                    stamps[2].sprite = mikeyTopaz;
                }
                else
                {
                    Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = mikeyTopaz;
                    stamps[2].sprite = mikeyTopaz;
                }
            }

            else if (levelStats._timeBestMicroseconds < communityTimes[1])
            {
                __instance._levelMedal.sprite = purpleMedal;
                __instance.devTime.SetText(TopazMedalTime);
                __instance.devTime.color = new Color(0.976f, 0.341f, 0f);

                if (level.isSidequest)
                {
                    __instance._medalInfoHolder.SetActive(true);
                    __instance.devStamp.SetActive(true);

                    __instance._crystalHolderFilledImage.sprite = purpleCrystal;

                    Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = mikeyAmethyst;
                    stamps[2].sprite = mikeyAmethyst;
                }
                else
                {
                    Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = mikeyAmethyst;
                    stamps[2].sprite = mikeyAmethyst;
                }
            }
            else if (levelStats._timeBestMicroseconds < communityTimes[0])
            {
                __instance._levelMedal.sprite = emeraldMedal;
                __instance.devTime.SetText(AmethystMedalTime);
                __instance.devTime.color = new Color(0.674f, 0.313f, 0.913f);

                if (level.isSidequest)
                {
                    __instance._medalInfoHolder.SetActive(true);
                    __instance.devStamp.SetActive(true);

                    Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = mikeyEmerald;
                    stamps[2].sprite = mikeyEmerald;

                    __instance._crystalHolderFilledImage.sprite = emeraldCrystal;
                }
                else
                {
                    Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                    if (stamps.Length < 3) return;

                    stamps[1].sprite = mikeyEmerald;
                    stamps[2].sprite = mikeyEmerald;
                }
            }
            else if (!level.isSidequest)
            {
                __instance.devTime.SetText(EmeraldMedalTime);
                __instance.devTime.color = new Color(0.388f, 0.8f, 0.388f);

                Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                if (stamps.Length < 3) return;

                stamps[1].sprite = mikeyOriginal;
                stamps[2].sprite = mikeyOriginal;
            }

            else if (level.isSidequest)
            {
                __instance._medalInfoHolder.SetActive(true);
                __instance.devStamp.SetActive(true);
                __instance.devTime.SetText(EmeraldMedalTime);
                __instance.devTime.color = new Color(0.388f, 0.8f, 0.388f);

                Image[] stamps = __instance.devStamp.GetComponentsInChildren<Image>();
                if (stamps.Length < 3) return;

                stamps[1].sprite = mikeyOriginal;
                stamps[2].sprite = mikeyOriginal;
            }
        }

        //        topaz medal time (249, 87, 0), (0.976f, 0.341f, 0f)
        //        amethyst medal time (0.674f, 0.313f, 0.913f);
        //        emerald medal time (99,204,99), (0.388f, 0.8f, 0.388f)
        //        original medal color (107,4,11) (0.420f, 0.015f, 0.043f)

        public static void PostSetLevelData(MenuButtonLevel __instance, ref LevelData ld, ref int displayIndex)
        {
            if (SceneManager.GetActiveScene().name == "CustomLevel") return;

            if (!NeonLite.CommunityMedals_enable.Value)
                return;

            GameData GameDataRef = Singleton<Game>.Instance.GetGameData();

            LevelStats levelStats = GameDataRef.GetLevelStats(ld.levelID);
            var communityTimes = CommunityMedalTimes[ld.levelID];

            if (levelStats._timeBestMicroseconds < communityTimes[2])
            {
                __instance._medal.sprite = topazMedal;
                if (ld.isSidequest)
                {
                    __instance._imageLoreFilled.sprite = topazCrystal;
                }
            }

            else if (levelStats._timeBestMicroseconds < communityTimes[1])
            {
                __instance._medal.sprite = purpleMedal;
                if (ld.isSidequest)
                {
                    __instance._imageLoreFilled.sprite = purpleCrystal;
                }
            }

            else if (levelStats._timeBestMicroseconds < communityTimes[0])
            {
                __instance._medal.sprite = emeraldMedal;
                if (ld.isSidequest)
                {
                    __instance._imageLoreFilled.sprite = emeraldCrystal;
                }
            }
        }

        void Start()
        {
            StartCoroutine(DownloadMedals());
        }

        public IEnumerator DownloadMedals()
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/Faustas156/NeonLiteBanList/main/communitymedals.json");
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    string timestext = webRequest.downloadHandler.text;
                    DataContractJsonSerializerSettings Settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
                    DataContractJsonSerializer deserializer = new(typeof(Dictionary<string, long[]>), Settings);

                    CommunityMedalTimes = (Dictionary<string, long[]>)deserializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(timestext)));
                    Debug.Log(CommunityMedalTimes.Count + " hello");
                    Initialize();
                    break;
            }
        }

        // Levelname -> (emerald medal time, purple medal time)

        //private static readonly Dictionary<string, long[]> CommunityMedalTimes = new()
        //{
        //    ["TUT_MOVEMENT"] = new long[] { 18209999L, 17882999L }, // Movement, 18.209 , 17.882
        //    ["TUT_SHOOTINGRANGE"] = new long[] { 7249999L, 6799999L }, // Pummel, 7.249 , 6.799
        //    ["SLUGGER"] = new long[] { 7513650L, 7026999L }, // Gunner, 7.513 , 7.026 
        //    ["TUT_FROG"] = new long[] { 9474999L, 9139999L }, // Cascade, 9.474 , 9.139
        //    ["TUT_JUMP"] = new long[] { 15384180L, 14825999L }, // Elevate, 15.384 ,  14.825
        //    ["GRID_TUT_BALLOON"] = new long[] { 16233228L, 15705999L }, // Bounce, 16.233 , 15.705
        //    ["TUT_BOMB2"] = new long[] { 8759372L, 8388999L }, // Purify, 8.759 , 8.388
        //    ["TUT_BOMBJUMP"] = new long[] { 10950999L, 10630999L }, // Climb, 10.950 , 10.630
        //    ["TUT_FASTTRACK"] = new long[] { 21766666L, 20999999L }, // Fasttrack, 21.766 , 20.999
        //    ["GRID_PORT"] = new long[] { 22200999L, 19849999L }, // Glass Port, 22.200 , 19.849 
        //    ["GRID_PAGODA"] = new long[] { 15789999L, 15249999L }, // Take Flight, 15.789 , 15.249
        //    ["TUT_RIFLE"] = new long[] { 5968789L, 5950999L }, // Godspeed 5.968 , 5.950
        //    ["TUT_RIFLEJOCK"] = new long[] { 9567365L, 9099999L }, // Dasher, 9.567 , 9.099
        //    ["TUT_DASHENEMY"] = new long[] { 11842663L, 11512999L }, // Thrasher, 11.842 , 11.512
        //    ["GRID_JUMPDASH"] = new long[] { 9931733L, 9499999L }, // Outstretched, 9.931 , 9.499
        //    ["GRID_SMACKDOWN"] = new long[] { 9975487L, 9789999L }, // Smackdown 9.975 , 9.789
        //    ["GRID_MEATY_BALLOONS"] = new long[] { 13800655L, 13648999L }, // Catwalk, 13.800 , 13.648
        //    ["GRID_FAST_BALLOON"] = new long[] { 21949799L, 21629999L }, // Fastlane 21.949 , 21.629
        //    ["GRID_DRAGON2"] = new long[] { 15320626L, 15133999L }, // Distinguish, 15.320 , 15.133
        //    ["GRID_DASHDANCE"] = new long[] { 16192999L, 15728999L }, // Dancer, 16.192 , 15.728
        //    ["TUT_GUARDIAN"] = new long[] { 20529077L, 19678999L }, // Guardian, 20.529 , 19.678
        //    ["TUT_UZI"] = new long[] { 14694806L, 13889999L }, // Stomp, 14.694 , 13.889
        //    ["TUT_JUMPER"] = new long[] { 13599682L, 12555999L }, // Jumper, 13.599 , 12.555
        //    ["TUT_BOMB"] = new long[] { 12912954L, 12299999L }, // Dash Tower, 12.912 , 12.299
        //    ["GRID_DESCEND"] = new long[] { 9999671L, 9499999L }, // Descent, 9.999 , 9.499
        //    ["GRID_STAMPEROUT"] = new long[] { 11199381L, 10879999L }, // Driller, 11.199, 10.879
        //    ["GRID_CRUISE"] = new long[] { 16972199L, 14655999L }, // Canals, 16.972, 14.655
        //    ["GRID_SPRINT"] = new long[] { 16639524L, 15978499L }, // Sprint, 16.639, 15.978
        //    ["GRID_MOUNTAIN"] = new long[] { 17751583L, 15924999L }, // Mountain, 17.751, 15.924
        //    ["GRID_SUPERKINETIC"] = new long[] { 16299711L, 14614999L }, // Superkinetic 16.299, 14.614
        //    ["GRID_ARRIVAL"] = new long[] { 21579773L, 20999999L }, // Arrival, 21.579, 20.999
        //    ["FLOATING"] = new long[] { 27918134L, 24570999L }, // Floating City [Forgotten City], 27.918, 24.570
        //    ["GRID_BOSS_YELLOW"] = new long[] { 36569550L, 33899999L }, // The Clocktower, 36.569, 33.899
        //    ["GRID_HOPHOP"] = new long[] { 16849970L, 16544999L }, // Expel [Fireball], 16.849, 16.544
        //    ["GRID_RINGER_TUTORIAL"] = new long[] { 13999808L, 12859989L }, // Ringer, 13.999, 12.859
        //    ["GRID_RINGER_EXPLORATION"] = new long[] { 12999938L, 11648999L }, // Cleaner, 12.999, 11.648
        //    ["GRID_HOPSCOTCH"] = new long[] { 11753942L, 11179785L }, // Warehouse, 11.753, 11.179
        //    ["GRID_BOOM"] = new long[] { 16341946L, 15599941L }, // Boom, 16.341 , 15.599
        //    ["GRID_SNAKE_IN_MY_BOOT"] = new long[] { 7354999L, 6884999L }, // Streets 7.354, 6.884
        //    ["GRID_FLOCK"] = new long[] { 12464666L, 11999999L }, // Steps, 12.464, 11.999
        //    ["GRID_BOMBS_AHOY"] = new long[] { 7094951L, 6720999L }, // Demolition, 7.094, 6.720
        //    ["GRID_ARCS"] = new long[] { 17292908L, 16399999L }, // Arcs, 17.292, 16.399
        //    ["GRID_APARTMENT"] = new long[] { 15382634L, 12879999L }, // Apartment, 15.382, 12.879
        //    ["TUT_TRIPWIRE"] = new long[] { 23009999L, 22104999L }, // Hanging Gardens, 23.009, 22.104
        //    ["GRID_TANGLED"] = new long[] { 13458910L, 12520999L }, // Tangled, 13.458, 12.520
        //    ["GRID_HUNT"] = new long[] { 20275598L, 19499999L }, // Waterworks, 20.275, 19.499
        //    ["GRID_CANNONS"] = new long[] { 25262354L, 24227999L }, // Killswitch, 25.262, 24.227
        //    ["GRID_FALLING"] = new long[] { 20452457L, 19464999L }, // Falling, 20.452, 19.464
        //    ["TUT_SHOCKER2"] = new long[] { 28277797L, 27289999L }, // Shocker, 28.277, 27.289
        //    ["TUT_SHOCKER"] = new long[] { 22471999L, 20999897L }, // Bouquet, 22.471, 20.999
        //    ["GRID_PREPARE"] = new long[] { 27522685L, 25487999L }, // Prepare, 27.522, 25.487
        //    ["GRID_TRIPMAZE"] = new long[] { 32816847L, 27599999L }, // Triptrack, 32.816, 27.599
        //    ["GRID_RACE"] = new long[] { 23074515L, 22247999L }, // Race 23.074, 22.247
        //    ["TUT_FORCEFIELD2"] = new long[] { 16165925L, 14420825L }, // Bubble 16.165, 14.420
        //    ["GRID_SHIELD"] = new long[] { 16771907L, 15748999L }, // Shield 16.771, 15.748
        //    ["SA L VAGE2"] = new long[] { 13340509L, 12411509L }, // Overlook 13.340, 12.411
        //    ["GRID_VERTICAL"] = new long[] { 24673282L, 23863999L }, // Pop, 24.673, 23.863
        //    ["GRID_MINEFIELD"] = new long[] { 12999097L, 11699999L }, // Minefield, 12.999,  11.699
        //    ["TUT_MIMIC"] = new long[] { 9735374L, 9325247L }, // Mimic, 9.735, 9.325
        //    ["GRID_MIMICPOP"] = new long[] { 20076717L, 18999999L }, // Trigger, 20.076, 18.999
        //    ["GRID_SWARM"] = new long[] { 7696856L, 7499999L }, // Greenhouse, 7.696, 7.499
        //    ["GRID_SWITCH"] = new long[] { 17922400L, 16735999L }, // Sweep 17.922, 16.735
        //    ["GRID_TRAPS2"] = new long[] { 25211403L, 22999999L }, // Fuse, 25.221, 22.999
        //    ["TUT_ROCKETJUMP"] = new long[] { 12238460L, 11449999L }, // Heaven's Edge, 12.238, 11.449
        //    ["TUT_ZIPLINE"] = new long[] { 11896723L, 11564999L }, // Zipline, 11.896, 11.564
        //    ["GRID_CLIMBANG"] = new long[] { 16681839L, 15315999L }, // Swing 16.681, 15.315
        //    ["GRID_ROCKETUZI"] = new long[] { 39872898L, 37599999L }, // Chute, 39.872, 37.599
        //    ["GRID_CRASHLAND"] = new long[] { 28464362L, 26679999L }, // Crash, 28.464, 26.679
        //    ["GRID_ESCALATE"] = new long[] { 25564361L, 22499999L }, // Ascent, 25.564, 22.499
        //    ["GRID_SPIDERCLAUS"] = new long[] { 39555414L, 37999999L }, // Straightaway, 39.555, 37.999
        //    ["GRID_FIRECRACKER_2"] = new long[] { 34527753L, 30563999L }, // Firecracker, 34.527, 30.563
        //    ["GRID_SPIDERMAN"] = new long[] { 25074568L, 21678999L }, // Streak, 25.074, 21.678
        //    ["GRID_DESTRUCTION"] = new long[] { 27966018L, 24999999L }, // Mirror, 27.966, 24.999
        //    ["GRID_HEAT"] = new long[] { 25890879L, 23999999L }, // Escalation, 25.890, 23.999
        //    ["GRID_BOLT"] = new long[] { 29229606L, 25599999L }, // Bolt, 29.229, 25.599
        //    ["GRID_PON"] = new long[] { 26999747L, 25799999L }, // Godstreak, 26.999, 25.799
        //    ["GRID_CHARGE"] = new long[] { 31121917L, 29759999L }, // Plunge, 31.121, 29.759
        //    ["GRID_MIMICFINALE"] = new long[] { 18188015L, 16624999L }, // Mayhem, 18.188, 16.624
        //    ["GRID_BARRAGE"] = new long[] { 31844284L, 28298999L }, // Barrage, 31.844, 28.298
        //    ["GRID_1GUN"] = new long[] { 36722186L, 31999999L }, // Estate, 36.722, 31.999
        //    ["GRID_HECK"] = new long[] { 22092454L, 20399999L }, // Trapwire, 22.092, 20.399
        //    ["GRID_ANTFARM"] = new long[] { 33791689L, 31179999L }, // Ricochet, 33.791, 31.179
        //    ["GRID_FORTRESS"] = new long[] { 29082751L, 24117999L }, // Fortress, 29.082, 24.117
        //    ["GRID_GODTEMPLE_ENTRY"] = new long[] { 52799936L, 51599999L }, // Holy Ground, 52.799, 51.599
        //    ["GRID_BOSS_GODSDEATHTEMPLE"] = new long[] { 69785332L, 59999999L }, // The Third Temple, 1:09.785, 59.999
        //    ["GRID_EXTERMINATOR"] = new long[] { 8359876L, 7499951L }, // Spree, 8.359 , 7.499
        //    ["GRID_FEVER"] = new long[] { 7869972L, 5299999L }, // Breakthrough, 7.869, 5.299
        //    ["GRID_SKIPSLIDE"] = new long[] { 9899996L, 8659999L }, // Glide 9.899, 8.659
        //    ["GRID_CLOSER"] = new long[] { 12333912L, 10888999L }, // Closer, 12.333, 10.888
        //    ["GRID_HIKE"] = new long[] { 8059915L, 7199999L }, // Hike, 8.059, 7.199
        //    ["GRID_SKIP"] = new long[] { 12899935L, 11889999L }, // Switch, 12.899, 11.889
        //    ["GRID_CEILING"] = new long[] { 16457145L, 14755999L }, // Access, 16.457, 14.755
        //    ["GRID_BOOP"] = new long[] { 24899987L, 23599999L }, // Congregation 24.899, 23.599
        //    ["GRID_TRIPRAP"] = new long[] { 10999891L, 9799999L }, // Sequence, 10.999, 9.799
        //    ["GRID_ZIPRAP"] = new long[] { 15299903L, 12999999L }, // Marathon, 15.299, 12.999
        //    ["TUT_ORIGIN"] = new long[] { 66999999L, 62794999L }, // Sacrifice 1:06.999, 1:02.794
        //    ["GRID_BOSS_RAPTURE"] = new long[] { 85999999L, 78999999L }, // Absolution, 1:25.999, 1:18.999
        //    ["SIDEQUEST_OBSTACLE_PISTOL"] = new long[] { 14899888L, 14419999L }, // Elevate Obstacle Course 1 [Elevate Traversal I], 14.899, 14.419
        //    ["SIDEQUEST_OBSTACLE_PISTOL_SHOOT"] = new long[] { 25699684L, 24599999L }, // Elevate Obstacle Course 2 [Elevate Traversal II], 25.699, 24.599
        //    ["SIDEQUEST_OBSTACLE_MACHINEGUN"] = new long[] { 31912929L, 30787999L }, // Purify Obstacle Course 2 [Purify Traversal], 31.912, 30.787
        //    ["SIDEQUEST_OBSTACLE_RIFLE_2"] = new long[] { 13094596L, 12569999L }, // Godspeed Obstacle Course 1 [Godspeed Traversal], 13.094, 12.569
        //    ["SIDEQUEST_OBSTACLE_UZI2"] = new long[] { 39228861L, 38284878L }, // Stomp Obstacle Course 1 [Stomp Traversal], 39.228, 38.284
        //    ["SIDEQUEST_OBSTACLE_SHOTGUN"] = new long[] { 35470132L, 33999999L }, // Expel Obstacle Course 2 [Fireball Traversal], 35.470, 33.999
        //    ["SIDEQUEST_OBSTACLE_ROCKETLAUNCHER"] = new long[] { 40499294L, 37548999L }, // Rocket Obstacle Course 2 [Dominion Traversal], 40.499, 37.548
        //    ["SIDEQUEST_RAPTURE_QUEST"] = new long[] { 1749748L, 1469999L }, // Telefrag Challenge [Book of Life Traversal], 1.749, 1.469
        //    ["SIDEQUEST_SUNSET_FLIP_POWERBOMB"] = new long[] { 36924825L, 36479899L }, // Sunset Flip Powerbomb, 36.924, 36.479 // maybe?
        //    ["GRID_BALLOONLAIR"] = new long[] { 19794182L, 19579999L }, // Balloon Climber [Balloon Mountain], 19.794, 19.579
        //    ["SIDEQUEST_BARREL_CLIMB"] = new long[] { 36874889L, 35959999L }, // Barrel Climb [Climbing Gym], 36.874, 35.959
        //    ["SIDEQUEST_FISHERMAN_SUPLEX"] = new long[] { 41596839L, 37989899L }, // Fisherman Suplex, 41.596, 37.989
        //    ["SIDEQUEST_STF"] = new long[] { 17899359L, 16842999L }, // STF, 17.899, 16.842
        //    ["SIDEQUEST_ARENASIXNINE"] = new long[] { 23389807L, 20999999L }, // Arena, 23.389, 20.999
        //    ["SIDEQUEST_ATTITUDE_ADJUSTMENT"] = new long[] { 41512642L, 40167898L }, // Attitude Adjustment, 41.512, 40.167
        //    ["SIDEQUEST_ROCKETGODZ"] = new long[] { 47999658L, 44259999L }, // Rocket, 47.999, 44.259
        //    ["SIDEQUEST_DODGER"] = new long[] { 19899937L, 18789999L }, // Dodger [Doghouse], 19.899, 18.789
        //    ["GRID_GLASSPATH"] = new long[] { 24678795L, 24399999L }, // Glass Path 1 [Choker], 24.678, 24.399
        //    ["GRID_GLASSPATH2"] = new long[] { 19199999L, 18639999L }, // Glass Path 2 [Chain], 19.199, 18.639
        //    ["GRID_HELLVATOR"] = new long[] { 21827813L, 21299999L }, // Hellvator [Hellevator], 21.827, 21.299
        //    ["GRID_GLASSPATH3"] = new long[] { 24534980L, 23937999L }, // Glass Path 3 [Razor], 24.534, 23.937
        //    ["SIDEQUEST_ALL_SEEING_EYE"] = new long[] { 27899976L, 27355999L }, // All Seeing Eye, 27.899, 27.355
        //    ["SIDEQUEST_RESIDENTSAWB"] = new long[] { 17299871L, 16699999L }, // Resident Saw I, 17.299, 16.699
        //    ["SIDEQUEST_RESIDENTSAW"] = new long[] { 16429999L, 16359999L } // Resident Saw II, 16.429, 16.359
        //};
    }
}
