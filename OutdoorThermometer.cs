using UnityEngine;
using Verse;

namespace HeatMap
{
    public class OutdoorThermometer
    {
        public void Update(HeatMap heatMap, float opacity)
        {
            if (heatMap == null)
                return;

            const float boxSize = 62f;
            var horizontalOffset = 8f;
            if (Prefs.AdaptiveTrainingEnabled)
                horizontalOffset += 216f;

            var outRect = new Rect(UI.screenWidth - horizontalOffset - boxSize, 8f, boxSize, boxSize);
            var temperature = Find.VisibleMap.mapTemperature.OutdoorTemp;
            var backColor = heatMap.GetColorForTemperature(temperature);
            backColor.a = opacity;
            GUI.DrawTexture(outRect, SolidColorMaterials.NewSolidColorTexture(backColor));
            GUI.DrawTexture(outRect, Resources.DisplayBoder);

            var temperatureForDisplay = temperature.ToStringTemperature("F0");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(outRect, temperatureForDisplay);
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}