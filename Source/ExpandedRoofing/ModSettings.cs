using System;
using Verse;
using UnityEngine;
using ModSettingsHelper;

namespace ExpandedRoofing
{
    public class ExpandedRoofingSettings : ModSettings
    {
        private const float maxOutput_default = 2500f;
        private const float wattagePerSolarPanel_default = 200f;
        public float solarController_maxOutput = maxOutput_default;
        public float solarController_wattagePerSolarPanel = wattagePerSolarPanel_default;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.solarController_maxOutput, "solarController_maxOutput", maxOutput_default);
            Scribe_Values.Look(ref this.solarController_wattagePerSolarPanel, "solarController_wattagePerSolarPanel", wattagePerSolarPanel_default);
        }
    }

    class ExpandedRoofingMod : Mod
    {
        public static ExpandedRoofingSettings settings;
        private string solarController_maxOutput_buffer;
        private string solarController_wattagePerSolarPanel_buffer;

        public ExpandedRoofingMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ExpandedRoofingSettings>();
        }

        public override string SettingsCategory() => "ER_ExpandedRoofing".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModWindowHelper.Reset();
            ModWindowHelper.MakeTextFieldNumericLabeled<float>(inRect, "ER_MaxOutputLabel".Translate(), ref settings.solarController_maxOutput, ref this.solarController_maxOutput_buffer);
            ModWindowHelper.MakeTextFieldNumericLabeled<float>(inRect, "ER_WattagePerSolarPanelLabel".Translate(), ref settings.solarController_wattagePerSolarPanel, ref this.solarController_wattagePerSolarPanel_buffer);
            settings.Write();
        }

    }
}