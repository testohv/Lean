/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QuantConnect.Configuration;

namespace QuantConnect.Views.Forms
{
    /// <summary>
    /// Primary MDI Container Window for Launching LEAN Desktop Application
    /// </summary>
    public class Container : Form
    {
        #region Properties
        //Setup Configuration:
        public static string IconPath = "../../Icons/";
        #endregion

        #region FormElementDeclarations
        //Menu Form elements:
        private MenuStrip _menu;
        private ToolStripMenuItem _menuFile;
        private ToolStripMenuItem _menuFileOpen;
        private ToolStripSeparator _menuFileSeparator;
        private ToolStripMenuItem _menuFileNewBacktest;
        private ToolStripMenuItem _menuFileExit;
        private ToolStripMenuItem _menuView;
        private ToolStripMenuItem _menuViewToolBar;
        private ToolStripMenuItem _menuViewStatusBar;
        private ToolStripMenuItem _menuData;
        private ToolStripMenuItem _menuDataOpenFolder;
        private ToolStripMenuItem _menuDataDownloadData;
        private ToolStripMenuItem _menuTools;
        private ToolStripMenuItem _menuToolsSettings;
        private ToolStripMenuItem _menuWindows;
        private ToolStripMenuItem _menuWindowsCascade;
        private ToolStripMenuItem _menuWindowsTileVertical;
        private ToolStripMenuItem _menuWindowsTileHorizontal;
        private ToolStripMenuItem _menuHelp;
        private ToolStripMenuItem _menuHelpAbout;
        //Toolstrip form elements:
        private ToolStrip _toolStrip;
        private ToolStripButton _toolStripOpen;
        private ToolStripSeparator _toolStripSeparator;
        private ToolStripButton _toolStripNewBacktest;
        //Status Stripe Elements:
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusStripLabel;
        private ToolStripStatusLabel _statusStripSpring;
        private ToolStripStatusLabel _statusStripStatistics;
        private ToolStripProgressBar _statusStripProgress;
        //Timer;
        private Timer _timer;
        private static Container _containerInstance;

        #endregion

        /// <summary>
        /// Launch the LEAN Desktop Interface
        /// </summary>
        [STAThread]
        static public void Main()
        {
            //Start GUI
            Console.WriteLine("Launching QuantConnect LEAN Desktop Application...");
            Application.Run(new Container());
        }

        /// <summary>
        /// Launch the Lean Engine Primary Form:
        /// </summary>
        public Container()
        {
            //Set the MDI Parent Instance
            _containerInstance = this;

            //Suspend the form layout while we're creating items
            SuspendLayout();

            //Images:
            var newIcon = Image.FromFile(Path.Combine(IconPath, "document-new-16.png"));
            var newBacktestIcon = Image.FromFile(Path.Combine(IconPath, "office-chart-area-16.png"));
            var openIcon = Image.FromFile(Path.Combine(IconPath, "folder-open-16.png"));
            var exitIcon = Image.FromFile(Path.Combine(IconPath, "application-exit-16.png"));
            var aboutIcon = Image.FromFile(Path.Combine(IconPath, "help-about-16.png"));
            var dataIcon = Image.FromFile(Path.Combine(IconPath, "server-database.png"));
            var dataDownloadIcon = Image.FromFile(Path.Combine(IconPath, "download.png"));
            var settingsIcon = Image.FromFile(Path.Combine(IconPath, "preferences-other.png"));
            var cascadeIcon = Image.FromFile(Path.Combine(IconPath, "window-cascade.png"));
            var tileVerticalIcon = Image.FromFile(Path.Combine(IconPath, "window-tile-vertical.png"));
            var tileHorizontalIcon = Image.FromFile(Path.Combine(IconPath, "window-tile-horizontal.png"));

            //Create and add the toolstrip
            _toolStrip = new ToolStrip();
            _toolStripOpen = new ToolStripButton("Open Algorithm", openIcon, MenuOpenAlgorithmOnClick);
            _toolStripSeparator = new ToolStripSeparator();
            _toolStripNewBacktest = new ToolStripButton("Launch Backtest", newBacktestIcon) { Enabled = false };
            _toolStrip.Items.AddRange(new ToolStripItem[] { _toolStripOpen, _toolStripSeparator, _toolStripNewBacktest });
            Controls.Add(_toolStrip);

            //Add the menu items to the tool strip
            _menu = new MenuStrip();
            
            //Create the file menu
            _menuFile = new ToolStripMenuItem("&File");
            _menuFileOpen = new ToolStripMenuItem("&Open Algorithm...", openIcon, MenuOpenAlgorithmOnClick);
            _menuFileNewBacktest = new ToolStripMenuItem("&Launch Backtest", newBacktestIcon) {Enabled = false};
            _menuFileExit = new ToolStripMenuItem("E&xit", exitIcon, MenuFileExitOnClick);
            _menuFile.DropDownItems.AddRange(new ToolStripItem[] { _menuFileOpen, new ToolStripSeparator(), _menuFileNewBacktest, new ToolStripSeparator(), _menuFileExit });
            
            //Create the view menu
            _menuView = new ToolStripMenuItem("&View");
            _menuViewToolBar = new ToolStripMenuItem("&Tool Bar") { CheckOnClick = true, Checked = true};
            _menuViewToolBar.Click += MenuViewToolBarOnClick;
            _menuViewStatusBar = new ToolStripMenuItem("&Status Bar") { CheckOnClick = true, Checked = true };
            _menuViewStatusBar.Click += MenuViewStatusBarOnClick;
            _menuView.DropDownItems.AddRange(new ToolStripItem[] { _menuViewToolBar, _menuViewStatusBar });

            //Create Data menu
            _menuData = new ToolStripMenuItem("&Data");
            _menuDataOpenFolder = new ToolStripMenuItem("Data &Folder", dataIcon, MenuDataOpenFolderOnClick);
            _menuDataDownloadData = new ToolStripMenuItem("&Download Data", dataDownloadIcon); _menuDataDownloadData.Click += MenuDataDownloadDataOnClick;
            _menuData.DropDownItems.AddRange(new ToolStripItem[] {_menuDataOpenFolder, _menuDataDownloadData});

            //Create Tools Menu
            _menuTools = new ToolStripMenuItem("&Tools");
            _menuToolsSettings = new ToolStripMenuItem("&Settings", settingsIcon);
            _menuTools.DropDownItems.AddRange(new ToolStripItem[] {_menuToolsSettings});

            //Create windows menu:
            _menuWindows = new ToolStripMenuItem("&Windows");
            _menuWindowsCascade = new ToolStripMenuItem("&Cascade", cascadeIcon); 
            _menuWindowsCascade.Click += MenuWindowsCascadeOnClick;
            _menuWindowsTileVertical = new ToolStripMenuItem("Tile &Vertical", tileVerticalIcon);
            _menuWindowsTileVertical.Click += MenuWindowsTileVerticalOnClick;
            _menuWindowsTileHorizontal = new ToolStripMenuItem("Tile &Horizontal", tileHorizontalIcon);
            _menuWindowsTileHorizontal.Click += MenuWindowsTileHorizontalOnClick;
            _menuWindows.DropDownItems.AddRange(new ToolStripItem[] {_menuWindowsCascade, _menuWindowsTileVertical, _menuWindowsTileHorizontal});

            //Create the help menu
            _menuHelp = new ToolStripMenuItem("&Help");
            _menuHelpAbout = new ToolStripMenuItem("&About", aboutIcon);
            _menuHelpAbout.Click += MenuHelpAboutOnClick;
            _menuHelp.DropDownItems.AddRange(new ToolStripItem[] {_menuHelpAbout});

            //Finalise the menu
            _menu.Items.AddRange(new ToolStripItem[] { _menuFile, _menuView, _menuData, _menuTools, _menuWindows, _menuHelp });
            Controls.Add(_menu);
            MainMenuStrip = _menu;

            //Create and add the status strip:
            _statusStrip = new StatusStrip();
            _statusStripLabel = new ToolStripStatusLabel("Loading Complete");
            _statusStripSpring = new ToolStripStatusLabel {Spring = true};
            _statusStripProgress = new ToolStripProgressBar();
            _statusStripStatistics = new ToolStripStatusLabel("Statistics: CPU:    Ram:    ");
            _statusStrip.Items.AddRange(new ToolStripItem[] {_statusStripLabel, _statusStripSpring,_statusStripStatistics, _statusStripProgress});
            Controls.Add(_statusStrip);
            
            //Completed adding items, layout the form
            //Setup the MDI Container Form.
            Name = "LeanEngineContainer";
            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Constants.Version;
            Size = new Size(1024, 768);
            MinimumSize = new Size(1024, 768);
            CenterToScreen();
            WindowState = FormWindowState.Maximized;
            MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
            Icon = new Icon("../../Icons/lean.ico");
            IsMdiContainer = true;

            //Setup Container Events:
            Load += OnLoad;

            //Trigger a timer event.
            _timer = new Timer {Interval = 1000};
            _timer.Tick += TimerOnTick;

            //Allow the drawing of the form
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Open the data downloader dialog box and display download options.
        /// </summary>
        private void MenuDataDownloadDataOnClick(object sender, EventArgs eventArgs)
        {
            
        }

        /// <summary>
        /// Open the folder in explorer to view the data files
        /// </summary>
        private void MenuDataOpenFolderOnClick(object sender, EventArgs eventArgs)
        {
            var dataDirectory = Path.GetFullPath(Config.Get("data-folder"));
            Process.Start(dataDirectory);
        }

        /// <summary>
        /// Show the open algorithm form
        /// </summary>
        private void MenuOpenAlgorithmOnClick(object sender, EventArgs eventArgs)
        {
            var open = OpenAlgorithm.Instance(Forms.Container.Instance());
            open.Show();
        }

        /// <summary>
        /// Update performance counters
        /// </summary>
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            _statusStripStatistics.Text = "Performance: CPU: " + OS.CpuUsagePercentage + " Ram: " + OS.TotalPhysicalMemoryUsed + " Mb";
        }

        /// <summary>
        /// Layout the MDI children horizontally
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MenuWindowsTileHorizontalOnClick(object sender, EventArgs eventArgs)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        /// <summary>
        /// Layout the MDI windows vertically.
        /// </summary>
        private void MenuWindowsTileVerticalOnClick(object sender, EventArgs eventArgs)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        /// <summary>
        /// Layout the internal MDI windows as cascade.
        /// </summary>
        private void MenuWindowsCascadeOnClick(object sender, EventArgs eventArgs)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        /// <summary>
        /// Initialization events on loading the container
        /// </summary>
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            //Show a welcome dialog on loading the container
            var welcome = Welcome.Instance(this);
            welcome.Show();

            //Start Stats Counter:
            _timer.Start();

            //Complete load
            _statusStripLabel.Text = "LEAN Desktop v" + Constants.Version + " Load Complete.";
        }

        /// <summary>
        /// Toggle the toolbar visibility
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MenuViewToolBarOnClick(object sender, EventArgs eventArgs)
        {
            _toolStrip.Visible = !_toolStrip.Visible;
        }

        /// <summary>
        /// Toggle the status bar visibility
        /// </summary>
        private void MenuViewStatusBarOnClick(object sender, EventArgs eventArgs)
        {
            _statusStrip.Visible = !_statusStrip.Visible;
        }

        /// <summary>
        /// Launch the about menu
        /// </summary>
        private void MenuHelpAboutOnClick(object sender, EventArgs eventArgs)
        {
            var about = About.Instance(this);
            about.Show();
        }


        /// <summary>
        /// Cancel any running backtest, dispose of the forms and exit the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MenuFileExitOnClick(object sender, EventArgs eventArgs)
        {
            Application.Exit();
        }

        /// <summary>
        /// Instance of the MDI Parent
        /// </summary>
        public static Container Instance()
        {
            return _containerInstance;
        }
    }
}
