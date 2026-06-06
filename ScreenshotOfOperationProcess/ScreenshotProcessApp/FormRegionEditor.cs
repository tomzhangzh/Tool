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
            pbImage = new PictureBox();
            btnSave = new Button();
            btnCancel = new Button();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)pbImage).BeginInit();
            SuspendLayout();
            // 
            // pbImage
            // 
            pbImage.BorderStyle = BorderStyle.FixedSingle;
            pbImage.Location = new Point(12, 60);
            pbImage.Name = "pbImage";
            pbImage.Size = new Size(1550, 1000);
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbImage.TabIndex = 3;
            pbImage.TabStop = false;
            pbImage.Paint += pbImage_Paint;
            pbImage.MouseDoubleClick += pbImage_MouseDoubleClick;
            pbImage.MouseDown += pbImage_MouseDown;
            pbImage.MouseMove += pbImage_MouseMove;
            pbImage.MouseUp += pbImage_MouseUp;
            // 
            // btnSave
            // 
            btnSave.Font = new Font("微软雅黑", 14F, FontStyle.Regular, GraphicsUnit.Point);
            btnSave.Location = new Point(1021, 9);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(120, 45);
            btnSave.TabIndex = 2;
            btnSave.Text = "保存";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new Font("微软雅黑", 14F, FontStyle.Regular, GraphicsUnit.Point);
            btnCancel.Location = new Point(1201, 9);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(120, 45);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "取消";
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("微软雅黑", 14F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(20, 25);
            label1.Name = "label1";
            label1.Size = new Size(494, 31);
            label1.TabIndex = 0;
            label1.Text = "在图片上拖动鼠标框选区域，设置链接和备注";
            // 
            // FormRegionEditor
            // 
            ClientSize = new Size(1600, 1066);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(pbImage);
            Name = "FormRegionEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "区域编辑器";
            ((System.ComponentModel.ISupportInitialize)pbImage).EndInit();
            ResumeLayout(false);
            PerformLayout();
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

            rbPage.CheckedChanged += (s, e) =>
            {
                cbTargetPage.Enabled = rbPage.Checked;
                cbTargetFlow.Enabled = rbFlow.Checked;
            };
            rbFlow.CheckedChanged += (s, e) =>
            {
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
            txtRemark = new TextBox();
            cbTargetPage = new ComboBox();
            btnOK = new Button();
            btnCancel = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            txtRectInfo = new TextBox();
            rbPage = new RadioButton();
            rbFlow = new RadioButton();
            cbTargetFlow = new ComboBox();
            label4 = new Label();
            SuspendLayout();
            txtRemark.Location = new Point(120, 80);
            txtRemark.Size = new Size(300, 28);
            txtRemark.Font = new Font("微软雅黑", 12F);
            cbTargetPage.Location = new Point(120, 130);
            cbTargetPage.Size = new Size(300, 28);
            cbTargetPage.Font = new Font("微软雅黑", 12F);
            btnOK.Location = new Point(150, 220);
            btnOK.Size = new Size(100, 35);
            btnOK.Text = "确定";
            btnOK.Font = new Font("微软雅黑", 12F);
            btnOK.Click += btnOK_Click;
            btnCancel.Location = new Point(280, 220);
            btnCancel.Size = new Size(100, 35);
            btnCancel.Text = "取消";
            btnCancel.Font = new Font("微软雅黑", 12F);
            btnCancel.Click += btnCancel_Click;
            label1.AutoSize = true;
            label1.Location = new Point(30, 50);
            label1.Text = "区域信息:";
            label1.Font = new Font("微软雅黑", 12F);
            label2.AutoSize = true;
            label2.Location = new Point(30, 85);
            label2.Text = "备注:";
            label2.Font = new Font("微软雅黑", 12F);
            label3.AutoSize = true;
            label3.Location = new Point(30, 135);
            label3.Text = "链接到:";
            label3.Font = new Font("微软雅黑", 12F);
            txtRectInfo.Location = new Point(120, 45);
            txtRectInfo.Size = new Size(300, 28);
            txtRectInfo.ReadOnly = true;
            txtRectInfo.Font = new Font("微软雅黑", 12F);
            rbPage.Location = new Point(120, 130);
            rbPage.Size = new Size(60, 28);
            rbPage.Text = "页面";
            rbPage.Font = new Font("微软雅黑", 12F);
            rbPage.Checked = true;
            rbFlow.Location = new Point(120, 175);
            rbFlow.Size = new Size(60, 28);
            rbFlow.Text = "流程";
            rbFlow.Font = new Font("微软雅黑", 12F);
            cbTargetPage.Location = new Point(190, 130);
            cbTargetPage.Size = new Size(230, 28);
            cbTargetPage.Font = new Font("微软雅黑", 12F);
            cbTargetFlow.Location = new Point(190, 175);
            cbTargetFlow.Size = new Size(230, 28);
            cbTargetFlow.Font = new Font("微软雅黑", 12F);
            cbTargetFlow.Enabled = false;
            label4.AutoSize = true;
            label4.Location = new Point(30, 180);
            label4.Text = "或:";
            label4.Font = new Font("微软雅黑", 12F);
            ClientSize = new Size(450, 280);
            Controls.Add(label4);
            Controls.Add(cbTargetFlow);
            Controls.Add(rbFlow);
            Controls.Add(rbPage);
            Controls.Add(txtRectInfo);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(cbTargetPage);
            Controls.Add(txtRemark);
            Text = "区域信息";
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
            PerformLayout();
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