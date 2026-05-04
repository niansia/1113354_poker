using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _1113354_陳冠瑋
{
    public partial class Form1 : Form
    {
        private int totalFunds = 1000000;
        private int currentBet = 0;

        private int[] allPoker = new int[52];
        private int[] playerPoker = new int[5];
        private int nextCardIndex = 0;

        private readonly Random rand = new Random();

        private PictureBox[] picCards = new PictureBox[5];
        private bool[] cardSelectedToChange = new bool[5];
        private bool canSelectCards = false;
        private bool hasDealtHand = false;

        private int recommendedHoldMask = -1;
        private StrategyResult lastBestStrategy = null;
        private List<StrategyResult> lastStrategyResults = new List<StrategyResult>();

        // 主版面
        private TableLayoutPanel mainLayout;
        private Panel pnlHeader;
        private Label lblHeaderTitle;
        private Label lblHeaderSubTitle;
        private Label lblHeaderStats;

        private GroupBox grpAI;
        private FlowLayoutPanel pnlAIButtons;

        // AI 分析區
        private PictureBox picAnalysisMain;
        private FlowLayoutPanel pnlAnalysisThumbs;
        private RichTextBox txtAILog;
        private ListView lvAnalysis;
        private Label lblWinRate;
        private Label lblAnalysisMode;
        private Label lblDetectedCount;
        private Label lblAvgConfidence;
        private Label lblAnalysisTitle;
        private ProgressBar prgAnalysis;

        // Dashboard
        private PictureBox picRankChart;
        private PictureBox picStrategyChart;
        private RichTextBox txtAdvice;
        private Label lblCurrentHand;
        private Label lblBestAction;
        private Label lblExpectedValue;

        private const float CardRatio = 96f / 71f;

        private readonly Color ColorBg = Color.FromArgb(9, 13, 20);
        private readonly Color ColorPanel = Color.FromArgb(16, 24, 36);
        private readonly Color ColorPanel2 = Color.FromArgb(20, 33, 48);
        private readonly Color ColorCasinoGreen = Color.FromArgb(7, 72, 45);
        private readonly Color ColorGold = Color.FromArgb(245, 190, 85);
        private readonly Color ColorNeon = Color.FromArgb(0, 220, 255);
        private readonly Color ColorText = Color.FromArgb(235, 242, 250);
        private readonly Color ColorMuted = Color.FromArgb(160, 175, 190);
        private readonly Color ColorDanger = Color.FromArgb(255, 80, 90);
        private readonly Color ColorSuccess = Color.FromArgb(70, 230, 150);

        private class AnalysisItem
        {
            public int CardIndex { get; set; }
            public string CardText { get; set; }
            public double Confidence { get; set; }
            public string Status { get; set; }
            public string Description { get; set; }
            public Image PreviewImage { get; set; }
        }

        private class HandEvaluation
        {
            public string RankName { get; set; }
            public string Category { get; set; }
            public int Multiplier { get; set; }
        }

        private class StrategyResult
        {
            public int HoldMask { get; set; }
            public double ExpectedMultiplier { get; set; }
            public double WinRate { get; set; }
            public int Samples { get; set; }
            public Dictionary<string, int> RankCounts { get; set; }
            public string HoldText { get; set; }
            public string ChangeText { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.SuspendLayout();

            this.Text = "AI Casino Poker Lab - 五張撲克牌";
            this.MinimumSize = new Size(1280, 860);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBg;
            this.KeyPreview = true;

            this.KeyPress -= Form1_KeyPress;
            this.KeyPress += Form1_KeyPress;

            btnBet.Click -= btnBet_Click;
            btnBet.Click += btnBet_Click;

            btnDeal.Click -= btnDeal_Click;
            btnDeal.Click += btnDeal_Click;

            btnChange.Click -= btnChange_Click;
            btnChange.Click += btnChange_Click;

            btnEvaluate.Click -= btnEvaluate_Click;
            btnEvaluate.Click += btnEvaluate_Click;

            BuildResponsiveLayout();
            BuildAIControls();
            InitializeCardPictureBoxes();

            grpTable.Resize += (s, ev) => LayoutCards();
            grpTable.Paint += GrpTable_Paint;

            this.Resize -= Form1_Resize;
            this.Resize += Form1_Resize;

            ResetGame();

            this.ResumeLayout(true);
            LayoutCards();
        }

        private void BuildResponsiveLayout()
        {
            this.AutoScroll = false;

            this.Controls.Remove(grpTable);
            this.Controls.Remove(grpBet);
            this.Controls.Remove(grpFunc);

            mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 5;
            mainLayout.Padding = new Padding(12);
            mainLayout.BackColor = ColorBg;

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 335F));

            pnlHeader = BuildHeaderPanel();

            grpTable.Text = " CASINO TABLE / 牌桌 ";
            grpTable.Dock = DockStyle.Fill;
            grpTable.Padding = new Padding(18, 28, 18, 18);
            grpTable.BackColor = ColorCasinoGreen;
            grpTable.ForeColor = ColorGold;

            grpBet.Text = " BETTING / 下注 ";
            grpBet.Dock = DockStyle.Fill;
            StyleGroupBox(grpBet);

            grpFunc.Text = " GAME CONTROL / 遊戲控制 ";
            grpFunc.Dock = DockStyle.Fill;
            StyleGroupBox(grpFunc);

            grpAI = new GroupBox();
            grpAI.Text = " AI PROJECT DASHBOARD / 影像辨識、統計分析與最佳換牌建議 ";
            grpAI.Dock = DockStyle.Fill;
            StyleGroupBox(grpAI);

            ConfigureBetGroup();
            ConfigureFuncGroup();

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(grpTable, 0, 1);
            mainLayout.Controls.Add(grpBet, 0, 2);
            mainLayout.Controls.Add(grpFunc, 0, 3);
            mainLayout.Controls.Add(grpAI, 0, 4);

            this.Controls.Add(mainLayout);
            this.Controls.SetChildIndex(mainLayout, 0);
        }

        private Panel BuildHeaderPanel()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = ColorPanel;
            header.Padding = new Padding(16, 8, 16, 8);

            header.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    header.ClientRectangle,
                    Color.FromArgb(14, 22, 34),
                    Color.FromArgb(35, 18, 8),
                    0F))
                {
                    e.Graphics.FillRectangle(brush, header.ClientRectangle);
                }

                using (Pen p = new Pen(Color.FromArgb(120, ColorGold), 2))
                {
                    e.Graphics.DrawRectangle(p, 1, 1, header.Width - 3, header.Height - 3);
                }
            };

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "AI CASINO POKER LAB";
            lblHeaderTitle.Font = new Font("Arial", 20F, FontStyle.Bold);
            lblHeaderTitle.ForeColor = ColorGold;
            lblHeaderTitle.AutoSize = false;
            lblHeaderTitle.Left = 18;
            lblHeaderTitle.Top = 8;
            lblHeaderTitle.Width = 360;
            lblHeaderTitle.Height = 28;

            lblHeaderSubTitle = new Label();
            lblHeaderSubTitle.Text = "五張撲克牌 × Computer Vision × Deep Learning × Dashboard";
            lblHeaderSubTitle.Font = new Font("微軟正黑體", 9.5F, FontStyle.Bold);
            lblHeaderSubTitle.ForeColor = ColorNeon;
            lblHeaderSubTitle.AutoSize = false;
            lblHeaderSubTitle.Left = 20;
            lblHeaderSubTitle.Top = 36;
            lblHeaderSubTitle.Width = 600;
            lblHeaderSubTitle.Height = 20;

            lblHeaderStats = new Label();
            lblHeaderStats.Font = new Font("Consolas", 11F, FontStyle.Bold);
            lblHeaderStats.ForeColor = ColorText;
            lblHeaderStats.TextAlign = ContentAlignment.MiddleRight;
            lblHeaderStats.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHeaderStats.Width = 520;
            lblHeaderStats.Height = 38;
            lblHeaderStats.Top = 12;
            lblHeaderStats.Left = header.Width - lblHeaderStats.Width - 20;

            header.Resize += (s, e) =>
            {
                lblHeaderStats.Left = header.Width - lblHeaderStats.Width - 20;
            };

            header.Controls.Add(lblHeaderTitle);
            header.Controls.Add(lblHeaderSubTitle);
            header.Controls.Add(lblHeaderStats);

            return header;
        }

        private void ConfigureBetGroup()
        {
            grpBet.Controls.Clear();

            TableLayoutPanel betLayout = new TableLayoutPanel();
            betLayout.Dock = DockStyle.Fill;
            betLayout.Padding = new Padding(18, 10, 18, 10);
            betLayout.ColumnCount = 7;
            betLayout.RowCount = 1;
            betLayout.BackColor = ColorPanel;

            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            betLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label lblTotal = CreateLabel("總資金", ColorGold, true);
            Label lblBet = CreateLabel("押注金額", ColorGold, true);

            StyleTextBox(txtTotalFunds);
            txtTotalFunds.Dock = DockStyle.Fill;
            txtTotalFunds.ReadOnly = true;
            txtTotalFunds.TextAlign = HorizontalAlignment.Right;
            txtTotalFunds.Margin = new Padding(3, 8, 18, 8);

            StyleTextBox(txtBetAmount);
            txtBetAmount.Dock = DockStyle.Fill;
            txtBetAmount.TextAlign = HorizontalAlignment.Right;
            txtBetAmount.Margin = new Padding(3, 8, 18, 8);

            if (string.IsNullOrWhiteSpace(txtBetAmount.Text))
            {
                txtBetAmount.Text = "500";
            }

            btnBet.Text = "押注";
            StyleButton(btnBet, ColorGold);
            btnBet.Dock = DockStyle.Fill;
            btnBet.Margin = new Padding(3, 8, 3, 8);

            Label lblTip = CreateLabel("提示：押注後發牌，AI 區可分析牌面與最佳換牌策略。", ColorMuted, false);
            lblTip.Dock = DockStyle.Fill;
            lblTip.TextAlign = ContentAlignment.MiddleLeft;

            betLayout.Controls.Add(lblTotal, 0, 0);
            betLayout.Controls.Add(txtTotalFunds, 1, 0);
            betLayout.Controls.Add(lblBet, 2, 0);
            betLayout.Controls.Add(txtBetAmount, 3, 0);
            betLayout.Controls.Add(btnBet, 4, 0);
            betLayout.Controls.Add(lblTip, 5, 0);
            betLayout.SetColumnSpan(lblTip, 2);

            grpBet.Controls.Add(betLayout);
        }

        private void ConfigureFuncGroup()
        {
            grpFunc.Controls.Clear();

            TableLayoutPanel funcLayout = new TableLayoutPanel();
            funcLayout.Dock = DockStyle.Fill;
            funcLayout.Padding = new Padding(18, 10, 18, 10);
            funcLayout.ColumnCount = 2;
            funcLayout.RowCount = 1;
            funcLayout.BackColor = ColorPanel;

            funcLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 335F));
            funcLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            FlowLayoutPanel pnlGameButtons = new FlowLayoutPanel();
            pnlGameButtons.Dock = DockStyle.Fill;
            pnlGameButtons.FlowDirection = FlowDirection.LeftToRight;
            pnlGameButtons.WrapContents = false;
            pnlGameButtons.AutoScroll = true;
            pnlGameButtons.BackColor = ColorPanel;

            btnDeal.Text = "發牌";
            btnChange.Text = "換牌";
            btnEvaluate.Text = "判斷牌型";

            StyleButton(btnDeal, ColorNeon);
            StyleButton(btnChange, ColorGold);
            StyleButton(btnEvaluate, ColorSuccess);

            SetButtonSize(btnDeal, 90, 36);
            SetButtonSize(btnChange, 90, 36);
            SetButtonSize(btnEvaluate, 115, 36);

            pnlGameButtons.Controls.Add(btnDeal);
            pnlGameButtons.Controls.Add(btnChange);
            pnlGameButtons.Controls.Add(btnEvaluate);

            StyleTextBox(txtResult);
            txtResult.Dock = DockStyle.Fill;
            txtResult.Multiline = true;
            txtResult.ReadOnly = true;
            txtResult.ScrollBars = ScrollBars.Vertical;
            txtResult.Margin = new Padding(8, 6, 0, 6);

            funcLayout.Controls.Add(pnlGameButtons, 0, 0);
            funcLayout.Controls.Add(txtResult, 1, 0);

            grpFunc.Controls.Add(funcLayout);
        }

        private void BuildAIControls()
        {
            grpAI.Controls.Clear();

            TableLayoutPanel aiLayout = new TableLayoutPanel();
            aiLayout.Dock = DockStyle.Fill;
            aiLayout.Padding = new Padding(12, 8, 12, 8);
            aiLayout.ColumnCount = 1;
            aiLayout.RowCount = 2;
            aiLayout.BackColor = ColorPanel;
            aiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            aiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlAIButtons = new FlowLayoutPanel();
            pnlAIButtons.Dock = DockStyle.Fill;
            pnlAIButtons.FlowDirection = FlowDirection.LeftToRight;
            pnlAIButtons.WrapContents = false;
            pnlAIButtons.AutoScroll = true;
            pnlAIButtons.BackColor = ColorPanel;

            Button btnRunCV = new Button() { Text = "CV 去霧" };
            Button btnRunRCNN = new Button() { Text = "R-CNN" };
            Button btnRunHOG = new Button() { Text = "HOG 梯度直方" };
            Button btnRunMask = new Button() { Text = "遮罩 Mask" };
            Button btnRunMOT = new Button() { Text = "多目標追蹤" };
            Button btnRunOCR = new Button() { Text = "OCR 光學辨識" };
            Button btnRunDashboard = new Button() { Text = "統計儀表板" };
            Button btnRunAdvice = new Button() { Text = "最佳換牌建議" };

            StyleButton(btnRunCV, ColorNeon);
            StyleButton(btnRunRCNN, ColorNeon);
            StyleButton(btnRunHOG, ColorNeon);
            StyleButton(btnRunMask, ColorNeon);
            StyleButton(btnRunMOT, ColorNeon);
            StyleButton(btnRunOCR, ColorNeon);
            StyleButton(btnRunDashboard, ColorGold);
            StyleButton(btnRunAdvice, ColorSuccess);

            SetButtonSize(btnRunCV, 95, 32);
            SetButtonSize(btnRunRCNN, 95, 32);
            SetButtonSize(btnRunHOG, 130, 32);
            SetButtonSize(btnRunMask, 110, 32);
            SetButtonSize(btnRunMOT, 120, 32);
            SetButtonSize(btnRunOCR, 125, 32);
            SetButtonSize(btnRunDashboard, 125, 32);
            SetButtonSize(btnRunAdvice, 135, 32);

            pnlAIButtons.Controls.Add(btnRunCV);
            pnlAIButtons.Controls.Add(btnRunRCNN);
            pnlAIButtons.Controls.Add(btnRunHOG);
            pnlAIButtons.Controls.Add(btnRunMask);
            pnlAIButtons.Controls.Add(btnRunMOT);
            pnlAIButtons.Controls.Add(btnRunOCR);
            pnlAIButtons.Controls.Add(btnRunDashboard);
            pnlAIButtons.Controls.Add(btnRunAdvice);

            SplitContainer splitAI = new SplitContainer();
            splitAI.Width = 1250;
            splitAI.Panel1MinSize = 390;
            splitAI.Panel2MinSize = 500;
            splitAI.SplitterDistance = 510;
            splitAI.Dock = DockStyle.Fill;
            splitAI.IsSplitterFixed = false;
            splitAI.BorderStyle = BorderStyle.FixedSingle;
            splitAI.BackColor = ColorPanel2;

            BuildAIPreviewPanel(splitAI.Panel1);
            BuildAIInfoPanel(splitAI.Panel2);

            aiLayout.Controls.Add(pnlAIButtons, 0, 0);
            aiLayout.Controls.Add(splitAI, 0, 1);

            grpAI.Controls.Add(aiLayout);

            btnRunCV.Click += (s, ev) => RunAIAnalysis("CV");
            btnRunRCNN.Click += (s, ev) => RunAIAnalysis("RCNN");
            btnRunHOG.Click += (s, ev) => RunAIAnalysis("HOG");
            btnRunMask.Click += (s, ev) => RunAIAnalysis("Mask");
            btnRunMOT.Click += (s, ev) => RunAIAnalysis("MOT");
            btnRunOCR.Click += (s, ev) => RunAIAnalysis("OCR");
            btnRunDashboard.Click += (s, ev) => RunDashboardAndAdvice(false);
            btnRunAdvice.Click += (s, ev) => RunDashboardAndAdvice(true);

            ResetAIPanel("請先押注並發牌。發牌後可執行 AI 分析、查看統計圖，或套用最佳換牌建議。");
        }

        private void BuildAIPreviewPanel(Control parent)
        {
            parent.BackColor = ColorPanel;

            TableLayoutPanel leftLayout = new TableLayoutPanel();
            leftLayout.Dock = DockStyle.Fill;
            leftLayout.RowCount = 3;
            leftLayout.ColumnCount = 1;
            leftLayout.Padding = new Padding(8);
            leftLayout.BackColor = ColorPanel;
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));

            lblAnalysisTitle = new Label();
            lblAnalysisTitle.Dock = DockStyle.Fill;
            lblAnalysisTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblAnalysisTitle.Font = new Font("微軟正黑體", 10F, FontStyle.Bold);
            lblAnalysisTitle.ForeColor = ColorGold;
            lblAnalysisTitle.Text = "AI 視覺化預覽";

            picAnalysisMain = new PictureBox();
            picAnalysisMain.Dock = DockStyle.Fill;
            picAnalysisMain.BackColor = Color.FromArgb(18, 18, 24);
            picAnalysisMain.BorderStyle = BorderStyle.FixedSingle;
            picAnalysisMain.SizeMode = PictureBoxSizeMode.Zoom;

            pnlAnalysisThumbs = new FlowLayoutPanel();
            pnlAnalysisThumbs.Dock = DockStyle.Fill;
            pnlAnalysisThumbs.AutoScroll = true;
            pnlAnalysisThumbs.WrapContents = false;
            pnlAnalysisThumbs.FlowDirection = FlowDirection.LeftToRight;
            pnlAnalysisThumbs.Padding = new Padding(4);
            pnlAnalysisThumbs.BackColor = Color.FromArgb(12, 18, 28);
            pnlAnalysisThumbs.BorderStyle = BorderStyle.FixedSingle;

            leftLayout.Controls.Add(lblAnalysisTitle, 0, 0);
            leftLayout.Controls.Add(picAnalysisMain, 0, 1);
            leftLayout.Controls.Add(pnlAnalysisThumbs, 0, 2);

            parent.Controls.Add(leftLayout);
        }

        private void BuildAIInfoPanel(Control parent)
        {
            parent.BackColor = ColorPanel;

            TabControl tab = new TabControl();
            tab.Dock = DockStyle.Fill;
            tab.Font = new Font("微軟正黑體", 9F, FontStyle.Bold);

            TabPage tabReport = new TabPage("AI 分析報告");
            TabPage tabDashboard = new TabPage("統計儀表板 / 換牌建議");

            tabReport.BackColor = ColorPanel;
            tabDashboard.BackColor = ColorPanel;

            BuildReportTab(tabReport);
            BuildDashboardTab(tabDashboard);

            tab.TabPages.Add(tabReport);
            tab.TabPages.Add(tabDashboard);

            parent.Controls.Add(tab);
        }

        private void BuildReportTab(Control parent)
        {
            TableLayoutPanel rightLayout = new TableLayoutPanel();
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.RowCount = 3;
            rightLayout.ColumnCount = 1;
            rightLayout.Padding = new Padding(8);
            rightLayout.BackColor = ColorPanel;
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel statusLayout = new TableLayoutPanel();
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.ColumnCount = 2;
            statusLayout.RowCount = 3;
            statusLayout.BackColor = ColorPanel;
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

            lblAnalysisMode = CreateLabel("目前模式：尚未執行", ColorGold, true);
            lblDetectedCount = CreateLabel("偵測張數：0", ColorText, false);
            lblAvgConfidence = CreateLabel("平均信心度：0.00", ColorText, false);

            lblWinRate = CreateLabel("DL 勝率：等待計算...", ColorNeon, true);
            lblWinRate.Font = new Font("微軟正黑體", 10, FontStyle.Bold);

            Label lblProgress = CreateLabel("分析完成度：", ColorMuted, false);

            prgAnalysis = new ProgressBar();
            prgAnalysis.Dock = DockStyle.Fill;
            prgAnalysis.Minimum = 0;
            prgAnalysis.Maximum = 100;
            prgAnalysis.Value = 0;
            prgAnalysis.Margin = new Padding(3, 4, 3, 4);

            statusLayout.Controls.Add(lblAnalysisMode, 0, 0);
            statusLayout.Controls.Add(lblDetectedCount, 1, 0);
            statusLayout.Controls.Add(lblAvgConfidence, 0, 1);
            statusLayout.Controls.Add(lblWinRate, 1, 1);
            statusLayout.Controls.Add(lblProgress, 0, 2);
            statusLayout.Controls.Add(prgAnalysis, 1, 2);

            txtAILog = new RichTextBox();
            txtAILog.Dock = DockStyle.Fill;
            txtAILog.ReadOnly = true;
            txtAILog.BackColor = Color.FromArgb(8, 12, 18);
            txtAILog.ForeColor = ColorText;
            txtAILog.BorderStyle = BorderStyle.FixedSingle;
            txtAILog.Font = new Font("微軟正黑體", 9F);
            txtAILog.ScrollBars = RichTextBoxScrollBars.Vertical;

            lvAnalysis = new ListView();
            lvAnalysis.Dock = DockStyle.Fill;
            lvAnalysis.View = View.Details;
            lvAnalysis.FullRowSelect = true;
            lvAnalysis.GridLines = true;
            lvAnalysis.HideSelection = false;
            lvAnalysis.BackColor = Color.FromArgb(10, 16, 24);
            lvAnalysis.ForeColor = ColorText;
            lvAnalysis.Font = new Font("微軟正黑體", 8.5F);
            lvAnalysis.Columns.Add("牌位", 55);
            lvAnalysis.Columns.Add("辨識內容", 90);
            lvAnalysis.Columns.Add("信心度", 75);
            lvAnalysis.Columns.Add("狀態", 95);
            lvAnalysis.Columns.Add("說明", 390);

            rightLayout.Controls.Add(statusLayout, 0, 0);
            rightLayout.Controls.Add(txtAILog, 0, 1);
            rightLayout.Controls.Add(lvAnalysis, 0, 2);

            parent.Controls.Add(rightLayout);
        }

        private void BuildDashboardTab(Control parent)
        {
            TableLayoutPanel dash = new TableLayoutPanel();
            dash.Dock = DockStyle.Fill;
            dash.RowCount = 3;
            dash.ColumnCount = 1;
            dash.Padding = new Padding(8);
            dash.BackColor = ColorPanel;
            dash.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            dash.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            dash.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

            TableLayoutPanel kpi = new TableLayoutPanel();
            kpi.Dock = DockStyle.Fill;
            kpi.ColumnCount = 3;
            kpi.RowCount = 1;
            kpi.BackColor = ColorPanel;
            kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            lblCurrentHand = CreateKpiBox("目前牌型", "等待發牌");
            lblBestAction = CreateKpiBox("最佳建議", "尚未分析");
            lblExpectedValue = CreateKpiBox("期望倍率", "0.00x");

            kpi.Controls.Add(lblCurrentHand, 0, 0);
            kpi.Controls.Add(lblBestAction, 1, 0);
            kpi.Controls.Add(lblExpectedValue, 2, 0);

            TableLayoutPanel charts = new TableLayoutPanel();
            charts.Dock = DockStyle.Fill;
            charts.ColumnCount = 2;
            charts.RowCount = 1;
            charts.BackColor = ColorPanel;
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            picRankChart = new PictureBox();
            picRankChart.Dock = DockStyle.Fill;
            picRankChart.SizeMode = PictureBoxSizeMode.Zoom;
            picRankChart.BackColor = Color.FromArgb(8, 12, 18);
            picRankChart.BorderStyle = BorderStyle.FixedSingle;
            picRankChart.Margin = new Padding(0, 0, 6, 6);

            picStrategyChart = new PictureBox();
            picStrategyChart.Dock = DockStyle.Fill;
            picStrategyChart.SizeMode = PictureBoxSizeMode.Zoom;
            picStrategyChart.BackColor = Color.FromArgb(8, 12, 18);
            picStrategyChart.BorderStyle = BorderStyle.FixedSingle;
            picStrategyChart.Margin = new Padding(6, 0, 0, 6);

            charts.Controls.Add(picRankChart, 0, 0);
            charts.Controls.Add(picStrategyChart, 1, 0);

            txtAdvice = new RichTextBox();
            txtAdvice.Dock = DockStyle.Fill;
            txtAdvice.ReadOnly = true;
            txtAdvice.BackColor = Color.FromArgb(8, 12, 18);
            txtAdvice.ForeColor = ColorText;
            txtAdvice.BorderStyle = BorderStyle.FixedSingle;
            txtAdvice.Font = new Font("微軟正黑體", 9.5F);
            txtAdvice.ScrollBars = RichTextBoxScrollBars.Vertical;

            dash.Controls.Add(kpi, 0, 0);
            dash.Controls.Add(charts, 0, 1);
            dash.Controls.Add(txtAdvice, 0, 2);

            parent.Controls.Add(dash);
        }

        private Label CreateKpiBox(string title, string value)
        {
            Label lbl = new Label();
            lbl.Dock = DockStyle.Fill;
            lbl.Margin = new Padding(4);
            lbl.Padding = new Padding(10, 6, 10, 6);
            lbl.BackColor = Color.FromArgb(10, 18, 28);
            lbl.ForeColor = ColorGold;
            lbl.Font = new Font("微軟正黑體", 10F, FontStyle.Bold);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Text = title + "\n" + value;
            return lbl;
        }

        private void SetButtonSize(Button button, int width, int height)
        {
            button.Width = width;
            button.Height = height;
            button.Margin = new Padding(4, 5, 4, 5);
        }

        private Label CreateLabel(string text, Color color, bool bold)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.ForeColor = color;
            lbl.BackColor = Color.Transparent;
            lbl.Font = new Font("微軟正黑體", 9F, bold ? FontStyle.Bold : FontStyle.Regular);
            return lbl;
        }

        private void StyleGroupBox(GroupBox gb)
        {
            gb.BackColor = ColorPanel;
            gb.ForeColor = ColorGold;
            gb.Font = new Font("微軟正黑體", 9F, FontStyle.Bold);
        }

        private void StyleButton(Button btn, Color accent)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = accent;
            btn.BackColor = Color.FromArgb(18, 27, 40);
            btn.ForeColor = ColorText;
            btn.Font = new Font("微軟正黑體", 9F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
        }

        private void StyleTextBox(TextBox tb)
        {
            tb.BackColor = Color.FromArgb(8, 12, 18);
            tb.ForeColor = ColorText;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("微軟正黑體", 9F, FontStyle.Bold);
        }

        private void InitializeCardPictureBoxes()
        {
            grpTable.Controls.Clear();

            for (int i = 0; i < 5; i++)
            {
                picCards[i] = new PictureBox();
                picCards[i].SizeMode = PictureBoxSizeMode.StretchImage;
                picCards[i].BorderStyle = BorderStyle.FixedSingle;
                picCards[i].BackColor = Color.White;
                picCards[i].Name = "pic" + i;
                picCards[i].Tag = "back";
                picCards[i].Enabled = false;
                picCards[i].Cursor = Cursors.Hand;
                picCards[i].MouseClick += PicCard_Click;

                grpTable.Controls.Add(picCards[i]);
            }

            LayoutCards();
        }

        private void GrpTable_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rect = grpTable.ClientRectangle;

            using (LinearGradientBrush bg = new LinearGradientBrush(
                rect,
                Color.FromArgb(5, 55, 35),
                Color.FromArgb(4, 25, 18),
                90F))
            {
                e.Graphics.FillRectangle(bg, rect);
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle ellipse = new Rectangle(80, 45, Math.Max(200, grpTable.Width - 160), Math.Max(100, grpTable.Height - 95));

            using (Pen p = new Pen(Color.FromArgb(95, ColorGold), 3))
            {
                e.Graphics.DrawEllipse(p, ellipse);
            }

            using (Pen p = new Pen(Color.FromArgb(55, ColorNeon), 1))
            {
                p.DashStyle = DashStyle.Dash;
                e.Graphics.DrawEllipse(p, ellipse.X + 18, ellipse.Y + 18, ellipse.Width - 36, ellipse.Height - 36);
            }

            using (Font f = new Font("Arial", 18F, FontStyle.Bold))
            using (Brush b = new SolidBrush(Color.FromArgb(35, ColorGold)))
            {
                string text = "AI CASINO POKER TABLE";
                SizeF sz = e.Graphics.MeasureString(text, f);
                e.Graphics.DrawString(text, f, b, (grpTable.Width - sz.Width) / 2, grpTable.Height - 60);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            LayoutCards();
        }

        private void LayoutCards()
        {
            if (picCards == null || picCards[0] == null || grpTable.ClientSize.Width <= 0 || grpTable.ClientSize.Height <= 0)
            {
                return;
            }

            int innerWidth = Math.Max(100, grpTable.ClientSize.Width - grpTable.Padding.Left - grpTable.Padding.Right - 30);
            int innerHeight = Math.Max(100, grpTable.ClientSize.Height - grpTable.Padding.Top - grpTable.Padding.Bottom - 20);

            int gap = Math.Max(16, Math.Min(34, innerWidth / 55));

            int cardWidthByWidth = (innerWidth - gap * 4) / 5;
            int cardWidthByHeight = (int)(innerHeight / CardRatio);

            int cardWidth = Math.Min(cardWidthByWidth, cardWidthByHeight);
            cardWidth = Math.Max(90, Math.Min(cardWidth, 230));

            int cardHeight = (int)(cardWidth * CardRatio);
            int totalCardsWidth = cardWidth * 5 + gap * 4;

            int startX = Math.Max(
                grpTable.Padding.Left,
                (grpTable.ClientSize.Width - totalCardsWidth) / 2
            );

            int startY = Math.Max(
                grpTable.Padding.Top,
                (grpTable.ClientSize.Height - cardHeight) / 2 + 8
            );

            for (int i = 0; i < 5; i++)
            {
                picCards[i].SetBounds(
                    startX + i * (cardWidth + gap),
                    startY,
                    cardWidth,
                    cardHeight
                );
            }

            grpTable.Invalidate();
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!btnEvaluate.Enabled)
            {
                return;
            }

            bool changed = true;

            switch (char.ToLowerInvariant(e.KeyChar))
            {
                case 'q':
                    playerPoker[0] = 51;
                    playerPoker[1] = 47;
                    playerPoker[2] = 43;
                    playerPoker[3] = 39;
                    playerPoker[4] = 3;
                    break;

                case 'w':
                    playerPoker[0] = 37;
                    playerPoker[1] = 33;
                    playerPoker[2] = 29;
                    playerPoker[3] = 25;
                    playerPoker[4] = 21;
                    break;

                case 'e':
                    playerPoker[0] = 50;
                    playerPoker[1] = 38;
                    playerPoker[2] = 34;
                    playerPoker[3] = 22;
                    playerPoker[4] = 18;
                    break;

                case 'r':
                    playerPoker[0] = 48;
                    playerPoker[1] = 39;
                    playerPoker[2] = 38;
                    playerPoker[3] = 37;
                    playerPoker[4] = 36;
                    break;

                case 't':
                    playerPoker[0] = 30;
                    playerPoker[1] = 29;
                    playerPoker[2] = 6;
                    playerPoker[3] = 5;
                    playerPoker[4] = 4;
                    break;

                case 'y':
                    playerPoker[0] = 48;
                    playerPoker[1] = 39;
                    playerPoker[2] = 15;
                    playerPoker[3] = 14;
                    playerPoker[4] = 13;
                    break;

                default:
                    changed = false;
                    break;
            }

            if (!changed)
            {
                return;
            }

            hasDealtHand = true;
            recommendedHoldMask = -1;

            for (int i = 0; i < 5; i++)
            {
                cardSelectedToChange[i] = false;
                picCards[i].Tag = "front";
                UpdateCardImage(i, playerPoker[i]);
            }

            txtResult.Text = "已套用測試牌型，可直接按「判斷牌型」，也可以使用下方 AI 儀表板分析。";
            ResetAIPanel("已套用測試牌型。可執行 AI 分析、統計儀表板，或最佳換牌建議。");
            RunDeepLearningModel();
        }

        private void PicCard_Click(object sender, MouseEventArgs e)
        {
            if (!canSelectCards)
            {
                return;
            }

            PictureBox pic = (PictureBox)sender;
            int idx = int.Parse(pic.Name.Replace("pic", ""));

            recommendedHoldMask = -1;
            cardSelectedToChange[idx] = !cardSelectedToChange[idx];

            UpdateAllCardVisuals();

            txtResult.Text = "你已手動選擇要換掉的牌。紅色標記代表按「換牌」時會被換掉。";
        }

        private void ResetGame()
        {
            currentBet = 0;
            hasDealtHand = false;
            recommendedHoldMask = -1;
            lastBestStrategy = null;
            lastStrategyResults.Clear();

            txtTotalFunds.Text = totalFunds.ToString();
            txtResult.Text = "請先輸入押注金額，然後按「押注」。";

            btnDeal.Enabled = false;
            btnChange.Enabled = false;
            btnEvaluate.Enabled = false;
            btnBet.Enabled = true;

            canSelectCards = false;

            Image backImage = Properties.Resources.back;

            for (int i = 0; i < 5; i++)
            {
                cardSelectedToChange[i] = false;

                if (picCards[i] != null)
                {
                    picCards[i].Image = backImage;
                    picCards[i].Tag = "back";
                    picCards[i].Enabled = false;
                }
            }

            ResetAIPanel("請先押注並發牌。發牌後可執行 AI 分析、查看統計圖，或套用最佳換牌建議。");
            UpdateHeaderStats();
            LayoutCards();
        }

        private void InitializeDeck()
        {
            for (int i = 0; i < allPoker.Length; i++)
            {
                allPoker[i] = i;
            }

            Shuffle();
        }

        private void Shuffle()
        {
            for (int i = allPoker.Length - 1; i > 0; i--)
            {
                int r = rand.Next(i + 1);

                int temp = allPoker[i];
                allPoker[i] = allPoker[r];
                allPoker[r] = temp;
            }
        }

        private void btnBet_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtBetAmount.Text.Trim(), out int betAmount))
            {
                MessageBox.Show("請輸入正確的數字！");
                return;
            }

            if (betAmount > totalFunds || betAmount <= 0)
            {
                MessageBox.Show("押注金額錯誤或資金不足！");
                return;
            }

            currentBet = betAmount;
            totalFunds -= currentBet;
            txtTotalFunds.Text = totalFunds.ToString();

            ApplyCIASecurity();

            btnDeal.Enabled = true;
            btnBet.Enabled = false;
            btnChange.Enabled = false;
            btnEvaluate.Enabled = false;

            txtResult.Text = "押注完成，請按「發牌」。";
            UpdateHeaderStats();
        }

        private void btnDeal_Click(object sender, EventArgs e)
        {
            InitializeDeck();
            nextCardIndex = 0;
            recommendedHoldMask = -1;

            for (int i = 0; i < 5; i++)
            {
                playerPoker[i] = allPoker[nextCardIndex++];

                cardSelectedToChange[i] = false;
                picCards[i].Tag = "front";
                picCards[i].Enabled = true;

                UpdateCardImage(i, playerPoker[i]);
            }

            hasDealtHand = true;

            btnDeal.Enabled = false;
            btnChange.Enabled = true;
            btnEvaluate.Enabled = true;

            canSelectCards = true;

            txtResult.Text = "請點擊要換掉的牌，紅色標記代表會被換掉。也可以按「最佳換牌建議」讓 AI 幫你選。";

            ResetAIPanel("已發牌完成。建議先按「統計儀表板」或「最佳換牌建議」查看分析。");
            RunDeepLearningModel();
            UpdateHeaderStats();
        }

        private void btnChange_Click(object sender, EventArgs e)
        {
            recommendedHoldMask = -1;

            for (int i = 0; i < 5; i++)
            {
                if (cardSelectedToChange[i])
                {
                    playerPoker[i] = allPoker[nextCardIndex++];
                }

                cardSelectedToChange[i] = false;
                picCards[i].Tag = "front";
                picCards[i].Enabled = false;

                UpdateCardImage(i, playerPoker[i]);
            }

            btnChange.Enabled = false;
            canSelectCards = false;

            txtResult.Text = "換牌完成，請按「判斷牌型」。你仍可使用 AI 分析目前牌面。";

            ResetAIPanel("換牌完成，可重新執行 AI 視覺分析與統計儀表板。");
            RunDeepLearningModel();
            UpdateHeaderStats();
        }

        private void UpdateCardImage(int index, int cardId)
        {
            if (picCards[index] == null)
            {
                return;
            }

            picCards[index].Image = CreateCardVisual(cardId, index);
        }

        private void UpdateAllCardVisuals()
        {
            if (!hasDealtHand)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                UpdateCardImage(i, playerPoker[i]);
            }
        }

        private Image CreateCardVisual(int cardId, int index)
        {
            object rm = Properties.Resources.ResourceManager.GetObject("pic" + (cardId + 1));

            Image baseImage = rm != null ? (Image)rm : Properties.Resources.back;
            Bitmap bmp = new Bitmap(baseImage);

            if (cardSelectedToChange[index])
            {
                DrawCardBadge(bmp, "換掉", ColorDanger);
            }
            else if (recommendedHoldMask >= 0)
            {
                if (IsCardHeld(recommendedHoldMask, index))
                {
                    DrawCardBadge(bmp, "保留", ColorSuccess);
                }
                else
                {
                    DrawCardBadge(bmp, "建議換", ColorGold);
                }
            }

            return bmp;
        }

        private void DrawCardBadge(Bitmap bmp, string text, Color color)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (Pen p = new Pen(color, 6))
                {
                    g.DrawRectangle(p, 4, 4, bmp.Width - 8, bmp.Height - 8);
                }

                Rectangle badge = new Rectangle(0, bmp.Height - 38, bmp.Width, 38);

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(205, color)))
                {
                    g.FillRectangle(bg, badge);
                }

                using (Font f = new Font("微軟正黑體", 16F, FontStyle.Bold))
                using (Brush b = new SolidBrush(Color.White))
                {
                    SizeF sz = g.MeasureString(text, f);
                    g.DrawString(text, f, b, (bmp.Width - sz.Width) / 2, bmp.Height - 34);
                }
            }
        }

        private void btnEvaluate_Click(object sender, EventArgs e)
        {
            HandEvaluation eval = EvaluateHandDetailed(playerPoker);

            int winAmount = currentBet * eval.Multiplier;
            totalFunds += winAmount;

            txtTotalFunds.Text = totalFunds.ToString();

            ApplyCIASecurity();
            RunDeepLearningModel();

            if (eval.Multiplier > 0)
            {
                txtResult.Text = $"{eval.RankName}！倍率：{eval.Multiplier} 倍，贏得：{winAmount}";
            }
            else
            {
                txtResult.Text = $"{eval.RankName}，沒中獎，再接再厲！";
            }

            btnEvaluate.Enabled = false;
            btnChange.Enabled = false;
            btnBet.Enabled = true;

            canSelectCards = false;
            recommendedHoldMask = -1;

            for (int i = 0; i < 5; i++)
            {
                picCards[i].Enabled = false;
                picCards[i].Tag = "front";
                cardSelectedToChange[i] = false;
                UpdateCardImage(i, playerPoker[i]);
            }

            UpdateHeaderStats();
        }

        private void RunAIAnalysis(string mode)
        {
            if (!hasDealtHand)
            {
                MessageBox.Show("請先發牌後再執行 AI 分析！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetAIPanel("目前尚未發牌，因此無法進行 AI 分析。");
                return;
            }

            prgAnalysis.Value = 12;
            lvAnalysis.Items.Clear();
            pnlAnalysisThumbs.Controls.Clear();

            List<AnalysisItem> results = new List<AnalysisItem>();

            for (int i = 0; i < 5; i++)
            {
                AnalysisItem item = BuildAnalysisItem(mode, i, playerPoker[i]);
                results.Add(item);
                prgAnalysis.Value = Math.Min(95, 20 + (i + 1) * 15);
            }

            FillAnalysisList(results);
            BuildAnalysisThumbs(results);

            Image boardImage = BuildBoardMontage(results);
            picAnalysisMain.Image = boardImage;
            lblAnalysisTitle.Text = $"AI 視覺化預覽 - {GetModeDisplayName(mode)}";

            double avgConf = results.Average(x => x.Confidence);
            lblAnalysisMode.Text = $"目前模式：{GetModeDisplayName(mode)}";
            lblDetectedCount.Text = $"偵測張數：{results.Count}";
            lblAvgConfidence.Text = $"平均信心度：{avgConf:F2}";
            prgAnalysis.Value = 100;

            txtAILog.Text = BuildAnalysisSummary(mode, results);
        }

        private AnalysisItem BuildAnalysisItem(string mode, int index, int cardId)
        {
            object rm = Properties.Resources.ResourceManager.GetObject("pic" + (cardId + 1));
            Image baseImage = rm != null ? (Image)rm : Properties.Resources.back;
            Bitmap bmp = new Bitmap(baseImage);

            string[] suitNames = { "梅花", "方塊", "紅心", "黑桃" };
            string[] suitSymbols = { "♣", "♦", "♥", "♠" };
            string[] pointNames = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

            int point = cardId / 4;
            int suit = cardId % 4;

            string cardText = $"{suitSymbols[suit]} {pointNames[point]}";
            double confidence = GenerateConfidenceByMode(mode);
            string status = "分析完成";
            string description = "";

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle fullRect = new Rectangle(0, 0, bmp.Width - 1, bmp.Height - 1);

                using (Font titleFont = new Font("Arial", 11, FontStyle.Bold))
                using (Font smallFont = new Font("Arial", 9, FontStyle.Bold))
                {
                    switch (mode)
                    {
                        case "CV":
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(55, 255, 244, 170)))
                            {
                                g.FillRectangle(b, fullRect);
                            }

                            using (Pen p = new Pen(Color.Goldenrod, 4))
                            {
                                g.DrawRectangle(p, 4, 4, bmp.Width - 8, bmp.Height - 8);
                            }

                            using (SolidBrush bg = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                            {
                                g.FillRectangle(bg, 0, 0, bmp.Width, 25);
                            }

                            g.DrawString("DEHAZE+", titleFont, Brushes.White, 6, 3);
                            description = $"已提升第 {index + 1} 張牌的亮度與清晰度，邊緣輪廓更明顯。";
                            break;

                        case "RCNN":
                            using (Pen p = new Pen(Color.Red, 4))
                            {
                                g.DrawRectangle(p, bmp.Width * 0.1f, bmp.Height * 0.1f, bmp.Width * 0.8f, bmp.Height * 0.8f);
                            }

                            using (SolidBrush bg = new SolidBrush(Color.Red))
                            {
                                g.FillRectangle(bg, 0, 0, 92, 23);
                            }

                            g.DrawString($"Card {confidence:F2}", smallFont, Brushes.White, 5, 3);
                            description = $"已框選主要牌面區域，辨識到 {suitNames[suit]} {pointNames[point]}。";
                            break;

                        case "HOG":
                            using (Pen p = new Pen(Color.LightGreen, 1))
                            {
                                int step = 18;
                                for (int y = 0; y < bmp.Height; y += step)
                                {
                                    g.DrawLine(p, 0, y, bmp.Width, y);
                                }

                                for (int x = 0; x < bmp.Width; x += step)
                                {
                                    g.DrawLine(p, x, 0, x, bmp.Height);
                                }
                            }

                            using (Pen p2 = new Pen(Color.LimeGreen, 2))
                            {
                                for (int y = 12; y < bmp.Height; y += 24)
                                {
                                    for (int x = 12; x < bmp.Width; x += 24)
                                    {
                                        g.DrawLine(p2, x - 4, y + 4, x + 4, y - 4);
                                    }
                                }
                            }

                            using (SolidBrush bg = new SolidBrush(Color.FromArgb(155, 0, 50, 0)))
                            {
                                g.FillRectangle(bg, 0, 0, bmp.Width, 25);
                            }

                            g.DrawString("HOG FEATURE", smallFont, Brushes.White, 5, 3);
                            description = $"已擷取梯度特徵，用於牌面紋理、輪廓與方向分析。";
                            break;

                        case "Mask":
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(115, 0, 120, 255)))
                            {
                                g.FillEllipse(b, bmp.Width * 0.14f, bmp.Height * 0.14f, bmp.Width * 0.72f, bmp.Height * 0.72f);
                            }

                            using (Pen p = new Pen(Color.DeepSkyBlue, 3))
                            {
                                g.DrawEllipse(p, bmp.Width * 0.14f, bmp.Height * 0.14f, bmp.Width * 0.72f, bmp.Height * 0.72f);
                            }

                            using (SolidBrush bg = new SolidBrush(Color.FromArgb(185, 0, 0, 60)))
                            {
                                g.FillRectangle(bg, 0, 0, 95, 23);
                            }

                            g.DrawString("MASK 0.88", smallFont, Brushes.White, 5, 3);
                            description = $"已產生牌面重點區域遮罩，可視化模型關注範圍。";
                            break;

                        case "MOT":
                            using (Pen p = new Pen(Color.Yellow, 3))
                            {
                                p.DashStyle = DashStyle.Dash;
                                int cx = bmp.Width / 2;
                                g.DrawLine(p, cx - 25, bmp.Height - 22, cx - 4, bmp.Height / 2 + 30);
                                g.DrawLine(p, cx - 4, bmp.Height / 2 + 30, cx + 20, bmp.Height / 2 - 8);
                            }

                            g.FillEllipse(Brushes.Red, bmp.Width / 2 - 7, bmp.Height / 2 - 7, 14, 14);

                            using (SolidBrush bg = new SolidBrush(Color.FromArgb(185, 50, 50, 0)))
                            {
                                g.FillRectangle(bg, 0, 0, 82, 23);
                            }

                            g.DrawString($"ID {index + 1}", smallFont, Brushes.White, 5, 3);
                            description = $"已指派追蹤 ID={index + 1}，可模擬多張牌的目標追蹤流程。";
                            break;

                        case "OCR":
                            using (SolidBrush bg = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                            {
                                g.FillRectangle(bg, 0, bmp.Height - 36, bmp.Width, 36);
                            }

                            Brush txtBrush = (suit == 1 || suit == 2) ? Brushes.OrangeRed : Brushes.Lime;
                            g.DrawString($"{suitNames[suit]} {pointNames[point]}", new Font("Arial", 15, FontStyle.Bold), txtBrush, 6, bmp.Height - 31);
                            description = $"已擷取 OCR 結果：{suitNames[suit]} {pointNames[point]}。";
                            break;
                    }

                    using (Pen border = new Pen(Color.WhiteSmoke, 2))
                    {
                        g.DrawRectangle(border, 1, 1, bmp.Width - 3, bmp.Height - 3);
                    }
                }
            }

            return new AnalysisItem
            {
                CardIndex = index + 1,
                CardText = cardText,
                Confidence = confidence,
                Status = status,
                Description = description,
                PreviewImage = bmp
            };
        }

        private double GenerateConfidenceByMode(string mode)
        {
            switch (mode)
            {
                case "CV":
                    return 0.92 + rand.NextDouble() * 0.05;
                case "RCNN":
                    return 0.95 + rand.NextDouble() * 0.04;
                case "HOG":
                    return 0.87 + rand.NextDouble() * 0.07;
                case "Mask":
                    return 0.89 + rand.NextDouble() * 0.06;
                case "MOT":
                    return 0.90 + rand.NextDouble() * 0.05;
                case "OCR":
                    return 0.93 + rand.NextDouble() * 0.05;
                default:
                    return 0.90;
            }
        }

        private void FillAnalysisList(List<AnalysisItem> results)
        {
            lvAnalysis.Items.Clear();

            foreach (AnalysisItem item in results)
            {
                ListViewItem row = new ListViewItem(item.CardIndex.ToString());
                row.SubItems.Add(item.CardText);
                row.SubItems.Add(item.Confidence.ToString("F2"));
                row.SubItems.Add(item.Status);
                row.SubItems.Add(item.Description);
                lvAnalysis.Items.Add(row);
            }
        }

        private void BuildAnalysisThumbs(List<AnalysisItem> results)
        {
            pnlAnalysisThumbs.Controls.Clear();

            for (int i = 0; i < results.Count; i++)
            {
                AnalysisItem item = results[i];

                Panel cardPanel = new Panel();
                cardPanel.Width = 88;
                cardPanel.Height = 112;
                cardPanel.Margin = new Padding(5);
                cardPanel.BackColor = Color.FromArgb(18, 27, 40);

                PictureBox thumb = new PictureBox();
                thumb.Width = 72;
                thumb.Height = 92;
                thumb.Left = 8;
                thumb.Top = 4;
                thumb.SizeMode = PictureBoxSizeMode.Zoom;
                thumb.Image = item.PreviewImage;
                thumb.BorderStyle = BorderStyle.FixedSingle;
                thumb.Cursor = Cursors.Hand;

                Label lbl = new Label();
                lbl.AutoSize = false;
                lbl.Width = 84;
                lbl.Height = 18;
                lbl.Left = 2;
                lbl.Top = 92;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.Font = new Font("微軟正黑體", 8.5F, FontStyle.Bold);
                lbl.ForeColor = ColorText;
                lbl.Text = $"#{item.CardIndex} {item.CardText}";

                int index = i;
                thumb.Click += (s, e) =>
                {
                    picAnalysisMain.Image = results[index].PreviewImage;
                    lblAnalysisTitle.Text = $"AI 視覺化預覽 - 第 {results[index].CardIndex} 張牌（{results[index].CardText}）";
                };

                cardPanel.Controls.Add(thumb);
                cardPanel.Controls.Add(lbl);
                pnlAnalysisThumbs.Controls.Add(cardPanel);
            }
        }

        private Image BuildBoardMontage(List<AnalysisItem> results)
        {
            int cardW = 120;
            int cardH = (int)(cardW * CardRatio);
            int gap = 10;
            int padding = 16;

            int width = padding * 2 + results.Count * cardW + (results.Count - 1) * gap;
            int height = padding * 2 + cardH + 38;

            Bitmap board = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(board))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (LinearGradientBrush bg = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(16, 18, 28),
                    Color.FromArgb(42, 23, 10),
                    90F))
                {
                    g.FillRectangle(bg, 0, 0, width, height);
                }

                using (Pen p = new Pen(Color.FromArgb(90, ColorGold), 2))
                {
                    g.DrawRectangle(p, 2, 2, width - 4, height - 4);
                }

                for (int i = 0; i < results.Count; i++)
                {
                    int x = padding + i * (cardW + gap);
                    int y = padding + 22;

                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    {
                        g.FillRectangle(shadow, x + 5, y + 5, cardW, cardH);
                    }

                    g.FillRectangle(Brushes.WhiteSmoke, x - 3, y - 3, cardW + 6, cardH + 6);
                    g.DrawImage(results[i].PreviewImage, x, y, cardW, cardH);

                    using (Brush txtBrush = new SolidBrush(ColorGold))
                    using (Font f = new Font("Arial", 9, FontStyle.Bold))
                    {
                        g.DrawString($"#{results[i].CardIndex}  {results[i].CardText}", f, txtBrush, x, 6);
                    }
                }
            }

            return board;
        }

        private string BuildAnalysisSummary(string mode, List<AnalysisItem> results)
        {
            StringBuilder sb = new StringBuilder();

            HandEvaluation current = EvaluateHandDetailed(playerPoker);

            sb.AppendLine($"【{GetModeDisplayName(mode)}】AI 專題展示報告");
            sb.AppendLine($"分析張數：{results.Count}");
            sb.AppendLine($"平均信心度：{results.Average(x => x.Confidence):F2}");
            sb.AppendLine($"目前手牌：{GetHandText()}");
            sb.AppendLine($"目前牌型：{current.RankName}，倍率 {current.Multiplier}x");
            sb.AppendLine();

            switch (mode)
            {
                case "CV":
                    sb.AppendLine("功能說明：以影像增強方式模擬去霧、提升亮度與局部清晰度，使牌面邊緣更容易觀察。");
                    break;
                case "RCNN":
                    sb.AppendLine("功能說明：以 R-CNN 的概念模擬牌面偵測，框出主要牌面範圍並給出信心度。");
                    break;
                case "HOG":
                    sb.AppendLine("功能說明：以 HOG 特徵視覺化方式呈現牌面的輪廓、紋理與方向分布。");
                    break;
                case "Mask":
                    sb.AppendLine("功能說明：以遮罩視覺化顯示模型特別關注的牌面區域。");
                    break;
                case "MOT":
                    sb.AppendLine("功能說明：模擬多目標追蹤流程，為每張牌標示追蹤 ID 與移動路徑。");
                    break;
                case "OCR":
                    sb.AppendLine("功能說明：直接辨識每張牌的花色與點數，作為文字化輸出。");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine("逐張分析：");

            foreach (AnalysisItem item in results)
            {
                sb.AppendLine($"- 第 {item.CardIndex} 張：{item.CardText} | 信心度 {item.Confidence:F2} | {item.Description}");
            }

            return sb.ToString();
        }

        private string GetModeDisplayName(string mode)
        {
            switch (mode)
            {
                case "CV": return "CV 去霧";
                case "RCNN": return "R-CNN 物件偵測";
                case "HOG": return "HOG 梯度直方";
                case "Mask": return "Mask R-CNN 遮罩";
                case "MOT": return "多目標追蹤";
                case "OCR": return "OCR 光學辨識";
                default: return mode;
            }
        }

        private void RunDashboardAndAdvice(bool applySuggestion)
        {
            if (!hasDealtHand)
            {
                MessageBox.Show("請先發牌後再執行統計分析！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetAIPanel("目前尚未發牌，因此無法建立統計儀表板。");
                return;
            }

            prgAnalysis.Value = 10;
            lblAnalysisMode.Text = applySuggestion ? "目前模式：最佳換牌建議" : "目前模式：統計儀表板";
            txtAILog.Text = "正在進行 Monte Carlo 模擬，分析 32 種保留組合...";
            txtAdvice.Text = "正在計算最佳換牌策略，請稍候...";

            Application.DoEvents();

            lastStrategyResults = AnalyzeAllHoldStrategies(420);
            lastBestStrategy = lastStrategyResults
                .OrderByDescending(x => x.ExpectedMultiplier)
                .ThenByDescending(x => x.WinRate)
                .First();

            prgAnalysis.Value = 70;

            HandEvaluation current = EvaluateHandDetailed(playerPoker);

            lblCurrentHand.Text = "目前牌型\n" + current.RankName + " / " + current.Multiplier + "x";
            lblBestAction.Text = "最佳建議\n" + lastBestStrategy.ChangeText;
            lblExpectedValue.Text = "期望倍率\n" + lastBestStrategy.ExpectedMultiplier.ToString("F2") + "x";

            picRankChart.Image = DrawRankDistributionChart(lastBestStrategy);
            picStrategyChart.Image = DrawStrategyEVChart(lastStrategyResults.Take(10).ToList());

            txtAdvice.Text = BuildAdviceSummary(lastBestStrategy, lastStrategyResults);

            recommendedHoldMask = lastBestStrategy.HoldMask;

            if (applySuggestion && canSelectCards)
            {
                for (int i = 0; i < 5; i++)
                {
                    cardSelectedToChange[i] = !IsCardHeld(lastBestStrategy.HoldMask, i);
                }

                txtResult.Text = "已套用 AI 最佳換牌建議。紅色標記的牌按「換牌」後會被換掉。";
            }
            else
            {
                txtResult.Text = "已完成統計儀表板與最佳換牌分析。若尚未換牌，可按「最佳換牌建議」自動標記建議換掉的牌。";
            }

            UpdateAllCardVisuals();

            picAnalysisMain.Image = BuildRecommendationBoard(lastBestStrategy);
            lblAnalysisTitle.Text = "AI 最佳換牌建議視覺化";

            lblDetectedCount.Text = "策略數：32";
            lblAvgConfidence.Text = $"最佳期望倍率：{lastBestStrategy.ExpectedMultiplier:F2}x";
            prgAnalysis.Value = 100;

            txtAILog.Text = BuildProjectStyleSummary(lastBestStrategy, current);
        }

        private List<StrategyResult> AnalyzeAllHoldStrategies(int samplesPerStrategy)
        {
            List<StrategyResult> results = new List<StrategyResult>();

            for (int mask = 0; mask < 32; mask++)
            {
                StrategyResult r = AnalyzeSingleHoldStrategy(mask, samplesPerStrategy);
                results.Add(r);
            }

            return results
                .OrderByDescending(x => x.ExpectedMultiplier)
                .ThenByDescending(x => x.WinRate)
                .ToList();
        }

        private StrategyResult AnalyzeSingleHoldStrategy(int holdMask, int samples)
        {
            List<int> currentCards = playerPoker.ToList();
            List<int> remainingDeck = Enumerable.Range(0, 52)
                .Where(id => !currentCards.Contains(id))
                .ToList();

            List<int> heldCards = new List<int>();

            for (int i = 0; i < 5; i++)
            {
                if (IsCardHeld(holdMask, i))
                {
                    heldCards.Add(playerPoker[i]);
                }
            }

            int drawCount = 5 - heldCards.Count;

            double totalMultiplier = 0;
            int winCount = 0;

            Dictionary<string, int> rankCounts = new Dictionary<string, int>();

            string[] categories = { "同花大順", "同花順", "鐵支", "葫蘆", "同花", "順子", "三條", "兩對", "一對", "雜牌" };

            foreach (string c in categories)
            {
                rankCounts[c] = 0;
            }

            for (int s = 0; s < samples; s++)
            {
                List<int> deck = new List<int>(remainingDeck);
                List<int> hand = new List<int>(heldCards);

                for (int d = 0; d < drawCount; d++)
                {
                    int idx = rand.Next(deck.Count);
                    hand.Add(deck[idx]);
                    deck.RemoveAt(idx);
                }

                HandEvaluation eval = EvaluateHandDetailed(hand);

                totalMultiplier += eval.Multiplier;

                if (eval.Multiplier > 0)
                {
                    winCount++;
                }

                if (!rankCounts.ContainsKey(eval.Category))
                {
                    rankCounts[eval.Category] = 0;
                }

                rankCounts[eval.Category]++;
            }

            StrategyResult result = new StrategyResult();
            result.HoldMask = holdMask;
            result.ExpectedMultiplier = totalMultiplier / samples;
            result.WinRate = winCount * 100.0 / samples;
            result.Samples = samples;
            result.RankCounts = rankCounts;
            result.HoldText = BuildHoldDescription(holdMask);
            result.ChangeText = BuildChangeDescription(holdMask);

            return result;
        }

        private bool IsCardHeld(int holdMask, int index)
        {
            return (holdMask & (1 << index)) != 0;
        }

        private string BuildHoldDescription(int holdMask)
        {
            if (holdMask == 0)
            {
                return "全部不保留";
            }

            if (holdMask == 31)
            {
                return "全部保留";
            }

            List<string> list = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                if (IsCardHeld(holdMask, i))
                {
                    list.Add($"第{i + 1}張({GetCardText(playerPoker[i])})");
                }
            }

            return "保留 " + string.Join("、", list);
        }

        private string BuildChangeDescription(int holdMask)
        {
            if (holdMask == 31)
            {
                return "不換牌";
            }

            if (holdMask == 0)
            {
                return "全部換掉";
            }

            List<string> list = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                if (!IsCardHeld(holdMask, i))
                {
                    list.Add($"第{i + 1}張({GetCardText(playerPoker[i])})");
                }
            }

            return "換掉 " + string.Join("、", list);
        }

        private string BuildAdviceSummary(StrategyResult best, List<StrategyResult> strategies)
        {
            StringBuilder sb = new StringBuilder();

            HandEvaluation current = EvaluateHandDetailed(playerPoker);

            sb.AppendLine("【最佳換牌建議】");
            sb.AppendLine($"目前手牌：{GetHandText()}");
            sb.AppendLine($"目前牌型：{current.RankName}，目前倍率：{current.Multiplier}x");
            sb.AppendLine();
            sb.AppendLine($"AI 建議：{best.ChangeText}");
            sb.AppendLine($"保留策略：{best.HoldText}");
            sb.AppendLine($"模擬樣本：每種策略 {best.Samples} 次，共分析 32 種保留組合");
            sb.AppendLine($"預估中獎率：{best.WinRate:F2}%");
            sb.AppendLine($"期望倍率：{best.ExpectedMultiplier:F2}x");
            sb.AppendLine();

            if (canSelectCards)
            {
                sb.AppendLine("操作提示：");
                sb.AppendLine("按「最佳換牌建議」會自動把建議換掉的牌標成紅色，接著直接按「換牌」即可。");
            }
            else
            {
                sb.AppendLine("操作提示：");
                sb.AppendLine("目前已不能再換牌，因此此建議僅供分析展示。");
            }

            sb.AppendLine();
            sb.AppendLine("Top 6 策略排名：");

            int rank = 1;

            foreach (StrategyResult s in strategies.Take(6))
            {
                sb.AppendLine($"{rank}. {s.ChangeText} | 期望倍率 {s.ExpectedMultiplier:F2}x | 中獎率 {s.WinRate:F2}%");
                rank++;
            }

            sb.AppendLine();
            sb.AppendLine("可能結果分布：");

            foreach (KeyValuePair<string, int> kv in best.RankCounts.OrderByDescending(x => x.Value))
            {
                double pct = kv.Value * 100.0 / best.Samples;
                sb.AppendLine($"- {kv.Key}：{pct:F2}%");
            }

            return sb.ToString();
        }

        private string BuildProjectStyleSummary(StrategyResult best, HandEvaluation current)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("【AI 專題展示摘要】");
            sb.AppendLine("主題：五張撲克牌遊戲中的影像辨識與決策輔助");
            sb.AppendLine();
            sb.AppendLine("1. 影像辨識模組");
            sb.AppendLine("   - CV 去霧：模擬影像前處理");
            sb.AppendLine("   - R-CNN：模擬牌面偵測與框選");
            sb.AppendLine("   - HOG：模擬輪廓與紋理特徵擷取");
            sb.AppendLine("   - Mask：模擬模型關注區域");
            sb.AppendLine("   - OCR：輸出花色與點數");
            sb.AppendLine();
            sb.AppendLine("2. 決策分析模組");
            sb.AppendLine($"   - 目前牌型：{current.RankName}");
            sb.AppendLine($"   - 最佳策略：{best.ChangeText}");
            sb.AppendLine($"   - 預估中獎率：{best.WinRate:F2}%");
            sb.AppendLine($"   - 期望倍率：{best.ExpectedMultiplier:F2}x");
            sb.AppendLine();
            sb.AppendLine("3. 統計圖表");
            sb.AppendLine("   - 左圖：採用最佳策略後的可能牌型分布");
            sb.AppendLine("   - 右圖：前十名換牌策略的期望倍率比較");
            sb.AppendLine();
            sb.AppendLine("這樣下方功能不只是裝飾，而是能呈現 AI 專題、統計決策與遊戲輔助效果。");

            return sb.ToString();
        }

        private Image BuildRecommendationBoard(StrategyResult strategy)
        {
            int cardW = 130;
            int cardH = (int)(cardW * CardRatio);
            int gap = 16;
            int padding = 20;

            int width = padding * 2 + 5 * cardW + 4 * gap;
            int height = padding * 2 + cardH + 48;

            Bitmap board = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(board))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (LinearGradientBrush bg = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(10, 18, 28),
                    Color.FromArgb(5, 70, 40),
                    90F))
                {
                    g.FillRectangle(bg, 0, 0, width, height);
                }

                using (Font titleFont = new Font("微軟正黑體", 13F, FontStyle.Bold))
                using (Brush titleBrush = new SolidBrush(ColorGold))
                {
                    g.DrawString("AI 最佳換牌建議", titleFont, titleBrush, 18, 10);
                }

                for (int i = 0; i < 5; i++)
                {
                    int x = padding + i * (cardW + gap);
                    int y = padding + 40;

                    Image card = CreateCardVisualForStrategy(playerPoker[i], i, strategy.HoldMask);

                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                    {
                        g.FillRectangle(shadow, x + 6, y + 6, cardW, cardH);
                    }

                    g.DrawImage(card, x, y, cardW, cardH);
                }
            }

            return board;
        }

        private Image CreateCardVisualForStrategy(int cardId, int index, int holdMask)
        {
            object rm = Properties.Resources.ResourceManager.GetObject("pic" + (cardId + 1));
            Image baseImage = rm != null ? (Image)rm : Properties.Resources.back;
            Bitmap bmp = new Bitmap(baseImage);

            if (IsCardHeld(holdMask, index))
            {
                DrawCardBadge(bmp, "保留", ColorSuccess);
            }
            else
            {
                DrawCardBadge(bmp, "換掉", ColorDanger);
            }

            return bmp;
        }

        private Image DrawRankDistributionChart(StrategyResult strategy)
        {
            List<KeyValuePair<string, double>> values = new List<KeyValuePair<string, double>>();

            string[] order = { "同花大順", "同花順", "鐵支", "葫蘆", "同花", "順子", "三條", "兩對", "一對", "雜牌" };

            foreach (string rank in order)
            {
                int count = strategy.RankCounts.ContainsKey(rank) ? strategy.RankCounts[rank] : 0;
                double pct = count * 100.0 / strategy.Samples;

                if (pct > 0.01)
                {
                    values.Add(new KeyValuePair<string, double>(rank, pct));
                }
            }

            return DrawBarChart("最佳策略後的牌型分布 %", values, "%");
        }

        private Image DrawStrategyEVChart(List<StrategyResult> strategies)
        {
            List<KeyValuePair<string, double>> values = new List<KeyValuePair<string, double>>();

            foreach (StrategyResult s in strategies)
            {
                values.Add(new KeyValuePair<string, double>(ShortMaskText(s.HoldMask), s.ExpectedMultiplier));
            }

            return DrawBarChart("Top 10 換牌策略期望倍率", values, "x");
        }

        private Image DrawBarChart(string title, List<KeyValuePair<string, double>> values, string unit)
        {
            int width = 560;
            int height = 260;

            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (LinearGradientBrush bg = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(8, 12, 18),
                    Color.FromArgb(18, 28, 42),
                    90F))
                {
                    g.FillRectangle(bg, 0, 0, width, height);
                }

                using (Font titleFont = new Font("微軟正黑體", 13F, FontStyle.Bold))
                using (Brush titleBrush = new SolidBrush(ColorGold))
                {
                    g.DrawString(title, titleFont, titleBrush, 16, 12);
                }

                if (values == null || values.Count == 0)
                {
                    using (Font f = new Font("微軟正黑體", 11F, FontStyle.Bold))
                    {
                        g.DrawString("尚無資料", f, Brushes.WhiteSmoke, 220, 115);
                    }

                    return bmp;
                }

                double max = values.Max(x => x.Value);

                if (max <= 0)
                {
                    max = 1;
                }

                int chartLeft = 80;
                int chartTop = 48;
                int chartWidth = width - 105;
                int chartHeight = height - 88;

                using (Pen axisPen = new Pen(Color.FromArgb(120, ColorMuted), 1))
                {
                    g.DrawLine(axisPen, chartLeft, chartTop, chartLeft, chartTop + chartHeight);
                    g.DrawLine(axisPen, chartLeft, chartTop + chartHeight, chartLeft + chartWidth, chartTop + chartHeight);
                }

                int barCount = values.Count;
                int gap = 6;
                int barWidth = Math.Max(18, (chartWidth - gap * (barCount - 1)) / barCount);

                using (Font labelFont = new Font("微軟正黑體", 8F, FontStyle.Bold))
                using (Font valueFont = new Font("Consolas", 8F, FontStyle.Bold))
                {
                    for (int i = 0; i < barCount; i++)
                    {
                        double v = values[i].Value;
                        int h = (int)(v / max * (chartHeight - 18));
                        int x = chartLeft + i * (barWidth + gap);
                        int y = chartTop + chartHeight - h;

                        Color barColor = i == 0 ? ColorGold : ColorNeon;

                        using (LinearGradientBrush bar = new LinearGradientBrush(
                            new Rectangle(x, y, barWidth, Math.Max(1, h)),
                            Color.FromArgb(230, barColor),
                            Color.FromArgb(90, barColor),
                            90F))
                        {
                            g.FillRectangle(bar, x, y, barWidth, h);
                        }

                        using (Pen p = new Pen(Color.FromArgb(160, barColor), 1))
                        {
                            g.DrawRectangle(p, x, y, barWidth, h);
                        }

                        string valueText = v.ToString("F2") + unit;
                        SizeF vs = g.MeasureString(valueText, valueFont);
                        g.DrawString(valueText, valueFont, Brushes.WhiteSmoke, x + (barWidth - vs.Width) / 2, y - 15);

                        string label = values[i].Key;
                        SizeF ls = g.MeasureString(label, labelFont);
                        g.DrawString(label, labelFont, Brushes.Gainsboro, x + (barWidth - ls.Width) / 2, chartTop + chartHeight + 5);
                    }
                }
            }

            return bmp;
        }

        private string ShortMaskText(int holdMask)
        {
            if (holdMask == 0)
            {
                return "全換";
            }

            if (holdMask == 31)
            {
                return "全留";
            }

            List<string> nums = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                if (IsCardHeld(holdMask, i))
                {
                    nums.Add((i + 1).ToString());
                }
            }

            return "留" + string.Join("", nums);
        }

        private void ResetAIPanel(string message)
        {
            if (pnlAnalysisThumbs != null)
            {
                pnlAnalysisThumbs.Controls.Clear();
            }

            if (lvAnalysis != null)
            {
                lvAnalysis.Items.Clear();
            }

            if (lblAnalysisTitle != null)
            {
                lblAnalysisTitle.Text = "AI 視覺化預覽";
            }

            if (lblAnalysisMode != null)
            {
                lblAnalysisMode.Text = "目前模式：尚未執行";
            }

            if (lblDetectedCount != null)
            {
                lblDetectedCount.Text = "偵測張數：0";
            }

            if (lblAvgConfidence != null)
            {
                lblAvgConfidence.Text = "平均信心度：0.00";
            }

            if (prgAnalysis != null)
            {
                prgAnalysis.Value = 0;
            }

            if (picAnalysisMain != null)
            {
                picAnalysisMain.Image = CreatePlaceholderImage("尚未執行分析");
            }

            if (txtAILog != null)
            {
                txtAILog.Text = message;
            }

            if (picRankChart != null)
            {
                picRankChart.Image = DrawEmptyChart("牌型分布圖");
            }

            if (picStrategyChart != null)
            {
                picStrategyChart.Image = DrawEmptyChart("策略期望倍率圖");
            }

            if (txtAdvice != null)
            {
                txtAdvice.Text = "等待發牌與統計分析。";
            }

            if (lblCurrentHand != null)
            {
                lblCurrentHand.Text = "目前牌型\n等待發牌";
            }

            if (lblBestAction != null)
            {
                lblBestAction.Text = "最佳建議\n尚未分析";
            }

            if (lblExpectedValue != null)
            {
                lblExpectedValue.Text = "期望倍率\n0.00x";
            }
        }

        private Image DrawEmptyChart(string title)
        {
            Bitmap bmp = new Bitmap(560, 260);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(8, 12, 18));

                using (Pen p = new Pen(Color.FromArgb(100, ColorMuted), 2))
                {
                    p.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(p, 25, 45, bmp.Width - 50, bmp.Height - 70);
                }

                using (Font f1 = new Font("微軟正黑體", 13F, FontStyle.Bold))
                using (Font f2 = new Font("微軟正黑體", 10F, FontStyle.Regular))
                {
                    g.DrawString(title, f1, new SolidBrush(ColorGold), 16, 12);

                    string msg = "發牌後按「統計儀表板」產生圖表";
                    SizeF s = g.MeasureString(msg, f2);
                    g.DrawString(msg, f2, Brushes.Gainsboro, (bmp.Width - s.Width) / 2, 120);
                }
            }

            return bmp;
        }

        private Image CreatePlaceholderImage(string text)
        {
            Bitmap bmp = new Bitmap(620, 300);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (LinearGradientBrush bg = new LinearGradientBrush(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    Color.FromArgb(8, 12, 18),
                    Color.FromArgb(40, 25, 10),
                    90F))
                {
                    g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
                }

                using (Pen p = new Pen(Color.FromArgb(120, ColorGold), 2))
                {
                    p.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(p, 25, 25, bmp.Width - 50, bmp.Height - 50);
                }

                using (Font f1 = new Font("微軟正黑體", 18, FontStyle.Bold))
                using (Font f2 = new Font("微軟正黑體", 10, FontStyle.Regular))
                {
                    SizeF s1 = g.MeasureString(text, f1);
                    g.DrawString(text, f1, Brushes.White, (bmp.Width - s1.Width) / 2, 105);

                    string sub = "AI Vision / Dashboard / Strategy Advisor";
                    SizeF s2 = g.MeasureString(sub, f2);
                    g.DrawString(sub, f2, new SolidBrush(ColorNeon), (bmp.Width - s2.Width) / 2, 145);
                }
            }

            return bmp;
        }

        private HandEvaluation EvaluateHandDetailed(IEnumerable<int> handCards)
        {
            List<int> cards = handCards.ToList();

            string[] suitsMap = { "梅花", "方塊", "紅心", "黑桃" };
            string[] pointsMap = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

            List<int> pokerPoint = cards.Select(id => id / 4).ToList();
            List<int> pokerColor = cards.Select(id => id % 4).ToList();

            var colorGroups = pokerColor
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .ToList();

            var pointGroups = pokerPoint
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key)
                .ToList();

            List<int> colorCount = colorGroups.Select(g => g.Count()).Concat(Enumerable.Repeat(0, 5)).ToList();
            List<string> colorList = colorGroups.Select(g => suitsMap[g.Key]).Concat(Enumerable.Repeat("", 5)).ToList();

            List<int> pointCount = pointGroups.Select(g => g.Count()).Concat(Enumerable.Repeat(0, 5)).ToList();
            List<string> pointList = pointGroups.Select(g => pointsMap[g.Key]).Concat(Enumerable.Repeat("", 5)).ToList();

            bool isFlush = colorCount[0] == 5;

            bool isSingle =
                pointCount[0] == 1 &&
                pointCount[1] == 1 &&
                pointCount[2] == 1 &&
                pointCount[3] == 1 &&
                pointCount[4] == 1;

            bool isDiffFour = pokerPoint.Max() - pokerPoint.Min() == 4;

            bool isRoyal =
                pokerPoint.Contains(0) &&
                pokerPoint.Contains(9) &&
                pokerPoint.Contains(10) &&
                pokerPoint.Contains(11) &&
                pokerPoint.Contains(12);

            bool isRoyalFlush = isFlush && isRoyal;
            bool isStraightFlush = isFlush && isSingle && isDiffFour;
            bool isStraight = isSingle && (isDiffFour || isRoyal);
            bool isFourOfAKind = pointCount[0] == 4;
            bool isFullHouse = pointCount[0] == 3 && pointCount[1] == 2;
            bool isThreeOfAKind = pointCount[0] == 3 && pointCount[1] == 1;
            bool isTwoPair = pointCount[0] == 2 && pointCount[1] == 2;
            bool isOnePair = pointCount[0] == 2 && pointCount[1] == 1;

            HandEvaluation result = new HandEvaluation();

            if (isRoyalFlush)
            {
                result.RankName = $"{colorList[0]} 同花大順";
                result.Category = "同花大順";
                result.Multiplier = 250;
            }
            else if (isStraightFlush)
            {
                result.RankName = $"{colorList[0]} 同花順";
                result.Category = "同花順";
                result.Multiplier = 50;
            }
            else if (isFourOfAKind)
            {
                result.RankName = $"{pointList[0]} 鐵支";
                result.Category = "鐵支";
                result.Multiplier = 25;
            }
            else if (isFullHouse)
            {
                result.RankName = $"{pointList[0]}三張、{pointList[1]}兩張，葫蘆";
                result.Category = "葫蘆";
                result.Multiplier = 9;
            }
            else if (isFlush)
            {
                result.RankName = $"{colorList[0]} 同花";
                result.Category = "同花";
                result.Multiplier = 6;
            }
            else if (isStraight)
            {
                result.RankName = "順子";
                result.Category = "順子";
                result.Multiplier = 4;
            }
            else if (isThreeOfAKind)
            {
                result.RankName = $"{pointList[0]} 三條";
                result.Category = "三條";
                result.Multiplier = 3;
            }
            else if (isTwoPair)
            {
                result.RankName = $"{pointList[0]}、{pointList[1]} 兩對";
                result.Category = "兩對";
                result.Multiplier = 2;
            }
            else if (isOnePair)
            {
                result.RankName = $"{pointList[0]} 一對";
                result.Category = "一對";
                result.Multiplier = 1;
            }
            else
            {
                result.RankName = "雜牌";
                result.Category = "雜牌";
                result.Multiplier = 0;
            }

            return result;
        }

        private int GetMultiplier(IEnumerable<int> currentHand)
        {
            return EvaluateHandDetailed(currentHand).Multiplier;
        }

        private bool IsSequence(List<int> sortedPoints)
        {
            sortedPoints = sortedPoints.OrderBy(v => v).ToList();

            bool normalSequence = true;

            for (int i = 1; i < sortedPoints.Count; i++)
            {
                if (sortedPoints[i] != sortedPoints[i - 1] + 1)
                {
                    normalSequence = false;
                    break;
                }
            }

            bool royalSequence =
                sortedPoints.Count == 5 &&
                sortedPoints[0] == 0 &&
                sortedPoints[1] == 9 &&
                sortedPoints[2] == 10 &&
                sortedPoints[3] == 11 &&
                sortedPoints[4] == 12;

            return normalSequence || royalSequence;
        }

        private string GetCardText(int cardId)
        {
            string[] suitSymbols = { "♣", "♦", "♥", "♠" };
            string[] pointNames = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

            int point = cardId / 4;
            int suit = cardId % 4;

            return $"{suitSymbols[suit]}{pointNames[point]}";
        }

        private string GetHandText()
        {
            if (!hasDealtHand)
            {
                return "尚未發牌";
            }

            List<string> list = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                list.Add(GetCardText(playerPoker[i]));
            }

            return string.Join("  ", list);
        }

        private void UpdateHeaderStats()
        {
            if (lblHeaderStats == null)
            {
                return;
            }

            string hand = hasDealtHand ? EvaluateHandDetailed(playerPoker).RankName : "尚未發牌";

            lblHeaderStats.Text =
                $"FUNDS: {totalFunds:N0}    BET: {currentBet:N0}    HAND: {hand}";
        }

        #region 核心技術專區 Computer Vision / Deep Learning / CIA Security

        private string ApplyDarkChannelPrior()
        {
            return "✅ CV 影像去霧與優化執行完成";
        }

        private string ApplyRCNNObjectDetection()
        {
            double confidence = 0.95 + rand.NextDouble() * 0.04;
            return $"✅ R-CNN 運算完畢。信心指標：{confidence:F2}";
        }

        private void RunDeepLearningModel()
        {
            this.Text = "AI Casino Poker Lab - 五張撲克牌";

            double winRate = 0;

            if (hasDealtHand && btnEvaluate.Enabled)
            {
                StrategyResult quick = AnalyzeAllHoldStrategies(80)
                    .OrderByDescending(x => x.ExpectedMultiplier)
                    .ThenByDescending(x => x.WinRate)
                    .First();

                winRate = quick.WinRate;
            }
            else if (hasDealtHand)
            {
                winRate = EvaluateHandDetailed(playerPoker).Multiplier > 0 ? 100 : 0;
            }

            if (lblWinRate != null)
            {
                lblWinRate.Text = $"DL 勝率：{winRate:F2}%";
            }

            UpdateHeaderStats();
        }

        private async void ApplyCIASecurity()
        {
            try
            {
                string encryptedFunds = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(totalFunds.ToString())
                );

                txtTotalFunds.Text = $"[CIA防護] {encryptedFunds}";

                await Task.Delay(700);

                if (!txtTotalFunds.IsDisposed)
                {
                    txtTotalFunds.Text = totalFunds.ToString();
                }
            }
            catch (Exception ex)
            {
                this.Text = $"[InfoSec - 安全攔截] {ex.Message}";
            }
        }

        #endregion
    }
}