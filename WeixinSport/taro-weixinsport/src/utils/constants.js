// src/utils/constants.js
// 角色定义
export const ROLE_LABEL = {
  teacher: '老师',
  student: '学生',
  parent: '家长'
};

// 运动项目类型 - 与小程序保持一致
export const EXERCISE_TYPES = [
  { id: 'running', name: '跑步', icon: '🏃', met: 8 },
  { id: 'walking', name: '快走', icon: '🚶', met: 4 },
  { id: 'cycling', name: '骑行', icon: '🚴', met: 6 },
  { id: 'jumping', name: '跳绳', icon: '🤸', met: 10 },
  { id: 'basketball', name: '篮球', icon: '🏀', met: 7 },
  { id: 'football', name: '足球', icon: '⚽', met: 7 },
  { id: 'badminton', name: '羽毛球', icon: '🏸', met: 5 },
  { id: 'swimming', name: '游泳', icon: '🏊', met: 8 },
  { id: 'dancing', name: '跳舞', icon: '💃', met: 5 },
  { id: 'other', name: '其他', icon: '💪', met: 4 }
];

// 周奖项类型
export const WEEKLY_AWARD_TYPES = [
  { id: 'calorie_star', name: '卡路里燃烧之星', icon: '🔥', desc: '本周累计消耗卡路里最多', field: 'totalCalorie' },
  { id: 'duration_star', name: '运动时长之星', icon: '⏱️', desc: '本周累计运动时长最长', field: 'totalDuration' },
  { id: 'frequency_star', name: '运动坚持之星', icon: '📅', desc: '本周打卡天数最多', field: 'checkinDays' },
  { id: 'diversity_star', name: '运动多面手', icon: '🎯', desc: '本周运动种类最丰富', field: 'exerciseTypes' },
  { id: 'improvement_star', name: '进步之星', icon: '📈', desc: '本周比上周进步最大', field: 'improvement' },
  { id: 'early_bird', name: '早起运动之星', icon: '🌅', desc: '本周最早开始运动', field: 'earlyCheckin' }
];

// 月度奖项
export const MONTHLY_AWARD_TYPES = [
  { id: 'monthly_champion', name: '月度运动冠军', icon: '🏆', desc: '本月综合得分第一' },
  { id: 'monthly_runner_up', name: '月度运动亚军', icon: '🥈', desc: '本月综合得分第二' },
  { id: 'monthly_third', name: '月度运动季军', icon: '🥉', desc: '本月综合得分第三' },
  { id: 'monthly_persistent', name: '月度坚持之星', icon: '💪', desc: '本月打卡满20天' },
  { id: 'monthly_improvement', name: '月度进步之星', icon: '🌟', desc: '本月进步显著' }
];

// 排名奖牌
export const MEDAL = ['🥇', '🥈', '🥉'];

// 综合得分权重（月度）
export const SCORE_WEIGHTS = {
  calorie: 0.3,
  duration: 0.3,
  frequency: 0.25,
  diversity: 0.15
};

// 计算卡路里
export const calcCalorie = (met, duration, weight) => {
  return Math.round((met * 3.5 * weight / 200) * duration);
};

// 时间格式化
export const timeAgo = (ts) => {
  if (!ts) return '';
  const now = Date.now();
  const diff = now - ts;
  if (diff < 60000) return '刚刚';
  if (diff < 3600000) return Math.floor(diff / 60000) + '分钟前';
  if (diff < 86400000) return Math.floor(diff / 3600000) + '小时前';
  if (diff < 604800000) return Math.floor(diff / 86400000) + '天前';
  const d = new Date(ts);
  const m = (d.getMonth() + 1).toString().padStart(2, '0');
  const day = d.getDate().toString().padStart(2, '0');
  return `${m}-${day}`;
};
