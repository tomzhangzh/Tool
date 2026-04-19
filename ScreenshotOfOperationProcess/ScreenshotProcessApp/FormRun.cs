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

            if (_currentPage != null)
            {
                if (_currentPage.ImageData != null && _currentPage.ImageData.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(_currentPage.ImageData))
                    {
                        pbImage.Image = Image.FromStream(ms);
                    }
                }
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
                        
                        if (!string.IsNullOrEmpty(region.Remark))
                        {
                            MessageBox.Show(region.Remark, "备注信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
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
                    using (Pen pen = new Pen(Color.Green, 2))
                    {
                        e.Graphics.DrawRectangle(pen, region.X, region.Y, region.Width, region.Height);
                        
                        using (Brush brush = new SolidBrush(Color.Green))
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
            this.cbFlows = new System.Windows.Forms.ComboBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.pbImage = new System.Windows.Forms.PictureBox();
            this.btnBack = new System.Windows.Forms.Button();
            this.lblPageName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).BeginInit();
            this.SuspendLayout();
            this.cbFlows.Location = new System.Drawing.Point(150, 30);
            this.cbFlows.Size = new System.Drawing.Size(400, 35);
            this.cbFlows.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.btnStart.Location = new System.Drawing.Point(600, 28);
            this.btnStart.Size = new System.Drawing.Size(120, 40);
            this.btnStart.Text = "开始运行";
            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            this.pbImage.Location = new System.Drawing.Point(20, 100);
            this.pbImage.Size = new System.Drawing.Size(1550, 1000);
            this.pbImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbImage.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pbImage_MouseClick);
            this.pbImage.Paint += new System.Windows.Forms.PaintEventHandler(this.pbImage_Paint);
            this.btnBack.Location = new System.Drawing.Point(20, 1110);
            this.btnBack.Size = new System.Drawing.Size(120, 40);
            this.btnBack.Text = "返回上一页";
            this.btnBack.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.btnBack.Enabled = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            this.lblPageName.AutoSize = true;
            this.lblPageName.Location = new System.Drawing.Point(20, 70);
            this.lblPageName.Text = "页面名称";
            this.lblPageName.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 35);
            this.label1.Text = "选择流程:";
            this.label1.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.ClientSize = new System.Drawing.Size(1600, 1200);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblPageName);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.pbImage);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.cbFlows);
            this.Text = "流程运行";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
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