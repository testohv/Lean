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
using System.IO;
using System.Windows.Forms;

namespace QuantConnect.Views.Forms
{
    /// <summary>
    /// About form to display some information about Lean.
    /// </summary>
    public class About : Form
    {
        // Storage for the singleton instance
        private static About _instance;
        private PictureBox _picture;
        private Label _title;
        private Label _description;

        /// <summary>
        /// Create the About Form.
        /// </summary>
        public About(Form parent)
        {
            //Layout pieces
            Size = new Size(420, 140);
            Name = "About";
            Text = "About QuantConnect LEAN Engine v" + Constants.Version;
            Icon = new Icon("../../Icons/lean.ico");
            MdiParent = parent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Shown += (sender, args) => CenterToScreen();

            //Add on the form elements
            _picture = new PictureBox();
            _picture.Image = Image.FromFile(Path.Combine(Forms.Container.IconPath, "icon_128x128.png"));
            _picture.Size = new Size(80, 80);
            _picture.Location = new Point(10, 10);
            _picture.SizeMode = PictureBoxSizeMode.Zoom;
            Controls.Add(_picture);

            //Put some labels on the form to talk about LEAN
            _title = new Label();
            _title.Text = "QuantConnect LEAN Algortihmic Trading Engine v" + Constants.Version;
            _title.Location = new Point(100, 10);
            _title.Size = new Size(300, 20);
            Controls.Add(_title);

            _description = new Label();
            _description.Location = new Point(100, 40);
            _description.Size = new Size(300, 60);
            _description.Text = "LEAN is an open source algorithmic trading platform build in C#." + 
                                "It was open sourced in 2015 by QuantConnect Corporation under the apache license. " +
                                "For support inquiries please contact support@quantconnect.com";
            Controls.Add(_description);
        }

        /// <summary>
        /// Singleton implementation for the about form.
        /// </summary>
        public static About Instance(Form parent)
        {
            if (_instance == null) _instance = new About(parent);
            return _instance;
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
