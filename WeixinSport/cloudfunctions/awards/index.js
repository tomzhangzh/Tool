// cloudfunctions/awards/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

// 周奖项类型
const WEEKLY_AWARD_TYPES = [
  { id: 'calorie_star', name: '卡路里燃烧之星', icon: '🔥', desc: '本周累计消耗卡路里最多', field: 'calorie' },
  { id: 'duration_star', name: '运动时长之星', icon: '⏱️', desc: '本周累计运动时长最长', field: 'duration' },
  { id: 'frequency_star', name: '运动坚持之星', icon: '📅', desc: '本周打卡天数最多', field: 'frequency' },
  { id: 'diversity_star', name: '运动多面手', icon: '🎯', desc: '本周运动种类最丰富', field: 'diversity' },
  { id: 'improvement_star', name: '进步之星', icon: '📈', desc: '本周比上周进步最大', field: 'improvement' },
  { id: 'early_bird', name: '早起运动之星', icon: '🌅', desc: '本周最早开始运动', field: 'early' }
];

// 月度专项奖项
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

exports.main = async (event, context) => {
  const wxCtx = cloud.getWXContext();
  const openid = wxCtx.OPENID;
  const action = event.action;

  try {
    // 查询周评选结果
    if (action === 'weekly') {
      const offset = event.weekOffset || 0;
      const { start, end } = getWeekRange(offset);
      const weekKey = getWeekKey(start);
      const weekLabel = getWeekLabel(start, end);

      // 找我的班级
      const userQ = await db.collection('users').where({ openid }).get();
      const u = userQ.data[0];
      if (!u) return { code: 0, data: { awards: [], weekLabel, canCalc: false } };

      let classId = event.classId;
      if (!classId) {
        if (u.role === 'teacher') {
          const cls = await db.collection('classes').where({ teacherOpenid: openid }).limit(1).get();
          if (cls.data.length) classId = cls.data[0]._id;
        } else {
          const targetOpenid = u.role === 'parent' ? u.childOpenid : openid;
          const m = await db.collection('class_members').where({ openid: targetOpenid }).limit(1).get();
          if (m.data.length) classId = m.data[0].classId;
        }
      }
      if (!classId) return { code: 0, data: { awards: [], weekLabel, canCalc: false } };

      // 查已生成的奖项
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

      // 老师可手动计算（本周已结束或在周日及以后）
      const canCalc = u.role === 'teacher';

      return { code: 0, data: { awards, weekLabel, canCalc } };
    }

    // 老师手动生成本周评选
    if (action === 'calcWeekly') {
      const offset = event.weekOffset || 0;
      const { start, end } = getWeekRange(offset);
      const weekKey = getWeekKey(start);

      const userQ = await db.collection('users').where({ openid }).get();
      const u = userQ.data[0];
      if (!u || u.role !== 'teacher') return { code: 1, message: '仅老师可生成' };
      const cls = await db.collection('classes').where({ teacherOpenid: openid }).limit(1).get();
      if (!cls.data.length) return { code: 1, message: '请先创建班级' };
      const classId = cls.data[0]._id;

      // 班级学生
      const members = (await db.collection('class_members').where({ classId, role: 'student' }).get()).data;
      const openids = members.map(m => m.openid);
      if (!openids.length) return { code: 1, message: '班级还没有学生' };

      const users = (await db.collection('users').where({ openid: _.in(openids) }).get()).data;
      const uMap = {};
      users.forEach(uu => { uMap[uu.openid] = uu; });

      // 本周打卡
      const weekQ = await db.collection('checkins').where({
        openid: _.in(openids),
        createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
      }).get();
      const weekList = weekQ.data;

      // 上周打卡（用于进步奖）
      const prevRange = getWeekRange(offset - 1);
      const prevQ = await db.collection('checkins').where({
        openid: _.in(openids),
        createTime: _.gte(prevRange.start.getTime()).and(_.lte(prevRange.end.getTime()))
      }).get();
      const prevList = prevQ.data;

      // 聚合每个学生
      const stat = {};
      openids.forEach(oid => {
        stat[oid] = {
          calorie: 0, duration: 0, frequency: new Set(), diversity: new Set(),
          earliestTime: null,
          prevCalorie: 0
        };
      });
      weekList.forEach(c => {
        const s = stat[c.openid];
        if (!s) return;
        s.calorie += (c.calorie || 0);
        s.duration += (c.duration || 0);
        s.frequency.add(c.dateStr);
        s.diversity.add(c.exerciseId);
        if (s.earliestTime === null || c.createTime < s.earliestTime) s.earliestTime = c.createTime;
      });
      prevList.forEach(c => {
        if (stat[c.openid]) stat[c.openid].prevCalorie += (c.calorie || 0);
      });

      // 生成各奖项 top3
      const pickTop = (field, n = 3) => {
        return Object.entries(stat)
          .filter(([_, s]) => {
            if (field === 'diversity') return s.diversity.size > 0;
            if (field === 'early') return s.earliestTime !== null;
            return (s[field] || 0) > 0;
          })
          .map(([oid, s]) => {
            let val = 0;
            let valueText = '';
            if (field === 'calorie') { val = s.calorie; valueText = `${val}千卡`; }
            else if (field === 'duration') { val = s.duration; valueText = `${val}分钟`; }
            else if (field === 'frequency') { val = s.frequency.size; valueText = `打卡${val}天`; }
            else if (field === 'diversity') { val = s.diversity.size; valueText = `${val}种运动`; }
            else if (field === 'improvement') { val = s.calorie - s.prevCalorie; valueText = `+${val}千卡`; }
            else if (field === 'early') { val = -s.earliestTime; valueText = new Date(s.earliestTime).toLocaleString('zh-CN'); }
            return {
              openid: oid,
              name: uMap[oid] ? uMap[oid].name : '',
              avatar: uMap[oid] ? uMap[oid].avatar : '',
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

      // 清理旧的本周奖项
      await db.collection('awards').where({ classId, periodKey: weekKey, periodType: 'weekly' }).remove();

      // 写入新奖项
      const periodLabel = getWeekLabel(start, end);
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

      // 批量写入数据库
      for (const w of winnersList) {
        // 班级奖项
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
        // 给每个获奖者个人奖项副本（用于个人奖项墙）
        for (const win of w.winners) {
          await db.collection('awards').add({
            data: {
              openid: win.openid,
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

      return { code: 0, data: { generated: winnersList.length } };
    }

    // 查询月度明星
    if (action === 'monthly') {
      const offset = event.monthOffset || 0;
      const { start, end } = getMonthRange(offset);
      const monthKey = `M${start.getFullYear()}${String(start.getMonth() + 1).padStart(2, '0')}`;
      const monthLabel = `${start.getFullYear()}年${start.getMonth() + 1}月`;

      const userQ = await db.collection('users').where({ openid }).get();
      const u = userQ.data[0];
      if (!u) return { code: 0, data: { top3: [], specialAwards: [], monthLabel, canCalc: false } };

      let classId = event.classId;
      if (!classId) {
        if (u.role === 'teacher') {
          const cls = await db.collection('classes').where({ teacherOpenid: openid }).limit(1).get();
          if (cls.data.length) classId = cls.data[0]._id;
        } else {
          const targetOpenid = u.role === 'parent' ? u.childOpenid : openid;
          const m = await db.collection('class_members').where({ openid: targetOpenid }).limit(1).get();
          if (m.data.length) classId = m.data[0].classId;
        }
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

      return { code: 0, data: { top3, specialAwards, monthLabel, canCalc: u.role === 'teacher' } };
    }

    // 老师生成月度明星
    if (action === 'calcMonthly') {
      const offset = event.monthOffset || 0;
      const { start, end } = getMonthRange(offset);
      const monthKey = `M${start.getFullYear()}${String(start.getMonth() + 1).padStart(2, '0')}`;
      const monthLabel = `${start.getFullYear()}年${start.getMonth() + 1}月`;

      const userQ = await db.collection('users').where({ openid }).get();
      const u = userQ.data[0];
      if (!u || u.role !== 'teacher') return { code: 1, message: '仅老师可生成' };
      const cls = await db.collection('classes').where({ teacherOpenid: openid }).limit(1).get();
      if (!cls.data.length) return { code: 1, message: '请先创建班级' };
      const classId = cls.data[0]._id;

      const members = (await db.collection('class_members').where({ classId, role: 'student' }).get()).data;
      const openids = members.map(m => m.openid);
      if (!openids.length) return { code: 1, message: '班级还没有学生' };

      const users = (await db.collection('users').where({ openid: _.in(openids) }).get()).data;
      const uMap = {};
      users.forEach(uu => { uMap[uu.openid] = uu; });

      const monthQ = await db.collection('checkins').where({
        openid: _.in(openids),
        createTime: _.gte(start.getTime()).and(_.lte(end.getTime()))
      }).get();
      const mList = monthQ.data;

      // 上月数据（计算进步）
      const prevRange = getMonthRange(offset - 1);
      const prevQ = await db.collection('checkins').where({
        openid: _.in(openids),
        createTime: _.gte(prevRange.start.getTime()).and(_.lte(prevRange.end.getTime()))
      }).get();
      const prevList = prevQ.data;

      const stat = {};
      openids.forEach(oid => {
        stat[oid] = { calorie: 0, duration: 0, frequency: new Set(), diversity: new Set(), prevCalorie: 0 };
      });
      mList.forEach(c => {
        const s = stat[c.openid];
        if (!s) return;
        s.calorie += (c.calorie || 0);
        s.duration += (c.duration || 0);
        s.frequency.add(c.dateStr);
        s.diversity.add(c.exerciseId);
      });
      prevList.forEach(c => {
        if (stat[c.openid]) stat[c.openid].prevCalorie += (c.calorie || 0);
      });

      // 综合得分（标准化后加权）
      const max = { calorie: 1, duration: 1, frequency: 1, diversity: 1 };
      Object.values(stat).forEach(s => {
        max.calorie = Math.max(max.calorie, s.calorie);
        max.duration = Math.max(max.duration, s.duration);
        max.frequency = Math.max(max.frequency, s.frequency.size);
        max.diversity = Math.max(max.diversity, s.diversity.size);
      });
      const scored = Object.entries(stat).map(([oid, s]) => {
        const score = (s.calorie / max.calorie) * 30 + (s.duration / max.duration) * 30 + (s.frequency.size / max.frequency) * 25 + (s.diversity.size / max.diversity) * 15;
        return {
          openid: oid,
          name: uMap[oid] ? uMap[oid].name : '',
          avatar: uMap[oid] ? uMap[oid].avatar : '',
          calorie: s.calorie,
          duration: s.duration,
          frequency: s.frequency.size,
          diversity: s.diversity.size,
          score: Math.round(score * 10) / 10,
          improvement: s.calorie - s.prevCalorie
        };
      }).sort((a, b) => b.score - a.score);

      // 清理旧数据
      await db.collection('awards').where({ classId, periodKey: monthKey }).remove();

      // 写入 top3
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
      // 给 top3 个人奖项副本
      const topAwardName = ['月度运动冠军', '月度运动亚军', '月度运动季军'];
      const topIcon = ['🏆', '🥈', '🥉'];
      for (let i = 0; i < top3.length; i++) {
        await db.collection('awards').add({
          data: {
            openid: top3[i].openid,
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

      // 专项奖：坚持之星
      const specialAwards = [];
      const persistent = scored.filter(s => s.frequency >= 20);
      if (persistent.length) {
        const winner = persistent.sort((a, b) => b.frequency - a.frequency)[0];
        specialAwards.push({
          awardType: 'monthly_persistent',
          awardName: '月度坚持之星',
          awardIcon: '💪',
          desc: '本月打卡满20天',
          winner: { name: winner.name, avatar: winner.avatar, openid: winner.openid },
          reason: `本月打卡${winner.frequency}天`
        });
        await db.collection('awards').add({
          data: {
            openid: winner.openid,
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

      // 专项奖：进步之星
      const improved = scored.filter(s => s.improvement > 0).sort((a, b) => b.improvement - a.improvement);
      if (improved.length) {
        const winner = improved[0];
        specialAwards.push({
          awardType: 'monthly_improvement',
          awardName: '月度进步之星',
          awardIcon: '🌟',
          desc: '本月比上月进步最大',
          winner: { name: winner.name, avatar: winner.avatar, openid: winner.openid },
          reason: `比上月多${winner.improvement}千卡`
        });
        await db.collection('awards').add({
          data: {
            openid: winner.openid,
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

      // 写入班级专项奖项记录
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

      return { code: 0, data: { top3, specialAwards } };
    }

    // 我的奖项墙
    if (action === 'mine') {
      const userQ = await db.collection('users').where({ openid }).get();
      const u = userQ.data[0];
      if (!u) return { code: 0, data: [] };

      let targetOpenid = openid;
      if (u.role === 'parent' && u.childOpenid) targetOpenid = u.childOpenid;

      let q = db.collection('awards').where({ openid: targetOpenid });
      if (event.type) {
        q = db.collection('awards').where({ openid: targetOpenid, periodType: event.type === 'weekly' ? 'weekly' : 'monthly' });
      }
      const res = await q.orderBy('createTime', 'desc').limit(event.limit || 100).get();
      // 数据可能包含班级维度奖项（无 openid 的不会查到，因为 where openid）；
      // 这里直接返回所有个人维度奖项
      return { code: 0, data: res.data };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('awards cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};
