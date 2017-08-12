using UnityEngine;
using Verse;
using RimWorld;

namespace ExpandedRoofing
{
    public class Designator_BuildTransparentRoof : Designator_BuildCustomRoof
    {
        public Designator_BuildTransparentRoof() : base(ThingDefOf.RoofTransparentFraming, RoofDefOf.RoofTransparent) { }
    }

    public class Designator_BuildSolarRoof : Designator_BuildCustomRoof
    {
        public Designator_BuildSolarRoof() : base(ThingDefOf.RoofSolarFraming, RoofDefOf.RoofSolar) { }
    }

    public class Designator_BuildCustomRoof : Designator_Build
    {
        protected RoofDef roofDef;

        public Designator_BuildCustomRoof(BuildableDef entDef, RoofDef rDef) : base(entDef)
        {
            //this.icon = roof.uiIcon;
            //bool flag = this.icon == null;
            //if (flag)
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/BuildRoofAreaExpand", true);
            this.roofDef = rDef;
        }

        public override BuildableDef PlacingDef
        {
            get
            {
                return this.entDef;
            }
        }

        // TODO: need to figure a better way to handle support...
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!RoofCollapseUtility.WithinRangeOfRoofHolder(loc, base.Map)) return false;
            RoofDef rDef = base.Map.roofGrid.RoofAt(loc);
            if (rDef == null) return true;
            // assuming thick roofs are always natural too..
            if (!rDef.isThickRoof && rDef != this.roofDef) return true;
            return false;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(new EventPack(base.TutorTagDesignate, c)))
            {
                return;
            }
            if (DebugSettings.godMode)
            {
                if (this.entDef is TerrainDef)
                {
                    base.Map.terrainGrid.SetTerrain(c, (TerrainDef)this.entDef);
                }
                else
                {
                    Thing thing = ThingMaker.MakeThing((ThingDef)this.entDef, null);
                    thing.SetFactionDirect(Faction.OfPlayer);
                    GenSpawn.Spawn(thing, c, base.Map, this.placingRot, false);
                }
            }
            else
            {
                //GenSpawn.WipeExistingThings(c, this.placingRot, this.entDef.blueprintDef, base.Map, DestroyMode.Deconstruct);
                GenConstruct.PlaceBlueprintForBuild(this.entDef, c, base.Map, this.placingRot, Faction.OfPlayer, null);
            }
            MoteMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, this.placingRot, this.entDef.Size), base.Map);
            if (this.entDef is ThingDef && (this.entDef as ThingDef).IsOrbitalTradeBeacon)
            {
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BuildOrbitalTradeBeacon, KnowledgeAmount.Total);
            }
            if (TutorSystem.TutorialMode)
            {
                TutorSystem.Notify_Event(new EventPack(base.TutorTagDesignate, c));
            }
            if (this.entDef.PlaceWorkers != null)
            {
                for (int i = 0; i < this.entDef.PlaceWorkers.Count; i++)
                {
                    this.entDef.PlaceWorkers[i].PostPlace(base.Map, this.entDef, c, this.placingRot);
                }
            }
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
    }
}
