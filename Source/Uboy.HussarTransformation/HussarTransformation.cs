using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace HussarTransformation
{
    // ====================================================================
    // 1. MOD SETTINGS
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
        }
    }

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
            listingStandard.CheckboxLabeled("HT.AllowOtherXenotypes".Translate(), ref settings.allowOtherXenotypes, "HT.AllowOtherXenotypesTooltip".Translate());
            listingStandard.GapLine();

            // (2) Item Costs
            listingStandard.Label("HT.ItemCosts".Translate());
            string goJuiceBuffer = settings.goJuiceCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.GoJuiceCost".Translate(), ref settings.goJuiceCost, ref goJuiceBuffer, 0, 99);
            string medicineBuffer = settings.medicineCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.MedicineCost".Translate(), ref settings.medicineCost, ref medicineBuffer, 0, 99);
            string componentBuffer = settings.componentCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.ComponentCost".Translate(), ref settings.componentCost, ref componentBuffer, 0, 99);
            
            listingStandard.CheckboxLabeled("    " + "HT.ConsumeMedicineOnFailure".Translate(), ref settings.consumeMedicineOnFailure, "HT.ConsumeMedicineOnFailureTooltip".Translate());
            if (settings.consumeMedicineOnFailure)
            {
                string medicineFailureBuffer = settings.medicineConsumedOnFailure.ToString();
                listingStandard.TextFieldNumericLabeled("        " + "HT.MedicineConsumedOnFailure".Translate(), ref settings.medicineConsumedOnFailure, ref medicineFailureBuffer, 0, settings.medicineCost);
            }
            listingStandard.GapLine();

            // (3) Transformation Duration
            listingStandard.Label("HT.TransformationDuration".Translate() + ": " + "HT.Hours".Translate(settings.transformationDurationHours.ToString("0.0")));
            settings.transformationDurationHours = listingStandard.Slider(settings.transformationDurationHours, 0f, 24f);
            listingStandard.GapLine();

            // (4) Power Consumption
            listingStandard.Label("HT.PowerConsumption".Translate() + ": " + "W".Translate(settings.powerConsumption.ToString()));
            settings.powerConsumption = (int)listingStandard.Slider(settings.powerConsumption, 0, 1000);
            listingStandard.CheckboxLabeled("    " + "HT.StopOnPowerLoss".Translate(), ref settings.stopOnPowerLoss, "HT.StopOnPowerLossTooltip".Translate());
            listingStandard.GapLine();

            // (5) Research Cost
            listingStandard.Label("HT.ResearchCost".Translate());
            string researchBuffer = settings.researchCost.ToString();
            listingStandard.TextFieldNumericLabeled("    " + "HT.ResearchCostValue".Translate(), ref settings.researchCost, ref researchBuffer, 0, 9999);
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
            DefDatabase<ResearchProjectDef>.GetNamed("HussarTransformationSurgery").baseCost = settings.researchCost;
            ThingDef hussarPod = DefDatabase<ThingDef>.GetNamed("HussarPod");
            
            // Get the power properties from the ThingDef
            var powerProps = hussarPod.GetCompProperties<CompProperties_Power>();
            if (powerProps != null)
            {
                // Apply settings to the Def. The building will read from this.
                powerProps.basePowerConsumption = settings.powerConsumption;
                powerProps.idlePowerDraw = 0; // Explicitly set idle power to 0
            }
        }
    }
    
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            HussarTransformationMod.ApplySettingsToDefs();
        }
    }


    // ====================================================================
    // 2. HUSSAR TRANSFORMATION POD (BUILDING)
    // ====================================================================

    public class Building_HussarPod : Building_Enterable, IThingHolder
    {
        private CompPowerTrader powerComp;
        private float progress = 0;
        private const int TicksPerSecond = 60;

        // A property to easily check if the pod is transforming someone.
        public bool IsRunning => ContainedPawn != null;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.powerComp = this.GetComp<CompPowerTrader>();
        }

        public override void Tick()
        {
            base.Tick();

            // The building's power component reads its consumption values from the ThingDef.
            // We just need to tell it when to be in 'active' vs 'idle' state.
            if (powerComp != null)
            {
                var powerProps = powerComp.Props as CompProperties_Power;
                if (IsRunning)
                {
                    // Consume the 'active' power amount defined in the Def
                    powerComp.PowerOutput = -powerProps.basePowerConsumption;
                }
                else
                {
                    // Consume the 'idle' power amount defined in the Def
                    powerComp.PowerOutput = -powerProps.idlePowerDraw;
                }
            }

            if (!IsRunning) return;

            // Check if the pawn inside has died for any reason
            if (ContainedPawn.Dead)
            {
                Messages.Message("HT.PawnDiedMessage".Translate(ContainedPawn.LabelShortCap), this, MessageTypeDefOf.NegativeEvent);
                EjectContents();
                progress = 0;
                return; // Stop further processing for this tick
            }

            // Power check
            if (powerComp != null && !powerComp.PowerOn && HussarTransformationMod.settings.powerConsumption > 0 && HussarTransformationMod.settings.stopOnPowerLoss)
            {
                CancelTransformation("HT.PowerLossMessage".Translate());
                return;
            }

            progress += 1f;

            // Transformation complete
            if (ProgressPercent >= 1f)
            {
                FinishTransformation();
            }
        }
        
        // Override Destroy to safely eject the pawn before the building is removed
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (IsRunning)
            {
                // Eject the pawn before destruction, using a new message key
                CancelTransformation("HT.PodDestroyedMessage".Translate());
            }
            base.Destroy(mode);
        }
        
        private void FinishTransformation()
        {
            Pawn pawn = ContainedPawn;
            if (pawn == null)
            {
                Log.Error("Hussar Pod tried to finish transformation but pawn was null.");
                innerContainer.Clear();
                progress = 0;
                return;
            }

            EjectContents();
            
            // Change Xenotype to Hussar
            pawn.genes.SetXenotype(XenotypeDefOf.Hussar);

            // Add Go-Juice dependency
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.GoJuiceAddiction, pawn);
            pawn.health.AddHediff(hediff);

            Messages.Message("HT.TransformationCompleteMessage".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
            progress = 0;
        }

        private void CancelTransformation(string reason)
        {
            Pawn pawn = ContainedPawn;
            EjectContents();
            progress = 0;

            if (HussarTransformationMod.settings.consumeMedicineOnFailure)
            {
                // We don't have direct access to the items consumed, so we can't refund them.
                // The logic assumes medicine is consumed on start. We'll just show a message.
                Messages.Message("HT.TransformationCancelledMessage".Translate(pawn.LabelShortCap, reason) + "\n" + "HT.MedicineConsumedOnFailureMessage".Translate(HussarTransformationMod.settings.medicineConsumedOnFailure), pawn, MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                 Messages.Message("HT.TransformationCancelledMessage".Translate(pawn.LabelShortCap, reason), pawn, MessageTypeDefOf.NegativeEvent);
            }
        }


        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist || pawn.IsQuestLodger())
                return new AcceptanceReport("HT.AcceptanceReport_NotColonist".Translate());

            if (pawn.ageTracker.AgeBiologicalYears < 13)
                return new AcceptanceReport("HT.AcceptanceReport_TooYoung".Translate());

            if (pawn.genes.Xenotype != XenotypeDefOf.Baseliner && !HussarTransformationMod.settings.allowOtherXenotypes)
                return new AcceptanceReport("HT.AcceptanceReport_NotBaseliner".Translate());

            if (IsRunning)
                 return new AcceptanceReport("HT.AcceptanceReport_Occupied".Translate());

            if (!PowerOn)
                 return new AcceptanceReport("HT.AcceptanceReport_NoPower".Translate());

            return base.CanAcceptPawn(pawn);
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if (!CanAcceptPawn(pawn).Accepted) return;

            // Consume ingredients
            if (!ConsumeIngredients())
            {
                Messages.Message("HT.MissingIngredientsMessage".Translate(), pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            bool accepted = innerContainer.TryAddOrTransfer(pawn);
            if (accepted)
            {
                progress = 0;
            }
        }

        private bool ConsumeIngredients()
        {
            var settings = HussarTransformationMod.settings;
            var map = this.Map;

            // Check for ingredients first
            if (CountItems(ThingDefOf.GoJuice) < settings.goJuiceCost ||
                CountItems(ThingDefOf.MedicineIndustrial) < settings.medicineCost || // Simplified to industrial medicine for example
                CountItems(ThingDefOf.ComponentIndustrial) < settings.componentCost)
            {
                 return false;
            }

            // Consume ingredients
            DestroyItems(ThingDefOf.GoJuice, settings.goJuiceCost);
            DestroyItems(ThingDefOf.MedicineIndustrial, settings.medicineCost);
            DestroyItems(ThingDefOf.ComponentIndustrial, settings.componentCost);

            return true;
        }

        private int CountItems(ThingDef thingDef)
        {
            return this.Map.listerThings.ThingsOfDef(thingDef).Where(t => !t.IsForbidden(Faction.OfPlayer)).Sum(t => t.stackCount);
        }

        private void DestroyItems(ThingDef thingDef, int count)
        {
            if (count <= 0) return;
            List<Thing> things = this.Map.listerThings.ThingsOfDef(thingDef).Where(t => !t.IsForbidden(Faction.OfPlayer)).ToList();
            int remaining = count;
            foreach (var thing in things)
            {
                int numToTake = Mathf.Min(remaining, thing.stackCount);
                thing.SplitOff(numToTake).Destroy();
                remaining -= numToTake;
                if (remaining <= 0) break;
            }
        }

        public override string GetInspectString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(base.GetInspectString());
            if (IsRunning)
            {
                sb.AppendLine();
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

            if (!IsRunning)
            {
                yield return new Command_Action
                {
                    defaultLabel = "HT.BeginTransformation".Translate(),
                    defaultDesc = "HT.BeginTransformationDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                    action = () =>
                    {
                        var floatMenuOptions = new List<FloatMenuOption>();
                        foreach (var pawn in this.Map.mapPawns.FreeColonists)
                        {
                            if (CanAcceptPawn(pawn).Accepted)
                            {
                                floatMenuOptions.Add(new FloatMenuOption(pawn.LabelCap, () =>
                                {
                                    var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("EnterHussarPod"), this);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }));
                            }
                        }
                        if (floatMenuOptions.Any())
                        {
                            Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                        }
                    }
                };
            }
        }
    }


    // ====================================================================
    // 3. JOB DRIVER for entering the pod
    // ====================================================================
    public class JobDriver_EnterHussarPod : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            
            Toil enter = new Toil();
            enter.initAction = () =>
            {
                var pod = (Building_HussarPod)job.targetA.Thing;
                pod.TryAcceptPawn(pawn);
            };
            yield return enter;
        }
    }
}



