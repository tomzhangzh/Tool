using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotProcessApp
{
    public class UcProcessStructure : UserControl
    {
        private Database _db;
        private TreeView treeView;
        private Label lblPageTitle;
        private Button btnRefresh;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnClear;
        private Label lblSearchHint;
        private ImageList imageList;
        private string _currentKeyword = "";

        // 节点类型
        private enum NodeType { Flow, Page, Region, Annotation, Dummy }

        // 节点附加信息
        private class NodeInfo
        {
            public NodeType Type;
            public int Id;
            public int FlowId;  // 用于Page节点，记录所属流程ID
        }

        public UcProcessStructure(Database db)
        {
            _db = db;
            InitializeComponent();
            LoadRootFlows();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 标题
            lblPageTitle = new Label();
            lblPageTitle.Text = "流程结构";
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
            btnRefresh.Click += (s, e) => { txtSearch.Text = ""; LoadRootFlows(); };
            this.Controls.Add(btnRefresh);

            // 搜索栏
            lblSearchHint = new Label();
            lblSearchHint.Text = "查询：";
            lblSearchHint.Location = new Point(405, 12);
            lblSearchHint.Size = new Size(55, 22);
            lblSearchHint.Font = new Font("微软雅黑", 10F);
            lblSearchHint.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblSearchHint);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(465, 10);
            txtSearch.Size = new Size(280, 28);
            txtSearch.Font = new Font("微软雅黑", 10F);
            txtSearch.PlaceholderText = "输入关键字（流程/页面/区域/注释）...";
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SearchTree(); } };
            this.Controls.Add(txtSearch);

            btnSearch = new Button();
            btnSearch.Text = "搜索";
            btnSearch.Location = new Point(750, 9);
            btnSearch.Size = new Size(75, 30);
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.BackColor = Color.FromArgb(40, 167, 69);
            btnSearch.ForeColor = Color.White;
            btnSearch.Cursor = Cursors.Hand;
            btnSearch.Font = new Font("微软雅黑", 9F);
            btnSearch.Click += (s, e) => SearchTree();
            this.Controls.Add(btnSearch);

            btnClear = new Button();
            btnClear.Text = "清除";
            btnClear.Location = new Point(830, 9);
            btnClear.Size = new Size(75, 30);
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.BackColor = Color.FromArgb(108, 117, 125);
            btnClear.ForeColor = Color.White;
            btnClear.Cursor = Cursors.Hand;
            btnClear.Font = new Font("微软雅黑", 9F);
            btnClear.Click += (s, e) => { txtSearch.Text = ""; SearchTree(); };
            this.Controls.Add(btnClear);

            // 图标列表
            imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.Images.Add("Flow", CreateIcon(Color.FromArgb(0, 120, 215)));          // 流程 - 蓝色
            imageList.Images.Add("Page", CreateIcon(Color.FromArgb(40, 167, 69)));          // 页面 - 绿色
            imageList.Images.Add("Region", CreateIcon(Color.FromArgb(255, 193, 7), Color.Black));   // 区域 - 黄色
            imageList.Images.Add("Annotation", CreateIcon(Color.FromArgb(220, 53, 69)));    // 注释 - 红色

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
            treeView.BeforeExpand += TreeView_BeforeExpand;
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
                // 绘制简单字符标记
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

        // 加载根节点（流程列表）
        private void LoadRootFlows()
        {
            _currentKeyword = "";
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            var flows = _db.GetAllFlows();
            foreach (var flow in flows)
            {
                TreeNode flowNode = new TreeNode();
                flowNode.Text = flow.Name + (flow.StartPageId > 0 ? "" : " (未设置起始页)");
                flowNode.ImageKey = "Flow";
                flowNode.SelectedImageKey = "Flow";
                flowNode.Tag = new NodeInfo { Type = NodeType.Flow, Id = flow.Id };
                flowNode.ToolTipText = $"ID: {flow.Id}\r\n描述: {flow.Description ?? "无"}\r\n创建时间: {flow.CreateTime:yyyy-MM-dd HH:mm}\r\n双击运行此流程";

                // 添加虚拟子节点以显示展开箭头（懒加载）
                AddDummyNode(flowNode);
                treeView.Nodes.Add(flowNode);
            }

            if (treeView.Nodes.Count == 0)
            {
                TreeNode empty = new TreeNode("（暂无流程，请先在流程管理中创建）");
                empty.ForeColor = Color.Gray;
                treeView.Nodes.Add(empty);
            }

            treeView.EndUpdate();
        }

        // 搜索树（深度搜索流程/页面/区域/注释的标签文本）
        private void SearchTree()
        {
            string keyword = txtSearch.Text.Trim();
            _currentKeyword = keyword;

            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            if (string.IsNullOrEmpty(keyword))
            {
                // 关键字为空，恢复懒加载模式
                var flows = _db.GetAllFlows();
                foreach (var flow in flows)
                {
                    TreeNode flowNode = CreateFlowNode(flow);
                    AddDummyNode(flowNode);
                    treeView.Nodes.Add(flowNode);
                }
                if (treeView.Nodes.Count == 0)
                {
                    TreeNode empty = new TreeNode("（暂无流程，请先在流程管理中创建）");
                    empty.ForeColor = Color.Gray;
                    treeView.Nodes.Add(empty);
                }
                treeView.EndUpdate();
                return;
            }

            // 深度搜索：遍历所有流程及其子节点，仅保留命中节点及其父链
            var allFlows = _db.GetAllFlows();
            foreach (var flow in allFlows)
            {
                TreeNode? filtered = BuildFilteredFlowNode(flow, keyword);
                if (filtered != null)
                {
                    treeView.Nodes.Add(filtered);
                }
            }

            if (treeView.Nodes.Count == 0)
            {
                TreeNode empty = new TreeNode($"未找到匹配 \"{keyword}\" 的项");
                empty.ForeColor = Color.Gray;
                treeView.Nodes.Add(empty);
            }
            else
            {
                treeView.ExpandAll();
            }

            treeView.EndUpdate();
        }

        // 关键字匹配（不区分大小写）
        private bool MatchKeyword(string? text, string keyword)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 创建流程节点（基础信息，不含子节点）
        private TreeNode CreateFlowNode(ProcessFlow flow)
        {
            TreeNode node = new TreeNode();
            node.Text = flow.Name + (flow.StartPageId > 0 ? "" : " (未设置起始页)");
            node.ImageKey = "Flow";
            node.SelectedImageKey = "Flow";
            node.Tag = new NodeInfo { Type = NodeType.Flow, Id = flow.Id };
            node.ToolTipText = $"ID: {flow.Id}\r\n描述: {flow.Description ?? "无"}\r\n创建时间: {flow.CreateTime:yyyy-MM-dd HH:mm}\r\n双击运行此流程";
            return node;
        }

        // 创建页面节点（基础信息，不含子节点）
        private TreeNode CreatePageNode(ProcessPage page, ProcessFlow flow)
        {
            TreeNode node = new TreeNode();
            node.Text = page.Name + (flow.StartPageId == page.Id ? " (起始页)" : "");
            node.ImageKey = "Page";
            node.SelectedImageKey = "Page";
            node.Tag = new NodeInfo { Type = NodeType.Page, Id = page.Id, FlowId = flow.Id };
            node.ToolTipText = $"页面ID: {page.Id}\r\n备注: {page.Remark ?? "无"}\r\n双击运行流程并跳转到此页面";
            return node;
        }

        // 格式化文件大小
        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes}B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1}KB";
            return $"{bytes / 1024.0 / 1024.0:F2}MB";
        }

        // 在页面节点下添加附件分组子节点
        private void AddAttachmentNodes(TreeNode pageNode, int pageId, string keyword)
        {
            var attachments = _db.GetAttachmentsByPageId(pageId);
            if (attachments == null || attachments.Count == 0) return;

            // 搜索模式：仅显示匹配项
            if (!string.IsNullOrEmpty(keyword))
            {
                var matched = attachments.FindAll(a => MatchKeyword(a.FileName, keyword) || MatchKeyword(a.Remark, keyword));
                if (matched.Count == 0) return;

                TreeNode attGroup = new TreeNode($"附件 ({matched.Count}/{attachments.Count})");
                attGroup.ImageKey = "Annotation";
                attGroup.SelectedImageKey = "Annotation";
                foreach (var att in matched)
                {
                    TreeNode aNode = new TreeNode($"📎 {att.FileName} ({FormatSize(att.FileSize)})");
                    aNode.ImageKey = "Annotation";
                    aNode.SelectedImageKey = "Annotation";
                    aNode.Tag = new NodeInfo { Type = NodeType.Annotation, Id = att.Id, FlowId = pageNode.Tag is NodeInfo ni ? ni.FlowId : 0 };
                    aNode.ToolTipText = $"附件ID: {att.Id}\r\n文件名: {att.FileName}\r\n大小: {FormatSize(att.FileSize)}\r\n备注: {att.Remark ?? "无"}\r\n上传时间: {att.CreateTime:yyyy-MM-dd HH:mm}";
                    HighlightNode(aNode);
                    attGroup.Nodes.Add(aNode);
                }
                pageNode.Nodes.Add(attGroup);
            }
            else
            {
                TreeNode attGroup = new TreeNode($"附件 ({attachments.Count})");
                attGroup.ImageKey = "Annotation";
                attGroup.SelectedImageKey = "Annotation";
                foreach (var att in attachments)
                {
                    TreeNode aNode = new TreeNode($"📎 {att.FileName} ({FormatSize(att.FileSize)})");
                    aNode.ImageKey = "Annotation";
                    aNode.SelectedImageKey = "Annotation";
                    aNode.Tag = new NodeInfo { Type = NodeType.Annotation, Id = att.Id, FlowId = pageNode.Tag is NodeInfo ni ? ni.FlowId : 0 };
                    aNode.ToolTipText = $"附件ID: {att.Id}\r\n文件名: {att.FileName}\r\n大小: {FormatSize(att.FileSize)}\r\n备注: {att.Remark ?? "无"}\r\n上传时间: {att.CreateTime:yyyy-MM-dd HH:mm}";
                    attGroup.Nodes.Add(aNode);
                }
                pageNode.Nodes.Add(attGroup);
            }
        }

        // 构建过滤后的流程节点（命中返回节点，未命中返回null）
        private TreeNode? BuildFilteredFlowNode(ProcessFlow flow, string keyword)
        {
            bool flowMatch = MatchKeyword(flow.Name, keyword) || MatchKeyword(flow.Description, keyword);
            var flowNode = CreateFlowNode(flow);
            if (flowMatch) HighlightNode(flowNode);

            bool hasMatchingChild = false;
            var pages = _db.GetPagesByFlowId(flow.Id);
            foreach (var page in pages)
            {
                TreeNode? pageNode = BuildFilteredPageNode(page, flow, keyword);
                if (pageNode != null)
                {
                    flowNode.Nodes.Add(pageNode);
                    hasMatchingChild = true;
                }
            }

            if (flowMatch || hasMatchingChild)
            {
                return flowNode;
            }
            return null;
        }

        // 构建过滤后的页面节点
        private TreeNode? BuildFilteredPageNode(ProcessPage page, ProcessFlow flow, string keyword)
        {
            bool pageMatch = MatchKeyword(page.Name, keyword) || MatchKeyword(page.Remark, keyword);
            var pageNode = CreatePageNode(page, flow);
            if (pageMatch) HighlightNode(pageNode);

            var regions = _db.GetRegionsByPageId(page.Id);
            var annotations = _db.GetAnnotationsByPageId(page.Id);

            bool hasMatchingRegion = false;
            bool hasMatchingAnnotation = false;

            // 区域分组
            var matchedRegions = regions.FindAll(r => MatchKeyword(r.Remark, keyword));
            if (matchedRegions.Count > 0)
            {
                TreeNode regionGroup = new TreeNode($"区域 ({matchedRegions.Count}/{regions.Count})");
                regionGroup.ImageKey = "Region";
                regionGroup.SelectedImageKey = "Region";
                regionGroup.Tag = new NodeInfo { Type = NodeType.Dummy };
                foreach (var region in matchedRegions)
                {
                    string target = region.TargetPageId.HasValue ? $" → 跳转页面#{region.TargetPageId}" : "";
                    TreeNode rNode = new TreeNode($"区域#{region.Id} ({region.X},{region.Y},{region.Width}x{region.Height}){target}" + (string.IsNullOrEmpty(region.Remark) ? "" : $" - {region.Remark}"));
                    rNode.ImageKey = "Region";
                    rNode.SelectedImageKey = "Region";
                    rNode.Tag = new NodeInfo { Type = NodeType.Region, Id = region.Id, FlowId = flow.Id };
                    rNode.ToolTipText = $"区域ID: {region.Id}\r\n位置: ({region.X}, {region.Y})\r\n尺寸: {region.Width} x {region.Height}\r\n备注: {region.Remark ?? "无"}\r\n目标页面: {(region.TargetPageId.HasValue ? region.TargetPageId.Value.ToString() : "无")}";
                    HighlightNode(rNode);
                    regionGroup.Nodes.Add(rNode);
                }
                pageNode.Nodes.Add(regionGroup);
                hasMatchingRegion = true;
            }

            // 注释分组
            var matchedAnns = annotations.FindAll(a => MatchKeyword(a.Text, keyword));
            if (matchedAnns.Count > 0)
            {
                TreeNode annGroup = new TreeNode($"注释 ({matchedAnns.Count}/{annotations.Count})");
                annGroup.ImageKey = "Annotation";
                annGroup.SelectedImageKey = "Annotation";
                annGroup.Tag = new NodeInfo { Type = NodeType.Dummy };
                foreach (var ann in matchedAnns)
                {
                    TreeNode aNode = new TreeNode($"注释#{ann.Id} - {(ann.Text ?? "")}");
                    aNode.ImageKey = "Annotation";
                    aNode.SelectedImageKey = "Annotation";
                    aNode.Tag = new NodeInfo { Type = NodeType.Annotation, Id = ann.Id, FlowId = flow.Id };
                    aNode.ToolTipText = $"注释ID: {ann.Id}\r\n文本: {ann.Text ?? "无"}\r\n位置: ({ann.TextX}, {ann.TextY})\r\n尺寸: {ann.TextWidth} x {ann.TextHeight}\r\n箭头终点: {(ann.ArrowEndX.HasValue ? $"({ann.ArrowEndX}, {ann.ArrowEndY})" : "无")}";
                    HighlightNode(aNode);
                    annGroup.Nodes.Add(aNode);
                }
                pageNode.Nodes.Add(annGroup);
                hasMatchingAnnotation = true;
            }

            if (pageMatch || hasMatchingRegion || hasMatchingAnnotation)
            {
                // 加载附件分组（搜索模式下仅显示匹配项）
                AddAttachmentNodes(pageNode, page.Id, keyword);
                return pageNode;
            }
            // 即使页面本身不匹配，也检查附件是否匹配
            var attachments = _db.GetAttachmentsByPageId(page.Id);
            bool hasMatchingAttachment = attachments.Exists(a => MatchKeyword(a.FileName, keyword) || MatchKeyword(a.Remark, keyword));
            if (hasMatchingAttachment)
            {
                AddAttachmentNodes(pageNode, page.Id, keyword);
                return pageNode;
            }
            return null;
        }

        // 高亮命中节点
        private void HighlightNode(TreeNode node)
        {
            node.BackColor = Color.FromArgb(255, 255, 200);
            node.ForeColor = Color.FromArgb(200, 50, 50);
            if (node.NodeFont == null)
            {
                node.NodeFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            }
            else
            {
                node.NodeFont = new Font(node.NodeFont, FontStyle.Bold);
            }
        }

        // 添加虚拟子节点（占位，触发懒加载）
        private void AddDummyNode(TreeNode parent)
        {
            TreeNode dummy = new TreeNode();
            dummy.Tag = new NodeInfo { Type = NodeType.Dummy };
            parent.Nodes.Add(dummy);
        }

        // 展开前懒加载子节点
        private void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Tag is NodeInfo info)
            {
                // 仅当包含虚拟节点时才需要加载
                if (HasDummyOnly(node))
                {
                    node.Nodes.Clear();
                    switch (info.Type)
                    {
                        case NodeType.Flow:
                            LoadPagesForFlow(node, info.Id);
                            break;
                        case NodeType.Page:
                            LoadRegionsAndAnnotationsForPage(node, info.Id);
                            break;
                    }
                }
            }
        }

        private bool HasDummyOnly(TreeNode node)
        {
            return node.Nodes.Count == 1 && node.Nodes[0].Tag is NodeInfo ni && ni.Type == NodeType.Dummy;
        }

        // 为流程节点加载页面子节点
        private void LoadPagesForFlow(TreeNode flowNode, int flowId)
        {
            var pages = _db.GetPagesByFlowId(flowId);
            var flow = _db.GetFlowById(flowId);
            foreach (var page in pages)
            {
                TreeNode pageNode = new TreeNode();
                pageNode.Text = page.Name + (flow != null && flow.StartPageId == page.Id ? " (起始页)" : "");
                pageNode.ImageKey = "Page";
                pageNode.SelectedImageKey = "Page";
                pageNode.Tag = new NodeInfo { Type = NodeType.Page, Id = page.Id, FlowId = flowId };
                pageNode.ToolTipText = $"页面ID: {page.Id}\r\n备注: {page.Remark ?? "无"}\r\n双击运行流程并跳转到此页面";
                AddDummyNode(pageNode);
                flowNode.Nodes.Add(pageNode);
            }

            if (pages.Count == 0)
            {
                TreeNode empty = new TreeNode("（无页面）");
                empty.ForeColor = Color.Gray;
                empty.Tag = new NodeInfo { Type = NodeType.Dummy };
                flowNode.Nodes.Add(empty);
            }
        }

        // 为页面节点加载区域和注释子节点
        private void LoadRegionsAndAnnotationsForPage(TreeNode pageNode, int pageId)
        {
            var regions = _db.GetRegionsByPageId(pageId);
            var annotations = _db.GetAnnotationsByPageId(pageId);

            if (regions.Count > 0)
            {
                TreeNode regionGroup = new TreeNode($"区域 ({regions.Count})");
                regionGroup.ImageKey = "Region";
                regionGroup.SelectedImageKey = "Region";
                regionGroup.Tag = new NodeInfo { Type = NodeType.Dummy };
                foreach (var region in regions)
                {
                    TreeNode rNode = new TreeNode();
                    string target = region.TargetPageId.HasValue ? $" → 跳转页面#{region.TargetPageId}" : "";
                    rNode.Text = $"区域#{region.Id} ({region.X},{region.Y},{region.Width}x{region.Height}){target}";
                    if (!string.IsNullOrEmpty(region.Remark))
                    {
                        rNode.Text += $" - {region.Remark}";
                    }
                    rNode.ImageKey = "Region";
                    rNode.SelectedImageKey = "Region";
                    rNode.Tag = new NodeInfo { Type = NodeType.Region, Id = region.Id, FlowId = pageId };
                    rNode.ToolTipText = $"区域ID: {region.Id}\r\n位置: ({region.X}, {region.Y})\r\n尺寸: {region.Width} x {region.Height}\r\n备注: {region.Remark ?? "无"}\r\n目标页面: {(region.TargetPageId.HasValue ? region.TargetPageId.Value.ToString() : "无")}";
                    regionGroup.Nodes.Add(rNode);
                }
                pageNode.Nodes.Add(regionGroup);
            }

            if (annotations.Count > 0)
            {
                TreeNode annGroup = new TreeNode($"注释 ({annotations.Count})");
                annGroup.ImageKey = "Annotation";
                annGroup.SelectedImageKey = "Annotation";
                annGroup.Tag = new NodeInfo { Type = NodeType.Dummy };
                foreach (var ann in annotations)
                {
                    TreeNode aNode = new TreeNode();
                    aNode.Text = $"注释#{ann.Id} - {(ann.Text ?? "")}";
                    aNode.ImageKey = "Annotation";
                    aNode.SelectedImageKey = "Annotation";
                    aNode.Tag = new NodeInfo { Type = NodeType.Annotation, Id = ann.Id, FlowId = pageId };
                    aNode.ToolTipText = $"注释ID: {ann.Id}\r\n文本: {ann.Text ?? "无"}\r\n位置: ({ann.TextX}, {ann.TextY})\r\n尺寸: {ann.TextWidth} x {ann.TextHeight}\r\n箭头终点: {(ann.ArrowEndX.HasValue ? $"({ann.ArrowEndX}, {ann.ArrowEndY})" : "无")}";
                    annGroup.Nodes.Add(aNode);
                }
                pageNode.Nodes.Add(annGroup);
            }

            if (regions.Count == 0 && annotations.Count == 0)
            {
                TreeNode empty = new TreeNode("（无区域和注释）");
                empty.ForeColor = Color.Gray;
                empty.Tag = new NodeInfo { Type = NodeType.Dummy };
                pageNode.Nodes.Add(empty);
            }

            // 加载附件分组
            AddAttachmentNodes(pageNode, pageId, "");
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
