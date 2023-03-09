using System;
using Rewired;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoPlay.Gameplay {
    public class AISetup {
        public static string InteractableDriver = "ChaseInteractable";
        public static string TeleporterDriver = "ChaseTeleporter";
        public static string HoldoutZoneDriver = "ChargeHoldoutZone";
        public static GameObject PlayerMaster;
        private static string parentID;
        private static string convoID;
        public static void Initalize() {
            OverrideCharacterMaster();
            DisablePlayerInput();
        }

        private static void OverrideCharacterMaster() {
            PlayerMaster = Utils.Paths.GameObject.PlayerMaster.Load<GameObject>();
            BaseAI ai = PlayerMaster.AddComponent<BaseAI>();
            EntityStateMachine machine = PlayerMaster.AddComponent<EntityStateMachine>();
            machine.customName = "AI";
            machine.mainStateType = new(typeof(EntityStates.AI.Walker.Wander));
            machine.initialStateType = new(typeof(EntityStates.AI.Walker.Wander));

            ai.stateMachine = machine;
            ai.fullVision = true;
            ai.scanState = new(typeof(EntityStates.AI.Walker.Wander));
            ai.aimVectorDampTime = 0;
            ai.aimVectorMaxSpeed = 9000;
        }

        private static AISkillDriver SetupSkillDriver(GameObject obj, string name, SkillSlot slot, AISkillDriver.MovementType move, AISkillDriver.AimType aim, AISkillDriver.TargetType target, float max, float min, bool sprint = false, bool noRepeat = false, float minHp = 0f, float maxHp = 100f, bool hold = false, bool reqEquip = false) {
            AISkillDriver driver = obj.AddComponent<AISkillDriver>();
            driver.customName = name;
            driver.skillSlot = slot;
            driver.requireSkillReady = true;
            driver.aimType = aim;
            driver.movementType = move;
            driver.moveTargetType = target;
            driver.minDistance = min;
            driver.maxDistance = max;
            driver.buttonPressType = hold ? AISkillDriver.ButtonPressType.Hold : AISkillDriver.ButtonPressType.TapContinuous;
            driver.shouldSprint = sprint;
            driver.resetCurrentEnemyOnNextDriverSelection = true;
            driver.minUserHealthFraction = minHp;
            driver.maxUserHealthFraction = maxHp;
            driver.noRepeat = noRepeat;
            driver.requireEquipmentReady = reqEquip;
            if (slot != SkillSlot.None) {
                driver.shouldFireEquipment = true;
            }

            return driver;
        }

        private static void DisablePlayerInput() {
            On.RoR2.PlayerCharacterMasterController.CanSendBodyInput += DenyInputs;
            On.RoR2.Stage.Start += NoMorePods;
            On.RoR2.CharacterAI.BaseAI.UpdateBodyAim += PerfectAim;
            On.RoR2.PlayerCharacterMasterController.Update += DenyUpdate;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate += DenyFixedUpdate;
            On.RoR2.SceneDirector.Start += DisableSkyMeadowArtifact;
            On.RoR2.CharacterMaster.Start += SetupAIDrivers;
        }

        private static void SetupAIDrivers(On.RoR2.CharacterMaster.orig_Start orig, CharacterMaster self) {
            orig(self);
            if (!self.GetComponent<PlayerCharacterMasterController>()) {
                return;
            }
            switch (self.bodyPrefab.name) {
                case "ToolbotBody":
                    SetupSkillDriver(self.gameObject, "AttackSecondary", SkillSlot.Secondary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "AttackUtility", SkillSlot.Utility, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.MoveDirection, AISkillDriver.TargetType.CurrentEnemy, 1000, 0);
                    SetupSkillDriver(self.gameObject, "StrafePrimary", SkillSlot.Primary, AISkillDriver.MovementType.StrafeMovetarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 20, 0, false, false, 0, 100, true);
                    SetupSkillDriver(self.gameObject, "ChasePrimary", SkillSlot.Primary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtCurrentEnemy, AISkillDriver.TargetType.CurrentEnemy, 60, 20, false, false, 0, 100, true);
                    SetupSkillDriver(self.gameObject, "FindTarget", SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, InteractableDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, TeleporterDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    break;
                case "CrocoBody":
                    SetupSkillDriver(self.gameObject, "AttackSecondary", SkillSlot.Secondary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "AttackUtility", SkillSlot.Utility, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.MoveDirection, AISkillDriver.TargetType.CurrentEnemy, 1000, 0);
                    SetupSkillDriver(self.gameObject, "AttackSpecial", SkillSlot.Special, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "StrafePrimary", SkillSlot.Primary, AISkillDriver.MovementType.StrafeMovetarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 3, 0);
                    SetupSkillDriver(self.gameObject, "FindTarget", SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, InteractableDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, TeleporterDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    break;
                case "MercBody":
                    SetupSkillDriver(self.gameObject, "AttackSecondary", SkillSlot.Secondary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "AttackUtility", SkillSlot.Utility, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.MoveDirection, AISkillDriver.TargetType.CurrentEnemy, 1000, 0);
                    SetupSkillDriver(self.gameObject, "AttackSpecial", SkillSlot.Special, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "StrafePrimary", SkillSlot.Primary, AISkillDriver.MovementType.StrafeMovetarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 3, 0);
                    SetupSkillDriver(self.gameObject, "FindTarget", SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, InteractableDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, TeleporterDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    break;
                case "LoaderBody":
                    SetupSkillDriver(self.gameObject, "AttackSecondary", SkillSlot.Secondary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0, false, false, 0, 100, true);
                    SetupSkillDriver(self.gameObject, "AttackUtility", SkillSlot.Utility, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 10, 0);
                    SetupSkillDriver(self.gameObject, "AttackSpecial", SkillSlot.Special, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "StrafePrimary", SkillSlot.Primary, AISkillDriver.MovementType.StrafeMovetarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 3, 0);
                    SetupSkillDriver(self.gameObject, "FindTarget", SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, InteractableDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, TeleporterDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    break;
                default:
                    SetupSkillDriver(self.gameObject, "AttackSecondary", SkillSlot.Secondary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "AttackUtility", SkillSlot.Utility, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.MoveDirection, AISkillDriver.TargetType.CurrentEnemy, 1000, 0);
                    SetupSkillDriver(self.gameObject, "AttackSpecial", SkillSlot.Special, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 50, 0);
                    SetupSkillDriver(self.gameObject, "StrafePrimary", SkillSlot.Primary, AISkillDriver.MovementType.StrafeMovetarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 20, 0);
                    SetupSkillDriver(self.gameObject, "ChasePrimary", SkillSlot.Primary, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtCurrentEnemy, AISkillDriver.TargetType.CurrentEnemy, 60, 20);
                    SetupSkillDriver(self.gameObject, "FindTarget", SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.CurrentEnemy, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, InteractableDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    SetupSkillDriver(self.gameObject, TeleporterDriver, SkillSlot.None, AISkillDriver.MovementType.ChaseMoveTarget, AISkillDriver.AimType.AtMoveTarget, AISkillDriver.TargetType.Custom, 1000, 0, true);
                    break;
            }

            self.GetComponent<BaseAI>().skillDrivers = self.GetComponents<AISkillDriver>();
            self.AddComponent<AIController>();
        }

        private static void DisableSkyMeadowArtifact(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self) {
            orig(self);
            if (SceneManager.GetActiveScene().name == "skymeadow") {
                Transform root = GameObject.Find("HOLDER: Randomization").transform.Find("GROUP: Plateau 13 and Underground");
                GameObject bridges = root.Find("Underground").gameObject;
                GameObject door1 = root.Find("P13").Find("Gate Doors").Find("Gate Doors Front").Find("BbRuinGateDoor1_LOD0").gameObject;
                GameObject door2 = root.Find("P13").Find("Gate Doors").Find("Gate Doors Front").Find("BbRuinGateDoor2_LOD0").gameObject;
                GameObject door3 = root.Find("P13").Find("Gate Doors").Find("Gate Doors Back").Find("BbRuinGateDoor1_LOD0").gameObject;
                GameObject door4 = root.Find("P13").Find("Gate Doors").Find("Gate Doors Back").Find("BbRuinGateDoor2_LOD0").gameObject;

                bridges.SetActive(false);
                door1.SetActive(true);
                door2.SetActive(true);
                door3.SetActive(true);
                door4.SetActive(true);
            }
        }

        private static void DenyUpdate(On.RoR2.PlayerCharacterMasterController.orig_Update orig, PlayerCharacterMasterController self) {
            return;
        }

        private static void DenyFixedUpdate(On.RoR2.PlayerCharacterMasterController.orig_FixedUpdate orig, PlayerCharacterMasterController self) {
            return;
        }

        private static void PerfectAim(On.RoR2.CharacterAI.BaseAI.orig_UpdateBodyAim orig, BaseAI self, float t) {
            if (self.GetComponent<PlayerCharacterMasterController>() && self.bodyInputBank) {
                self.bodyInputBank.aimDirection = self.bodyInputs.desiredAimDirection;
            }
            else {
                orig(self, t);
            }
        }

        private static bool DenyInputs(On.RoR2.PlayerCharacterMasterController.orig_CanSendBodyInput orig, NetworkUser user, out LocalUser local, out Player player, out CameraRigController cam) {
            local = user.localUser;
            player = user.inputPlayer;
            cam = user.cameraRigController;
            return false;
        }

        private static void NoMorePods(On.RoR2.Stage.orig_Start orig, Stage self) {
            self.usePod = false;
            orig(self);
        }

        internal class Response {
            public string answer;
            public string error;
            public string convoID;
            public string parentID;
        }
    }
}