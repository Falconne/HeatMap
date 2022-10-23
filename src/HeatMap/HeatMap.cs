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

        private SettingHandle<bool> _useCustomRange;
        private SettingHandle<int> _customRangeMin;
        private SettingHandle<int> _customRangeMax;


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
                "FALCHM.OverrideVanillaOverlayDesc".Translate(),
                true);
            _overrideVanillaOverlay.ValueChanged += 
                val => ResetAll();

            _showIndoorsOnly = Settings.GetHandle(
                "showRoomsOnly",
                "FALCHM.ShowIndoorsOnly".Translate(),
                "FALCHM.ShowIndoorsOnlyDesc".Translate(),
                true);
            _showIndoorsOnly.ValueChanged += 
                val => ResetAll();

            _opacity = Settings.GetHandle(
                "opacity", "FALCHM.OverlayOpacity".Translate(),
                "FALCHM.OverlayOpacityDesc".Translate(), 30,
                Validators.IntRangeValidator(0, 100));
            _opacity.ValueChanged += 
                val => ResetAll();

            _updateDelay = Settings.GetHandle("updateDelay",
                "FALCHM.UpdateDelay".Translate(),
                "FALCHM.UpdateDelayDesc".Translate(),
                100,
                Validators.IntRangeValidator(1, 9999));


            _showOutdoorThermometer = Settings.GetHandle(
                "showOutdoorThermometer",
                "FALCHM.ShowOutDoorThermometer".Translate(),
                "FALCHM.ShowOutDoorThermometerDesc".Translate(),
                true);

            _outdoorThermometerOpacity = Settings.GetHandle(
                "outdoorThermometerOpacity",
                "FALCHM.ThermometerOpacity".Translate(),
                "FALCHM.ThermometerOpacityDesc".Translate(),
                30,
                Validators.IntRangeValidator(1, 100));
            _outdoorThermometerOpacity.ValueChanged += 
                val => _temperatureTextureCache.Clear();

			_outdoorThermometerFixed = Settings.GetHandle(
				"outdoorThermometerFixed",
				"FALCHM.ThermometerFixed".Translate(),
				"FALCHM.ThermometerFixedDesc".Translate(),
                false);

			_outdoorThermometerRight = Settings.GetHandle(
				"outdoorThermometerRight",
				"FALCHM.ThermometerRight".Translate(),
				"FALCHM.ThermometerRightDesc".Translate(),
				8f + _boxSize);

			_outdoorThermometerTop = Settings.GetHandle(
				"outdoorThermometerTop",
				"FALCHM.ThermometerTop".Translate(),
				"FALCHM.ThermometerTopDesc".Translate(),
				8f);


			_showTemperatureOverRooms = Settings.GetHandle(
                "showTemperatureOverRooms",
                "FALCHM.ShowTemperatureOverRooms".Translate(),
                "FALCHM.ShowTemperatureOverRoomsDesc".Translate(),
                true);


            _useCustomRange = Settings.GetHandle(
                "useCustomeRange",
                "FALCHM.UseCustomeRange".Translate(),
                "FALCHM.UseCustomeRangeDesc".Translate(),
                false);
            _useCustomRange.ValueChanged += 
                val => ResetAll();


            _customRangeMin = Settings.GetHandle("customRangeMin", "Unused", "Unused", 0);
            _customRangeMax = Settings.GetHandle("customRangeMax", "Unused", "Unused", 40);

            _customRangeMin.VisibilityPredicate = () => false;
            _customRangeMax.VisibilityPredicate = () => false;


            var customRangeValidator = Validators.IntRangeValidator(
                (int)GenTemperature.CelsiusTo(-100, Prefs.TemperatureMode),
                (int)GenTemperature.CelsiusTo(100, Prefs.TemperatureMode));

            var customRangeMin = Settings.GetHandle(
                "customRangeMinPlaceholder",
                "FALCHM.CustomRangeMin".Translate(),
                $"{"FALCHM.CustomRangeMinDesc".Translate()} ({Prefs.TemperatureMode.ToStringHuman()})",
                (int)GenTemperature.CelsiusTo(_customRangeMin, Prefs.TemperatureMode),
                customRangeValidator);

            customRangeMin.Unsaved = true;
            customRangeMin.VisibilityPredicate = () => _useCustomRange;

            var customRangeMax = Settings.GetHandle(
                "customRangeMaxPlaceholder",
                "FALCHM.CustomRangeMax".Translate(),
                $"{"FALCHM.CustomRangeMaxDesc".Translate()} ({Prefs.TemperatureMode.ToStringHuman()})",
                (int)GenTemperature.CelsiusTo(_customRangeMax, Prefs.TemperatureMode),
                customRangeValidator);

            customRangeMax.Unsaved = true;
            customRangeMax.VisibilityPredicate = () => _useCustomRange;


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
