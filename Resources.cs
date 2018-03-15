using UnityEngine;
using Verse;

namespace HeatMap
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        public static Texture2D Icon = ContentFinder<Texture2D>.Get("HeatMap");

        public static Texture2D DisplayBoder = ContentFinder<Texture2D>.Get("DisplayBorder");
    }
}