﻿using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Outage.Common;
using Outage.Common.GDA;
using Outage.DataImporter.CIMAdapter;
using Outage.DataImporter.CIMAdapter.Manager;
using Outage.DataImporter.CIMAdapter.Importer;

namespace Outage.DataImporter.ModelLabsApp
{
	public partial class ModelLabsAppForm : Form
	{
		private ILogger logger;

        protected ILogger Logger
        {
            get { return logger ?? (logger = LoggerWrapper.Instance); }
        }

        private CIMAdapterClass adapter = new CIMAdapterClass();
        private Delta nmsDelta = null;
		private EnumDescs enumDescs = null;

        public ModelLabsAppForm()
		{
			enumDescs = new EnumDescs();
			InitializeComponent();
			InitGUIElements();
		}

		private void InitGUIElements()
		{
			buttonConvertCIM.Enabled = false;
            buttonApplyDelta.Enabled = false;

            comboBoxProfile.DataSource = Enum.GetValues(typeof(SupportedProfiles));
            comboBoxProfile.SelectedItem = SupportedProfiles.Outage;
            comboBoxProfile.Enabled = false; //// other profiles are not supported
        }

        private void ShowOpenCIMXMLFileDialog()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Open CIM Document File..";
			openFileDialog.Filter = "CIM-XML Files|*.xml;*.txt;*.rdf|All Files|*.*";
			openFileDialog.RestoreDirectory = true;

			DialogResult dialogResponse = openFileDialog.ShowDialog(this);
			if (dialogResponse == DialogResult.OK)
			{
				textBoxCIMFile.Text = openFileDialog.FileName;
				toolTipControl.SetToolTip(textBoxCIMFile, openFileDialog.FileName);
                buttonConvertCIM.Enabled = true;
                richTextBoxReport.Clear();
			}
			else
			{
                buttonConvertCIM.Enabled = false;
            }
		}

		private void ConvertCIMXMLToDMSNetworkModelDelta(EnumDescs enumDescs)
		{
			////SEND CIM/XML to ADAPTER
			try
			{
                if (textBoxCIMFile.Text == string.Empty)
                {
                    MessageBox.Show("Must enter CIM/XML file.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logger.LogInfo("Must enter CIM/XML file.");
                    return;
                }

				StringBuilder log = new StringBuilder();
				nmsDelta = null;
				using (FileStream fs = File.Open(textBoxCIMFile.Text, FileMode.Open))
				{
					var result = adapter.CreateDelta(fs, (SupportedProfiles)(comboBoxProfile.SelectedItem), log);

					if(result.HasValue)
                    {
						nmsDelta = result.Value;
						Logger.LogInfo(log.ToString());
						richTextBoxReport.Text = log.ToString();
                    }
				}

				if (nmsDelta != null)
				{
					//// export delta to file
					using (XmlTextWriter xmlWriter = new XmlTextWriter(".\\deltaExport.xml", Encoding.UTF8))
					{
						xmlWriter.Formatting = Formatting.Indented;
						nmsDelta.ExportToXml(xmlWriter, enumDescs);
						xmlWriter.Flush();
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format("An error occurred.\n\n{0}", e.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogError("An error occurred.", e);
            }

			buttonApplyDelta.Enabled = (nmsDelta != null);
            textBoxCIMFile.Text = string.Empty;
		}

		private void ApplyDMSNetworkModelDelta()
		{
			//// APPLY Delta
            if (nmsDelta != null)
            {
                try
                {
                    string log = adapter.ApplyUpdates(nmsDelta);
                    richTextBoxReport.AppendText(log);
                    nmsDelta = null;
                    buttonApplyDelta.Enabled = (nmsDelta != null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("An error occurred.\n\n{0}", e.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logger.LogError("An error occurred.", e);
                }
            }
            else
            {
                MessageBox.Show("No data is imported into delta object.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.LogInfo("No data is imported into delta object.");
            }
		}

		
		private void buttonBrowseLocationOnClick(object sender, EventArgs e)
		{
			ShowOpenCIMXMLFileDialog();
		}

		private void textBoxCIMFileOnDoubleClick(object sender, EventArgs e)
		{
			ShowOpenCIMXMLFileDialog();
		}

		private void buttonConvertCIMOnClick(object sender, EventArgs e)
		{
			ConvertCIMXMLToDMSNetworkModelDelta(enumDescs);
		}

        private void buttonApplyDeltaOnClick(object sender, EventArgs e)
        {
            ApplyDMSNetworkModelDelta();
        }

        private void buttonExitOnClick(object sender, EventArgs e)
		{
			Close();
		}
    }
}
