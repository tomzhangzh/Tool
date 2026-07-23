using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcAnnotationManage : UserControl
    {
        private Database _db;
        private DataGridView dgvAnnotations;
        private ComboBox cbFlows;
        private ComboBox cbPages;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnEdit;
        private Button btnSave;
        private GroupBox grpDetail;
        private TextBox txtText;
        private Label lblPageTitle;

        private int _selectedFlowId = 0;
        private int _selectedPageId = 0;
        private int _selectedAnnotationId = 0;
        private List<ProcessPage> _pages;

        public UcAnnotationManage(Database db)
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
            lblPageTitle.Text = "注释管理";
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
            txtSearch.PlaceholderText = "搜索注释...";
            this.Controls.Add(txtSearch);

            btnSearch = CreateButton("搜索", 710, 49, Color.FromArgb(0, 120, 215));
            btnSearch.Click += (s, e) => LoadAnnotations();
            this.Controls.Add(btnSearch);

            // 操作按钮
            btnAdd = CreateButton("编辑器", 790, 49, Color.FromArgb(40, 167, 69));
            btnAdd.Click += (s, e) => OpenEditor();
            this.Controls.Add(btnAdd);

            btnDelete = CreateButton("删除", 870, 49, Color.FromArgb(220, 53, 69));
            btnDelete.Click += (s, e) => DeleteAnnotation();
            this.Controls.Add(btnDelete);

            // 列表
            dgvAnnotations = new DataGridView();
            dgvAnnotations.Location = new Point(10, 90);
            dgvAnnotations.Size = new Size(1550, 350);
            dgvAnnotations.AllowUserToAddRows = false;
            dgvAnnotations.ReadOnly = true;
            dgvAnnotations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAnnotations.MultiSelect = false;
            dgvAnnotations.RowHeadersVisible = false;
            dgvAnnotations.BackgroundColor = Color.White;
            dgvAnnotations.BorderStyle = BorderStyle.FixedSingle;
            dgvAnnotations.ColumnHeadersHeight = 35;
            dgvAnnotations.Font = new Font("微软雅黑", 10F);
            dgvAnnotations.SelectionChanged += (s, e) => OnAnnotationSelected();
            this.Controls.Add(dgvAnnotations);

            // 详情区
            grpDetail = new GroupBox();
            grpDetail.Text = "注释详情";
            grpDetail.Location = new Point(10, 450);
            grpDetail.Size = new Size(1550, 180);
            grpDetail.Font = new Font("微软雅黑", 10F);
            grpDetail.BackColor = Color.White;
            this.Controls.Add(grpDetail);

            var lblText = new Label();
            lblText.Text = "注释文本：";
            lblText.Location = new Point(20, 35);
            lblText.Size = new Size(100, 25);
            lblText.Font = new Font("微软雅黑", 10F);
            lblText.TextAlign = ContentAlignment.MiddleLeft;
            grpDetail.Controls.Add(lblText);

            txtText = new TextBox();
            txtText.Location = new Point(125, 33);
            txtText.Size = new Size(800, 28);
            txtText.Font = new Font("微软雅黑", 10F);
            grpDetail.Controls.Add(txtText);

            btnSave = CreateButton("保存", 125, 70, Color.FromArgb(0, 120, 215));
            btnSave.Click += (s, e) => SaveAnnotation();
            grpDetail.Controls.Add(btnSave);

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
            btn.Size = new Size(80, 30);
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
                LoadAnnotations();
            }
        }

        private void LoadAnnotations()
        {
            if (_selectedPageId == 0) return;
            string keyword = txtSearch.Text.Trim();
            var annotations = _db.GetAnnotationsByPageId(_selectedPageId);

            dgvAnnotations.Columns.Clear();
            dgvAnnotations.Columns.Add("Id", "ID");
            dgvAnnotations.Columns.Add("Text", "注释文本");
            dgvAnnotations.Columns.Add("Position", "位置(X,Y)");
            dgvAnnotations.Columns.Add("Size", "宽x高");
            dgvAnnotations.Columns.Add("Arrow", "箭头终点");
            dgvAnnotations.Columns["Id"].Width = 80;
            dgvAnnotations.Columns["Text"].Width = 600;
            dgvAnnotations.Columns["Position"].Width = 150;
            dgvAnnotations.Columns["Size"].Width = 120;
            dgvAnnotations.Columns["Arrow"].Width = 200;
            dgvAnnotations.Rows.Clear();

            foreach (var ann in annotations)
            {
                string text = ann.Text ?? "";
                string arrow = ann.ArrowEndX.HasValue && ann.ArrowEndY.HasValue 
                    ? $"({ann.ArrowEndX},{ann.ArrowEndY})" : "无";
                if (string.IsNullOrEmpty(keyword) || text.Contains(keyword))
                {
                    dgvAnnotations.Rows.Add(ann.Id, text, $"({ann.TextX},{ann.TextY})", $"{ann.TextWidth}x{ann.TextHeight}", arrow);
                }
            }
        }

        private void OnAnnotationSelected()
        {
            if (dgvAnnotations.SelectedRows.Count > 0)
            {
                _selectedAnnotationId = Convert.ToInt32(dgvAnnotations.SelectedRows[0].Cells["Id"].Value);
                var ann = _db.GetAnnotationById(_selectedAnnotationId);
                if (ann != null)
                {
                    txtText.Text = ann.Text ?? "";
                }
            }
            else
            {
                _selectedAnnotationId = 0;
                txtText.Text = "";
            }
        }

        private void OpenEditor()
        {
            if (_selectedPageId == 0)
            {
                MessageBox.Show("请先选择一个页面", "提示");
                return;
            }
            var page = _db.GetPageById(_selectedPageId);
            if (page != null && page.ImageData != null)
            {
                using (MemoryStream ms = new MemoryStream(page.ImageData))
                {
                    Image img = Image.FromStream(ms);
                    using (FormAnnotationEditor editor = new FormAnnotationEditor(_db, _selectedPageId, img))
                    {
                        editor.ShowDialog(this);
                    }
                }
                LoadAnnotations();
            }
            else
            {
                MessageBox.Show("页面没有图片", "提示");
            }
        }

        private void DeleteAnnotation()
        {
            if (_selectedAnnotationId == 0)
            {
                MessageBox.Show("请先选择一个注释", "提示");
                return;
            }
            if (MessageBox.Show("确定删除此注释？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _db.DeleteAnnotation(_selectedAnnotationId);
                LoadAnnotations();
            }
        }

        private void SaveAnnotation()
        {
            if (_selectedAnnotationId == 0)
            {
                MessageBox.Show("请先选择一个注释", "提示");
                return;
            }
            var ann = _db.GetAnnotationById(_selectedAnnotationId);
            if (ann != null)
            {
                ann.Text = txtText.Text.Trim();
                _db.UpdateAnnotation(ann);
                int selectedRow = dgvAnnotations.SelectedRows[0].Index;
                LoadAnnotations();
                if (selectedRow >= 0 && selectedRow < dgvAnnotations.Rows.Count)
                {
                    dgvAnnotations.Rows[selectedRow].Selected = true;
                }
                MessageBox.Show("保存成功", "提示");
            }
        }
    }
}
