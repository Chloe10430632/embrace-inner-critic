// Product vocabulary informed by VIA, OECD SSES and O*NET; not an assessment scale.
// Keep labels stable: saved entries store the user's original strings.
export const strengthTraitGroups = [
  { id: 'learning', name: '探索與學習', description: '我如何理解新事物，累積經驗？', tags: ['好奇心', '學習力', '觀察力', '專注力', '開放心態', '反思能力', '知識整合'] },
  { id: 'thinking', name: '思考與解題', description: '我如何整理資訊，找到可行的方法？', tags: ['分析能力', '邏輯推理', '判斷力', '批判思考', '全局觀', '問題解決', '決策能力', '數量推理'] },
  { id: 'expression', name: '創意與表達', description: '我如何創作，讓想法被理解？', tags: ['創造力', '想像力', '美感', '寫作能力', '表達能力', '說故事能力', '幽默感', '藝術表現'] },
  { id: 'action', name: '行動與執行', description: '我如何開始、安排與完成一件事？', tags: ['勇氣', '主動性', '毅力', '自制力', '責任感', '整理能力', '規劃能力', '時間管理', '細膩'] },
  { id: 'relationships', name: '人際與合作', description: '我如何理解別人，一起把事情做好？', tags: ['同理心', '傾聽能力', '友善', '尊重他人', '合作能力', '協調能力', '領導能力', '教導能力'] },
  { id: 'values', name: '品格與價值', description: '我如何在選擇與行動中實踐重視的事？', tags: ['誠實', '守信', '公平', '謙遜', '感恩', '審慎', '希望感', '意義感'] },
  { id: 'emotions', name: '情緒與調適', description: '我如何理解感受，面對變化與挫折？', tags: ['自我覺察', '情緒辨識', '情緒調節', '耐心', '適應力', '挫折調適', '壓力調適'] },
  { id: 'care', name: '自我照顧與界線', description: '我如何照顧需要，保留自己的空間？', tags: ['自我照顧', '自我接納', '適時休息', '界線表達', '主動求助', '需求表達', '健康習慣'] },
  { id: 'practical', name: '生活與實作', description: '我如何運用身體、工具與生活技能？', tags: ['數位工具運用', '技術實作', '手作能力', '空間感', '身體協調', '運動技能', '生活料理', '財務管理'] }
] as const
