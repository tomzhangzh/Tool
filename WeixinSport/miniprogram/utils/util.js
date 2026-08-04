// utils/util.js
const formatDate = (date, fmt = 'YYYY-MM-DD') => {
  if (!date) date = new Date();
  if (typeof date === 'number' || typeof date === 'string') date = new Date(date);
  const o = {
    'YYYY': date.getFullYear(),
    'MM': String(date.getMonth() + 1).padStart(2, '0'),
    'DD': String(date.getDate()).padStart(2, '0'),
    'HH': String(date.getHours()).padStart(2, '0'),
    'mm': String(date.getMinutes()).padStart(2, '0'),
    'ss': String(date.getSeconds()).padStart(2, '0')
  };
  return fmt.replace(/YYYY|MM|DD|HH|mm|ss/g, m => o[m]);
};

// 获取本周一至周日 [start, end]
const getWeekRange = (date) => {
  if (!date) date = new Date();
  if (typeof date === 'number' || typeof date === 'string') date = new Date(date);
  const day = date.getDay() || 7; // 周日转为7
  const monday = new Date(date);
  monday.setDate(date.getDate() - day + 1);
  monday.setHours(0, 0, 0, 0);
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  sunday.setHours(23, 59, 59, 999);
  return { start: monday, end: sunday };
};

// 获取本月 [start, end]
const getMonthRange = (date) => {
  if (!date) date = new Date();
  if (typeof date === 'number' || typeof date === 'string') date = new Date(date);
  const start = new Date(date.getFullYear(), date.getMonth(), 1);
  const end = new Date(date.getFullYear(), date.getMonth() + 1, 0, 23, 59, 59, 999);
  return { start, end };
};

// 计算卡路里: MET × 体重(kg) × 时长(小时)
// 体重默认30kg（小学生平均），实际可由用户填写
const calcCalorie = (met, durationMinutes, weight = 30) => {
  return Math.round(met * weight * (durationMinutes / 60));
};

// 格式化时长（分钟 -> x小时y分钟）
const formatDuration = (minutes) => {
  if (!minutes) return '0分钟';
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}分钟`;
  if (m === 0) return `${h}小时`;
  return `${h}小时${m}分钟`;
};

// 周/月标识
const getWeekKey = (date) => {
  const { start } = getWeekRange(date);
  return `W${start.getFullYear()}${String(start.getMonth() + 1).padStart(2, '0')}${String(start.getDate()).padStart(2, '0')}`;
};

const getMonthKey = (date) => {
  if (!date) date = new Date();
  if (typeof date === 'number' || typeof date === 'string') date = new Date(date);
  return `M${date.getFullYear()}${String(date.getMonth() + 1).padStart(2, '0')}`;
};

// 显示友好时间
const timeAgo = (ts) => {
  if (!ts) return '';
  const now = Date.now();
  const diff = now - ts;
  if (diff < 60000) return '刚刚';
  if (diff < 3600000) return `${Math.floor(diff / 60000)}分钟前`;
  if (diff < 86400000) return `${Math.floor(diff / 3600000)}小时前`;
  if (diff < 604800000) return `${Math.floor(diff / 86400000)}天前`;
  return formatDate(ts, 'MM-DD');
};

// 转换 Date 到云数据库时间对象（用于查询）
const dateToCommand = (date) => {
  return date.getTime();
};

module.exports = {
  formatDate,
  getWeekRange,
  getMonthRange,
  calcCalorie,
  formatDuration,
  getWeekKey,
  getMonthKey,
  timeAgo,
  dateToCommand
};
