using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class FormAttachmentManage : Form
    {
        private Database _db;
        private int _pageId;
        private string _pageName;

        private Label lblTitle;
        private DataGridView dgvAttachments;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnDownload;
        private Button btnClose;
        private Label lblTip;

        private List<PageAttachment> _attachments = new List<PageAttachment>();

        public FormAttachmentManage(Database db, int pageId, string pageName)
        {
            _db = db;
            _pageId = pageId;
            _pageName = pageName;
            InitializeComponent();
            LoadAttachments();
        }

        private void InitializeComponent()
        {
            this.Text = "附件管理";
            this.ClientSize = new Size(780, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("微软雅黑", 10F);
            this.BackColor = Color.FromArgb(245, 246, 247);

            lblTitle = new Label();
            lblTitle.Text = $"附件管理 - 页面：{_pageName}";
            lblTitle.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblTitle.Location = new Point(20, 12);
            lblTitle.Size = new Size(740, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblTitle);

            dgvAttachments = new DataGridView();
            dgvAttachments.Location = new Point(20, 55);
            dgvAttachments.Size = new Size(740, 340);
            dgvAttachments.AllowUserToAddRows = false;
            dgvAttachments.ReadOnly = true;
            dgvAttachments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAttachments.MultiSelect = false;
            dgvAttachments.RowHeadersVisible = false;
            dgvAttachments.BackgroundColor = Color.White;
            dgvAttachments.BorderStyle = BorderStyle.FixedSingle;
            dgvAttachments.ColumnHeadersHeight = 35;
            dgvAttachments.Font = new Font("微软雅黑", 10F);
            dgvAttachments.Columns.Add("Id", "ID");
            dgvAttachments.Columns.Add("FileName", "文件名");
            dgvAttachments.Columns.Add("FileSize", "大小");
            dgvAttachments.Columns.Add("Remark", "备注");
            dgvAttachments.Columns.Add("CreateTime", "上传时间");
            dgvAttachments.Columns["Id"].Width = 60;
            dgvAttachments.Columns["FileName"].Width = 200;
            dgvAttachments.Columns["FileSize"].Width = 90;
            dgvAttachments.Columns["Remark"].Width = 220;
            dgvAttachments.Columns["CreateTime"].Width = 150;
            dgvAttachments.SelectionChanged += (s, e) => UpdateButtonState();
            this.Controls.Add(dgvAttachments);

            btnAdd = CreateButton("添加", 20, 405, Color.FromArgb(40, 167, 69));
            btnAdd.Click += (s, e) => AddAttachment();
            this.Controls.Add(btnAdd);

            btnDelete = CreateButton("删除", 110, 405, Color.FromArgb(220, 53, 69));
            btnDelete.Click += (s, e) => DeleteAttachment();
            this.Controls.Add(btnDelete);

            btnDownload = CreateButton("下载/打开", 200, 405, Color.FromArgb(0, 120, 215));
            btnDownload.Click += (s, e) => DownloadAndOpen();
            this.Controls.Add(btnDownload);

            btnClose = CreateButton("关闭", 670, 405, Color.FromArgb(108, 117, 125));
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            lblTip = new Label();
            lblTip.Text = "提示：双击行也可下载并打开附件";
            lblTip.ForeColor = Color.FromArgb(120, 120, 120);
            lblTip.Location = new Point(310, 410);
            lblTip.Size = new Size(350, 25);
            this.Controls.Add(lblTip);

            this.AcceptButton = btnDownload;
            this.CancelButton = btnClose;

            dgvAttachments.DoubleClick += (s, e) => DownloadAndOpen();

            UpdateButtonState();
        }

        private Button CreateButton(string text, int x, int y, Color backColor)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(85, 32);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("微软雅黑", 10F);
            return btn;
        }

        private void LoadAttachments()
        {
            _attachments = _db.GetAttachmentsByPageId(_pageId);
            dgvAttachments.Rows.Clear();
            foreach (var att in _attachments)
            {
                dgvAttachments.Rows.Add(att.Id, att.FileName, FormatFileSize(att.FileSize), att.Remark ?? "", att.CreateTime.ToString("yyyy-MM-dd HH:mm"));
            }
            UpdateButtonState();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1024.0 / 1024.0:F2} MB";
        }

        private void UpdateButtonState()
        {
            bool hasSelection = dgvAttachments.SelectedRows.Count > 0;
            btnDelete.Enabled = hasSelection;
            btnDownload.Enabled = hasSelection;
        }

        private void AddAttachment()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "选择要上传的附件文件";
                ofd.Filter = "所有文件|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(ofd.FileName);
                        var attachment = new PageAttachment
                        {
                            PageId = _pageId,
                            FileName = Path.GetFileName(ofd.FileName),
                            FileData = data,
                            FileSize = data.Length,
                            Remark = "",
                            CreateTime = DateTime.Now
                        };
                        _db.AddAttachment(attachment);
                        LoadAttachments();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"添加附件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DeleteAttachment()
        {
            if (dgvAttachments.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAttachments.SelectedRows[0].Cells["Id"].Value);
            string name = dgvAttachments.SelectedRows[0].Cells["FileName"].Value.ToString();
            if (MessageBox.Show($"确定删除附件「{name}」吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _db.DeleteAttachment(id);
                LoadAttachments();
            }
        }

        private void DownloadAndOpen()
        {
            if (dgvAttachments.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAttachments.SelectedRows[0].Cells["Id"].Value);
            var att = _db.GetAttachmentById(id);
            if (att == null || att.FileData == null)
            {
                MessageBox.Show("附件不存在", "提示");
                return;
            }

            try
            {
                // 保存到临时目录并打开
                string tempDir = Path.Combine(Path.GetTempPath(), "ScreenshotProcessApp_Attachments");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                // 使用时间戳前缀避免覆盖同名文件
                string safeName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{att.FileName}";
                string tempFile = Path.Combine(tempDir, safeName);
                File.WriteAllBytes(tempFile, att.FileData);

                // 用系统默认程序打开
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开附件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
