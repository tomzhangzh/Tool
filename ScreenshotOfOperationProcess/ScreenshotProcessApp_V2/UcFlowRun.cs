using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcFlowRun : UserControl
    {
        private Database _db;
        private ComboBox cbFlows;
        private Button btnStart;
        private Button btnBack;
        private PictureBox pbImage;
        private RichTextBox txtRemark;
        private Label lblPageInfo;
        private Label lblPageTitle;

        private int _currentPageId;
        private ProcessPage _currentPage;
        private List<PageRegion> _currentRegions;
        private List<PageAnnotation> _currentAnnotations;
        private Stack<int> _pageHistory = new Stack<int>();
        private Image _displayImage;

        public UcFlowRun(Database db)
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
            lblPageTitle.Text = "运行流程";
            lblPageTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            lblPageTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblPageTitle.Location = new Point(10, 5);
            lblPageTitle.Size = new Size(150, 35);
            this.Controls.Add(lblPageTitle);

            // 页面信息 - 与标题同一行
            lblPageInfo = new Label();
            lblPageInfo.Text = "请选择流程并点击开始运行";
            lblPageInfo.Location = new Point(170, 12);
            lblPageInfo.Size = new Size(600, 25);
            lblPageInfo.Font = new Font("微软雅黑", 10F);
            lblPageInfo.ForeColor = Color.FromArgb(0, 120, 215);
            lblPageInfo.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblPageInfo);

            // 工具栏
            var lblFlow = new Label();
            lblFlow.Text = "选择流程：";
            lblFlow.Location = new Point(10, 52);
            lblFlow.Size = new Size(80, 28);
            lblFlow.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblFlow);

            cbFlows = new ComboBox();
            cbFlows.Location = new Point(90, 50);
            cbFlows.Size = new Size(250, 28);
            cbFlows.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(cbFlows);

            btnStart = CreateButton("开始运行", 350, 49, Color.FromArgb(40, 167, 69));
            btnStart.Click += (s, e) => StartFlow();
            this.Controls.Add(btnStart);

            btnBack = CreateButton("返回上页", 440, 49, Color.FromArgb(108, 117, 125));
            btnBack.Click += (s, e) => GoBack();
            btnBack.Enabled = false;
            this.Controls.Add(btnBack);

            // 备注区 - 标签在上方，多行3行带滚动条
            var lblRemark = new Label();
            lblRemark.Text = "备注：";
            lblRemark.Location = new Point(840, 2);
            lblRemark.Size = new Size(60, 18);
            lblRemark.Font = new Font("微软雅黑", 9F);
            this.Controls.Add(lblRemark);

            txtRemark = new RichTextBox();
            txtRemark.Location = new Point(840, 24);
            txtRemark.Size = new Size(800, 76);
            txtRemark.ReadOnly = true;
            txtRemark.Font = new Font("微软雅黑", 9F);
            txtRemark.BackColor = Color.FromArgb(255, 255, 230);
            txtRemark.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.Controls.Add(txtRemark);

            // 图片显示区 - 与FormRun保持一致的尺寸(1550x1000)
            pbImage = new PictureBox();
            pbImage.Location = new Point(12, 130);
            pbImage.Size = new Size(1550, 1000);
            pbImage.BorderStyle = BorderStyle.FixedSingle;
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbImage.BackColor = Color.White;
            pbImage.Paint += PbImage_Paint;
            pbImage.MouseClick += PbImage_MouseClick;
            this.Controls.Add(pbImage);

            this.ResumeLayout(false);
        }

        private Button CreateButton(string text, int x, int y, Color backColor)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(85, 30);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
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
        }

        private void StartFlow()
        {
            if (cbFlows.SelectedItem is FlowItem item)
            {
                var flow = _db.GetFlowById(item.Id);
                if (flow != null && flow.StartPageId > 0)
                {
                    _pageHistory.Clear();
                    LoadPage(flow.StartPageId);
                    btnBack.Enabled = false;
                }
                else
                {
                    MessageBox.Show("该流程尚未设置开始页面，请先在页面管理中设置", "提示");
                }
            }
        }

        private void LoadPage(int pageId)
        {
            _currentPageId = pageId;
            _currentPage = _db.GetPageById(pageId);
            if (_currentPage == null) return;

            _currentRegions = _db.GetRegionsByPageId(pageId);
            _currentAnnotations = _db.GetAnnotationsByPageId(pageId);

            // 加载图片
            if (_currentPage.ImageData != null && _currentPage.ImageData.Length > 0)
            {
                using (MemoryStream ms = new MemoryStream(_currentPage.ImageData))
                {
                    _displayImage = Image.FromStream(ms);
                    pbImage.Image = _displayImage;
                }
            }

            // 显示备注
            txtRemark.Text = (_currentPage.Remark ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");

            lblPageInfo.Text = $"当前页面：{_currentPage.Name}（ID: {_currentPage.Id}）";
            pbImage.Invalidate();
        }

        private void PbImage_Paint(object sender, PaintEventArgs e)
        {
            if (_currentRegions == null) return;

            // 绘制区域
            foreach (var region in _currentRegions)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, region.X, region.Y, region.Width, region.Height);

                    // 绘制备注
                    if (!string.IsNullOrEmpty(region.Remark))
                    {
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, 255, 255, 0)))
                        {
                            e.Graphics.FillRectangle(brush, region.X, region.Y - 20, 
                                region.Remark.Length * 12 + 10, 20);
                        }
                        e.Graphics.DrawString(region.Remark, new Font("微软雅黑", 9F), 
                            Brushes.Black, region.X + 3, region.Y - 18);
                    }
                }
            }

            // 绘制注释
            if (_currentAnnotations != null)
            {
                foreach (var ann in _currentAnnotations)
                {
                    // 绘制文本框背景
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 200)))
                    {
                        e.Graphics.FillRectangle(bgBrush, ann.TextX, ann.TextY, ann.TextWidth, ann.TextHeight);
                    }
                    using (Pen borderPen = new Pen(Color.FromArgb(180, 50, 200, 50), 1))
                    {
                        e.Graphics.DrawRectangle(borderPen, ann.TextX, ann.TextY, ann.TextWidth, ann.TextHeight);
                    }
                    // 绘制文本
                    if (!string.IsNullOrEmpty(ann.Text))
                    {
                        e.Graphics.DrawString(ann.Text, new Font("微软雅黑", 9F), 
                            Brushes.DarkGreen, ann.TextX + 3, ann.TextY + 3);
                    }
                    // 绘制箭头
                    if (ann.ArrowEndX.HasValue && ann.ArrowEndY.HasValue)
                    {
                        using (Pen arrowPen = new Pen(Color.Red, 2))
                        {
                            arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                            int startX = ann.TextX + ann.TextWidth / 2;
                            int startY = ann.TextY + ann.TextHeight / 2;
                            e.Graphics.DrawLine(arrowPen, startX, startY, ann.ArrowEndX.Value, ann.ArrowEndY.Value);
                        }
                    }
                }
            }
        }

        private void PbImage_MouseClick(object sender, MouseEventArgs e)
        {
            if (_currentRegions == null) return;

            // 检查是否点击了某个区域
            foreach (var region in _currentRegions)
            {
                if (e.X >= region.X && e.X <= region.X + region.Width &&
                    e.Y >= region.Y && e.Y <= region.Y + region.Height)
                {
                    if (region.TargetPageId.HasValue)
                    {
                        _pageHistory.Push(_currentPageId);
                        LoadPage(region.TargetPageId.Value);
                        btnBack.Enabled = _pageHistory.Count > 0;
                    }
                    return;
                }
            }
        }

        private void GoBack()
        {
            if (_pageHistory.Count > 0)
            {
                int prevPageId = _pageHistory.Pop();
                LoadPage(prevPageId);
                btnBack.Enabled = _pageHistory.Count > 0;
            }
        }
    }
}
