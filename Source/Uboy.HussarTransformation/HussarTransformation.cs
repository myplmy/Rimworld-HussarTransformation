using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

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
        public bool consumeGoJuiceOnFailure = false;
        public int goJuiceConsumedOnFailure = 0;
        public bool consumeComponentOnFailure = false;
        public int componentConsumedOnFailure = 0;
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
            Scribe_Values.Look(ref consumeGoJuiceOnFailure, "consumeMedicineOnFailure", false);
            Scribe_Values.Look(ref goJuiceConsumedOnFailure, "goJuiceConsumedOnFailure", 0);
            Scribe_Values.Look(ref consumeComponentOnFailure, "consumeComponentOnFailure", false);
            Scribe_Values.Look(ref componentConsumedOnFailure, "componentConsumedOnFailure", 0);
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
            if (goJuiceConsumedOnFailure > goJuiceCost)
            {
                goJuiceConsumedOnFailure = goJuiceCost;
            }
            if (componentConsumedOnFailure > componentCost)
            {
                componentConsumedOnFailure = componentCost;
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

            listingStandard.CheckboxLabeled("    " + "HT.ConsumeGoJuiceOnFailure".Translate(),
                ref settings.consumeGoJuiceOnFailure, "HT.ConsumeGoJuiceOnFailureTooltip".Translate());

            if (settings.consumeGoJuiceOnFailure)
            {
                string goJuiceFailureBuffer = settings.goJuiceConsumedOnFailure.ToString();
                listingStandard.TextFieldNumericLabeled("        " + "HT.GoJuiceConsumedOnFailure".Translate(),
                    ref settings.goJuiceConsumedOnFailure, ref goJuiceFailureBuffer, 0, settings.goJuiceCost);
            }

            listingStandard.CheckboxLabeled("    " + "HT.ConsumeComponentOnFailure".Translate(),
                ref settings.consumeComponentOnFailure, "HT.ConsumeComponentOnFailureTooltip".Translate());

            if (settings.consumeComponentOnFailure)
            {
                string componentFailureBuffer = settings.componentConsumedOnFailure.ToString();
                listingStandard.TextFieldNumericLabeled("        " + "HT.ComponentConsumedOnFailure".Translate(),
                    ref settings.componentConsumedOnFailure, ref componentFailureBuffer, 0, settings.medicineCost);
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

    public class Building_HussarPod : Building_Bed, IHaulDestination, IThingHolder
    {
        private CompPowerTrader powerComp;
        private float progress = 0f;
        private const int TicksPerRealSecond = 60;
        private const int TicksPerHour = 2500; // RimWorld hour

        // Properties
        public bool IsRunning => GetCurOccupant(0) != null;
        public Pawn ContainedPawn => GetCurOccupant(0);
        public bool PowerOn => powerComp?.PowerOn ?? true;
        public float ProgressPercent => IsRunning ? progress / (HussarTransformationMod.settings.transformationDurationHours * TicksPerHour) : 0f;
        public bool HasAnyContents => AnyOccupants;

        // 재료 저장 전용 innerContainer
        protected ThingOwner innerContainer;

        // 새로 추가: 재료 저장 변수들
        private int storedGoJuice = 0;
        private int storedMedicine = 0;
        private int storedComponents = 0;

        // 재료 저장 용량 (설정 가능)
        private const int MAX_STORED_GOJUICE = 5;
        private const int MAX_STORED_MEDICINE = 5;
        private const int MAX_STORED_COMPONENTS = 5;

        public int MaxStoredGoJuice => MAX_STORED_GOJUICE;
        public int MaxStoredMedicine => MAX_STORED_MEDICINE;
        public int MaxStoredComponents => MAX_STORED_COMPONENTS;

        // 새로 추가: 재료 관리 프로퍼티들
        public int StoredGoJuice => storedGoJuice;
        public int StoredMedicine => storedMedicine;
        public int StoredComponents => storedComponents;

        public bool HasEnoughMaterials =>
            StoredGoJuice >= HussarTransformationMod.settings.goJuiceCost &&
            StoredMedicine >= HussarTransformationMod.settings.medicineCost &&
            StoredComponents >= HussarTransformationMod.settings.componentCost;

        // 새로 추가: 재료 추가 메서드들
        public bool TryAddGoJuice(int amount)
        {
            if (storedGoJuice + amount <= MAX_STORED_GOJUICE)
            {
                storedGoJuice += amount;
                return true;
            }
            return false;
        }

        public bool TryAddMedicine(int amount)
        {
            if (storedMedicine + amount <= MAX_STORED_MEDICINE)
            {
                storedMedicine += amount;
                return true;
            }
            return false;
        }

        public bool TryAddComponents(int amount)
        {
            if (storedComponents + amount <= MAX_STORED_COMPONENTS)
            {
                storedComponents += amount;
                return true;
            }
            return false;
        }

        // 재료 소모 메서드 (기존 ConsumeIngredients 대체)
        private bool ConsumeStoredMaterials()
        {
            var settings = HussarTransformationMod.settings;

            // 저장된 재료가 충분한지 확인
            if (!HasEnoughMaterials) return false;

            // 재료 소모
            storedGoJuice -= settings.goJuiceCost;
            storedMedicine -= settings.medicineCost;
            storedComponents -= settings.componentCost;

            return true;
        }
        private void ConsumeStoredMaterials_Cancel()
        {
            var settings = HussarTransformationMod.settings;
            if (!settings.consumeMedicineOnFailure)
            {
                return;
            }
            else
            {
                if(settings.medicineConsumedOnFailure > 0 && settings.consumeMedicineOnFailure)
                {
                    storedMedicine -= Math.Min(settings.medicineConsumedOnFailure, storedMedicine);
                }
                if(settings.goJuiceConsumedOnFailure > 0 && settings.consumeGoJuiceOnFailure)
                {
                    storedGoJuice -= Math.Min(settings.goJuiceConsumedOnFailure, storedGoJuice);
                }
                if(settings.componentConsumedOnFailure > 0 && settings.consumeComponentOnFailure)
                {
                    storedComponents -= Math.Min(settings.componentConsumedOnFailure, storedComponents);
                }

            }
                
        }

        // ====================================================================
        // IHaulDestination 구현 - 바닐라 운반 시스템
        // ====================================================================
        public virtual bool ShouldProduceWorkNeeded()
        {
            // 저장소에 여유가 있으면 작업이 필요함을 알림
            return SpaceRemainingFor(ThingDefOf.GoJuice) > 0 ||
                   SpaceRemainingFor(ThingDefOf.ComponentIndustrial) > 0 ||
                   DefDatabase<ThingDef>.AllDefs.Where(def => def.IsMedicine)
                       .Any(medicineDef => SpaceRemainingFor(medicineDef) > 0);
        }

        public bool Accepts(Thing thing)
        {
            // 디버그 로그 추가
/*            Log.Message($"[HussarPod DEBUG] Accepts() called - Thing: {thing?.def?.label ?? "null"}, " +
                        $"StackCount: {thing?.stackCount ?? 0}");*/

            if (thing == null)
            {
                //Log.Message($"[HussarPod DEBUG] Accepts() returning FALSE - Thing is null");
                return false;
            }

            // 자동공급 설정과 관계없이 저장 용량만 확인
            // (자동공급 제어는 HaulDestinationEnabled에서 처리)
            if (thing.def == ThingDefOf.GoJuice && StoredGoJuice < MAX_STORED_GOJUICE)
            {
                /*
                  Log.Message($"[HussarPod DEBUG] Accepts() returning TRUE - Go-Juice accepted " +
                            $"(Current: {StoredGoJuice}/{MAX_STORED_GOJUICE})");
                */
                return true;
            }
            if (thing.def.IsMedicine && StoredMedicine < MAX_STORED_MEDICINE)
            {
                /*
                Log.Message($"[HussarPod DEBUG] Accepts() returning TRUE - Medicine accepted " +
                            $"(Current: {StoredMedicine}/{MAX_STORED_MEDICINE})");
                */
                return true;
            }
            if (thing.def == ThingDefOf.ComponentIndustrial && StoredComponents < MAX_STORED_COMPONENTS)
            {
/*                Log.Message($"[HussarPod DEBUG] Accepts() returning TRUE - Components accepted " +
                            $"(Current: {StoredComponents}/{MAX_STORED_COMPONENTS})");*/
                return true;
            }

            //Log.Message($"[HussarPod DEBUG] Accepts() returning FALSE - No matching condition");
            return false;
        }

        public bool TryAcceptHaulable(Thing thing, Pawn hauler)
        {
            Log.Message($"[HussarPod DEBUG] TryAcceptHaulable() called - Thing: {thing?.def?.label ?? "null"}, " +
                        $"StackCount: {thing?.stackCount ?? 0}, " +
                        $"Hauler: {hauler?.LabelShort ?? "null"}");

            if (!Accepts(thing))
            {
                Log.Message($"[HussarPod DEBUG] TryAcceptHaulable() returning FALSE - Accepts() returned false");
                return false;
            }

            // SpaceRemainingFor로 실제 수용 가능한 양 확인
            int spaceRemaining = SpaceRemainingFor(thing.def);
            if (spaceRemaining <= 0)
            {
                Log.Message($"[HussarPod DEBUG] TryAcceptHaulable() returning FALSE - No space remaining");
                return false;
            }

            // 실제 수용할 양 계산
            int amountToAccept = Math.Min(thing.stackCount, spaceRemaining);
            Log.Message($"[HussarPod DEBUG] Calculated amountToAccept: {amountToAccept} (stackCount: {thing.stackCount}, spaceRemaining: {spaceRemaining})");

            if (amountToAccept <= 0)
            {
                Log.Message($"[HussarPod DEBUG] TryAcceptHaulable() returning FALSE - Nothing to accept");
                return false;
            }

            // 재료별 처리
            bool success = false;
            string materialName = "";

            if (thing.def == ThingDefOf.GoJuice)
            {
                if (TryAddGoJuice(amountToAccept))
                {
                    success = true;
                    materialName = "Go-Juice";
                }
            }
            else if (thing.def.IsMedicine)
            {
                if (TryAddMedicine(amountToAccept))
                {
                    success = true;
                    materialName = thing.def.label;
                }
            }
            else if (thing.def == ThingDefOf.ComponentIndustrial)
            {
                if (TryAddComponents(amountToAccept))
                {
                    success = true;
                    materialName = "Components";
                }
            }

            if (success)
            {
                // 성공한 경우 아이템 처리
                if (amountToAccept >= thing.stackCount)
                {
                    // 전체 스택을 수용하는 경우
                    Log.Message($"[HussarPod DEBUG] Full acceptance - Destroying entire stack: {thing.stackCount}");
                    thing.Destroy(DestroyMode.Vanish);
                    Messages.Message($"Added {amountToAccept} {materialName} to transformation pod",
                                    this, MessageTypeDefOf.TaskCompletion);
                    return true;
                }
                else
                {
                    // 일부만 수용하는 경우
                    Log.Message($"[HussarPod DEBUG] Partial acceptance - Splitting: accept {amountToAccept}, leave {thing.stackCount - amountToAccept}");
                    Thing acceptedPortion = thing.SplitOff(amountToAccept);
                    acceptedPortion.Destroy(DestroyMode.Vanish);

                    Messages.Message($"Added {amountToAccept} {materialName} to transformation pod (storage limit reached)",
                                    this, MessageTypeDefOf.TaskCompletion);

                    // 여기서 false를 반환하여 바닐라 운반 시스템이 나머지를 처리하도록 함
                    //Log.Message($"[HussarPod DEBUG] TryAcceptHaulable() returning FALSE - Partial acceptance, let vanilla handle remainder");
                    return false;
                }
            }

            //Log.Message($"[HussarPod DEBUG] TryAcceptHaulable() returning FALSE - Storage failed");
            return false;
        }


        // 또는 SpaceRemainingFor 메서드 구현을 통해 올바른 양만 운반하도록 안내
        public int SpaceRemainingFor(ThingDef thingDef)
        {
            int remaining = 0;
            if (thingDef == ThingDefOf.GoJuice)
                remaining = MAX_STORED_GOJUICE - StoredGoJuice;
            else if (thingDef.IsMedicine)
                remaining = MAX_STORED_MEDICINE - StoredMedicine;
            else if (thingDef == ThingDefOf.ComponentIndustrial)
                remaining = MAX_STORED_COMPONENTS - StoredComponents;

            //Log.Message($"[HussarPod DEBUG] SpaceRemainingFor({thingDef?.label}) called - returning {remaining}");
            return remaining;
        }


        // ====================================================================
        // IHaulDestination & IStoreSettingsParent 인터페이스 구현 추가
        // ====================================================================

        private StorageSettings storageSettings;

        // IHaulDestination.HaulDestinationEnabled 구현
        public bool HaulDestinationEnabled
        {
            get
            {
                return false; // 자동공급 설정에 따라 반환
            }
        }

        // IStoreSettingsParent 인터페이스 구현
        public StorageSettings GetStoreSettings()
        {
            if (storageSettings == null)
            {
                storageSettings = new StorageSettings(this);
                // 3가지 재료만 허용하도록 설정
                storageSettings.filter.SetDisallowAll();
                storageSettings.filter.SetAllow(ThingDefOf.GoJuice, true);
                storageSettings.filter.SetAllow(ThingDefOf.ComponentIndustrial, true);

                // 모든 의약품 허용
                foreach (ThingDef medicineDef in DefDatabase<ThingDef>.AllDefs.Where(def => def.IsMedicine))
                {
                    storageSettings.filter.SetAllow(medicineDef, true);
                }
            }
            return storageSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return null; // 부모 저장소 설정 없음
        }

        public void Notify_SettingsChanged()
        {
            // 저장소 설정 변경 시 호출되는 메서드 (현재는 아무 작업 안 함)
        }

        public bool StorageTabVisible => true; // 저장소 탭 표시 여부

        // ====================================================================
        // 우클릭 메뉴 추가 (선택사항 - 더 명확한 UX를 위해)
        // ====================================================================

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield break;
            }

            // Go-Juice 운반 옵션
            if (StoredGoJuice < MAX_STORED_GOJUICE)
            {
                var goJuice = selPawn.Map.listerThings.ThingsOfDef(ThingDefOf.GoJuice)
                    .Where(t => !t.IsForbidden(selPawn.Faction) &&
                               selPawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    .OrderBy(t => t.Position.DistanceToSquared(selPawn.Position))
                    .FirstOrDefault();

                if (goJuice != null)
                {
                    int canHaul = Math.Min(goJuice.stackCount, MAX_STORED_GOJUICE - StoredGoJuice);
                    yield return new FloatMenuOption($"Haul {canHaul} {ThingDefOf.GoJuice.label} to {this.Label} ({StoredGoJuice}/{MAX_STORED_GOJUICE})", () =>
                    {
                        var job = JobMaker.MakeJob(JobDefOf.HaulToContainer, goJuice, this);
                        job.count = canHaul; // 실제 필요한 만큼만 운반
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
                }
            }

            // Medicine 운반 옵션
            if (StoredMedicine < MAX_STORED_MEDICINE)
            {
                var medicine = selPawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine)
                    .Where(t => !t.IsForbidden(selPawn.Faction) &&
                               selPawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    .OrderBy(t => t.MarketValue)
                    .ThenBy(t => t.Position.DistanceToSquared(selPawn.Position))
                    .FirstOrDefault();

                if (medicine != null)
                {
                    int canHaul = Math.Min(medicine.stackCount, MAX_STORED_MEDICINE - StoredMedicine);
                    yield return new FloatMenuOption($"Haul {canHaul} {medicine.def.label} to {this.Label} ({StoredMedicine}/{MAX_STORED_MEDICINE})", () =>
                    {
                        var job = JobMaker.MakeJob(JobDefOf.HaulToContainer, medicine, this);
                        job.count = canHaul; // 실제 필요한 만큼만 운반
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
                }
            }

            // Components 운반 옵션
            if (StoredComponents < MAX_STORED_COMPONENTS)
            {
                var components = selPawn.Map.listerThings.ThingsOfDef(ThingDefOf.ComponentIndustrial)
                    .Where(t => !t.IsForbidden(selPawn.Faction) &&
                               selPawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    .OrderBy(t => t.Position.DistanceToSquared(selPawn.Position))
                    .FirstOrDefault();

                if (components != null)
                {
                    int canHaul = Math.Min(components.stackCount, MAX_STORED_COMPONENTS - StoredComponents);
                    yield return new FloatMenuOption($"Haul {canHaul} components to {this.Label} ({StoredComponents}/{MAX_STORED_COMPONENTS})", () =>
                    {
                        var job = JobMaker.MakeJob(JobDefOf.HaulToContainer, components, this);
                        job.count = canHaul; // 실제 필요한 만큼만 운반
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
                }
            }
        }

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
        }

        // ====================================================================
        // I ThingHolder 구현
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
        // Main Update Loop
        // ====================================================================

        protected override void Tick()
        {
            base.Tick();

            // materialContainer의 아이템을 저장소로 변환
            if (innerContainer.Count > 0)
            {
                List<Thing> itemsToProcess = new List<Thing>();

                foreach (Thing thing in innerContainer)
                {
                    if (!(thing is Pawn)) // Pawn이 아닌 아이템들만 처리
                    {
                        itemsToProcess.Add(thing);
                    }
                }

                // 아이템들을 하나씩 처리하되, 처리 후 즉시 컨테이너에서 제거
                for (int i = itemsToProcess.Count - 1; i >= 0; i--) // 역순으로 처리
                {
                    Thing item = itemsToProcess[i];

                    Log.Message($"[HussarPod DEBUG] Processing container item: {item.def.label}, StackCount: {item.stackCount}");

                    // 저장 가능한 양 계산
                    int canStore = 0;
                    if (item.def == ThingDefOf.GoJuice)
                        canStore = Math.Min(item.stackCount, MAX_STORED_GOJUICE - StoredGoJuice);
                    else if (item.def.IsMedicine)
                        canStore = Math.Min(item.stackCount, MAX_STORED_MEDICINE - StoredMedicine);
                    else if (item.def == ThingDefOf.ComponentIndustrial)
                        canStore = Math.Min(item.stackCount, MAX_STORED_COMPONENTS - StoredComponents);

                    Log.Message($"[HussarPod DEBUG] Can store: {canStore} of {item.stackCount}");

                    if (canStore > 0)
                    {
                        // 저장 처리
                        bool stored = false;
                        string materialName = "";

                        if (item.def == ThingDefOf.GoJuice && TryAddGoJuice(canStore))
                        {
                            stored = true;
                            materialName = "Go-Juice";
                        }
                        else if (item.def.IsMedicine && TryAddMedicine(canStore))
                        {
                            stored = true;
                            materialName = item.def.label;
                        }
                        else if (item.def == ThingDefOf.ComponentIndustrial && TryAddComponents(canStore))
                        {
                            stored = true;
                            materialName = "Components";
                        }

                        if (stored)
                        {
                            Log.Message($"[HussarPod DEBUG] Successfully stored {canStore} {materialName}");

                            if (canStore >= item.stackCount)
                            {
                                // 전체 아이템 저장 - 컨테이너에서 제거
                                innerContainer.Remove(item);
                                Log.Message($"[HussarPod DEBUG] Removed entire item from container");
                            }
                            else
                            {
                                // 일부만 저장 - 스택 크기 줄이기
                                item.stackCount -= canStore;
                                Log.Message($"[HussarPod DEBUG] Reduced item stack to {item.stackCount}");

                                // 나머지를 맵에 드롭
                                Thing remainder = item.SplitOff(item.stackCount);
                                innerContainer.Remove(item); // 원본 제거

                                // 나머지를 근처에 드롭
                                if (!GenPlace.TryPlaceThing(remainder, this.InteractionCell, this.Map, ThingPlaceMode.Near))
                                {
                                    GenPlace.TryPlaceThing(remainder, this.Position, this.Map, ThingPlaceMode.Near);
                                }
                                Log.Message($"[HussarPod DEBUG] Dropped remainder {remainder.stackCount} items near pod");
                            }

                            Messages.Message($"Added {canStore} {materialName} to transformation pod",
                                           this, MessageTypeDefOf.TaskCompletion);
                        }
                    }
                    else
                    {
                        // 저장할 수 없음 - 아이템을 맵에 드롭하고 컨테이너에서 제거
                        Log.Message($"[HussarPod DEBUG] Cannot store {item.def.label} - dropping to ground");
                        innerContainer.Remove(item);

                        if (!GenPlace.TryPlaceThing(item, this.InteractionCell, this.Map, ThingPlaceMode.Near))
                        {
                            GenPlace.TryPlaceThing(item, this.Position, this.Map, ThingPlaceMode.Near);
                        }
                    }
                }
            }

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
            /*
            // 변형 Job 유지 확인
            if (pawn.CurJob == null || pawn.CurJob.targetA.Thing != this)
            {
                Job transformJob = JobMaker.MakeJob(JobDefOf.LayDown, this);
                transformJob.forceSleep = true;
                pawn.jobs.TryTakeOrderedJob(transformJob, JobTag.Misc);
            }*/

            // 정신적 상태 관리
            if (pawn?.mindState?.mentalStateHandler != null)
            {
                pawn.mindState.mentalStateHandler.Reset();
            }

            // Progress the transformation
            progress += 1f;


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

        // TryAcceptPawn 수정 (기존 ConsumeIngredients → ConsumeStoredMaterials)
        private bool TryAcceptPawn(Pawn pawn)
        {
            if (!CanAcceptPawn(pawn).Accepted) return false;
            if (!HasEnoughMaterials)
            {
                Messages.Message("HT.MissingStoredMaterialsMessage".Translate(), pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            Job transformJob = JobMaker.MakeJob(JobDefOf.LayDown, this);
            transformJob.forceSleep = true;
            pawn.jobs.TryTakeOrderedJob(transformJob, JobTag.Misc);

            progress = 0f;
            return true;
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

            // 이미 다른 포드에서 변형 중인지 체크
            if (this.Map != null)
            {
                foreach (var building in this.Map.listerBuildings.AllBuildingsColonistOfClass<Building_HussarPod>())
                {
                    if (building != this && building.ContainedPawn == pawn)
                    {
                        return new AcceptanceReport("HT.AcceptanceReport_AlreadyInTransformation".Translate());
                    }
                }
            }

            if (HasAnyContents)
                return new AcceptanceReport("HT.AcceptanceReport_Occupied".Translate());

            bool needsPower = HussarTransformationMod.settings.powerConsumption > 0;
            if (needsPower && !PowerOn)
                return new AcceptanceReport("HT.AcceptanceReport_NoPower".Translate());

            return true;
        }


        public virtual void EjectContents()
        {
            var pawn = ContainedPawn;
            if (pawn != null)
            {
                // 침대에서 일어나게 하기
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
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
                progress = 0f;
                return;
            }

            EjectContents();

            // Change Xenotype to Hussar
            if (pawn.genes != null)
            {
                pawn.genes.SetXenotype(XenotypeDefOf.Hussar);
            }
            if(ConsumeStoredMaterials())
            {
                Log.Error("[Hussar Transformation] Not Enough Materials in Pod");
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
                progress = 0f;
                return;
            }

            EjectContents();
            progress = 0f;

            if (HussarTransformationMod.settings.consumeMedicineOnFailure)
            {
                ConsumeStoredMaterials_Cancel();
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
            var pawn = ContainedPawn;
            if (pawn != null && mode != DestroyMode.Deconstruct)
            {
                HealthUtility.DamageUntilDowned(pawn);
            }

            base.Destroy(mode);
        }

        // ====================================================================
        // ExposeData 메서드 수정 (저장소 설정 저장/로드)
        // ====================================================================

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref progress, "progress", 0f);
            Scribe_Deep.Look(ref storageSettings, "storageSettings", this);

            // 재료 저장 정보
            Scribe_Values.Look(ref storedGoJuice, "storedGoJuice", 0);
            Scribe_Values.Look(ref storedMedicine, "storedMedicine", 0);
            Scribe_Values.Look(ref storedComponents, "storedComponents", 0);
        }
        // ====================================================================
        // GetInspectString
        // ====================================================================

        // GetInspectString 수정 (재료 저장 상태 표시)
        public override string GetInspectString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(base.GetInspectString());

            // 빈 줄 제거를 위해 조건부 AppendLine 사용
            if (sb.Length > 0)
            {
                sb.Append("\n"); // AppendLine() 대신 "\n" 사용
            }
            if (IsRunning && ContainedPawn != null)
            {
                sb.Append("HT.TransformationProgress".Translate(ProgressPercent.ToStringPercent()));
                sb.Append("\n");
            }
            sb.Append("HT.StoredMaterials".Translate() + ":");
            sb.Append($"\n  {ThingDefOf.GoJuice.LabelCap}: {StoredGoJuice}/{MAX_STORED_GOJUICE}");
            sb.Append($"\n  {"HT.Medicine".Translate()}: {StoredMedicine}/{MAX_STORED_MEDICINE}");
            sb.Append($"\n  {ThingDefOf.ComponentIndustrial.LabelCap}: {StoredComponents}/{MAX_STORED_COMPONENTS}");

            return sb.ToString();
        }

        // ====================================================================
        // GetGizmos - 사용자정의 버튼 추가
        // ====================================================================

        public override IEnumerable<Gizmo> GetGizmos()
        {

            // 전력 관련 기즈모 수동 추가
            var powerComp = GetComp<CompPowerTrader>();
            if (powerComp != null)
            {
                foreach (var gizmo in powerComp.CompGetGizmosExtra())
                {
                    yield return gizmo;
                }
            }

            // Begin Transformation 버튼
            if (!IsRunning && HasEnoughMaterials && this.Map != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "HT.BeginTransformation".Translate(),
                    defaultDesc = "HT.BeginTransformationDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Xenotypes/Hussar", false),
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
                                    // 실행 시점 재검증
                                    var currentReport = CanAcceptPawn(pawn);
                                    if (!currentReport.Accepted)
                                    {
                                        Messages.Message($"{pawn.LabelShortCap}: {currentReport.Reason}",
                                            MessageTypeDefOf.RejectInput, false);
                                        return;
                                    }

                                    // 추가 안전 검사: 정착민이 맵에 있는지 확인
                                    if (!pawn.Spawned || pawn.Map != this.Map)
                                    {
                                        Messages.Message($"{pawn.LabelShortCap} is no longer available.",
                                            MessageTypeDefOf.RejectInput, false);
                                        return;
                                    }

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

            // Cancel 버튼
            if (IsRunning && ContainedPawn != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Cancel Transformation",
                    defaultDesc = "Cancel the ongoing transformation and eject the pawn.",
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", false),
                    action = () =>
                    {
                        CancelTransformation("manually cancelled");
                    }
                };
            }
        }
    }

    // ====================================================================
    // 6. JOB DRIVER for entering the pod & supplying materials
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

    public class JobDriver_SupplyMaterials : JobDriver
    {
        private Building_HussarPod Pod => (Building_HussarPod)job.targetA.Thing;
        private Thing Material => job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) &&
                    pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);
            this.FailOnBurningImmobile(TargetIndex.A);

            // 재료로 이동
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            // 디버그 로그 - 예약 체크 수정
            Toil checkMaterial = new Toil();
            checkMaterial.initAction = () =>
            {
                var material = Material;
                bool isReserved = material != null ?
                    this.Map.reservationManager.IsReservedByAnyoneOf(material, Faction.OfPlayer) : false; // 올바른 방법

                Log.Message($"[HussarPod] Checking material: {material?.def?.label}, StackCount: {material?.stackCount}, Forbidden: {material?.IsForbidden(Faction.OfPlayer)}, Reserved: {isReserved}");
            };
            checkMaterial.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return checkMaterial;

            // 재료 집기
            //yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            // Toils_Haul.StartCarryThing 대신 직접 구현
            Toil pickupMaterial = new Toil();
            pickupMaterial.initAction = () =>
            {
                var material = Material;

                // 이미 들고 있는 것이 있으면 먼저 처리
                if (pawn.carryTracker.CarriedThing != null)
                {
                    Log.Message($"[HussarPod] Pawn already carrying: {pawn.carryTracker.CarriedThing.def.label}");
                    // 이미 들고 있는 것을 바닥에 놓기
                    Thing carriedThing = pawn.carryTracker.CarriedThing;
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing droppedThing);
                }

                if (material != null && material.Spawned)
                {
                    int countToTake = Math.Min(job.count, material.stackCount);
                    Thing takenThing = material.SplitOff(countToTake);

                    if (pawn.carryTracker.TryStartCarry(takenThing))
                    {
                        Log.Message($"[HussarPod] Successfully picked up {countToTake} {takenThing.def.label}");
                    }
                    else
                    {
                        Log.Error($"[HussarPod] Failed to start carrying {takenThing.def.label}");
                        GenPlace.TryPlaceThing(takenThing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }
                }
            };
            pickupMaterial.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickupMaterial;

            // 포드로 이동
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 재료 넣기
            Toil supplyMaterial = new Toil();
            supplyMaterial.initAction = () =>
            {
                var material = pawn.carryTracker.CarriedThing;
                var pod = Pod;

                if (material != null && pod != null)
                {
                    bool success = false;
                    int amount = material.stackCount;
                    string materialName = "";

                    // 재료 타입별 처리
                    if (material.def == ThingDefOf.GoJuice)
                    {
                        int canAccept = Mathf.Min(amount, pod.MaxStoredGoJuice - pod.StoredGoJuice);
                        if (canAccept > 0)
                        {
                            success = pod.TryAddGoJuice(canAccept);
                            if (success)
                            {
                                materialName = "Go-Juice";
                                if (canAccept < amount)
                                {
                                    // 일부만 사용하고 나머지는 반환
                                    Thing remainder = material.SplitOff(canAccept);
                                    remainder.Destroy();
                                }
                                else
                                {
                                    material.Destroy();
                                }
                            }
                        }
                    }
                    else if (material.def.IsMedicine)
                    {
                        int canAccept = Mathf.Min(amount, pod.MaxStoredMedicine - pod.StoredMedicine);
                        if (canAccept > 0)
                        {
                            success = pod.TryAddMedicine(canAccept);
                            if (success)
                            {
                                materialName = "Medicine";
                                if (canAccept < amount)
                                {
                                    Thing remainder = material.SplitOff(canAccept);
                                    remainder.Destroy();
                                }
                                else
                                {
                                    material.Destroy();
                                }
                            }
                        }
                    }
                    else if (material.def == ThingDefOf.ComponentIndustrial)
                    {
                        int canAccept = Mathf.Min(amount, pod.MaxStoredComponents - pod.StoredComponents);
                        if (canAccept > 0)
                        {
                            success = pod.TryAddComponents(canAccept);
                            if (success)
                            {
                                materialName = "Components";
                                if (canAccept < amount)
                                {
                                    Thing remainder = material.SplitOff(canAccept);
                                    remainder.Destroy();
                                }
                                else
                                {
                                    material.Destroy();
                                }
                            }
                        }
                    }

                    if (success)
                    {
                        Messages.Message($"Added {materialName} to hussar pod",
                            pod, MessageTypeDefOf.TaskCompletion);
                    }
                    else
                    {
                        Messages.Message($"Could not add {materialName} - storage full",
                            pod, MessageTypeDefOf.RejectInput);

                        // 실패 시 재료를 바닥에 떨어뜨리기
                        GenPlace.TryPlaceThing(material, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }
                }
            };
            supplyMaterial.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return supplyMaterial;
        }
    }
}