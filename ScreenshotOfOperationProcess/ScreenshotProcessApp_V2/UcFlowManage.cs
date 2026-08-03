using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcFlowManage : UserControl
    {
        private Database _db;
        private DataGridView dgvFlows;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnSave;
        private Button btnRun;
        private GroupBox grpDetail;
        private Label lblName;
        private Label lblDesc;
        private TextBox txtName;
        private TextBox txtDesc;
        private Label lblPageTitle;

        private int _selectedFlowId = 0;

        public UcFlowManage(Database db)
        {
            _db = db;
            InitializeComponent();
            this.Load += (s, e) => LoadFlows();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 标题
            lblPageTitle = new Label();
            lblPageTitle.Text = "流程管理";
            lblPageTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            lblPageTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblPageTitle.Location = new Point(10, 5);
            lblPageTitle.Size = new Size(300, 35);
            this.Controls.Add(lblPageTitle);

            // 搜索栏
            txtSearch = new TextBox();
            txtSearch.Location = new Point(10, 50);
            txtSearch.Size = new Size(300, 28);
            txtSearch.Font = new Font("微软雅黑", 10F);
            txtSearch.PlaceholderText = "输入流程名称搜索...";
            this.Controls.Add(txtSearch);

            btnSearch = new Button();
            btnSearch.Text = "搜索";
            btnSearch.Location = new Point(320, 49);
            btnSearch.Size = new Size(80, 30);
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.BackColor = Color.FromArgb(0, 120, 215);
            btnSearch.ForeColor = Color.White;
            btnSearch.Cursor = Cursors.Hand;
            btnSearch.Click += (s, e) => LoadFlows();
            this.Controls.Add(btnSearch);

            // 操作按钮
            btnAdd = CreateButton("新增", 410, 49, Color.FromArgb(40, 167, 69));
            btnAdd.Click += (s, e) => AddFlow();
            this.Controls.Add(btnAdd);

            btnDelete = CreateButton("删除", 490, 49, Color.FromArgb(220, 53, 69));
            btnDelete.Click += (s, e) => DeleteFlow();
            this.Controls.Add(btnDelete);

            btnSave = CreateButton("保存", 570, 49, Color.FromArgb(0, 120, 215));
            btnSave.Click += (s, e) => SaveFlow();
            this.Controls.Add(btnSave);

            btnRun = CreateButton("运行", 650, 49, Color.FromArgb(255, 193, 7), Color.Black);
            btnRun.Enabled = false;
            btnRun.Click += (s, e) => RunFlow();
            this.Controls.Add(btnRun);

            // 列表
            dgvFlows = new DataGridView();
            dgvFlows.Location = new Point(10, 90);
            dgvFlows.Size = new Size(1550, 350);
            dgvFlows.AllowUserToAddRows = false;
            dgvFlows.ReadOnly = true;
            dgvFlows.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFlows.MultiSelect = false;
            dgvFlows.RowHeadersVisible = false;
            dgvFlows.BackgroundColor = Color.White;
            dgvFlows.BorderStyle = BorderStyle.FixedSingle;
            dgvFlows.ColumnHeadersHeight = 35;
            dgvFlows.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvFlows.Font = new Font("微软雅黑", 10F);
            dgvFlows.SelectionChanged += (s, e) => OnFlowSelected();
            this.Controls.Add(dgvFlows);

            // 详情编辑区
            grpDetail = new GroupBox();
            grpDetail.Text = "流程详情";
            grpDetail.Location = new Point(10, 450);
            grpDetail.Size = new Size(1550, 200);
            grpDetail.Font = new Font("微软雅黑", 10F);
            grpDetail.BackColor = Color.White;
            this.Controls.Add(grpDetail);

            lblName = new Label();
            lblName.Text = "名称：";
            lblName.Location = new Point(20, 35);
            lblName.Size = new Size(80, 25);
            lblName.Font = new Font("微软雅黑", 10F);
            lblName.TextAlign = ContentAlignment.MiddleLeft;
            grpDetail.Controls.Add(lblName);

            txtName = new TextBox();
            txtName.Location = new Point(105, 33);
            txtName.Size = new Size(400, 28);
            txtName.Font = new Font("微软雅黑", 10F);
            grpDetail.Controls.Add(txtName);

            lblDesc = new Label();
            lblDesc.Text = "描述：";
            lblDesc.Location = new Point(20, 75);
            lblDesc.Size = new Size(80, 25);
            lblDesc.Font = new Font("微软雅黑", 10F);
            lblDesc.TextAlign = ContentAlignment.MiddleLeft;
            grpDetail.Controls.Add(lblDesc);

            txtDesc = new TextBox();
            txtDesc.Location = new Point(105, 73);
            txtDesc.Size = new Size(1400, 28);
            txtDesc.Font = new Font("微软雅黑", 10F);
            grpDetail.Controls.Add(txtDesc);

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

        private void LoadFlows()
        {
            string keyword = txtSearch.Text.Trim();
            var flows = _db.GetAllFlows();
            dgvFlows.Columns.Clear();
            dgvFlows.Columns.Add("Id", "ID");
            dgvFlows.Columns.Add("Name", "流程名称");
            dgvFlows.Columns.Add("Description", "描述");
            dgvFlows.Columns.Add("StartPageId", "起始页面ID");
            dgvFlows.Columns.Add("CreateTime", "创建时间");
            dgvFlows.Columns["Id"].Width = 80;
            dgvFlows.Columns["Name"].Width = 250;
            dgvFlows.Columns["Description"].Width = 600;
            dgvFlows.Columns["StartPageId"].Width = 120;
            dgvFlows.Columns["CreateTime"].Width = 200;
            dgvFlows.Rows.Clear();

            foreach (var flow in flows)
            {
                string desc = flow.Description ?? "";
                if (string.IsNullOrEmpty(keyword) || flow.Name.Contains(keyword) || desc.Contains(keyword))
                {
                    dgvFlows.Rows.Add(flow.Id, flow.Name, desc, flow.StartPageId, flow.CreateTime.ToString("yyyy-MM-dd HH:mm"));
                }
            }
        }

        private void OnFlowSelected()
        {
            if (dgvFlows.SelectedRows.Count > 0)
            {
                var row = dgvFlows.SelectedRows[0];
                _selectedFlowId = Convert.ToInt32(row.Cells["Id"].Value);
                txtName.Text = row.Cells["Name"].Value?.ToString() ?? "";
                txtDesc.Text = row.Cells["Description"].Value?.ToString() ?? "";
                btnRun.Enabled = true;
            }
            else
            {
                _selectedFlowId = 0;
                txtName.Text = "";
                txtDesc.Text = "";
                btnRun.Enabled = false;
            }
        }

        private void AddFlow()
        {
            txtName.Text = "";
            txtDesc.Text = "";
            _selectedFlowId = 0;
            txtName.Focus();

            // 直接新增一条空记录
            var flow = new ProcessFlow
            {
                Name = "新流程",
                Description = "",
                StartPageId = 0,
                CreateTime = DateTime.Now
            };
            int id = _db.AddFlow(flow);
            LoadFlows();
            SelectRowById(id);
        }

        private void DeleteFlow()
        {
            if (_selectedFlowId == 0)
            {
                MessageBox.Show("请先选择一个流程", "提示");
                return;
            }
            if (MessageBox.Show("确定删除此流程及所有关联数据？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _db.DeleteFlow(_selectedFlowId);
                _selectedFlowId = 0;
                LoadFlows();
            }
        }

        private void SaveFlow()
        {
            if (_selectedFlowId == 0)
            {
                MessageBox.Show("请先选择一个流程", "提示");
                return;
            }
            var flow = _db.GetFlowById(_selectedFlowId);
            if (flow != null)
            {
                flow.Name = txtName.Text.Trim();
                flow.Description = txtDesc.Text.Trim();
                _db.UpdateFlow(flow);
                int selectedRow = dgvFlows.SelectedRows[0].Index;
                LoadFlows();
                if (selectedRow >= 0 && selectedRow < dgvFlows.Rows.Count)
                {
                    dgvFlows.Rows[selectedRow].Selected = true;
                }
                MessageBox.Show("保存成功", "提示");
            }
        }

        private void RunFlow()
        {
            if (_selectedFlowId > 0)
            {
                var flow = _db.GetFlowById(_selectedFlowId);
                if (flow != null && flow.StartPageId > 0)
                {
                    FormRun formRun = new FormRun(_db);
                    formRun.SelectFlowAndStart(flow.Id);
                    formRun.Show();
                }
                else
                {
                    MessageBox.Show("该流程尚未设置开始页面", "提示");
                }
            }
        }

        private void SelectRowById(int id)
        {
            foreach (DataGridViewRow row in dgvFlows.Rows)
            {
                if (Convert.ToInt32(row.Cells["Id"].Value) == id)
                {
                    row.Selected = true;
                    return;
                }
            }
        }
    }
}
