using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;

namespace AutoPlay {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class AutoPlay : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pseudopulse";
        public const string PluginName = "AutoPlay";
        public const string PluginVersion = "1.0.0";

        public static BepInEx.Logging.ManualLogSource ModLogger;

        public void Awake() {
            // set logger
            ModLogger = Logger;

            Gameplay.AISetup.Initalize();

            InteractableSpawnCard printerW = Utils.Paths.InteractableSpawnCard.iscDuplicator.Load<InteractableSpawnCard>();
            InteractableSpawnCard printerC = Utils.Paths.InteractableSpawnCard.iscDuplicatorLarge.Load<InteractableSpawnCard>();
            InteractableSpawnCard printerR = Utils.Paths.InteractableSpawnCard.iscDuplicatorMilitary.Load<InteractableSpawnCard>();
            InteractableSpawnCard printerY = Utils.Paths.InteractableSpawnCard.iscDuplicatorWild.Load<InteractableSpawnCard>();
            InteractableSpawnCard scrapper = Utils.Paths.InteractableSpawnCard.iscScrapper.Load<InteractableSpawnCard>();

            printerW.maxSpawnsPerStage = 0; printerC.maxSpawnsPerStage = 0; printerR.maxSpawnsPerStage = 0; printerY.maxSpawnsPerStage = 0; scrapper.maxSpawnsPerStage = 0;
        }
    }
}