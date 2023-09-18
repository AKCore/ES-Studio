﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading;
using AquaControls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;

namespace ES_GUI
{
    public enum MapParam
    {
        None,
        RPM,
        Throttle,
        ManifoldPressure,
        Gear,
        SpeedMPH,
        SpeedKMH,
        Clutch
    }

    public enum MapControlParam
    {
        None,
        RevLimit,
        IgnitionAdvance,
        ActiveCylinders
    }

    public enum MapParamType
    {
        X,
        Y
    }

    public class Map
    {
        public MapParam xParam = 0;
        public MapParam yParam = 0;
        public MapControlParam controlParam = 0;

        public string name;

        ESClient client;

        public bool enabled = false;
        private bool tableReadyEdit = true;

        private DataGridView gridView;
        private DataTable dataTable;

        private TabControl parentTabControl;
        private TabPage parentPage;
        private Label xName;
        private Label yName;
        private Label ctrlName;
        private Button generateMapButton;
        private Button enableButton;
        private Button disableButton;
        private Form mainForm;

        public MapController mapController;

        public PictureBox tableOverlay;

        public Map(Form parentForm) 
        {
            mainForm = parentForm;
        }

        private void manageGlobalControlParams(bool active)
        {
            switch (controlParam)
            {
                case MapControlParam.RevLimit:
                    client.edit.useRpmTable = active;
                    break;
                case MapControlParam.IgnitionAdvance:
                    client.edit.useIgnTable = active;
                    break;
                case MapControlParam.ActiveCylinders:
                    client.edit.useCylinderTable = active;
                    break;
            }
        }

        public void enable()
        {
            manageGlobalControlParams(true);
            enabled = true;
            tableOverlay.Parent = gridView;
            tableOverlay.Size = gridView.Size;;
        }

        public void disable() 
        { 
            manageGlobalControlParams(false);
            enabled = false;
            tableOverlay.Parent = null;
            tableOverlay.Size = new Size(0, 0);
        }

        public void Update()
        {
            if (!enabled) return;
            if (gridView.ColumnCount <= 0 || gridView.RowCount <= 0) return;

            mapController.xValue = GetMapValue(xParam);
            mapController.yValue = GetMapValue(yParam);
            mapController.UpdateTablePos();

            switch (controlParam)
            {
                case MapControlParam.RevLimit:
                    client.edit.customRevLimit = mapController.Pos2Val(true);
                    break;
                case MapControlParam.IgnitionAdvance:
                    client.edit.customSpark = mapController.Pos2Val();
                    break;
                case MapControlParam.ActiveCylinders:
                    client.edit.activeCylinderCount = (int)mapController.Pos2Val(true);
                    break;
            }
        }

        private double GetMapValue(MapParam param)
        {
            switch (param)
            {
                case MapParam.RPM:
                    return client.update.RPM;
                case MapParam.Throttle:
                    return client.update.tps;
                case MapParam.ManifoldPressure:
                    return client.update.manifoldPressure;
                case MapParam.Gear:
                    return client.update.gear + 1;
                case MapParam.SpeedMPH:
                    return client.update.vehicleSpeed /= (1000.0 / (60 * 60));
                case MapParam.SpeedKMH:
                    return client.update.vehicleSpeed /= (1609.344 / (60 * 60));
                case MapParam.Clutch:
                    return client.update.clutchPosition;
                default:
                    return 0;
            }
        }

        private void setControllerValue(MapParamType t, double val, bool setMax)
        {
            switch (t)
            {
                case MapParamType.X:
                    if (setMax)
                        mapController.maxX = val;
                    else
                        mapController.xValue = val;
                    break;
                case MapParamType.Y:
                    if (setMax) 
                        mapController.maxY = val;
                    else 
                        mapController.yValue = val;
                    break;
            }
        }

        private List<string> getParamDataList(int size, MapParam param, MapParamType t)
        {
            List<string> ret = new List<string>();
            for (int i = 0; i < size; i++)
            {
                switch (param)
                {
                    case MapParam.None: return new List<string>() { "0" };
                    case MapParam.RPM:
                        ret.Add(Math.Round((client.update.maxRPM / (size - 1)) * i).ToString());
                        setControllerValue(t, client.update.maxRPM, true);
                        break;
                    case MapParam.Throttle:
                        ret.Add((5 * i).ToString());
                        setControllerValue(t, 1, true);
                        break;
                    case MapParam.ManifoldPressure:
                        ret.Add(Math.Round((102000.0 / (size - 1)) * i).ToString());
                        setControllerValue(t, 102000, true);
                        break;
                    case MapParam.Gear:
                        ret.Add(i.ToString());
                        setControllerValue(t, 19, true);
                        break;
                    case MapParam.SpeedMPH:
                        ret.Add(Math.Round((200.0 / (size - 1)) * i).ToString());
                        setControllerValue(t, 200, true);
                        break;
                    case MapParam.SpeedKMH:
                        ret.Add(Math.Round((300.0 / (size - 1)) * i).ToString());
                        setControllerValue(t, 300, true);
                        break;
                    case MapParam.Clutch:
                        ret.Add((5 * i).ToString());
                        setControllerValue(t, 1, true);
                        break;
                    default: return new List<string>();
                }
            }
            return ret;
        }

        #region Creation
        public void Configure(MapParam xAxis, MapParam yAxis, MapControlParam outControl, string mapName)
        {
            xParam = xAxis;
            yParam = yAxis;
            controlParam = outControl;
            name = mapName;
        }

        public void Create(TabControl parentTab, ESClient inclient)
        {
            TabPage newTab = new TabPage();
            parentPage = newTab;
            newTab.Text = name;

            parentTabControl = parentTab;
            client = inclient;

            dataTable = new DataTable();
            mapController = new MapController();

            gridView = new DataGridView();
            gridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllHeaders;
            gridView.RowHeadersWidth = 75;
            gridView.AllowUserToAddRows = false;
            gridView.Location = new Point(19, 19);
            gridView.Size = new Size(1101, 380);
            newTab.Controls.Add(gridView);

            xName = new Label();
            xName.Text = xParam.ToString();
            xName.Location = new Point(16, 3);
            newTab.Controls.Add(xName);

            yName = new Label();
            yName.Text = string.Join("\n", yParam.ToString().ToCharArray());
            yName.Location = new Point(1, 40);
            yName.TextAlign = ContentAlignment.MiddleCenter;
            yName.Size = new Size(20, 250);
            newTab.Controls.Add(yName);

            generateMapButton = new Button();
            generateMapButton.Text = "Regenerate Map";
            generateMapButton.Location = new Point(1018, 405);
            generateMapButton.Size = new Size(102, 23);
            generateMapButton.Click += regenMapButton_Click;
            newTab.Controls.Add(generateMapButton);

            enableButton = new Button();
            enableButton.Text = "Enable";
            enableButton.Location = new Point(1018, 500);
            enableButton.Size = new Size(102, 23);
            enableButton.Click += enableButton_Click;
            newTab.Controls.Add(enableButton);

            disableButton = new Button();
            disableButton.Text = "Disable";
            disableButton.Location = new Point(1018, 529);
            disableButton.Size = new Size(102, 23);
            disableButton.Click += disableButton_Click;
            newTab.Controls.Add(disableButton);

            ctrlName = new Label();
            ctrlName.Text = controlParam.ToString();
            ctrlName.Location = new Point(16, 402);
            newTab.Controls.Add(ctrlName);

            tableOverlay = new PictureBox();
            tableOverlay.Parent = null;
            tableOverlay.Size = new Size(0, 0);
            tableOverlay.Location = new Point(0, 0);
            tableOverlay.BackColor = Color.Transparent;
            tableOverlay.Paint += new PaintEventHandler(tableOverlayPaint);

            gridView.CellValueChanged += gridView_CellValueChanged;
            gridView.KeyDown += gridView_KeyDown;

            parentTabControl.TabPages.Insert(parentTabControl.TabPages.Count - 1, newTab);
        }

        public void BuildTable(bool autoGradient = false)
        {
            if (client.isConnected)
            {
                tableReadyEdit = false;
                dataTable.Clear();
                dataTable = new DataTable();
                gridView.DataSource = dataTable;

                List<double> entries = new List<double>();

                switch (controlParam) //output control
                {
                    case MapControlParam.None: return;
                    case MapControlParam.RevLimit:
                        entries = Enumerable.Repeat(client.update.maxRPM, 20).ToList();
                        break;
                    case MapControlParam.IgnitionAdvance: 
                        entries = client.update.sparkTimingList;
                        autoGradient = true;
                        break;
                    case MapControlParam.ActiveCylinders:
                        entries = Enumerable.Repeat((double)client.update.cylinderCount, 21).ToList();
                        break;
                    default: return;
                }

                foreach (string t in getParamDataList(entries.Count, xParam, MapParamType.X))
                {
                    dataTable.Columns.Add(t);
                }

                List<double> last = new List<double>();
                last.AddRange(entries);

                double offset = 1;
                double min = last.OrderBy(x => x).ElementAt(1);

                List<List<double>> processed = new List<List<double>>();

                if (yParam != MapParam.None)
                {
                    for (int i = 0; i <= 20; i++)
                    {
                        List<double> t = new List<double>();

                        if (i == 0)
                        {
                            processed.Add(entries);
                            continue;
                        }

                        for (int s = 0; s < last.Count; s++)
                        {
                            if (s != 0 && autoGradient)
                                t.Add((last[s] - (offset)).Clamp(min, 100000f));
                            else
                                t.Add(last[s]);
                        }

                        processed.Add(t);
                        last.Clear();
                        last.AddRange(t);
                    }

                    processed.Reverse();
                } else
                {
                    processed.Add(entries);
                }

                List<string> yParamData = getParamDataList(processed.Count, yParam, MapParamType.Y);

                for (int i = 0; i <= processed.Count - 1; i++)
                {
                    dataTable.Rows.Add(processed[i].Select(x => x.ToString()).ToArray());
                    gridView.Rows[i].HeaderCell.Value = yParamData[i];
                }

                mapController.cellRectangle = gridView.GetCellDisplayRectangle(gridView.ColumnCount - 1, gridView.RowCount - 1, false);
                mapController.cellOffset.X = gridView.Rows[0].HeaderCell.Size.Width;
                mapController.cellOffset.Y = gridView.Columns[0].HeaderCell.Size.Height;
                mapController.tablePoint.Width = mapController.cellRectangle.Width;
                mapController.tablePoint.Height = mapController.cellRectangle.Height;

                updateHeatMap();
                BuildData();
                tableReadyEdit = true;
            }
        }
        #endregion

        #region Buttons

        private void regenMapButton_Click(object sender, EventArgs e)
        {
            BuildTable();
        }

        private void enableButton_Click(object sender, EventArgs e)
        {
            enable();
        }

        private void disableButton_Click(object sender, EventArgs e)
        {
            disable();
        }

        #endregion

        #region HeatMap
        private Color HeatMap(float value, float max)
        {
            if (value < 0)
            {
                return Color.FromArgb(255, 0, 0, 255);
            }
            int r, g, b;
            float val = value / max;
            if (val > 1)
                val = 1;
            if (val > 0.5f)
            {
                val = (val - 0.5f) * 2;
                r = Convert.ToByte(255 * val);
                g = Convert.ToByte(255 * (1 - val));
                b = 0;
            }
            else
            {
                val = val * 2;
                r = 0;
                g = Convert.ToByte(255 * val);
                b = Convert.ToByte(255 * (1 - val));
            }
            return Color.FromArgb(255, r, g, b);
        }

        public void updateHeatMap()
        {
            Thread t = new Thread(() =>
            {
                gridView.Invoke((MethodInvoker)delegate
                {
                    foreach (DataGridViewRow row in gridView.Rows)
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            float i = 0f;
                            float.TryParse(cell.Value.ToString(), out i);
                            Color col = HeatMap(i, 40f);
                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.BackColor = col;
                            style.ForeColor = Color.Black;
                            style.Font = new Font(mainForm.Font.Name, mainForm.Font.Size, FontStyle.Bold);
                            cell.Style = style;
                        }
                    }
                });
                Thread.Sleep(10);
            });
            t.Start();
        }
        #endregion

        #region TableUpdates
        private void tableOverlayPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Pen p = new Pen(Color.Yellow, 3))
            {
                g.DrawEllipse(p, mapController.tablePoint);
            }
        }

        private void gridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (tableReadyEdit)
            {
                DataGridViewCell cell = gridView[e.ColumnIndex, e.RowIndex];
                float i = 0f;
                float.TryParse(cell.Value.ToString(), out i);
                Color col = HeatMap(i, 40f);
                DataGridViewCellStyle style = new DataGridViewCellStyle();
                style.BackColor = col;
                style.ForeColor = Color.Black;
                style.Font = new Font(mainForm.Font.Name, mainForm.Font.Size, FontStyle.Bold);
                cell.Style = style;
                BuildData();
            }
        }

        private void BuildData()
        {
            gridView.Invoke((MethodInvoker)delegate
            {
                List<MapCell> cellList = new List<MapCell>();
                foreach (DataGridViewRow row in gridView.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        float i = 0f;
                        float.TryParse(cell.Value.ToString(), out i);
                        MapCell sc = new MapCell();
                        Rectangle pos = gridView.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, false);
                        sc.Position = pos;
                        sc.Value = i;
                        cellList.Add(sc);
                    }
                }
                mapController.CacheData(cellList);
            });
        }

        private void gridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.V)
                {
                    foreach (DataGridViewCell cell in gridView.SelectedCells)
                    {
                        cell.Value = Clipboard.GetText();
                    }
                    return;
                }
                if (e.KeyCode == Keys.C)
                {
                    DataGridViewCell c = gridView.SelectedCells[0];
                    Clipboard.SetText(c.Value.ToString());
                    return;
                }
            }
        }
        #endregion
    }
}