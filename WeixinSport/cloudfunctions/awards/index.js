// cloudfunctions/awards/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

const WEEKLY_AWARD_TYPES = [
  { id: 'calorie_star', name: '卡路里燃烧之星', icon: '🔥', desc: '本周累计消耗卡路里最多', field: 'calorie' },
  { id: 'duration_star', name: '运动时长之星', icon: '⏱️', desc: '本周累计运动时长最长', field: 'duration' },
  { id: 'frequency_star', name: '运动坚持之星', icon: '📅', desc: '本周打卡天数最多', field: 'frequency' },
  { id: 'diversity_star', name: '运动多面手', icon: '🎯', desc: '本周运动种类最丰富', field: 'diversity' },
  { id: 'improvement_star', name: '进步之星', icon: '📈', desc: '本周比上周进步最大', field: 'improvement' },
  { id: 'early_bird', name: '早起运动之星', icon: '🌅', desc: '本周最早开始运动', field: 'early' }
];

const MONTHLY_SPECIAL = [
  { id: 'monthly_persistent', name: '月度坚持之星', icon: '💪', desc: '本月打卡满20天', threshold: 20 },
  { id: 'monthly_improvement', name: '月度进步之星', icon: '🌟', desc: '本月比上月进步最大' }
];

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

const getWeekKey = (date) => {
  const day = date.getDay() || 7;
  const monday = new Date(date);
  monday.setDate(date.getDate() - day + 1);
  return `W${monday.getFullYear()}${String(monday.getMonth() + 1).padStart(2, '0')}${String(monday.getDate()).padStart(2, '0')}`;
};

const getWeekLabel = (start, end) => {
  return `${start.getFullYear()}.${start.getMonth() + 1}.${start.getDate()}-${end.getMonth() + 1}.${end.getDate()}`;
};

const getMonthKey = (date) => {
  return `M${date.getFullYear()}${String(date.getMonth() + 1).padStart(2, '0')}`;
};

const getMonthLabel = (start) => {
  return `${start.getFullYear()}年${start.getMonth() + 1}月`;
};

const findUser = async (username) => {
  if (!username) return null;
  const q = await db.collection('users').where({ username }).get();
  return q.data.length ? q.data[0] : null;
};

const getAllClassIds = async () => {
  const teachers = await db.collection('class_teachers').get();
  const classIds = [...new Set(teachers.data.map(t => t.classId).filter(Boolean))];
  return classIds;
};

const generateWeeklyForClass = async (classId, offset = 0) => {
  const { start, end } = getWeekRange(offset);
  const weekKey = getWeekKey(start);
  const periodLabel = getWeekLabel(start, end);

  const existing = await db.collection('awards').where({
    classId, periodKey: weekKey, periodType: 'weekly'
  }).get();
  if (existing.data.length > 0) {
    return { skipped: true, reason: 'already_exists', weekKey };
  }

  const members = (await db.collection('class_members').where({ classId, role: 'student' }).get()).data;
  const memberUsernames = members.map(m => m.username).filter(Boolean);
  if (!memberUsernames.length) {
    return { skipped: true, reason: 'no_students' };
  }

  const users = (await db.collection('users').where({ username: _.in(memberUsernames) }).get()).data;
  const uMap = {};
  users.forEach(uu => { uMap[uu.username] = uu; });

  const weekQ = await db.collection('checkins').where({
    username: _.in(memberUsernames),
    createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
  }).get();
  const weekList = weekQ.data;

  const prevRange = getWeekRange(offset - 1);
  const prevQ = await db.collection('checkins').where({
    username: _.in(memberUsernames),
    createTime: _.gte(prevRange.start.getTime()).and(_.lte(prevRange.end.getTime()))
  }).get();
  const prevList = prevQ.data;

  const stat = {};
  memberUsernames.forEach(uname => {
    stat[uname] = {
      calorie: 0, duration: 0, frequency: new Set(), diversity: new Set(),
      earliestTime: null, prevCalorie: 0
    };
  });
  weekList.forEach(c => {
    const s = stat[c.username];
    if (!s) return;
    s.calorie += (c.calorie || 0);
    s.duration += (c.duration || 0);
    s.frequency.add(c.dateStr);
    s.diversity.add(c.exerciseId);
    if (s.earliestTime === null || c.createTime < s.earliestTime) s.earliestTime = c.createTime;
  });
  prevList.forEach(c => {
    if (stat[c.username]) stat[c.username].prevCalorie += (c.calorie || 0);
  });

  const pickTop = (field, n = 3) => {
    return Object.entries(stat)
      .filter(([_, s]) => {
        if (field === 'diversity') return s.diversity.size > 0;
        if (field === 'early') return s.earliestTime !== null;
        return (s[field] || 0) > 0;
      })
      .map(([uname, s]) => {
        let val = 0;
        let valueText = '';
        if (field === 'calorie') { val = s.calorie; valueText = `${val}千卡`; }
        else if (field === 'duration') { val = s.duration; valueText = `${val}分钟`; }
        else if (field === 'frequency') { val = s.frequency.size; valueText = `打卡${val}天`; }
        else if (field === 'diversity') { val = s.diversity.size; valueText = `${val}种运动`; }
        else if (field === 'improvement') { val = s.calorie - s.prevCalorie; valueText = `+${val}千卡`; }
        else if (field === 'early') { val = -s.earliestTime; valueText = new Date(s.earliestTime).toLocaleString('zh-CN'); }
        return {
          username: uname,
          name: uMap[uname] ? uMap[uname].name : '',
          avatar: uMap[uname] ? uMap[uname].avatar : '',
          value: valueText,
          _val: val
        };
      })
      .sort((a, b) => b._val - a._val)
      .slice(0, n)
      .map((w, i) => ({ ...w, rank: i + 1 }));
  };

  const fieldMap = {
    calorie_star: 'calorie',
    duration_star: 'duration',
    frequency_star: 'frequency',
    diversity_star: 'diversity',
    improvement_star: 'improvement',
    early_bird: 'early'
  };

  const winnersList = [];
  WEEKLY_AWARD_TYPES.forEach(t => {
    const winners = pickTop(fieldMap[t.id]);
    if (winners.length) {
      winnersList.push({
        awardType: t.id,
        awardName: t.name,
        awardIcon: t.icon,
        desc: t.desc,
        winners
      });
    }
  });

  for (const w of winnersList) {
    await db.collection('awards').add({
      data: {
        classId,
        periodType: 'weekly',
        periodKey: weekKey,
        periodLabel,
        awardType: w.awardType,
        awardName: w.awardName,
        awardIcon: w.awardIcon,
        winners: w.winners,
        createTime: Date.now()
      }
    });
    for (const win of w.winners) {
      await db.collection('awards').add({
        data: {
          username: win.username,
          classId,
          periodType: 'weekly',
          periodKey: weekKey,
          periodLabel,
          awardType: w.awardType,
          awardName: w.awardName,
          awardIcon: w.awardIcon,
          rank: win.rank,
          valueText: win.value,
          createTime: Date.now()
        }
      });
    }
  }

  return { success: true, generated: winnersList.length, weekKey, periodLabel };
};

const generateMonthlyForClass = async (classId, offset = 0) => {
  const { start, end } = getMonthRange(offset);
  const monthKey = getMonthKey(start);
  const monthLabel = getMonthLabel(start);

  const existing = await db.collection('awards').where({
    classId, periodKey: monthKey, periodType: 'monthly_top'
  }).get();
  if (existing.data.length > 0) {
    return { skipped: true, reason: 'already_exists', monthKey };
  }

  const members = (await db.collection('class_members').where({ classId, role: 'student' }).get()).data;
  const memberUsernames = members.map(m => m.username).filter(Boolean);
  if (!memberUsernames.length) {
    return { skipped: true, reason: 'no_students' };
  }

  const users = (await db.collection('users').where({ username: _.in(memberUsernames) }).get()).data;
  const uMap = {};
  users.forEach(uu => { uMap[uu.username] = uu; });

  const monthQ = await db.collection('checkins').where({
    username: _.in(memberUsernames),
    createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
  }).get();
  const mList = monthQ.data;

  const prevRange = getMonthRange(offset - 1);
  const prevQ = await db.collection('checkins').where({
    username: _.in(memberUsernames),
    createTime: _.gte(prevRange.start.getTime()).and(_.lte(prevRange.end.getTime()))
  }).get();
  const prevList = prevQ.data;

  const stat = {};
  memberUsernames.forEach(uname => {
    stat[uname] = { calorie: 0, duration: 0, frequency: new Set(), diversity: new Set(), prevCalorie: 0 };
  });
  mList.forEach(c => {
    const s = stat[c.username];
    if (!s) return;
    s.calorie += (c.calorie || 0);
    s.duration += (c.duration || 0);
    s.frequency.add(c.dateStr);
    s.diversity.add(c.exerciseId);
  });
  prevList.forEach(c => {
    if (stat[c.username]) stat[c.username].prevCalorie += (c.calorie || 0);
  });

  const max = { calorie: 1, duration: 1, frequency: 1, diversity: 1 };
  Object.values(stat).forEach(s => {
    max.calorie = Math.max(max.calorie, s.calorie);
    max.duration = Math.max(max.duration, s.duration);
    max.frequency = Math.max(max.frequency, s.frequency.size);
    max.diversity = Math.max(max.diversity, s.diversity.size);
  });
  const scored = Object.entries(stat).map(([uname, s]) => {
    const score = (s.calorie / max.calorie) * 30 + (s.duration / max.duration) * 30 + (s.frequency.size / max.frequency) * 25 + (s.diversity.size / max.diversity) * 15;
    return {
      username: uname,
      name: uMap[uname] ? uMap[uname].name : '',
      avatar: uMap[uname] ? uMap[uname].avatar : '',
      calorie: s.calorie,
      duration: s.duration,
      frequency: s.frequency.size,
      diversity: s.diversity.size,
      score: Math.round(score * 10) / 10,
      improvement: s.calorie - s.prevCalorie
    };
  }).sort((a, b) => b.score - a.score);

  const top3 = scored.slice(0, 3).map((s, i) => ({ ...s, rank: i + 1 }));
  await db.collection('awards').add({
    data: {
      classId,
      periodType: 'monthly_top',
      periodKey: monthKey,
      periodLabel: monthLabel,
      winners: top3,
      createTime: Date.now()
    }
  });
  const topAwardName = ['月度运动冠军', '月度运动亚军', '月度运动季军'];
  const topIcon = ['🏆', '🥈', '🥉'];
  for (let i = 0; i < top3.length; i++) {
    await db.collection('awards').add({
      data: {
        username: top3[i].username,
        classId,
        periodType: 'monthly',
        periodKey: monthKey,
        periodLabel: monthLabel,
        awardType: ['monthly_champion', 'monthly_runner_up', 'monthly_third'][i],
        awardName: topAwardName[i],
        awardIcon: topIcon[i],
        rank: i + 1,
        valueText: `${top3[i].score}分`,
        createTime: Date.now()
      }
    });
  }

  const specialAwards = [];
  const persistent = scored.filter(s => s.frequency >= 20);
  if (persistent.length) {
    const winner = persistent.sort((a, b) => b.frequency - a.frequency)[0];
    specialAwards.push({
      awardType: 'monthly_persistent',
      awardName: '月度坚持之星',
      awardIcon: '💪',
      desc: '本月打卡满20天',
      winner: { name: winner.name, avatar: winner.avatar, username: winner.username },
      reason: `本月打卡${winner.frequency}天`
    });
    await db.collection('awards').add({
      data: {
        username: winner.username,
        classId,
        periodType: 'monthly',
        periodKey: monthKey,
        periodLabel: monthLabel,
        awardType: 'monthly_persistent',
        awardName: '月度坚持之星',
        awardIcon: '💪',
        rank: 0,
        valueText: `打卡${winner.frequency}天`,
        createTime: Date.now()
      }
    });
  }

  const improved = scored.filter(s => s.improvement > 0).sort((a, b) => b.improvement - a.improvement);
  if (improved.length) {
    const winner = improved[0];
    specialAwards.push({
      awardType: 'monthly_improvement',
      awardName: '月度进步之星',
      awardIcon: '🌟',
      desc: '本月比上月进步最大',
      winner: { name: winner.name, avatar: winner.avatar, username: winner.username },
      reason: `比上月多${winner.improvement}千卡`
    });
    await db.collection('awards').add({
      data: {
        username: winner.username,
        classId,
        periodType: 'monthly',
        periodKey: monthKey,
        periodLabel: monthLabel,
        awardType: 'monthly_improvement',
        awardName: '月度进步之星',
        awardIcon: '🌟',
        rank: 0,
        valueText: `+${winner.improvement}千卡`,
        createTime: Date.now()
      }
    });
  }

  for (const s of specialAwards) {
    await db.collection('awards').add({
      data: {
        classId,
        periodType: 'monthly_special',
        periodKey: monthKey,
        periodLabel: monthLabel,
        awardType: s.awardType,
        awardName: s.awardName,
        awardIcon: s.awardIcon,
        desc: s.desc,
        winner: s.winner,
        reason: s.reason,
        createTime: Date.now()
      }
    });
  }

  return { success: true, top3, specialAwards, monthKey, monthLabel };
};

exports.main = async (event, context) => {
  if (event.Type === 'Timer') {
    console.log('[Timer] 自动结算触发', JSON.stringify(event));

    let timerType = event.timerType;
    let offset = 0;

    if (!timerType) {
      const now = new Date();
      const dayOfWeek = now.getDay() || 7;
      if (now.getDate() === 1) {
        timerType = 'monthly';
        offset = -1;
      } else if (dayOfWeek === 7) {
        timerType = 'weekly';
      } else {
        timerType = 'weekly';
      }
    } else if (timerType === 'monthly' && new Date().getDate() === 1) {
      offset = -1;
    }

    console.log(`[Timer] type=${timerType}, offset=${offset}`);

    const classIds = await getAllClassIds();
    const results = [];

    for (const classId of classIds) {
      try {
        if (timerType === 'weekly') {
          const r = await generateWeeklyForClass(classId, offset);
          results.push({ classId, type: 'weekly', ...r });
        } else if (timerType === 'monthly') {
          const r = await generateMonthlyForClass(classId, offset);
          results.push({ classId, type: 'monthly', ...r });
        }
      } catch (e) {
        console.error(`[Timer] 处理班级 ${classId} 失败`, e);
        results.push({ classId, type: timerType, error: String(e.message || e) });
      }
    }

    const summary = {
      timerType,
      total: classIds.length,
      success: results.filter(r => r.success).length,
      skipped: results.filter(r => r.skipped).length,
      failed: results.filter(r => r.error).length,
      results
    };
    console.log('[Timer] 结算完成', JSON.stringify(summary));
    return { code: 0, message: '自动结算完成', data: summary };
  }

  const action = event.action;
  const username = event.username;

  try {
    const user = await findUser(username);
    if (!user) return { code: 1, message: '用户不存在' };

    if (action === 'weekly') {
      const offset = event.weekOffset || 0;
      const { start, end } = getWeekRange(offset);
      const weekKey = getWeekKey(start);
      const weekLabel = getWeekLabel(start, end);

      let classId = event.classId;
      let isTeacherOf = false;
      if (!classId) {
        if (user.role === 'teacher') {
          const t = await db.collection('class_teachers').where({ username }).limit(1).get();
          if (t.data.length) {
            classId = t.data[0].classId;
            isTeacherOf = true;
          }
        } else {
          const targetUsername = user.role === 'parent' ? user.childUsername : username;
          const m = await db.collection('class_members').where({ username: targetUsername }).limit(1).get();
          if (m.data.length) classId = m.data[0].classId;
        }
      } else if (user.role === 'teacher') {
        const t = await db.collection('class_teachers').where({ classId, username }).count();
        isTeacherOf = t.total > 0;
      }
      if (!classId) return { code: 0, data: { awards: [], weekLabel, canCalc: false } };

      const aQ = await db.collection('awards').where({ classId, periodKey: weekKey, periodType: 'weekly' }).get();
      const awardsMap = {};
      aQ.data.forEach(a => { awardsMap[a.awardType] = a; });

      const awards = WEEKLY_AWARD_TYPES.map(t => {
        const a = awardsMap[t.id];
        return {
          awardType: t.id,
          awardName: t.name,
          awardIcon: t.icon,
          desc: t.desc,
          weekKey,
          winners: a ? a.winners : []
        };
      });

      const canCalc = isTeacherOf;
      return { code: 0, data: { awards, weekLabel, canCalc } };
    }

    if (action === 'calcWeekly') {
      const offset = event.weekOffset || 0;

      if (user.role !== 'teacher') return { code: 1, message: '仅老师可生成' };

      let classId = event.classId;
      if (classId) {
        const t = await db.collection('class_teachers').where({ classId, username }).count();
        if (t.total === 0) return { code: 1, message: '无权操作该班级' };
      } else {
        const t = await db.collection('class_teachers').where({ username }).limit(1).get();
        if (!t.data.length) return { code: 1, message: '请先创建或加入班级' };
        classId = t.data[0].classId;
      }

      const result = await generateWeeklyForClass(classId, offset);
      if (result.skipped) {
        if (result.reason === 'already_exists') {
          return { code: 0, message: '本周奖项已生成，无需重复生成', data: result };
        }
        return { code: 1, message: result.reason === 'no_students' ? '班级还没有学生' : '无法生成', data: result };
      }
      return { code: 0, data: result };
    }

    if (action === 'monthly') {
      const offset = event.monthOffset || 0;
      const { start, end } = getMonthRange(offset);
      const monthKey = getMonthKey(start);
      const monthLabel = getMonthLabel(start);

      let classId = event.classId;
      let isTeacherOf = false;
      if (!classId) {
        if (user.role === 'teacher') {
          const t = await db.collection('class_teachers').where({ username }).limit(1).get();
          if (t.data.length) {
            classId = t.data[0].classId;
            isTeacherOf = true;
          }
        } else {
          const targetUsername = user.role === 'parent' ? user.childUsername : username;
          const m = await db.collection('class_members').where({ username: targetUsername }).limit(1).get();
          if (m.data.length) classId = m.data[0].classId;
        }
      } else if (user.role === 'teacher') {
        const t = await db.collection('class_teachers').where({ classId, username }).count();
        isTeacherOf = t.total > 0;
      }
      if (!classId) return { code: 0, data: { top3: [], specialAwards: [], monthLabel, canCalc: false } };

      const aQ = await db.collection('awards').where({ classId, periodKey: monthKey, periodType: 'monthly_top' }).get();
      const sQ = await db.collection('awards').where({ classId, periodKey: monthKey, periodType: 'monthly_special' }).get();
      const top3 = aQ.data[0] ? aQ.data[0].winners : [];
      const specialAwards = sQ.data.map(a => ({
        awardType: a.awardType,
        awardName: a.awardName,
        awardIcon: a.awardIcon,
        desc: a.desc,
        winner: a.winner || {}
      }));

      return { code: 0, data: { top3, specialAwards, monthLabel, canCalc: isTeacherOf } };
    }

    if (action === 'calcMonthly') {
      const offset = event.monthOffset || 0;

      if (user.role !== 'teacher') return { code: 1, message: '仅老师可生成' };

      let classId = event.classId;
      if (classId) {
        const t = await db.collection('class_teachers').where({ classId, username }).count();
        if (t.total === 0) return { code: 1, message: '无权操作该班级' };
      } else {
        const t = await db.collection('class_teachers').where({ username }).limit(1).get();
        if (!t.data.length) return { code: 1, message: '请先创建或加入班级' };
        classId = t.data[0].classId;
      }

      const result = await generateMonthlyForClass(classId, offset);
      if (result.skipped) {
        if (result.reason === 'already_exists') {
          return { code: 0, message: '本月奖项已生成，无需重复生成', data: result };
        }
        return { code: 1, message: result.reason === 'no_students' ? '班级还没有学生' : '无法生成', data: result };
      }
      return { code: 0, data: result };
    }

    if (action === 'mine') {
      let targetUsername = username;
      if (user.role === 'parent' && user.childUsername) targetUsername = user.childUsername;

      let q = db.collection('awards').where({ username: targetUsername });
      if (event.type) {
        q = db.collection('awards').where({ username: targetUsername, periodType: event.type === 'weekly' ? 'weekly' : 'monthly' });
      }
      const res = await q.orderBy('createTime', 'desc').limit(event.limit || 100).get();
      return { code: 0, data: res.data };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('awards cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};