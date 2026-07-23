using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcRegionManage : UserControl
    {
        private Database _db;
        private DataGridView dgvRegions;
        private ComboBox cbFlows;
        private ComboBox cbPages;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnEdit;
        private GroupBox grpDetail;
        private TextBox txtRemark;
        private ComboBox cbTargetPage;
        private Label lblPageTitle;

        private int _selectedFlowId = 0;
        private int _selectedPageId = 0;
        private List<ProcessPage> _pages;

        public UcRegionManage(Database db)
        {
            _db = db;
            InitializeComponent();
            LoadFlowsCombo();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 标题
            lblPageTitle = new Label();
            lblPageTitle.Text = "区域管理";
            lblPageTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            lblPageTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblPageTitle.Location = new Point(10, 5);
            lblPageTitle.Size = new Size(300, 35);
            this.Controls.Add(lblPageTitle);

            // 流程选择
            var lblFlow = new Label();
            lblFlow.Text = "流程：";
            lblFlow.Location = new Point(10, 52);
            lblFlow.Size = new Size(50, 28);
            lblFlow.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblFlow);

            cbFlows = new ComboBox();
            cbFlows.Location = new Point(60, 50);
            cbFlows.Size = new Size(200, 28);
            cbFlows.DropDownStyle = ComboBoxStyle.DropDownList;
            cbFlows.SelectedIndexChanged += (s, e) => OnFlowChanged();
            this.Controls.Add(cbFlows);

            // 页面选择
            var lblPage = new Label();
            lblPage.Text = "页面：";
            lblPage.Location = new Point(280, 52);
            lblPage.Size = new Size(50, 28);
            lblPage.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblPage);

            cbPages = new ComboBox();
            cbPages.Location = new Point(330, 50);
            cbPages.Size = new Size(200, 28);
            cbPages.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPages.SelectedIndexChanged += (s, e) => OnPageChanged();
            this.Controls.Add(cbPages);

            // 搜索
            txtSearch = new TextBox();
            txtSearch.Location = new Point(550, 50);
            txtSearch.Size = new Size(150, 28);
            txtSearch.Font = new Font("微软雅黑", 10F);
            txtSearch.PlaceholderText = "搜索备注...";
            this.Controls.Add(txtSearch);

            btnSearch = CreateButton("搜索", 710, 49, Color.FromArgb(0, 120, 215));
            btnSearch.Click += (s, e) => LoadRegions();
            this.Controls.Add(btnSearch);

            // 操作按钮
            btnAdd = CreateButton("新增", 790, 49, Color.FromArgb(40, 167, 69));
            btnAdd.Click += (s, e) => AddRegion();
            this.Controls.Add(btnAdd);

            btnEdit = CreateButton("编辑", 870, 49, Color.FromArgb(0, 120, 215));
            btnEdit.Click += (s, e) => EditRegion();
            this.Controls.Add(btnEdit);

            btnDelete = CreateButton("删除", 950, 49, Color.FromArgb(220, 53, 69));
            btnDelete.Click += (s, e) => DeleteRegion();
            this.Controls.Add(btnDelete);

            // 列表
            dgvRegions = new DataGridView();
            dgvRegions.Location = new Point(10, 90);
            dgvRegions.Size = new Size(1550, 350);
            dgvRegions.AllowUserToAddRows = false;
            dgvRegions.ReadOnly = true;
            dgvRegions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRegions.MultiSelect = false;
            dgvRegions.RowHeadersVisible = false;
            dgvRegions.BackgroundColor = Color.White;
            dgvRegions.BorderStyle = BorderStyle.FixedSingle;
            dgvRegions.ColumnHeadersHeight = 35;
            dgvRegions.Font = new Font("微软雅黑", 10F);
            dgvRegions.SelectionChanged += (s, e) => OnRegionSelected();
            this.Controls.Add(dgvRegions);

            // 详情区
            grpDetail = new GroupBox();
            grpDetail.Text = "区域详情";
            grpDetail.Location = new Point(10, 450);
            grpDetail.Size = new Size(1550, 180);
            grpDetail.Font = new Font("微软雅黑", 10F);
            grpDetail.BackColor = Color.White;
            this.Controls.Add(grpDetail);

            var lblTarget = new Label();
            lblTarget.Text = "链接到页面：";
            lblTarget.Location = new Point(20, 35);
            lblTarget.Size = new Size(110, 25);
            lblTarget.Font = new Font("微软雅黑", 10F);
            lblTarget.TextAlign = ContentAlignment.MiddleLeft;
            grpDetail.Controls.Add(lblTarget);

            cbTargetPage = new ComboBox();
            cbTargetPage.Location = new Point(135, 33);
            cbTargetPage.Size = new Size(400, 28);
            cbTargetPage.DropDownStyle = ComboBoxStyle.DropDownList;
            grpDetail.Controls.Add(cbTargetPage);

            var lblRemark = new Label();
            lblRemark.Text = "备注：";
            lblRemark.Location = new Point(20, 75);
            lblRemark.Size = new Size(110, 25);
            lblRemark.Font = new Font("微软雅黑", 10F);
            lblRemark.TextAlign = ContentAlignment.MiddleLeft;
            grpDetail.Controls.Add(lblRemark);

            txtRemark = new TextBox();
            txtRemark.Location = new Point(135, 73);
            txtRemark.Size = new Size(800, 28);
            txtRemark.Font = new Font("微软雅黑", 10F);
            grpDetail.Controls.Add(txtRemark);

            var btnQuickSave = CreateButton("保存", 135, 110, Color.FromArgb(0, 120, 215));
            btnQuickSave.Click += (s, e) => QuickSaveRegion();
            grpDetail.Controls.Add(btnQuickSave);

            this.ResumeLayout(false);
        }

        private Button CreateButton(string text, int x, int y, Color backColor)
        {
            return CreateButton(text, x, y, backColor, Color.White);
        }

        private Button CreateButton(string text, int x, int y, Color backColor, Color foreColor)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(75, 30);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("微软雅黑", 9F);
            return btn;
        }

        private void LoadFlowsCombo()
        {
            cbFlows.Items.Clear();
            var flows = _db.GetAllFlows();
            foreach (var flow in flows)
            {
                cbFlows.Items.Add(new FlowItem(flow.Id, flow.Name, flow.StartPageId));
            }
            if (cbFlows.Items.Count > 0)
            {
                cbFlows.SelectedIndex = 0;
            }
        }

        private void OnFlowChanged()
        {
            if (cbFlows.SelectedItem is FlowItem item)
            {
                _selectedFlowId = item.Id;
                LoadPagesCombo();
            }
        }

        private void LoadPagesCombo()
        {
            cbPages.Items.Clear();
            _pages = _db.GetPagesByFlowId(_selectedFlowId);
            foreach (var page in _pages)
            {
                cbPages.Items.Add(new PageItem(page.Id, page.Name));
            }
            if (cbPages.Items.Count > 0)
            {
                cbPages.SelectedIndex = 0;
            }
        }

        private void OnPageChanged()
        {
            if (cbPages.SelectedItem is PageItem item)
            {
                _selectedPageId = item.Id;
                LoadRegions();
            }
        }

        private void LoadRegions()
        {
            if (_selectedPageId == 0) return;
            string keyword = txtSearch.Text.Trim();
            var regions = _db.GetRegionsByPageId(_selectedPageId);

            dgvRegions.Columns.Clear();
            dgvRegions.Columns.Add("Id", "ID");
            dgvRegions.Columns.Add("Position", "坐标(X,Y)");
            dgvRegions.Columns.Add("Size", "宽x高");
            dgvRegions.Columns.Add("Target", "链接(页面/流程)");
            dgvRegions.Columns.Add("TargetName", "名称");
            dgvRegions.Columns.Add("Remark", "备注");
            dgvRegions.Columns["Id"].Width = 80;
            dgvRegions.Columns["Position"].Width = 150;
            dgvRegions.Columns["Size"].Width = 120;
            dgvRegions.Columns["Target"].Width = 150;
            dgvRegions.Columns["TargetName"].Width = 300;
            dgvRegions.Columns["Remark"].Width = 500;
            dgvRegions.Rows.Clear();

            foreach (var region in regions)
            {
                string targetInfo = "无";
                string targetName = "";
                if (region.TargetPageId.HasValue)
                {
                    var targetPage = _pages?.Find(p => p.Id == region.TargetPageId.Value);
                    if (targetPage != null)
                    {
                        targetInfo = "页面";
                        targetName = targetPage.Name;
                    }
                }
                string remark = region.Remark ?? "";
                if (string.IsNullOrEmpty(keyword) || remark.Contains(keyword))
                {
                    dgvRegions.Rows.Add(region.Id, $"({region.X},{region.Y})", $"{region.Width}x{region.Height}", targetInfo, targetName, remark);
                }
            }

            // 加载目标页面下拉
            LoadTargetPageCombo();
        }

        private void LoadTargetPageCombo()
        {
            cbTargetPage.Items.Clear();
            cbTargetPage.Items.Add(new PageItem(0, "无链接"));
            if (_pages != null)
            {
                foreach (var page in _pages)
                {
                    cbTargetPage.Items.Add(new PageItem(page.Id, page.Name));
                }
            }
        }

        private void OnRegionSelected()
        {
            if (dgvRegions.SelectedRows.Count > 0)
            {
                var row = dgvRegions.SelectedRows[0];
                int regionId = Convert.ToInt32(row.Cells["Id"].Value);
                var region = _db.GetRegionById(regionId);
                if (region != null)
                {
                    txtRemark.Text = region.Remark ?? "";
                    // 选中目标页面
                    if (region.TargetPageId.HasValue)
                    {
                        for (int i = 0; i < cbTargetPage.Items.Count; i++)
                        {
                            if (cbTargetPage.Items[i] is PageItem pi && pi.Id == region.TargetPageId.Value)
                            {
                                cbTargetPage.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        cbTargetPage.SelectedIndex = 0;
                    }
                }
            }
        }

        private void AddRegion()
        {
            if (_selectedPageId == 0)
            {
                MessageBox.Show("请先选择一个页面", "提示");
                return;
            }
            var pages = _db.GetPagesByFlowId(_selectedFlowId);
            FormRegionEditor editor = new FormRegionEditor(_db, _selectedPageId, pages);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                LoadRegions();
            }
        }

        private void EditRegion()
        {
            if (dgvRegions.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个区域", "提示");
                return;
            }
            var pages = _db.GetPagesByFlowId(_selectedFlowId);
            FormRegionEditor editor = new FormRegionEditor(_db, _selectedPageId, pages);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                LoadRegions();
            }
        }

        private void DeleteRegion()
        {
            if (dgvRegions.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个区域", "提示");
                return;
            }
            int regionId = Convert.ToInt32(dgvRegions.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("确定删除此区域？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _db.DeleteRegion(regionId);
                LoadRegions();
            }
        }

        private void QuickSaveRegion()
        {
            if (dgvRegions.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个区域", "提示");
                return;
            }
            int regionId = Convert.ToInt32(dgvRegions.SelectedRows[0].Cells["Id"].Value);
            var region = _db.GetRegionById(regionId);
            if (region != null)
            {
                region.Remark = txtRemark.Text.Trim();
                if (cbTargetPage.SelectedItem is PageItem pi)
                {
                    region.TargetPageId = pi.Id == 0 ? null : (int?)pi.Id;
                }
                _db.UpdateRegion(region);
                int selectedRow = dgvRegions.SelectedRows[0].Index;
                LoadRegions();
                if (selectedRow >= 0 && selectedRow < dgvRegions.Rows.Count)
                {
                    dgvRegions.Rows[selectedRow].Selected = true;
                }
                MessageBox.Show("保存成功", "提示");
            }
        }
    }
}
