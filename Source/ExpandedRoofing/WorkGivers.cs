using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ExpandedRoofing
{
    public class WorkGiver_PerformRoofMaintenance : WorkGiver_Scanner
    {
        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            return pawn.Map.GetComponent<RoofMaintenance_MapComponenent>()?.MaintenanceRequired;
        }

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            if (c.IsForbidden(pawn))
                return false;
            if (!pawn.CanReserve((LocalTargetInfo)c, 1, -1, ReservationLayerDefOf.Ceiling, false))
                return false;
            if (!pawn.CanReach(c, PathEndMode.Touch, pawn.NormalMaxDanger(), false, TraverseMode.ByPawn))
                return false;
            if (!pawn.Map.roofGrid.RoofAt(c).IsBuildableThickRoof())
                return false;
            return pawn.Map.GetComponent<RoofMaintenance_MapComponenent>()?.MaintenanceNeeded(c) == true;
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false) => new Job(JobDefOf.PerformRoofMaintenance, c);

    }

}