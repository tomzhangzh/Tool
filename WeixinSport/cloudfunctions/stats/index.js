// cloudfunctions/stats/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

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

// 按 username 查找用户
const findUser = async (username) => {
  if (!username) return null;
  const q = await db.collection('users').where({ username }).get();
  return q.data.length ? q.data[0] : null;
};

// 按 username 获取用户的班级 ID
const getUserClassIds = async (username) => {
  if (!username) return [];
  const q = await db.collection('class_members').where({ username }).get();
  return q.data.map(m => m.classId);
};

exports.main = async (event, context) => {
  const action = event.action;
  const username = event.username;

  try {
    // 查找当前用户
    const user = await findUser(username);
    if (!user) return { code: 1, message: '用户不存在' };

    if (action === 'weekly') {
      const offset = event.weekOffset || 0;
      const { start, end } = getWeekRange(offset);
      
      // 目标 username：家长看孩子
      let targetUsername = username;
      if (user.role === 'parent' && user.childUsername) {
        targetUsername = user.childUsername;
      }
      
      const q = await db.collection('checkins').where({
        username: targetUsername,
        createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
      }).get();
      const list = q.data;

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

      const dayMap = {};
      list.forEach(c => {
        if (!dayMap[c.dateStr]) {
          const d = new Date(c.createTime);
          dayMap[c.dateStr] = { date: c.dateStr, label: `${d.getMonth() + 1}/${d.getDate()}`, duration: 0, calorie: 0 };
        }
        dayMap[c.dateStr].duration += (c.duration || 0);
        dayMap[c.dateStr].calorie += (c.calorie || 0);
      });
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
      
      let targetUsername = username;
      if (user.role === 'parent' && user.childUsername) {
        targetUsername = user.childUsername;
      }
      
      const q = await db.collection('checkins').where({
        username: targetUsername,
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
      const range = type === 'week' ? getWeekRange(0) : getMonthRange(0);

      // 获取班级 ID
      let classId = event.classId;
      if (!classId) {
        if (user.role === 'teacher') {
          const t = await db.collection('class_teachers').where({ username }).limit(1).get();
          if (t.data.length) classId = t.data[0].classId;
        } else {
          const targetUsername = user.role === 'parent' ? user.childUsername : username;
          const m = await db.collection('class_members').where({ username: targetUsername }).limit(1).get();
          if (m.data.length) classId = m.data[0].classId;
        }
      }
      if (!classId) return { code: 0, data: { ranking: [], myRank: null } };

      // 获取班级成员
      const members = (await db.collection('class_members').where({ classId, role: 'student' }).get()).data;
      const memberUsernames = members.map(m => m.username).filter(Boolean);
      if (!memberUsernames.length) return { code: 0, data: { ranking: [], myRank: null } };

      // 获取用户信息
      const users = (await db.collection('users').where({ username: _.in(memberUsernames) }).get()).data;
      const uMap = {};
      users.forEach(u => { uMap[u.username] = u; });

      // 查询打卡记录
      const q = await db.collection('checkins').where({
        username: _.in(memberUsernames),
        createTime: _.gte(range.start.getTime()).and(_.lte(range.end.getTime()))
      }).get();

      // 聚合：每个学生 metric 值
      const stat = {};
      q.data.forEach(c => {
        if (!stat[c.username]) stat[c.username] = { calorie: 0, duration: 0, frequency: new Set(), name: '', avatar: '' };
        stat[c.username].calorie += (c.calorie || 0);
        stat[c.username].duration += (c.duration || 0);
        stat[c.username].frequency.add(c.dateStr);
        stat[c.username].name = uMap[c.username] ? uMap[c.username].name : '';
        stat[c.username].avatar = uMap[c.username] ? uMap[c.username].avatar : '';
      });

      const targetUsername = user.role === 'parent' ? user.childUsername : username;

      const arr = Object.entries(stat).map(([uname, v]) => ({
        username: uname,
        name: v.name,
        avatar: v.avatar,
        calorie: v.calorie,
        duration: v.duration,
        frequency: v.frequency.size
      })).sort((a, b) => (b[metric] || 0) - (a[metric] || 0));

      const metricMap = { calorie: '千卡', duration: '分钟', frequency: '天' };
      const ranking = arr.map((r) => ({
        ...r,
        isMe: r.username === targetUsername,
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
