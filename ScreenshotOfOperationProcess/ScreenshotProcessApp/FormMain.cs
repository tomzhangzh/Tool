using System;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public partial class FormMain : Form
    {
        private Database _db;

        public FormMain()
        {
            InitializeComponent();
            string dbPath = System.IO.Path.Combine(Application.StartupPath, "process.db");
            _db = new Database(dbPath);
        }

        private void btnManage_Click(object sender, EventArgs e)
        {
            FormManage form = new FormManage(_db);
            form.ShowDialog();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            FormRun form = new FormRun(_db);
            form.ShowDialog();
        }

        #region Windows Form Designer generated code
        private System.ComponentModel.IContainer components = null;
        private Button btnManage;
        private Button btnRun;
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
            btnManage = new Button();
            btnRun = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // btnManage
            // 
            btnManage.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            btnManage.Location = new Point(165, 147);
            btnManage.Name = "btnManage";
            btnManage.Size = new Size(239, 45);
            btnManage.TabIndex = 2;
            btnManage.Text = "流程维护";
            btnManage.Click += btnManage_Click;
            // 
            // btnRun
            // 
            btnRun.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            btnRun.Location = new Point(165, 217);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(239, 45);
            btnRun.TabIndex = 1;
            btnRun.Text = "运行流程";
            btnRun.Click += btnRun_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("微软雅黑", 15F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(185, 50);
            label1.Name = "label1";
            label1.Size = new Size(215, 33);
            label1.TabIndex = 0;
            label1.Text = "截图操作流程系统";
            label1.Click += label1_Click;
            // 
            // FormMain
            // 
            ClientSize = new Size(555, 381);
            Controls.Add(label1);
            Controls.Add(btnRun);
            Controls.Add(btnManage);
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "截图操作流程系统";
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}