using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class FormAnnotationEditor : Form
    {
        private Database _db;
        private int _pageId;
        private Image _pageImage;
        private List<PageAnnotation> _annotations;
        private PageAnnotation _selectedAnnotation;
        private bool _isDraggingText = false;
        private bool _isDraggingArrow = false;
        private bool _isResizing = false;
        private Point _dragStart;
        private PictureBox pbImage;
        private Button btnAddAnnotation;
        private Button btnDeleteAnnotation;
        private Button btnSave;
        private Button btnCancel;
        private TextBox txtAnnotationText;
        private Label label1;

        public FormAnnotationEditor(Database db, int pageId, Image pageImage)
        {
            _db = db;
            _pageId = pageId;
            _pageImage = pageImage;
            _annotations = db.GetAnnotationsByPageId(pageId);
            InitializeComponent();
            pbImage.Image = _pageImage;
        }

        private void InitializeComponent()
        {
            pbImage = new PictureBox();
            btnAddAnnotation = new Button();
            btnDeleteAnnotation = new Button();
            btnSave = new Button();
            btnCancel = new Button();
            txtAnnotationText = new TextBox();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)pbImage).BeginInit();
            SuspendLayout();
            // 
            // pbImage
            // 
            pbImage.BorderStyle = BorderStyle.FixedSingle;
            pbImage.Location = new Point(20, 60);
            pbImage.Name = "pbImage";
            pbImage.Size = new Size(1500, 1000);
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbImage.TabIndex = 0;
            pbImage.TabStop = false;
            pbImage.Paint += pbImage_Paint;
            pbImage.MouseDown += pbImage_MouseDown;
            pbImage.MouseMove += pbImage_MouseMove;
            pbImage.MouseUp += pbImage_MouseUp;
            // 
            // btnAddAnnotation
            // 
            btnAddAnnotation.Font = new Font("微软雅黑", 11F, FontStyle.Regular, GraphicsUnit.Point);
            btnAddAnnotation.Location = new Point(1061, 12);
            btnAddAnnotation.Name = "btnAddAnnotation";
            btnAddAnnotation.Size = new Size(100, 35);
            btnAddAnnotation.TabIndex = 3;
            btnAddAnnotation.Text = "添加注释";
            btnAddAnnotation.Click += btnAddAnnotation_Click;
            // 
            // btnDeleteAnnotation
            // 
            btnDeleteAnnotation.Font = new Font("微软雅黑", 11F, FontStyle.Regular, GraphicsUnit.Point);
            btnDeleteAnnotation.Location = new Point(1167, 12);
            btnDeleteAnnotation.Name = "btnDeleteAnnotation";
            btnDeleteAnnotation.Size = new Size(100, 35);
            btnDeleteAnnotation.TabIndex = 4;
            btnDeleteAnnotation.Text = "删除注释";
            btnDeleteAnnotation.Click += btnDeleteAnnotation_Click;
            // 
            // btnSave
            // 
            btnSave.Font = new Font("微软雅黑", 11F, FontStyle.Regular, GraphicsUnit.Point);
            btnSave.Location = new Point(1300, 10);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 35);
            btnSave.TabIndex = 5;
            btnSave.Text = "保存";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Font = new Font("微软雅黑", 11F, FontStyle.Regular, GraphicsUnit.Point);
            btnCancel.Location = new Point(1410, 10);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 35);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "取消";
            btnCancel.Click += btnCancel_Click;
            // 
            // txtAnnotationText
            // 
            txtAnnotationText.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtAnnotationText.Location = new Point(141, 13);
            txtAnnotationText.Name = "txtAnnotationText";
            txtAnnotationText.Size = new Size(878, 34);
            txtAnnotationText.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(20, 15);
            label1.Name = "label1";
            label1.Size = new Size(97, 27);
            label1.TabIndex = 1;
            label1.Text = "注释文本:";
            // 
            // FormAnnotationEditor
            // 
            ClientSize = new Size(1550, 1100);
            Controls.Add(pbImage);
            Controls.Add(label1);
            Controls.Add(txtAnnotationText);
            Controls.Add(btnAddAnnotation);
            Controls.Add(btnDeleteAnnotation);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            MaximizeBox = false;
            Name = "FormAnnotationEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "注释编辑器";
            ((System.ComponentModel.ISupportInitialize)pbImage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void pbImage_Paint(object sender, PaintEventArgs e)
        {
            foreach (var annotation in _annotations)
            {
                bool isSelected = annotation == _selectedAnnotation;
                Color borderColor = isSelected ? Color.Red : Color.Blue;
                Color bgColor = Color.FromArgb(180, Color.Yellow);

                using (Pen pen = new Pen(borderColor, isSelected ? 3 : 2))
                {
                    e.Graphics.DrawRectangle(pen, annotation.TextX, annotation.TextY, annotation.TextWidth, annotation.TextHeight);
                }

                using (Brush brush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(brush, annotation.TextX + 2, annotation.TextY + 2, annotation.TextWidth - 4, annotation.TextHeight - 4);
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

                    using (Pen arrowPen = new Pen(isSelected ? Color.Red : Color.Blue, 2))
                    {
                        arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(8, 8);
                        e.Graphics.DrawLine(arrowPen, startX, startY, endX, endY);
                    }

                    using (Brush handleBrush = new SolidBrush(isSelected ? Color.Red : Color.Blue))
                    {
                        e.Graphics.FillEllipse(handleBrush, endX - 6, endY - 6, 12, 12);
                    }
                }

                if (isSelected)
                {
                    int resizeHandleX = annotation.TextX + annotation.TextWidth - 8;
                    int resizeHandleY = annotation.TextY + annotation.TextHeight - 8;
                    using (Brush handleBrush = new SolidBrush(Color.Red))
                    {
                        e.Graphics.FillRectangle(handleBrush, resizeHandleX, resizeHandleY, 8, 8);
                    }
                }
            }
        }

        private void pbImage_MouseDown(object sender, MouseEventArgs e)
        {
            foreach (var annotation in _annotations)
            {
                if (annotation.ArrowEndX.HasValue && annotation.ArrowEndY.HasValue)
                {
                    Rectangle arrowHandle = new Rectangle(annotation.ArrowEndX.Value - 8, annotation.ArrowEndY.Value - 8, 16, 16);
                    if (arrowHandle.Contains(e.Location))
                    {
                        _selectedAnnotation = annotation;
                        _isDraggingArrow = true;
                        _dragStart = e.Location;
                        txtAnnotationText.Text = annotation.Text ?? "";
                        pbImage.Invalidate();
                        return;
                    }
                }

                Rectangle resizeHandle = new Rectangle(
                    annotation.TextX + annotation.TextWidth - 10,
                    annotation.TextY + annotation.TextHeight - 10,
                    10, 10);
                if (resizeHandle.Contains(e.Location))
                {
                    _selectedAnnotation = annotation;
                    _isResizing = true;
                    _dragStart = e.Location;
                    txtAnnotationText.Text = annotation.Text ?? "";
                    pbImage.Invalidate();
                    return;
                }

                Rectangle textRect = new Rectangle(annotation.TextX, annotation.TextY, annotation.TextWidth, annotation.TextHeight);
                if (textRect.Contains(e.Location))
                {
                    _selectedAnnotation = annotation;
                    _isDraggingText = true;
                    _dragStart = e.Location;
                    txtAnnotationText.Text = annotation.Text ?? "";
                    pbImage.Invalidate();
                    return;
                }
            }

            _selectedAnnotation = null;
            txtAnnotationText.Text = "";
            pbImage.Invalidate();
        }

        private void pbImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedAnnotation == null) return;

            int dx = e.X - _dragStart.X;
            int dy = e.Y - _dragStart.Y;

            if (_isDraggingText)
            {
                _selectedAnnotation.TextX += dx;
                _selectedAnnotation.TextY += dy;
                _dragStart = e.Location;
                pbImage.Invalidate();
            }
            else if (_isDraggingArrow)
            {
                _selectedAnnotation.ArrowEndX = e.X;
                _selectedAnnotation.ArrowEndY = e.Y;
                _dragStart = e.Location;
                pbImage.Invalidate();
            }
            else if (_isResizing)
            {
                _selectedAnnotation.TextWidth = Math.Max(50, _selectedAnnotation.TextWidth + dx);
                _selectedAnnotation.TextHeight = Math.Max(30, _selectedAnnotation.TextHeight + dy);
                _dragStart = e.Location;
                pbImage.Invalidate();
            }
        }

        private void pbImage_MouseUp(object sender, MouseEventArgs e)
        {
            _isDraggingText = false;
            _isDraggingArrow = false;
            _isResizing = false;
        }

        private void btnAddAnnotation_Click(object sender, EventArgs e)
        {
            var newAnnotation = new PageAnnotation
            {
                PageId = _pageId,
                TextX = 100,
                TextY = 100,
                TextWidth = 150,
                TextHeight = 60,
                Text = txtAnnotationText.Text,
                ArrowEndX = null,
                ArrowEndY = null
            };
            _annotations.Add(newAnnotation);
            _selectedAnnotation = newAnnotation;
            pbImage.Invalidate();
        }

        private void btnDeleteAnnotation_Click(object sender, EventArgs e)
        {
            if (_selectedAnnotation != null)
            {
                _annotations.Remove(_selectedAnnotation);
                _selectedAnnotation = null;
                txtAnnotationText.Text = "";
                pbImage.Invalidate();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_selectedAnnotation != null)
            {
                _selectedAnnotation.Text = txtAnnotationText.Text;
            }

            var existingIds = new HashSet<int>();
            foreach (var annotation in _annotations)
            {
                if (annotation.Id > 0)
                {
                    existingIds.Add(annotation.Id);
                }
            }

            var dbAnnotations = _db.GetAnnotationsByPageId(_pageId);
            foreach (var dbAnnotation in dbAnnotations)
            {
                if (!existingIds.Contains(dbAnnotation.Id))
                {
                    _db.DeleteAnnotation(dbAnnotation.Id);
                }
            }

            foreach (var annotation in _annotations)
            {
                if (annotation.Id > 0)
                {
                    _db.UpdateAnnotation(annotation);
                }
                else
                {
                    annotation.Id = _db.AddAnnotation(annotation);
                }
            }

            MessageBox.Show("注释已保存");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
