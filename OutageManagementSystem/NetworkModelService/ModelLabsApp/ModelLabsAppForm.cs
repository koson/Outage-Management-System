﻿using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Outage.Common;
using Outage.Common.GDA;
using Outage.DataImporter.CIMAdapter;
using Outage.DataImporter.CIMAdapter.Manager;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Outage.DataImporter.ModelLabsApp
{
	public partial class ModelLabsAppForm : Form
	{
		private ILogger logger;

        protected ILogger Logger
        {
            get { return logger ?? (logger = LoggerWrapper.Instance); }
        }

		private readonly EnumDescs enumDescs;
        private readonly CIMAdapterClass adapter;

        private ConditionalValue<Delta> nmsDeltaResult;

        public ModelLabsAppForm()
		{
			enumDescs = new EnumDescs();
			adapter = new CIMAdapterClass();

			nmsDeltaResult = new ConditionalValue<Delta>(false, null);

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

		private async Task ConvertCIMXMLToDMSNetworkModelDelta()
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

				StringBuilder logBuilder = new StringBuilder();
				nmsDeltaResult = new ConditionalValue<Delta>(false, null);

				using (FileStream fs = File.Open(textBoxCIMFile.Text, FileMode.Open))
				{
					nmsDeltaResult = await adapter.CreateDelta(fs, (SupportedProfiles)(comboBoxProfile.SelectedItem), logBuilder);

                    Logger.LogInfo(logBuilder.ToString());
					richTextBoxReport.Text = logBuilder.ToString();
				}

				if (nmsDeltaResult.HasValue)
				{
					//// export delta to file
					using (XmlTextWriter xmlWriter = new XmlTextWriter(".\\deltaExport.xml", Encoding.UTF8))
					{
						xmlWriter.Formatting = Formatting.Indented;
						nmsDeltaResult.Value.ExportToXml(xmlWriter, enumDescs);
						xmlWriter.Flush();
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format("An error occurred.\n\n{0}", e.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogError("An error occurred.", e);
            }

			buttonApplyDelta.Enabled = nmsDeltaResult.HasValue;
            textBoxCIMFile.Text = string.Empty;
		}

		private async Task ApplyDMSNetworkModelDelta()
		{
			//// APPLY Delta
            if (!nmsDeltaResult.HasValue)
			{
				MessageBox.Show("No data is imported into delta object.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
				Logger.LogInfo("No data is imported into delta object.");
				return;
			}

			try
            {
                string log = await adapter.ApplyUpdates(nmsDeltaResult.Value);

                richTextBoxReport.AppendText(log);
				nmsDeltaResult = new ConditionalValue<Delta>(false, null);
                buttonApplyDelta.Enabled = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("An error occurred.\n\n{0}", e.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogError("An error occurred.", e);
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
			this.buttonConvertCIM.Enabled = false;
			Dispatcher.CurrentDispatcher.Invoke(ConvertCIMXMLToDMSNetworkModelDelta);
		}

        private void buttonApplyDeltaOnClick(object sender, EventArgs e)
        {
			this.buttonApplyDelta.Enabled = false;
			Dispatcher.CurrentDispatcher.Invoke(ApplyDMSNetworkModelDelta);
		}

        private void buttonExitOnClick(object sender, EventArgs e)
		{
			Close();
		}
    }
}
