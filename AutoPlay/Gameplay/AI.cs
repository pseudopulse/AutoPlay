using System;

namespace AutoPlay.Gameplay {
    public class AIController : MonoBehaviour {
        public BaseAI ai;
        public AISkillDriver teleDriver;
        public AISkillDriver interactDriver;
        public PurchaseInteraction[] chests;
        public CharacterMaster master;
        public PurchaseInteraction target;
        public TeleporterInteraction teleporter;
        public GenericPickupController pickup;
        public float money => master.money;
        public float searchDistance = 25;
        public float maxEnemies = 4;
        public float stopwatch = 0f;
        public float retryDelay = 2f;
        public bool shouldSearchTeleporter = false;
        public int attempts;
        public int maxAttempts = 26;
        public Interactor interactor => master?.GetBody()?.GetComponent<Interactor>() ?? null;
        // debug stuff
        public bool canReachTarget;
        public float desiredJumpVelocity;
        public bool wasObstructed;
        public float frustration;

        public void Start() {
            teleDriver = gameObject.FindDriverByName(AISetup.TeleporterDriver);
            interactDriver = gameObject.FindDriverByName(AISetup.InteractableDriver);
            master = GetComponent<CharacterMaster>();
            ai = GetComponent<BaseAI>();

            Stage.onStageStartGlobal += RegatherChests;
            RegatherChests(null);
        }

        private void RegatherChests(Stage stage) {
            chests = GameObject.FindObjectsOfType<PurchaseInteraction>();
            shouldSearchTeleporter = false;
        }

        private void FixedUpdate() {
            if (!interactor) {
                return;
            }
            stopwatch += Time.fixedDeltaTime;

            if (attempts >= maxAttempts) {
                attempts = 0;
                target = null;
                pickup = null;
                ai.customTarget.gameObject = null;
                ai.EvaluateSkillDrivers();
            }

            if (stopwatch >= retryDelay) {
                stopwatch = 0f;

                pickup = FindItemPickup();

                if (shouldSearchTeleporter) {
                    TryGetTeleporter();
                }
                else {
                    TrySearchInteractables();
                }

                if (IsChasingValid()) {
                    GameObject currentTarget;
                    if (pickup) {
                        ai.customTarget.gameObject = pickup.gameObject;
                        ai.skillDriverEvaluation = new BaseAI.SkillDriverEvaluation {
                            target = ai.customTarget,
                            dominantSkillDriver = interactDriver,
                        };
                        ai.BeginSkillDriver(ai.skillDriverEvaluation);
                        currentTarget = pickup.gameObject;
                    }
                    else if (shouldSearchTeleporter) {
                        ai.customTarget.gameObject = teleporter.gameObject;
                        ai.skillDriverEvaluation = new BaseAI.SkillDriverEvaluation {
                            target = ai.customTarget,
                            dominantSkillDriver = teleDriver,
                        };
                        ai.BeginSkillDriver(ai.skillDriverEvaluation);
                        currentTarget = teleporter.gameObject;
                    }
                    else {
                        ai.customTarget.gameObject = target.gameObject;
                        ai.skillDriverEvaluation = new BaseAI.SkillDriverEvaluation {
                            target = ai.customTarget,
                            dominantSkillDriver = interactDriver,
                        };
                        ai.BeginSkillDriver(ai.skillDriverEvaluation);
                        currentTarget = target.gameObject;
                    }
                    
                    if (Vector3.Distance(interactor.transform.position, currentTarget.transform.position) < 5) {
                        interactor.maxInteractionDistance = 5;
                        interactor.AttemptInteraction(currentTarget);
                        if (currentTarget.GetComponent<TeleporterInteraction>()) {
                            currentTarget.GetComponent<TeleporterInteraction>().OnInteractionBegin(interactor);
                        }

                        if (currentTarget.GetComponent<ChestBehavior>()) {
                            ai.customTarget.gameObject = null;
                        }
                        target = null;
                        pickup = null;
                    }

                }
                else {
                    ai.EvaluateSkillDrivers();
                    ai.customTarget.gameObject = null;
                }

                if (shouldSearchTeleporter && teleporter && Vector3.Distance(teleporter.transform.position, interactor.transform.position) > 50) {
                    ai.customTarget.gameObject = teleporter.gameObject;
                    ai.skillDriverEvaluation = new BaseAI.SkillDriverEvaluation {
                        target = ai.customTarget,
                        dominantSkillDriver = teleDriver,
                    };
                    ai.BeginSkillDriver(ai.skillDriverEvaluation);
                }

                if (Stage.instance && Stage.instance.entryTime.timeSince >= 330) {
                    shouldSearchTeleporter = true;
                }
            }

            if (ai && ai.customTarget.gameObject) {
                ai.customTarget.Update();
                ai.localNavigator.allowWalkOffCliff = true;
                ai.SetGoalPosition(ai.customTarget.gameObject.transform.position);
                ai.localNavigator.targetPosition = ai.customTarget.gameObject.transform.position;
                ai.localNavigator.Update(Time.fixedDeltaTime);
            }

            if (shouldSearchTeleporter && teleporter) {
                ai.customTarget.gameObject = teleporter.gameObject;
            }

            if (shouldSearchTeleporter && !teleporter) {
                teleporter = TeleporterInteraction.instance ?? null;
            } 

            if (interactor) {
                interactor.GetComponent<InputBankTest>().interact.PushState(true);

                if (ai.localNavigator.jumpSpeed > 0f) {
                    interactor.GetComponent<InputBankTest>().jump.PushState(true);
                    ai.localNavigator.jumpSpeed = 0;
                    ai.localNavigator.walkFrustration = 0;
                }
            }

            if (ai) {
                canReachTarget = ai.broadNavigationAgent.output.targetReachable;
                desiredJumpVelocity = ai.localNavigator.jumpSpeed;
                wasObstructed = ai.localNavigator.wasObstructedLastUpdate;
                frustration = ai.localNavigator.walkFrustration;
            }
        }

        private void TrySearchInteractables() {
            if (TeleporterInteraction.instance && TeleporterInteraction.instance.isCharged) {
                shouldSearchTeleporter = true;
                return;
            }
            if (!interactor || target || (TeleporterInteraction.instance && TeleporterInteraction.instance.isCharging)) {
                attempts++;
                return;
            }
            foreach (PurchaseInteraction behavior in chests.OrderBy(x => Random.value)) {
                if (behavior && behavior.available && !behavior.gameObject.name.ToLower().Contains("newt")) {
                    if (behavior.CanBeAffordedByInteractor(interactor)) {
                        target = behavior;
                        attempts = 0;
                    }
                }
            }

        }

        private void TryGetTeleporter() {
            if ((TeleporterInteraction.instance && TeleporterInteraction.instance.isCharging)) {
                return;
            }
            if (TeleporterInteraction.instance) {
                teleporter = TeleporterInteraction.instance;
                pickup = null;
                target = null;
            }
        }

        private bool IsChasingValid() {
            return true;
            if (!target || (shouldSearchTeleporter && !teleporter)) {
                return false;
            }
            int t = 0;
            SphereSearch search = new();
            search.radius = searchDistance;
            search.origin = interactor.transform.position;
            search.RefreshCandidates();
            search.FilterCandidatesByDistinctHurtBoxEntities();
            foreach (HurtBox box in search.GetHurtBoxes()) {
                if (box.teamIndex != TeamIndex.Player && box.gameObject.activeInHierarchy) {
                    t++;
                }
            }

            return t < maxEnemies;
        }

        private GenericPickupController FindItemPickup() {
            if (!interactor) {
                return null;
            }
            GenericPickupController[] pickups = GameObject.FindObjectsOfType<GenericPickupController>();
            foreach (GenericPickupController pickup in pickups.OrderBy(x => Vector3.Distance(interactor.transform.position, x.transform.position))) {
                #pragma warning disable
                ItemDef def = ItemCatalog.GetItemDef(pickup.pickupIndex.itemIndex);
                EquipmentDef edef = EquipmentCatalog.GetEquipmentDef(pickup.pickupIndex.equipmentIndex);
                #pragma warning restore
                if (def) {
                    return pickup;
                }
                if (edef && interactor.GetComponent<EquipmentSlot>() && interactor.GetComponent<EquipmentSlot>().equipmentIndex == EquipmentIndex.None) {
                    return pickup;
                }
            }

            return null;
        }
    }
}