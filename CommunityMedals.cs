using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NeonWhiteQoL
{
    public class CommunityMedals
    {
        public static Sprite platinumMedal, rainbowMedal;
        private static readonly FieldInfo _getLevelData = typeof(MenuButtonLevel).GetField("_levelData", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void Initialize()
        {
            MethodInfo method = typeof(LevelInfo).GetMethod("SetLevel");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(CommunityMedals).GetMethod("PostSetLevel"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MenuButtonLevel).GetMethod("UpdateTime");
            harmonyMethod = new HarmonyMethod(typeof(CommunityMedals).GetMethod("PostUpdateTime"));
            NeonLite.Harmony.Patch(method, null, harmonyMethod);

            platinumMedal = LoadSprite(Properties.Resources.uiMedal_Platinum);
            rainbowMedal = LoadSprite(Properties.Resources.uiMedal_Rainbow);
        }

        private static Sprite LoadSprite(byte[] image)
        {
            Texture2D SpriteTexture = new(2, 2);
            SpriteTexture.LoadImage(image);

            return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), 100f);
        }

        public static void PostSetLevel(LevelInfo __instance, ref LevelData level, ref bool fromStore, ref bool isNewScore, ref bool skipNewScoreInitalDelay)
        {
            GameData gameData = Singleton<Game>.Instance.GetGameData();
            LevelStats levelStats = gameData.GetLevelStats(level.levelID);

            if (!levelStats.GetCompleted() || level.isSidequest) return;

            //__instance._levelMedal.sprite = gameData.GetSpriteForMedal(levelStats.GetMedalAchieved());
            var communityTimes = CommunityMedalTimes[level.levelID];

            if (levelStats._timeBestMicroseconds < communityTimes.Item1)
                __instance._levelMedal.sprite = platinumMedal;
            else if (levelStats._timeBestMicroseconds < communityTimes.Item2)
                __instance._levelMedal.sprite = rainbowMedal;
        }

        public static void PostUpdateTime(MenuButtonLevel __instance)
        {
            GameData GameDataRef = Singleton<Game>.Instance.GetGameData();
            LevelData _levelData = _getLevelData.GetValue(__instance) as LevelData;

            LevelStats levelStats = GameDataRef.GetLevelStats(_levelData.levelID);
            var communityTimes = CommunityMedalTimes[_levelData.levelID];

            if (levelStats._timeBestMicroseconds < communityTimes.Item1)
                __instance._medal.sprite = platinumMedal;
            else if (levelStats._timeBestMicroseconds < communityTimes.Item2)
                __instance._medal.sprite = rainbowMedal;
        }


        private static readonly Dictionary<string, (long, long)> CommunityMedalTimes = new()
        {
            ["TUT_MOVEMENT"] = (182099990L, 10L), // Movement, 18.209 
            ["TUT_SHOOTINGRANGE"] = (7289999L, 10L), // Pummel, 7.289 
            ["SLUGGER"] = (7546650L, 10L), // Gunner, 7.546 
            ["TUT_FROG"] = (9759999L, 10L), // Cascade, 9.759 
            ["TUT_JUMP"] = (15334180L, 10L), // Elevate
            ["GRID_TUT_BALLOON"] = (16151228L, 10L), // Bounce
            ["TUT_BOMB2"] = (9149372L, 10L), // Purify
            ["TUT_BOMBJUMP"] = (11789203L, 10L), // Climb, 11.789
            ["TUT_FASTTRACK"] = (21503666L, 10L), // Fasttrack
            ["GRID_PORT"] = (22909394L, 10L), // Glass Port, 22.909
            ["GRID_PAGODA"] = (15505762L, 10L), // Take Flight
            ["TUT_RIFLE"] = (5958456L, 10L), // Godspeed
            ["TUT_RIFLEJOCK"] = (9757365L, 10L), // Dasher
            ["TUT_DASHENEMY"] = (12034663L, 10L), // Thrasher
            ["GRID_JUMPDASH"] = (10031733L, 10L), // Outstretched
            ["GRID_SMACKDOWN"] = (9975487L, 10L), // Smackdown
            ["GRID_MEATY_BALLOONS"] = (13980655L, 10L), // Catwalk
            ["GRID_FAST_BALLOON"] = (22220199L, 10L), // Fastlane
            ["GRID_DRAGON2"] = (15509626L, 10L), // Distinguish
            ["GRID_DASHDANCE"] = (16678540L, 10L), // Dancer
            ["TUT_GUARDIAN"] = (20729077L, 10L), // Guardian
            ["TUT_UZI"] = (14991806L, 10L), // Stomp
            ["TUT_JUMPER"] = (13332682L, 10L), // Jumper
            ["TUT_BOMB"] = (12912954L, 10L), // Dash Tower
            ["GRID_DESCEND"] = (10335671L, 10L), // Descent
            ["GRID_STAMPEROUT"] = (11832381L, 10L), // Driller
            ["GRID_CRUISE"] = (16972199L, 10L), // Canals
            ["GRID_SPRINT"] = (16539524L, 10L), // Sprint
            ["GRID_MOUNTAIN"] = (17751583L, 10L), // Mountain
            ["GRID_SUPERKINETIC"] = (18940711L, 10L), // Superkinetic
            ["GRID_ARRIVAL"] = (21670773L, 10L), // Arrival
            ["FLOATING"] = (29918134L, 10L), // Floating City
            ["GRID_BOSS_YELLOW"] = (35869550L, 10L), // The Clocktower
            ["GRID_HOPHOP"] = (17307570L, 10L), // Expel
            ["GRID_RINGER_TUTORIAL"] = (13477808L, 10L), // Ringer
            ["GRID_RINGER_EXPLORATION"] = (12345938L, 10L), // Cleaner
            ["GRID_HOPSCOTCH"] = (12215342L, 10L), // Warehouse
            ["GRID_BOOM"] = (16667146L, 10L), // Boom
            ["GRID_SNAKE_IN_MY_BOOT"] = (7433524L, 10L), // Streets
            ["GRID_FLOCK"] = (12049666L, 10L), // Steps
            ["GRID_BOMBS_AHOY"] = (7455451L, 10L), // Demolition
            ["GRID_ARCS"] = (17538608L, 10L), // Arcs
            ["GRID_APARTMENT"] = (15282634L, 10L), // Apartment
            ["TUT_TRIPWIRE"] = (23794515L, 10L), // Hanging Gardens
            ["GRID_TANGLED"] = (13849910L, 10L), // Tangled
            ["GRID_HUNT"] = (21075598L, 10L), // Waterworks
            ["GRID_CANNONS"] = (25262354L, 10L), // Killswitch
            ["GRID_FALLING"] = (20352457L, 10L), // Falling
            ["TUT_SHOCKER2"] = (28314797L, 10L), // 
            ["TUT_SHOCKER"] = (22993897L, 10L), // Shocker
            ["GRID_PREPARE"] = (29542685L, 10L), // Prepare
            ["GRID_TRIPMAZE"] = (33386847L, 10L), // Triptrack
            ["GRID_RACE"] = (24454515L, 10L), // Race
            ["TUT_FORCEFIELD2"] = (15450825L, 10L), // 
            ["GRID_SHIELD"] = (17571507L, 10L), // 
            ["SA L VAGE2"] = (13101509L, 10L), // Salvage
            ["GRID_VERTICAL"] = (24673282L, 10L), // 
            ["GRID_MINEFIELD"] = (13999097L, 10L), // 
            ["TUT_MIMIC"] = (9735374L, 10L), // Mimic
            ["GRID_MIMICPOP"] = (20076717L, 10L), // 
            ["GRID_SWARM"] = (8526356L, 10L), // Locker
            ["GRID_SWITCH"] = (19402400L, 10L), // 
            ["GRID_TRAPS2"] = (25211403L, 10L), // 
            ["TUT_ROCKETJUMP"] = (12738460L, 10L), // 
            ["TUT_ZIPLINE"] = (12406723L, 10L), // Zipline
            ["GRID_CLIMBANG"] = (18301839L, 10L), // Swing
            ["GRID_ROCKETUZI"] = (40260798L, 10L), // Bounce
            ["GRID_CRASHLAND"] = (29967362L, 10L), // Crash
            ["GRID_ESCALATE"] = (25564361L, 10L), // Escalate
            ["GRID_SPIDERCLAUS"] = (41411414L, 10L), // Straightaway
            ["GRID_FIRECRACKER_2"] = (33825553L, 10L), // Firecracker
            ["GRID_SPIDERMAN"] = (25334568L, 10L), // Streak
            ["GRID_DESTRUCTION"] = (29866018L, 10L), // DESTRUCTION
            ["GRID_HEAT"] = (26454879L, 10L), // Escalation
            ["GRID_BOLT"] = (29229606L, 10L), // SMOTHER
            ["GRID_PON"] = (28318747L, 10L), // GODSTREAK
            ["GRID_CHARGE"] = (32276517L, 10L), // 
            ["GRID_MIMICFINALE"] = (18188015L, 10L), // 
            ["GRID_BARRAGE"] = (31944284L, 10L), // 
            ["GRID_1GUN"] = (35522186L, 10L), // CLEANSE
            ["GRID_HECK"] = (23262454L, 10L), // Trick
            ["GRID_ANTFARM"] = (33691689L, 10L), // Pinball
            ["GRID_FORTRESS"] = (29082751L, 10L), // FORTRESS
            ["GRID_GODTEMPLE_ENTRY"] = (53154236L, 10L), // 
            ["GRID_BOSS_GODSDEATHTEMPLE"] = (67185332L, 10L), // 
            ["GRID_EXTERMINATOR"] = (8534697L, 10L), // 
            ["GRID_FEVER"] = (5710572L, 10L), // 
            ["GRID_SKIPSLIDE"] = (9886996L, 10L), // 
            ["GRID_CLOSER"] = (12283612L, 10L), // 
            ["GRID_HIKE"] = (8392915L, 10L), // 
            ["GRID_SKIP"] = (12777535L, 10L), // 
            ["GRID_CEILING"] = (16457145L, 10L), // 
            ["GRID_BOOP"] = (25078087L, 10L), // 
            ["GRID_TRIPRAP"] = (11291891L, 10L), // 
            ["GRID_ZIPRAP"] = (14371503L, 10L), // 
            ["TUT_ORIGIN"] = (66285453L, 10L), // 
            ["GRID_BOSS_RAPTURE"] = (85179804L, 10L),
            ["SIDEQUEST_OBSTACLE_PISTOL"] = (15022354L, 10L), // Elevate Obstacle Course 1
            ["SIDEQUEST_OBSTACLE_PISTOL_SHOOT"] = (26145684L, 10L), // Elevate Obstacle Course 2
            ["SIDEQUEST_OBSTACLE_MACHINEGUN"] = (31912929L, 10L), // Purify Obstacle Course 2
            ["SIDEQUEST_OBSTACLE_RIFLE_2"] = (13720596L, 10L), // Godspeed Obstacle Course 1
            ["SIDEQUEST_OBSTACLE_UZI2"] = (39198861L, 10L), // Stomp Obstacle Course 1
            ["SIDEQUEST_OBSTACLE_SHOTGUN"] = (36360132L, 10L), // Expel Obstacle Course 2
            ["SIDEQUEST_OBSTACLE_ROCKETLAUNCHER"] = (44690294L, 10L), // Rocket Obstacle Course 2
            ["SIDEQUEST_RAPTURE_QUEST"] = (1399748L, 10L), // Telefrag Challenge
            ["SIDEQUEST_SUNSET_FLIP_POWERBOMB"] = (37824825L, 10L), // 
            ["GRID_BALLOONLAIR"] = (19774182L, 10L), // Balloon Climber
            ["SIDEQUEST_BARREL_CLIMB"] = (37504589L, 10L), // Barrel Climb
            ["SIDEQUEST_FISHERMAN_SUPLEX"] = (43806639L, 10L), // 
            ["SIDEQUEST_STF"] = (18672359L, 10L), // 
            ["SIDEQUEST_ARENASIXNINE"] = (25047807L, 10L), // 
            ["SIDEQUEST_ATTITUDE_ADJUSTMENT"] = (41812642L, 10L), // 
            ["SIDEQUEST_ROCKETGODZ"] = (47798658L, 10L), // 
            ["SIDEQUEST_DODGER"] = (19930937L, 10L), // Dodger
            ["GRID_GLASSPATH"] = (25075795L, 10L), // Glass Path 1
            ["GRID_GLASSPATH2"] = (19368072L, 10L), // Glass Path 2
            ["GRID_HELLVATOR"] = (21305813L, 10L), // Hellvator
            ["GRID_GLASSPATH3"] = (24712880L, 10L), // Glass Path 3
            ["SIDEQUEST_ALL_SEEING_EYE"] = (28070976L, 10L), // All Seeing Eye
            ["SIDEQUEST_RESIDENTSAWB"] = (17299871L, 10L), // 
            ["SIDEQUEST_RESIDENTSAW"] = (16410187L, 10L) //
        };
    }
}
