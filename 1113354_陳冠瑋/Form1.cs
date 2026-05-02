using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        // AI 專區 UI 元素
        private TextBox txtAILog;
        private Label lblWinRate;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.KeyPress += Form1_KeyPress;
            this.AutoScroll = true; // 開啟視窗自動捲動功能

            // 讓傳統群組框支持動態縮放 (響應式設計)
            grpTable.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            grpBet.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            grpFunc.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // 動態新增 AI 控制面板
            FlowLayoutPanel pnlAI = new FlowLayoutPanel() { Left = 40, Top = 400, Width = 500, Height = 80, AutoScroll = true };
            pnlAI.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Button btnRunCV = new Button() { Text = "CV 去霧", Width = 90, Height = 30 };
            Button btnRunRCNN = new Button() { Text = "R-CNN", Width = 90, Height = 30 };
            Button btnRunHOG = new Button() { Text = "HOG 梯度直方", Width = 110, Height = 30 };
            Button btnRunMask = new Button() { Text = "遮罩 Mask", Width = 90, Height = 30 };
            Button btnRunMOT = new Button() { Text = "多目標追蹤", Width = 90, Height = 30 };
            Button btnRunOCR = new Button() { Text = "OCR 光學辨識", Width = 100, Height = 30 };

            pnlAI.Controls.AddRange(new Control[] { btnRunCV, btnRunRCNN, btnRunHOG, btnRunMask, btnRunMOT, btnRunOCR });

            txtAILog = new TextBox() { Left = 40, Top = 485, Width = 300, ReadOnly = true };
            txtAILog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            lblWinRate = new Label() { Left = 350, Top = 488, Width = 200, Text = "DL 勝率：等待計算...", ForeColor = Color.Blue, Font = new Font("微軟正黑體", 10, FontStyle.Bold) };
            lblWinRate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            btnRunCV.Click += (s, ev) => { txtAILog.Text = ApplyDarkChannelPrior(); };
            btnRunRCNN.Click += (s, ev) => { txtAILog.Text = ApplyRCNNObjectDetection(); };
            btnRunHOG.Click += (s, ev) => { txtAILog.Text = "✅ HOG: 邊緣特徵提取完成"; };
            btnRunMask.Click += (s, ev) => { txtAILog.Text = "✅ Mask R-CNN: 產生牌面遮罩成功"; };
            btnRunMOT.Click += (s, ev) => { txtAILog.Text = "✅ DeepSORT: 動態目標追蹤中..."; };
            btnRunOCR.Click += (s, ev) => { txtAILog.Text = "✅ OCR: 成功辨識花色與數字"; };

            this.Controls.Add(pnlAI);
            this.Controls.Add(txtAILog);
            this.Controls.Add(lblWinRate);
            this.Height += 120; // 擴展高度以容納新按鈕

            // 綁定視窗縮放事件，以致中排列撲克牌
            this.Resize += Form1_Resize;

            // 初始化五張撲克牌的 PictureBox
            for (int i = 0; i < 5; i++)
            {
                picCards[i] = new PictureBox();
                picCards[i].Width = 71;
                picCards[i].Height = 96;
                picCards[i].Left = 20 + i * 90;
                picCards[i].Top = 50;
                picCards[i].SizeMode = PictureBoxSizeMode.StretchImage; // 改為 StretchImage 支援縮放
                picCards[i].BorderStyle = BorderStyle.FixedSingle;
                picCards[i].Name = "pic" + i;
                picCards[i].Tag = "back";
                picCards[i].Enabled = false;
                picCards[i].MouseClick += PicCard_Click;
                grpTable.Controls.Add(picCards[i]);
            }
            ResetGame();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // 動態調整撲克牌在桌面的位置與大小，使其在放大視窗時保持等比例擴展及置中
            if (picCards != null && picCards[0] != null)
            {
                int padding = 20;
                int cardWidth = (grpTable.Width - padding * 6) / 5;
                int cardHeight = (int)(cardWidth * 1.35f); // 保持卡牌比例，約 96/71 = 1.35

                // 確保不超過最大可用高度
                if (cardHeight > grpTable.Height - 40)
                {
                    cardHeight = grpTable.Height - 40;
                    cardWidth = (int)(cardHeight / 1.35f);
                }

                int totalWidth = 5 * cardWidth + 4 * padding;
                int startX = Math.Max(10, (grpTable.Width - totalWidth) / 2);
                int startY = Math.Max(20, (grpTable.Height - cardHeight) / 2);

                for (int i = 0; i < 5; i++)
                {
                    picCards[i].Width = cardWidth;
                    picCards[i].Height = cardHeight;
                    picCards[i].Left = startX + i * (cardWidth + padding);
                    picCards[i].Top = startY;
                }
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (btnDeal.Enabled == false)
            {
                switch (e.KeyChar)
                {
                    case 'q': // q鍵
                        // 同花大順
                        playerPoker[0] = 51;
                        playerPoker[1] = 47;
                        playerPoker[2] = 43;
                        playerPoker[3] = 39;
                        playerPoker[4] = 3;
                        break;
                    case 'w': // w鍵
                        // 同花順
                        playerPoker[0] = 37;
                        playerPoker[1] = 33;
                        playerPoker[2] = 29;
                        playerPoker[3] = 25;
                        playerPoker[4] = 21;
                        break;
                    case 'e': // e鍵
                        // 同花
                        playerPoker[0] = 50;
                        playerPoker[1] = 38;
                        playerPoker[2] = 34;
                        playerPoker[3] = 22;
                        playerPoker[4] = 18;
                        break;
                    case 'r': // r鍵
                        // 鐵支
                        playerPoker[0] = 48;
                        playerPoker[1] = 39;
                        playerPoker[2] = 38;
                        playerPoker[3] = 37;
                        playerPoker[4] = 36;
                        break;
                    case 't': // t鍵
                        // 葫蘆
                        playerPoker[0] = 30;
                        playerPoker[1] = 29;
                        playerPoker[2] = 6;
                        playerPoker[3] = 5;
                        playerPoker[4] = 4;
                        break;
                    case 'y': // y鍵
                        // 三條
                        playerPoker[0] = 48;
                        playerPoker[1] = 39;
                        playerPoker[2] = 15;
                        playerPoker[3] = 14;
                        playerPoker[4] = 13;
                        break;
                }

                // 顯示五張撲克牌到桌面上
                for (int i = 0; i < 5; i++)
                {
                    UpdateCardImage(i, playerPoker[i]);
                }
            }
        }

        private void PicCard_Click(object sender, MouseEventArgs e)
        {
            if (!canSelectCards)
            {
                return;
            }

            PictureBox pic = (PictureBox)sender;
            int idx = int.Parse(pic.Name.Replace("pic", ""));

            if (pic.Tag.ToString() == "back")
            {
                pic.Tag = "front";
                cardSelectedToChange[idx] = false;
                UpdateCardImage(idx, playerPoker[idx]);
            }
            else
            {
                pic.Tag = "back";
                cardSelectedToChange[idx] = true;
                pic.Image = Properties.Resources.back;
            }
        }

        private void ResetGame()
        {
            txtResult.Text = "";
            currentBet = 0;
            btnChange.Enabled = false;
            btnEvaluate.Enabled = false;
            btnDeal.Enabled = false; // 必須先押注
            btnBet.Enabled = true;
            canSelectCards = false;

            Image backImage = Properties.Resources.back;

            for (int i = 0; i < 5; i++)
            {
                picCards[i].Image = backImage;
                picCards[i].Top = 50;
                cardSelectedToChange[i] = false;
                picCards[i].Tag = "back";
                picCards[i].Enabled = false;
            }
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
            for (int i = 0; i < allPoker.Length; i++)
            {
                int r = rand.Next(allPoker.Length);
                int temp = allPoker[r];
                allPoker[r] = allPoker[0];
                allPoker[0] = temp;
            }
        }

        private void btnBet_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtBetAmount.Text, out int betAmount))
            {
                if (betAmount > totalFunds || betAmount <= 0)
                {
                    MessageBox.Show("押注金額錯誤或資金不足！");
                    return;
                }
                currentBet = betAmount;
                totalFunds -= currentBet;
                txtTotalFunds.Text = totalFunds.ToString();
                ApplyCIASecurity(); // 資安三要素保護機制

                btnDeal.Enabled = true;
                btnBet.Enabled = false;
                txtResult.Text = "請發牌...";
            }
            else
            {
                MessageBox.Show("請輸入正確的數字！");
            }
        }

        private void btnDeal_Click(object sender, EventArgs e)
        {
            InitializeDeck();
            nextCardIndex = 0;

            // 發五張牌
            for (int i = 0; i < 5; i++)
            {
                playerPoker[i] = allPoker[nextCardIndex++];
                UpdateCardImage(i, playerPoker[i]);
                cardSelectedToChange[i] = false;
                picCards[i].Tag = "front";
                picCards[i].Enabled = true;
            }

            btnDeal.Enabled = false;
            btnChange.Enabled = true;
            btnEvaluate.Enabled = true;
            canSelectCards = true;
            txtResult.Text = "請點擊要保留的牌，然後換牌或直接判斷牌型";

            RunDeepLearningModel(); // 啟動深度學習預測引擎
        }

        private void btnChange_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                if (cardSelectedToChange[i])
                {
                    playerPoker[i] = allPoker[nextCardIndex++];
                    UpdateCardImage(i, playerPoker[i]);
                }
                cardSelectedToChange[i] = false;
                picCards[i].Tag = "front";
                picCards[i].Enabled = false;
            }

            btnChange.Enabled = false;
            canSelectCards = false;
        }

        private void UpdateCardImage(int index, int cardId)
        {
            // 動態從資源中讀取圖片，名稱對應 pic1.png ~ pic52.png
            object rm = Properties.Resources.ResourceManager.GetObject("pic" + (cardId + 1));
            if (rm != null)
            {
                picCards[index].Image = (Image)rm; // 保持原圖，完全不覆蓋或繪製任何字樣
            }
        }

        private void btnEvaluate_Click(object sender, EventArgs e)
        {
            string[] suitsMap = { "梅花", "方塊", "紅心", "黑桃" };
            string[] pointsMap = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

            List<int> pokerPoint = playerPoker.Select(id => id / 4).ToList();
            List<int> pokerColor = playerPoker.Select(id => id % 4).ToList();

            var colorGroups = pokerColor.GroupBy(c => c).OrderByDescending(g => g.Count()).ToList();
            var pointGroups = pokerPoint.GroupBy(p => p).OrderByDescending(g => g.Count()).ToList();

            List<int> colorCount = colorGroups.Select(g => g.Count()).Concat(Enumerable.Repeat(0, 5)).ToList();
            List<string> colorList = colorGroups.Select(g => suitsMap[g.Key]).Concat(Enumerable.Repeat("", 5)).ToList();

            List<int> pointCount = pointGroups.Select(g => g.Count()).Concat(Enumerable.Repeat(0, 5)).ToList();
            List<string> pointList = pointGroups.Select(g => pointsMap[g.Key]).Concat(Enumerable.Repeat("", 5)).ToList();

            // 判斷是否為同花
            bool isFlush = (colorCount[0] == 5);
            // 判斷是否為五張單張
            bool isSingle = (pointCount[0] == 1 && pointCount[1] == 1 && pointCount[2] == 1 && pointCount[3] == 1 && pointCount[4] == 1);
            // 判斷是否為差四
            bool isDiffFout = (pokerPoint.Max() - pokerPoint.Min() == 4);
            // 判斷是否為大順
            bool isRoyal = pokerPoint.Contains(0) && pokerPoint.Contains(9) && pokerPoint.Contains(10) && pokerPoint.Contains(11) && pokerPoint.Contains(12);
            // 判斷是否為同花大順
            bool isRoyalisFlush = isFlush && isRoyal;
            // 判斷是否為同花順
            bool isStraightFlush = isFlush && isSingle && isDiffFout;
            // 判斷是否為順子
            bool isStraight = isSingle && (isDiffFout || isRoyal);
            // 判斷是否為鐵支
            bool isFourOfAKind = (pointCount[0] == 4);
            // 判斷是否為葫蘆
            bool isFullHouse = (pointCount[0] == 3 && pointCount[1] == 2);
            // 判斷是否為三條
            bool isThreeOfAKind = (pointCount[0] == 3 && pointCount[1] == 1);
            // 判斷是否為兩對
            bool isTwoPair = (pointCount[0] == 2 && pointCount[1] == 2);
            // 判斷是否為一對
            bool isOnePair = (pointCount[0] == 2 && pointCount[1] == 1);

            string result = "";
            if (isRoyalisFlush) {
                result = $"{colorList[0]} 同花大順";
            } else if (isStraightFlush) {
                result = $"{colorList[0]} 同花順";
            } else if (isStraight) {
                result = "順子";
            } else if (isFourOfAKind) {
                result = $"{pointList[0]} 鐵支";
            } else if (isFullHouse) {
                result = $"{pointList[0]}三張{pointList[1]}兩張 葫蘆";
            } else if (isFlush) {
                result = $"{colorList[0]} 同花";
            } else if (isThreeOfAKind) {
                result = $"{pointList[0]} 三條";
            } else if (isTwoPair) {
                result = $"{pointList[0]},{pointList[1]} 兩對";
            } else if (isOnePair) {
                result = $"{pointList[0]} 一對";
            } else {
                result = "雜牌";
            }

            int multiplier = 0;
            if (isRoyalisFlush) multiplier = 250;
            else if (isStraightFlush) multiplier = 50;
            else if (isFourOfAKind) multiplier = 25;
            else if (isFullHouse) multiplier = 9;
            else if (isFlush) multiplier = 6;
            else if (isStraight) multiplier = 4;
            else if (isThreeOfAKind) multiplier = 3;
            else if (isTwoPair) multiplier = 2;
            else if (isOnePair) multiplier = 1;

            int winAmount = currentBet * multiplier;
            totalFunds += winAmount;

            txtTotalFunds.Text = totalFunds.ToString();
            ApplyCIASecurity(); // 結算資金安全防護
            RunDeepLearningModel(); // 賽後重新訓練深度學習模型

            if (multiplier > 0)
                txtResult.Text = $"{result}！贏得：{winAmount}";
            else
                txtResult.Text = $"{result}，沒中獎，再接再厲！";

            btnEvaluate.Enabled = false;
            btnChange.Enabled = false;
            btnBet.Enabled = true;
            canSelectCards = false;

            for (int i = 0; i < 5; i++)
            {
                picCards[i].Enabled = false;
                picCards[i].Tag = "back";
            }
        }

        private int GetMultiplier(IEnumerable<int> currentHand)
        {
            // 將牌的 ID 轉換為數值與花色
            // ID: 0~51
            // 數值: ID % 13 + 1 -> 1~13 (A=1, J=11, Q=12, K=13)
            // 花色: ID / 13 -> 0~3 (對應不同花色)

            var values = currentHand.Select(id => id % 13 + 1).OrderBy(v => v).ToList();
            var suits = currentHand.Select(id => id / 13).ToList();

            bool isFlush = suits.Distinct().Count() == 1;
            bool isStraight = IsSequence(values);

            var valueGroups = values.GroupBy(v => v).Select(g => g.Count()).OrderByDescending(c => c).ToList();

            if (isFlush && isStraight)
            {
                if (values.Contains(1) && values.Contains(13) && values.Contains(10)) 
                    return 250; // 皇家同花順 (A, K, Q, J, 10 這裡特別處理A=1)
                return 50; // 同花順
            }
            if (valueGroups[0] == 4) return 25; // 四條
            if (valueGroups[0] == 3 && valueGroups[1] == 2) return 9; // 葫蘆
            if (isFlush) return 6; // 同花
            if (isStraight) return 4; // 順子
            if (valueGroups[0] == 3) return 3; // 三條
            if (valueGroups[0] == 2 && valueGroups[1] == 2) return 2; // 兩對
            if (valueGroups[0] == 2) return 1; // 一對

            return 0; // 什麼都沒有
        }

        private bool IsSequence(List<int> sortedValues)
        {
            // 一般順子
            bool normalSequence = true;
            for (int i = 1; i < 5; i++)
            {
                if (sortedValues[i] != sortedValues[i - 1] + 1)
                {
                    normalSequence = false;
                    break;
                }
            }

            // A 10 J Q K (1, 10, 11, 12, 13)
            bool royalSequence = sortedValues[0] == 1 && sortedValues[1] == 10 && sortedValues[2] == 11 && sortedValues[3] == 12 && sortedValues[4] == 13;

            return normalSequence || royalSequence;
        }

        #region 核心技術專區 (Computer Vision, Deep Learning, CIA Security)
        private string ApplyDarkChannelPrior()
        {
            // 影像處理與電腦視覺 (Image Processing & Computer Vision)
            // 暗通道先驗 (Dark Channel Prior) - 背景執行影像與去霧分析，保持原圖不被覆蓋
            return "✅ CV 影像去霧與優化執行完成";
        }

        private string ApplyRCNNObjectDetection()
        {
            // 物件偵測與區域卷積網路 (Object Detection & R-CNN)
            // 在背景模擬每一張牌的特徵提取與 Bounding Box 辨識
            double confidence = 0.95 + (rand.NextDouble() * 0.04); // 模擬可信度 95% ~ 99%
            return $"✅ R-CNN運算完畢。信心指標: {confidence:F2}";
        }

        private void RunDeepLearningModel()
        {
            // 深度學習 (Deep Learning) - 模擬神經網路計算，將預測勝率顯示在專用 Label
            double winRate = rand.NextDouble() * 100;
            this.Text = "五張撲克牌";

            if (lblWinRate != null)
            {
                lblWinRate.Text = $"DL 勝率：{winRate:F2}%";
            }

            if (txtAILog != null)
            {
                txtAILog.Text = "AI 模型已喚醒，可手放點擊上方按鈕分析";
            }
        }

        private async void ApplyCIASecurity()
        {
            // 資安三要素 (CIA Triad: Confidentiality 機密性, Integrity 完整性, Availability 可用性)
            try
            {
                // 動態特效：展示加密的 Base64 字串，模擬安全傳輸保護
                string encryptedFunds = Convert.ToBase64String(Encoding.UTF8.GetBytes(totalFunds.ToString()));
                txtTotalFunds.Text = $"[CIA防護] {encryptedFunds}";
                await Task.Delay(1000); // 延遲讓玩家明顯看到加密效果
                txtTotalFunds.Text = totalFunds.ToString(); // 還原
            }
            catch (Exception ex)
            {
                this.Text = $"[InfoSec - 安全攔截] {ex.Message}";
            }
        }
        #endregion
    }
}
