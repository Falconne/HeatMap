using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class HeatMap : HugsLib.ModBase
    {
        internal new ModLogger Logger => base.Logger;

        internal static HeatMap Instance { get; private set; }

        public override string ModIdentifier => "HeatMap";

        public RoomTemperatureDisplayer TemperatureDisplayer { get; } = new RoomTemperatureDisplayer();

        private const float _boxSize = 62f;
        private bool _draggingThermometer = false;
        private float _dragThermometerRight = 0f;
        private float _dragThermometerTop = 0f;

        private readonly Dictionary<int, Texture2D> _temperatureTextureCache = new Dictionary<int, Texture2D>();

        private SettingHandle<bool> _overrideVanillaOverlay;
        private SettingHandle<bool> _showIndoorsOnly;
        private SettingHandle<int> _opacity;
        private SettingHandle<int> _updateDelay;

        private SettingHandle<bool> _showOutdoorThermometer;
        private SettingHandle<int> _outdoorThermometerOpacity;
        private SettingHandle<bool> _outdoorThermometerFixed;
        private SettingHandle<float> _outdoorThermometerRight;
        private SettingHandle<float> _outdoorThermometerTop;

        private SettingHandle<bool> _showTemperatureOverRooms;

        public const int GradientSteps = 5;
        private readonly SettingHandle<float>[] _gradientHue = new SettingHandle<float>[GradientSteps];

        private SettingHandle<bool> _useCustomRange;
        private SettingHandle<int> _customRangeMin;
        private SettingHandle<int> _customRangeMax;
        private SettingHandle<int> _customRangeComfortMin;
        private SettingHandle<int> _customRangeComfortMax;


        public bool OverrideVanillaOverlay =>
            _overrideVanillaOverlay;
        public bool ShowIndoorsOnly =>
            _showIndoorsOnly;
        public float OverlayOpacity =>
            _opacity / 100f;
        public bool ShouldUseCustomRange =>
            _useCustomRange;
        public int CustomRangeMin =>
            _customRangeMin;
        public int CustomRangeMax =>
            _customRangeMax;
        public int CustomRangeComfortMin =>
            _customRangeComfortMin;
        public int CustomRangeComfortMax =>
            _customRangeComfortMax;

        public Color GetGradientColor(int index)
		{
            if (index >= 0 && index < _gradientHue.Length)
                return Color.HSVToRGB(_gradientHue[index] / 360f, 1f, 1f);
            return Color.black;
        }


        public HeatMap()
        {
            Instance = this;
        }

		public void UpdateOutdoorThermometer()
        {
            if (!_showOutdoorThermometer)
                return;

			var right = Mathf.Clamp(_draggingThermometer ? _dragThermometerRight : _outdoorThermometerRight, _boxSize, UI.screenWidth);
			var top = Mathf.Clamp(_draggingThermometer ? _dragThermometerTop : _outdoorThermometerTop, 0, UI.screenHeight - _boxSize);
			var outRect = new Rect(UI.screenWidth - right, top, _boxSize, _boxSize);
			if (TutorSystem.AdaptiveTrainingEnabled && Find.PlaySettings.showLearningHelper)
			{
				if (typeof(LearningReadout).GetField("windowRect", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Find.Tutor.learningReadout) is Rect helpRect
					&& helpRect.Overlaps(outRect) == true)
						outRect.x = helpRect.x - _boxSize - 5f;
			}

			if (!_outdoorThermometerFixed && Event.current.isMouse)
			{
				switch (Event.current.type)
				{
					case EventType.MouseDown:
						if (Mouse.IsOver(outRect) && Event.current.modifiers == EventModifiers.Shift)
						{
							Event.current.Use();

							_dragThermometerRight = _outdoorThermometerRight.Value;
							_dragThermometerTop = _outdoorThermometerTop.Value;

							_draggingThermometer = true;
						}
						break;
					case EventType.MouseDrag:
						if (_draggingThermometer)
						{
							Event.current.Use();
							
							_dragThermometerRight -= Event.current.delta.x;
							_dragThermometerRight = Mathf.Clamp(_dragThermometerRight, _boxSize, UI.screenWidth);
							_dragThermometerTop += Event.current.delta.y;
							_dragThermometerTop = Mathf.Clamp(_dragThermometerTop, 0, UI.screenHeight - _boxSize);
							//outRect = new Rect(outRect.x - _dragThermometerRight, outRect.y + _dragThermometerTop, _boxSize, _boxSize); // repositioning is processed on next update
						}
						break;
					case EventType.MouseUp:
						if (_draggingThermometer)
						{
							Event.current.Use();

							_outdoorThermometerRight.Value = _dragThermometerRight;
							_outdoorThermometerTop.Value = _dragThermometerTop;

							_draggingThermometer = false;
						}
						break;
				}
			}
			
			var temperature = Find.CurrentMap.mapTemperature.OutdoorTemp;
            var textureIndex = HeatMapHelper.GetIndexForTemperature(temperature);
            if (!_temperatureTextureCache.ContainsKey(textureIndex))
            {
                var backColor = HeatMapHelper.GetColorForTemperature(temperature);
                backColor.a = _outdoorThermometerOpacity / 100f;
                _temperatureTextureCache[textureIndex] = SolidColorMaterials.NewSolidColorTexture(backColor);
            }
            GUI.DrawTexture(outRect, _temperatureTextureCache[textureIndex]);
            GUI.DrawTexture(outRect, Resources.DisplayBoder);

            var temperatureForDisplay = temperature.ToStringTemperature("F0");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(outRect, temperatureForDisplay);

            if (Widgets.ButtonInvisible(outRect))
                Find.PlaySettings.showTemperatureOverlay = !Find.PlaySettings.showTemperatureOverlay;

            TooltipHandler.TipRegion(outRect, "FALCHM.ThermometerTooltip".Translate());

            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void OnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing 
                || Find.CurrentMap == null 
                || WorldRendererUtility.WorldRenderedNow)
                return;

            UpdateOutdoorThermometer();
            if (Find.PlaySettings.showTemperatureOverlay && _showTemperatureOverRooms)
            {
                TemperatureDisplayer.Update(_updateDelay);
                TemperatureDisplayer.OnGUI();
            }

            if (Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None)
                return;

            if (HeatMapKeyBingings.ToggleHeatMap.JustPressed)
            {
                if (WorldRendererUtility.WorldRenderedNow)
                    return;

                Find.PlaySettings.showTemperatureOverlay = !Find.PlaySettings.showTemperatureOverlay;
            }
        }

        public override void WorldLoaded()
        {
            ResetAll();
        }

        public override void DefsLoaded()
        {
            _overrideVanillaOverlay = Settings.GetHandle(
                "overrideVanillaOverlay",
                "FALCHM.OverrideVanillaOverlay".Translate(),
                "FALCHM.OverrideVanillaOverlayDesc".Translate(new NamedArgument(true, "default")),
                true);
            _overrideVanillaOverlay.ValueChanged += val => ResetAll();

            _showIndoorsOnly = Settings.GetHandle(
                "showRoomsOnly",
                "FALCHM.ShowIndoorsOnly".Translate(),
                "FALCHM.ShowIndoorsOnlyDesc".Translate(new NamedArgument(true, "default")),
                true);
            _showIndoorsOnly.ValueChanged += val => ResetAll();

            _opacity = Settings.GetHandle(
                "opacity", 
                "FALCHM.OverlayOpacity".Translate(),
                "FALCHM.OverlayOpacityDesc".Translate(new NamedArgument(30, "default")), 
                30,
                Validators.IntRangeValidator(0, 100));
            _opacity.ValueChanged += val => ResetAll();

            _updateDelay = Settings.GetHandle(
                "updateDelay",
                "FALCHM.UpdateDelay".Translate(),
                "FALCHM.UpdateDelayDesc".Translate(new NamedArgument(100, "default")),
                100,
                Validators.IntRangeValidator(1, 9999));


            _showOutdoorThermometer = Settings.GetHandle(
                "showOutdoorThermometer",
                "FALCHM.ShowOutDoorThermometer".Translate(),
                "FALCHM.ShowOutDoorThermometerDesc".Translate(new NamedArgument(true, "default")),
                true);

            _outdoorThermometerOpacity = Settings.GetHandle(
                "outdoorThermometerOpacity",
                "FALCHM.ThermometerOpacity".Translate(),
                "FALCHM.ThermometerOpacityDesc".Translate(new NamedArgument(30, "default")),
                30,
                Validators.IntRangeValidator(1, 100));
            _outdoorThermometerOpacity.ValueChanged += val => _temperatureTextureCache.Clear();

			_outdoorThermometerFixed = Settings.GetHandle(
				"outdoorThermometerFixed",
				"FALCHM.ThermometerFixed".Translate(),
				"FALCHM.ThermometerFixedDesc".Translate(new NamedArgument(false, "default")),
                false);

			_outdoorThermometerRight = Settings.GetHandle(
				"outdoorThermometerRight",
				"FALCHM.ThermometerRight".Translate(),
				"FALCHM.ThermometerRightDesc".Translate(new NamedArgument(8f + _boxSize, "default")),
				8f + _boxSize);

			_outdoorThermometerTop = Settings.GetHandle(
				"outdoorThermometerTop",
				"FALCHM.ThermometerTop".Translate(),
				"FALCHM.ThermometerTopDesc".Translate(new NamedArgument(8f, "default")),
				8f);


			_showTemperatureOverRooms = Settings.GetHandle(
                "showTemperatureOverRooms",
                "FALCHM.ShowTemperatureOverRooms".Translate(),
                "FALCHM.ShowTemperatureOverRoomsDesc".Translate(new NamedArgument(true, "default")),
                true);

            (var mappedRange, var minComfortTemp, var maxComfortTemp) = HeatMapHelper.GetComfortTemperatureRanges();

            var gradientValidator = Validators.FloatRangeValidator(0f, 360f);
            for (int i = 0; i < GradientSteps; i++)
            {
                _gradientHue[i] = Settings.GetHandle(
                    $"gradientHue{i}",
                    $"FALCHM.GradientHue".Translate(new NamedArgument(i, "index")),
                    $"FALCHM.GradientHueDesc".Translate(
                        new NamedArgument(240f - 60f * i, "default"),
                        new NamedArgument(mappedRange.min, "min"),
                        new NamedArgument(mappedRange.max, "max"),
                        new NamedArgument(minComfortTemp, "comfortMin"),
                        new NamedArgument(maxComfortTemp, "comfortMax")),
                    240f - 60f * i, // 0° = red, 60° = yellow, 120° = green, 180° = cyan, 240° = blue, 300° = magenta; standard begins at blue (low) and ends at red (high)
                    gradientValidator);
                _gradientHue[i].ValueChanged += val => ResetAll();
            }


            _useCustomRange = Settings.GetHandle(
                "useCustomeRange",
                "FALCHM.UseCustomeRange".Translate(),
                "FALCHM.UseCustomeRangeDesc".Translate(new NamedArgument(false, "default")),
                false);
            _useCustomRange.ValueChanged += val => ResetAll();


            _customRangeMin = Settings.GetHandle("customRangeMin", "Unused", "Unused", mappedRange.min);
            _customRangeMin.VisibilityPredicate = () => false;

            _customRangeMax = Settings.GetHandle("customRangeMax", "Unused", "Unused", mappedRange.max);
            _customRangeMax.VisibilityPredicate = () => false;

            _customRangeComfortMin = Settings.GetHandle("customRangeComfortMin", "Unused", "Unused", minComfortTemp);
            _customRangeComfortMin.VisibilityPredicate = () => false;

            _customRangeComfortMax = Settings.GetHandle("customRangeComfortMax", "Unused", "Unused", maxComfortTemp);
            _customRangeComfortMax.VisibilityPredicate = () => false;


            var customRangeValidator = Validators.IntRangeValidator(
                (int)GenTemperature.CelsiusTo(-273f, Prefs.TemperatureMode),
                (int)GenTemperature.CelsiusTo(1000f, Prefs.TemperatureMode));

            var customRangeMin = Settings.GetHandle<int>(
                "customRangeMinPlaceholder",
                "FALCHM.CustomRangeMin".Translate(),
                $"{"FALCHM.CustomRangeMinDesc".Translate(new NamedArgument(mappedRange.min, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
                validator: customRangeValidator);
            customRangeMin.Unsaved = true;
            customRangeMin.VisibilityPredicate = () => _useCustomRange;
            customRangeMin.Value = (int)GenTemperature.CelsiusTo(_customRangeMin, Prefs.TemperatureMode);

            var customRangeMax = Settings.GetHandle<int>(
                "customRangeMaxPlaceholder",
                "FALCHM.CustomRangeMax".Translate(),
                $"{"FALCHM.CustomRangeMaxDesc".Translate(new NamedArgument(mappedRange.max, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
                validator: customRangeValidator);
            customRangeMax.Unsaved = true;
            customRangeMax.VisibilityPredicate = () => _useCustomRange;
            customRangeMax.Value = (int)GenTemperature.CelsiusTo(_customRangeMax, Prefs.TemperatureMode);

            var customRangeComfortMin = Settings.GetHandle<int>(
                "customRangeComfortMinPlaceholder",
                "FALCHM.CustomRangeComfortMin".Translate(),
                $"{"FALCHM.CustomRangeComfortMinDesc".Translate(new NamedArgument(minComfortTemp, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
                validator: customRangeValidator);
            customRangeComfortMin.Unsaved = true;
            customRangeComfortMin.VisibilityPredicate = () => _useCustomRange;
            customRangeComfortMin.Value = (int)GenTemperature.CelsiusTo(_customRangeComfortMin, Prefs.TemperatureMode);

            var customRangeComfortMax = Settings.GetHandle<int>(
                "customRangeComfortMaxPlaceholder",
                "FALCHM.CustomRangeComfortMax".Translate(),
                $"{"FALCHM.CustomRangeComfortMaxDesc".Translate(new NamedArgument(maxComfortTemp, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
                validator: customRangeValidator);
            customRangeComfortMax.Unsaved = true;
            customRangeComfortMax.VisibilityPredicate = () => _useCustomRange;
            customRangeComfortMax.Value = (int)GenTemperature.CelsiusTo(_customRangeComfortMax, Prefs.TemperatureMode);


            customRangeMin.ValueChanged += val =>
            {
                if (customRangeMax <= customRangeMin)
                    customRangeMax.Value = customRangeMin + 1;

                _customRangeMin.Value = ConvertToCelcius(customRangeMin);
                ResetAll();
            };
            customRangeMax.ValueChanged += val =>
            {
                if (customRangeMin >= customRangeMax)
                    customRangeMin.Value = customRangeMax - 1;

                _customRangeMax.Value = ConvertToCelcius(customRangeMax);
                ResetAll();
            };
            customRangeComfortMin.ValueChanged += val =>
            {
                _customRangeComfortMin.Value = ConvertToCelcius(customRangeComfortMin);
                ResetAll();
            };
            customRangeComfortMax.ValueChanged += val =>
            {
                _customRangeComfortMax.Value = ConvertToCelcius(customRangeComfortMax);
                ResetAll();
            };
        }

		public void ResetAll()
        {
            HeatMapHelper.RegenerateColorMap();
            TemperatureDisplayer.Reset();
            _temperatureTextureCache.Clear();

            Find.CurrentMap?.mapTemperature?.Drawer?.SetDirty();
        }

        private static int ConvertToCelcius(int value)
        {
            switch (Prefs.TemperatureMode)
            {
                default:
                case TemperatureDisplayMode.Celsius:
                    return value;

                case TemperatureDisplayMode.Kelvin:
                    return value - 273;

                case TemperatureDisplayMode.Fahrenheit:
                    return (int)((value - 32) / 1.8f);
            }
        }
    }
}
