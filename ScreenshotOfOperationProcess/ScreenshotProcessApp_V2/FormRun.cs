using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public partial class FormRun : Form
    {
        private Database _db;
        private int _currentPageId;
        private ProcessPage _currentPage;
        private List<PageRegion> _currentRegions;
        private List<PageAnnotation> _currentAnnotations;
        private Label label2;
        private RichTextBox richTextBoxRemark;
        private Stack<int> _pageHistory = new Stack<int>();

        public FormRun(Database db)
        {
            InitializeComponent();
            _db = db;
            LoadFlows();
        }

        private void LoadFlows()
        {
            var flows = _db.GetAllFlows();
            cbFlows.Items.Clear();
            foreach (var flow in flows)
            {
                cbFlows.Items.Add(new FlowItem(flow.Id, flow.Name, flow.StartPageId));
            }
            cbFlows.DisplayMember = "Name";
            cbFlows.ValueMember = "Id";
        }

        public void SelectFlowAndStart(int flowId)
        {
            for (int i = 0; i < cbFlows.Items.Count; i++)
            {
                FlowItem item = (FlowItem)cbFlows.Items[i];
                if (item.Id == flowId)
                {
                    cbFlows.SelectedIndex = i;
                    btnStart_Click(null, null);
                    return;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (cbFlows.SelectedItem != null)
            {
                FlowItem item = (FlowItem)cbFlows.SelectedItem;
                if (item.StartPageId > 0)
                {
                    _pageHistory.Clear();
                    LoadPage(item.StartPageId);
                }
                else
                {
                    MessageBox.Show("该流程尚未设置开始页面");
                }
            }
            else
            {
                MessageBox.Show("请选择一个流程");
            }
        }

        private void LoadPage(int pageId)
        {
            _currentPageId = pageId;
            _currentPage = _db.GetPageById(pageId);
            _currentRegions = _db.GetRegionsByPageId(pageId);
            _currentAnnotations = _db.GetAnnotationsByPageId(pageId);

            if (_currentPage != null)
            {
                if (_currentPage.ImageData != null && _currentPage.ImageData.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(_currentPage.ImageData))
                    {
                        pbImage.Image = Image.FromStream(ms);
                    }
                }
                richTextBoxRemark.Text = _currentPage.Remark;
                lblPageName.Text = _currentPage.Name;
                pbImage.Invalidate();
            }

            btnBack.Enabled = _pageHistory.Count > 0;
        }

        private void pbImage_MouseClick(object sender, MouseEventArgs e)
        {
            if (_currentRegions == null || _currentPage == null) return;

            foreach (var region in _currentRegions)
            {
                Rectangle rect = new Rectangle(region.X, region.Y, region.Width, region.Height);
                if (rect.Contains(e.Location))
                {
                    if (region.TargetPageId.HasValue)
                    {
                        _pageHistory.Push(_currentPageId);
                        LoadPage(region.TargetPageId.Value);

                        //if (!string.IsNullOrEmpty(region.Remark))
                        //{
                        //    MessageBox.Show(region.Remark, "备注信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //}
                    }
                    break;
                }
            }
        }

        private void pbImage_Paint(object sender, PaintEventArgs e)
        {
            if (_currentRegions != null)
            {
                foreach (var region in _currentRegions)
                {
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        e.Graphics.DrawRectangle(pen, region.X, region.Y, region.Width, region.Height);

                        using (Brush brush = new SolidBrush(Color.Red))
                        {
                            e.Graphics.FillPolygon(brush, new Point[] {
                                new Point(region.X + region.Width - 10, region.Y),
                                new Point(region.X + region.Width, region.Y),
                                new Point(region.X + region.Width, region.Y + 10)
                            });
                        }

                        if (!string.IsNullOrEmpty(region.Remark))
                        {
                            using (Brush brush = new SolidBrush(Color.Yellow))
                            using (Font font = new Font("Arial", 10))
                            {
                                SizeF textSize = e.Graphics.MeasureString(region.Remark, font);
                                float textX = region.X + region.Width + 5;
                                float textY = region.Y;

                                e.Graphics.FillRectangle(brush, textX, textY, textSize.Width + 4, textSize.Height + 2);
                                using (Pen textPen = new Pen(Color.Black, 1))
                                {
                                    e.Graphics.DrawRectangle(textPen, textX, textY, textSize.Width + 4, textSize.Height + 2);
                                }
                                e.Graphics.DrawString(region.Remark, font, Brushes.Black, textX + 2, textY + 1);
                            }
                        }
                    }
                }
            }

            if (_currentAnnotations != null)
            {
                foreach (var annotation in _currentAnnotations)
                {
                    Color bgColor = Color.FromArgb(100, Color.LightYellow);
                    using (Brush brush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillRectangle(brush, annotation.TextX, annotation.TextY, annotation.TextWidth, annotation.TextHeight);
                    }
                    using (Pen pen = new Pen(Color.LightSkyBlue, 1.5f))
                    {
                        e.Graphics.DrawRectangle(pen, annotation.TextX, annotation.TextY, annotation.TextWidth, annotation.TextHeight);
                    }

                    if (!string.IsNullOrEmpty(annotation.Text))
                    {
                        using (Font font = new Font("微软雅黑", 10F))
                        using (Brush textBrush = new SolidBrush(Color.Black))
                        {
                            StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            e.Graphics.DrawString(annotation.Text, font, textBrush,
                                new RectangleF(annotation.TextX, annotation.TextY, annotation.TextWidth, annotation.TextHeight), sf);
                        }
                    }

                    if (annotation.ArrowEndX.HasValue && annotation.ArrowEndY.HasValue)
                    {
                        int startX = annotation.TextX + annotation.TextWidth / 2;
                        int startY = annotation.TextY + annotation.TextHeight / 2;
                        int endX = annotation.ArrowEndX.Value;
                        int endY = annotation.ArrowEndY.Value;

                        using (Pen arrowPen = new Pen(Color.Blue, 2))
                        {
                            arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(8, 8);
                            e.Graphics.DrawLine(arrowPen, startX, startY, endX, endY);
                        }
                    }
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (_pageHistory.Count > 0)
            {
                int prevPageId = _pageHistory.Pop();
                LoadPage(prevPageId);
            }
        }

        #region Windows Form Designer generated code
        private System.ComponentModel.IContainer components = null;
        private ComboBox cbFlows;
        private Button btnStart;
        private PictureBox pbImage;
        private Button btnBack;
        private Label lblPageName;
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
            cbFlows = new ComboBox();
            btnStart = new Button();
            pbImage = new PictureBox();
            btnBack = new Button();
            lblPageName = new Label();
            label1 = new Label();
            label2 = new Label();
            richTextBoxRemark = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)pbImage).BeginInit();
            SuspendLayout();
            // 
            // cbFlows
            // 
            cbFlows.Font = new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point);
            cbFlows.Location = new Point(100, 8);
            cbFlows.Name = "cbFlows";
            cbFlows.Size = new Size(300, 28);
            cbFlows.TabIndex = 5;
            //
            // btnStart
            //
            btnStart.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.BackColor = Color.FromArgb(40, 167, 69);
            btnStart.ForeColor = Color.White;
            btnStart.Cursor = Cursors.Hand;
            btnStart.Location = new Point(410, 7);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(85, 30);
            btnStart.TabIndex = 4;
            btnStart.Text = "开始运行";
            btnStart.Click += btnStart_Click;
            // 
            // pbImage
            // 
            pbImage.BorderStyle = BorderStyle.FixedSingle;
            pbImage.Location = new Point(12, 74);
            pbImage.Name = "pbImage";
            pbImage.Size = new Size(1550, 1000);
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbImage.TabIndex = 3;
            pbImage.TabStop = false;
            pbImage.Paint += pbImage_Paint;
            pbImage.MouseClick += pbImage_MouseClick;
            // 
            // btnBack
            // 
            btnBack.Enabled = false;
            btnBack.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.BackColor = Color.FromArgb(108, 117, 125);
            btnBack.ForeColor = Color.White;
            btnBack.Cursor = Cursors.Hand;
            btnBack.Location = new Point(505, 7);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(85, 30);
            btnBack.TabIndex = 2;
            btnBack.Text = "返回上页";
            btnBack.Click += btnBack_Click;
            // 
            // lblPageName
            // 
            lblPageName.AutoSize = true;
            lblPageName.Font = new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblPageName.Location = new Point(27, 48);
            lblPageName.Name = "lblPageName";
            lblPageName.Size = new Size(69, 20);
            lblPageName.TabIndex = 1;
            lblPageName.Text = "页面名称";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(17, 12);
            label1.Name = "label1";
            label1.Size = new Size(116, 31);
            label1.TabIndex = 0;
            label1.Text = "选择流程:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("微软雅黑", 11F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(1576, 67);
            label2.Name = "label2";
            label2.Size = new Size(123, 35);
            label2.TabIndex = 6;
            label2.Text = "页面备注";
            // 
            // richTextBoxRemark
            // 
            richTextBoxRemark.Location = new Point(1576, 105);
            richTextBoxRemark.Name = "richTextBoxRemark";
            richTextBoxRemark.Size = new Size(309, 996);
            richTextBoxRemark.TabIndex = 7;
            richTextBoxRemark.Text = "";
            // 
            // FormRun
            // 
            ClientSize = new Size(1892, 1072);
            Controls.Add(richTextBoxRemark);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblPageName);
            Controls.Add(btnBack);
            Controls.Add(pbImage);
            Controls.Add(btnStart);
            Controls.Add(cbFlows);
            MaximizeBox = false;
            Name = "FormRun";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "流程运行";
            ((System.ComponentModel.ISupportInitialize)pbImage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion
    }

    public class FlowItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int StartPageId { get; set; }

        public FlowItem(int id, string name, int startPageId)
        {
            Id = id;
            Name = name;
            StartPageId = startPageId;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}