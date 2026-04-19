using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public partial class FormRegionEditor : Form
    {
        private Database _db;
        private int _pageId;
        private List<ProcessPage> _pages;
        private List<ProcessFlow> _flows;
        private ProcessPage _currentPage;
        private List<PageRegion> _regions;
        private bool _isDrawing = false;
        private Point _startPoint;
        private Point _endPoint;
        private Rectangle _currentRect;

        public FormRegionEditor(Database db, int pageId, List<ProcessPage> pages)
        {
            InitializeComponent();
            _db = db;
            _pageId = pageId;
            _pages = pages;
            _flows = db.GetAllFlows();
            LoadPageAndRegions();
        }

        private void LoadPageAndRegions()
        {
            _currentPage = _db.GetPageById(_pageId);
            if (_currentPage != null)
            {
                if (_currentPage.ImageData != null && _currentPage.ImageData.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(_currentPage.ImageData))
                    {
                        pbImage.Image = Image.FromStream(ms);
                    }
                }
                _regions = _db.GetRegionsByPageId(_pageId);
            }
        }

        private void pbImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDrawing = true;
                _startPoint = e.Location;
            }
        }

        private void pbImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                _endPoint = e.Location;
                _currentRect = new Rectangle(
                    Math.Min(_startPoint.X, _endPoint.X),
                    Math.Min(_startPoint.Y, _endPoint.Y),
                    Math.Abs(_endPoint.X - _startPoint.X),
                    Math.Abs(_endPoint.Y - _startPoint.Y)
                );
                pbImage.Invalidate();
            }
        }

        private void pbImage_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                if (_currentRect.Width > 10 && _currentRect.Height > 10)
                {
                    using (FormRegionInfo infoForm = new FormRegionInfo(_pages, _flows, _currentRect))
                    {
                        if (infoForm.ShowDialog() == DialogResult.OK)
                        {
                            PageRegion region = new PageRegion
                            {
                                PageId = _pageId,
                                X = _currentRect.X,
                                Y = _currentRect.Y,
                                Width = _currentRect.Width,
                                Height = _currentRect.Height,
                                Remark = infoForm.Remark,
                                TargetPageId = infoForm.TargetPageId
                            };
                            
                            if (infoForm.TargetFlowId.HasValue)
                            {
                                var flow = _flows.Find(f => f.Id == infoForm.TargetFlowId.Value);
                                if (flow != null && flow.StartPageId > 0)
                                {
                                    region.TargetPageId = flow.StartPageId;
                                }
                            }
                            
                            _db.AddRegion(region);
                            _regions = _db.GetRegionsByPageId(_pageId);
                            pbImage.Invalidate();
                        }
                    }
                }
            }
        }

        private void pbImage_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PageRegion clickedRegion = null;
            foreach (var region in _regions)
            {
                Rectangle regionRect = new Rectangle(region.X, region.Y, region.Width, region.Height);
                if (regionRect.Contains(e.Location))
                {
                    clickedRegion = region;
                    break;
                }
            }
            
            if (clickedRegion != null)
            {
                Rectangle rect = new Rectangle(clickedRegion.X, clickedRegion.Y, clickedRegion.Width, clickedRegion.Height);
                using (FormRegionInfo infoForm = new FormRegionInfo(_pages, _flows, rect, clickedRegion))
                {
                    if (infoForm.ShowDialog() == DialogResult.OK)
                    {
                        clickedRegion.Remark = infoForm.Remark;
                        clickedRegion.TargetPageId = infoForm.TargetPageId;
                        
                        if (infoForm.TargetFlowId.HasValue)
                        {
                            var flow = _flows.Find(f => f.Id == infoForm.TargetFlowId.Value);
                            if (flow != null && flow.StartPageId > 0)
                            {
                                clickedRegion.TargetPageId = flow.StartPageId;
                            }
                        }
                        
                        _db.UpdateRegion(clickedRegion);
                        _regions = _db.GetRegionsByPageId(_pageId);
                        pbImage.Invalidate();
                    }
                }
            }
        }

        private void pbImage_Paint(object sender, PaintEventArgs e)
        {
            if (_currentRect.Width > 0 && _currentRect.Height > 0 && _isDrawing)
            {
                using (Pen pen = new Pen(Color.Red, 3))
                {
                    e.Graphics.DrawRectangle(pen, _currentRect);
                }
            }

            foreach (var region in _regions)
            {
                using (Pen pen = new Pen(Color.Blue, 3))
                {
                    e.Graphics.DrawRectangle(pen, region.X, region.Y, region.Width, region.Height);
                    
                    if (!string.IsNullOrEmpty(region.Remark))
                    {
                        using (Brush brush = new SolidBrush(Color.Yellow))
                        using (Font font = new Font("Arial", 12))
                        {
                            SizeF textSize = e.Graphics.MeasureString(region.Remark, font);
                            float textX = region.X + region.Width + 8;
                            float textY = region.Y;
                            
                            e.Graphics.FillRectangle(brush, textX, textY, textSize.Width + 6, textSize.Height + 4);
                            using (Pen textPen = new Pen(Color.Black, 1))
                            {
                                e.Graphics.DrawRectangle(textPen, textX, textY, textSize.Width + 6, textSize.Height + 4);
                            }
                            e.Graphics.DrawString(region.Remark, font, Brushes.Black, textX + 3, textY + 2);
                        }
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        #region Windows Form Designer generated code
        private System.ComponentModel.IContainer components = null;
        private PictureBox pbImage;
        private Button btnSave;
        private Button btnCancel;
        private Label label1;

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
            this.pbImage = new System.Windows.Forms.PictureBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).BeginInit();
            this.SuspendLayout();
            this.pbImage.Location = new System.Drawing.Point(20, 60);
            this.pbImage.Size = new System.Drawing.Size(1550, 1000);
            this.pbImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbImage.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbImage_MouseDown);
            this.pbImage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbImage_MouseMove);
            this.pbImage.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbImage_MouseUp);
            this.pbImage.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pbImage_MouseDoubleClick);
            this.pbImage.Paint += new System.Windows.Forms.PaintEventHandler(this.pbImage_Paint);
            this.btnSave.Location = new System.Drawing.Point(700, 1080);
            this.btnSave.Size = new System.Drawing.Size(120, 45);
            this.btnSave.Text = "保存";
            this.btnSave.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnCancel.Location = new System.Drawing.Point(880, 1080);
            this.btnCancel.Size = new System.Drawing.Size(120, 45);
            this.btnCancel.Text = "取消";
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 25);
            this.label1.Text = "在图片上拖动鼠标框选区域，设置链接和备注";
            this.label1.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.ClientSize = new System.Drawing.Size(1600, 1200);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.pbImage);
            this.Text = "区域编辑器";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }

    public partial class FormRegionInfo : Form
    {
        public int RegionId { get; set; }
        public string Remark { get; set; }
        public int? TargetPageId { get; set; }
        public int? TargetFlowId { get; set; }

        private RadioButton rbPage;
        private RadioButton rbFlow;
        private ComboBox cbTargetFlow;
        private Label label4;

        public FormRegionInfo(List<ProcessPage> pages, List<ProcessFlow> flows, Rectangle rect, PageRegion existingRegion = null)
        {
            InitializeComponent();
            foreach (var page in pages)
            {
                cbTargetPage.Items.Add(new PageItem(page.Id, page.Name));
            }
            cbTargetPage.DisplayMember = "Name";
            cbTargetPage.ValueMember = "Id";
            
            foreach (var flow in flows)
            {
                if (flow.StartPageId > 0)
                {
                    cbTargetFlow.Items.Add(new FlowItem(flow.Id, flow.Name, flow.StartPageId));
                }
            }
            cbTargetFlow.DisplayMember = "Name";
            cbTargetFlow.ValueMember = "Id";
            
            txtRectInfo.Text = $"区域: ({rect.X}, {rect.Y}) {rect.Width}x{rect.Height}";
            
            if (existingRegion != null)
            {
                RegionId = existingRegion.Id;
                txtRemark.Text = existingRegion.Remark ?? "";
                TargetPageId = existingRegion.TargetPageId;
                
                if (existingRegion.TargetPageId.HasValue)
                {
                    rbPage.Checked = true;
                    cbTargetFlow.Enabled = false;
                    
                    foreach (PageItem item in cbTargetPage.Items)
                    {
                        if (item.Id == existingRegion.TargetPageId.Value)
                        {
                            cbTargetPage.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            
            rbPage.CheckedChanged += (s, e) => {
                cbTargetPage.Enabled = rbPage.Checked;
                cbTargetFlow.Enabled = rbFlow.Checked;
            };
            rbFlow.CheckedChanged += (s, e) => {
                cbTargetPage.Enabled = rbPage.Checked;
                cbTargetFlow.Enabled = rbFlow.Checked;
            };
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Remark = txtRemark.Text;
            if (rbPage.Checked && cbTargetPage.SelectedItem != null)
            {
                TargetPageId = ((PageItem)cbTargetPage.SelectedItem).Id;
                TargetFlowId = null;
            }
            else if (rbFlow.Checked && cbTargetFlow.SelectedItem != null)
            {
                TargetFlowId = ((FlowItem)cbTargetFlow.SelectedItem).Id;
                TargetPageId = null;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #region Windows Form Designer generated code
        private System.ComponentModel.IContainer components = null;
        private TextBox txtRemark;
        private ComboBox cbTargetPage;
        private Button btnOK;
        private Button btnCancel;
        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox txtRectInfo;

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
            this.txtRemark = new System.Windows.Forms.TextBox();
            this.cbTargetPage = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtRectInfo = new System.Windows.Forms.TextBox();
            this.rbPage = new System.Windows.Forms.RadioButton();
            this.rbFlow = new System.Windows.Forms.RadioButton();
            this.cbTargetFlow = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.txtRemark.Location = new System.Drawing.Point(120, 80);
            this.txtRemark.Size = new System.Drawing.Size(300, 28);
            this.txtRemark.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.cbTargetPage.Location = new System.Drawing.Point(120, 130);
            this.cbTargetPage.Size = new System.Drawing.Size(300, 28);
            this.cbTargetPage.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.btnOK.Location = new System.Drawing.Point(150, 220);
            this.btnOK.Size = new System.Drawing.Size(100, 35);
            this.btnOK.Text = "确定";
            this.btnOK.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.btnCancel.Location = new System.Drawing.Point(280, 220);
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.Text = "取消";
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 50);
            this.label1.Text = "区域信息:";
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(30, 85);
            this.label2.Text = "备注:";
            this.label2.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(30, 135);
            this.label3.Text = "链接到:";
            this.label3.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.txtRectInfo.Location = new System.Drawing.Point(120, 45);
            this.txtRectInfo.Size = new System.Drawing.Size(300, 28);
            this.txtRectInfo.ReadOnly = true;
            this.txtRectInfo.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.rbPage.Location = new System.Drawing.Point(120, 130);
            this.rbPage.Size = new System.Drawing.Size(60, 28);
            this.rbPage.Text = "页面";
            this.rbPage.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.rbPage.Checked = true;
            this.rbFlow.Location = new System.Drawing.Point(120, 175);
            this.rbFlow.Size = new System.Drawing.Size(60, 28);
            this.rbFlow.Text = "流程";
            this.rbFlow.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.cbTargetPage.Location = new System.Drawing.Point(190, 130);
            this.cbTargetPage.Size = new System.Drawing.Size(230, 28);
            this.cbTargetPage.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.cbTargetFlow.Location = new System.Drawing.Point(190, 175);
            this.cbTargetFlow.Size = new System.Drawing.Size(230, 28);
            this.cbTargetFlow.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.cbTargetFlow.Enabled = false;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 180);
            this.label4.Text = "或:";
            this.label4.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.ClientSize = new System.Drawing.Size(450, 280);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbTargetFlow);
            this.Controls.Add(this.rbFlow);
            this.Controls.Add(this.rbPage);
            this.Controls.Add(this.txtRectInfo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbTargetPage);
            this.Controls.Add(this.txtRemark);
            this.Text = "区域信息";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }

    public class PageItem
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public PageItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Id} - {Name}";
        }
    }
}