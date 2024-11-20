using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using RoR2.Orbs;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using RoR2.Networking;
using System.Collections.Generic;
using static RoR2.CharacterBody;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using HarmonyLib;
using System.Linq;
using static ProBuilder.MeshOperations.pb_MeshImporter;
using System;
using static System.Collections.Specialized.BitVector32;

namespace RandomPlayerCount
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RandomPlayerCount : BaseUnityPlugin
    {

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "TaranDev";
        public const string PluginName = "RandomPlayerCount";
        public const string PluginVersion = "1.0.0";

        static int stagePlayerCount;
        System.Random rnd;

        private static Harmony instance = null;

        public void Awake()
        {
            Log.Init(Logger);
            configs();
            rnd = new System.Random();

         
        }

        public void OnEnable()
        {
            instance = Harmony.CreateAndPatchAll(typeof(RandomPlayerCount));
            RoR2.Run.onRunStartGlobal += RunStart;
            On.RoR2.SceneDirector.Start += SceneStart;

            On.RoR2.CombatDirector.AttemptSpawnOnTarget += AttemptSpawnOnTarget;

        }

        private bool AttemptSpawnOnTarget(On.RoR2.CombatDirector.orig_AttemptSpawnOnTarget orig, CombatDirector self, Transform spawnTarget, DirectorPlacementRule.PlacementMode placementMode)
        {
            if(spawnTarget != null && spawnTarget.name != null && spawnTarget.name.Contains("Teleporter"))
            {
                self.skipSpawnIfTooCheap = false;
            }
            return orig(self, spawnTarget, placementMode);
        }

        private void RunStart(Run run)
        {
            stagePlayerCount = (int) startingPlayerCount.Value;
        }

        public void OnDisable()
        {
            instance?.UnpatchSelf();
            instance = null;

            RoR2.Run.onRunStartGlobal -= RunStart;
            On.RoR2.SceneDirector.Start -= SceneStart;

            On.RoR2.CombatDirector.AttemptSpawnOnTarget -= AttemptSpawnOnTarget;
        }

        private void SceneStart(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            if (SceneManager.GetActiveScene().name != "title" && Run.instance != null && PlayerCharacterMasterController.instances.Count > 0)
            {
                if (randomPlayerCountEveryStage.Value)
                {
                    stagePlayerCount = rnd.Next((int)minRandomPlayers.Value, (int)maxRandomPlayers.Value + 1);

                } else
                {
                    if(Run.instance.stageClearCount > 0)
                    {
                        if (countHiddenStages.Value || (!countHiddenStages.Value && SceneManager.GetActiveScene().name != "bazaar" && SceneManager.GetActiveScene().name != "mysteryspace" && SceneManager.GetActiveScene().name != "goldshores" && SceneManager.GetActiveScene().name != "arena"))
                        {
                            stagePlayerCount = stagePlayerCount * (int) Math.Floor(exponentialScaling.Value) + (int) linearScaling.Value;
                        }
                    }
                }
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "Player Count: " + stagePlayerCount });

            }
            orig(self);
        }

        [HarmonyPatch(typeof(Run), nameof(Run.participatingPlayerCount), MethodType.Getter)]
        [HarmonyPostfix]
        private static int GetPlayerCount(int participatingPlayerCount) => participatingPlayerCount > 0 ? stagePlayerCount : participatingPlayerCount;

        public static ConfigEntry<float> startingPlayerCount;

        public static ConfigEntry<bool> randomPlayerCountEveryStage;

        public static ConfigEntry<float> minRandomPlayers;

        public static ConfigEntry<float> maxRandomPlayers;

        public static ConfigEntry<float> exponentialScaling;

        public static ConfigEntry<float> linearScaling;

        public static ConfigEntry<bool> countHiddenStages;

        private void configs()
        {
            startingPlayerCount = Config.Bind("General", "Starting Player Count", 1f, "Starting Number of players.\nDefault is 1.");
            ModSettingsManager.AddOption(new StepSliderOption(startingPlayerCount,
                new StepSliderConfig
                {
                    min = 1f,
                    max = 1000f,
                    increment = 1f
                }));

            randomPlayerCountEveryStage = Config.Bind("General", "Randomise player count every stage", true, "If the player count should randomise every stage.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(randomPlayerCountEveryStage));

            minRandomPlayers = Config.Bind("General", "Minimum Random Player Count", 1f, "Minimum number of players that can be randomly set.\nDefault is 1.");
            ModSettingsManager.AddOption(new StepSliderOption(minRandomPlayers,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 1000f,
                    increment = 1f
                }));

            maxRandomPlayers = Config.Bind("General", "Maximum Random Player Count", 50f, "Maximum number of players that can be randomly set.\nDefault is 50.");
            ModSettingsManager.AddOption(new StepSliderOption(maxRandomPlayers,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 1000f,
                    increment = 1f
                }));

            exponentialScaling = Config.Bind("General", "Exponential Player Count Scaling", 2f, "How much to multiply player count by per stage. Set to 1 to keep player count the same. Does nothing if randomise player count per stage is turned on.\nDefault is 2 (2x per stage).");
            ModSettingsManager.AddOption(new StepSliderOption(exponentialScaling,
                new StepSliderConfig
                {
                    min = 1f,
                    max = 1000f,
                    increment = 1f
                }));

            linearScaling = Config.Bind("General", "Linear Player Count Scaling", 0f, "How many players to add to the player count per stage. Set to 0 to keep player count the same. Does nothing if randomise player count per stage is turned on.\nDefault is 0 (+0 per stage).");
            ModSettingsManager.AddOption(new StepSliderOption(linearScaling,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 1000f,
                    increment = 1f
                }));

            countHiddenStages = Config.Bind("General", "Multiply on Hidden Realms", false, "If the player count should multiply on hidden realm stages, such as Bazaar and Gilded Coast.\nDefault is false.");
            ModSettingsManager.AddOption(new CheckBoxOption(countHiddenStages));
        }
    }
}
