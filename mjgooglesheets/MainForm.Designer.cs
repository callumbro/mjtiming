namespace RaceBeam
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                sheetDebounceTimer?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.scoresTextBox = new System.Windows.Forms.TextBox();
            this.scoringList = new System.Windows.Forms.CheckedListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bestRunRadioButton = new System.Windows.Forms.RadioButton();
            this.day1Plusset2RadioButton = new System.Windows.Forms.RadioButton();
            this.set2RadioButton = new System.Windows.Forms.RadioButton();
            this.set1RadioButton = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.OfficialRunsTextBox = new System.Windows.Forms.TextBox();
            this.Day1TextBox = new System.Windows.Forms.TextBox();
            this.Day2TextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.DataFolderTextBox = new System.Windows.Forms.TextBox();
            this.groupBoxGoogle = new System.Windows.Forms.GroupBox();
            this.forceFullRefreshButton = new System.Windows.Forms.Button();
            this.syncEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.grantGoogleButton = new System.Windows.Forms.Button();
            this.applicationNameTextBox = new System.Windows.Forms.TextBox();
            this.labelAppName = new System.Windows.Forms.Label();
            this.labelSheetsHint = new System.Windows.Forms.Label();
            this.spreadsheetIdTextBox = new System.Windows.Forms.TextBox();
            this.labelSpreadsheetId = new System.Windows.Forms.Label();
            this.clientSecretTextBox = new System.Windows.Forms.TextBox();
            this.labelClientSecret = new System.Windows.Forms.Label();
            this.clientIdTextBox = new System.Windows.Forms.TextBox();
            this.labelClientId = new System.Windows.Forms.Label();
            this.sheetStatusLabel = new System.Windows.Forms.Label();
            this.labelSheetStatus = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBoxGoogle.SuspendLayout();
            this.SuspendLayout();
            //
            // scoresTextBox
            //
            this.scoresTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scoresTextBox.Font = new System.Drawing.Font("Lucida Console", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scoresTextBox.Location = new System.Drawing.Point(11, 402);
            this.scoresTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.scoresTextBox.Multiline = true;
            this.scoresTextBox.Name = "scoresTextBox";
            this.scoresTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.scoresTextBox.Size = new System.Drawing.Size(760, 241);
            this.scoresTextBox.TabIndex = 0;
            this.scoresTextBox.WordWrap = false;
            //
            // scoringList
            //
            this.scoringList.BackColor = System.Drawing.SystemColors.Control;
            this.scoringList.CheckOnClick = true;
            this.scoringList.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scoringList.FormattingEnabled = true;
            this.scoringList.Items.AddRange(new object[] {
            "Run Times",
            "Raw Times",
            "PAX Times",
            "Class Times",
            "XGroup Times",
            "Teams",
            "Cone Counts"});
            this.scoringList.Location = new System.Drawing.Point(11, 192);
            this.scoringList.Name = "scoringList";
            this.scoringList.Size = new System.Drawing.Size(129, 132);
            this.scoringList.TabIndex = 9;
            this.scoringList.SelectedIndexChanged += new System.EventHandler(this.ScoringListSelectedIndexChanged);
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.bestRunRadioButton);
            this.groupBox1.Controls.Add(this.day1Plusset2RadioButton);
            this.groupBox1.Controls.Add(this.set2RadioButton);
            this.groupBox1.Controls.Add(this.set1RadioButton);
            this.groupBox1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(162, 191);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Size = new System.Drawing.Size(136, 117);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            //
            // bestRunRadioButton
            //
            this.bestRunRadioButton.Location = new System.Drawing.Point(5, 68);
            this.bestRunRadioButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bestRunRadioButton.Name = "bestRunRadioButton";
            this.bestRunRadioButton.Size = new System.Drawing.Size(127, 20);
            this.bestRunRadioButton.TabIndex = 3;
            this.bestRunRadioButton.TabStop = true;
            this.bestRunRadioButton.Text = "Best single run";
            this.bestRunRadioButton.UseVisualStyleBackColor = true;
            this.bestRunRadioButton.CheckedChanged += new System.EventHandler(this.BestRunRadioButtonCheckedChanged);
            //
            // day1Plusset2RadioButton
            //
            this.day1Plusset2RadioButton.Location = new System.Drawing.Point(5, 50);
            this.day1Plusset2RadioButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.day1Plusset2RadioButton.Name = "day1Plusset2RadioButton";
            this.day1Plusset2RadioButton.Size = new System.Drawing.Size(127, 20);
            this.day1Plusset2RadioButton.TabIndex = 2;
            this.day1Plusset2RadioButton.TabStop = true;
            this.day1Plusset2RadioButton.Text = "Set1+Set2";
            this.day1Plusset2RadioButton.UseVisualStyleBackColor = true;
            this.day1Plusset2RadioButton.CheckedChanged += new System.EventHandler(this.Day1Plusset2RadioButtonCheckedChanged);
            //
            // set2RadioButton
            //
            this.set2RadioButton.Location = new System.Drawing.Point(5, 32);
            this.set2RadioButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.set2RadioButton.Name = "set2RadioButton";
            this.set2RadioButton.Size = new System.Drawing.Size(127, 20);
            this.set2RadioButton.TabIndex = 1;
            this.set2RadioButton.TabStop = true;
            this.set2RadioButton.Text = "Set2 only";
            this.set2RadioButton.UseVisualStyleBackColor = true;
            this.set2RadioButton.CheckedChanged += new System.EventHandler(this.Set2RadioButtonCheckedChanged);
            //
            // set1RadioButton
            //
            this.set1RadioButton.Location = new System.Drawing.Point(4, 16);
            this.set1RadioButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.set1RadioButton.Name = "set1RadioButton";
            this.set1RadioButton.Size = new System.Drawing.Size(127, 20);
            this.set1RadioButton.TabIndex = 0;
            this.set1RadioButton.TabStop = true;
            this.set1RadioButton.Text = "Set1 only";
            this.set1RadioButton.UseVisualStyleBackColor = true;
            this.set1RadioButton.CheckedChanged += new System.EventHandler(this.Set1RadioButtonCheckedChanged);
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.OfficialRunsTextBox);
            this.groupBox2.Location = new System.Drawing.Point(321, 190);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Size = new System.Drawing.Size(118, 70);
            this.groupBox2.TabIndex = 30;
            this.groupBox2.TabStop = false;
            //
            // label3
            //
            this.label3.Font = new System.Drawing.Font("Arial", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(5, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(106, 18);
            this.label3.TabIndex = 28;
            this.label3.Text = "# official runs";
            //
            // OfficialRunsTextBox
            //
            this.OfficialRunsTextBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OfficialRunsTextBox.Location = new System.Drawing.Point(45, 38);
            this.OfficialRunsTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.OfficialRunsTextBox.Name = "OfficialRunsTextBox";
            this.OfficialRunsTextBox.Size = new System.Drawing.Size(40, 21);
            this.OfficialRunsTextBox.TabIndex = 27;
            this.OfficialRunsTextBox.Text = "99";
            //
            // Day1TextBox
            //
            this.Day1TextBox.Location = new System.Drawing.Point(521, 204);
            this.Day1TextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Day1TextBox.Name = "Day1TextBox";
            this.Day1TextBox.Size = new System.Drawing.Size(123, 20);
            this.Day1TextBox.TabIndex = 33;
            //
            // Day2TextBox
            //
            this.Day2TextBox.Location = new System.Drawing.Point(521, 230);
            this.Day2TextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Day2TextBox.Name = "Day2TextBox";
            this.Day2TextBox.Size = new System.Drawing.Size(123, 20);
            this.Day2TextBox.TabIndex = 34;
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(462, 204);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 19);
            this.label1.TabIndex = 35;
            this.label1.Text = "Day 1:";
            //
            // label2
            //
            this.label2.Location = new System.Drawing.Point(462, 234);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 19);
            this.label2.TabIndex = 36;
            this.label2.Text = "Day 2:";
            //
            // label4
            //
            this.label4.Location = new System.Drawing.Point(462, 262);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(113, 27);
            this.label4.TabIndex = 37;
            this.label4.Text = "Event data folder";
            //
            // DataFolderTextBox
            //
            this.DataFolderTextBox.Location = new System.Drawing.Point(462, 284);
            this.DataFolderTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.DataFolderTextBox.Name = "DataFolderTextBox";
            this.DataFolderTextBox.Size = new System.Drawing.Size(259, 20);
            this.DataFolderTextBox.TabIndex = 38;
            //
            // groupBoxGoogle
            //
            this.groupBoxGoogle.Controls.Add(this.forceFullRefreshButton);
            this.groupBoxGoogle.Controls.Add(this.syncEnabledCheckBox);
            this.groupBoxGoogle.Controls.Add(this.grantGoogleButton);
            this.groupBoxGoogle.Controls.Add(this.applicationNameTextBox);
            this.groupBoxGoogle.Controls.Add(this.labelAppName);
            this.groupBoxGoogle.Controls.Add(this.labelSheetsHint);
            this.groupBoxGoogle.Controls.Add(this.spreadsheetIdTextBox);
            this.groupBoxGoogle.Controls.Add(this.labelSpreadsheetId);
            this.groupBoxGoogle.Controls.Add(this.clientSecretTextBox);
            this.groupBoxGoogle.Controls.Add(this.labelClientSecret);
            this.groupBoxGoogle.Controls.Add(this.clientIdTextBox);
            this.groupBoxGoogle.Controls.Add(this.labelClientId);
            this.groupBoxGoogle.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.groupBoxGoogle.Location = new System.Drawing.Point(11, 8);
            this.groupBoxGoogle.Name = "groupBoxGoogle";
            this.groupBoxGoogle.Size = new System.Drawing.Size(760, 172);
            this.groupBoxGoogle.TabIndex = 50;
            this.groupBoxGoogle.TabStop = false;
            this.groupBoxGoogle.Text = "Google Sheets";
            //
            // labelClientId
            //
            this.labelClientId.Font = new System.Drawing.Font("Arial", 8.25F);
            this.labelClientId.Location = new System.Drawing.Point(10, 22);
            this.labelClientId.Name = "labelClientId";
            this.labelClientId.Size = new System.Drawing.Size(200, 14);
            this.labelClientId.TabIndex = 0;
            this.labelClientId.Text = "OAuth Client ID";
            //
            // clientIdTextBox
            //
            this.clientIdTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.clientIdTextBox.Location = new System.Drawing.Point(10, 38);
            this.clientIdTextBox.Name = "clientIdTextBox";
            this.clientIdTextBox.Size = new System.Drawing.Size(340, 20);
            this.clientIdTextBox.TabIndex = 1;
            //
            // labelClientSecret
            //
            this.labelClientSecret.Font = new System.Drawing.Font("Arial", 8.25F);
            this.labelClientSecret.Location = new System.Drawing.Point(10, 58);
            this.labelClientSecret.Name = "labelClientSecret";
            this.labelClientSecret.Size = new System.Drawing.Size(200, 14);
            this.labelClientSecret.TabIndex = 2;
            this.labelClientSecret.Text = "OAuth Client secret";
            //
            // clientSecretTextBox
            //
            this.clientSecretTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.clientSecretTextBox.Location = new System.Drawing.Point(10, 74);
            this.clientSecretTextBox.Name = "clientSecretTextBox";
            this.clientSecretTextBox.Size = new System.Drawing.Size(340, 20);
            this.clientSecretTextBox.TabIndex = 3;
            this.clientSecretTextBox.UseSystemPasswordChar = true;
            //
            // labelSpreadsheetId
            //
            this.labelSpreadsheetId.Font = new System.Drawing.Font("Arial", 8.25F);
            this.labelSpreadsheetId.Location = new System.Drawing.Point(370, 22);
            this.labelSpreadsheetId.Name = "labelSpreadsheetId";
            this.labelSpreadsheetId.Size = new System.Drawing.Size(280, 14);
            this.labelSpreadsheetId.TabIndex = 4;
            this.labelSpreadsheetId.Text = "Spreadsheet ID (from URL)";
            //
            // spreadsheetIdTextBox
            //
            this.spreadsheetIdTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.spreadsheetIdTextBox.Location = new System.Drawing.Point(370, 38);
            this.spreadsheetIdTextBox.Name = "spreadsheetIdTextBox";
            this.spreadsheetIdTextBox.Size = new System.Drawing.Size(380, 20);
            this.spreadsheetIdTextBox.TabIndex = 5;
            //
            // labelSheetsHint
            //
            this.labelSheetsHint.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSheetsHint.Location = new System.Drawing.Point(370, 58);
            this.labelSheetsHint.Name = "labelSheetsHint";
            this.labelSheetsHint.Size = new System.Drawing.Size(380, 36);
            this.labelSheetsHint.TabIndex = 6;
            this.labelSheetsHint.Text = "Writes an \"Overall\" tab (HTML overall-ranking columns) plus one tab per class (HTML class table columns, including Score). Missing tabs are created automatically.";
            //
            // labelAppName
            //
            this.labelAppName.Font = new System.Drawing.Font("Arial", 8.25F);
            this.labelAppName.Location = new System.Drawing.Point(370, 98);
            this.labelAppName.Name = "labelAppName";
            this.labelAppName.Size = new System.Drawing.Size(200, 14);
            this.labelAppName.TabIndex = 8;
            this.labelAppName.Text = "App name (consent screen)";
            //
            // applicationNameTextBox
            //
            this.applicationNameTextBox.Font = new System.Drawing.Font("Arial", 8.25F);
            this.applicationNameTextBox.Location = new System.Drawing.Point(370, 114);
            this.applicationNameTextBox.Name = "applicationNameTextBox";
            this.applicationNameTextBox.Size = new System.Drawing.Size(380, 20);
            this.applicationNameTextBox.TabIndex = 9;
            this.applicationNameTextBox.Text = "MJ Timing Live";
            //
            // grantGoogleButton
            //
            this.grantGoogleButton.Location = new System.Drawing.Point(10, 98);
            this.grantGoogleButton.Name = "grantGoogleButton";
            this.grantGoogleButton.Size = new System.Drawing.Size(200, 24);
            this.grantGoogleButton.TabIndex = 10;
            this.grantGoogleButton.Text = "Grant Google permission…";
            this.grantGoogleButton.UseVisualStyleBackColor = true;
            this.grantGoogleButton.Click += new System.EventHandler(this.GrantGoogleButtonClick);
            //
            // syncEnabledCheckBox
            //
            // sync: own row under App name (right column) so it does not overlap labelAppName
            this.syncEnabledCheckBox.AutoSize = false;
            this.syncEnabledCheckBox.Checked = true;
            this.syncEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.syncEnabledCheckBox.Font = new System.Drawing.Font("Arial", 8.25F);
            this.syncEnabledCheckBox.Location = new System.Drawing.Point(370, 138);
            this.syncEnabledCheckBox.Name = "syncEnabledCheckBox";
            this.syncEnabledCheckBox.Size = new System.Drawing.Size(380, 22);
            this.syncEnabledCheckBox.TabIndex = 11;
            this.syncEnabledCheckBox.Text = "Push results when timing file updates";
            this.syncEnabledCheckBox.UseVisualStyleBackColor = true;
            this.syncEnabledCheckBox.CheckedChanged += new System.EventHandler(this.SyncEnabledCheckBoxCheckedChanged);
            //
            // forceFullRefreshButton
            //
            this.forceFullRefreshButton.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.forceFullRefreshButton.Location = new System.Drawing.Point(10, 126);
            this.forceFullRefreshButton.Name = "forceFullRefreshButton";
            this.forceFullRefreshButton.Size = new System.Drawing.Size(180, 24);
            this.forceFullRefreshButton.TabIndex = 12;
            this.forceFullRefreshButton.Text = "Force full refresh";
            this.forceFullRefreshButton.UseVisualStyleBackColor = true;
            this.forceFullRefreshButton.Click += new System.EventHandler(this.ForceFullRefreshButtonClick);
            //
            // labelSheetStatus
            //
            this.labelSheetStatus.AutoSize = true;
            this.labelSheetStatus.Location = new System.Drawing.Point(14, 330);
            this.labelSheetStatus.Name = "labelSheetStatus";
            this.labelSheetStatus.Size = new System.Drawing.Size(72, 13);
            this.labelSheetStatus.TabIndex = 51;
            this.labelSheetStatus.Text = "Sheet status:";
            //
            // sheetStatusLabel
            //
            this.sheetStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.sheetStatusLabel.AutoSize = true;
            this.sheetStatusLabel.Location = new System.Drawing.Point(14, 348);
            this.sheetStatusLabel.MaximumSize = new System.Drawing.Size(757, 0);
            this.sheetStatusLabel.Name = "sheetStatusLabel";
            this.sheetStatusLabel.Size = new System.Drawing.Size(0, 13);
            this.sheetStatusLabel.TabIndex = 52;
            this.sheetStatusLabel.UseMnemonic = false;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(783, 655);
            this.Controls.Add(this.groupBoxGoogle);
            this.Controls.Add(this.DataFolderTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Day2TextBox);
            this.Controls.Add(this.Day1TextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.scoringList);
            this.Controls.Add(this.scoresTextBox);
            this.Controls.Add(this.labelSheetStatus);
            this.Controls.Add(this.sheetStatusLabel);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "mjgooglesheets";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBoxGoogle.ResumeLayout(false);
            this.groupBoxGoogle.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.GroupBox groupBoxGoogle;
        private System.Windows.Forms.Button forceFullRefreshButton;
        private System.Windows.Forms.CheckBox syncEnabledCheckBox;
        private System.Windows.Forms.Button grantGoogleButton;
        private System.Windows.Forms.TextBox applicationNameTextBox;
        private System.Windows.Forms.Label labelAppName;
        private System.Windows.Forms.Label labelSheetsHint;
        private System.Windows.Forms.TextBox spreadsheetIdTextBox;
        private System.Windows.Forms.Label labelSpreadsheetId;
        private System.Windows.Forms.TextBox clientSecretTextBox;
        private System.Windows.Forms.Label labelClientSecret;
        private System.Windows.Forms.TextBox clientIdTextBox;
        private System.Windows.Forms.Label labelClientId;
        private System.Windows.Forms.Label sheetStatusLabel;
        private System.Windows.Forms.Label labelSheetStatus;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox DataFolderTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox Day2TextBox;
        private System.Windows.Forms.TextBox Day1TextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox OfficialRunsTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton set1RadioButton;
        private System.Windows.Forms.RadioButton set2RadioButton;
        private System.Windows.Forms.RadioButton day1Plusset2RadioButton;
        private System.Windows.Forms.RadioButton bestRunRadioButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckedListBox scoringList;
        private System.Windows.Forms.TextBox scoresTextBox;
    }
}
