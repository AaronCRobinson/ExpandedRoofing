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

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c)
        {
            bool result;
            if (c.IsForbidden(pawn))
                result = false;
            else
            {
                LocalTargetInfo target = c;
                ReservationLayerDef ceiling = ReservationLayerDefOf.Ceiling;
                result = pawn.CanReserve(target, 1, -1, ceiling, false) && pawn.CanReach(c, PathEndMode.Touch, pawn.NormalMaxDanger(), false, TraverseMode.ByPawn) && RoofCollapseUtility.WithinRangeOfRoofHolder(c, pawn.Map) && RoofCollapseUtility.ConnectedToRoofHolder(c, pawn.Map, true);
            }
            return result;
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c) => new Job(JobDefOf.PerformRoofMaintenance, c);

    }

}