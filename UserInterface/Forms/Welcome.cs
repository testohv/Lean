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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuantConnect.Views.GitHub;

namespace QuantConnect.Views.Forms
{
    /// <summary>
    /// Welcome screen on loading the Desktop Interface for LEAN Engine
    /// </summary>
    public class Welcome : Form
    {
        // Storage for the singleton instance
        private static Welcome _instance;

        #region FormElementDeclarations

        private PictureBox _logo;
        private Label _labelTitle;
        private Label _labelDescription;
        private Label _labelLatestTitle;
        private Label _labelLatestGitHub;
        private Button _okButton;
        private Task _gitLoader;
        private CancellationTokenSource _gitCancelTokenSource;
        #endregion

        /// <summary>
        /// Create the About Form.
        /// </summary>
        public Welcome(Form parent)
        {
            //Create the form buttons
            _logo = new PictureBox();
            _logo.Image = Image.FromFile(Path.Combine(Forms.Container.IconPath, "icon_128x128.png"));
            _logo.SizeMode = PictureBoxSizeMode.AutoSize;
            _logo.Location = new Point(12, 12);
            Controls.Add(_logo);
            
            //Create the label
            _labelTitle = new Label { AutoSize = true };
            _labelTitle.Font = new Font("Microsoft Sans Serif", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _labelTitle.Location = new Point(146, 12);
            _labelTitle.Size = new Size(186, 25);
            _labelTitle.Text = "Welcome to LEAN";
            Controls.Add(_labelTitle);

            //Create the description label
            _labelDescription = new Label();
            _labelDescription.Location = new Point(148, 45);
            _labelDescription.Size = new Size(642, 29);
            _labelDescription.Text = "LEAN is an open source algorithmic trading platform build in C#." +
                                "It was open sourced in 2015 by QuantConnect Corporation under the apache license. " +
                                "For support inquiries please contact support@quantconnect.com";
            Controls.Add(_labelDescription);

            //Welcome form button
            _okButton = new Button();
            _okButton.Location = new Point(620, 300);
            _okButton.Size = new Size(147, 45);
            _okButton.TabIndex = 5;
            _okButton.Text = "&OK";
            _okButton.UseVisualStyleBackColor = true;
            _okButton.Image = Image.FromFile(Path.Combine(Forms.Container.IconPath, "dialog-ok-apply-32.png"));
            _okButton.ImageAlign = ContentAlignment.MiddleLeft;
            _okButton.Click += OkButtonOnClick;
            Controls.Add(_okButton);

            //Create label for latest from github title
            _labelLatestTitle = new Label();
            _labelLatestTitle.AutoSize = true;
            _labelLatestTitle.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _labelLatestTitle.Location = new Point(148, 78);
            _labelLatestTitle.Name = "_labelLatestTitle";
            _labelLatestTitle.Size = new Size(145, 20);
            _labelLatestTitle.TabIndex = 3;
            _labelLatestTitle.Text = "Latest from GitHub";
            Controls.Add(_labelLatestTitle);
            
            //Create label for the latest github body.
            _labelLatestGitHub = new Label();
            _labelLatestGitHub.AutoSize = true;
            _labelLatestGitHub.Location = new Point(149, 113);
            _labelLatestGitHub.Name = "_labelLatestGitHub";
            _labelLatestGitHub.Size = new Size(71, 13);
            _labelLatestGitHub.TabIndex = 4;
            _labelLatestGitHub.Text = "Downloading GitHub History...";
            Controls.Add(_labelLatestGitHub);

            //Github Cancellation:
            _gitCancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            //Layout pieces
            Size = new Size(800, 400);
            Name = "Welcome";
            Text = "Welcome to LEAN Desktop";
            Icon = new Icon("../../Icons/lean.ico");
            MdiParent = parent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Shown += (sender, args) => CenterToScreen();

            Load += OnLoad;
        }

        /// <summary>
        /// Hide the modal and show the open algorithm dialog.
        /// </summary>
        private void OkButtonOnClick(object sender, EventArgs eventArgs)
        {
            Hide();
            Dispose();
            var open = OpenAlgorithm.Instance(Forms.Container.Instance());
            open.Show();
        }

        /// <summary>
        /// Singleton implementation for the welcome form.
        /// </summary>
        public static Welcome Instance(Form parent)
        {
            if (_instance == null) _instance = new Welcome(parent);
            return _instance;
        }

        /// <summary>
        /// Trigger the onload event for the form. Load the github data.
        /// </summary>
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            _gitLoader = Task.Run(() =>
            {
                var git = new Git("QuantConnect");
                var commits = git.GetCommits("Lean");
                var text = "";

                foreach (var commit in commits.Take(8))
                {
                    var commitMessages = commit.Details.Message.Split(Environment.NewLine.ToCharArray());
                    var length = (commitMessages[0].Length > 50) ? 50 : commitMessages[0].Length;
                    text += commit.Details.Author.Date + " " + commitMessages[0].Substring(0, length) + " by " + commit.Details.Author.Name + Environment.NewLine + Environment.NewLine;
                }

                _labelLatestGitHub.SafeInvoke(() =>
                {
                    _labelLatestGitHub.Text = text;
                });
            }, _gitCancelTokenSource.Token);
        }

        /// <summary>
        /// Reset our about form static instance
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _gitCancelTokenSource.Cancel();
            base.Dispose(disposing);
            _instance = null;
        }
    }
}
