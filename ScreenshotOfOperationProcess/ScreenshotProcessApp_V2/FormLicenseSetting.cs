using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class FormLicenseSetting : Form
    {
        private Database _db;
        private Label lblTitle;
        private Label lblCurrent;
        private Label lblCurrentDate;
        private Label lblNewDate;
        private DateTimePicker dtpExpire;
        private Button btnSave;
        private Button btnClose;
        private Label lblTip;

        public FormLicenseSetting(Database db)
        {
            _db = db;
            InitializeComponent();
            LoadCurrentDate();
        }

        private void InitializeComponent()
        {
            this.Text = "授权设置";
            this.ClientSize = new Size(420, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("微软雅黑", 10F);
            this.BackColor = Color.FromArgb(245, 246, 247);

            lblTitle = new Label();
            lblTitle.Text = "授权过期时间设置";
            lblTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(380, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            lblCurrent = new Label();
            lblCurrent.Text = "当前授权到期日：";
            lblCurrent.Location = new Point(40, 65);
            lblCurrent.Size = new Size(140, 25);
            lblCurrent.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblCurrent);

            lblCurrentDate = new Label();
            lblCurrentDate.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            lblCurrentDate.ForeColor = Color.FromArgb(0, 120, 215);
            lblCurrentDate.Location = new Point(180, 65);
            lblCurrentDate.Size = new Size(200, 25);
            lblCurrentDate.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblCurrentDate);

            lblNewDate = new Label();
            lblNewDate.Text = "设置新的到期日：";
            lblNewDate.Location = new Point(40, 110);
            lblNewDate.Size = new Size(140, 25);
            lblNewDate.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblNewDate);

            dtpExpire = new DateTimePicker();
            dtpExpire.Format = DateTimePickerFormat.Long;
            dtpExpire.Location = new Point(180, 108);
            dtpExpire.Size = new Size(200, 25);
            dtpExpire.MinDate = new DateTime(2020, 1, 1);
            dtpExpire.MaxDate = new DateTime(2099, 12, 31);
            dtpExpire.Value = DateTime.Today.AddYears(1);
            this.Controls.Add(dtpExpire);

            lblTip = new Label();
            lblTip.Text = "提示：到期后程序将无法启动，请提前更新授权。";
            lblTip.ForeColor = Color.FromArgb(180, 80, 80);
            lblTip.Location = new Point(40, 150);
            lblTip.Size = new Size(340, 30);
            this.Controls.Add(lblTip);

            btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.Font = new Font("微软雅黑", 10F);
            btnSave.Location = new Point(120, 195);
            btnSave.Size = new Size(90, 35);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.BackColor = Color.FromArgb(0, 120, 215);
            btnSave.ForeColor = Color.White;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnClose = new Button();
            btnClose.Text = "关闭";
            btnClose.Font = new Font("微软雅黑", 10F);
            btnClose.Location = new Point(230, 195);
            btnClose.Size = new Size(90, 35);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.BackColor = Color.FromArgb(100, 100, 100);
            btnClose.ForeColor = Color.White;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            this.AcceptButton = btnSave;
            this.CancelButton = btnClose;
        }

        private void LoadCurrentDate()
        {
            DateTime current = _db.GetLicenseExpireDate();
            lblCurrentDate.Text = current.ToString("yyyy-MM-dd");

            bool expired = current.Date < DateTime.Today;
            if (expired)
            {
                lblCurrentDate.ForeColor = Color.FromArgb(220, 53, 69);
                lblTip.Text = $"警告：授权已于 {current:yyyy-MM-dd} 过期！请立即更新。";
            }
            else
            {
                int daysLeft = (current - DateTime.Today).Days;
                lblTip.Text = $"剩余有效期：{daysLeft} 天（{current:yyyy-MM-dd} 到期）";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            DateTime newDate = dtpExpire.Value.Date;

            if (_db.SetLicenseExpireDate(newDate))
            {
                MessageBox.Show($"授权到期日已更新为 {newDate:yyyy-MM-dd}", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCurrentDate();
            }
            else
            {
                MessageBox.Show("保存失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
