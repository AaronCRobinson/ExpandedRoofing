using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

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

    public class Designator_BuildThickStoneRoof : Designator_BuildCustomRoof
    {
        public Designator_BuildThickStoneRoof() : base(ThingDefOf.ThickStoneRoofFraming, RoofDefOf.ThickStoneRoof) { }
    }

    public class Designator_BuildCustomRoof : Designator_Build
    {
        private MethodInfo MI_SetBuildingToReinstall = AccessTools.Method(typeof(Blueprint_Install), "SetBuildingToReinstall");
        private MethodInfo MI_SetThingToInstallFromMinified = AccessTools.Method(typeof(Blueprint_Install), "SetThingToInstallFromMinified");
        private FieldInfo FI_stuffDef = AccessTools.Field(typeof(Designator_Build), "stuffDef");
        protected RoofDef roofDef;

        public Designator_BuildCustomRoof(BuildableDef entDef, RoofDef rDef) : base(entDef)
        {
            this.roofDef = rDef;
        }

        //public override string Label => this.roofDef.label;

        public override BuildableDef PlacingDef { get => this.entDef; }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (loc.GetFirstThing(base.Map, this.entDef.blueprintDef) != null) return false;

            RoofDef rDef = base.Map.roofGrid.RoofAt(loc);
            if (rDef == null) return true;
            
            if ((!rDef.isThickRoof || ResearchProjectDefOf.ThickStoneRoofRemoval.IsFinished) && rDef != this.roofDef) return true;
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
                    ThingDef stuff = null;
                    var val = FI_stuffDef.GetValue(this);
                    if (val != null) stuff = (ThingDef)val;
                    Thing thing = ThingMaker.MakeThing((ThingDef)this.entDef, stuff);
                    thing.SetFactionDirect(Faction.OfPlayer);
                    GenSpawn.Spawn(thing, c, base.Map, this.placingRot);
                }
            }
            else
            {
                //GenSpawn.WipeExistingThings(c, this.placingRot, this.entDef.blueprintDef, base.Map, DestroyMode.Deconstruct);
                base.Map.areaManager.NoRoof[c] = false;
                ThingDef stuff = null;
                var val = FI_stuffDef.GetValue(this);
                if (val != null) stuff = (ThingDef)val;
                GenConstruct.PlaceBlueprintForBuild(this.entDef, c, base.Map, this.placingRot, Faction.OfPlayer, stuff);
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

    public class Designator_AreaNoThickRoof : Designator_AreaNoRoof
    {
        public Designator_AreaNoThickRoof() : base()
        {
            this.defaultLabel = "DesignatorAreaNoThickRoof".Translate();
            this.defaultDesc = "DesignatorAreaNoThickRoofDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/NoRoofArea", true);
            //this.hotKey = KeyBindingDefOf.Misc5;
            this.soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            this.soundDragChanged = null;
            this.soundSucceeded = SoundDefOf.Designate_ZoneAdd;
            this.useMouseIcon = true;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
                return false;
            if (c.Fogged(base.Map))
                return false;
            bool flag = base.Map.areaManager.NoRoof[c];
            /*if (this.mode == DesignateMode.Add)
                return !flag;*/
            return !flag;
        }

        public override bool Visible
        {
            get
            {
                if (DebugSettings.godMode) return true;
                return ResearchProjectDefOf.ThickStoneRoofRemoval.IsFinished;
            }
        }
    }

}
