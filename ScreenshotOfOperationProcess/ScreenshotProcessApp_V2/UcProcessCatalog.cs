using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcProcessCatalog : UserControl
    {
        private Database _db;
        private TreeView treeView;
        private Label lblPageTitle;
        private Button btnRefresh;
        private ImageList imageList;
        private Label lblRecursionWarn;

        // 节点类型
        private enum NodeType { Flow, Page, CycleWarning, Empty }

        // 节点附加信息
        private class NodeInfo
        {
            public NodeType Type;
            public int Id;
            public int FlowId;  // 所属流程ID（用于双击运行）
        }

        public UcProcessCatalog(Database db)
        {
            _db = db;
            InitializeComponent();
            LoadCatalog();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 标题
            lblPageTitle = new Label();
            lblPageTitle.Text = "流程目录";
            lblPageTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            lblPageTitle.ForeColor = Color.FromArgb(48, 53, 65);
            lblPageTitle.Location = new Point(10, 5);
            lblPageTitle.Size = new Size(300, 35);
            this.Controls.Add(lblPageTitle);

            // 刷新按钮
            btnRefresh = new Button();
            btnRefresh.Text = "刷新";
            btnRefresh.Location = new Point(320, 8);
            btnRefresh.Size = new Size(75, 30);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.BackColor = Color.FromArgb(0, 120, 215);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Font = new Font("微软雅黑", 9F);
            btnRefresh.Click += (s, e) => LoadCatalog();
            this.Controls.Add(btnRefresh);

            // 递归提示标签
            lblRecursionWarn = new Label();
            lblRecursionWarn.Text = "";
            lblRecursionWarn.Location = new Point(405, 12);
            lblRecursionWarn.Size = new Size(800, 22);
            lblRecursionWarn.Font = new Font("微软雅黑", 9F);
            lblRecursionWarn.ForeColor = Color.FromArgb(220, 53, 69);
            lblRecursionWarn.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblRecursionWarn);

            // 图标列表
            imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.Images.Add("Flow", CreateIcon(Color.FromArgb(0, 120, 215)));       // 流程 - 蓝色
            imageList.Images.Add("Page", CreateIcon(Color.FromArgb(40, 167, 69)));       // 页面 - 绿色
            imageList.Images.Add("Cycle", CreateIcon(Color.FromArgb(220, 53, 69)));      // 递归 - 红色

            // 树视图
            treeView = new TreeView();
            treeView.Location = new Point(10, 50);
            treeView.Size = new Size(1550, 1000);
            treeView.Font = new Font("微软雅黑", 10F);
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.BackColor = Color.White;
            treeView.ImageList = imageList;
            treeView.ShowNodeToolTips = true;
            treeView.HideSelection = false;
            treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;
            this.Controls.Add(treeView);

            this.ResumeLayout(false);
        }

        // 创建纯色图标
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

        // 加载目录树
        private void LoadCatalog()
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            lblRecursionWarn.Text = "";

            int totalRecursion = 0;
            var flows = _db.GetAllFlows();
            foreach (var flow in flows)
            {
                TreeNode flowNode = new TreeNode();
                flowNode.Text = flow.Name + (flow.StartPageId > 0 ? "" : " (未设置起始页)");
                flowNode.ImageKey = "Flow";
                flowNode.SelectedImageKey = "Flow";
                flowNode.Tag = new NodeInfo { Type = NodeType.Flow, Id = flow.Id };
                flowNode.ToolTipText = $"ID: {flow.Id}\r\n描述: {flow.Description ?? "无"}\r\n创建时间: {flow.CreateTime:yyyy-MM-dd HH:mm}\r\n双击运行此流程";

                if (flow.StartPageId > 0)
                {
                    // 从起始页开始构建导航目录，跟踪路径用于递归检测
                    var path = new HashSet<int>();
                    totalRecursion += BuildNavigationSubtree(flowNode, flow.StartPageId, flow.Id, path, null);
                }
                else
                {
                    TreeNode warn = new TreeNode("（未设置起始页，无法生成目录）");
                    warn.ForeColor = Color.Gray;
                    warn.Tag = new NodeInfo { Type = NodeType.Empty };
                    flowNode.Nodes.Add(warn);
                }

                treeView.Nodes.Add(flowNode);
            }

            if (treeView.Nodes.Count == 0)
            {
                TreeNode empty = new TreeNode("（暂无流程，请先在流程管理中创建）");
                empty.ForeColor = Color.Gray;
                treeView.Nodes.Add(empty);
            }

            if (totalRecursion > 0)
            {
                lblRecursionWarn.Text = $"检测到 {totalRecursion} 处递归引用，对应分支已停止展开（红色节点标识）";
            }

            treeView.ExpandAll();
            treeView.EndUpdate();
        }

        // 构建导航子树，返回检测到的递归数量
        // path: 当前导航路径中的页面ID集合（祖先链），用于递归检测
        // sourceRegion: 引导到本页面的区域（用于在节点标签中显示来源），起始页为 null
        private int BuildNavigationSubtree(TreeNode parentNode, int pageId, int rootFlowId, HashSet<int> path, PageRegion sourceRegion)
        {
            // 递归检测：目标页面已在当前导航路径中
            if (path.Contains(pageId))
            {
                var cyclePage = _db.GetPageById(pageId);
                string cycleName = cyclePage?.Name ?? $"页面#{pageId}";
                TreeNode cycleNode = new TreeNode();
                cycleNode.Text = $"[递归] → {cycleName} (已在路径中，停止展开)";
                cycleNode.ForeColor = Color.Red;
                cycleNode.ImageKey = "Cycle";
                cycleNode.SelectedImageKey = "Cycle";
                cycleNode.Tag = new NodeInfo { Type = NodeType.CycleWarning, Id = pageId, FlowId = rootFlowId };
                cycleNode.ToolTipText = $"检测到递归：目标页面「{cycleName}」(ID:{pageId}) 已在当前导航路径中。\r\n为避免无限循环，此分支已停止展开。";
                parentNode.Nodes.Add(cycleNode);
                return 1;
            }

            var page = _db.GetPageById(pageId);
            if (page == null)
            {
                TreeNode warn = new TreeNode($"[页面不存在] ID={pageId}");
                warn.ForeColor = Color.Gray;
                warn.Tag = new NodeInfo { Type = NodeType.Empty };
                parentNode.Nodes.Add(warn);
                return 0;
            }

            // 若目标页面属于其他流程，显示流程名提示
            string flowHint = "";
            if (page.FlowId != rootFlowId)
            {
                var targetFlow = _db.GetFlowById(page.FlowId);
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

            // 添加页面节点
            TreeNode pageNode = new TreeNode();
            pageNode.Text = page.Name + flowHint + regionHint;
            pageNode.ImageKey = "Page";
            pageNode.SelectedImageKey = "Page";
            pageNode.Tag = new NodeInfo { Type = NodeType.Page, Id = page.Id, FlowId = page.FlowId };
            pageNode.ToolTipText = $"页面ID: {page.Id}\r\n所属流程ID: {page.FlowId}{flowHint}\r\n备注: {page.Remark ?? "无"}\r\n双击运行所属流程并跳转到此页面";
            parentNode.Nodes.Add(pageNode);

            // 加入路径（用于递归检测）
            path.Add(pageId);

            int recursionCount = 0;
            var regions = _db.GetRegionsByPageId(pageId);
            foreach (var region in regions)
            {
                if (region.TargetPageId.HasValue)
                {
                    // 区域关联的目标页面作为当前页面的子节点
                    recursionCount += BuildNavigationSubtree(pageNode, region.TargetPageId.Value, rootFlowId, path, region);
                }
            }

            // 回溯：从路径中移除当前页面（允许兄弟分支访问同一页面，这不算递归）
            path.Remove(pageId);

            return recursionCount;
        }

        // 双击节点：流程→运行流程；页面→运行流程并跳转到此页面
        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode node = e.Node;
            if (!(node.Tag is NodeInfo info)) return;

            switch (info.Type)
            {
                case NodeType.Flow:
                    RunFlow(info.Id);
                    break;
                case NodeType.Page:
                    RunFlowAtPage(info.FlowId, info.Id);
                    break;
            }
        }

        private void RunFlow(int flowId)
        {
            var flow = _db.GetFlowById(flowId);
            if (flow == null)
            {
                MessageBox.Show("未找到流程", "提示");
                return;
            }
            if (flow.StartPageId <= 0)
            {
                MessageBox.Show("该流程尚未设置开始页面，请先在页面管理中设置", "提示");
                return;
            }
            FormRun formRun = new FormRun(_db);
            formRun.SelectFlowAndStart(flow.Id);
            formRun.Show();
        }

        private void RunFlowAtPage(int flowId, int pageId)
        {
            var flow = _db.GetFlowById(flowId);
            if (flow == null)
            {
                MessageBox.Show("未找到流程", "提示");
                return;
            }
            FormRun formRun = new FormRun(_db);
            formRun.SelectFlowAndStartAtPage(flow.Id, pageId);
            formRun.Show();
        }
    }
}
