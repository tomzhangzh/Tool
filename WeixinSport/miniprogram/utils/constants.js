// utils/constants.js
// 运动项目类型
const EXERCISE_TYPES = [
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

// 角色定义
const ROLES = {
  TEACHER: 'teacher',
  STUDENT: 'student',
  PARENT: 'parent'
};

const ROLE_LABEL = {
  teacher: '老师',
  student: '学生',
  parent: '家长'
};

// 周奖项类型 - 多元化奖项让更多孩子获得成就感
const WEEKLY_AWARD_TYPES = [
  { id: 'calorie_star', name: '卡路里燃烧之星', icon: '🔥', desc: '本周累计消耗卡路里最多', field: 'totalCalorie' },
  { id: 'duration_star', name: '运动时长之星', icon: '⏱️', desc: '本周累计运动时长最长', field: 'totalDuration' },
  { id: 'frequency_star', name: '运动坚持之星', icon: '📅', desc: '本周打卡天数最多', field: 'checkinDays' },
  { id: 'diversity_star', name: '运动多面手', icon: '🎯', desc: '本周运动种类最丰富', field: 'exerciseTypes' },
  { id: 'improvement_star', name: '进步之星', icon: '📈', desc: '本周比上周进步最大', field: 'improvement' },
  { id: 'early_bird', name: '早起运动之星', icon: '🌅', desc: '本周最早开始运动', field: 'earlyCheckin' }
];

// 月度奖项
const MONTHLY_AWARD_TYPES = [
  { id: 'monthly_champion', name: '月度运动冠军', icon: '🏆', desc: '本月综合得分第一' },
  { id: 'monthly_runner_up', name: '月度运动亚军', icon: '🥈', desc: '本月综合得分第二' },
  { id: 'monthly_third', name: '月度运动季军', icon: '🥉', desc: '本月综合得分第三' },
  { id: 'monthly_persistent', name: '月度坚持之星', icon: '💪', desc: '本月打卡满20天' },
  { id: 'monthly_improvement', name: '月度进步之星', icon: '🌟', desc: '本月进步显著' }
];

// 排名奖牌
const MEDAL = ['🥇', '🥈', '🥉'];

// 综合得分权重（月度）
const SCORE_WEIGHTS = {
  calorie: 0.3,
  duration: 0.3,
  frequency: 0.25,
  diversity: 0.15
};

module.exports = {
  EXERCISE_TYPES,
  ROLES,
  ROLE_LABEL,
  WEEKLY_AWARD_TYPES,
  MONTHLY_AWARD_TYPES,
  MEDAL,
  SCORE_WEIGHTS
};
