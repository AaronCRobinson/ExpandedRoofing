﻿using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace SettingsHelper
{
    // REFERENCE: https://github.com/erdelf/GodsOfRimworld/blob/master/Source/Ankh/ModControl.cs
    // REFERENCE: https://github.com/erdelf/PrisonerRansom/
    public static class ListingStandardHelper
    {
        private static float gap = 12f;
        private static float lineGap = 3f;

        public static float Gap { get => gap; set => gap = value; }
        public static float LineGap { get => lineGap; set => lineGap = value; }

        //NOTE: are these Text.Anchor necessary??

        public static void AddHorizontalLine(this Listing_Standard listing_Standard, float? gap = null)
        {
            listing_Standard.Gap(gap ?? lineGap);
            listing_Standard.GapLine(gap ?? lineGap);
        }

        public static void AddLabelLine(this Listing_Standard listing_Standard, string label, float? height = null)
        {
            listing_Standard.Gap(Gap);
            Rect lineRect = listing_Standard.GetRect(height);

            // TODO: tooltips
            //Widgets.DrawHighlightIfMouseover(lineRect);
            //TooltipHandler.TipRegion(lineRect, "TODO: TIP GOES HERE");

            Widgets.Label(lineRect, label);
        }

        private static Rect GetRect(this Listing_Standard listing_Standard,  float? height = null)
        {
            return listing_Standard.GetRect(height ?? Text.LineHeight);
        }

        private static Rect LineRectSpilter(this Listing_Standard listing_Standard, out Rect leftHalf, float? height = null)
        {
            Rect lineRect = listing_Standard.GetRect(height);
            leftHalf = lineRect.LeftHalf().Rounded();
            return lineRect;
        }

        private static Rect LineRectSpilter(this Listing_Standard listing_Standard, out Rect leftHalf, out Rect rightHalf, float? height = null)
        {
            Rect lineRect = listing_Standard.LineRectSpilter(out leftHalf, height);

            rightHalf = lineRect.RightHalf().Rounded();

            return lineRect;
        }

        public static void AddLabeledRadioList(this Listing_Standard listing_Standard, string header, string[] labels, ref string val, float? headerHeight = null)
        {
            listing_Standard.Gap(Gap);
            listing_Standard.AddLabelLine(header, headerHeight);
            listing_Standard.AddRadioList<string>(GenerateLabeledRadioValues(labels), ref val);
        }

        private static void AddRadioList<T>(this Listing_Standard listing_Standard, List<LabeledRadioValue<T>> items, ref T val, float? height = null)
        {
            foreach (LabeledRadioValue<T> item in items)
            {
                listing_Standard.Gap(Gap);
                Rect lineRect = listing_Standard.GetRect(height);
                if (Widgets.RadioButtonLabeled(lineRect, item.Label, EqualityComparer<T>.Default.Equals(item.Value, val)))
                    val = item.Value;
            }
        }

        private static List<LabeledRadioValue<string>> GenerateLabeledRadioValues(string[] labels)
        {
            List<LabeledRadioValue<string>> list = new List<LabeledRadioValue<string>>();
            foreach (string label in labels)
            {
                list.Add(new LabeledRadioValue<string>(label, label));
            }
            return list;
        }

        // (label, value) => (key, value)
        /*private static List<LabeledRadioValue<T>> GenerateLabeledRadioValues<T>(Dictionary<string, T> dict)
        {
            List<LabeledRadioValue<T>> list = new List<LabeledRadioValue<T>>();
            foreach (KeyValuePair<string, T> entry in dict)
            {
                list.Add(new LabeledRadioValue<T>(entry.Key, entry.Value));
            }
            return list;
        }*/

        public class LabeledRadioValue<T>
        {
            private string label;
            private T val;

            public LabeledRadioValue(string label, T val)
            {
                Label = label;
                Value = val;
            }

            public string Label
            {
                get { return label; }
                set { label = value; }
            }

            public T Value
            {
                get { return val; }
                set { val = value; }
            }
        }

        public static void AddLabeledTextField(this Listing_Standard listing_Standard, string label, ref string settingsValue)
        {
            listing_Standard.Gap(Gap);
            listing_Standard.LineRectSpilter(out Rect leftHalf, out Rect rightHalf);

            // TODO: tooltips
            //Widgets.DrawHighlightIfMouseover(lineRect);
            //TooltipHandler.TipRegion(lineRect, "TODO: TIP GOES HERE");

            Widgets.Label(leftHalf, label);

            string buffer = settingsValue.ToString();
            settingsValue = Widgets.TextField(rightHalf, buffer);
        }

        public static void AddLabeledNumericalTextField<T>(this Listing_Standard listing_Standard, string label, ref T settingsValue) where T : struct
        {
            listing_Standard.Gap(Gap);
            listing_Standard.LineRectSpilter(out Rect leftHalf, out Rect rightHalf);

            // TODO: tooltips
            //Widgets.DrawHighlightIfMouseover(lineRect);
            //TooltipHandler.TipRegion(lineRect, "TODO: TIP GOES HERE");

            Widgets.Label(leftHalf, label);

            string buffer = settingsValue.ToString();
            Widgets.TextFieldNumeric<T>(rightHalf, ref settingsValue, ref buffer, 1f, 100000f);
        }

        public static void AddLabeledCheckbox(this Listing_Standard listing_Standard, string label, ref bool settingsValue)
        {
            listing_Standard.Gap(Gap);
            listing_Standard.CheckboxLabeled(label, ref settingsValue);
        }

        public static void AddLabeledSlider(this Listing_Standard listing_Standard, string label, ref float value, float leftValue, float rightValue)
        {
            listing_Standard.Gap(Gap);
            listing_Standard.LineRectSpilter(out Rect leftHalf, out Rect rightHalf);

            Widgets.Label(leftHalf, label);

            float bufferVal = value;
            // NOTE: this BottomPart will probably need some reworking if the height of rect is greater than a line
            value = Widgets.HorizontalSlider(rightHalf.BottomPart(0.70f), bufferVal, leftValue, rightValue);
        }
    }
}