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
            this.btnManage = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.btnManage.Location = new System.Drawing.Point(120, 100);
            this.btnManage.Size = new System.Drawing.Size(160, 45);
            this.btnManage.Text = "流程维护";
            this.btnManage.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnManage.Click += new System.EventHandler(this.btnManage_Click);
            this.btnRun.Location = new System.Drawing.Point(120, 170);
            this.btnRun.Size = new System.Drawing.Size(160, 45);
            this.btnRun.Text = "运行流程";
            this.btnRun.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(110, 50);
            this.label1.Size = new System.Drawing.Size(180, 24);
            this.label1.Text = "截图操作流程系统";
            this.label1.Font = new System.Drawing.Font("微软雅黑", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ClientSize = new System.Drawing.Size(400, 280);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.btnManage);
            this.Text = "截图操作流程系统";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
}