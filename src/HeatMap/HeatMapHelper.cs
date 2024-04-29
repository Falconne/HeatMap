using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public static class HeatMapHelper
    {
        internal static IntRange MappedTemperatureRange;
        internal static Color[] MappedColors;

        public static void RegenerateColorMap()
        {
            if (HeatMap.Instance.ShouldUseCustomRange)
                CreateCustomMap();
            else
                CreateComfortMap();
        }

        private static void CreateCustomMap()
        {
            MappedTemperatureRange = new IntRange(
                HeatMap.Instance.CustomRangeMin, HeatMap.Instance.CustomRangeMax);

            var mappedColorCount = MappedTemperatureRange.max - MappedTemperatureRange.min;
            MappedColors = new Color[mappedColorCount];

            var delta = 2f / (mappedColorCount - 1);
            var channelR = -1f;
            var channelG = 0f;
            var channelB = 1f;
            var greenRising = true;

            for (var i = 0; i < mappedColorCount - 1; i++)
            {
                var realR = Math.Min(channelR, 1f);
                realR = Math.Max(realR, 0f);

                var realG = Math.Min(channelG, 1f);
                realG = Math.Max(realG, 0f);

                var realB = Math.Min(channelB, 1f);
                realB = Math.Max(realB, 0f);

                MappedColors[i] = new Color(realR, realG, realB);

                if (channelG >= 1f)
                    greenRising = false;

                channelR += delta;
                channelG += greenRising ? delta : -delta;
                channelB -= delta;
            }

            // Force high end to be red (or else if the temperature range is an even number,
            // the green channel will not go down to zero in above loop).
            MappedColors[mappedColorCount - 1] = Color.red;
        }

        private static void CreateComfortMap()
        {
            var minComfortTemp = (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) + 3;
            var maxComfortTemp = (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) - 3;

            // Narrow down the green range to a quarter scale, to make boundary temps stand out more.

            var comfortDoubleRange = (maxComfortTemp - minComfortTemp) * 2;
            MappedTemperatureRange = new IntRange(
                minComfortTemp - comfortDoubleRange, maxComfortTemp + comfortDoubleRange);

            var mappedColorCount = MappedTemperatureRange.max - MappedTemperatureRange.min;
            MappedColors = new Color[mappedColorCount];

            var channelDelta = 1f / comfortDoubleRange;
            var channelR = -2f;
            var channelG = 0f;
            var channelB = 2f;
            var greenRising = true;

            var mappingTemperature = MappedTemperatureRange.min;
            for (var i = 0; i < mappedColorCount - 1; i++, mappingTemperature++)
            {
                var realR = Math.Min(channelR, 1f);
                realR = Math.Max(realR, 0f);

                var realG = Math.Min(channelG, 1f);
                realG = Math.Max(realG, 0f);

                var realB = Math.Min(channelB, 1f);
                realB = Math.Max(realB, 0f);

                MappedColors[i] = new Color(realR, realG, realB);

                if (channelG >= 2f)
                    greenRising = false;

                var delta = channelDelta;
                if (mappingTemperature >= minComfortTemp - 1 &&
                    mappingTemperature <= maxComfortTemp)
                {
                    delta *= 4;
                }

                channelR += delta;
                channelG += greenRising ? delta : -delta;
                channelB -= delta;
            }

            // Force high end to be red (or else if the temperature range is an even number,
            // the green channel will not go down to zero in above loop).
            MappedColors[mappedColorCount - 1] = Color.red;
        }

        public static int GetIndexForTemperature(float temperature)
        {
            var colorMapIndex = (int)temperature - MappedTemperatureRange.min;
            if (colorMapIndex < 0)
                colorMapIndex = 0;
            else if (colorMapIndex >= MappedColors.Length)
                colorMapIndex = MappedColors.Length - 1;
            return colorMapIndex;
        }
        public static Color GetColorForTemperature(float temperature)
        {
            return MappedColors[GetIndexForTemperature(temperature)];
        }
    }
}
