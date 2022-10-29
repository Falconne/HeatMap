using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HeatMap
{
	public class ColorHelper
	{ 
		/// <summary>
		/// Calculate a color from a color gradient between two colors
		/// </summary>
		/// <param name="start">Gradient start color</param>
		/// <param name="end">Gradient end color</param>
		/// <param name="pos">Position in color gradient; can be equal <paramref name="steps"/> to return <paramref name="end"/></param>
		/// <param name="steps">Number of steps in the color gradient</param>
		/// <param name="a">Alpha-Channel</param>
		/// <returns></returns>
		public static Color Gradient(Color start, Color end, float pos, float steps, float a = 1f)
		{
			if (steps <= 0)
				return new Color(end.r, end.g, end.b, a);

			pos %= steps + 1;

			var rMin = start.r;
			var rMax = end.r;
			var gMin = start.g;
			var gMax = end.g;
			var bMin = start.b;
			var bMax = end.b;

			var rAverage = rMin + ((rMax - rMin) * pos / steps);
			var gAverage = gMin + ((gMax - gMin) * pos / steps);
			var bAverage = bMin + ((bMax - bMin) * pos / steps);

			return new Color(rAverage, gAverage, bAverage, a);
		}

		/// <summary>
		/// Creates a color gradient using <paramref name="colors"/> and <paramref name="steps"/>
		/// </summary>
		/// <param name="colors">Colors to be used for the gradient with relative weighting factor; relatively higher factors will make the color be used for more steps</param>
		/// <param name="steps">Steps with stepwidth; longer stepwidth advances through the gradient faster</param>
		/// <param name="a">Alpha-Channel</param>
		/// <returns></returns>
		public static List<Color> Gradient(IList<Tuple<Color, float>> colors, IList<float> steps, float a = 1f)
		{
			if (!(colors?.Count > 0))
			{
				Log.Warning("Colors must have at least 1 element");
				return new List<Color> { Color.white };
			}

			if (!(steps?.Count > 0))
			{
				Log.Warning("Steps must have at least 1 element");
				return new List<Color> { Color.white };
			}

			var stepsTotal = 0f;
			foreach (var step in steps)
				stepsTotal += step;
			if (stepsTotal == 0)
			{
				Log.Warning("Steps total must be greater than 0");
				return new List<Color> { Color.white };
			}

			var total = 0f;
			for (int i = 0; i < colors.Count - 1; i++)
				total += colors[i].Item2;

			var offset = 0f;
			var list = new List<Tuple<Color, float>>();
			foreach (var item in colors)
			{
				list.Add(new Tuple<Color, float>(item.Item1, offset));
				offset += stepsTotal * (item.Item2 / total);
			}

			var output = new List<Color>();
			var pos = 0f;
			for (int i = 0, j = 0; i < list.Count - 1; i++)
			{
				var item = list[i];
				var next = list[i + 1];
				while (pos < next.Item2)
				{
					output.Add(Gradient(
						item.Item1,
						next.Item1,
						pos - item.Item2,
						next.Item2 - item.Item2,
						a));
					pos += steps[j++];
				}
			}
			output.Add(colors.Last().Item1);
			return output;
		}
	}
}
