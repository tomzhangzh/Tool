using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcImportData : UserControl
    {
        private Database _targetDb;  // process.db (目标库)
        private Database _sourceDb;  // 选中的源文件
        private string _sourceDbPath = "";

        private Label lblPageTitle;
        private Button btnSelectFile;
        private Label lblFileName;
        private Button btnRefresh;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private Button btnImport;
        private TreeView treeView;
        private ImageList imageList;
        private ImageList stateImageList;
        private Label lblStatus;
        private Label lblRecursionWarn;

        // 状态图标索引（手动管理复选框）
        private const int STATE_UNCHECKED = 0;
        private const int STATE_CHECKED = 1;
        private const int STATE_BLANK = 2;

        // 节点类型
        private enum NodeType { Flow, Page, CycleWarning, Empty }

        private class NodeInfo
        {
            public NodeType Type;
            public int Id;
            public int FlowId;
        }

        public UcImportData(Database db)
        {
            _targetDb = db;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 标题
            lblPageTitle = new Label();
            lblPageTitle.Text = "导入数据";
            lblPageTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            lblPageTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblPageTitle.Location = new Point(10, 5);
            lblPageTitle.Size = new Size(300, 35);
            this.Controls.Add(lblPageTitle);

            // 选择文件按钮
            btnSelectFile = new Button();
            btnSelectFile.Text = "选择文件...";
            btnSelectFile.Location = new Point(320, 8);
            btnSelectFile.Size = new Size(100, 30);
            btnSelectFile.FlatStyle = FlatStyle.Flat;
            btnSelectFile.BackColor = Color.FromArgb(0, 120, 215);
            btnSelectFile.ForeColor = Color.White;
            btnSelectFile.Cursor = Cursors.Hand;
            btnSelectFile.Font = new Font("微软雅黑", 9F);
            btnSelectFile.Click += (s, e) => SelectSourceFile();
            this.Controls.Add(btnSelectFile);

            // 文件名标签
            lblFileName = new Label();
            lblFileName.Text = "（未选择文件）";
            lblFileName.Location = new Point(430, 12);
            lblFileName.Size = new Size(380, 22);
            lblFileName.Font = new Font("微软雅黑", 9F);
            lblFileName.ForeColor = Color.Gray;
            lblFileName.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblFileName);

            // 刷新按钮
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Location = new Point(820, 8);
            btnRefresh.Size = new Size(65, 30);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.BackColor = Color.FromArgb(108, 117, 125);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Font = new Font("微软雅黑", 9F);
            btnRefresh.Click += (s, e) => LoadSourceTree();
            this.Controls.Add(btnRefresh);

            // 全选按钮
            btnSelectAll = new Button();
            btnSelectAll.Text = "全选";
            btnSelectAll.Location = new Point(895, 8);
            btnSelectAll.Size = new Size(65, 30);
            btnSelectAll.FlatStyle = FlatStyle.Flat;
            btnSelectAll.BackColor = Color.FromArgb(40, 167, 69);
            btnSelectAll.ForeColor = Color.White;
            btnSelectAll.Cursor = Cursors.Hand;
            btnSelectAll.Font = new Font("微软雅黑", 9F);
            btnSelectAll.Click += (s, e) => SelectAllFlows(true);
            this.Controls.Add(btnSelectAll);

            // 全不选按钮
            btnDeselectAll = new Button();
            btnDeselectAll.Text = "全不选";
            btnDeselectAll.Location = new Point(970, 8);
            btnDeselectAll.Size = new Size(70, 30);
            btnDeselectAll.FlatStyle = FlatStyle.Flat;
            btnDeselectAll.BackColor = Color.FromArgb(108, 117, 125);
            btnDeselectAll.ForeColor = Color.White;
            btnDeselectAll.Cursor = Cursors.Hand;
            btnDeselectAll.Font = new Font("微软雅黑", 9F);
            btnDeselectAll.Click += (s, e) => SelectAllFlows(false);
            this.Controls.Add(btnDeselectAll);

            // 导入按钮
            btnImport = new Button();
            btnImport.Text = "导入选中流程";
            btnImport.Location = new Point(1050, 8);
            btnImport.Size = new Size(130, 30);
            btnImport.FlatStyle = FlatStyle.Flat;
            btnImport.BackColor = Color.FromArgb(255, 193, 7);
            btnImport.ForeColor = Color.Black;
            btnImport.Cursor = Cursors.Hand;
            btnImport.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            btnImport.Click += (s, e) => ImportSelectedFlows();
            this.Controls.Add(btnImport);

            // 状态标签
            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Location = new Point(10, 42);
            lblStatus.Size = new Size(800, 20);
            lblStatus.Font = new Font("微软雅黑", 9F);
            lblStatus.ForeColor = Color.FromArgb(0, 120, 215);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblStatus);

            // 递归提示标签
            lblRecursionWarn = new Label();
            lblRecursionWarn.Text = "";
            lblRecursionWarn.Location = new Point(810, 42);
            lblRecursionWarn.Size = new Size(800, 20);
            lblRecursionWarn.Font = new Font("微软雅黑", 9F);
            lblRecursionWarn.ForeColor = Color.FromArgb(220, 53, 69);
            lblRecursionWarn.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblRecursionWarn);

            // 类型图标列表
            imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.Images.Add("Flow", CreateIcon(Color.FromArgb(0, 120, 215)));
            imageList.Images.Add("Page", CreateIcon(Color.FromArgb(40, 167, 69)));
            imageList.Images.Add("Cycle", CreateIcon(Color.FromArgb(220, 53, 69)));

            // 状态图标列表（手动复选框）
            stateImageList = new ImageList();
            stateImageList.ImageSize = new Size(16, 16);
            stateImageList.ColorDepth = ColorDepth.Depth32Bit;
            stateImageList.Images.Add(CreateCheckboxIcon(false));  // 0: 未勾选
            stateImageList.Images.Add(CreateCheckboxIcon(true));   // 1: 已勾选
            Bitmap blank = new Bitmap(16, 16);                     // 2: 空白（无复选框）
            stateImageList.Images.Add(blank);

            // 树视图
            treeView = new TreeView();
            treeView.Location = new Point(10, 70);
            treeView.Size = new Size(1550, 980);
            treeView.Font = new Font("微软雅黑", 10F);
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.BackColor = Color.White;
            treeView.ImageList = imageList;
            treeView.StateImageList = stateImageList;
            treeView.ShowNodeToolTips = true;
            treeView.HideSelection = false;
            treeView.CheckBoxes = false;  // 手动管理复选框
            treeView.NodeMouseClick += TreeView_NodeMouseClick;
            this.Controls.Add(treeView);

            this.ResumeLayout(false);
        }

        // 选择源数据文件
        private void SelectSourceFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "SQLite数据库文件 (*.db)|*.db|所有文件 (*.*)|*.*";
                ofd.Title = "选择要导入的数据文件";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // 防止选中当前正在使用的 process.db
                    string targetPath = Path.Combine(Application.StartupPath, "process.db");
                    try
                    {
                        if (string.Equals(Path.GetFullPath(ofd.FileName), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("源文件不能与当前数据库相同", "提示");
                            return;
                        }
                    }
                    catch { }

                    _sourceDbPath = ofd.FileName;
                    lblFileName.Text = Path.GetFileName(_sourceDbPath);
                    lblFileName.ForeColor = Color.FromArgb(48, 53, 65);
                    LoadSourceTree();
                }
            }
        }

        // 加载源文件树（结构同流程目录，流程节点带复选框）
        private void LoadSourceTree()
        {
            if (string.IsNullOrEmpty(_sourceDbPath))
            {
                MessageBox.Show("请先选择源数据文件", "提示");
                return;
            }

            _sourceDb = new Database(_sourceDbPath);

            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            lblRecursionWarn.Text = "";

            int totalRecursion = 0;
            var flows = _sourceDb.GetAllFlows();
            foreach (var flow in flows)
            {
                TreeNode flowNode = new TreeNode();
                flowNode.Text = flow.Name + (flow.StartPageId > 0 ? "" : " (未设置起始页)");
                flowNode.ImageKey = "Flow";
                flowNode.SelectedImageKey = "Flow";
                flowNode.Tag = new NodeInfo { Type = NodeType.Flow, Id = flow.Id };
                flowNode.StateImageIndex = STATE_UNCHECKED;  // 复选框（未勾选）
                flowNode.ToolTipText = $"ID: {flow.Id}\r\n描述: {flow.Description ?? "无"}\r\n创建时间: {flow.CreateTime:yyyy-MM-dd HH:mm}\r\n勾选后将导入此流程";

                if (flow.StartPageId > 0)
                {
                    var path = new HashSet<int>();
                    totalRecursion += BuildNavigationSubtree(flowNode, flow.StartPageId, flow.Id, path, null);
                }
                else
                {
                    TreeNode warn = new TreeNode("（未设置起始页，无法生成目录）");
                    warn.ForeColor = Color.Gray;
                    warn.Tag = new NodeInfo { Type = NodeType.Empty };
                    warn.StateImageIndex = STATE_BLANK;
                    flowNode.Nodes.Add(warn);
                }

                treeView.Nodes.Add(flowNode);
            }

            if (treeView.Nodes.Count == 0)
            {
                TreeNode empty = new TreeNode("（源文件中暂无流程）");
                empty.ForeColor = Color.Gray;
                empty.StateImageIndex = STATE_BLANK;
                treeView.Nodes.Add(empty);
            }

            if (totalRecursion > 0)
            {
                lblRecursionWarn.Text = $"检测到 {totalRecursion} 处递归引用，对应分支已停止展开";
            }

            treeView.ExpandAll();
            treeView.EndUpdate();
        }

        // 构建导航子树（同流程目录逻辑，递归检测）
        private int BuildNavigationSubtree(TreeNode parentNode, int pageId, int rootFlowId, HashSet<int> path, PageRegion sourceRegion)
        {
            // 递归检测
            if (path.Contains(pageId))
            {
                var cyclePage = _sourceDb.GetPageById(pageId);
                string cycleName = cyclePage?.Name ?? $"页面#{pageId}";
                TreeNode cycleNode = new TreeNode();
                cycleNode.Text = $"[递归] → {cycleName} (已在路径中，停止展开)";
                cycleNode.ForeColor = Color.Red;
                cycleNode.ImageKey = "Cycle";
                cycleNode.SelectedImageKey = "Cycle";
                cycleNode.Tag = new NodeInfo { Type = NodeType.CycleWarning, Id = pageId, FlowId = rootFlowId };
                cycleNode.StateImageIndex = STATE_BLANK;
                cycleNode.ToolTipText = $"检测到递归：目标页面「{cycleName}」(ID:{pageId}) 已在当前导航路径中。";
                parentNode.Nodes.Add(cycleNode);
                return 1;
            }

            var page = _sourceDb.GetPageById(pageId);
            if (page == null)
            {
                TreeNode warn = new TreeNode($"[页面不存在] ID={pageId}");
                warn.ForeColor = Color.Gray;
                warn.Tag = new NodeInfo { Type = NodeType.Empty };
                warn.StateImageIndex = STATE_BLANK;
                parentNode.Nodes.Add(warn);
                return 0;
            }

            // 若目标页面属于其他流程，显示流程名提示
            string flowHint = "";
            if (page.FlowId != rootFlowId)
            {
                var targetFlow = _sourceDb.GetFlowById(page.FlowId);
                if (targetFlow != null)
                {
                    flowHint = $" [所属流程: {targetFlow.Name}]";
                }
            }

            // 来源区域信息
            string regionHint = "";
            if (sourceRegion != null)
            {
                string remark = string.IsNullOrEmpty(sourceRegion.Remark) ? "" : $": {sourceRegion.Remark}";
                regionHint = $" (区域#{sourceRegion.Id}{remark})";
            }

            TreeNode pageNode = new TreeNode();
            pageNode.Text = page.Name + flowHint + regionHint;
            pageNode.ImageKey = "Page";
            pageNode.SelectedImageKey = "Page";
            pageNode.Tag = new NodeInfo { Type = NodeType.Page, Id = page.Id, FlowId = page.FlowId };
            pageNode.StateImageIndex = STATE_BLANK;  // 页面节点无复选框
            pageNode.ToolTipText = $"页面ID: {page.Id}\r\n所属流程ID: {page.FlowId}{flowHint}\r\n备注: {page.Remark ?? "无"}";
            parentNode.Nodes.Add(pageNode);

            path.Add(pageId);

            int recursionCount = 0;
            var regions = _sourceDb.GetRegionsByPageId(pageId);
            foreach (var region in regions)
            {
                if (region.TargetPageId.HasValue)
                {
                    recursionCount += BuildNavigationSubtree(pageNode, region.TargetPageId.Value, rootFlowId, path, region);
                }
            }

            path.Remove(pageId);
            return recursionCount;
        }

        // 点击流程节点复选框区域时切换勾选状态
        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is NodeInfo info && info.Type == NodeType.Flow)
            {
                TreeViewHitTestInfo hitTest = treeView.HitTest(e.X, e.Y);
                if (hitTest.Location == TreeViewHitTestLocations.StateImage)
                {
                    if (e.Node.StateImageIndex == STATE_UNCHECKED)
                        e.Node.StateImageIndex = STATE_CHECKED;
                    else if (e.Node.StateImageIndex == STATE_CHECKED)
                        e.Node.StateImageIndex = STATE_UNCHECKED;
                }
            }
        }

        // 全选/全不选流程
        private void SelectAllFlows(bool check)
        {
            int state = check ? STATE_CHECKED : STATE_UNCHECKED;
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is NodeInfo info && info.Type == NodeType.Flow)
                {
                    node.StateImageIndex = state;
                }
            }
        }

        // 导入选中的流程
        private void ImportSelectedFlows()
        {
            // 收集勾选的流程ID
            List<int> selectedFlowIds = new List<int>();
            foreach (TreeNode flowNode in treeView.Nodes)
            {
                if (flowNode.Tag is NodeInfo info && info.Type == NodeType.Flow &&
                    flowNode.StateImageIndex == STATE_CHECKED)
                {
                    selectedFlowIds.Add(info.Id);
                }
            }

            if (selectedFlowIds.Count == 0)
            {
                MessageBox.Show("请先勾选要导入的流程", "提示");
                return;
            }

            if (_sourceDb == null)
            {
                MessageBox.Show("请先选择源数据文件", "提示");
                return;
            }

            if (MessageBox.Show($"确定要导入选中的 {selectedFlowIds.Count} 个流程吗？\n所有页面、区域、注释、附件将使用新的ID导入。", "确认导入",
                MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }

            try
            {
                // ID 映射表
                Dictionary<int, int> oldToNewFlowId = new Dictionary<int, int>();
                Dictionary<int, int> oldToNewPageId = new Dictionary<int, int>();

                // === 阶段1：导入流程和页面 ===
                foreach (int oldFlowId in selectedFlowIds)
                {
                    var oldFlow = _sourceDb.GetFlowById(oldFlowId);
                    if (oldFlow == null) continue;

                    // 插入新流程（StartPageId 暂为 0，后续更新）
                    int newFlowId = _targetDb.AddFlow(new ProcessFlow
                    {
                        Name = oldFlow.Name,
                        Description = oldFlow.Description,
                        StartPageId = 0,
                        CreateTime = oldFlow.CreateTime
                    });
                    oldToNewFlowId[oldFlowId] = newFlowId;

                    // 导入该流程的所有页面
                    var pages = _sourceDb.GetPagesByFlowId(oldFlowId);
                    foreach (var page in pages)
                    {
                        int newPageId = _targetDb.AddPage(new ProcessPage
                        {
                            FlowId = newFlowId,
                            Name = page.Name,
                            ImageData = page.ImageData,
                            ImageName = page.ImageName,
                            Remark = page.Remark
                        });
                        oldToNewPageId[page.Id] = newPageId;
                    }
                }

                // === 阶段2：更新各流程的 StartPageId ===
                foreach (int oldFlowId in selectedFlowIds)
                {
                    var oldFlow = _sourceDb.GetFlowById(oldFlowId);
                    if (oldFlow == null) continue;
                    int newFlowId = oldToNewFlowId[oldFlowId];

                    int newStartPageId = 0;
                    if (oldFlow.StartPageId > 0 && oldToNewPageId.ContainsKey(oldFlow.StartPageId))
                    {
                        newStartPageId = oldToNewPageId[oldFlow.StartPageId];
                    }
                    _targetDb.SetFlowStartPage(newFlowId, newStartPageId);
                }

                // === 阶段3：导入区域和注释 ===
                int importedRegions = 0;
                int importedAnnotations = 0;
                int importedAttachments = 0;
                int nullifiedTargets = 0;

                foreach (int oldFlowId in selectedFlowIds)
                {
                    var pages = _sourceDb.GetPagesByFlowId(oldFlowId);
                    foreach (var page in pages)
                    {
                        int newPageId = oldToNewPageId[page.Id];

                        // 导入区域
                        var regions = _sourceDb.GetRegionsByPageId(page.Id);
                        foreach (var region in regions)
                        {
                            int? newTargetPageId = null;
                            if (region.TargetPageId.HasValue)
                            {
                                int oldTargetPageId = region.TargetPageId.Value;
                                if (oldToNewPageId.ContainsKey(oldTargetPageId))
                                {
                                    // 目标页面所属流程也被导入 → 映射到新ID
                                    newTargetPageId = oldToNewPageId[oldTargetPageId];
                                }
                                else
                                {
                                    // 目标页面所属流程未导入 → 置空
                                    nullifiedTargets++;
                                }
                            }

                            _targetDb.AddRegion(new PageRegion
                            {
                                PageId = newPageId,
                                X = region.X,
                                Y = region.Y,
                                Width = region.Width,
                                Height = region.Height,
                                Remark = region.Remark,
                                TargetPageId = newTargetPageId
                            });
                            importedRegions++;
                        }

                        // 导入注释
                        var annotations = _sourceDb.GetAnnotationsByPageId(page.Id);
                        foreach (var ann in annotations)
                        {
                            _targetDb.AddAnnotation(new PageAnnotation
                            {
                                PageId = newPageId,
                                TextX = ann.TextX,
                                TextY = ann.TextY,
                                TextWidth = ann.TextWidth,
                                TextHeight = ann.TextHeight,
                                Text = ann.Text,
                                ArrowEndX = ann.ArrowEndX,
                                ArrowEndY = ann.ArrowEndY
                            });
                            importedAnnotations++;
                        }

                        // === 阶段4：导入附件 ===
                        var attachments = _sourceDb.GetAttachmentsByPageId(page.Id);
                        foreach (var att in attachments)
                        {
                            _targetDb.AddAttachment(new PageAttachment
                            {
                                PageId = newPageId,
                                FileName = att.FileName,
                                FileData = att.FileData,
                                FileSize = att.FileSize,
                                Remark = att.Remark,
                                CreateTime = att.CreateTime
                            });
                            importedAttachments++;
                        }
                    }
                }

                int importedPages = oldToNewPageId.Count;
                string nullifiedMsg = nullifiedTargets > 0
                    ? $"\n其中 {nullifiedTargets} 个区域的目标页面因未导入已置空"
                    : "";
                string msg = $"导入成功！\n流程: {selectedFlowIds.Count}\n页面: {importedPages}\n区域: {importedRegions}{nullifiedMsg}\n注释: {importedAnnotations}\n附件: {importedAttachments}";
                MessageBox.Show(msg, "导入完成");
                lblStatus.Text = $"上次导入: {selectedFlowIds.Count} 流程, {importedPages} 页面, {importedRegions} 区域, {importedAnnotations} 注释, {importedAttachments} 附件";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误");
            }
        }

        // 创建类型图标（纯色方块）
        private Bitmap CreateIcon(Color backColor)
        {
            return CreateIcon(backColor, Color.White);
        }

        private Bitmap CreateIcon(Color backColor, Color foreColor)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (SolidBrush brush = new SolidBrush(backColor))
                {
                    g.FillRectangle(brush, 1, 1, 14, 14);
                }
                using (Pen pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                {
                    g.DrawRectangle(pen, 1, 1, 14, 14);
                }
                using (Font font = new Font("微软雅黑", 8F, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(foreColor))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("·", font, brush, new RectangleF(0, 0, 16, 16), sf);
                }
            }
            return bmp;
        }

        // 创建复选框图标
        private Bitmap CreateCheckboxIcon(bool isChecked)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Rectangle rect = new Rectangle(2, 2, 12, 12);
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, rect);
                }
                using (Pen pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                {
                    g.DrawRectangle(pen, rect);
                }
                if (isChecked)
                {
                    using (Pen pen = new Pen(Color.FromArgb(0, 120, 215), 2))
                    {
                        g.DrawLine(pen, 4, 8, 6, 11);
                        g.DrawLine(pen, 6, 11, 11, 4);
                    }
                }
            }
            return bmp;
        }
    }
}
