using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public partial class FormManage : Form
    {
        private Database _db;
        private int _selectedFlowId = 0;
        private int _selectedPageId = 0;
        private List<ProcessFlow> _flows;
        private List<ProcessPage> _pages;
        private Label label6;
        private List<PageRegion> _regions;

        public FormManage(Database db)
        {
            InitializeComponent();
            _db = db;
            LoadFlows();
        }

        private void LoadFlows()
        {
            _flows = _db.GetAllFlows();
            lbFlows.Items.Clear();
            foreach (var flow in _flows)
            {
                lbFlows.Items.Add($"{flow.Id} - {flow.Name}");
            }
            if (_flows.Count > 0)
            {
                lbFlows.SelectedIndex = 0;
            }
        }

        private void LoadPages(int flowId)
        {
            _pages = _db.GetPagesByFlowId(flowId);
            lbPages.Items.Clear();
            foreach (var page in _pages)
            {
                lbPages.Items.Add($"{page.Id} - {page.Name}");
            }
            if (_pages.Count > 0)
            {
                lbPages.SelectedIndex = 0;
            }
            else
            {
                pbImage.Image = null;
                _regions = new List<PageRegion>();
                LoadRegionsToList();
            }
        }

        private void LoadRegions(int pageId)
        {
            _regions = _db.GetRegionsByPageId(pageId);
            LoadRegionsToList();
        }

        private void LoadRegionsToList()
        {
            lbRegions.Items.Clear();
            foreach (var region in _regions)
            {
                string targetInfo = "无链接";
                if (region.TargetPageId.HasValue)
                {
                    var targetPage = _pages.Find(p => p.Id == region.TargetPageId.Value);
                    if (targetPage != null)
                    {
                        var flow = _flows.Find(f => f.StartPageId == region.TargetPageId.Value);
                        if (flow != null)
                        {
                            targetInfo = $"(流程) {flow.Name}";
                        }
                        else
                        {
                            targetInfo = $"(页面) {targetPage.Name}";
                        }
                    }
                    else
                    {
                        targetInfo = $"(页面){region.TargetPageId}";
                    }
                }
                lbRegions.Items.Add($"({region.X},{region.Y}) {region.Width}x{region.Height} {targetInfo} 【{region.Id}】");
            }
        }

        private void lbFlows_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbFlows.SelectedIndex >= 0)
            {
                _selectedFlowId = _flows[lbFlows.SelectedIndex].Id;
                LoadPages(_selectedFlowId);
                var flow = _flows[lbFlows.SelectedIndex];
                txtFlowName.Text = flow.Name;
                txtFlowDesc.Text = flow.Description;
            }
        }

        private void lbPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbPages.SelectedIndex >= 0)
            {
                _selectedPageId = _pages[lbPages.SelectedIndex].Id;
                var page = _pages[lbPages.SelectedIndex];
                txtPageName.Text = page.Name;
                txtPageRemark.Text = page.Remark ?? "";

                if (page.ImageData != null && page.ImageData.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(page.ImageData))
                    {
                        pbImage.Image = Image.FromStream(ms);
                    }
                }
                LoadRegions(_selectedPageId);
            }
        }

        private void btnAddFlow_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入流程名称:", "新建流程", "新流程");
            if (!string.IsNullOrEmpty(name))
            {
                ProcessFlow flow = new ProcessFlow
                {
                    Name = name,
                    Description = "",
                    StartPageId = 0,
                    CreateTime = DateTime.Now
                };
                int id = _db.AddFlow(flow);
                LoadFlows();
                lbFlows.SelectedIndex = lbFlows.Items.Count - 1;
            }
        }

        private void btnDelFlow_Click(object sender, EventArgs e)
        {
            if (_selectedFlowId > 0 && MessageBox.Show("确定删除此流程及所有关联数据？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _db.DeleteFlow(_selectedFlowId);
                LoadFlows();
            }
        }

        private void btnSaveFlow_Click(object sender, EventArgs e)
        {
            if (_selectedFlowId > 0)
            {
                var flow = _db.GetFlowById(_selectedFlowId);
                if (flow != null)
                {
                    flow.Name = txtFlowName.Text;
                    flow.Description = txtFlowDesc.Text;
                    _db.UpdateFlow(flow);
                    int currentIndex = lbFlows.SelectedIndex;
                    LoadFlows();
                    if (currentIndex >= 0 && currentIndex < lbFlows.Items.Count)
                    {
                        lbFlows.SelectedIndex = currentIndex;
                    }
                }
            }
        }

        private void btnAddPage_Click(object sender, EventArgs e)
        {
            if (_selectedFlowId == 0)
            {
                MessageBox.Show("请先选择一个流程");
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string name = Microsoft.VisualBasic.Interaction.InputBox("请输入页面名称:", "新建页面", "新页面");
                    if (!string.IsNullOrEmpty(name))
                    {
                        byte[] imageData = File.ReadAllBytes(ofd.FileName);
                        ProcessPage page = new ProcessPage
                        {
                            FlowId = _selectedFlowId,
                            Name = name,
                            ImageData = imageData,
                            ImageName = Path.GetFileName(ofd.FileName)
                        };
                        int id = _db.AddPage(page);
                        LoadPages(_selectedFlowId);
                    }
                }
            }
        }

        private void btnPastePage_Click(object sender, EventArgs e)
        {
            if (_selectedFlowId == 0)
            {
                MessageBox.Show("请先选择一个流程");
                return;
            }

            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("剪贴板中没有图片");
                return;
            }

            string name = Microsoft.VisualBasic.Interaction.InputBox("请输入页面名称:", "从剪贴板新建页面", "新页面");
            if (!string.IsNullOrEmpty(name))
            {
                Image image = Clipboard.GetImage();
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] imageData = ms.ToArray();

                    ProcessPage page = new ProcessPage
                    {
                        FlowId = _selectedFlowId,
                        Name = name,
                        ImageData = imageData,
                        ImageName = "clipboard.png"
                    };
                    int id = _db.AddPage(page);
                    LoadPages(_selectedFlowId);
                }
            }
        }

        private void btnDelPage_Click(object sender, EventArgs e)
        {
            if (_selectedPageId > 0 && MessageBox.Show("确定删除此页面及所有关联区域？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _db.DeletePage(_selectedPageId);
                LoadPages(_selectedFlowId);
            }
        }

        private void btnSavePage_Click(object sender, EventArgs e)
        {
            if (_selectedPageId > 0)
            {
                var page = _db.GetPageById(_selectedPageId);
                if (page != null)
                {
                    page.Name = txtPageName.Text;
                    page.Remark = txtPageRemark.Text;
                    _db.UpdatePage(page);
                    int currentIndex = lbPages.SelectedIndex;
                    LoadPages(_selectedFlowId);
                    if (currentIndex >= 0 && currentIndex < lbPages.Items.Count)
                    {
                        lbPages.SelectedIndex = currentIndex;
                    }
                    MessageBox.Show("页面信息已保存");
                }
            }
            else
            {
                MessageBox.Show("请先选择一个页面");
            }
        }

        private void btnEditRegion_Click(object sender, EventArgs e)
        {
            if (_selectedPageId == 0)
            {
                MessageBox.Show("请先选择一个页面");
                return;
            }

            FormRegionEditor editor = new FormRegionEditor(_db, _selectedPageId, _pages);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                LoadRegions(_selectedPageId);
            }
        }

        private void btnDelRegion_Click(object sender, EventArgs e)
        {
            if (lbRegions.SelectedIndex >= 0 && _regions.Count > lbRegions.SelectedIndex)
            {
                int regionId = _regions[lbRegions.SelectedIndex].Id;
                if (MessageBox.Show("确定删除此区域？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _db.DeleteRegion(regionId);
                    LoadRegions(_selectedPageId);
                }
            }
        }

        private void btnSetStartPage_Click(object sender, EventArgs e)
        {
            if (_selectedFlowId > 0 && _selectedPageId > 0)
            {
                var flow = _db.GetFlowById(_selectedFlowId);
                if (flow != null)
                {
                    flow.StartPageId = _selectedPageId;
                    _db.UpdateFlow(flow);
                    MessageBox.Show("已设置开始页面");
                }
            }
        }

        #region Windows Form Designer generated code
        private System.ComponentModel.IContainer components = null;
        private ListBox lbFlows;
        private ListBox lbPages;
        private ListBox lbRegions;
        private Button btnAddFlow;
        private Button btnDelFlow;
        private Button btnSaveFlow;
        private Button btnAddPage;
        private Button btnDelPage;
        private Button btnPastePage;
        private Button btnSavePage;
        private Button btnEditRegion;
        private Button btnDelRegion;
        private Button btnSetStartPage;
        private PictureBox pbImage;
        private TextBox txtFlowName;
        private TextBox txtFlowDesc;
        private TextBox txtPageName;
        private RichTextBox txtPageRemark;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lbFlows = new ListBox();
            lbPages = new ListBox();
            lbRegions = new ListBox();
            btnAddFlow = new Button();
            btnDelFlow = new Button();
            btnSaveFlow = new Button();
            btnAddPage = new Button();
            btnDelPage = new Button();
            btnPastePage = new Button();
            btnSavePage = new Button();
            btnEditRegion = new Button();
            btnDelRegion = new Button();
            pbImage = new PictureBox();
            txtFlowName = new TextBox();
            txtFlowDesc = new TextBox();
            txtPageName = new TextBox();
            txtPageRemark = new RichTextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            btnSetStartPage = new Button();
            label6 = new Label();
            ((System.ComponentModel.ISupportInitialize)pbImage).BeginInit();
            SuspendLayout();
            // 
            // lbFlows
            // 
            lbFlows.ItemHeight = 20;
            lbFlows.Location = new Point(20, 45);
            lbFlows.Name = "lbFlows";
            lbFlows.Size = new Size(325, 264);
            lbFlows.TabIndex = 21;
            lbFlows.SelectedIndexChanged += lbFlows_SelectedIndexChanged;
            // 
            // lbPages
            // 
            lbPages.ItemHeight = 20;
            lbPages.Location = new Point(20, 388);
            lbPages.Name = "lbPages";
            lbPages.Size = new Size(325, 304);
            lbPages.TabIndex = 20;
            lbPages.SelectedIndexChanged += lbPages_SelectedIndexChanged;
            // 
            // lbRegions
            // 
            lbRegions.ItemHeight = 20;
            lbRegions.Location = new Point(20, 822);
            lbRegions.Name = "lbRegions";
            lbRegions.Size = new Size(325, 304);
            lbRegions.TabIndex = 19;
            // 
            // btnAddFlow
            // 
            btnAddFlow.Location = new Point(20, 330);
            btnAddFlow.Name = "btnAddFlow";
            btnAddFlow.Size = new Size(75, 30);
            btnAddFlow.TabIndex = 18;
            btnAddFlow.Text = "添加";
            btnAddFlow.Click += btnAddFlow_Click;
            // 
            // btnDelFlow
            // 
            btnDelFlow.Location = new Point(100, 330);
            btnDelFlow.Name = "btnDelFlow";
            btnDelFlow.Size = new Size(75, 30);
            btnDelFlow.TabIndex = 17;
            btnDelFlow.Text = "删除";
            btnDelFlow.Click += btnDelFlow_Click;
            // 
            // btnSaveFlow
            // 
            btnSaveFlow.Location = new Point(180, 330);
            btnSaveFlow.Name = "btnSaveFlow";
            btnSaveFlow.Size = new Size(75, 30);
            btnSaveFlow.TabIndex = 16;
            btnSaveFlow.Text = "保存";
            btnSaveFlow.Click += btnSaveFlow_Click;
            // 
            // btnAddPage
            // 
            btnAddPage.Location = new Point(25, 754);
            btnAddPage.Name = "btnAddPage";
            btnAddPage.Size = new Size(75, 30);
            btnAddPage.TabIndex = 15;
            btnAddPage.Text = "添加";
            btnAddPage.Click += btnAddPage_Click;
            // 
            // btnDelPage
            // 
            btnDelPage.Location = new Point(105, 754);
            btnDelPage.Name = "btnDelPage";
            btnDelPage.Size = new Size(75, 30);
            btnDelPage.TabIndex = 14;
            btnDelPage.Text = "删除";
            btnDelPage.Click += btnDelPage_Click;
            // 
            // btnPastePage
            // 
            btnPastePage.Location = new Point(20, 709);
            btnPastePage.Name = "btnPastePage";
            btnPastePage.Size = new Size(80, 30);
            btnPastePage.TabIndex = 13;
            btnPastePage.Text = "粘贴";
            btnPastePage.Click += btnPastePage_Click;
            // 
            // btnSavePage
            // 
            btnSavePage.Location = new Point(105, 709);
            btnSavePage.Name = "btnSavePage";
            btnSavePage.Size = new Size(80, 30);
            btnSavePage.TabIndex = 12;
            btnSavePage.Text = "保存";
            btnSavePage.Click += btnSavePage_Click;
            // 
            // btnEditRegion
            // 
            btnEditRegion.Location = new Point(20, 1149);
            btnEditRegion.Name = "btnEditRegion";
            btnEditRegion.Size = new Size(75, 30);
            btnEditRegion.TabIndex = 11;
            btnEditRegion.Text = "编辑";
            btnEditRegion.Click += btnEditRegion_Click;
            // 
            // btnDelRegion
            // 
            btnDelRegion.Location = new Point(100, 1149);
            btnDelRegion.Name = "btnDelRegion";
            btnDelRegion.Size = new Size(75, 30);
            btnDelRegion.TabIndex = 10;
            btnDelRegion.Text = "删除";
            btnDelRegion.Click += btnDelRegion_Click;
            // 
            // pbImage
            // 
            pbImage.BorderStyle = BorderStyle.FixedSingle;
            pbImage.Location = new Point(360, 30);
            pbImage.Name = "pbImage";
            pbImage.Size = new Size(1200, 950);
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbImage.TabIndex = 9;
            pbImage.TabStop = false;
            // 
            // txtFlowName
            // 
            txtFlowName.Location = new Point(360, 1000);
            txtFlowName.Name = "txtFlowName";
            txtFlowName.Size = new Size(300, 27);
            txtFlowName.TabIndex = 8;
            // 
            // txtFlowDesc
            // 
            txtFlowDesc.Location = new Point(360, 1040);
            txtFlowDesc.Name = "txtFlowDesc";
            txtFlowDesc.Size = new Size(300, 27);
            txtFlowDesc.TabIndex = 7;
            // 
            // txtPageName
            // 
            txtPageName.Location = new Point(710, 1000);
            txtPageName.Name = "txtPageName";
            txtPageName.Size = new Size(300, 27);
            txtPageName.TabIndex = 6;
            // 
            // txtPageRemark
            // 
            txtPageRemark.Font = new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtPageRemark.Location = new Point(710, 1068);
            txtPageRemark.Name = "txtPageRemark";
            txtPageRemark.Size = new Size(500, 120);
            txtPageRemark.TabIndex = 13;
            txtPageRemark.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(20, 20);
            label1.Name = "label1";
            label1.Size = new Size(92, 27);
            label1.TabIndex = 5;
            label1.Text = "流程列表";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(20, 363);
            label2.Name = "label2";
            label2.Size = new Size(92, 27);
            label2.TabIndex = 4;
            label2.Text = "页面列表";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label3.Location = new Point(20, 797);
            label3.Name = "label3";
            label3.Size = new Size(92, 27);
            label3.TabIndex = 3;
            label3.Text = "区域列表";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label4.Location = new Point(360, 980);
            label4.Name = "label4";
            label4.Size = new Size(97, 27);
            label4.TabIndex = 2;
            label4.Text = "流程名称:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label5.Location = new Point(710, 980);
            label5.Name = "label5";
            label5.Size = new Size(97, 27);
            label5.TabIndex = 1;
            label5.Text = "页面名称:";
            // 
            // btnSetStartPage
            // 
            btnSetStartPage.Location = new Point(211, 709);
            btnSetStartPage.Name = "btnSetStartPage";
            btnSetStartPage.Size = new Size(80, 30);
            btnSetStartPage.TabIndex = 0;
            btnSetStartPage.Text = "设为开始";
            btnSetStartPage.Click += btnSetStartPage_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label6.Location = new Point(710, 1038);
            label6.Name = "label6";
            label6.Size = new Size(92, 27);
            label6.TabIndex = 22;
            label6.Text = "页面描述";
            label6.Click += label6_Click;
            // 
            // FormManage
            // 
            ClientSize = new Size(1600, 1200);
            Controls.Add(label6);
            Controls.Add(btnSetStartPage);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtPageName);
            Controls.Add(txtPageRemark);
            Controls.Add(txtFlowDesc);
            Controls.Add(txtFlowName);
            Controls.Add(pbImage);
            Controls.Add(btnDelRegion);
            Controls.Add(btnEditRegion);
            Controls.Add(btnSavePage);
            Controls.Add(btnPastePage);
            Controls.Add(btnDelPage);
            Controls.Add(btnAddPage);
            Controls.Add(btnSaveFlow);
            Controls.Add(btnDelFlow);
            Controls.Add(btnAddFlow);
            Controls.Add(lbRegions);
            Controls.Add(lbPages);
            Controls.Add(lbFlows);
            Name = "FormManage";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "流程维护";
            ((System.ComponentModel.ISupportInitialize)pbImage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }
}