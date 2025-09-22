using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HussarTransformation
{
    // ====================================================================
    // 1. DEF REFERENCES
    // ====================================================================

    [DefOf]
    public static class XenotypeDefOf
    {
        public static XenotypeDef Hussar;
        public static XenotypeDef Baseliner;

        static XenotypeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(XenotypeDefOf));
        }
    }

    [DefOf]
    public static class HediffDefOf
    {
        public static HediffDef GoJuiceAddiction;

        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }
    }

    // ====================================================================
    // 2. MOD SETTINGS
    // ====================================================================

    public class HussarTransformationSettings : ModSettings
    {
        public bool allowOtherXenotypes = false;
        public int goJuiceCost = 1;
        public int medicineCost = 1;
        public int componentCost = 1;
        public bool consumeMedicineOnFailure = true;
        public int medicineConsumedOnFailure = 1;
        public float transformationDurationHours = 4f;
        public int powerConsumption = 200;
        public bool stopOnPowerLoss = true;
        public int researchCost = 600;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref allowOtherXenotypes, "allowOtherXenotypes", false);
            Scribe_Values.Look(ref goJuiceCost, "goJuiceCost", 1);
            Scribe_Values.Look(ref medicineCost, "medicineCost", 1);
            Scribe_Values.Look(ref componentCost, "componentCost", 1);
            Scribe_Values.Look(ref consumeMedicineOnFailure, "consumeMedicineOnFailure", true);
            Scribe_Values.Look(ref medicineConsumedOnFailure, "medicineConsumedOnFailure", 1);
            Scribe_Values.Look(ref transformationDurationHours, "transformationDurationHours", 4f);
            Scribe_Values.Look(ref powerConsumption, "powerConsumption", 200);
            Scribe_Values.Look(ref stopOnPowerLoss, "stopOnPowerLoss", true);
            Scribe_Values.Look(ref researchCost, "researchCost", 600);
            base.ExposeData();
        }

        public void ClampValues()
        {
            if (medicineConsumedOnFailure > medicineCost)
            {
                medicineConsumedOnFailure = medicineCost;
            }
            goJuiceCost = Mathf.Clamp(goJuiceCost, 0, 99);
            medicineCost = Mathf.Clamp(medicineCost, 0, 99);
            componentCost = Mathf.Clamp(componentCost, 0, 99);
            medicineConsumedOnFailure = Mathf.Clamp(medicineConsumedOnFailure, 0, medicineCost);
            transformationDurationHours = Mathf.Clamp(transformationDurationHours, 0f, 24f);
            powerConsumption = Mathf.Clamp(powerConsumption, 0, 1000);
            researchCost = Mathf.Clamp(researchCost, 0, 9999);
        }
    }

    // ====================================================================
    // 3. MOD CLASS
    // ====================================================================

    public class HussarTransformationMod : Mod
    {
        public static HussarTransformationSettings settings;

        public HussarTransformationMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<HussarTransformationSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // (1) Allow other xenotypes
            listingStandard.CheckboxLabeled("HT.AllowOtherXenotypes".Translate(), ref settings.allowOtherXenotypes,
                "HT.AllowOtherXenotypesTooltip".Translate());
            listingStandard.GapLine();

            // (2) Item Costs
            listingStandard.Label("HT.ItemCosts".Translate());

            string goJuiceBuffer = settings.goJuiceCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.GoJuiceCost".Translate(),
                ref settings.goJuiceCost, ref goJuiceBuffer, 0, 99);

            string medicineBuffer = settings.medicineCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.MedicineCost".Translate(),
                ref settings.medicineCost, ref medicineBuffer, 0, 99);

            string componentBuffer = settings.componentCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.ComponentCost".Translate(),
                ref settings.componentCost, ref componentBuffer, 0, 99);

            listingStandard.CheckboxLabeled("    " + "HT.ConsumeMedicineOnFailure".Translate(),
                ref settings.consumeMedicineOnFailure, "HT.ConsumeMedicineOnFailureTooltip".Translate());

            if (settings.consumeMedicineOnFailure)
            {
                string medicineFailureBuffer = settings.medicineConsumedOnFailure.ToString();
                listingStandard.TextFieldNumericLabeled("        " + "HT.MedicineConsumedOnFailure".Translate(),
                    ref settings.medicineConsumedOnFailure, ref medicineFailureBuffer, 0, settings.medicineCost);
            }
            listingStandard.GapLine();

            // (3) Transformation Duration
            listingStandard.Label("HT.TransformationDuration".Translate() + ": " +
                "HT.Hours".Translate(settings.transformationDurationHours.ToString("0.0")));
            settings.transformationDurationHours = listingStandard.Slider(settings.transformationDurationHours, 0f, 24f);
            listingStandard.GapLine();

            // (4) Power Consumption
            listingStandard.Label("HT.PowerConsumption".Translate() + ": " +
                settings.powerConsumption.ToString() + " W");
            settings.powerConsumption = (int)listingStandard.Slider(settings.powerConsumption, 0, 1000);
            listingStandard.CheckboxLabeled("    " + "HT.StopOnPowerLoss".Translate(),
                ref settings.stopOnPowerLoss, "HT.StopOnPowerLossTooltip".Translate());
            listingStandard.GapLine();

            // (5) Research Cost
            listingStandard.Label("HT.ResearchCost".Translate());
            string researchBuffer = settings.researchCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.ResearchCostValue".Translate(),
                ref settings.researchCost, ref researchBuffer, 0, 9999);
            listingStandard.Label("HT.ResearchCostNote".Translate(), -1, "HT.ResearchCostNoteTooltip".Translate());

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Hussar Transformation";
        }

        public override void WriteSettings()
        {
            settings.ClampValues();
            base.WriteSettings();
            ApplySettingsToDefs();
        }

        public static void ApplySettingsToDefs()
        {
            if (settings == null) return;

            // Apply research cost only
            var researchDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("HussarTransformationSurgery");
            if (researchDef != null)
            {
                researchDef.baseCost = settings.researchCost;
            }

            // 전력 설정은 제거 - Tick()에서 PowerOutput으로 동적 관리
        }
    }

    // ====================================================================
    // 4. STARTUP INITIALIZER
    // ====================================================================

    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            HussarTransformationMod.ApplySettingsToDefs();
        }
    }

    // ====================================================================
    // 5. HUSSAR TRANSFORMATION POD (BUILDING)
    // ====================================================================

    // Building_HussarPod 클래스 수정 - Building 상속으로 변경

    public class Building_HussarPod : Building, IThingHolder
    {
        private CompPowerTrader powerComp;
        private float progress = 0f;
        private const int TicksPerRealSecond = 60;
        private const int TicksPerHour = 2500; // RimWorld hour

        // innerContainer를 직접 구현
        protected ThingOwner innerContainer;

        // Properties
        public bool IsRunning => ContainedPawn != null;
        public Pawn ContainedPawn => innerContainer?.FirstOrDefault() as Pawn;
        public bool PowerOn => powerComp?.PowerOn ?? true;
        public float ProgressPercent => IsRunning ?
            progress / (HussarTransformationMod.settings.transformationDurationHours * TicksPerHour) : 0f;

        // Building_Casket처럼 HasAnyContents 구현
        public bool HasAnyContents => innerContainer.Count > 0;

        // ====================================================================
        // Initialization
        // ====================================================================

        public Building_HussarPod()
        {
            innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
            }
        }

        // ====================================================================
        // IThingHolder 인터페이스 구현
        // ====================================================================

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        // ====================================================================
        // Save/Load
        // ====================================================================

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref progress, "progress", 0f);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        // ====================================================================
        // Main Update Loop - 이제 확실히 작동할 것
        // ====================================================================

        protected override void Tick()
        {
            base.Tick();

            // 디버그 로그
            /*
            if (Find.TickManager.TicksGame % 60 == 0) // 1초마다만 로그
            {
                string containedPawnName = ContainedPawn != null ? ContainedPawn.LabelShort : "None";
                string containerInfo = innerContainer != null ? $"Count: {innerContainer.Count}" : "null";
                Log.Message($"[HussarPod] Tick | ContainedPawn: {containedPawnName}, Container: {containerInfo}, Progress: {progress}, IsRunning: {IsRunning}");
            }
            */

            // Update power consumption based on state
            if (powerComp != null)
            {
                powerComp.PowerOutput = IsRunning ?
                    -HussarTransformationMod.settings.powerConsumption : 0f;
            }

            if (!IsRunning)
            {
                return;
            }

            // Check if the pawn inside has died
            if (ContainedPawn == null || ContainedPawn.Dead)
            {
                if (ContainedPawn != null)
                {
                    Messages.Message("HT.PawnDiedMessage".Translate(ContainedPawn.LabelShortCap),
                        this, MessageTypeDefOf.NegativeEvent);
                }
                EjectContents();
                progress = 0f;
                return;
            }

            // Power check
            bool needsPower = HussarTransformationMod.settings.powerConsumption > 0;
            if (needsPower && !PowerOn && HussarTransformationMod.settings.stopOnPowerLoss)
            {
                CancelTransformation("HT.PowerLossMessage".Translate());
                return;
            }

            // 욕구 관리
            var pawn = ContainedPawn;
            if (pawn?.needs != null)
            {
                if (pawn.needs.food != null) pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
                if (pawn.needs.rest != null) pawn.needs.rest.CurLevel = pawn.needs.rest.MaxLevel;
                if (pawn.needs.joy != null) pawn.needs.joy.CurLevel = pawn.needs.joy.MaxLevel;
                if (pawn.needs.comfort != null) pawn.needs.comfort.CurLevel = pawn.needs.comfort.MaxLevel;
                if (pawn.needs.beauty != null) pawn.needs.beauty.CurLevel = pawn.needs.beauty.MaxLevel;
                if (pawn.needs.outdoors != null) pawn.needs.outdoors.CurLevel = pawn.needs.outdoors.MaxLevel;
            }

            // 정신적 상태 관리
            if (pawn?.mindState?.mentalStateHandler != null)
            {
                pawn.mindState.mentalStateHandler.Reset();
            }

            // Progress the transformation
            progress += 1f;

            // 진행 상황 로그 (10초마다)
            /*
            if (Find.TickManager.TicksGame % 600 == 0)
            {
                Log.Message($"[HussarPod] Progress updated: {progress}, Percent: {ProgressPercent:P2}");
            }
            */

            // Check if transformation is complete
            if (ProgressPercent >= 1f)
            {
                FinishTransformation();
            }
        }

        // ====================================================================
        // 직접 구현한 Pawn 관리 메서드들
        // ====================================================================

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (thing is Pawn pawn)
            {
                return CanAcceptPawn(pawn).Accepted && TryAcceptPawn(pawn);
            }
            return false;
        }

        private bool TryAcceptPawn(Pawn pawn)
        {
            if (!CanAcceptPawn(pawn).Accepted) return false;

            // Consume ingredients
            if (!ConsumeIngredients())
            {
                Messages.Message("HT.MissingIngredientsMessage".Translate(), pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            // Pawn을 DeSpawn하고 container에 추가
            if (pawn.Spawned)
            {
                pawn.DeSpawn();
            }

            bool success = innerContainer.TryAdd(pawn, true);

            if (success)
            {
                progress = 0f;

                // Pawn의 faction 정보 업데이트 (Building_Casket 방식)
                if (pawn.Faction != null && pawn.Faction.IsPlayer)
                {
                    // contentsKnown = true; // 필요시 추가
                }

                Log.Message($"[HussarPod] Pawn {pawn.LabelShort} accepted, Container count: {innerContainer?.Count ?? 0}");
            }

            return success;
        }

        public AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (pawn == null)
                return false;

            if (!pawn.IsColonist || pawn.IsQuestLodger())
                return new AcceptanceReport("HT.AcceptanceReport_NotColonist".Translate());

            if (pawn.ageTracker?.AgeBiologicalYears < 13)
                return new AcceptanceReport("HT.AcceptanceReport_TooYoung".Translate());

            if (pawn.genes?.Xenotype != null &&
                pawn.genes.Xenotype != XenotypeDefOf.Baseliner &&
                !HussarTransformationMod.settings.allowOtherXenotypes)
                return new AcceptanceReport("HT.AcceptanceReport_NotBaseliner".Translate());

            if (HasAnyContents)
                return new AcceptanceReport("HT.AcceptanceReport_Occupied".Translate());

            bool needsPower = HussarTransformationMod.settings.powerConsumption > 0;
            if (needsPower && !PowerOn)
                return new AcceptanceReport("HT.AcceptanceReport_NoPower".Translate());

            return true;
        }

        // Building_Casket의 EjectContents를 직접 구현
        public virtual void EjectContents()
        {
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }

        // ====================================================================
        // Transformation Logic
        // ====================================================================

        private void FinishTransformation()
        {
            Pawn pawn = ContainedPawn;
            if (pawn == null)
            {
                Log.Error("Hussar Pod tried to finish transformation but pawn was null.");
                innerContainer.Clear();
                progress = 0f;
                return;
            }

            EjectContents();

            // Change Xenotype to Hussar
            if (pawn.genes != null)
            {
                pawn.genes.SetXenotype(XenotypeDefOf.Hussar);
            }

            // Add Go-Juice dependency
            if (pawn.health != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.GoJuiceAddiction, pawn);
                pawn.health.AddHediff(hediff);
            }

            Messages.Message("HT.TransformationCompleteMessage".Translate(pawn.LabelShortCap),
                pawn, MessageTypeDefOf.PositiveEvent);
            progress = 0f;
        }

        private void CancelTransformation(string reason)
        {
            Pawn pawn = ContainedPawn;
            if (pawn == null)
            {
                innerContainer.Clear();
                progress = 0f;
                return;
            }

            EjectContents();
            progress = 0f;

            if (HussarTransformationMod.settings.consumeMedicineOnFailure)
            {
                Messages.Message(
                    "HT.TransformationCancelledMessage".Translate(pawn.LabelShortCap, reason) + "\n" +
                    "HT.MedicineConsumedOnFailureMessage".Translate(
                        HussarTransformationMod.settings.medicineConsumedOnFailure),
                    pawn, MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                Messages.Message(
                    "HT.TransformationCancelledMessage".Translate(pawn.LabelShortCap, reason),
                    pawn, MessageTypeDefOf.NegativeEvent);
            }
        }

        // ====================================================================
        // 파괴 시 내용물 처리 (Building_Casket 방식)
        // ====================================================================

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.Destroy(mode);

            if (innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
            {
                if (mode != DestroyMode.Deconstruct)
                {
                    // Pawn이 있으면 기절시키기
                    List<Pawn> pawns = new List<Pawn>();
                    foreach (Thing item in innerContainer)
                    {
                        if (item is Pawn pawn)
                        {
                            pawns.Add(pawn);
                        }
                    }
                    foreach (Pawn pawn in pawns)
                    {
                        HealthUtility.DamageUntilDowned(pawn);
                    }
                }
                innerContainer.TryDropAll(Position, map, ThingPlaceMode.Near);
            }
            innerContainer.ClearAndDestroyContents();
        }

        // ====================================================================
        // 나머지 메서드들은 기존과 동일 (ConsumeIngredients, GetInspectString, GetGizmos 등)
        // ====================================================================

        private bool ConsumeIngredients()
        {
            var settings = HussarTransformationMod.settings;
            var map = this.Map;
            if (map == null) return false;

            // Check Go-Juice
            int goJuiceAvailable = CountAvailableThings(ThingDefOf.GoJuice);
            if (goJuiceAvailable < settings.goJuiceCost)
                return false;

            // Check Medicine (any type)
            int medicineAvailable = CountAvailableMedicine();
            if (medicineAvailable < settings.medicineCost)
                return false;

            // Check Components
            int componentsAvailable = CountAvailableThings(ThingDefOf.ComponentIndustrial);
            if (componentsAvailable < settings.componentCost)
                return false;

            // Consume ingredients
            ConsumeThings(ThingDefOf.GoJuice, settings.goJuiceCost);
            ConsumeMedicine(settings.medicineCost);
            ConsumeThings(ThingDefOf.ComponentIndustrial, settings.componentCost);

            return true;
        }

        private int CountAvailableThings(ThingDef thingDef)
        {
            if (this.Map == null) return 0;
            return this.Map.listerThings.ThingsOfDef(thingDef)
                .Where(t => !t.IsForbidden(Faction.OfPlayer) && t.Spawned)
                .Sum(t => t.stackCount);
        }

        private int CountAvailableMedicine()
        {
            if (this.Map == null) return 0;

            var medicines = this.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine)
                .Where(t => !t.IsForbidden(Faction.OfPlayer) && t.Spawned)
                .Sum(t => t.stackCount);

            return medicines;
        }

        private void ConsumeThings(ThingDef thingDef, int count)
        {
            if (count <= 0 || this.Map == null) return;

            var things = this.Map.listerThings.ThingsOfDef(thingDef)
                .Where(t => !t.IsForbidden(Faction.OfPlayer) && t.Spawned)
                .OrderBy(t => t.Position.DistanceToSquared(this.Position))
                .ToList();

            int remaining = count;
            foreach (var thing in things)
            {
                int numToTake = Mathf.Min(remaining, thing.stackCount);
                thing.SplitOff(numToTake).Destroy(DestroyMode.Vanish);
                remaining -= numToTake;
                if (remaining <= 0) break;
            }
        }

        private void ConsumeMedicine(int count)
        {
            if (count <= 0 || this.Map == null) return;

            var medicines = this.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine)
                .Where(t => !t.IsForbidden(Faction.OfPlayer) && t.Spawned)
                .OrderBy(t => t.MarketValue)
                .ThenBy(t => t.Position.DistanceToSquared(this.Position))
                .ToList();

            int remaining = count;
            foreach (var medicine in medicines)
            {
                int numToTake = Mathf.Min(remaining, medicine.stackCount);
                medicine.SplitOff(numToTake).Destroy(DestroyMode.Vanish);
                remaining -= numToTake;
                if (remaining <= 0) break;
            }
        }

        public override string GetInspectString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(base.GetInspectString());

            if (IsRunning && ContainedPawn != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("HT.TransformationProgress".Translate(ProgressPercent.ToStringPercent()));
            }

            return sb.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            // Add "Begin Transformation" button when not running
            if (!IsRunning && this.Map != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "HT.BeginTransformation".Translate(),
                    defaultDesc = "HT.BeginTransformationDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/OpenCryptosleepCasket", false),
                    action = () =>
                    {
                        var floatMenuOptions = new List<FloatMenuOption>();
                        foreach (var pawn in this.Map.mapPawns.FreeColonists)
                        {
                            var report = CanAcceptPawn(pawn);
                            if (report.Accepted)
                            {
                                floatMenuOptions.Add(new FloatMenuOption(pawn.LabelCap, () =>
                                {
                                    var job = JobMaker.MakeJob(
                                        DefDatabase<JobDef>.GetNamed("EnterHussarPod"), this);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }));
                            }
                            else
                            {
                                floatMenuOptions.Add(new FloatMenuOption(
                                    pawn.LabelCap + ": " + report.Reason, null));
                            }
                        }

                        if (floatMenuOptions.Any())
                        {
                            Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                        }
                        else
                        {
                            Messages.Message("No suitable colonists available.",
                                MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };
            }

            // Add "Cancel Transformation" button when running
            if (IsRunning && ContainedPawn != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Cancel Transformation",
                    defaultDesc = "Cancel the ongoing transformation and eject the pawn.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/CancelLoad", false),
                    action = () =>
                    {
                        CancelTransformation("manually cancelled");
                    }
                };
            }
        }
    }

    // ====================================================================
    // 6. JOB DRIVER for entering the pod
    // ====================================================================

    public class JobDriver_EnterHussarPod : JobDriver
    {
        private Building_HussarPod Pod => (Building_HussarPod)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            // Go to the pod
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // Enter the pod - Building_Casket 방식으로 변경
            Toil enter = new Toil();
            enter.initAction = () =>
            {
                var pod = Pod;
                if (pod != null)
                {
                    // Building_Casket의 TryAcceptThing 사용
                    pod.TryAcceptThing(pawn, true);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}