using System.Linq;
using Verse;
using UnityEngine;
using SettingsHelper;
using System.Collections.Generic;
using System;

namespace ExpandedRoofing
{
    public class ExpandedRoofingSettings : ModSettings
    {
        private const float maxOutput_default = 2500f;
        private const float wattagePerSolarPanel_default = 200f;
        public float solarController_maxOutput = maxOutput_default;
        public float solarController_wattagePerSolarPanel = wattagePerSolarPanel_default;
        public bool glassLights = true;
        public bool roofMaintenance = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.solarController_maxOutput, "solarController_maxOutput", maxOutput_default);
            Scribe_Values.Look(ref this.solarController_wattagePerSolarPanel, "solarController_wattagePerSolarPanel", wattagePerSolarPanel_default);
            Scribe_Values.Look(ref this.glassLights, "glassLights", true);
            Scribe_Values.Look(ref this.roofMaintenance, "roofMaintenance", true);
        }
    }

    class ExpandedRoofingMod : Mod
    {
        public static ExpandedRoofingSettings settings;

        // Used to detect 
        private const string dontTemptMe_ModName = "Don't Tempt Me!";
        private const string glassLights_ModName = "Glass+Lights";

        private bool dontTemptMe = false;
        private static bool glassLights = false;

        public static bool GlassLights { get => glassLights; set => glassLights = value; }

        public ExpandedRoofingMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ExpandedRoofingSettings>();

            Dictionary<string, Action> modCheck = new Dictionary<string, Action>() {
                { dontTemptMe_ModName, () => { dontTemptMe = true; } },
                { glassLights_ModName, () => { glassLights = true; } }
            };

            //this.dontTemptMe = ModLister.AllInstalledMods.FirstOrDefault(m => m.Name == dontTemptMe_ModName)?.Active ?? false;

            foreach (ModMetaData metaData in ModLister.AllInstalledMods)
                if ( metaData.Active && modCheck.ContainsKey(metaData.Name) )
                    modCheck[metaData.Name]();
        }

        public override string SettingsCategory() => "ER_ExpandedRoofing".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Current.ProgramState == ProgramState.Entry
            // NOTE: this disables the mod settings when "Don't Tempt Me!" is installed.
            if (!this.dontTemptMe)
            {
                Listing_Standard listing_Standard = new Listing_Standard();
                listing_Standard.Begin(inRect);
                listing_Standard.AddLabeledNumericalTextField<float>("ER_MaxOutputLabel".Translate(), ref settings.solarController_maxOutput);
                listing_Standard.AddLabeledNumericalTextField<float>("ER_WattagePerSolarPanelLabel".Translate(), ref settings.solarController_wattagePerSolarPanel);
                listing_Standard.AddLabeledCheckbox("ER_GlassLights".Translate(), ref settings.glassLights);
                listing_Standard.AddLabeledCheckbox("ER_RoofMaintenance".Translate(), ref settings.roofMaintenance);
                listing_Standard.End();
                settings.Write();
            }
        }

    }
}