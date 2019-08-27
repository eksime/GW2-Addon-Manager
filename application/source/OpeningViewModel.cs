﻿using GalaSoft.MvvmLight.Command;
using IWshRuntimeLibrary;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using File = System.IO.File;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// <c>OpeningViewModel</c> serves as the DataContext for OpeningView.xaml, which is the first screen encountered upon opening the application.
    /// </summary>
    public class OpeningViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// An event used to indicate that a property's value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A method used in notifying that a property's value has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed value.</param>
        protected void propertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// <summary>
        /// This constructor initializes various default properties across the class and then
        /// applies any updated values to them using <c>ApplyDefaultConfig</c>.
        /// </summary>
        public OpeningViewModel()
        {
            /* default values for various properties */
            ArcDPS_CheckBox = false;
            ArcDPS_Content = "ArcDPS";

            GW2Radial_CheckBox = false;
            GW2Radial_Content = "GW2 Radial";

            d912pxy_CheckBox = false;
            d912pxy_Content = "d912pxy";

            UpdateLinkVisibility = "Hidden";

            GamePath = configuration.getConfig().game_path;  

            /* applying any values from config.ini */
            configuration.ApplyDefaultConfig(this);
        }


        /// <summary>
        /// A string that is assigned a value if there is an update available.
        /// </summary>
        public string UpdateAvailable { get; set; }
        /// <summary>
        /// A string representing a visibility value for the Github releases link.
        /// </summary>
        public string UpdateLinkVisibility { get; set; }

        /* ARC */
        private bool _arc_box;
        private string _arc_content;
        /// <summary>
        /// The data binding for the ArcDPS checkbox's IsChecked property.
        /// </summary>
        public bool ArcDPS_CheckBox
        {
            get { return _arc_box; }
            set { _arc_box = value; propertyChanged("ArcDPS_CheckBox"); }
        }

        /// <summary>
        /// The data binding for the ArcDPS checkbox's Content property.
        /// </summary>
        public string ArcDPS_Content
        {
            get { return _arc_content; }
            set { _arc_content = value; propertyChanged("ArcDPS_Content"); }
        }


        /* GW2 Radial */
        private bool _radial_box;
        private string _radial_content;

        /// <summary>
        /// The data binding for the GW2 Radial checkbox's IsChecked property.
        /// </summary>
        public bool GW2Radial_CheckBox
        {
            get { return _radial_box; }
            set { _radial_box = value; propertyChanged("GW2Radial_CheckBox"); }
        }
        /// <summary>
        /// The data binding for the GW2 Radial checkbox's Content property.
        /// </summary>
        public string GW2Radial_Content
        {
            get { return _radial_content; }
            set { _radial_content = value; propertyChanged("GW2Radial_Content"); }
        }


        /* d912pxy */
        private bool _d912pxy_box;
        private string _d912pxy_content;
        /// <summary>
        /// The data binding for the d912pxy checkbox's IsChecked property.
        /// </summary>
        public bool d912pxy_CheckBox
        {
            get { return _d912pxy_box; }
            set { _d912pxy_box = value; propertyChanged("d912pxy_CheckBox"); }
        }
        /// <summary>
        /// The data binding for the d912pxy checkbox's Content property.
        /// </summary>
        public string d912pxy_Content
        {
            get { return _d912pxy_content; }
            set { _d912pxy_content = value; propertyChanged("d912pxy_Content"); }
        }

        /// <summary>
        /// Binding for the Content property of the text box displayed on the opening page.
        /// </summary>
        public string GamePath { get; set; }



        /***** Button Handlers *****/

        /// <summary>
        /// Handles button commands for the "set" button next to the game path text field in the opening screen.
        /// <see cref="configuration.SetGamePath(string)"/>
        /// </summary>
        public ICommand SetGamePath
        {
            get { return new RelayCommand<object>(param => configuration.SetGamePath(GamePath), true); }
        }

        /// <summary>
        /// Handles button commands for the button to make the current add-on selection the default.
        /// <see cref="configuration.ChangeAddonConfig(OpeningViewModel)"/>
        /// </summary>
        public ICommand SetDefaultAddons
        {
            get { return new RelayCommand<object>(param => configuration.ChangeAddonConfig(this), true); }
        }

        /// <summary>
        /// Handles the disable selected addons button.
        /// </summary>
        public ICommand DisableSelected
        {
            get { return new RelayCommand<object>(param =>PluginManagement.DisableSelected(this), true); }
        }

        /// <summary>
        /// Handles the enable selected addons button.
        /// </summary>
        public ICommand EnableSelected
        {
            get { return new RelayCommand<object>(param => PluginManagement.EnableSelected(this), true); }
        }

        /// <summary>
        /// Handles the delete selected addons button.
        /// </summary>
        public ICommand DeleteSelected
        {
            get { return new RelayCommand<object>(param => PluginManagement.DeleteSelected(this), true); }
        }
        

        /// <summary>
        /// Handles the create shortcut button under the options menu. <see cref="cs_logic"/>
        /// </summary>
        public ICommand CreateShortcut
        {
            get { return new RelayCommand<object>(param => cs_logic(), true); }
        }

        /// <summary>
        /// Creates a shortcut in the current user's start menu.
        /// </summary>
        //see the accepted answer at https://stackoverflow.com/questions/25024785/how-to-create-start-menu-shortcut
        //- I did some modifications (based on another SO question) to avoid admin access requirement
        private void cs_logic()
        {
            string appPath = Directory.GetCurrentDirectory();
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string appShortcutPath = Path.Combine(startMenuPath, @"GW2-UOAOM");
            string shortcutNamePath = Path.Combine(appShortcutPath, "GW2 Addon Manager" + ".lnk");

            if (!Directory.Exists(appShortcutPath))
                Directory.CreateDirectory(appShortcutPath);

            if (!File.Exists(shortcutNamePath))
            {
                WshShell quickShell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)quickShell.CreateShortcut(shortcutNamePath);
                shortcut.Description = "The Guild Wars 2 Unofficial Add-On Manager";
                shortcut.IconLocation = Path.Combine(appPath, "resources\\logo.ico");
                shortcut.WorkingDirectory = appPath;
                shortcut.TargetPath = Path.Combine(appPath, "GW2 Addon Manager" + ".exe");
                shortcut.Save();
            }
        }
    }
}
