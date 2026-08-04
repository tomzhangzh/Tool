// cloudfunctions/checkin/index.js
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();
const _ = db.command;

const calcCalorie = (met, durationMinutes, weight = 30) => {
  return Math.round(met * weight * (durationMinutes / 60));
};

const formatDate = (date, fmt = 'YYYY-MM-DD') => {
  const o = {
    'YYYY': date.getFullYear(),
    'MM': String(date.getMonth() + 1).padStart(2, '0'),
    'DD': String(date.getDate()).padStart(2, '0')
  };
  return fmt.replace(/YYYY|MM|DD/g, m => o[m]);
};

exports.main = async (event, context) => {
  const wxCtx = cloud.getWXContext();
  const openid = wxCtx.OPENID;
  const action = event.action;

  try {
    // 提交打卡
    if (action === 'submit') {
      const { exerciseId, exerciseName, exerciseIcon, met, duration, note } = event;
      if (!exerciseId || !duration || duration <= 0) {
        return { code: 1, message: '参数错误' };
      }
      // 验证身份：仅学生可打卡
      const userQ = await db.collection('users').where({ openid }).get();
      if (!userQ.data.length) return { code: 1, message: '请先注册' };
      const user = userQ.data[0];
      if (user.role !== 'student') return { code: 1, message: '仅学生可打卡' };

      const now = new Date();
      const calorie = calcCalorie(met, duration, user.weight || 30);

      const addRes = await db.collection('checkins').add({
        data: {
          openid,
          userName: user.name,
          avatar: user.avatar,
          classIds: user.classIds || [],
          exerciseId,
          exerciseName,
          exerciseIcon,
          met,
          duration,
          calorie,
          note: note || '',
          createTime: now.getTime(),
          dateStr: formatDate(now),
          // 周/月标识便于后续聚合
          weekKey: getWeekKey(now),
          monthKey: getMonthKey(now)
        }
      });
      return { code: 0, data: { _id: addRes._id, calorie } };
    }

    // 今日打卡
    if (action === 'today') {
      const start = new Date();
      start.setHours(0, 0, 0, 0);
      const end = new Date();
      end.setHours(23, 59, 59, 999);
      const q = await db.collection('checkins')
        .where({ openid, createTime: _.gte(start.getTime()).and(_.lte(end.getTime())) })
        .orderBy('createTime', 'desc')
        .get();
      return { code: 0, data: q.data };
    }

    // 打卡历史（分页）
    if (action === 'list') {
      const page = event.page || 1;
      const pageSize = event.pageSize || 20;
      const targetOpenid = event.targetOpenid || openid; // 家长看孩子时传孩子 openid
      // 若为家长请求孩子数据，需校验绑定关系
      if (targetOpenid !== openid) {
        const userQ = await db.collection('users').where({ openid }).get();
        const u = userQ.data[0];
        if (!u || u.role === 'parent' && u.childOpenid !== targetOpenid) {
          return { code: 1, message: '无权查看' };
        }
      }
      const total = (await db.collection('checkins').where({ openid: targetOpenid }).count()).total;
      const q = await db.collection('checkins')
        .where({ openid: targetOpenid })
        .orderBy('createTime', 'desc')
        .skip((page - 1) * pageSize)
        .limit(pageSize)
        .get();
      return { code: 0, data: { list: q.data, total } };
    }

    // 删除打卡
    if (action === 'delete') {
      const { id } = event;
      const q = await db.collection('checkins').doc(id).get();
      if (!q.data || q.data.openid !== openid) {
        return { code: 1, message: '无权删除' };
      }
      await db.collection('checkins').doc(id).remove();
      return { code: 0, data: { ok: true } };
    }

    return { code: 1, message: '未知 action' };
  } catch (e) {
    console.error('checkin cloud function error', e);
    return { code: 1, message: String(e && e.message || e) };
  }
};

// 周标识：W20250105 (本周一日期)
function getWeekKey(date) {
  const day = date.getDay() || 7;
  const monday = new Date(date);
  monday.setDate(date.getDate() - day + 1);
  return `W${monday.getFullYear()}${String(monday.getMonth() + 1).padStart(2, '0')}${String(monday.getDate()).padStart(2, '0')}`;
}

function getMonthKey(date) {
  return `M${date.getFullYear()}${String(date.getMonth() + 1).padStart(2, '0')}`;
}
