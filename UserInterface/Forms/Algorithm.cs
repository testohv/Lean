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
using System.Drawing;
using System.Windows.Forms;

namespace QuantConnect.Views.Forms
{
    /// <summary>
    /// Algorithm backtesting form.
    /// </summary>
    public class Algorithm : Form
    {
        // Create the Algorithm instance
        private static Algorithm _instance;
        private static Language _language;
        private static string _file;
        private static string _className;

        #region FormElementDeclarations

        #endregion

        /// <summary>
        /// Create the algorithm backtesting form
        /// </summary>
        public Algorithm(Form parent, Language language, string file, string className)
        {
            //Save the class variables:
            _language = language;
            _file = file;
            _className = className;

            //Layout pieces
            Name = "";
            Text = className;
            Icon = new Icon("../../Icons/kformula-16.ico");
            MdiParent = parent;
            MinimizeBox = false;
            MaximizeBox = true;
            MinimumSize = new Size(1024, 576);
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = false;
            Load += OnLoad;
            Shown += OnShown;
            //ResizeEnd += OnResizeEnd;
        }


        /// <summary>
        /// On showing the form.
        /// </summary>
        private void OnShown(object sender, EventArgs eventArgs)
        {
            CenterToScreen();
            WindowState = FormWindowState.Normal;
            WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Singleton implementation for the welcome form.
        /// </summary>
        public static Algorithm Instance(Form parent, Language language, string file, string className)
        {
            if (_instance == null)
            {
                _instance = new Algorithm(parent, language, file, className);
            }
            return _instance;
        }

        /// <summary>
        /// Trigger the onload event for the form.
        /// </summary>
        private void OnLoad(object sender, EventArgs eventArgs)
        {

        }

        /// <summary>
        /// Reset our about form static instance
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _instance = null;
        }
    }
}
