﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Business;
using DataAccess;
using System.Windows.Media;

namespace NaturalGroundingPlayer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILayerContainer {
        private WindowHelper helper;
        public double DefaultHeight;
        public SearchSettings settings;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            DefaultHeight = this.Height;
            SessionCore.Instance.Start(this);
            helper = new WindowHelper(this);
        }

        #region UI Events

        /// <summary>
        /// Initialize the window when loaded.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            this.Title += " " + SessionCore.GetVersionText();
            AttachBusinessEvents(SessionCore.Instance.Business);
        }

        public async Task InitializationCompleted() {
            settings = SessionCore.Instance.Business.FilterSettings;
            this.DataContext = settings;
            PolarityFocus.ItemsSource = await SessionCore.Instance.Business.GetFocusCategoriesAsync();
            PolarityFocus.SelectedIndex = 0;
            Settings_Changed(null, null);
        }

        public void DetachControls() {
            MainMenuContainer.Content = null;
            RatingViewerContainer.Content = null;
            LayersContainer.Content = null;
            DetachBusinessEvents(SessionCore.Instance.Business);
        }

        public void AttachControls() {
            MainMenuContainer.Content = SessionCore.Instance.Menu;
            RatingViewerContainer.Content = SessionCore.Instance.RatingViewer;
            LayersContainer.Content = SessionCore.Instance.Layers;
            AttachBusinessEvents(SessionCore.Instance.Business);
        }

        private void AttachBusinessEvents(PlayerBusiness business) {
            business.GetConditions += business_GetConditions;
            business.IncreaseConditions += business_IncreaseConditions;
            business.PlaylistChanged += business_PlaylistChanged;
            business.DisplayPlayTime += business_DisplayPlayTime;
            business.NowPlaying += business_NowPlaying;
        }

        private void DetachBusinessEvents(PlayerBusiness business) {
            business.GetConditions -= business_GetConditions;
            business.IncreaseConditions -= business_IncreaseConditions;
            business.PlaylistChanged -= business_PlaylistChanged;
            business.DisplayPlayTime -= business_DisplayPlayTime;
            business.NowPlaying -= business_NowPlaying;
        }

        /// <summary>
        /// Update the display of conditions and notify the business layer of changes.
        /// </summary>
        private void Settings_Changed(object sender, RoutedEventArgs e) {
            if (this.IsLoaded) {
                if (IntensitySlider.Value == IntensitySlider.Maximum && GrowthSlider.Value > 0)
                    GrowthSlider.Value = 0;
                if (IntensitySlider.Value == IntensitySlider.Minimum && GrowthSlider.Value < 0)
                    GrowthSlider.Value = 0;

                SessionCore.Instance.Business.IsMinimumIntensity = (IntensitySlider.Value == IntensitySlider.Minimum);

                DisplayConditionsText();
                SessionCore.Instance.Business.ChangeConditions();
            }
        }

        #endregion

        #region Business Events

        private void business_NowPlaying(object sender, EventArgs e) {
            SessionCore.Instance.RatingViewer.Video = SessionCore.Instance.Business.CurrentVideo;
        }

        private void business_IncreaseConditions(object sender, EventArgs e) {
            IntensitySlider.Value += GrowthSlider.Value;
            // If decreasing heat, go down to Water from 1 above minimum
            if (GrowthSlider.Value < 0 && IntensitySlider.Value < IntensitySlider.Minimum + 1)
                IntensitySlider.Value = IntensitySlider.Minimum;
        }

        private async void business_GetConditions(object sender, GetConditionsEventArgs e) {
            var Filters = e.Conditions.RatingFilters;
            Filters.Clear();
            switch (SessionCore.Instance.Business.PlayMode) {
                case PlayerMode.Normal:
                    double MaxCond = IntensitySlider.Value + e.QueuePos * GrowthSlider.Value;
                    // If decreasing heat, go down to Water from 1 above minimum
                    if (GrowthSlider.Value < 0 && MaxCond < IntensitySlider.Minimum + 1)
                        MaxCond = IntensitySlider.Minimum;
                    if (MaxCond > IntensitySlider.Maximum)
                        MaxCond = IntensitySlider.Maximum;
                    if (MaxCond > IntensitySlider.Minimum) {
                        // Exclude videos with Water >= 6
                        Filters.AddRange(FilterPresets.PresetWater(true));
                        // Exclude videos with Fire >= 8.5 && Intensity >= 9.5 unless we're at maximum heat.
                        if (MaxCond < IntensitySlider.Maximum)
                            Filters.AddRange(FilterPresets.PresetFire(true));

                        if (PolarityFocus.Text == "Intensity")
                            MaxCond -= 1;
                        // Min/Max Values.
                        SearchRatingSetting MinFilter = new SearchRatingSetting(PolarityFocus.Text, OperatorConditionEnum.GreaterOrEqual, Math.Round(MaxCond - ToleranceSlider.Value, 1));
                        SearchRatingSetting MaxFilter = new SearchRatingSetting(PolarityFocus.Text, OperatorConditionEnum.Smaller, Math.Round(MaxCond, 1));
                        Filters.Add(MinFilter);
                        Filters.Add(MaxFilter);
                        if (PolarityFocus.Text == "Intensity" && MaxCond == IntensitySlider.Maximum - 1)
                            MaxFilter.Value = IntensitySlider.Maximum;
                        if (e.IncreaseTolerance) {
                            MinFilter.Value -= .5f;
                            MaxFilter.Value += .5f;
                        }
                        if (PolarityFocus.Text == "Physical" || PolarityFocus.Text == "Emotional" || PolarityFocus.Text == "Spiritual") {
                            // Don't get videos that are more than .5 stronger on other values.
                            Filters.Add(new SearchRatingSetting("!" + PolarityFocus.Text, OperatorConditionEnum.Smaller, MaxFilter.Value + .5f));
                        } else if (PolarityFocus.Text == "Intensity") {
                            Filters.Add(new SearchRatingSetting("!Intensity", OperatorConditionEnum.Smaller, MaxFilter.Value + 2f));
                        }
                    } else { // Water
                        IntensitySlider.Value = IntensitySlider.Minimum;
                        await SessionCore.Instance.Business.SetWaterVideosAsync(true);
                    }
                    // Change condition display after "Fire" returns to normal.
                    DisplayConditionsText();
                    break;
                case PlayerMode.WarmPause:
                case PlayerMode.Fire:
                    Filters.AddRange(FilterPresets.PresetPause());
                    break;
                case PlayerMode.Water:
                    if (!e.IncreaseTolerance) {
                        Filters.Add(new SearchRatingSetting("Water", OperatorConditionEnum.GreaterOrEqual, 4f));
                    } else {
                        Filters.Add(new SearchRatingSetting("Intensity", OperatorConditionEnum.Smaller, 7f));
                        Filters.Add(new SearchRatingSetting("Party", OperatorConditionEnum.Smaller, 2f));
                        Filters.Add(new SearchRatingSetting("Vitality", OperatorConditionEnum.Smaller, 2f));
                        Filters.Add(new SearchRatingSetting("Fire", OperatorConditionEnum.Smaller, 2f));
                        Filters.Add(new SearchRatingSetting("!", OperatorConditionEnum.Smaller, 9f));
                    }
                    break;
                case PlayerMode.RequestCategory:
                    // Filters are already defined in Business.FilterSettings.
                    Filters.Add(new SearchRatingSetting());
                    break;
            }
        }

        public void business_PlaylistChanged(object sender, EventArgs e) {
            txtVideosFound.Text = SessionCore.Instance.Business.LastSearchResultCount.ToString() + (SessionCore.Instance.Business.LastSearchResultCount > 1 ? " videos found" : " video found");
            txtNextVideo.Text = SessionCore.Instance.Business.GetVideoDisplayTitle(SessionCore.Instance.Business.NextVideo);
            txtCurrentVideo.Text = SessionCore.Instance.Business.GetVideoDisplayTitle(SessionCore.Instance.Business.CurrentVideo);
            DisplayConditionsText();
        }

        private void business_DisplayPlayTime(object sender, EventArgs e) {
            txtSessionTime.Text = FormatHelper.FormatTimeSpan(SessionCore.Instance.Business.SessionTotalSeconds);
            if (SessionCore.Instance.Business.IsPlaying == false)
                txtSessionTime.Text += " (Paused)";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the conditions text.
        /// </summary>
        private void DisplayConditionsText() {
            if (SessionCore.Instance.Business.IsMinimumIntensity && !SessionCore.Instance.Business.IsSpecialMode())
                IntensitySliderValue.Text = "Water...";
            else {
                switch (SessionCore.Instance.Business.PlayMode) {
                    case PlayerMode.Normal:
                    case PlayerMode.SpecialRequest:
                    case PlayerMode.Water:
                        double MaxCond = IntensitySlider.Value;
                        bool IsMaxIntensity = false;
                        if (((RatingCategory)PolarityFocus.SelectedItem).Name == "Intensity") {
                            if (MaxCond == IntensitySlider.Maximum)
                                IsMaxIntensity = true;
                            MaxCond -= 1;
                        }
                        IntensitySliderValue.Text = string.Format("{0:0.0} - {1:0.0}", MaxCond - ToleranceSlider.Value, IsMaxIntensity ? IntensitySlider.Maximum : MaxCond);
                        break;
                    case PlayerMode.WarmPause:
                        IntensitySliderValue.Text = "Warm Pause";
                        break;
                    case PlayerMode.RequestCategory:
                        StringBuilder Result = new StringBuilder();
                        if (!string.IsNullOrEmpty(settings.Search))
                            Result.Append(settings.Search);
                        if (!string.IsNullOrEmpty(settings.RatingCategory) && settings.RatingValue.HasValue) {
                            if (Result.Length > 0)
                                Result.Append(", ");
                            Result.Append(settings.RatingCategory);
                            if (settings.RatingOperator == OperatorConditionEnum.GreaterOrEqual)
                                Result.Append(" >= ");
                            else if (settings.RatingOperator == OperatorConditionEnum.Equal)
                                Result.Append(" = ");
                            else if (settings.RatingOperator == OperatorConditionEnum.Smaller)
                                Result.Append(" < ");
                            Result.Append(settings.RatingValue.Value.ToString("0.0"));
                        }
                        IntensitySliderValue.Text = Result.ToString();
                        break;
                    case PlayerMode.Fire:
                        IntensitySliderValue.Text = "Fire!!";
                        break;
                }
            }
        }

        public void AdjustHeight(double height) {
            using (var d = Dispatcher.DisableProcessing()) {
                Height += height * Settings.Zoom;
                LayersRow.Height = new GridLength(LayersRow.Height.Value + height);
            }
        }

        public void ResetHeight() {
            using (var d = Dispatcher.DisableProcessing()) {
                Height = DefaultHeight * Settings.Zoom;
                LayersRow.Height = new GridLength(0);
            }
        }

        #endregion

        private void RatioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.IsLoaded) {
                SessionCore.Instance.RatingViewer.Ratio = (double)RatioSlider.Value;
                Settings_Changed(null, null);
            }
        }

        private void window_Closed(object sender, EventArgs e) {
            MediaEncoderBusiness.ClearTempFolder();
        }
    }
}