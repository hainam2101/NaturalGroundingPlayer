﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Business;
using DataAccess;
using EmergenceGuardian.Downloader;

namespace NaturalGroundingPlayer {
    /// <summary>
    /// Interaction logic for AddDownload.xaml
    /// </summary>
    public partial class AddDownload : Window {
        private EditVideoBusiness business = new EditVideoBusiness();
        private Media video;
        private WindowHelper helper;
        protected ClosingCallback callback;
        private bool isUrlValid;
        // The Start Download checkbox will retain its value between window instances.
        private static bool StartDownloadDefault = true;

        /// <summary>
        /// Initializes a new instance of the Add Download window.
        /// </summary>
        public static AddDownload Instance(ClosingCallback callback) {
            AddDownload NewForm = new AddDownload();
            NewForm.callback = callback;
            SessionCore.Instance.Windows.Show(NewForm);
            return NewForm;
        }

        public AddDownload() {
            InitializeComponent();
            helper = new WindowHelper(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            video = business.NewVideo();
            this.DataContext = video;
            StartDownloadCheckBox.IsChecked = StartDownloadDefault;
            CategoryCombo.ItemsSource = business.GetCategories(video.MediaTypeId);
        }

        private async void DownloadUrlText_LostFocus(object sender, RoutedEventArgs e) {
            isUrlValid = false;
            ErrorText.Text = "";
            if (video.DownloadUrl.Length > 0) {
                string VTitle = await DownloadManager.GetVideoTitle(video.DownloadUrl);
                if (VTitle != null) {
                    video.DownloadName = VTitle;
                    isUrlValid = true;
                } else
                    ErrorText.Text = "Please enter a valid URL";
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e) {
            if (SaveChanges()) {
                StartDownloadDefault = StartDownloadCheckBox.IsChecked.Value;
                this.Close();

                // Start download after closing.
                if (StartDownloadCheckBox.IsChecked.Value)
                    await SessionCore.Instance.Business.DownloadManager.DownloadVideoAsync(video, -1, null);
            }
        }

        private bool SaveChanges() {
            SaveButton.Focus();
            video.Artist = video.Artist.Trim();
            video.Album = video.Album.Trim();
            video.Title = video.Title.Trim();
            video.DownloadName = video.DownloadName.Trim();
            video.DownloadUrl = video.DownloadUrl.Trim();
            video.BuyUrl = video.BuyUrl.Trim();

            ErrorText.Text = "";
            if (TitleText.Text.Length == 0) {
                ErrorText.Text = "Title is required.";
                return false;
            }
            if (business.IsTitleDuplicate(video)) {
                ErrorText.Text = "Artist and title already exist in the database.";
                return false;
            }

            if (!isUrlValid && StartDownloadCheckBox.IsChecked == true) {
                ErrorText.Text = "Please enter a valid URL";
                return false;
            }

            if (SessionCore.Instance.Business.DownloadManager.IsDownloadDuplicate(video.DownloadUrl)) {
                ErrorText.Text = "You are already downloading this video.";
                return false;
            }

            video.EditedOn = DateTime.UtcNow;
            business.Save();

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void CategoryCombo_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            ComboBox Obj = sender as ComboBox;
            if (Obj.SelectedItem != null)
                Obj.Text = ((KeyValuePair<Guid?, string>)Obj.SelectedItem).Value;
            else
                Obj.Text = "";
        }

        private void StartDownloadCheckBox_Click(object sender, RoutedEventArgs e) {
            SaveButton.Content = StartDownloadCheckBox.IsChecked == true ? "_Download" : "_Add";
        }
    }
}
