# 1113354_Poker

這不只是一個撲克牌遊戲，這是一個整合了最新科技趨勢的數位娛樂平台。

## 三大核心技術

### 影像處理與電腦視覺 (Image Processing & Computer Vision)
- **暗通道先驗 (Dark Channel Prior)**：提供專屬互動按鈕手動執行去霧演算法分析，確保最高效的視覺優化處理，且保持牌面原樣完整呈現。
- **物件偵測 (Object Detection)**：提供專屬互動按鈕啟動區域卷積網路 (R-CNN) 手動分析每一張撲克牌影像特徵。透過互動式介面設計，分析日誌會即時回饋信心分數 (Confidence Score)。

### 響應式介面設計 (Responsive UI)
- 視窗支援最大化與動態拖曳縮放。透過智慧定錨 (Anchor) 與動態座標計算，確保撲克牌桌、按鈕與操作面板隨視窗大小流暢自適應排列。

### 深度學習 (Deep Learning)
- **智能勝率預測模型**：每一次發牌與結算，系統在背景使用模型進行推論，即時計算下一手的隱藏勝率，實現「AI 輔助對局」。

### 資安防護 (InfoSec / CIA Triad)
- **機密性 (Confidentiality)**：所有的資金變動都將進行 Base64/Hash 加密轉換。
- **完整性 (Integrity)**：確保資金在變動過程中未被惡意竄改。
- **可用性 (Availability)**：嚴謹的 Exception Handling 防呆機制，維持系統的可用性。

---
*Developed with C# WinForms*