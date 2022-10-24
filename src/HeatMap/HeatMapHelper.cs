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
			var minComfortTemp = HeatMap.Instance.CustomRangeComfortableMin;
			var maxComfortTemp = HeatMap.Instance.CustomRangeComfortableMax;

			MappedTemperatureRange = new IntRange(HeatMap.Instance.CustomRangeMin, HeatMap.Instance.CustomRangeMax);
			MappedColors = CreateColorGradient(MappedTemperatureRange, minComfortTemp, maxComfortTemp);
		}

		private static void CreateComfortMap()
		{
			(var mappedRange, var minComfortTemp, var maxComfortTemp) = GetComfortTemperatureRanges();

			MappedTemperatureRange = mappedRange;
			MappedColors = CreateColorGradient(MappedTemperatureRange, minComfortTemp, maxComfortTemp);
		}

		private static Color[] CreateColorGradient(IntRange range, int minComfortTemp, int maxComfortTemp)
		{
			var mappedColorCount = MappedTemperatureRange.max - MappedTemperatureRange.min;

			var gradientColors = new List<Tuple<Color, float>>();
			for (int i = 0; i < HeatMap.GradientSteps; i++)
				gradientColors.Add(new Tuple<Color, float>(HeatMap.Instance.GetGradientColor(i), 1f));

			var gradientSteps = new List<float>();
			for (int i = 0, t = range.min; i < mappedColorCount; i++, t++)
				gradientSteps.Add((t >= minComfortTemp - 1 && t <= maxComfortTemp) ? 4 : 1);

			return ColorHelper.Gradient(gradientColors, gradientSteps).ToArray();
		}

		public static (IntRange mappedRange, int minComfortable, int maxComfortable) GetComfortTemperatureRanges()
		{
			var human = ThingDefOf.Human;
			var minComfortTemp = (int)human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) + 3;
			var maxComfortTemp = (int)human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) - 3;

			var comfortDoubleRange = (maxComfortTemp - minComfortTemp) * 2;
			var mappedTemperatureRange = new IntRange(
				minComfortTemp - comfortDoubleRange, maxComfortTemp + comfortDoubleRange);

			return (mappedTemperatureRange, minComfortTemp, maxComfortTemp);
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
