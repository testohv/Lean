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
using System.Reflection;
using System.Windows.Forms;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;

namespace QuantConnect.Views.Forms
{
    /// <summary>
    /// Welcome screen on loading the Desktop Interface for LEAN Engine
    /// </summary>
    public class OpenAlgorithm : Form
    {
        // Storage for the singleton instance
        private static OpenAlgorithm _instance;
        private readonly string _algorithmLocationFile = Config.Get("algorithm-location");
        private readonly string _algorithmTypeName = Config.Get("algorithm-type-name");

        #region FormElementDeclarations
        private Button _loadButton;
        private Button _cancelButton;
        private DataGridView _dataGridClassNames;
        private DataGridViewTextBoxColumn _algorithmNameColumn;
        private Button _algorithmLocationButton;
        private Label _algorithmLocationLabel;
        private Label _selectedLabel;
        private OpenFileDialog _openFileDialog;
        private RadioButton _languageCSharpRadioButton;
        private RadioButton _languageFSharpRadioButton;
        private RadioButton _languageVisualBasicRadioButton;
        private RadioButton _languagePythonRadioButton;
        private RadioButton _languageJavaRadioButton;
        private Language _language;

        #endregion

        /// <summary>
        /// Create the Open Algorithm Form
        /// </summary>
        public OpenAlgorithm(Form parent)
        {
            //Load algorithm button.
            _loadButton = new Button();
            _loadButton.Location = new Point(460, 300);
            _loadButton.Size = new Size(150, 45);
            _loadButton.TabIndex = 5;
            _loadButton.Text = "&Open";
            _loadButton.UseVisualStyleBackColor = true;
            _loadButton.Image = Image.FromFile(Path.Combine(Forms.Container.IconPath, "kformula-32.png"));
            _loadButton.ImageAlign = ContentAlignment.MiddleLeft;
            _loadButton.Click += LoadButtonOnClick;
            _loadButton.Enabled = true;
            Controls.Add(_loadButton);

            //Cancel dialog button.
            _cancelButton = new Button();
            _cancelButton.Location = new Point(620, 300);
            _cancelButton.Size = new Size(150, 45);
            _cancelButton.TabIndex = 5;
            _cancelButton.Text = "&Cancel";
            _cancelButton.UseVisualStyleBackColor = true;
            _cancelButton.Image = Image.FromFile(Path.Combine(Forms.Container.IconPath, "dialog-cancel-32.png"));
            _cancelButton.ImageAlign = ContentAlignment.MiddleLeft;
            _cancelButton.Click += CancelButtonOnClick;
            Controls.Add(_cancelButton);

            _algorithmLocationLabel = new Label();
            _algorithmLocationLabel.Location = new Point(10, 10);
            _algorithmLocationLabel.Size = new Size(600, 30);
            _algorithmLocationLabel.AutoSize = false;
            _algorithmLocationLabel.TextAlign = ContentAlignment.MiddleLeft;
            _algorithmLocationLabel.BorderStyle = BorderStyle.Fixed3D;
            Controls.Add(_algorithmLocationLabel);

            //Show the selected item:
            _languageCSharpRadioButton = new RadioButton() { AutoSize = true, Text = "CSharp", Location = new Point(10, 310), Checked = true};
            _languageFSharpRadioButton = new RadioButton() { AutoSize = true, Text = "FSharp", Location = new Point(70, 310) };
            _languageVisualBasicRadioButton = new RadioButton() { AutoSize = true, Text = "VisualBasic", Location = new Point(130, 310) };
            _languageJavaRadioButton = new RadioButton() { AutoSize = true, Text = "Java", Location = new Point(220, 310) };
            _languagePythonRadioButton = new RadioButton() { AutoSize = true, Text = "Python", Location = new Point(270, 310) };
            Controls.AddRange(new Control[] { _languageCSharpRadioButton, _languageFSharpRadioButton, _languageJavaRadioButton, _languagePythonRadioButton, _languageVisualBasicRadioButton });

            _selectedLabel = new Label();
            _selectedLabel.Location = new Point(10, 330);
            _selectedLabel.Size = new Size(450,20);
            _selectedLabel.AutoSize = false;
            Controls.Add(_selectedLabel);

            _openFileDialog = new OpenFileDialog();
            _openFileDialog.Multiselect = false;
            _openFileDialog.CheckFileExists = false;
            _openFileDialog.Filter = "Algorithm Class Library (*.dll)|*.dll";

            //Show select algorithm dialog.
            _algorithmLocationButton = new Button();
            _algorithmLocationButton.Location = new Point(620, 10);
            _algorithmLocationButton.Size = new Size(150, 30);
            _algorithmLocationButton.TabIndex = 5;
            _algorithmLocationButton.Text = "&Select File";
            _algorithmLocationButton.UseVisualStyleBackColor = true;
            _algorithmLocationButton.Image = Image.FromFile(Path.Combine(Forms.Container.IconPath, "document-open-16.png"));
            _algorithmLocationButton.ImageAlign = ContentAlignment.MiddleLeft;
            _algorithmLocationButton.Click += AlgorithmLocationButtonOnClick;
            Controls.Add(_algorithmLocationButton);
            
            _algorithmNameColumn = new DataGridViewTextBoxColumn();
            _algorithmNameColumn.HeaderText = "Algorithm Class Name";
            _algorithmNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _algorithmNameColumn.ReadOnly = true;

            _dataGridClassNames = new DataGridView();
            _dataGridClassNames.ReadOnly = true;
            _dataGridClassNames.MultiSelect = false;
            _dataGridClassNames.AllowUserToAddRows = false;
            _dataGridClassNames.AllowUserToDeleteRows = false;
            _dataGridClassNames.Size = new Size(760, 225);
            _dataGridClassNames.Location = new Point(10,65);
            _dataGridClassNames.Columns.AddRange(_algorithmNameColumn);
            _dataGridClassNames.RowHeadersVisible = false;
            _dataGridClassNames.AllowUserToResizeRows = false;
            _dataGridClassNames.AllowUserToResizeColumns = false;
            _dataGridClassNames.ScrollBars = ScrollBars.Vertical;
            _dataGridClassNames.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _dataGridClassNames.SelectionChanged += DataGridOnSelectionChanged;
            _dataGridClassNames.DoubleClick += DataGridOnDoubleClick;
            Controls.Add(_dataGridClassNames);

            Size = new Size(797, 395);
            Text = "Open Algorithm";
            Icon = new Icon("../../Icons/folder-open-16.ico");
            MdiParent = parent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = false;
            Shown += OnShown;
            Load += OnLoad;
        }

        /// <summary>
        /// Center the open algorithm dialog to the screen
        /// </summary>
        private void OnShown(object sender, EventArgs eventArgs)
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            WindowState = FormWindowState.Normal;
            CenterToScreen();
        }


        /// <summary>
        /// Load the selected algorithm and save the DLL and class name to the config.
        /// </summary>
        private void DataGridOnDoubleClick(object sender, EventArgs eventArgs)
        {
            if (_dataGridClassNames.SelectedCells.Count == 0) return;
            LoadAlgorithm(GetLanguage(), GetLocationFile(), GetClassName());
        }

        /// <summary>
        /// Get the language of the algorithm dll.
        /// </summary>
        private Language GetLanguage()
        {
            if (_languageCSharpRadioButton.Checked) return Language.CSharp;
            if (_languageFSharpRadioButton.Checked) return Language.FSharp;
            if (_languageJavaRadioButton.Checked) return Language.Java;
            if (_languageVisualBasicRadioButton.Checked) return Language.VisualBasic;
            if (_languagePythonRadioButton.Checked) return Language.Python;
            return Language.CSharp;
        }

        /// <summary>
        /// Get thestring location of the dll file.
        /// </summary>
        /// <returns></returns>
        private string GetLocationFile()
        {
            return _algorithmLocationLabel.Text;
        }

        /// <summary>
        /// Get the string class name selected.
        /// </summary>
        /// <returns></returns>
        private string GetClassName()
        {
            if (_dataGridClassNames.SelectedCells.Count > 0)
            {
                return _dataGridClassNames.SelectedCells[0].Value.ToString();
            }
            return _selectedLabel.Text;
        }

        /// <summary>
        /// Load the selected file and classname.
        /// </summary>
        /// <param name="language">Language of the algorithm we want to load</param>
        /// <param name="file">File dll location</param>
        /// <param name="className">Class name inside the dll</param>
        private void LoadAlgorithm(Language language, string file, string className)
        {
            // Save the data to configuration file:
            Config.Set("algorithm-language", language.ToString());
            Config.Set("algorithm-location", file);
            Config.Set("algorithm-type-name", className);
            Config.WriteAll();

            //Close the open dialog
            Hide();
            Dispose();

            // Load the algorithm form.
            var algorithmModal = Algorithm.Instance(Forms.Container.Instance(), language, file, className);
            algorithmModal.Show();
        }

        /// <summary>
        /// Show the selection in the bottom left
        /// </summary>
        private void DataGridOnSelectionChanged(object sender, EventArgs eventArgs)
        {
            if (_dataGridClassNames.SelectedCells.Count == 0) return;
            _selectedLabel.Text = _dataGridClassNames.SelectedCells[0].Value.ToString();
            _loadButton.Enabled = true;
        }


        /// <summary>
        /// Reload the data grid view class list.
        /// </summary>
        private void AlgorithmLocationButtonOnClick(object sender, EventArgs eventArgs)
        {
            //Select the file and set the dll.
            var result = _openFileDialog.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                LoadDllClasses(_openFileDialog.FileName);
            }
        }


        /// <summary>
        /// Event handler for clicking cancel.
        /// </summary>
        private void CancelButtonOnClick(object sender, EventArgs e)
        {
            Hide();
            Dispose();
        }

        /// <summary>
        /// Event handler for clicking the load button.
        /// </summary>
        private void LoadButtonOnClick(object sender, EventArgs e)
        {
            LoadAlgorithm(GetLanguage(), GetLocationFile(), GetClassName());
        }

        /// <summary>
        /// Singleton implementation for the welcome form.
        /// </summary>
        public static OpenAlgorithm Instance(Form parent)
        {
            if (_instance == null) _instance = new OpenAlgorithm(parent);
            return _instance;
        }

        /// <summary>
        /// Trigger the onload event for the form. Load the github data.
        /// </summary>
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            LoadDllClasses(_algorithmLocationFile);
        }

        /// <summary>
        /// Load the dll classes and put them into the dataGridView
        /// </summary>
        private void LoadDllClasses(string file)
        {
            _language = Language.CSharp;

            if (!File.Exists(file))
            {
                MessageBox.Show("Algorithm DLL specified in configuration file is not found. Please select a valid algorithm DLL to continue.");
                _loadButton.Enabled = false;
                return;
            }

            // Set the checkbox and then get the language enum
            if (file.Contains("CSharp")) { _languageCSharpRadioButton.Checked = true; }
            if (file.Contains("FSharp")) { _languageFSharpRadioButton.Checked = true; }
            if (file.Contains("VisualBasic")) { _languageVisualBasicRadioButton.Checked = true; }
            if (file.Contains("Java")) { _languageJavaRadioButton.Checked = true; }
            if (file.Contains("Python")) { _languagePythonRadioButton.Checked = true; }
            _language = GetLanguage();

            //Reset if its a valid file.
            _dataGridClassNames.Rows.Clear();

            //Set the algorithm location label on top.
            _algorithmLocationLabel.Text = file;

            try
            {
                //Load the class names from the location file.
                if (_language != Language.Python)
                {
                    var algorithmClasses = Loader.GetExtendedTypeNames(file);
                    foreach (var className in algorithmClasses)
                    {
                        var rowId = _dataGridClassNames.Rows.Add(className);

                        //If this is the class name from the config, automatically select it.
                        if (className == _algorithmTypeName)
                        {
                            _dataGridClassNames.ClearSelection();
                            _dataGridClassNames.Rows[rowId].Selected = true;
                            _selectedLabel.Text = className;
                        }
                    }
                }
                else
                {
                    //Load the python class name is a random hash so not worth showing.
                    //string errorMessage;
                    //IAlgorithm instance;
                    //var loader = new Loader(Language.Python, TimeSpan.FromSeconds(10), names => names.SingleOrDefault());
                    //loader.TryCreateAlgorithmInstance(file, out instance, out errorMessage);
                    //var name = instance.GetType().Name;
                    var name = "Algorithm class inside main.py";
                    var rowId = _dataGridClassNames.Rows.Add(name);
                    _dataGridClassNames.ClearSelection();
                    _dataGridClassNames.Rows[rowId].Selected = true;
                    _selectedLabel.Text = name;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Failed to load algorithm classes from the provided DLL. Please select another: " + err.Message, "Open Algorithm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
