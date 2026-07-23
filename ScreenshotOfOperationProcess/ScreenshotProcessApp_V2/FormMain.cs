using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public partial class FormMain : Form
    {
        private Database _db;
        private Panel navPanel;
        private Panel contentPanel;
        private Label lblTitle;
        private Button[] navButtons;
        private int _activeIndex = -1;

        public FormMain()
        {
            string dbPath = System.IO.Path.Combine(Application.StartupPath, "process.db");
            _db = new Database(dbPath);
            InitializeComponent();
            ShowContent(0);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "截图操作流程系统";
            this.ClientSize = new Size(1800, 1160);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 800);
            this.Font = new Font("微软雅黑", 9F);
            this.BackColor = Color.FromArgb(245, 246, 247);

            // 左侧导航栏
            navPanel = new Panel();
            navPanel.Dock = DockStyle.Left;
            navPanel.Width = 200;
            navPanel.BackColor = Color.FromArgb(48, 53, 65);
            navPanel.Paint += NavPanel_Paint;

            // 标题
            lblTitle = new Label();
            lblTitle.Text = "流程管理系统";
            lblTitle.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 60;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            navPanel.Controls.Add(lblTitle);

            // 导航按钮
            string[] navItems = { "流程结构", "流程目录", "流程管理", "页面管理", "区域管理", "注释管理", "运行流程" };
            navButtons = new Button[navItems.Length];
            int yPos = 70;
            for (int i = 0; i < navItems.Length; i++)
            {
                int index = i;
                navButtons[i] = new Button();
                navButtons[i].Text = "  " + navItems[i];
                navButtons[i].Font = new Font("微软雅黑", 11F);
                navButtons[i].Location = new Point(0, yPos);
                navButtons[i].Size = new Size(200, 50);
                navButtons[i].FlatStyle = FlatStyle.Flat;
                navButtons[i].FlatAppearance.BorderSize = 0;
                navButtons[i].FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 68, 82);
                navButtons[i].BackColor = Color.FromArgb(48, 53, 65);
                navButtons[i].ForeColor = Color.White;
                navButtons[i].TextAlign = ContentAlignment.MiddleLeft;
                navButtons[i].Cursor = Cursors.Hand;
                navButtons[i].Click += (s, e) => ShowContent(index);
                navPanel.Controls.Add(navButtons[i]);
                yPos += 55;
            }

            this.Controls.Add(navPanel);

            // 右侧内容区
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(245, 246, 247);
            contentPanel.Padding = new Padding(10);
            contentPanel.AutoScroll = true;
            this.Controls.Add(contentPanel);
            contentPanel.BringToFront();

            this.ResumeLayout(false);
        }

        private void NavPanel_Paint(object sender, PaintEventArgs e)
        {
            // 顶部标题区分隔线
            using (Pen pen = new Pen(Color.FromArgb(80, 84, 96), 1))
            {
                e.Graphics.DrawLine(pen, 0, 59, navPanel.Width, 59);
            }
        }

        private void ShowContent(int index)
        {
            // 更新导航按钮样式
            if (_activeIndex >= 0 && _activeIndex < navButtons.Length)
            {
                navButtons[_activeIndex].BackColor = Color.FromArgb(48, 53, 65);
            }
            _activeIndex = index;
            navButtons[index].BackColor = Color.FromArgb(0, 120, 215);

            contentPanel.Controls.Clear();
            UserControl uc = null;
            switch (index)
            {
                case 0: uc = new UcProcessStructure(_db); break;
                case 1: uc = new UcProcessCatalog(_db); break;
                case 2: uc = new UcFlowManage(_db); break;
                case 3: uc = new UcPageManage(_db); break;
                case 4: uc = new UcRegionManage(_db); break;
                case 5: uc = new UcAnnotationManage(_db); break;
                case 6: uc = new UcFlowRun(_db); break;
            }
            if (uc != null)
            {
                uc.Dock = DockStyle.Fill;
                contentPanel.Controls.Add(uc);
            }
        }
    }
}
