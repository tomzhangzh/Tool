// cloudfunctions/stats/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;
const $ = db.command.aggregate;

const getWeekRange = (offset = 0) => {
  const now = new Date();
  const day = now.getDay() || 7;
  const monday = new Date(now);
  monday.setDate(now.getDate() - day + 1 + offset * 7);
  monday.setHours(0, 0, 0, 0);
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  sunday.setHours(23, 59, 59, 999);
  return { start: monday, end: sunday };
};

const getMonthRange = (offset = 0) => {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  const end = new Date(now.getFullYear(), now.getMonth() + offset + 1, 0, 23, 59, 59, 999);
  return { start, end };
};

// 获取用户 ID：兼容小程序（OPENID）和 H5（event._uid）环境
const getUserId = (event) => {
  const wxCtx = cloud.getWXContext();
  if (wxCtx.OPENID) return wxCtx.OPENID;
  if (event._uid) return event._uid;
  return null;
};

exports.main = async (event, context) => {
  const openid = getUserId(event);
  if (!openid) return { code: 1, message: '未获取到用户身份' };
  const action = event.action;

  try {
    if (action === 'weekly') {
      const offset = event.weekOffset || 0;
      const { start, end } = getWeekRange(offset);
      // 目标 openid：家长看孩子
      let targetOpenid = openid;
      const userQ = await db.collection('users').where({ openid }).get();
      if (userQ.data[0] && userQ.data[0].role === 'parent' && userQ.data[0].childOpenid) {
        targetOpenid = userQ.data[0].childOpenid;
      }
      // 周内打卡
      const q = await db.collection('checkins').where({
        openid: targetOpenid,
        createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
      }).get();
      const list = q.data;

      // 周标识
      const weekLabel = `${start.getMonth() + 1}/${start.getDate()}-${end.getMonth() + 1}/${end.getDate()}`;

      if (!list.length) {
        return { code: 0, data: { totalDuration: 0, totalCalorie: 0, checkinDays: 0, exerciseTypes: 0, totalCount: 0, dailyData: [], exerciseDistribution: [], weekLabel } };
      }

      const totalDuration = list.reduce((s, c) => s + (c.duration || 0), 0);
      const totalCalorie = list.reduce((s, c) => s + (c.calorie || 0), 0);
      const daySet = new Set();
      const exMap = {};
      list.forEach(c => {
        daySet.add(c.dateStr);
        if (!exMap[c.exerciseId]) {
          exMap[c.exerciseId] = { exerciseId: c.exerciseId, name: c.exerciseName, icon: c.exerciseIcon, duration: 0 };
        }
        exMap[c.exerciseId].duration += (c.duration || 0);
      });

      // 按日聚合
      const dayMap = {};
      list.forEach(c => {
        if (!dayMap[c.dateStr]) {
          const d = new Date(c.createTime);
          dayMap[c.dateStr] = { date: c.dateStr, label: `${d.getMonth() + 1}/${d.getDate()}`, duration: 0, calorie: 0 };
        }
        dayMap[c.dateStr].duration += (c.duration || 0);
        dayMap[c.dateStr].calorie += (c.calorie || 0);
      });
      // 补齐本周每一天
      const dailyData = [];
      for (let i = 0; i < 7; i++) {
        const d = new Date(start);
        d.setDate(start.getDate() + i);
        const dateStr = formatDate(d);
        dailyData.push(dayMap[dateStr] || { date: dateStr, label: `${d.getMonth() + 1}/${d.getDate()}`, duration: 0, calorie: 0 });
      }

      const exDist = Object.values(exMap).sort((a, b) => b.duration - a.duration);
      const maxExDur = exDist.reduce((m, e) => Math.max(m, e.duration), 1) || 1;
      exDist.forEach(e => { e.percent = Math.round(e.duration * 100 / maxExDur); });

      return {
        code: 0,
        data: {
          totalDuration, totalCalorie,
          checkinDays: daySet.size,
          exerciseTypes: Object.keys(exMap).length,
          totalCount: list.length,
          dailyData,
          exerciseDistribution: exDist,
          weekLabel
        }
      };
    }

    if (action === 'monthly') {
      const offset = event.monthOffset || 0;
      const { start, end } = getMonthRange(offset);
      let targetOpenid = openid;
      const userQ = await db.collection('users').where({ openid }).get();
      if (userQ.data[0] && userQ.data[0].role === 'parent' && userQ.data[0].childOpenid) {
        targetOpenid = userQ.data[0].childOpenid;
      }
      const q = await db.collection('checkins').where({
        openid: targetOpenid,
        createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
      }).get();
      const list = q.data;
      const monthLabel = `${start.getFullYear()}年${start.getMonth() + 1}月`;

      if (!list.length) {
        return { code: 0, data: { totalDuration: 0, totalCalorie: 0, checkinDays: 0, exerciseTypes: 0, totalCount: 0, dailyData: [], exerciseDistribution: [], monthLabel } };
      }

      const totalDuration = list.reduce((s, c) => s + (c.duration || 0), 0);
      const totalCalorie = list.reduce((s, c) => s + (c.calorie || 0), 0);
      const daySet = new Set();
      const exMap = {};
      list.forEach(c => {
        daySet.add(c.dateStr);
        if (!exMap[c.exerciseId]) exMap[c.exerciseId] = { exerciseId: c.exerciseId, name: c.exerciseName, icon: c.exerciseIcon, duration: 0 };
        exMap[c.exerciseId].duration += (c.duration || 0);
      });

      // 按日聚合
      const dayMap = {};
      list.forEach(c => {
        if (!dayMap[c.dateStr]) {
          const d = new Date(c.createTime);
          dayMap[c.dateStr] = { date: c.dateStr, label: `${d.getDate()}`, duration: 0, calorie: 0 };
        }
        dayMap[c.dateStr].duration += (c.duration || 0);
        dayMap[c.dateStr].calorie += (c.calorie || 0);
      });
      const dailyData = Object.values(dayMap).sort((a, b) => a.date.localeCompare(b.date));

      const exDist = Object.values(exMap).sort((a, b) => b.duration - a.duration);
      const maxExDur = exDist.reduce((m, e) => Math.max(m, e.duration), 1) || 1;
      exDist.forEach(e => { e.percent = Math.round(e.duration * 100 / maxExDur); });

      return {
        code: 0,
        data: {
          totalDuration, totalCalorie,
          checkinDays: daySet.size,
          exerciseTypes: Object.keys(exMap).length,
          totalCount: list.length,
          dailyData,
          exerciseDistribution: exDist,
          monthLabel
        }
      };
    }

    if (action === 'ranking') {
      const { type = 'week', metric = 'calorie' } = event;
      let range = type === 'week' ? getWeekRange(0) : getMonthRange(0);

      // 取我的班级：老师→第一个班级（简化），学生/家长→其班级
      const userQ = await db.collection('users').where({ openid }).get();
      const u = userQ.data[0];
      let classId = event.classId;
      if (!classId) {
        if (u.role === 'teacher') {
          const t = await db.collection('class_teachers').where({ openid }).limit(1).get();
          if (t.data.length) classId = t.data[0].classId;
        } else {
          const targetOpenid = u.role === 'parent' ? u.childOpenid : openid;
          const m = await db.collection('class_members').where({ openid: targetOpenid }).limit(1).get();
          if (m.data.length) classId = m.data[0].classId;
        }
      }
      if (!classId) return { code: 0, data: { ranking: [], myRank: null } };

      const members = (await db.collection('class_members').where({ classId, role: 'student' }).get()).data;
      const openids = members.map(m => m.openid);
      if (!openids.length) return { code: 0, data: { ranking: [], myRank: null } };

      const users = (await db.collection('users').where({ openid: _.in(openids) }).get()).data;
      const uMap = {};
      users.forEach(u => uMap[u.openid] = u);

      const q = await db.collection('checkins').where({
        openid: _.in(openids),
        createTime: _.gte(range.start.getTime()).and(_.lte(range.end.getTime()))
      }).get();

      // 聚合：每个学生 metric 值
      const stat = {};
      q.data.forEach(c => {
        if (!stat[c.openid]) stat[c.openid] = { calorie: 0, duration: 0, frequency: new Set(), name: '', avatar: '' };
        stat[c.openid].calorie += (c.calorie || 0);
        stat[c.openid].duration += (c.duration || 0);
        stat[c.openid].frequency.add(c.dateStr);
        stat[c.openid].name = uMap[c.openid] ? uMap[c.openid].name : '';
        stat[c.openid].avatar = uMap[c.openid] ? uMap[c.openid].avatar : '';
      });

      const targetOpenid = u.role === 'parent' ? u.childOpenid : openid;

      const arr = Object.entries(stat).map(([oid, v]) => ({
        openid: oid,
        name: v.name,
        avatar: v.avatar,
        calorie: v.calorie,
        duration: v.duration,
        frequency: v.frequency.size
      })).sort((a, b) => (b[metric] || 0) - (a[metric] || 0));

      const metricMap = { calorie: '千卡', duration: '分钟', frequency: '天' };
      const ranking = arr.map((r, i) => ({
        ...r,
        isMe: r.openid === targetOpenid,
        value: `${r[metric]} ${metricMap[metric]}`
      }));

      const myIdx = ranking.findIndex(r => r.isMe);
      const myRank = myIdx >= 0 ? { rank: myIdx + 1, total: ranking.length, value: ranking[myIdx].value } : null;

      return { code: 0, data: { ranking, myRank } };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('stats cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};

function formatDate(date) {
  const o = {
    YYYY: date.getFullYear(),
    MM: String(date.getMonth() + 1).padStart(2, '0'),
    DD: String(date.getDate()).padStart(2, '0')
  };
  return 'YYYY-MM-DD'.replace(/YYYY|MM|DD/g, m => o[m]);
}
