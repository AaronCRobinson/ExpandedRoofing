using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ExpandedRoofing
{
    public class JobDriver_PerformRoofMaintenance : JobDriver
    {
        private const float MaintenanceTicks = 80f;

        protected float ticksToNextMaintenance = 0f;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil maintenance = new Toil()
            {
                initAction = () => this.ticksToNextMaintenance = MaintenanceTicks,
            };
            // NOTE: unlike repair, this only counts down once 
            maintenance.tickAction = delegate
            {
                Pawn actor = maintenance.actor;
                actor.skills.Learn(SkillDefOf.Construction, 0.2f, false); // NOTE: reduced
                float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                this.ticksToNextMaintenance -= statValue;
                Log.Message($"{this.ticksToNextMaintenance}");
                if (this.ticksToNextMaintenance <= 0f)
                {
                    pawn.Map.GetComponent<RoofMaintenance_MapComponenent>().DoMaintenance(TargetA.Cell);
                    // TODO: make unique RecordDef
                    actor.records.Increment(RecordDefOf.ThingsRepaired);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                }
            };
            maintenance.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            maintenance.WithEffect(EffecterDefOf.RoofWork, TargetIndex.A);
            maintenance.defaultCompleteMode = ToilCompleteMode.Never;
            yield return maintenance;
        }

        //protected override bool DoWorkFailOn()
    }
}
