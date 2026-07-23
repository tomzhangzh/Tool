using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcPageManage : UserControl
    {
        private Database _db;
        private DataGridView dgvPages;
        private ComboBox cbFlows;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnAdd;
        private Button btnPaste;
        private Button btnDelete;
        private Button btnSave;
        private Button btnSetStart;
        private Button btnEditRegion;
        private Button btnEditAnnotation;
        private GroupBox grpDetail;
        private TextBox txtName;
        private RichTextBox txtRemark;
        private PictureBox pbPreview;
        private Label lblPageTitle;

        private int _selectedFlowId = 0;
        private int _selectedPageId = 0;

        public UcPageManage(Database db)
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
            lblPageTitle.Text = "页面管理";
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

            // 搜索
            txtSearch = new TextBox();
            txtSearch.Location = new Point(280, 50);
            txtSearch.Size = new Size(200, 28);
            txtSearch.Font = new Font("微软雅黑", 10F);
            txtSearch.PlaceholderText = "搜索页面...";
            this.Controls.Add(txtSearch);

            btnSearch = CreateButton("搜索", 490, 49, Color.FromArgb(0, 120, 215));
            btnSearch.Click += (s, e) => LoadPages();
            this.Controls.Add(btnSearch);

            // 操作按钮
            btnAdd = CreateButton("上传", 570, 49, Color.FromArgb(40, 167, 69));
            btnAdd.Click += (s, e) => AddPageFromFile();
            this.Controls.Add(btnAdd);

            btnPaste = CreateButton("粘贴", 650, 49, Color.FromArgb(23, 162, 184));
            btnPaste.Click += (s, e) => AddPageFromClipboard();
            this.Controls.Add(btnPaste);

            btnDelete = CreateButton("删除", 730, 49, Color.FromArgb(220, 53, 69));
            btnDelete.Click += (s, e) => DeletePage();
            this.Controls.Add(btnDelete);

            // 列表
            dgvPages = new DataGridView();
            dgvPages.Location = new Point(10, 90);
            dgvPages.Size = new Size(700, 350);
            dgvPages.AllowUserToAddRows = false;
            dgvPages.ReadOnly = true;
            dgvPages.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPages.MultiSelect = false;
            dgvPages.RowHeadersVisible = false;
            dgvPages.BackgroundColor = Color.White;
            dgvPages.BorderStyle = BorderStyle.FixedSingle;
            dgvPages.ColumnHeadersHeight = 35;
            dgvPages.Font = new Font("微软雅黑", 10F);
            dgvPages.SelectionChanged += (s, e) => OnPageSelected();
            this.Controls.Add(dgvPages);

            // 预览图
            var lblPreview = new Label();
            lblPreview.Text = "图片预览：";
            lblPreview.Location = new Point(720, 90);
            lblPreview.Size = new Size(100, 25);
            lblPreview.Font = new Font("微软雅黑", 10F);
            this.Controls.Add(lblPreview);

            pbPreview = new PictureBox();
            pbPreview.Location = new Point(720, 115);
            pbPreview.Size = new Size(840, 325);
            pbPreview.BorderStyle = BorderStyle.FixedSingle;
            pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pbPreview.BackColor = Color.White;
            this.Controls.Add(pbPreview);

            // 详情区
            grpDetail = new GroupBox();
            grpDetail.Text = "页面详情";
            grpDetail.Location = new Point(10, 450);
            grpDetail.Size = new Size(1550, 200);
            grpDetail.Font = new Font("微软雅黑", 10F);
            grpDetail.BackColor = Color.White;
            this.Controls.Add(grpDetail);

            var lblName = new Label();
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

            var lblRemark = new Label();
            lblRemark.Text = "备注：";
            lblRemark.Location = new Point(20, 75);
            lblRemark.Size = new Size(80, 25);
            lblRemark.Font = new Font("微软雅黑", 10F);
            lblRemark.TextAlign = ContentAlignment.MiddleLeft;
            grpDetail.Controls.Add(lblRemark);

            txtRemark = new RichTextBox();
            txtRemark.Location = new Point(105, 73);
            txtRemark.Size = new Size(1400, 60);
            txtRemark.Font = new Font("微软雅黑", 10F);
            grpDetail.Controls.Add(txtRemark);

            btnSave = CreateButton("保存", 105, 145, Color.FromArgb(0, 120, 215));
            btnSave.Click += (s, e) => SavePage();
            grpDetail.Controls.Add(btnSave);

            btnSetStart = CreateButton("设为起始页", 190, 145, Color.FromArgb(255, 193, 7), Color.Black);
            btnSetStart.Click += (s, e) => SetStartPage();
            grpDetail.Controls.Add(btnSetStart);

            btnEditRegion = CreateButton("编辑区域", 285, 145, Color.FromArgb(108, 117, 125));
            btnEditRegion.Click += (s, e) => EditRegion();
            grpDetail.Controls.Add(btnEditRegion);

            btnEditAnnotation = CreateButton("编辑注释", 370, 145, Color.FromArgb(108, 117, 125));
            btnEditAnnotation.Click += (s, e) => EditAnnotation();
            grpDetail.Controls.Add(btnEditAnnotation);

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
                LoadPages();
            }
        }

        private void LoadPages()
        {
            if (_selectedFlowId == 0) return;
            string keyword = txtSearch.Text.Trim();
            var pages = _db.GetPagesByFlowId(_selectedFlowId);

            dgvPages.Columns.Clear();
            dgvPages.Columns.Add("Id", "ID");
            dgvPages.Columns.Add("Name", "页面名称");
            dgvPages.Columns.Add("Remark", "备注");
            dgvPages.Columns["Id"].Width = 80;
            dgvPages.Columns["Name"].Width = 250;
            dgvPages.Columns["Remark"].Width = 350;
            dgvPages.Rows.Clear();

            foreach (var page in pages)
            {
                string remark = page.Remark ?? "";
                if (string.IsNullOrEmpty(keyword) || page.Name.Contains(keyword) || remark.Contains(keyword))
                {
                    dgvPages.Rows.Add(page.Id, page.Name, remark);
                }
            }
        }

        private void OnPageSelected()
        {
            if (dgvPages.SelectedRows.Count > 0)
            {
                var row = dgvPages.SelectedRows[0];
                _selectedPageId = Convert.ToInt32(row.Cells["Id"].Value);
                var page = _db.GetPageById(_selectedPageId);
                if (page != null)
                {
                    txtName.Text = page.Name;
                    txtRemark.Text = page.Remark ?? "";
                    if (page.ImageData != null && page.ImageData.Length > 0)
                    {
                        using (MemoryStream ms = new MemoryStream(page.ImageData))
                        {
                            pbPreview.Image = Image.FromStream(ms);
                        }
                    }
                    else
                    {
                        pbPreview.Image = null;
                    }
                }
            }
            else
            {
                _selectedPageId = 0;
                txtName.Text = "";
                txtRemark.Text = "";
                pbPreview.Image = null;
            }
        }

        private void AddPageFromFile()
        {
            if (_selectedFlowId == 0)
            {
                MessageBox.Show("请先选择一个流程", "提示");
                return;
            }
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    byte[] imageData = File.ReadAllBytes(ofd.FileName);
                    var page = new ProcessPage
                    {
                        FlowId = _selectedFlowId,
                        Name = Path.GetFileNameWithoutExtension(ofd.FileName),
                        ImageData = imageData,
                        ImageName = Path.GetFileName(ofd.FileName)
                    };
                    int id = _db.AddPage(page);
                    LoadPages();
                    SelectRowById(id);
                }
            }
        }

        private void AddPageFromClipboard()
        {
            if (_selectedFlowId == 0)
            {
                MessageBox.Show("请先选择一个流程", "提示");
                return;
            }
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("剪贴板中没有图片", "提示");
                return;
            }
            Image image = Clipboard.GetImage();
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var page = new ProcessPage
                {
                    FlowId = _selectedFlowId,
                    Name = "粘贴页面_" + DateTime.Now.ToString("HHmmss"),
                    ImageData = ms.ToArray(),
                    ImageName = "clipboard.png"
                };
                int id = _db.AddPage(page);
                LoadPages();
                SelectRowById(id);
            }
        }

        private void DeletePage()
        {
            if (_selectedPageId == 0)
            {
                MessageBox.Show("请先选择一个页面", "提示");
                return;
            }
            if (MessageBox.Show("确定删除此页面及所有关联数据？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _db.DeletePage(_selectedPageId);
                _selectedPageId = 0;
                LoadPages();
            }
        }

        private void SavePage()
        {
            if (_selectedPageId == 0)
            {
                MessageBox.Show("请先选择一个页面", "提示");
                return;
            }
            var page = _db.GetPageById(_selectedPageId);
            if (page != null)
            {
                page.Name = txtName.Text.Trim();
                page.Remark = txtRemark.Text.Trim();
                _db.UpdatePage(page);
                int selectedRow = dgvPages.SelectedRows[0].Index;
                LoadPages();
                if (selectedRow >= 0 && selectedRow < dgvPages.Rows.Count)
                {
                    dgvPages.Rows[selectedRow].Selected = true;
                }
                MessageBox.Show("保存成功", "提示");
            }
        }

        private void SetStartPage()
        {
            if (_selectedFlowId > 0 && _selectedPageId > 0)
            {
                _db.SetFlowStartPage(_selectedFlowId, _selectedPageId);
                MessageBox.Show("已设置为起始页面", "提示");
            }
        }

        private void EditRegion()
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
            }
        }

        private void EditAnnotation()
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
            }
        }

        private void SelectRowById(int id)
        {
            foreach (DataGridViewRow row in dgvPages.Rows)
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
