using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NeonWhiteQoL
{
    public class CommunityMedals
    {
        public static Sprite emeraldMedal, purpleMedal, emeraldCrystal, purpleCrystal, mikeyEmerald, mikeyAmethyst, mikeyOriginal;
        public static string displaytime = "";

        // to do:
        // FIX SIDEQUEST STAMPS, THEY ARE LITERALLY COPY PASTED . I WAS WORKING ON THEM AT 2 AM .
        // fix bug where if you select sidequest stamps first --> go to main levels, the stamps will not work properly if selecting a level with a default red ace medal

        public static void Initialize()
        {
            emeraldMedal = LoadSprite(Properties.Resources.uiMedal_Emerald);
            purpleMedal = LoadSprite(Properties.Resources.uiMedal_Purple);
            emeraldCrystal = LoadSprite(Properties.Resources.uiCrystal_Emerald);
            purpleCrystal = LoadSprite(Properties.Resources.uiCrystal_Amethyst);
            mikeyEmerald = LoadSprite(Properties.Resources.mikeysealEmerald);
            mikeyAmethyst = LoadSprite(Properties.Resources.mikeysealAmethyst);

            MethodInfo method = typeof(LevelInfo).GetMethod("SetLevel");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(CommunityMedals).GetMethod("PostSetLevel"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MenuButtonLevel).GetMethod("SetLevelData");
            harmonyMethod = new HarmonyMethod(typeof(CommunityMedals).GetMethod("PostSetLevelData"));
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
            if (!NeonLite.CommunityMedals_enable.Value)
            {
                __instance.devTime.color = new Color(0.420f, 0.015f, 0.043f);
                return;
            }

            if (mikeyOriginal == null)
            {
                GameObject mikeyStamp = GameObject.Find("Main Menu/Canvas/Ingame Menu/Menu Holder/Inventory Inspector/Inventory Inspector Holder/Panels/Leaderboards And LevelInfo/Level Panel/Info Holder/Stats/Normal Level Stats/Layout Right/Medal Info/Holder/MikeyStamp/");
                if (mikeyStamp.activeSelf)
                    mikeyOriginal = mikeyStamp.transform.Find("MikeyStampGraphic").gameObject.GetComponent<Image>().sprite;
            }

            GameData gameData = Singleton<Game>.Instance.GetGameData();
            LevelStats levelStats = gameData.GetLevelStats(level.levelID);

            if (!levelStats.GetCompleted()) return;

            //if (level.isSidequest)
            //{
            //    __instance._levelMedal.sprite = gameData.GetSpriteForMedal(levelStats.GetMedalAchieved());
            //    __instance._levelMedal.gameObject.SetActive(true);
            //}


            var communityTimes = CommunityMedalTimes[level.levelID];

            string AmethystMedalTime = Game.GetTimerFormattedMillisecond(communityTimes.Item2);
            string EmeraldMedalTime = Game.GetTimerFormattedMillisecond(communityTimes.Item1);
            //Utils.LongToTime(communityTimes.Item1, "#0:00.000");


            if (levelStats._timeBestMicroseconds < communityTimes.Item2)
            {
                __instance._levelMedal.sprite = purpleMedal;
                __instance.devTime.SetText(AmethystMedalTime);
                __instance.devTime.color = new Color(0.674f, 0.313f, 0.913f);

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
            else if (levelStats._timeBestMicroseconds < communityTimes.Item1)
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
        //        amethyst medal time (0.674f, 0.313f, 0.913f);
        //        emerald medal time (99,204,99), (0.388f, 0.8f, 0.388f)
        //        original medal color (107,4,11) (0.420f, 0.015f, 0.043f)

        public static void PostSetLevelData(MenuButtonLevel __instance, ref LevelData ld, ref int displayIndex)
        {
            if (!NeonLite.CommunityMedals_enable.Value)
                return;

            GameData GameDataRef = Singleton<Game>.Instance.GetGameData();

            LevelStats levelStats = GameDataRef.GetLevelStats(ld.levelID);
            var communityTimes = CommunityMedalTimes[ld.levelID];

            //if (ld.isSidequest)
            //{
            //    __instance._medalHolder.SetActive(true);
            //    __instance._medalBG.sprite = __instance._iconBGFull;
            //}

            if (levelStats._timeBestMicroseconds < communityTimes.Item2)
            {
                __instance._medal.sprite = purpleMedal;
                if (ld.isSidequest)
                {
                    __instance._imageLoreFilled.sprite = purpleCrystal;
                }
            }

            else if (levelStats._timeBestMicroseconds < communityTimes.Item1)
            {
                __instance._medal.sprite = emeraldMedal;
                if (ld.isSidequest)
                {
                    __instance._imageLoreFilled.sprite = emeraldCrystal;
                }
            }
        }


        private static readonly Dictionary<string, (long, long)> CommunityMedalTimes = new()
        {
            ["TUT_MOVEMENT"] = (18209999L, 17840999L), // Movement, 18.209 , 17.840
            ["TUT_SHOOTINGRANGE"] = (7249999L, 6999999L), // Pummel, 7.289 , 6.999
            ["SLUGGER"] = (7513650L, 7026999L), // Gunner, 7.513 , 7.026 
            ["TUT_FROG"] = (9759999L, 9439999L), // Cascade, 9.759 , 9.439
            ["TUT_JUMP"] = (15384180L, 14999999L), // Elevate, 15.384 ,  14.999
            ["GRID_TUT_BALLOON"] = (16233228L, 15705999L), // Bounce, 16.233 , 15.705
            ["TUT_BOMB2"] = (8759372L, 8517999L), // Purify, 8.759 , 8.517
            ["TUT_BOMBJUMP"] = (10950999L, 10630999L), // Climb, 10.950 , 10.630
            ["TUT_FASTTRACK"] = (21766666L, 20999999L), // Fasttrack, 21.766 , 20.999
            ["GRID_PORT"] = (22200999L, 19999999L), // Glass Port, 22.200 , 19.999 
            ["GRID_PAGODA"] = (15789999L, 15383999L), // Take Flight, 15.789 , 15.383
            ["TUT_RIFLE"] = (5968789L, 5950999L), // Godspeed 5.968 , 5.950
            ["TUT_RIFLEJOCK"] = (9567365L, 9268999L), // Dasher, 9.567 , 9.268
            ["TUT_DASHENEMY"] = (11842663L, 11512999L), // Thrasher, 11.842 , 11.512
            ["GRID_JUMPDASH"] = (9931733L, 9599999L), // Outstretched, 9.931 , 9.599
            ["GRID_SMACKDOWN"] = (9975487L, 9789999L), // Smackdown 9.975 , 9.789
            ["GRID_MEATY_BALLOONS"] = (13800655L, 13725999L), // Catwalk, 13.800 , 13.725
            ["GRID_FAST_BALLOON"] = (21949799L, 21742999L), // Fastlane 21.949 , 21.742
            ["GRID_DRAGON2"] = (15320626L, 15133999L), // Distinguish, 15.320 , 15.133
            ["GRID_DASHDANCE"] = (16192999L, 15999999L), // Dancer, 16.192 , 15.999
            ["TUT_GUARDIAN"] = (20529077L, 19974999L), // Guardian, 20.529 , 19.974
            ["TUT_UZI"] = (14694806L, 13699999L), // Stomp, 14.694 , 13.699
            ["TUT_JUMPER"] = (13599682L, 12999999L), // Jumper, 13.599 , 12.999
            ["TUT_BOMB"] = (12912954L, 12299999L), // Dash Tower, 12.912 , 12.299
            ["GRID_DESCEND"] = (9999671L, 9579999L), // Descent, 9.999 , 9.579
            ["GRID_STAMPEROUT"] = (11199381L, 10989999L), // Driller, 11.199, 10.989
            ["GRID_CRUISE"] = (16972199L, 14975999L), // Canals, 16.972, 14.975
            ["GRID_SPRINT"] = (16539524L, 15978499L), // Sprint, 16.539, 15.978
            ["GRID_MOUNTAIN"] = (17751583L, 16078999L), // Mountain, 17.751, 16.078
            ["GRID_SUPERKINETIC"] = (16299711L, 15064999L), // Superkinetic 16.299, 15.064
            ["GRID_ARRIVAL"] = (21579773L, 20999999L), // Arrival, 21.579, 20.999
            ["FLOATING"] = (27918134L, 24650999L), // Floating City (Forgotten City), 27.918, 24.650
            ["GRID_BOSS_YELLOW"] = (36569550L, 34199999L), // The Clocktower, 36.569, 34.199
            ["GRID_HOPHOP"] = (16787970L, 16544999L), // Expel (Fireball), 16.787, 16.544
            ["GRID_RINGER_TUTORIAL"] = (13999808L, 12859989L), // Ringer, 13.999, 12.859
            ["GRID_RINGER_EXPLORATION"] = (12999938L, 11648999L), // Cleaner, 12.999, 11.648
            ["GRID_HOPSCOTCH"] = (11753942L, 11179785L), // Warehouse, 11.753, 11.179
            ["GRID_BOOM"] = (16341946L, 15599941L), // Boom, 16.341 , 15.599
            ["GRID_SNAKE_IN_MY_BOOT"] = (7354999L, 6984999L), // Streets 7.354, 6.984
            ["GRID_FLOCK"] = (12464666L, 12140666L), // Steps, 12.464, 12.140
            ["GRID_BOMBS_AHOY"] = (7094951L, 6720999L), // Demolition, 7.094, 6.720
            ["GRID_ARCS"] = (17292908L, 16699999L), // Arcs, 17.292, 16.699
            ["GRID_APARTMENT"] = (15382634L, 12879999L), // Apartment, 15.382, 12.879
            ["TUT_TRIPWIRE"] = (23009999L, 21821999L), // Hanging Gardens, 23.009, 21.821 (from here on out it gets harder)
            ["GRID_TANGLED"] = (13458910L, 12520999L), // Tangled, 13.458, 12.520
            ["GRID_HUNT"] = (20275598L, 19799999L), // Waterworks, 20.275, 19.799
            ["GRID_CANNONS"] = (25262354L, 24227999L), // Killswitch, 25.262, 24.227
            ["GRID_FALLING"] = (20452457L, 19464999L), // Falling, 20.452, 19.464
            ["TUT_SHOCKER2"] = (28277797L, 27289999L), // Shocker, 28.277, 27.289
            ["TUT_SHOCKER"] = (22471999L, 20999897L), // Bouquet, 22.471, 20.999
            ["GRID_PREPARE"] = (27522685L, 25487999L), // Prepare, 27.522, 25.487
            ["GRID_TRIPMAZE"] = (32816847L, 29999999L), // Triptrack, 32.816, 29.999
            ["GRID_RACE"] = (23074515L, 22247999L), // Race 23.074, 22.247
            ["TUT_FORCEFIELD2"] = (16165925L, 14420825L), // Bubble 16.165, 14.420
            ["GRID_SHIELD"] = (16771907L, 15748999L), // Shield 16.771, 15.748
            ["SA L VAGE2"] = (13340509L, 12681509L), // Overlook 13.340, 12.681
            ["GRID_VERTICAL"] = (24673282L, 23949999L), // Pop, 24.673, 23.949
            ["GRID_MINEFIELD"] = (12999097L, 12280999L), // Minefield, 12.999,  12.280
            ["TUT_MIMIC"] = (9735374L, 9325247L), // Mimic, 9.735, 9.325
            ["GRID_MIMICPOP"] = (20076717L, 19378999L), // Trigger, 20.076, 19.378
            ["GRID_SWARM"] = (7676856L, 7499999L), // Greenhouse, 7.676, 7.499
            ["GRID_SWITCH"] = (17922400L, 16735999L), // Sweep 17.922, 16.735
            ["GRID_TRAPS2"] = (25211403L, 22999999L), // Fuse, 25.221, 22.999
            ["TUT_ROCKETJUMP"] = (12238460L, 11449999L), // Heaven's Edge, 12.238, 11.449
            ["TUT_ZIPLINE"] = (11896723L, 11564999L), // Zipline, 11.896, 11.564
            ["GRID_CLIMBANG"] = (16681839L, 15315999L), // Swing 16.681, 15.315
            ["GRID_ROCKETUZI"] = (39872898L, 37799999L), // Chute, 39.872, 37.799
            ["GRID_CRASHLAND"] = (28464362L, 26679999L), // Crash, 28.464, 26.679
            ["GRID_ESCALATE"] = (25564361L, 22699999L), // Ascent, 25.564, 22.699
            ["GRID_SPIDERCLAUS"] = (39555414L, 37999999L), // Straightaway, 39.555, 37.999
            ["GRID_FIRECRACKER_2"] = (34527753L, 30999999L), // Firecracker, 34.527, 30.999 
            ["GRID_SPIDERMAN"] = (25074568L, 21678999L), // Streak, 25.074, 21.678
            ["GRID_DESTRUCTION"] = (27966018L, 24999999L), // Mirror, 27.966, 24.999
            ["GRID_HEAT"] = (25890879L, 23999999L), // Escalation, 25.890, 23.999
            ["GRID_BOLT"] = (29229606L, 26599999L), // Bolt, 29.229, 26.599
            ["GRID_PON"] = (26999747L, 25999999L), // Godstreak, 26.999, 25.999
            ["GRID_CHARGE"] = (31121917L, 29759999L), // Plunge, 31.121, 29.759
            ["GRID_MIMICFINALE"] = (18188015L, 16599999L), // Mayhem, 18.188, 16.599
            ["GRID_BARRAGE"] = (31844284L, 28298999L), // Barrage, 31.844, 28.298
            ["GRID_1GUN"] = (36722186L, 32875999L), // Estate, 36.722, 32.875
            ["GRID_HECK"] = (22092454L, 20399999L), // Trapwire, 22.092, 20.399
            ["GRID_ANTFARM"] = (33791689L, 31179999L), // Ricochet, 33.791, 31.179
            ["GRID_FORTRESS"] = (29082751L, 24117999L), // Fortress, 29.082, 24.117
            ["GRID_GODTEMPLE_ENTRY"] = (52799936L, 51599999L), // Holy Ground, 52.799, 51.599
            ["GRID_BOSS_GODSDEATHTEMPLE"] = (69785332L, 59999999L), // The Third Temple, 1:09.785, 59.999
            ["GRID_EXTERMINATOR"] = (8359876L, 7294251L), // Spree, 8.359 , 7.294
            ["GRID_FEVER"] = (5999972L, 5299999L), // Breakthrough, 5.999, 5.299
            ["GRID_SKIPSLIDE"] = (9899996L, 9229999L), // Glide 9.899, 9.229
            ["GRID_CLOSER"] = (12333912L, 10888999L), // Closer, 12.333, 10.888
            ["GRID_HIKE"] = (8159915L, 7499999L), // Hike, 8.159, 7.499
            ["GRID_SKIP"] = (12899935L, 11889999L), // Switch, 12.899, 11.889
            ["GRID_CEILING"] = (16457145L, 15299999L), // Access, 16.457, 15.299
            ["GRID_BOOP"] = (24899987L, 23699999L), // Congregation 24.899, 23.699
            ["GRID_TRIPRAP"] = (10699891L, 9799999L), // Sequence, 10.699, 9.799
            ["GRID_ZIPRAP"] = (14899903L, 12999999L), // Marathon, 14.899, 12.999
            ["TUT_ORIGIN"] = (66999999L, 63750999L), // Sacrifice 1:06.999, 1:03.750
            ["GRID_BOSS_RAPTURE"] = (85999999L, 78999999L), // Absolution, 1:25.999, 1:18.999
            ["SIDEQUEST_OBSTACLE_PISTOL"] = (14899888L, 14399999L), // Elevate Obstacle Course 1 (Elevate Traversal I), 14.899, 14.399 (formula used for these times may be slightly altered since i got more friends on neon white now xd)
            ["SIDEQUEST_OBSTACLE_PISTOL_SHOOT"] = (25699684L, 24999999L), // Elevate Obstacle Course 2 (Elevate Traversal II), 25.699, 24.999
            ["SIDEQUEST_OBSTACLE_MACHINEGUN"] = (31912929L, 30787999L), // Purify Obstacle Course 2 (Purify Traversal), 31.912, 30.787
            ["SIDEQUEST_OBSTACLE_RIFLE_2"] = (12874596L, 12569999L), // Godspeed Obstacle Course 1 (Godspeed Traversal), 12.874, 12.569
            ["SIDEQUEST_OBSTACLE_UZI2"] = (39228861L, 38284878L), // Stomp Obstacle Course 1 (Stomp Traversal), 39.228, 28.284
            ["SIDEQUEST_OBSTACLE_SHOTGUN"] = (35470132L, 33999999L), // Expel Obstacle Course 2 (Fireball Traversal), 35.470, 33.999
            ["SIDEQUEST_OBSTACLE_ROCKETLAUNCHER"] = (39699294L, 37548999L), // Rocket Obstacle Course 2 (Dominion Traversal), 39.699, 37.548
            ["SIDEQUEST_RAPTURE_QUEST"] = (1549748L, 1399999L), // Telefrag Challenge (Book of Life Traversal), 1.549, 1.399
            ["SIDEQUEST_SUNSET_FLIP_POWERBOMB"] = (36924825L, 36479899L), // Sunset Flip Powerbomb, 36.924, 36.479 
            ["GRID_BALLOONLAIR"] = (19774182L, 19579999L), // Balloon Climber (Balloon Mountain), 19.774, 19.579
            ["SIDEQUEST_BARREL_CLIMB"] = (36874889L, 35899999L), // Barrel Climb (Climbing Gym), 36.874, 35.899
            ["SIDEQUEST_FISHERMAN_SUPLEX"] = (41596839L, 37989899L), // Fisherman Suplex, 41.596, 37.989
            ["SIDEQUEST_STF"] = (17772359L, 16842999L), // STF, 17.772, 16.842
            ["SIDEQUEST_ARENASIXNINE"] = (23389807L, 20999999L), // Arena, 23.389, 20.999
            ["SIDEQUEST_ATTITUDE_ADJUSTMENT"] = (41512642L, 40247898L), // Attitude Adjustment, 41.512, 40.247
            ["SIDEQUEST_ROCKETGODZ"] = (47999658L, 44259999L), // Rocket, 47.999, 44.259
            ["SIDEQUEST_DODGER"] = (19699937L, 18789999L), // Dodger (Doghouse), 19.699, 18.789
            ["GRID_GLASSPATH"] = (25075795L, 24399999L), // Glass Path 1 (Choker), 24.678, 24.399
            ["GRID_GLASSPATH2"] = (19199999L, 18599999L), // Glass Path 2 (Chain), 19.199, 18.599
            ["GRID_HELLVATOR"] = (21720813L, 21299999L), // Hellvator (Hellevator), 21.720, 21.299
            ["GRID_GLASSPATH3"] = (24534980L, 23937999L), // Glass Path 3 (Razor), 24.534, 23.937
            ["SIDEQUEST_ALL_SEEING_EYE"] = (27899976L, 27355999L), // All Seeing Eye, 27.899, 27.355
            ["SIDEQUEST_RESIDENTSAWB"] = (17299871L, 16699999L), // Resident Saw I, 17.199, 16.699
            ["SIDEQUEST_RESIDENTSAW"] = (16399999L, 16359999L) // Resident Saw II, 16.399, 16.359   
        };
    }
}
