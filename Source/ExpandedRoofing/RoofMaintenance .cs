using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Harmony;

namespace ExpandedRoofing
{
    [StaticConstructorOnStartup]
    class RoofMaintenance_Patches
    {
        static RoofMaintenance_Patches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.expandedroofing.roofmaintenance");

            harmony.Patch(AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.SetRoof)), null, null, new HarmonyMethod(typeof(RoofMaintenance_Patches), nameof(SetRoofTranspiler)));
        }

        public static IEnumerable<CodeInstruction> SetRoofTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo FI_RoofGrid_roofGrid = AccessTools.Field(typeof(RoofGrid), "roofGrid"); // TODO: look at centralizing?
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i;
            for (i = 0; i < instructionsList.Count - 1; i++)
            {
                if (instructionsList[i].opcode == OpCodes.Bne_Un)
                {
                    yield return instructionsList[i++];
                    yield return instructionsList[i++];
                    break;
                }
                else
                    yield return instructionsList[i];
            }

            // infix helper call before assignment (allows us to see previous value)
            yield return instructionsList[i++]; // IL_0038: ldarg.0 (label destination)
            yield return new CodeInstruction(OpCodes.Ldfld, HarmonyPatches.FI_RoofGrid_map);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldarg_2);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RoofMaintenance_Patches), nameof(SetRoofHelper)));
            yield return new CodeInstruction(OpCodes.Ldarg_0);

            for (; i < instructionsList.Count - 1; i++)
                yield return instructionsList[i];

            yield return instructionsList[i];
        }

        // handle adding thick roofs to tracker.
        public static void SetRoofHelper(Map map, IntVec3 c, RoofDef def)
        {
            if (def.IsBuildableThickRoof())
                map.GetComponent<RoofMaintenance_MapComponenent>()?.AddMaintainableRoof(c);
            else
            {
                RoofDef prevRoof = map.roofGrid.RoofAt(c);
                if (prevRoof.IsBuildableThickRoof())
                    map.GetComponent<RoofMaintenance_MapComponenent>()?.RemoveMaintainableRoof(c);
            }
        }
    }

    public class RoofMaintenance_MapComponenent : MapComponent
    {
        private RoofMaintenanceGrid roofMaintenanceGrid;

        public RoofMaintenance_MapComponenent(Map map) : base(map)
        {
            this.roofMaintenanceGrid = new RoofMaintenanceGrid(map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.roofMaintenanceGrid, "roofMaintenanceGrid", new object[] { this.map });

            // Handles upgrading settings
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (this.roofMaintenanceGrid == null)
                    this.roofMaintenanceGrid = new RoofMaintenanceGrid(this.map);
            }
        }

        public override void MapComponentTick() => roofMaintenanceGrid.Tick();

        public void AddMaintainableRoof(IntVec3 c) => this.roofMaintenanceGrid.Add(c);
        public void RemoveMaintainableRoof(IntVec3 c) => this.roofMaintenanceGrid.Remove(c);
        public void DoMaintenance(IntVec3 c) => this.roofMaintenanceGrid.Reset(c);

        public IEnumerable<IntVec3> MaintenanceRequired
        {
            get => this.roofMaintenanceGrid.CurrentlyRequireMaintenance;
        }

    }

    internal sealed class RoofMaintenanceGrid : IExposable
    {
        const int long_TickInterval = 2000;
        const int minTicksBeforeMaintenance = 5000;
        const int minTicksBeforeMTBCollapses = 7500;
        private Map map;

        // NOTE: int => Cell index, ushort => time to breakdown
        // TODO: consider SortedList?
        Dictionary<int, int> grid = new Dictionary<int, int>();

        public RoofMaintenanceGrid(Map map) => this.map = map;

        public void ExposeData()
        {
            Scribe_Collections.Look<int, int>(ref this.grid, "grid");

            // TODO: can other update changes be moved here
            // Handles upgrading settings
            if (Scribe.mode == LoadSaveMode.LoadingVars && this.grid == null)
            {
                this.grid = new Dictionary<int, int>();
                this.InitExistingMap();
            }
        }

        private void InitExistingMap()
        {
            Log.Message("ExpandedRoofing: Need to initialize RoofMaintenanceGrid.");
            foreach (IntVec3 cell in this.map.AllCells)
            {
                RoofDef roofDef = this.map.roofGrid.RoofAt(cell);
                if (roofDef?.IsBuildableThickRoof() == true)
                    this.Add(cell);
            }
        }

        public void Add(IntVec3 c)
        {
            int ind = map.cellIndices.CellToIndex(c);
            if (!this.grid.ContainsKey(ind))
                this.grid.Add(ind, 0);
            else
                this.Reset(c);
        }
        public void Remove(IntVec3 c) => this.grid.Remove(map.cellIndices.CellToIndex(c));
        public void Reset(IntVec3 c) => this.grid[map.cellIndices.CellToIndex(c)] = 0;
        public int GetValue(IntVec3 c) => this.grid[map.cellIndices.CellToIndex(c)];

        private IntVec3 GetIntVec3(int index) => this.map.cellIndices.IndexToCell(index);

        public void Tick()
        {
            // bucketing? kind of?
            this.grid.Where(kp => Find.TickManager.TicksGame + kp.Key.HashOffset() % long_TickInterval == 0).Do( kp =>
            {
                this.grid[kp.Key] += 1;
                if (this.grid[kp.Key] > minTicksBeforeMTBCollapses && Rand.MTBEventOccurs(3.5f, 60000f, long_TickInterval))
                    this.map.roofCollapseBuffer.MarkToCollapse(this.GetIntVec3(kp.Key));
            });
        }

        public IEnumerable<IntVec3> CurrentlyRequireMaintenance
        {
            get
            {
                foreach (KeyValuePair<int, int> pair in this.grid)
                    if (pair.Value > minTicksBeforeMaintenance)
                        yield return this.GetIntVec3(pair.Key);
            }
        }

    }

}
